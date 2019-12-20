using FlatRedBall.Content.Scene;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TileGraphicsPlugin.Controllers
{
    public class TilesetXnaRightClickController
    {
        #region Fields

        ContextMenuStrip mMenuStrip;

        #endregion

        public void Initialize(ContextMenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;
            RefreshMenuItems();
        }

        public void RefreshMenuItems()
        {
            //ToolStripMenuItem tsmi = new ToolStripMenuItem();
            //tsmi.Text = "Create Entity for this tile";
            //tsmi.Click += EntityCreationController.Self.HandleCreateEntityClick;
            //mMenuStrip.Items.Add(tsmi);
        }

    }
}
