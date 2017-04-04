using GlueView.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using GlueView.Facades;
using GlueView.Forms;

namespace GlueView.EmbeddedPlugins.LocalizationPlugin
{
    [Export(typeof(GlueViewPlugin))]
    public class MainPlugin : GlueViewPlugin
    {
        public override string FriendlyName
        {
            get
            {
                return "Localization Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            GlueViewCommands.Self.CollapsibleFormCommands.AddCollapsableForm(
                "Localization", -1, new LocalizationControl(), this);

        }
    }
}
