using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticyDraftPlugin
{
    [Export(typeof(PluginBase))]
    public class MainArticyDraftPlugin : PluginBase
    {
        public override string FriendlyName => "Articy:Draft Plugin";

        public override Version Version =>
            new Version(1, 0, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {

        }
    }
}
