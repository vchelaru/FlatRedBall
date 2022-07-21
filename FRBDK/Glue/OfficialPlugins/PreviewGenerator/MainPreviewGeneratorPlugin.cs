using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using OfficialPlugins.PreviewGenerator.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace OfficialPlugins.PreviewGenerator
{
    [Export(typeof(PluginBase))]
    internal class MainPreviewGeneratorPlugin : PluginBase
    {
        public override string FriendlyName => "Preview Generator";

        public override Version Version => new Version(1, 0);

        PluginTab previewPreviewTab;

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            var view = new MainPreviewGeneratorControl();
            previewPreviewTab = this.CreateTab(view, "Preview Preview");

            this.ReactToItemSelectHandler = HandleReactToItemSelectHandler;
        }

        private void HandleReactToItemSelectHandler(ITreeNode selectedTreeNode)
        {
            var shouldShow = GlueState.Self.CurrentEntitySave != null;

            if(shouldShow)
            {
                previewPreviewTab.Show();
            }
            else
            {
                previewPreviewTab.Hide();
            }
        }
    }
}
