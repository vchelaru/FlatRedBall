using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.FormHelpers;

namespace OfficialPlugins.FrbUpdater
{
    public partial class FrbUpdaterPlugin
    {
        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        [Import("GlueState")]
        public IGlueState GlueState { get; set; }

        public string FriendlyName
        {
            get { return "FRB Sync Plugin"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        public string GithubRepoOwner => null;
        public string GithubRepoName => null;
        public bool CheckGithubForNewRelease => false;

        public void StartUp()
        {
            var settings = FrbUpdaterSettings.LoadSettings();

            if (settings.AutoUpdate && (settings.SelectedSource == "Daily Build" || settings.SelectedSource == "Current"))
            {
                try
                {
                    var window = new UpdateWindow(this);
                    GlueCommands.DialogCommands.SetFormOwner(window);
                    if (window.Owner == null)
                        window.TopMost = true;
                    window.Show();
                }
                catch (Exception e)
                {
                    GlueCommands.PrintError("Error starting updater plugin:\n" + e.ToString());
                }
            }
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            ToolStripMenuItem itemToAddTo = null;
            
            itemToAddTo = ToolStripHelper.Self.GetItem(mMenuStrip, PluginsMenuItem);

            itemToAddTo?.DropDownItems.Remove(mMenuItem);

            if (!mForm.Disposing && !mForm.IsDisposed)
                mForm.Hide();

            return true;
        }
    }
}
