using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Rss;
using System;
using System.ComponentModel.Composition;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins
{
    [Export(typeof(PluginBase))]
    class MainPlugin : EmbeddedPlugin
    {
        PluginTab tab;
        MainControl mainControl;

        public override void StartUp()
        {
            this.AddMenuItemTo(L.Texts.ManagePlugins, L.MenuIds.ManagePluginsId, HandleManagePlugins, L.MenuIds.PluginId);
        }

        private void HandleManagePlugins(object sender, EventArgs e)
        {
            if(mainControl == null)
            {
                mainControl = new MainControl();

                tab = CreateAndAddTab(mainControl, L.Texts.Plugins, TabLocation.Left);
            }
            else
            {
                mainControl.RefreshCheckboxes();
            }
            tab.Show();
            tab.Focus();
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
