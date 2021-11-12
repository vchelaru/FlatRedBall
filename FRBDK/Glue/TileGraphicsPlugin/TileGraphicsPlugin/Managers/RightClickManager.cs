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
                var isPartOfScreen = rightClickedTreeNode.GetContainingElementTreeNode()?.IsScreenNode() == true;

                if(isPartOfScreen)
                {
                    shouldShowMenu = rightClickedTreeNode.IsFilesContainerNode() ||
                        rightClickedTreeNode.IsFolderInFilesContainerNode();
                }
            }
                


            // Tiled levels was a nice idea, but it tied the developer's hands
            // too much. 
            //if (shouldShowMenu)
            //{
            //    var menuToAdd = new ToolStripMenuItem("Add Tiled Level");

            //    menuToModify.Items.Add(menuToAdd);

            //    menuToAdd.Click += HandleAddTiledLevelClick;

            //}
        }
    }
}
