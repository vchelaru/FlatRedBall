using GlueView2.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.EmbeddedPlugins.ProjectTreeView
{
    [Export(typeof(PluginBase))]
    public class MainProjectTreeViewPlugin : PluginBase
    {
        public override string FriendlyName { get { return "Tree View Plugin"; } }

        public override Version Version { get { return new Version(1, 0, 0); } }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            //var tab = this.AddUi(new Views.MainWindow());
            //tab.Title = "Project Explorer";
        }
    }
}
