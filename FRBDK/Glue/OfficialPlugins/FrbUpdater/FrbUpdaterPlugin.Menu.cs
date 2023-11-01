using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.Interfaces;
using Glue;
using FlatRedBall.Glue.FormHelpers;
using System.Threading.Tasks;

namespace OfficialPlugins.FrbUpdater
{
    [Export(typeof(IMenuStripPlugin))]
    public partial class FrbUpdaterPlugin : IMenuStripPlugin
    {
        ToolStripMenuItem mMenuItem;
        MenuStrip mMenuStrip;
        private static readonly string UpdateLibrariesText = Localization.Texts.UpdateLibraries;
        public const string PluginsMenuItem = Localization.MenuIds.UpdateId;
        FrbUpdaterPluginForm mForm;

#pragma warning disable CS0067 // needed for interface
        public event Action<IPlugin, string, string> ReactToPluginEventAction;
#pragma warning restore CS0067 // The event 'FrbUpdaterPlugin.ReactToPluginEventAction' is never used

        public event Action<IPlugin, string, string> ReactToPluginEventWithReturnAction;

        public FrbUpdaterPlugin()
        {
            mForm = new FrbUpdaterPluginForm(this);
        }

        public void InitializeMenu(MenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;

            mMenuItem = new ToolStripMenuItem(UpdateLibrariesText);
            var itemToAddTo = ToolStripHelper.Self.GetItem(mMenuStrip, PluginsMenuItem);

            itemToAddTo.DropDownItems.Add(mMenuItem);
            mMenuItem.Click += MenuItemClick;
        }

        void MenuItemClick(object sender, EventArgs e)
        {
            if(mForm.Disposing || mForm.IsDisposed)
                mForm = new FrbUpdaterPluginForm(this);

            GlueCommands.DialogCommands.SetFormOwner(mForm);
            mForm.Show();
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
    }
}
