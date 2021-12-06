using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using GlueFormsCore.Plugins.EmbeddedPlugins.StartupScreenPlugin.Errors;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.StartupScreenPlugin
{
    [Export(typeof(PluginBase))]
    class MainStartupScreenPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            var errorReporter = new ErrorReporter();
            this.AddErrorReporter(errorReporter);
        }
    }
}
