using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins;
using Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Threading.Tasks;

namespace PluginTestbed.GlobalContentManagerPlugins
{
    [Export(typeof(IMenuStripPlugin))]
    public class GlobalContentManagerHelperPlugin : IMenuStripPlugin
    {
        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        ToolStripMenuItem mMenuItem;
        MenuStrip mMenuStrip;

        public event Action<IPlugin, string, string> ReactToPluginEventAction;
        public event Action<IPlugin, string, string> ReactToPluginEventWithReturnAction;

        #region IPlugin Members

        public string FriendlyName
        {
            get { return "Global ContentManager Helper Plugin"; }
        }

        public Version Version
        {
            get { return new Version(1,0); }
        }

        public string GithubRepoOwner => null;
        public string GithubRepoName => null;
        public bool CheckGithubForNewRelease => false;

        public void StartUp()
        {
            
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            ToolStripMenuItem itemToAddTo = GetItem("Content");

            itemToAddTo.DropDownItems.Remove(mMenuItem);

            return true;
        }

        #endregion

        #region IMenuStripPlugin Members

        public void InitializeMenu(MenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;

            mMenuItem = new ToolStripMenuItem("GlobalContent Membership");
            ToolStripMenuItem itemToAddTo = GetItem("Content");

            itemToAddTo.DropDownItems.Add(mMenuItem);
            mMenuItem.Click += new EventHandler(mMenuItem_Click);
        }

        void mMenuItem_Click(object sender, EventArgs e)
        {
            PluginForm pluginForm = new PluginForm();
            pluginForm.GlueCommands = GlueCommands;
            pluginForm.RefreshElements();

            pluginForm.ShowDialog(MainGlueWindow.Self);

        }

        ToolStripMenuItem GetItem(string name)
        {
            foreach (ToolStripMenuItem item in mMenuStrip.Items)
            {
                if (item.Text == name)
                {
                    return item;
                }
            }

            return null;
        }

        public void HandleEvent(string eventName, string payload)
        {
        }

        public Task<string> HandleEventWithReturn(string eventName, string payload)
        {
            return Task.FromResult((string)null);
        }

        public void HandleEventResponseWithReturn(string payload)
        {
        }

        #endregion
    }
}
