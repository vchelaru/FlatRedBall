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
using System.Threading.Tasks;

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

        
        public void GenerateCurrentElementCode()
        {
            GlueElement element = GlueState.CurrentElement;
            if (element != null)
            {
                var throwaway = TaskManager.Self.AddAsync(async ()  =>
                {
                    // This can happen when the user exits the program, so let's check:
                    if(GlueState.CurrentGlueProject != null)
                    {
                        await CodeWriter.GenerateCode(element);
                    }
                }, $"Generating element {element}", TaskExecutionPreference.AddOrMoveToEnd);
            }
        }

        public void GenerateElementCode(GlueElement element)
        {
            if(element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            string taskName = nameof(GenerateElementCode) + " " + element.ToString();

            var throwaway = TaskManager.Self.AddAsync(async () => await CodeGeneratorIElement.GenerateElementAndDerivedCode(element),
                taskName,
                TaskExecutionPreference.AddOrMoveToEnd);
        }

        public Task GenerateElementCodeAsync(GlueElement element)
        {
            string taskName = nameof(GenerateElementCode) + " " + element.ToString();

            return TaskManager.Self.AddAsync(async () => await CodeGeneratorIElement.GenerateElementAndDerivedCode(element),
                taskName,
                TaskExecutionPreference.AddOrMoveToEnd);
        }

        public async Task GenerateElementAndReferencedObjectCode(GlueElement element)
        {
            HashSet<GlueElement> toRegenerateHashSet = new HashSet<GlueElement>();
            if (element != null)
            {
                toRegenerateHashSet.Add(element);


                var namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(element);
                
                foreach (var nos in namedObjects)
                {
                    var nosElement = ObjectFinder.Self.GetElementContaining(nos);
                    toRegenerateHashSet.Add(nosElement);

                }
            }
            foreach(var elementToRegenerate in toRegenerateHashSet)
            {
                await GenerateElementCodeAsync(elementToRegenerate);
            }
        }

        /// <summary>
        /// Generates global content code in a task, or runs immediately if already in a task.
        /// </summary>
        public void GenerateGlobalContentCode()
        {
            TaskManager.Self.AddOrRunIfTasked(GlobalContentCodeGenerator.UpdateLoadGlobalContentCode, nameof(GenerateGlobalContentCode), TaskExecutionPreference.AddOrMoveToEnd);
        }


        public void GenerateGlobalContentCodeTask()
        {
            TaskManager.Self.Add(GlobalContentCodeGenerator.UpdateLoadGlobalContentCode, nameof(GenerateGlobalContentCode), TaskExecutionPreference.AddOrMoveToEnd);
        }

        public string GetNamespaceForElement(GlueElement element)
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
            ReferencedFileSaveCodeGenerator.RefreshGlobalContentDictionary();
            GlobalContentCodeGenerator.SuppressGlobalContentDictionaryRefresh = true;
            var glueProject = GlueState.CurrentGlueProject;

            CameraSetupCodeGenerator.UpdateOrAddCameraSetup();

            CameraSetupCodeGenerator.GenerateCallInGame1(ProjectManager.GameClassFileName, true);

            foreach (var screen in glueProject.Screens)
            {
                #region Check for exiting the function becuase Glue is closing

                if (ProjectManager.WantsToCloseProject)
                {
                    PluginManager.ReceiveOutput("Stopping generation because the project is closing");
                    return;
                }

                #endregion

                CodeWriter.GenerateCode(screen);
            }


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

                if (ProjectManager.WantsToCloseProject)
                {
                    PluginManager.ReceiveOutput("Stopping generation because the project is closing");
                    return;
                }

                #endregion

                CodeWriter.GenerateCode(entity);
            }

            #region Check for exiting the function becuase Glue is closing

            if (ProjectManager.WantsToCloseProject)
            {
                PluginManager.ReceiveOutput("Stopping generation because the project is closing");
                return;
            }

            #endregion

            GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

            #region Check for exiting the function becuase Glue is closing

            if (ProjectManager.WantsToCloseProject)
            {
                PluginManager.ReceiveOutput("Stopping generation because the project is closing");
                return;
            }

            #endregion

            CsvCodeGenerator.RegenerateAllCsvs();

            CsvCodeGenerator.GenerateAllCustomClasses(glueProject);

            GlueCommands.GenerateCodeCommands.GenerateStartupScreenCode();

            if (glueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.AddedGeneratedGame1)
            {
                GlueCommands.GenerateCodeCommands.GenerateGame1();
            }

            GlobalContentCodeGenerator.SuppressGlobalContentDictionaryRefresh = false;

        }

        public void GenerateCustomClassesCode()
        {
            CsvCodeGenerator.GenerateAllCustomClasses(GlueState.CurrentGlueProject);
        }

        public async void GenerateStartupScreenCode()
        {
            var glueProjectVersion = GlueState.CurrentGlueProject.FileVersion;

            if(glueProjectVersion < (int)GlueProjectSave.GluxVersions.StartupInGeneratedGame)
            {
                CodeWriter.RefreshStartupScreenCode();
            }


            // On Version 13 and later, we do this:
            await TaskManager.Self.AddAsync(
                GenerateGame1Internal,
                "Generating Game1.Generated.cs",
                TaskExecutionPreference.AddOrMoveToEnd
                );
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
            if(GlueState.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.AddedGeneratedGame1)
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
}
