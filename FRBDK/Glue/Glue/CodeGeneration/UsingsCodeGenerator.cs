using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Utilities;

namespace FlatRedBall.Glue.CodeGeneration
{
    public static class UsingsCodeGenerator
    {
        // We need to make this be part of the function so that we can have everything be async
        //static List<string> mUsingStrings = new List<string>();
        internal static void GenerateUsingStatements(ICodeBlock codeBlock, IElement SaveObject)
        {
            List<string> usingStrings = new List<string>();

            if (SaveObject is EntitySave)
            {
                EntitySave asEntitySave = (EntitySave)SaveObject;

                bool hasScreensInProject = ProjectManager.GlueProjectSave.Screens.Count != 0;
                if (hasScreensInProject)
                {
                    usingStrings.Add(ProjectManager.ProjectNamespace + ".Screens");
                }

                // I don't think we need this anymore
                //mUsingStrings.Add("Matrix = Microsoft.Xna.Framework.Matrix");


                // Since Entities inherit are IDestroyable
                usingStrings.Add("FlatRedBall.Graphics");
                // And since we want a Visible property, we'll have extension methods for Lists of this
                usingStrings.Add("FlatRedBall.Math");


                if (asEntitySave.CreatedByOtherEntities)
                {
                    usingStrings.Add(ProjectManager.ProjectNamespace + ".Performance");
                }

                if (asEntitySave.ImplementsIClickable || asEntitySave.ImplementsIWindow)
                {
                   usingStrings.Add("FlatRedBall.Gui");
                }
            }

            // We are phasing this out:
            //mUsingStrings.Add("FlatRedBall.Broadcasting");



            #region Add the using PROJECT.Factories if necessary
            if (ProjectManager.GlueProjectSave.Entities.Count != 0)
            {
                // add their usings
                for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
                {
                    EntitySave entitySave = ProjectManager.GlueProjectSave.Entities[i];

                    string entityNamespace = FileManager.MakeRelative(FileManager.GetDirectory(entitySave.Name)).Replace('/', '.');
                    entityNamespace = ProjectManager.ProjectNamespace + "." + entityNamespace.Substring(0, entityNamespace.Length - 1);

                    if (!usingStrings.Contains(entityNamespace))
                    {
                        usingStrings.Add(entityNamespace);
                    }
                }



                for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
                {
                    if (ProjectManager.GlueProjectSave.Entities[i].CreatedByOtherEntities)
                    {
                        usingStrings.Add(ProjectManager.ProjectNamespace + ".Factories");
                        break;
                    }
                }
            }

            #endregion

            usingStrings.Add("FlatRedBall");
            usingStrings.Add("FlatRedBall.Screens");

            usingStrings.Add("System");
            usingStrings.Add("System.Collections.Generic");
            usingStrings.Add("System.Text");

            bool shouldAddUsingForDataTypes = false;

            for (int i = 0; i < SaveObject.ReferencedFiles.Count; i++)
            {
                if (FileManager.GetExtension(SaveObject.ReferencedFiles[i].Name) == "csv")
                {
                    shouldAddUsingForDataTypes = true;
                    break;
                }
            }

            if (shouldAddUsingForDataTypes)
            {
                usingStrings.Add(ProjectManager.ProjectNamespace + ".DataTypes");
                usingStrings.Add("FlatRedBall.IO.Csv");
            }

            NamedObjectSaveCodeGenerator.AddUsingsForNamedObjects(usingStrings, SaveObject);

            if (ObjectFinder.Self.GlueProject.UsesTranslation)
            {
                usingStrings.Add("FlatRedBall.Localization");
            }

            // Plugins don't generate using statements.
            // This is intentional so we don't get naming conflicts
            
            // Remove duplicates
            StringFunctions.RemoveDuplicates(usingStrings);

            foreach (string s in usingStrings.Distinct())
            {
                codeBlock.Line("using " + s + ";");
            }
        }



    }
}
