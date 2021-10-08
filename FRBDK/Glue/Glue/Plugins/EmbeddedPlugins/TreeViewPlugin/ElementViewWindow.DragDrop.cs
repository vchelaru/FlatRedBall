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

                    RightClickHelper.PopulateRightClickItems(targetNode, MenuShowingAction.RightButtonDrag);
                    MainGlueWindow.Self.mElementContextMenu.Show(new Point(e.X, e.Y));
                    //ElementViewWindow.mTreeView.ContextMenu.Show(ElementViewWindow.mTreeView, pt);
                }
                else
                {


                    DragDropTreeNode((TreeView)sender, targetNode);
                }
            }

        }

        internal static void DragDropTreeNode(TreeView treeView, TreeNode targetNode)
        {
#if !DEBUG
            try
#endif
            {
                #region Get the nodeMoving and targetNode
                TreeNode nodeMoving = TreeNodeDraggedOff;

                #endregion

                bool shouldSaveGlux = false;

                if (nodeMoving == targetNode || nodeMoving == null)
                {
                    // do nothing
                }
                else if (nodeMoving.IsEntityNode())
                {
                    DragDropManager.Self.MoveEntityOn(nodeMoving as EntityTreeNode, targetNode);
                    shouldSaveGlux = true;

                }
                else if (nodeMoving.IsReferencedFile())
                {
                    DragDropManager.Self.MoveReferencedFile(nodeMoving, targetNode);
                    shouldSaveGlux = true;
                }
                else if (nodeMoving.IsNamedObjectNode())
                {
                    DragDropManager.Self.MoveNamedObject(nodeMoving, targetNode);
                    shouldSaveGlux = true;
                }
                else if (nodeMoving.IsStateNode())
                {
                    DragDropManager.Self.MoveState(nodeMoving, targetNode);
                    shouldSaveGlux = true;
                }
                else if (nodeMoving.IsStateCategoryNode())
                {
                    DragDropManager.Self.MoveStateCategory(nodeMoving, targetNode);
                    shouldSaveGlux = true;
                }
                else if (nodeMoving.IsCustomVariable())
                {
                    MoveCustomVariable(nodeMoving, targetNode);
                    shouldSaveGlux = true;
                }
                if (shouldSaveGlux)
                {
                    GluxCommands.Self.SaveGlux();
                }

            }
#if !DEBUG
            catch (Exception exception)
            {
                System.Windows.Forms.MessageBox.Show("Error moving object: " + exception.ToString());
            }
#endif
        }

        private static void MoveCustomVariable(TreeNode nodeMoving, TreeNode targetNode)
        {
            CustomVariable customVariable = nodeMoving.Tag as CustomVariable;

            if (targetNode.IsRootEventsNode())
            {
                // The user dragged a variable onto the events node, so they want to make
                // an event for this.  We'll assume an "after" event since I think no one makes
                // before events
             

                if (customVariable != null)
                {
                    customVariable.CreatesEvent = true;

                    FlatRedBall.Glue.Events.EventResponseSave eventResponseSave = new Events.EventResponseSave();
                    eventResponseSave.EventName = "After" + customVariable.Name + "Set";

                    eventResponseSave.SourceObject = null;
                    eventResponseSave.SourceObjectEvent = null;

                    eventResponseSave.SourceVariable = customVariable.Name;
                    eventResponseSave.BeforeOrAfter = BeforeOrAfter.After;

                    eventResponseSave.DelegateType = null;

                    RightClickHelper.AddEventToElementAndSave(GlueState.Self.CurrentElement, eventResponseSave);

                }
            }
            else if (targetNode.IsRootCustomVariablesNode())
            {
                // let's see if the user is moving a variable from one element to another
                var sourceElement = nodeMoving.GetContainingElementTreeNode().Tag as GlueElement;
                var targetElement = targetNode.GetContainingElementTreeNode().Tag as GlueElement;

                if (sourceElement != targetElement)
                {
                    // copying a variable from one element to another
                    // eventually we need to add some error checking here.
                    CustomVariable newVariable = customVariable.Clone();

                    targetElement.CustomVariables.Add(newVariable);


                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(targetElement);
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(targetElement);
                }
            }
        }

        private static void DragDropFile(object sender, DragEventArgs e)
        {
            TreeNode targetNode = null;

            TreeView tree = (TreeView)sender;
            TreeNode directoryNode = null;


            Point pt = new Point(e.X, e.Y);
            pt = tree.PointToClient(pt);
            targetNode = tree.GetNodeAt(pt);

            TreeNode nodeDroppedOn = targetNode;

            while (targetNode != null && !targetNode.IsEntityNode() && !targetNode.IsScreenNode())
            {
                if (directoryNode == null && targetNode.IsDirectoryNode())
                    directoryNode = targetNode;

                targetNode = targetNode.Parent;
            }

            var directoryPath = directoryNode == null ? null : directoryNode.GetRelativePath();

            bool userCancelled = false;

            if (targetNode == null)
            {
                ElementViewWindow.SelectedNode = nodeDroppedOn;

                var droppedFiles = (string[])e.Data.GetData("FileDrop");

                foreach (var fileName in droppedFiles)
                {
                    string extension = FileManager.GetExtension(fileName);

                    if (extension == "entz" || extension == "scrz")
                    {
                        ElementImporter.ImportElementFromFile(fileName, true);
                    }
                    else if(extension == "plug")
                    {
                        Plugins.PluginManager.InstallPlugin(InstallationType.ForUser, fileName);
                    }
                    else
                    {
                        AddExistingFileManager.Self.AddSingleFile(fileName, ref userCancelled);
                    }
                }

                GluxCommands.Self.SaveGlux();
            }
            else if (targetNode is ScreenTreeNode || targetNode is EntityTreeNode)
            {
                bool any = false;

                var filesToAdd = ((string[])e.Data.GetData("FileDrop"))
                    .Select(item => new FilePath(item));

                // gather all dependencies
                // reverse loop and we'll add at the end
                foreach(var file in filesToAdd)
                {

                }


                foreach (var fileName in filesToAdd)
                {
                    // First select the entity
                    ElementViewWindow.SelectedNode = targetNode;


                    var element = GlueState.Self.CurrentElement;
                    if(string.IsNullOrEmpty(directoryPath))
                    {
                        directoryPath = GlueState.Self.CurrentTreeNode.GetRelativePath();
                    }
            

                    FlatRedBall.Glue.Managers.TaskManager.Self.Add(() =>
                        {
                            var newRfs = AddExistingFileManager.Self.AddSingleFile(fileName.FullPath, ref userCancelled, element, directoryPath);

                            GlueCommands.Self.DoOnUiThread(() => GlueCommands.Self.SelectCommands.Select(newRfs));
                        },
                        "Add file " + fileName);
                    any = true;
                }
                if (any)
                {
                    GluxCommands.Self.SaveGlux();
                }
            }
        }





    }
}
