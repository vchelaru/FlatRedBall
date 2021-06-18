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

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.GlueControlManager.cs", 
                "GlueControlManager.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.Editing.EditingManager.cs",
                "Editing/EditingManager.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.Editing.SelectionLogic.cs",
                "Editing/SelectionLogic.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.Editing.Markers.SelectionMarker.cs",
                "Editing/Markers/SelectionMarker.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.Editing.VariableAssignmentLogic.cs",
                "Editing/VariableAssignmentLogic.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.Dtos.cs",
                "Dtos.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.InstanceLogic.cs",
                "InstanceLogic.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.Forms.ObjectCreationWindow.cs",
                "Forms/ObjectCreationWindow.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.Models.NamedObjectSave.cs",
                "Models/NamedObjectSave.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.Editing.Guides.cs",
                "Editing/Guides.Generated.cs");

            SaveEmbeddedFile("OfficialPlugins.Compiler.Embedded.Editing.CameraLogic.cs",
                "Editing/CameraLogic.Generated.cs");
        }

        private static void SaveEmbeddedFile(string resourcePath, string relativeDestinationFilePath)
        {
            string glueControlManagerCode = GlueControlCodeGenerator.GetEmbeddedStringContents(resourcePath);
            FilePath destinationFilePath = glueControlFolder + relativeDestinationFilePath;
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(destinationFilePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(destinationFilePath.FullPath, glueControlManagerCode));
        }
    }
}
