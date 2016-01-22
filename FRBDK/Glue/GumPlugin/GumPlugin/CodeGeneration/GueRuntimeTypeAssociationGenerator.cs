using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    public class GueRuntimeTypeAssociationGenerator : Singleton<GueRuntimeTypeAssociationGenerator>
    {



        public string GetRuntimeRegistrationPartialClassContents()
        {
            CodeBlockBase codeBlock = new CodeBlockBase(null);

            ICodeBlock currentBlock = codeBlock.Namespace("FlatRedBall.Gum");

            currentBlock = currentBlock.Class("public partial", "GumIdb", "");

            currentBlock = currentBlock.Function("public static void", "RegisterTypes", "");
            {
                AddAssignmentFunctionContents(currentBlock);
            }


            return codeBlock.ToString();
        }

        void AddAssignmentFunctionContents(ICodeBlock codeBlock)
        {
            foreach (var element in AppState.Self.AllLoadedElements)
            {
                if (GueDerivingClassCodeGenerator.Self.ShouldGenerateRuntimeFor(element))
                {
                    AddRegisterCode(codeBlock, element);
                }
            }
        }

        private static void AddRegisterCode(ICodeBlock codeBlock, Gum.DataTypes.ElementSave element)
        {
            // don't remove the path:
            string unqualifiedName = FlatRedBall.IO.FileManager.RemovePath(  element.Name );
            string elementNameString = element.Name.Replace("\\", "\\\\") ;

            codeBlock.Line(
                "GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType(\"" +
                elementNameString +
                "\", typeof(" +
                GueDerivingClassCodeGenerator.GueRuntimeNamespace + "." +
                unqualifiedName + "Runtime" +
                "));");
        }
    }
}
