using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedGrinPlugin.CodeGenerators
{
    class MessagesCodeGenerator
    {
        public static void GenerateAllMessages()
        {
            GenerateClaimEntityMessage();
        }

        private static void GenerateClaimEntityMessage()
        {
            var code = GenerateClaimEntityMessageCode();

            var filePath = GetClaimEntityFilePath();

            CodeGeneratorCommonLogic.SaveFile(code, filePath);

            CodeGeneratorCommonLogic.AddCodeFileToProject(filePath);
        }

        private static string GenerateClaimEntityMessageCode()
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            ICodeBlock codeBlock = topBlock.Namespace(GlueState.Self.ProjectNamespace + ".Messages");

            codeBlock = codeBlock.Class("", "ClaimEntity");

            codeBlock.AutoProperty("public string", "EntityName");
            codeBlock.AutoProperty("public long", "OwnerId");
            codeBlock.AutoProperty("public long", "EntityId");


            return topBlock.ToString();
        }

        private static FilePath GetClaimEntityFilePath()
        {
            return GlueState.Self.CurrentGlueProjectDirectory + "Messages\\ClaimEntityMessage.Generated.cs";
        }
    }
}
