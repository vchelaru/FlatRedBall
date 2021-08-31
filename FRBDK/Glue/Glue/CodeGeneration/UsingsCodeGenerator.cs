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

            // Not sure if we still need this, as we've been slowly ripping out using statements from generated code.
            usingStrings.Add("using Color = Microsoft.Xna.Framework.Color;");
            // Need this for FindByNameSyntax which may use linq
            usingStrings.Add("using System.Linq;");

            if (SaveObject is EntitySave)
            {
                EntitySave asEntitySave = (EntitySave)SaveObject;

                // I don't think we need this anymore
                //mUsingStrings.Add("Matrix = Microsoft.Xna.Framework.Matrix");


                // Since Entities inherit are IDestroyable
                usingStrings.Add("FlatRedBall.Graphics");
                // And since we want a Visible property, we'll have extension methods for Lists of this
                usingStrings.Add("FlatRedBall.Math");

                if (asEntitySave.ImplementsIClickable || asEntitySave.ImplementsIWindow)
                {
                   usingStrings.Add("FlatRedBall.Gui");
                }
            }

            // We are phasing this out:
            //mUsingStrings.Add("FlatRedBall.Broadcasting");


            usingStrings.Add("FlatRedBall");

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
