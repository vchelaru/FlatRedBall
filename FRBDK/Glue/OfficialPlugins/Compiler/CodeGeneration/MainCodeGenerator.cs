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
        public static void GenerateAll()
        {
            var glueControlManagerCode = GlueControlCodeGenerator.GetStringContents();
            FilePath filePath = GlueState.Self.CurrentGlueProjectDirectory + 
                "GlueControl/GlueControlManager.Generated.cs";
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(filePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(filePath.FullPath, glueControlManagerCode));
        }
    }
}
