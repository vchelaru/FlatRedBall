using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.FormHelpers;
using System.Windows.Forms;
using TileGraphicsPlugin.Controllers;

namespace TileGraphicsPlugin.Managers
{
    class RightClickManager : FlatRedBall.Glue.Managers.Singleton<RightClickManager>
    {

        internal void HandleTreeViewRightClick(System.Windows.Forms.TreeNode rightClickedTreeNode, System.Windows.Forms.ContextMenuStrip menuToModify)
        {
            bool shouldShowMenu = rightClickedTreeNode.IsScreenNode();
            
            if(!shouldShowMenu)
            {
                bool isPartOfScreen = rightClickedTreeNode.GetContainingElementTreeNode().IsScreenNode();

                if(isPartOfScreen)
                {
                    shouldShowMenu = rightClickedTreeNode.IsFilesContainerNode() ||
                        rightClickedTreeNode.IsFolderInFilesContainerNode();
                }
            }
                



            if (shouldShowMenu)
            {
                var menuToAdd = new ToolStripMenuItem("Add Tiled Level");

                menuToModify.Items.Add(menuToAdd);

                menuToAdd.Click += HandleAddTiledLevelClick;

            }
        }

        void HandleAddTiledLevelClick(object sender, EventArgs e)
        {
            AddLevelController.Self.ShowAddLevelUi();
        }
    }
}
