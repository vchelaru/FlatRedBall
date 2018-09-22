using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace OfficialPlugins.GlueView
{
    [Export(typeof(IMenuStripPlugin))]
    public partial class GlueViewPlugin : IMenuStripPlugin
    {
        MenuStrip _menuStrip;
        ToolStripMenuItem _menuItem;

        public void InitializeMenu(MenuStrip menuStrip)
        {
            _menuStrip = menuStrip;

            var menuItem = new ToolStripMenuItem("Glue View");

            menuStrip.Items.Add(menuItem);
            _menuItem = menuItem;

            var connectItem = new ToolStripMenuItem("Connect to Glue View");

            menuItem.DropDownItems.Add(connectItem);
            connectItem.Click += ConnectItemClick;
        }

        void ConnectItemClick(object sender, EventArgs e)
        {
            var glueProjectFileName = GlueState.Self.GlueProjectFileName;

            _selectionInterface.AttemptConnection();
            _selectionInterface.SetGlueProjectFile(glueProjectFileName, true);

            gview2SelectionInterface.AttemptConnection();
            gview2SelectionInterface.SetGlueProjectFile(glueProjectFileName);
        }
        
    }
}
