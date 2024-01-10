using System;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.UnreferencedFiles;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.UnreferencedFiles
{
    [Export(typeof(PluginBase))]
    public class UnreferencedFilesPlugin : EmbeddedPlugin
    {
        UnreferencedFilesView view;
        PluginTab tab;

        public override void StartUp()
        {
            AddMenuItemTo(L.Texts.ViewUnreferencedFiles, L.MenuIds.ViewUnreferencedFilesId, HandleScanForUnreferencedFiles, L.MenuIds.ContentId);
        }

        private void HandleScanForUnreferencedFiles(object sender, EventArgs args)
        {
            if (view == null)
            {
                view = new UnreferencedFilesView();

                var viewModel = new UnreferencedFilesViewModel();

                view.DataContext = viewModel;

                tab = CreateTab(view, L.Texts.UnreferencedFiles);

                // It refreshes itself when the radio button is set when the view is created,
                // so we don't need to:
                //viewModel.Refresh();

            }
            tab.Show();
            tab.Focus();
        }
    }
}
