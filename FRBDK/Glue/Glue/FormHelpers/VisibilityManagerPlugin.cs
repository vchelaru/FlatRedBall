using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;

namespace FlatRedBall.Glue.FormHelpers
{
    [Export(typeof(Plugins.PluginBase))]
    public class VisibilityManagerPlugin : EmbeddedPlugin
    {

        public override void StartUp()
        {
            this.ReactToLoadedGlux += HandleLoadedGlux;
        }

        private void HandleLoadedGlux()
        {
            VisibilityManager.ReactivelySetItemViewVisibility();
        }
    }
}
