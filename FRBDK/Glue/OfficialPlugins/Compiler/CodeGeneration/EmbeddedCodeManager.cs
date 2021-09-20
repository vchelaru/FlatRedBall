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
    public static class EmbeddedCodeManager
    {
        static string glueControlFolder => GlueState.Self.CurrentGlueProjectDirectory + "GlueControl/";

        public static void EmbedAll(bool fullyGenerate)
        {
            GlueControlCodeGenerator.GenerateFull = fullyGenerate;

            SaveEmbeddedFile("Editing.Markers.SelectionMarker.cs",
                "Editing/Markers/SelectionMarker.Generated.cs");

            SaveEmbeddedFile("Editing.Markers.TileShapeCollectionMarker.cs",
                "Editing/Markers/TileShapeCollectionMarker.Generated.cs");

            SaveEmbeddedFile("Editing.Visuals.Arrow.cs",
                "Editing/Visuals/Arrow.Generated.cs");

            SaveEmbeddedFile("Editing.CameraLogic.cs",
                "Editing/CameraLogic.Generated.cs");

            SaveEmbeddedFile("Editing.CopyPasteManager.cs",
                "Editing/CopyPasteManager.Generated.cs");

            SaveEmbeddedFile("Editing.EditingManager.cs",
                "Editing/EditingManager.Generated.cs");

            SaveEmbeddedFile("Editing.EditorVisuals.cs",
                "Editing/EditorVisuals.Generated.cs");


            SaveEmbeddedFile("GlueControlManager.cs", 
                "GlueControlManager.Generated.cs");

            SaveEmbeddedFile("CommandReceiver.cs",
                "CommandReceiver.Generated.cs");

            SaveEmbeddedFile("Dtos.cs",
                "Dtos.Generated.cs");


            SaveEmbeddedFile("Editing.SelectionLogic.cs",
                "Editing/SelectionLogic.Generated.cs");

            SaveEmbeddedFile("Editing.VariableAssignmentLogic.cs",
                "Editing/VariableAssignmentLogic.Generated.cs");

            SaveEmbeddedFile("Editing.Guides.cs",
                "Editing/Guides.Generated.cs");

            SaveEmbeddedFile("Editing.MoveObjectToContainerLogic.cs",
                "Editing/MoveObjectToContainerLogic.Generated.cs");

            SaveEmbeddedFile("InstanceLogic.cs",
                "InstanceLogic.Generated.cs");

            SaveEmbeddedFile("Forms.ObjectCreationWindow.cs",
                "Forms/ObjectCreationWindow.Generated.cs");

            SaveEmbeddedFile("Models.CustomVariable.cs",
                "Models/CustomVariable.Generated.cs");

            SaveEmbeddedFile("Models.NamedObjectSave.cs",
                "Models/NamedObjectSave.Generated.cs");

            SaveEmbeddedFile("Models.StateSave.cs",
                "Models/StateSave.Generated.cs");

            SaveEmbeddedFile("Models.StateSaveCategory.cs",
                "Models/StateSaveCategory.Generated.cs");

            SaveEmbeddedFile("Models.CustomVariable.cs",
                "Models/CustomVariable.Generated.cs");

            SaveEmbeddedFile("Models.GlueElement.cs",
                "Models/GlueElement.Generated.cs");

            SaveEmbeddedFile("Runtime.DynamicEntity.cs",
                "Runtime/DynamicEntitys.Generated.cs");

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
