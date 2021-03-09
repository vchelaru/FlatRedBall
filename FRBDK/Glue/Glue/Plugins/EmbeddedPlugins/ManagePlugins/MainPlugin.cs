using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Rss;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins
{
    [Export(typeof(PluginBase))]
    class MainPlugin : EmbeddedPlugin
    {
        PluginTab tab;
        MainControl mainControl;

        public override void StartUp()
        {
            this.AddMenuItemTo("Manage Plugins", HandleManagePlugins, "Plugins");
        }

        private void HandleManagePlugins(object sender, EventArgs e)
        {
            if(mainControl == null)
            {
                mainControl = new MainControl();

                tab = CreateAndAddTab(mainControl, "Plugins", TabLocation.Left);
            }
            else
            {
                mainControl.RefreshCheckboxes();
            }
            tab.Show();
        }

        private void HandleFinishedDownloading(AllFeed allFeed, DownloadState state)
        {
            if (mainControl != null)
            {
                GlueCommands.Self.DoOnUiThread(() =>
               {
                   mainControl.AllFeed = allFeed;
                   mainControl.DownloadState = state;
               });
            }
        }
    }
}
