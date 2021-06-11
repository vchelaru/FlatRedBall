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
            GlueControlCodeGenerator.GenerateFull = fullyGenerate;
            SaveEmbeddedFile(GlueControlCodeGenerator.GetGlueControlManagerContents(), 
                "GlueControlManager.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetEditingManagerContents(), 
                "Editing/EditingManager.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetSelectionLogicContents(),
                "Editing/SelectionLogic.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetSelectionMarkerContents(),
                "Editing/SelectionMarker.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetEmbeddedStringContents(
                "OfficialPlugins.Compiler.Embedded.Editing.VariableAssignmentLogic.cs"),
                "Editing/VariableAssignmentLogic.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetDtosContents(),
                "Dtos.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetInstanceLogicContents(),
                "InstanceLogic.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetEmbeddedStringContents(
                "OfficialPlugins.Compiler.Embedded.Forms.ObjectCreationWindow.cs"),
                "Forms/ObjectCreationWindow.Generated.cs");

            SaveEmbeddedFile(GlueControlCodeGenerator.GetEmbeddedStringContents(
                "OfficialPlugins.Compiler.Embedded.Models.NamedObjectSave.cs"),
                "Models/NamedObjectSave.Generated.cs");

        }

        private static void SaveEmbeddedFile(string glueControlManagerCode, string relativeDestinationFilePath)
        {
            FilePath destinationFilePath = glueControlFolder + relativeDestinationFilePath;
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(destinationFilePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(destinationFilePath.FullPath, glueControlManagerCode));
        }
    }
}
