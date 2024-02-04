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

#pragma warning disable CS0067 // Needed for interface
        public event Action<IPlugin, string, string> ReactToPluginEventAction;
#pragma warning restore CS0067 // The event 'GlobalContentManagerHelperPlugin.ReactToPluginEventAction' is never used


#pragma warning disable CS0067 // Needed for interface
        public event Action<IPlugin, string, string> ReactToPluginEventWithReturnAction;
#pragma warning restore CS0067 // The event 'GlobalContentManagerHelperPlugin.ReactToPluginEventWithReturnAction' is never used

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
            ToolStripMenuItem itemToAddTo = GetItem(Localization.MenuIds.ContentId);

            itemToAddTo.DropDownItems.Remove(mMenuItem);

            return true;
        }

        #endregion

        #region IMenuStripPlugin Members

        public void InitializeMenu(MenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;

            mMenuItem = new ToolStripMenuItem(Localization.Texts.GlobalContentMembership);
            ToolStripMenuItem itemToAddTo = GetItem(Localization.MenuIds.ContentId);

            itemToAddTo.DropDownItems.Add(mMenuItem);
            mMenuItem.Click += mMenuItem_Click;
        }

        void mMenuItem_Click(object sender, EventArgs e)
        {
            PluginForm pluginForm = new PluginForm();
            pluginForm.GlueCommands = GlueCommands;
            pluginForm.RefreshElements();

            pluginForm.ShowDialog(GlueCommands.DialogCommands.Win32Window);

        }

        ToolStripMenuItem GetItem(string name)
        {
            foreach (ToolStripMenuItem item in mMenuStrip.Items)
            {
                if (item.Name == name)
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
