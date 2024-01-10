using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins;
using OfficialPlugins.BuiltFileSizeInspector.ViewModels;
using OfficialPlugins.BuiltFileSizeInspector.Views;

namespace OfficialPlugins.BuiltFileSizeInspector
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        SizeInspectorControl mainControl;
        PluginTab tab;

        public override string FriendlyName
        {
            get { return "Built File Size Inspector"; }
        }

        public override Version Version
        {
            get { return new Version(1,0); }
        }

        public override void StartUp()
        {
            base.AddMenuItemTo(Localization.Texts.ViewBuiltProjectSizes, Localization.MenuIds.ViewBuiltProjectSizesId, HandleViewBuiltProjectSizes, Localization.MenuIds.PluginId);
        }

        private void HandleViewBuiltProjectSizes(object sender, EventArgs e)
        {
            if(mainControl == null)
            {
                mainControl = new SizeInspectorControl();
                mainControl.DataContext = new BuiltFileSizeViewModel();

                tab = this.CreateTab(mainControl, "Built File Size");
            }
            tab.Show();
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
