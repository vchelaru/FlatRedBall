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

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class GenerateCodeCommands : IGenerateCodeCommands
    {
        static IGlueState GlueState => Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => Container.Get<IGlueCommands>();

        public void GenerateAllCode()
        {
            TaskManager.Self.AddAsyncTask(
                () => GenerateAllCodeSync(new object()),
                "Generating all code"
                
                );
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

        public void GenerateGlobalContentCode()
        {
            GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();
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

            CameraSetupCodeGenerator.CallSetupCamera(ProjectManager.GameClassFileName, true);

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

            CsvCodeGenerator.GenerateAllCustomClasses(glueProject);

            GlueCommands.PrintOutput("Done with all generation");

        }

        public void GenerateCustomClassesCode()
        {
            CsvCodeGenerator.GenerateAllCustomClasses(GlueState.CurrentGlueProject);
        }


    }
}
