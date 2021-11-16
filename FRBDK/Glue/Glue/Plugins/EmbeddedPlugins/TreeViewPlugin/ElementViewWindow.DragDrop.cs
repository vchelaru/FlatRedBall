using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using FlatRedBall.Glue.Controls;
using FlatRedBall.IO;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.StandardTypes;
using Glue;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Elements;
using EditorObjects.Parsing;
using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Managers;
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin;

namespace FlatRedBall.Glue.FormHelpers
{
    public static partial class ElementViewWindow
    {
        internal static MouseButtons ButtonUsed
        {
            get;
            set;
        }

        #region Event Methods

        public static void DragOver(object sender, DragEventArgs e)
        {
            TreeView tree = (TreeView)sender;
            Point pt = new Point(e.X, e.Y);
            pt = tree.PointToClient(pt);


            e.Effect = DragDropEffects.Move;

            TreeNode nodeSource = (TreeNode)e.Data.GetData(typeof(EntityTreeNode));
            if (nodeSource != null)
            {



                TreeNode nodeTarget = tree.GetNodeAt(pt);
                if (nodeTarget != null && nodeTarget != nodeSource)
                {
                    tree.SelectedNode = nodeTarget;
                }
            }

            const int minimumHeight = 50;

            if (tree.Size.Height > minimumHeight)
            {
                if (pt.Y < minimumHeight / 2)
                {
                    // scroll up
                    TreeNode treeNode = tree.GetNodeAt(pt);

                    if (treeNode != null)
                    {
                        TreeNode treeNodeToShow = treeNode.PrevVisibleNode;

                        if (treeNodeToShow != null)
                        {
                            treeNodeToShow.EnsureVisible();
                        }
                    }

                }
                else if (pt.Y > tree.Size.Height - minimumHeight / 2)
                {
                    //scroll down
                    TreeNode treeNode = tree.GetNodeAt(pt);

                    if (treeNode != null)
                    {
                        TreeNode treeNodeToShow = treeNode.NextNode;

                        if (treeNodeToShow != null)
                        {
                            treeNodeToShow.EnsureVisible();
                        }
                        else
                        {
                            if (treeNode.NextNodeCrawlingTree() != null)
                            {
                                treeNode.NextNodeCrawlingTree().EnsureVisible();
                            }
                        }
                    }
                }

            }
        }

        #endregion

        internal static void DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop"))
            {
                DragDropFile(sender, e);
            }
            else
            {
                TreeNode targetNode = null;
                TreeView tree = (TreeView)sender;


                Point pt = new Point(e.X, e.Y);
                pt = tree.PointToClient(pt);
                targetNode = tree.GetNodeAt(pt);

                if (ButtonUsed == MouseButtons.Right)
                {
                    MainExplorerPlugin.Self.ElementTreeView.SelectedNode = targetNode;

                    RightClickHelper.PopulateRightClickItems(targetNode, MenuShowingAction.RightButtonDrag);
                    MainGlueWindow.Self.mElementContextMenu.Show(new Point(e.X, e.Y));
                    //ElementViewWindow.mTreeView.ContextMenu.Show(ElementViewWindow.mTreeView, pt);
                }
                else
                {
                    TreeNode nodeMoving = TreeNodeDraggedOff;
                    DragDropManager.DragDropTreeNode(
                        TreeNodeWrapper.CreateOrNull(targetNode), TreeNodeWrapper.CreateOrNull(nodeMoving));
                }
            }

        }




        private static void DragDropFile(object sender, DragEventArgs e)
        {
            TreeNode targetNode = null;

            TreeView tree = (TreeView)sender;


            Point pt = new Point(e.X, e.Y);
            pt = tree.PointToClient(pt);
            targetNode = tree.GetNodeAt(pt);

            FilePath[] droppedFiles = ((string[])e.Data.GetData("FileDrop"))
                .Select(item => new FilePath(item))
                .ToArray();

            DragDropManager.Self.HandleDropExternalFileOnTreeNode(droppedFiles, TreeNodeWrapper.CreateOrNull( targetNode));
        }
    }
}
