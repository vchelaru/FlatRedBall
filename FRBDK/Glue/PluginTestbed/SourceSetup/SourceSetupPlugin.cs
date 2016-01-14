using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace PluginTestbed.SourceSetup
{
    public partial class SourceSetupPlugin
    {
        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        public string FriendlyName
        {
            get { return "Source Setup Plugin"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        public void StartUp()
        {
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            ToolStripMenuItem itemToAddTo = GetItem(PluginsMenuItem);

            itemToAddTo.DropDownItems.Remove(_menuItem);

            return true;
        }
    }
}
