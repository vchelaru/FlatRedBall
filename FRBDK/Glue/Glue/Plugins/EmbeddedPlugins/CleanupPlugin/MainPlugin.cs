using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CleanupPlugin
{
    [Export(typeof(PluginBase))]
    class MainPlugin : EmbeddedPlugin
    {
        EventRemovalLogic eventRemovalLogic;
        FileRemovalLogic fileRemovalLogic;

        public override void StartUp()
        {
            eventRemovalLogic = new EventRemovalLogic();
            fileRemovalLogic = new FileRemovalLogic();

            this.ReactToFileRemoved += HandleFileRemoved;
            this.ReactToEventRemoved += HandleEventRemoved;
        }

        private void HandleEventRemoved(GlueElement element, EventResponseSave eventResponse)
        {
            eventRemovalLogic.HandleEventRemoved(element, eventResponse);
        }

        private void HandleFileRemoved(GlueElement element, ReferencedFileSave file)
        {
            fileRemovalLogic.HandleFileRemoved(element, file);
        }
    }
}
