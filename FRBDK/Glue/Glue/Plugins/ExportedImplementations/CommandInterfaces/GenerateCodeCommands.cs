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

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class GenerateCodeCommands : IGenerateCodeCommands
    {
        static IGlueState GlueState => Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => Container.Get<IGlueCommands>();

        [Obsolete("Use GenerateAllCodeTask")]
        public void GenerateAllCode()
        {
            GenerateAllCodeTask();
        }

        public void GenerateAllCodeTask()
        {

            TaskManager.Self.Add(
                () => GenerateAllCodeSync(new object()),
                "Generating all code",
                TaskExecutionPreference.AddOrMoveToEnd

                );
        }

        public void GenerateCurrentElementCodeTask()
        {
            var element = GlueState.CurrentElement;
            TaskManager.Self.Add(()  =>
            {

                if (element != null)
                {
                    CodeWriter.
                    GenerateCode(element);
                }
            }, $"Generating element {element}", TaskExecutionPreference.AddOrMoveToEnd);
        }

        public void GenerateCurrentElementCode()
        {
            var element = GlueState.CurrentElement;

            if (element != null)
            {
                CodeWriter.GenerateCode(element);
            }
        }

        public void GenerateElementCode(IElement element)
        {
            CodeGeneratorIElement.GenerateElementAndDerivedCode(element);
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
            GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();
        }

        public void GenerateGlobalContentCodeTask()
        {
            TaskManager.Self.Add(GenerateGlobalContentCode, nameof(GenerateGlobalContentCode), TaskExecutionPreference.AddOrMoveToEnd);
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
                CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);
            }
        }
        
        /// <summary>
        /// Generates all code for the entire project synchronously.
        /// </summary>
        public void GenerateAllCodeSync()
        {
            GenerateAllCodeSync(null);
        }

        static void GenerateAllCodeSync  (object throwaway)
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

        public void GenerateGame1Task()
        {
            TaskManager.Self.Add(
                GenerateGame1,
                "Generating Game1.Generated.cs",
                TaskExecutionPreference.AddOrMoveToEnd
                );
        }

        public void GenerateGame1()
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
