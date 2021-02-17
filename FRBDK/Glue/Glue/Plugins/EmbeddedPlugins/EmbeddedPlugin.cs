using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins
{
    public enum DesiredOrder
    {
        Critical,
        Early,
        Normal,
        Late,
        Last
    }

    public abstract class EmbeddedPlugin : PluginBase
    {
        public DesiredOrder DesiredOrder { get; set; } = DesiredOrder.Normal;
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
