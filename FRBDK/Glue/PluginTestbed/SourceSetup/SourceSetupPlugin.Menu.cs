using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.VSHelpers;

namespace PluginTestbed.SourceSetup
{
    [Export(typeof(IMenuStripPlugin))]
	public partial class SourceSetupPlugin : IMenuStripPlugin
	{
        ToolStripMenuItem _menuItem;
        MenuStrip _menuStrip;
        private const string SetupSourceMenuItem = "Set up Source";
        public const string PluginsMenuItem = "Plugins";
        SourceSetupPluginForm _form;

        public SourceSetupPlugin()
        {
            _form = new SourceSetupPluginForm(this);
        }

	    public void InitializeMenu(MenuStrip menuStrip)
	    {
            _menuStrip = menuStrip;

            _menuItem = new ToolStripMenuItem(SetupSourceMenuItem);
            var itemToAddTo = GetItem(PluginsMenuItem);

            itemToAddTo.DropDownItems.Add(_menuItem);
            _menuItem.Click += MenuItemClick;
	    }

        void MenuItemClick(object sender, EventArgs e)
        {
            if (_form.Disposing || _form.IsDisposed)
                _form = new SourceSetupPluginForm(this);

            GlueCommands.DialogCommands.SetFormOwner(_form);
            _form.Show();
        }

        ToolStripMenuItem GetItem(string name)
        {
            return _menuStrip.Items.Cast<ToolStripMenuItem>().FirstOrDefault(item => item.Text == name);
        }
	}
}
