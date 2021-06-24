using System;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Managers;
using System.Linq;
using System.Collections.Generic;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using EditorObjects.IoC;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class GenerateCodeCommands : IGenerateCodeCommands
    {
        static IGlueState GlueState => Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => Container.Get<IGlueCommands>();

        public void GenerateAllCode()
        {
            TaskManager.Self.AddOrRunIfTasked(
                () => GenerateAllCodeSync(),
                "Generating all code",
                TaskExecutionPreference.AddOrMoveToEnd
                );
        }

        public void GenerateAllCodeTask()
        {

            TaskManager.Self.Add(
                () => GenerateAllCodeSync(),
                "Generating all code",
                TaskExecutionPreference.AddOrMoveToEnd

                );
        }

        public void GenerateCurrentElementCode()
        {
            IElement element = null;

            GlueCommands.DoOnUiThread(() => element = GlueState.CurrentElement);
            if (element != null)
            {
                TaskManager.Self.AddOrRunIfTasked(()  =>
                {
                    CodeWriter.GenerateCode(element);
                }, $"Generating element {element}", TaskExecutionPreference.AddOrMoveToEnd);
            }
        }

        public void GenerateElementCode(IElement element)
        {
            string taskName = nameof(GenerateElementCodeTask) + " " + element.ToString();

            TaskManager.Self.AddOrRunIfTasked(() => CodeGeneratorIElement.GenerateElementAndDerivedCode(element),
                taskName,
                TaskExecutionPreference.AddOrMoveToEnd);
        }

        public void GenerateElementAndReferencedObjectCodeTask(IElement element)
        {
            if (element != null)
            {
                GenerateElementCodeTask(element);

                var namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(element);

                foreach (var nos in namedObjects)
                {
                    var nosElement = ObjectFinder.Self.GetElementContaining(nos);
                    GenerateElementCodeTask(element);
                }
            }
        }

        public void GenerateElementCodeTask(IElement element)
        {
            string taskName = nameof(GenerateElementCodeTask) + " " + element.ToString();

            TaskManager.Self.Add(() => CodeGeneratorIElement.GenerateElementAndDerivedCode(element),
                taskName,
                TaskExecutionPreference.AddOrMoveToEnd);
        }

        public void GenerateGlobalContentCode()
        {
            TaskManager.Self.AddOrRunIfTasked(GlobalContentCodeGenerator.UpdateLoadGlobalContentCode, nameof(GenerateGlobalContentCode), TaskExecutionPreference.AddOrMoveToEnd);
        }

        public void GenerateGlobalContentCodeTask()
        {
            TaskManager.Self.Add(GlobalContentCodeGenerator.UpdateLoadGlobalContentCode, nameof(GenerateGlobalContentCode), TaskExecutionPreference.AddOrMoveToEnd);
        }

        public string GetNamespaceForElement(IElement element)
        {
            string directory = FileManager.GetDirectory(element.Name, RelativeType.Relative);

            string returnString = GlueState.ProjectNamespace + "." + directory.Replace('\\', '.').Replace('/', '.');
            // This ends in a period.
            returnString = returnString.Substring(0, returnString.Length - 1);
            return returnString;

        }

        public void GenerateCurrentCsvCode()
        {
            ReferencedFileSave rfs = GlueState.CurrentReferencedFileSave;
            if(rfs != null && rfs.IsCsvOrTreatedAsCsv)
            {
                TaskManager.Self.AddOrRunIfTasked(() =>
                {
                    CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);
                },
                nameof(GenerateCurrentCsvCode) + " " + rfs,
                TaskExecutionPreference.Fifo);
            }
        }
        
        /// <summary>
        /// Generates all code for the entire project synchronously.
        /// </summary>
        static void GenerateAllCodeSync  ()
        {
            var glueProject = GlueState.CurrentGlueProject;

            CameraSetupCodeGenerator.UpdateOrAddCameraSetup();

            CameraSetupCodeGenerator.GenerateCallInGame1(ProjectManager.GameClassFileName, true);

            //Parallel.For(0, layer.data[0].tiles.Count, (count) =>
            GlueCommands.PrintOutput("Starting to generate all Screens");

            // make sure the user hasn't exited the program

            foreach (var screen in glueProject.Screens)
            {
                #region Check for exiting the function becuase Glue is closing

                if (ProjectManager.WantsToClose)
                {
                    PluginManager.ReceiveOutput("Stopping generation because the project is closing");
                    return;
                }

                #endregion


                CodeWriter.GenerateCode(screen);

            }

            GlueCommands.PrintOutput("Done generating Screens, starting Entities");

            // not sure which is faster:
            // Currently the referenced file dictionary makes this not work well:
            //Parallel.ForEach(GlueState.Self.CurrentGlueProject.Entities, (entity) =>
            //    {
            //        CodeWriter.GenerateCode(entity);
            //    });

            // Let's make this thread safe:
            IEnumerable<EntitySave> entityList = null;
            lock (glueProject.Entities)
            {
                entityList = glueProject.Entities.Where(item => true).ToList();
            }

            foreach (var entity in entityList)
            {
                #region Check for exiting the function becuase Glue is closing

                if (ProjectManager.WantsToClose)
                {
                    PluginManager.ReceiveOutput("Stopping generation because the project is closing");
                    return;
                }

                #endregion

                CodeWriter.GenerateCode(entity);
            }




            PluginManager.ReceiveOutput("Done generating Entities, starting GlobalContent");

            #region Check for exiting the function becuase Glue is closing

            if (ProjectManager.WantsToClose)
            {
                PluginManager.ReceiveOutput("Stopping generation because the project is closing");
                return;
            }

            #endregion

            GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();



            #region Check for exiting the function becuase Glue is closing

            if (ProjectManager.WantsToClose)
            {
                PluginManager.ReceiveOutput("Stopping generation because the project is closing");
                return;
            }

            #endregion

            CsvCodeGenerator.RegenerateAllCsvs();
            CsvCodeGenerator.GenerateAllCustomClasses(glueProject);

            if(glueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.AddedGeneratedGame1)
            {
                GlueCommands.GenerateCodeCommands.GenerateGame1();
            }

            GlueCommands.PrintOutput("Done with all generation");

        }

        public void GenerateCustomClassesCode()
        {
            CsvCodeGenerator.GenerateAllCustomClasses(GlueState.CurrentGlueProject);
        }

        public void GenerateStartupScreenCode()
        {
            CodeWriter.RefreshStartupScreenCode();
        }

        public void GenerateGame1()
        {
            TaskManager.Self.AddOrRunIfTasked(
                GenerateGame1Internal,
                "Generating Game1.Generated.cs",
                TaskExecutionPreference.AddOrMoveToEnd
                );
        }

        void GenerateGame1Internal()
        {
            var code = Game1CodeGeneratorManager.GetGame1GeneratedContents();

            FilePath filePath = GlueState.CurrentGlueProjectDirectory + "Game1.Generated.cs";

            GlueCommands.TryMultipleTimes(() =>
            {
                System.IO.File.WriteAllText(filePath.FullPath, code);

                GlueCommands.ProjectCommands.CreateAndAddCodeFile(filePath);
            },
            5);
        }
    }
}
