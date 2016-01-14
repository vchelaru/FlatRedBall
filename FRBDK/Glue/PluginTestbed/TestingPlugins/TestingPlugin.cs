using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace PluginTestbed.TestingPlugins
{
    [Export(typeof(PluginBase))]
    public class TestingPlugin : PluginBase
    {
        MenuStrip mMenuStrip;
        ToolStripMenuItem mMenuItem;

        public override string FriendlyName
        {
            get { return "Testing Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }

        public override void StartUp()
        {
            this.InitializeMenuHandler += HandleInitializeMenuHandler;
        }

        void HandleInitializeMenuHandler(MenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;

            mMenuItem = new ToolStripMenuItem("Run Tests");
            ToolStripMenuItem itemToAddTo = GetItem("Plugins");

            itemToAddTo.DropDownItems.Add(mMenuItem);
            mMenuItem.Click += new EventHandler(HandleMenuClick);
        }

        void HandleMenuClick(object sender, EventArgs args)
        {
            //CsvTests.TestRenaming();
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
        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
