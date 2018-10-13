using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes.Behaviors;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using GumPlugin.DataGeneration;

namespace GumPlugin.CodeGeneration
{
    class BehaviorCodeGenerator
    {
        internal string GenerateInterfaceCodeFor(BehaviorSave behavior)
        {
            CodeBlockBase fileLevel = new CodeBlockBase(null);
            ICodeBlock namespaceLevel = fileLevel.Namespace(GueDerivingClassCodeGenerator.GueRuntimeNamespace);

            StateCodeGenerator.Self.GenerateStateEnums(behavior, namespaceLevel, enumNamePrefix:behavior.Name);


            ICodeBlock interfaceLevel = namespaceLevel.Interface("public", $"I{behavior.Name}", "");


            GenerateInterface(interfaceLevel, behavior);

            return fileLevel.ToString();
        }

        private void GenerateInterface(ICodeBlock codeBlock, BehaviorSave behavior)
        {

            foreach (var category in behavior.Categories)
            {
                string propertyName = behavior.Name + category.Name;

                codeBlock.Line($"{propertyName} Current{propertyName}State {{set;}}");
            }

        }

        private void GenerateEnums(ICodeBlock codeBlock, BehaviorSave behavior)
        {
            foreach(var category in behavior.Categories)
            {
                codeBlock = codeBlock.Enum("public", category.Name);
            }

        }
    }
}
