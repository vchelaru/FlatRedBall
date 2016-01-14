using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace OfficialPlugins.FrbdkUpdater
{

    [Export(typeof(IMenuStripPlugin))]
    public partial class FrbdkUpdaterPlugin : IMenuStripPlugin
    {
        ToolStripMenuItem _menuItem;
        MenuStrip _menuStrip;
        private const string FrbdkSyncMenuItem = "Update FRBDK";
        public const string PluginsMenuItem = "Update";
        FrbdkUpdaterPluginForm _form;

        public FrbdkUpdaterPlugin()
        {
            _form = new FrbdkUpdaterPluginForm(this);
        }

        public void InitializeMenu(MenuStrip menuStrip)
        {
            _menuStrip = menuStrip;

            _menuItem = new ToolStripMenuItem(FrbdkSyncMenuItem);
            var itemToAddTo = GetItem(PluginsMenuItem);

            itemToAddTo.DropDownItems.Add(_menuItem);
            _menuItem.Click += MenuItemClick;
        }

        void MenuItemClick(object sender, EventArgs e)
        {
            if(_form.Disposing || _form.IsDisposed)
                _form = new FrbdkUpdaterPluginForm(this);

            GlueCommands.DialogCommands.SetFormOwner(_form);
            _form.Show();
        }

        ToolStripMenuItem GetItem(string name)
        {
            return _menuStrip.Items.Cast<ToolStripMenuItem>().FirstOrDefault(item => item.Text == name);
        }
    }
}
