using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueView.Plugin
{
    /// <summary>
    /// Represents a plugin that is using the plugin system for cleanliness, but it should
    /// never be turned off or appear in the plugin list.
    /// </summary>
    public abstract class EmbeddedPlugin : GlueViewPlugin
    {
        public override string FriendlyName
        {
            get { return "InternalPlugin"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            // internal plugins can't be shut down
            return false;
        }
    }
}
