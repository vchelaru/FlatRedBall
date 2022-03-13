using FlatRedBall.Glue.Plugins;
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

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            var view = new MainPreviewGeneratorControl();
            this.CreateAndAddTab(view, "Preview Preview");
        }
    }
}
