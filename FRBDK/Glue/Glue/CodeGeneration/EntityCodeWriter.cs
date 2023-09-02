using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.CodeGeneration
{
    internal static class EntityCodeWriter
    {
        public static void GenerateFieldsAndProperties(EntitySave entity, ICodeBlock codeBlock)
        {
            if (entity.InheritsFromElement())
            {
                string baseQualifiedName = ProjectManager.ProjectNamespace + "." + entity.BaseElement.Replace("\\", ".");

                codeBlock.Line("// This is made static so that static lazy-loaded content can access it.");
                codeBlock.Property("public static new string", "ContentManagerName")
                    .Get().Line($"return {baseQualifiedName}.ContentManagerName;").End()
                    .Set().Line($"{baseQualifiedName}.ContentManagerName = value;").End();
            }
            else
            {
                codeBlock.Line("// This is made static so that static lazy-loaded content can access it.");
                codeBlock.AutoProperty("public static string", "ContentManagerName");

            }

            // This is to detect double-activity calls
            codeBlock.Line("#if DEBUG");
            codeBlock.Line("private double mLastTimeCalledActivity;");
            codeBlock.Line("#endif");

        }

        public static void GenerateActivity(IElement saveObject, ICodeBlock codeBlock)
        {
            codeBlock.Line("#if DEBUG");
            
            // in codeblock, write code to check if the mLastTimeCalledActivity is equal to the current time. If so, throw an exception
            codeBlock.Line("if(mLastTimeCalledActivity == FlatRedBall.TimeManager.CurrentScreenTime)")
                .Line("{")
                .Line("    throw new System.Exception(\"Activity was called twice in the same frame. This can cause objects to move 2x as fast.\");")
                .Line("}")
                .Line("mLastTimeCalledActivity = FlatRedBall.TimeManager.CurrentScreenTime;")
                .Line("#endif");

        }
    }
}
