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
        static string glueControlFolder => GlueState.Self.CurrentGlueProjectDirectory + "GlueControl/";

        public static void GenerateAll(bool fullyGenerate)
        {
            SaveEmbeddedFile(GlueControlCodeGenerator.GetGlueControlManagerContents(fullyGenerate), 
                "GlueControlManager.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetEditingManagerContents(fullyGenerate), 
                "Editing/EditingManager.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetSelectionLogicContents(fullyGenerate),
                "Editing/SelectionLogic.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetSelectionMarkerContents(fullyGenerate),
                "Editing/SelectionMarker.Generated.cs");
        }

        private static void SaveEmbeddedFile(string glueControlManagerCode, string relativeDestinationFilePath)
        {
            FilePath destinationFilePath = glueControlFolder + relativeDestinationFilePath;
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(destinationFilePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(destinationFilePath.FullPath, glueControlManagerCode));
        }
    }
}
