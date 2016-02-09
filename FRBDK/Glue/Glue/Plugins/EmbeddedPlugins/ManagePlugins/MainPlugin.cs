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
        const string mAllFeedUrl = "http://www.gluevault.com/glueplugins";

        MainControl mainControl;

        BrowseGlueVaultViewModel viewModel;


        public override void StartUp()
        {
            this.AddMenuItemTo("Manage Plugins", HandleManagePlugins, "Plugins");
        }

        private void HandleManagePlugins(object sender, EventArgs e)
        {
            if(mainControl == null)
            {
                viewModel = new BrowseGlueVaultViewModel();
                mainControl = new MainControl();
                mainControl.GlueVaultBrowser.DataContext = viewModel;

                this.AddToTab(PluginManager.LeftTab, mainControl, "Plugins");
            }
            else
            {
                this.AddTab();
            }



         

            AllFeed.StartDownloadingInformation(mAllFeedUrl, HandleFinishedDownloading);
        }

        private void HandleFinishedDownloading(AllFeed allFeed, DownloadState state)
        {
            if (mainControl != null)
            {
                GlueCommands.Self.DoOnUiThread(() =>
               {
                   viewModel.UpdateFrom(allFeed);

                   mainControl.AllFeed = allFeed;
                   mainControl.DownloadState = state;
               });
            }
        }
    }
}
