using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.RuntimeDebuggingPlugin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.RuntimeDebuggingPlugin
{
    [Export(typeof(PluginBase))]
    public class MainRuntimeDebuggingPlugin : PluginBase
    {
        public override string FriendlyName => "Runtime Debugging Plugin";

        public override Version Version => new Version(1,0);

        MainControl mainControl;

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            this.AddMenuItemTo("Runtime Debugging", HandleMenuItemClicked, "Plugins");
        }

        private void HandleMenuItemClicked(object sender, EventArgs e)
        {
            CreateOrShowTab();
        }

        private void CreateOrShowTab()
        {
            if (mainControl == null)
            {
                mainControl = new Controls.MainControl();
                this.AddToTab(PluginManager.CenterTab, mainControl, "Runtime Debugging");
            }

                this.AddTab();
        }
    }
}
