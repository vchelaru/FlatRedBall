using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.Managers
{
    public enum GumEventTypes
    {
        ElementAdded,
        ElementDeleted,
        ElementRenamed,
        StateRenamed,
        StateCategoryRenamed,
        InstanceAdded,
        InstanceDeleted,
        InstanceRenamed,
    }

    public class ExportedEvent
    {
        public string NewName { get; set; }

        public string OldName { get; set; }

        public GumEventTypes EventType { get; set; }
    }

    public class EventExportManager : Singleton<EventExportManager>
    {
        public void HandleEventExportFile(string fileName)
        {
            string contents = null;
            try
            {
                contents = System.IO.File.ReadAllText(fileName);
            }
            catch
            {
                // do nothing, could have gotten deleted last minute...
            }

            if(!string.IsNullOrEmpty(contents))
            {
                var deserialized = 
                    JsonConvert.DeserializeObject<ExportedEvent>(contents);

                ReactToExportedEvent(deserialized);
            }
        }

        private void ReactToExportedEvent(ExportedEvent deserialized)
        {
            switch(deserialized.EventType)
            {
                case GumEventTypes.ElementDeleted:
                    HandleElementDeleted(deserialized.OldName);
                    break;
            }
        }

        private void HandleElementDeleted(string oldName)
        {
            var indexOfSlash = oldName.IndexOf("/");
            var elementType = oldName.Substring(0, indexOfSlash);

            var strippedName = oldName.Substring(indexOfSlash + 1);

            switch(elementType)
            {
                case "Screens":
                case "Components":
                    // screen was deleted, so remove the runtime:
                    var customFileToRemove = 
                        $"{GlueState.Self.CurrentGlueProjectDirectory}GumRuntimes/{strippedName}Runtime.cs";
                    var generatedFile = 
                        $"{GlueState.Self.CurrentGlueProjectDirectory}GumRuntimes/{strippedName}Runtime.Generated.cs";

                    TaskManager.Self.Add(() =>
                    {
                        GlueCommands.Self.ProjectCommands.RemoveFromProjects(customFileToRemove, false);
                        GlueCommands.Self.ProjectCommands.RemoveFromProjects(generatedFile, true);

                    },
                    $"Removing Gum object {strippedName}");

                    break;
            }
        }
    }
}
