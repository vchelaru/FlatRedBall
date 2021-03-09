using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.UnreferencedFiles;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.UnreferencedFiles
{
    [Export(typeof(PluginBase))]
    public class UnreferencedFilesPlugin : EmbeddedPlugin
    {
        UnreferencedFilesView view;
        PluginTab tab;

        public override void StartUp()
        {
            AddMenuItemTo("View Unreferenced Files", HandleScanForUnreferencedFiles, "Content");
        }

        private void HandleScanForUnreferencedFiles(object sender, EventArgs args)
        {
            if (view == null)
            {
                view = new UnreferencedFilesView();

                UnreferencedFilesViewModel viewModel = new UnreferencedFilesViewModel();

                view.DataContext = viewModel;

                tab = CreateTab(view, "Unreferenced Files");
                tab.SuggestedLocation = TabLocation.Left;

                // It refreshes itself when the radio button is set when the view is created,
                // so we don't need to:
                //viewModel.Refresh();

            }
            tab.Show();
            tab.Focus();
        }
    }
}
