using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins
{
    public abstract class EmbeddedPlugin : PluginBase
    {
        public override string FriendlyName
        {
            get { return "Embedded Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }

        public override bool ShutDown(Interfaces.PluginShutDownReason shutDownReason)
        {
            // this can't be shut down
            return false; 
        }
    }
}
