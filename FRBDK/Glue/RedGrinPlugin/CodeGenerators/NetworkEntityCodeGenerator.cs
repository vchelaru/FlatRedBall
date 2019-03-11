using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using RedGrinPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedGrinPlugin.CodeGenerators
{
    class NetworkEntityCodeGenerator
    {
        public static void GenerateCodeFor(EntitySave entitySave)
        {
            var isNetworkEntity = NetworkEntityViewModel.IsNetworked(entitySave);

            if(isNetworkEntity)
            {
                var entityGeneratedCode = GetGeneratedEntityNetworkCode(entitySave);
                var generatedEntityNetworkFilePath = CodeGeneratorCommonLogic.GetGeneratedElementNetworkFilePathFor(entitySave);

                CodeGeneratorCommonLogic.SaveFile(entityGeneratedCode, generatedEntityNetworkFilePath);
                CodeGeneratorCommonLogic.AddCodeFileToProject(generatedEntityNetworkFilePath);

                var netStateGeneratedCode = GenerateNetStateGeneratedCode(entitySave);
                var generatedNetStateFilePath = CodeGeneratorCommonLogic.GetGeneratedNetStateFilePathFor(entitySave);

                CodeGeneratorCommonLogic.SaveFile(netStateGeneratedCode, generatedNetStateFilePath);
                CodeGeneratorCommonLogic.AddCodeFileToProject(generatedNetStateFilePath);

                var customNetStateFilePath = CodeGeneratorCommonLogic.GetCustomNetStateFilePathFor(entitySave);
                if(customNetStateFilePath.Exists() == false)
                {
                    var customNetStateCode = GenerateEmptyCustomNetStateCode(entitySave);
                    CodeGeneratorCommonLogic.SaveFile(customNetStateCode, customNetStateFilePath);
                }
                CodeGeneratorCommonLogic.AddCodeFileToProject(customNetStateFilePath);

                var customEntityNetworkFilePath = CodeGeneratorCommonLogic.GetCustomElementNetworkFilePathFor(entitySave);
                if(customEntityNetworkFilePath.Exists() == false)
                {
                    var customEntityNetworkCode = GenerateEmptyCustomEntityNetworkCode(entitySave);
                    CodeGeneratorCommonLogic.SaveFile(customEntityNetworkCode, customEntityNetworkFilePath);
                }
                CodeGeneratorCommonLogic.AddCodeFileToProject(customEntityNetworkFilePath);



                GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }

        #region Generated code methods

        private static string GetGeneratedEntityNetworkCode(EntitySave entitySave)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string entityNamespace = CodeGeneratorCommonLogic.GetElementNamespace(entitySave);

            ICodeBlock codeBlock = topBlock.Namespace(entityNamespace);

            codeBlock = codeBlock.Class("public partial", entitySave.GetStrippedName(), " : RedGrin.INetworkEntity");

            codeBlock.AutoProperty("public long", "OwnerId");
            codeBlock.AutoProperty("public long", "EntityId");

            GenerateGetStateMethod(entitySave, codeBlock);

            GenerateUpdatestateMethod(entitySave, codeBlock);

            return topBlock.ToString();
        }



        private static void GenerateUpdatestateMethod(EntitySave entitySave, ICodeBlock codeBlock)
        {
            var functionBlock = codeBlock.Function("public void", "UpdateState", "object entityState, double stateTime");

            var variables = GetNetworkVariables(entitySave);

            if(variables.Any())
            {
                var netStateFullName = CodeGeneratorCommonLogic.GetNetStateFullName(entitySave);

                //var state = entityState as NetStates.Entity2NetState();
                functionBlock.Line($"var state = entityState as {netStateFullName};");

                foreach(var variable in variables)
                {
                    functionBlock.Line($"this.{variable.Name} = state.{variable.Name};");
                }
                

            }

            functionBlock.Line("CustomUpdateFromState(state);");

        }

        private static void GenerateGetStateMethod(EntitySave entitySave, ICodeBlock codeBlock)
        {
            var netStateFullName = CodeGeneratorCommonLogic.GetNetStateFullName(entitySave);

            var getStateFunc = codeBlock.Function("public object", "GetState");
            getStateFunc.Line($"var state = new {netStateFullName}();");
            var variables = GetNetworkVariables(entitySave);
            foreach (var variable in variables)
            {
                getStateFunc.Line($"state.{variable.Name} = this.{variable.Name};");
            }

            getStateFunc.Line("CustomGetState(state);");

            getStateFunc.Line("return state;");
        }


        private static string GenerateNetStateGeneratedCode(EntitySave entitySave)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string netStateNamespace = CodeGeneratorCommonLogic.GetNetStateNamespace(entitySave);

            ICodeBlock codeBlock = topBlock.Namespace(netStateNamespace);

            codeBlock = codeBlock.Class("public partial", entitySave.GetStrippedName() + "NetState");

            var variables = GetNetworkVariables(entitySave);

            foreach (var variable in variables)
            {
                codeBlock.AutoProperty($"public {variable.Type}", variable.Name);
            }

            return topBlock.ToString();
        }

        private static CustomVariable[] GetNetworkVariables(EntitySave entitySave)
        {
            return entitySave.CustomVariables
                .Where(item =>
                    NetworkEntityViewModel.IsNetworked(item))
                .ToArray();
        }

        #endregion

        #region Empty custom code generation methods

        private static string GenerateEmptyCustomEntityNetworkCode(EntitySave entitySave)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string entityNamespace = CodeGeneratorCommonLogic.GetElementNamespace(entitySave);

            ICodeBlock codeBlock = topBlock.Namespace(entityNamespace);

            codeBlock = codeBlock.Class("public partial", entitySave.GetStrippedName());

            codeBlock.Function("void", "CustomUpdateFromState", $"{CodeGeneratorCommonLogic.GetNetStateFullName(entitySave)} state");
            codeBlock.Function("void", "CustomGetState", $"{CodeGeneratorCommonLogic.GetNetStateFullName(entitySave)} state");



            return topBlock.ToString();
        }

        private static string GenerateEmptyCustomNetStateCode(EntitySave entitySave)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string netStateNamespace = CodeGeneratorCommonLogic.GetNetStateNamespace(entitySave);

            ICodeBlock codeBlock = topBlock.Namespace(netStateNamespace);

            codeBlock = codeBlock.Class("public partial", entitySave.GetStrippedName() + "NetState");

            return topBlock.ToString();
        }

        #endregion

    }
}
