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


            SaveEmbeddedFile("GlueControlManager.cs", 
                "GlueControlManager.Generated.cs");

            SaveEmbeddedFile("Editing.EditingManager.cs",
                "Editing/EditingManager.Generated.cs");

            SaveEmbeddedFile("Editing.SelectionLogic.cs",
                "Editing/SelectionLogic.Generated.cs");

            SaveEmbeddedFile("Editing.Markers.SelectionMarker.cs",
                "Editing/Markers/SelectionMarker.Generated.cs");

            SaveEmbeddedFile("Editing.VariableAssignmentLogic.cs",
                "Editing/VariableAssignmentLogic.Generated.cs");

            SaveEmbeddedFile("Dtos.cs",
                "Dtos.Generated.cs");

            SaveEmbeddedFile("InstanceLogic.cs",
                "InstanceLogic.Generated.cs");

            SaveEmbeddedFile("Forms.ObjectCreationWindow.cs",
                "Forms/ObjectCreationWindow.Generated.cs");

            SaveEmbeddedFile("Models.NamedObjectSave.cs",
                "Models/NamedObjectSave.Generated.cs");

            SaveEmbeddedFile("Editing.Guides.cs",
                "Editing/Guides.Generated.cs");

            SaveEmbeddedFile("Editing.CameraLogic.cs",
                "Editing/CameraLogic.Generated.cs");

            SaveEmbeddedFile("Screens.EntityViewingScreen.cs",
                "Screens/EntityViewingScreen.Generated.cs");
        }

        private static void SaveEmbeddedFile(string resourcePath, string relativeDestinationFilePath)
        {
            var prefix = "OfficialPlugins.Compiler.Embedded.";
            string glueControlManagerCode = GlueControlCodeGenerator.GetEmbeddedStringContents(prefix + resourcePath);
            FilePath destinationFilePath = glueControlFolder + relativeDestinationFilePath;
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(destinationFilePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(destinationFilePath.FullPath, glueControlManagerCode));
        }
    }
}
