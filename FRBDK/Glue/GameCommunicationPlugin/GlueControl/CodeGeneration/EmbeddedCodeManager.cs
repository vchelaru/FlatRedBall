using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration
{
    public static class EmbeddedCodeManager
    {
        static string glueControlFolder => GlueState.Self.CurrentGlueProjectDirectory + "GlueControl/";

        static List<string> filesToSave = new List<string>
        {
            "CommandReceiver.cs",
            
            "Dtos.cs",
            "GlueCallsClassGenerationManager.cs",
            
            "Editing.CameraLogic.cs",
            "Editing.CopyPasteManager.cs",
            "Editing.EditingManager.cs",
            "Editing.EditorVisuals.cs",
            "Editing.Guides.cs",
            
            "Editing.Managers.GenerateCodeCommands.cs",
            "Editing.Managers.GlueCommands.cs",
            "Editing.Managers.GlueCommandsStateBase.cs",
            
            // Vic says - Scott commented these out, but I'm not sure why....
            // I know we were going to move to a new generated solution, but I don
            // see this active in current projects.
            // actually it looks like they are generated...
            // See Editing_Managers_GlueState
            //SaveEmbeddedFile("Editing.Managers.GlueState.cs");
            //SaveEmbeddedFile("Editing.Managers.GluxCommands.cs");
            "Editing.Managers.ObjectFinder.cs",
            "Editing.Managers.RefreshCommands.cs",
            
            
            "Editing.Markers.MeasurementMarker.cs",
            "Editing.Markers.PolygonPointHandles.cs",
            "Editing.Markers.SelectionMarker.cs",
            "Editing.Markers.TileShapeCollectionMarker.cs",
            
            "Editing.MoveObjectToContainerLogic.cs",
            
            "Editing.SelectionLogic.cs",
            
            "Editing.VariableAssignmentLogic.cs",
            
            "Editing.Visuals.Arrow.cs",
            
            // We don't use this anymore...
            //SaveEmbeddedFile("Forms.ObjectCreationWindow.cs");
            
            "GlueControlManager.cs",
            
            "InstanceLogic.cs",
            
            
            "Models.CustomVariable.cs",
            "Models.GlueElement.cs",
            "Models.GlueElementFileReference.cs",
            "Models.GlueProjectSave.cs",
            "Models.GlueProjectSaveExtensions.cs",
            "Models.IElementExtensionMethods.cs",
            "Models.INamedObjectContainer.cs",
            "Models.NamedObjectSave.cs",
            "Models.NamedObjectSaveExtensionMethods.cs",
            "Models.StateSave.cs",
            "Models.ReferencedFileSave.cs",
            "Models.StateSaveCategory.cs",
            
            "Runtime.DynamicEntity.cs",
            
            "Screens.EntityViewingScreen.cs"
        };

        public static void EmbedAll(bool fullyGenerate)
        {
            GlueControlCodeGenerator.GenerateFull = fullyGenerate;

            foreach(var file in filesToSave)
            {
                SaveEmbeddedFile(file);
            }

            // This was a typo in old projects:
            RemoveEmbeddedFile("Runtime/DynamicEntitys.Generated.cs", saveAfterRemoving:true);
        }

        public static void RemoveAll()
        {
            int count = filesToSave.Count;
            for (int i = 0; i < filesToSave.Count; i++)
            {
                string file = filesToSave[i];

                var withoutCs = file.Substring(0, file.Length - 3);
                var withSlashes = withoutCs.Replace(".", "/");

                var withGenerated = withSlashes + ".Generated.cs";


                var shouldSave = i == count - 1;
                RemoveEmbeddedFile(withGenerated, shouldSave);
            }
        }




        private static void RemoveEmbeddedFile(string relativePath, bool saveAfterRemoving)
        {
            FilePath absoluteFile = GlueState.Self.CurrentGlueProjectDirectory + "GlueControl/" + relativePath;

            GlueCommands.Self.ProjectCommands.RemoveFromProjects(absoluteFile, saveAfterRemoving);
        }

        private static void SaveEmbeddedFile(string resourcePath)
        {
            var split = resourcePath.Split(".").ToArray();
            split = split.Take(split.Length - 1).ToArray(); // take off the .cs
            var combined = string.Join('/', split) + ".Generated.cs";
            var relativeDestinationFilePath = combined;

            var prefix = "GameCommunicationPlugin.GlueControl.Embedded.";
            string glueControlManagerCode = GlueControlCodeGenerator.GetEmbeddedStringContents(prefix + resourcePath);
            FilePath destinationFilePath = glueControlFolder + relativeDestinationFilePath;
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(destinationFilePath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(destinationFilePath.FullPath, glueControlManagerCode));
        }
    }
}
