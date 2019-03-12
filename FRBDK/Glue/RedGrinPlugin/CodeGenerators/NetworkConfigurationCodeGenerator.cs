using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using RedGrinPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RedGrinPlugin.CodeGenerators
{
    class NetworkConfigurationCodeGenerator
    {
        public static void GenerateConfiguration()
        {
            var configurationCode = GenerateConfigurationCode();

            var filePath = GetConfigurationFilePath();

            CodeGeneratorCommonLogic.SaveFile(configurationCode, filePath);
            CodeGeneratorCommonLogic.AddCodeFileToProject(filePath);
        }

        private static string GenerateConfigurationCode()
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            var fileName = GlueState.Self.CurrentGlueProjectFileName;

            var projectName = FileManager.RemoveExtension(FileManager.RemovePath(fileName));

            ICodeBlock codeBlock = topBlock.Namespace(GlueState.Self.ProjectNamespace);
            codeBlock = codeBlock.Class("", "GameNetworkConfiguration : RedGrin.NetworkConfiguration");

            var constructor = codeBlock.Constructor("public", "GameNetworkConfiguration", "");


            var portNumber = projectName.GetHashCode() % (ushort.MaxValue - 1024) + 1024;
            

            constructor.Line($"ApplicationName = \"{projectName}\";");
            constructor.Line($"ApplicationPort = {portNumber};");
            constructor.Line($"DeadReckonSeconds = 1.0f;");
            constructor.Line($"EntityStateTypes = new System.Collections.Generic.List<System.Type>();");

            var netEntities = GlueState.Self.CurrentGlueProject.Entities
                .Where(item => NetworkEntityViewModel.IsNetworked(item));

            foreach(var netEntity in netEntities)
            {
                var netStateFullName = CodeGeneratorCommonLogic.GetNetStateFullName(netEntity);
                constructor.Line(
                    $"EntityStateTypes.Add(typeof({netStateFullName}));");
            }
            return topBlock.ToString();
        }

        private static FilePath GetConfigurationFilePath()
        {
            return GlueState.Self.CurrentGlueProjectDirectory + "GameNetworkConfiguration.Generated.cs";
        }

    }
}
