using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace OfficialPlugins.FrbdkUpdater
{
    public partial class FrbdkUpdaterPlugin
    {
        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        public string FriendlyName
        {
            get { return "FRBDK Sync Plugin"; }
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

            if(!_form.Disposing && !_form.IsDisposed)
                _form.Hide();

            return true;
        }
    }
}
