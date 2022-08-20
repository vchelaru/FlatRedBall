using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CleanupPlugin
{
    class EventRemovalLogic
    {
        internal void HandleEventRemoved(IElement element, EventResponseSave eventResponse)
        {
            bool hasAnyEvents = element.Events.Count > 0;

            if (!hasAnyEvents)
            {
                RemoveEventGeneratedCodefile(element);
            }
        }

        private void RemoveEventGeneratedCodefile(IElement element)
        {
            string fileName = element.Name + ".Generated.Event.cs";
            FilePath fullFileName = ProjectManager.ProjectBase.Directory + fileName;

            GlueCommands.Self.ProjectCommands.RemoveFromProjects(fullFileName);
            
        }
    }
}
