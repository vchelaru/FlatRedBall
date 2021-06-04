using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.CodeGeneration
{
    public static class MainCodeGenerator
    {
        public static void GenerateAll(bool fullyGenerate)
        {
            var glueControlManagerCode = GlueControlCodeGenerator.GetStringContents(fullyGenerate);
            var glueControlFolder = GlueState.Self.CurrentGlueProjectDirectory + "GlueControl/";
            FilePath glueControlManagerFilePath = glueControlFolder + "GlueControlManager.Generated.cs";
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(glueControlManagerFilePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(glueControlManagerFilePath.FullPath, glueControlManagerCode));
        }
    }
}
