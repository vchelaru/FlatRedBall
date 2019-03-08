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
    class NetworkCodeGenerator
    {
        public static void GenerateCodeFor(EntitySave entitySave)
        {
            var isNetworkEntity = entitySave.Properties
                .GetValue<bool>(nameof(NetworkEntityViewModel.IsNetworkEntity));

            if(isNetworkEntity)
            {
                var entityGeneratedCode = GetGeneratedEntityNetworkCode(entitySave);
                var generatedEntityNetworkFilePath = GetGeneratedentityNetworkFileNameFor(entitySave);

                SaveFile(entityGeneratedCode, generatedEntityNetworkFilePath);
                AddCodeFileToProject(generatedEntityNetworkFilePath);

                var netStateGeneratedCode = GenerateNetStateGeneratedCode(entitySave);
                var generatedNetStateFilePath = GetGeneratedNetStateFilePathFor(entitySave);

                SaveFile(netStateGeneratedCode, generatedNetStateFilePath);
                AddCodeFileToProject(generatedNetStateFilePath);

                var customNetStateFilePath = GetCustomNetStateFilePathFor(entitySave);
                if(customNetStateFilePath.Exists() == false)
                {
                    var customNetStateCode = GenerateEmptyCustomNetStateCode(entitySave);
                    SaveFile(customNetStateCode, customNetStateFilePath);
                }
                AddCodeFileToProject(customNetStateFilePath);

                var customEntityNetworkFilePath = GetCustomEntityNetworkFilePath(entitySave);
                if(customEntityNetworkFilePath.Exists() == false)
                {
                    var customEntityNetworkCode = GenerateEmptyCustomEntityNetworkCode(entitySave);
                    SaveFile(customEntityNetworkCode, customEntityNetworkFilePath);
                }
                AddCodeFileToProject(customEntityNetworkFilePath);
            }
        }

        #region FilePath Methods

        private static FilePath GetCustomEntityNetworkFilePath(EntitySave entitySave)
        {
            return GlueState.Self.CurrentGlueProjectDirectory +
                entitySave.Name + 
                ".Network.cs";
        }

        private static FilePath GetGeneratedentityNetworkFileNameFor(EntitySave entitySave)
        {
            return GlueState.Self.CurrentGlueProjectDirectory +
                entitySave.Name +
                ".Generated.Network.cs";
        }

        private static FilePath GetGeneratedNetStateFilePathFor(EntitySave entitySave)
        {
            return GlueState.Self.CurrentGlueProjectDirectory +
                entitySave.Name +
                "NetState.Generated.cs";
        }

        private static FilePath GetCustomNetStateFilePathFor(EntitySave entitySave)
        {
            return GlueState.Self.CurrentGlueProjectDirectory +
                entitySave.Name +
                "NetState.cs";
        }

        #endregion

        #region Generated code methods

        private static string GetGeneratedEntityNetworkCode(EntitySave entitySave)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string entityNamespace = GlueState.Self.ProjectNamespace +
                "." + entitySave.Name.Replace("/", ".").Replace("\\", ".").Substring(
                0, entitySave.Name.Length - (entitySave.ClassName.Length + 1));

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
                var netStateFullName = GetNetStateFullName(entitySave);

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
            var netStateFullName = GetNetStateFullName(entitySave);

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

        private static string GetNetStateFullName(EntitySave entitySave)
        {
            var netStateFullName = $"{GetNetStateNamespace(entitySave)}.{entitySave.GetStrippedName()}NetState";

            return netStateFullName;
        }

        private static string GenerateNetStateGeneratedCode(EntitySave entitySave)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string netStateNamespace = GetNetStateNamespace(entitySave);

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
                    item.Properties.GetValue<bool>(NetworkEntityViewModel.IsNetworkVariableProperty))
                .ToArray();
        }

        #endregion

        #region Empty custom code generation methods

        private static string GenerateEmptyCustomEntityNetworkCode(EntitySave entitySave)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string entityNamespace = GlueState.Self.ProjectNamespace +
                "." + entitySave.Name.Replace("/", ".").Replace("\\", ".").Substring(
                0, entitySave.Name.Length - (entitySave.ClassName.Length + 1));

            ICodeBlock codeBlock = topBlock.Namespace(entityNamespace);

            codeBlock = codeBlock.Class("public partial", entitySave.GetStrippedName());

            codeBlock.Function("void", "CustomUpdateFromState", $"{GetNetStateFullName(entitySave)} state");
            codeBlock.Function("void", "CustomGetState", $"{GetNetStateFullName(entitySave)} state");



            return topBlock.ToString();
        }

        private static string GenerateEmptyCustomNetStateCode(EntitySave entitySave)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string netStateNamespace = GetNetStateNamespace(entitySave);

            ICodeBlock codeBlock = topBlock.Namespace(netStateNamespace);

            codeBlock = codeBlock.Class("public partial", entitySave.GetStrippedName() + "NetState");

            return topBlock.ToString();
        }

        private static string GetNetStateNamespace(EntitySave entitySave)
        {
            string entityNamespace =
                entitySave.Name.Replace("/", ".").Replace("\\", ".").Substring(
                0, entitySave.Name.Length - (entitySave.ClassName.Length + 1));

            var removedEntities = entityNamespace.Substring("Entities".Length);

            if(string.IsNullOrEmpty(removedEntities))
            {
                entityNamespace = "NetStates";
            }
            else
            {
                entityNamespace = "NetStates." + removedEntities;
            }

            entityNamespace = "." + entityNamespace;
            entityNamespace = GlueState.Self.ProjectNamespace + entityNamespace;
            return entityNamespace;
        }

        #endregion

        #region Utility Methods

        private static void SaveFile(string code, FilePath filePath)
        {
            System.IO.Directory.CreateDirectory(filePath.GetDirectoryContainingThis().FullPath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(filePath.FullPath, code));
        }

        private static void AddCodeFileToProject(FilePath filePath)
        {
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(filePath);
        }

        #endregion
    }
}
