using GlueView2.Plugin;
using RemotingHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.EmbeddedPlugins.SelectionFromGlue
{
    [Export(typeof(PluginBase))]
    public class MainSelectionFromGluePlugin : PluginBase
    {
        public override string FriendlyName { get { return "Selection from Glue"; } }

        public override Version Version { get { return new Version(1, 0, 0); } }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            RemotingServer.SetupPort(8687);
            RemotingServer.SetupInterface<SelectionInterface2>();
        }


    }
}
