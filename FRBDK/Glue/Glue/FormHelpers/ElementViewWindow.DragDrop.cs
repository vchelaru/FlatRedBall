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
                    MoveEntityOn(nodeMoving as EntityTreeNode, targetNode);
                    shouldSaveGlux = true;

                }
                else if (nodeMoving.IsReferencedFile())
                {
                    MoveReferencedFile(nodeMoving, targetNode);
                    shouldSaveGlux = true;
                }
                else if (nodeMoving.IsNamedObjectNode())
                {
                    MoveNamedObject(nodeMoving, targetNode);
                    shouldSaveGlux = true;
                }
                else if (nodeMoving.IsStateNode())
                {
                    MoveState(nodeMoving, targetNode);
                    shouldSaveGlux = true;
                }
                else if (nodeMoving.IsStateCategoryNode())
                {
                    MoveStateCategory(nodeMoving, targetNode);
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

        private static void MoveStateCategory(TreeNode nodeMoving, TreeNode targetNode)
        {
            if (targetNode.IsRootCustomVariablesNode() || targetNode.IsCustomVariable())
            {
                // The user drag+dropped a state category into the variables
                // Let's make sure that it's all in the same Element though:
                if (targetNode.GetContainingElementTreeNode() == nodeMoving.GetContainingElementTreeNode())
                {
                    StateSaveCategory category = nodeMoving.Tag as StateSaveCategory;

                    // expose a variable that exposes the category
                    CustomVariable customVariable = new CustomVariable();

                    if (category.SharesVariablesWithOtherCategories)
                    {
                        customVariable.Type = "VariableState";
                        customVariable.Name = "CurrentState";
                    }
                    else
                    {
                        customVariable.Type = category.Name;
                        customVariable.Name = "Current" + category.Name + "State";
                    }

                    IElement element = targetNode.GetContainingElementTreeNode().Tag as IElement;

                    element.CustomVariables.Add(customVariable);
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                    EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
                }
            }
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

                    RightClickHelper.AddEventToElementAndSave(EditorLogic.CurrentElement, eventResponseSave);

                }
            }
            else if (targetNode.IsRootCustomVariablesNode())
            {
                // let's see if the user is moving a variable from one element to another
                IElement sourceElement = nodeMoving.GetContainingElementTreeNode().Tag as IElement;
                IElement targetElement = targetNode.GetContainingElementTreeNode().Tag as IElement;

                if (sourceElement != targetElement)
                {
                    // copying a variable from one element to another
                    // eventually we need to add some error checking here.
                    CustomVariable newVariable = customVariable.Clone();

                    targetElement.CustomVariables.Add(newVariable);


                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(targetElement);
                    GlueCommands.Self.RefreshCommands.RefreshUi(targetElement);
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
                foreach (var fileName in (string[])e.Data.GetData("FileDrop"))
                {
                    string creationReport;

                    string extension = FileManager.GetExtension(fileName);

                    if (extension == "entz" || extension == "scrz")
                    {
                        ElementImporter.ImportElementFromFile(fileName, true);
                    }
                    else
                    {
                        string errorMessage;
                        
                        RightClickHelper.AddSingleFile(fileName, ref userCancelled);
                    }
                }

                GluxCommands.Self.SaveGlux();
            }
            else if (targetNode is ScreenTreeNode || targetNode is EntityTreeNode)
            {
                bool any = false;
                foreach (var fileName in (string[])e.Data.GetData("FileDrop"))
                {
                    // First select the entity
                    ElementViewWindow.SelectedNode = targetNode;


                    var element = GlueState.Self.CurrentElement;
                    string directoryOfTreeNode = EditorLogic.CurrentTreeNode.GetRelativePath();
            

                    FlatRedBall.Glue.Managers.TaskManager.Self.AddSync(() =>
                        {
                            RightClickHelper.AddSingleFile(fileName, ref userCancelled, element, directoryOfTreeNode);
                        },
                        "Add file " + fileName);
                    any = true;
                }
                if (any)
                {
                    FlatRedBall.Glue.Managers.TaskManager.Self.AddSync(() =>
                    {
                        GluxCommands.Self.SaveGlux();
                    },
                    "Save .glux");
                }
            }
        }

        private static void MoveState(TreeNode nodeMoving, TreeNode targetNode)
        {
            IElement currentElement = EditorLogic.CurrentElement;
            StateSave toAdd = (StateSave)nodeMoving.Tag;

            IElement sourceContainer = nodeMoving.GetContainingElementTreeNode().Tag as IElement;
            IElement targetContainer = targetNode.GetContainingElementTreeNode().Tag as IElement;

            if (targetNode.IsStateCategoryNode() || targetNode.IsStateListNode())
            {
                if (sourceContainer == targetContainer)
                {
                    EditorLogic.CurrentElement.RemoveState(EditorLogic.CurrentStateSave);
                }
                else
                {
                    toAdd = toAdd.Clone();
                }

                if (targetNode.IsStateCategoryNode())
                {
                    ((StateSaveCategory)targetNode.Tag).States.Add(toAdd);
                }
                else
                {
                    targetContainer.States.Add(toAdd);
                }

                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(targetContainer);
                GlueCommands.Self.RefreshCommands.RefreshUi(targetContainer);

            }


        }

        private static void MoveReferencedFile(TreeNode treeNodeMoving, TreeNode targetNode)
        {
            while (targetNode != null && targetNode.IsReferencedFile
                ())
            {
                targetNode = targetNode.Parent;
            }
            // If the user drops a file on a Screen or Entity, let's allow them to
            // complete the operation on the Files node
            if (targetNode is BaseElementTreeNode)
            {
                targetNode = ((BaseElementTreeNode)targetNode).FilesTreeNode;
            }

            ReferencedFileSave referencedFileSave = treeNodeMoving.Tag as ReferencedFileSave;

            if (targetNode.IsGlobalContentContainerNode())
            {
                if (targetNode.GetContainingElementTreeNode() == null)
                {
                    string targetDirectory = ProjectManager.MakeAbsolute(targetNode.GetRelativePath(), true);
                    MoveReferencedFileToDirectory(referencedFileSave, targetDirectory);
                }
                else
                {
                    DragAddFileToGlobalContent(treeNodeMoving, referencedFileSave);
                    // This means the user wants to add the file
                    // to global content.
                }
            }
            else if (targetNode.IsFolderForGlobalContentFiles())
            {
                string targetDirectory = ProjectManager.MakeAbsolute(targetNode.GetRelativePath(), true);
                MoveReferencedFileToDirectory(referencedFileSave, targetDirectory);
            }
            else if(targetNode.IsRootNamedObjectNode())
            {
                AddObjectViewModel viewModel = new AddObjectViewModel();
                viewModel.SourceType = SourceType.File;
                viewModel.SourceFile = (treeNodeMoving.Tag as ReferencedFileSave).Name;
                GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog(viewModel);
            }
            else if (!targetNode.IsFilesContainerNode() &&
                !targetNode.IsFolderInFilesContainerNode() &&
                !targetNode.IsFolderForGlobalContentFiles())
            {
                MessageBox.Show(@"Can't drop this file here");
                return;
            }
            else if (!string.IsNullOrEmpty(referencedFileSave.SourceFile) ||
                referencedFileSave.SourceFileCache.Count != 0)
            {
                MessageBox.Show("Can't move the file\n\n" + referencedFileSave.Name + "\n\nbecause it has source-referencing files.  These sources will be broken " +
                    "if the file is moved.  You will need to manually move the file, modify the source references, remove this file, then add the newly-created file.");
                return;
            }
            //if (targetNode.IsFolderInFilesContainerNode() || targetNode.IsFilesContainerNode())
            else
            {
                // See if we're moving the RFS from one Element to another
                IElement container = ObjectFinder.Self.GetElementContaining(referencedFileSave);
                TreeNode elementTreeNodeDroppingIn = targetNode.GetContainingElementTreeNode();
                IElement elementDroppingIn = null;
                if (elementTreeNodeDroppingIn != null)
                {
                    // User didn't drop on an entity, but instead on a node within the entity.
                    // Let's check if it's a subfolder. If so, we need to tell the user that we
                    // can't add the file in a subfolder.

                    if (targetNode.IsFolderInFilesContainerNode())
                    {
                        MessageBox.Show("Shared files cannot be added to subfolders, so it will be added directly to \"Files\"");
                    }

                    elementDroppingIn = elementTreeNodeDroppingIn.Tag as IElement;
                }

                if (container != elementDroppingIn)
                {
                    ElementViewWindow.SelectedNode = targetNode;

                    string absoluteFileName = ProjectManager.MakeAbsolute(referencedFileSave.Name, true);
                    string creationReport;
                    string errorMessage;

                    var newlyCreatedFile = ElementCommands.Self.CreateReferencedFileSaveForExistingFile(elementDroppingIn, null, absoluteFileName,
                                                                    PromptHandleEnum.Prompt, 
                                                                    referencedFileSave.GetAssetTypeInfo(),
                                                                    out creationReport,
                                                                    out errorMessage);

                    ElementViewWindow.UpdateChangedElements();

                    
                    

                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        MessageBox.Show(errorMessage);
                    }
                    else if(newlyCreatedFile != null)
                    {
                        GlueCommands.Self.TreeNodeCommands.SelectTreeNode(newlyCreatedFile);

                    }
                }
                else
                {
                    // Not moving into or out of an element
                    string targetDirectory = ProjectManager.MakeAbsolute(targetNode.GetRelativePath(), true);
                    MoveReferencedFileToDirectory(referencedFileSave, targetDirectory);
                }
            }
        }

        private static void MoveReferencedFileToDirectory(ReferencedFileSave referencedFileSave, string targetDirectory)
        {
            // Things to do:
            // 1 Move the TreeNode from one parent TreeNode to another UPDATE:  We will just refresh the UI for the Element or GlobalContent
            // 2 Move the file from one folder to another
            // 3 Remove the BuildItems from the project and add them back in the VisualStudio project
            // 4 Change the ReferencedFileSave's name
            // 5 Re-generate the containing Element (Screen or Entity)
            // 6 Save everything

            string oldNodeText = referencedFileSave.Name.Replace("/", "\\");



            string newNodeText = FlatRedBall.IO.FileManager.MakeRelative(targetDirectory, ProjectManager.ContentProject.Directory) + FileManager.RemovePath(referencedFileSave.Name);
            newNodeText = newNodeText.Replace("/", "\\");

            string oldFileName = ProjectManager.MakeAbsolute(referencedFileSave.Name, true);
            string targetFile = targetDirectory + FileManager.RemovePath(oldFileName);

            bool canMove = true;

            // There's so much error checking and validation that we
            // could/should do here, but man, I just can't spend forever
            // on it because I need to get the game I'm working on moving forward
            // But I'm going to at least improve it a little bit by having the referenced
            // files get copied over.
            Dictionary<string, string> mOldNewDependencyFileDictionary = new Dictionary<string, string>();
            List<string> referencedFiles = ContentParser.GetFilesReferencedByAsset(oldFileName, TopLevelOrRecursive.Recursive);
            string oldDirectoryFull = FileManager.GetDirectory(oldFileName);

            foreach (string file in referencedFiles)
            {
                string relativeToRfs = FileManager.MakeRelative(file, FileManager.GetDirectory(oldFileName));

                string targetReferencedFileName = targetDirectory + relativeToRfs;

                mOldNewDependencyFileDictionary.Add(file, targetReferencedFileName);

                if (!FileManager.IsRelativeTo(targetReferencedFileName, targetDirectory))
                {
                    MessageBox.Show("The file\n\n" + file + "\n\nis not relative to the file being moved, so it cannot be moved.  You must manually move these files and manually update the file reference.");
                    canMove = false;
                    break;
                }
            }


            if (canMove && File.Exists(targetFile))
            {
                MessageBox.Show("There is already a file by this name located in the directory you're trying to move to.");
                canMove = false;
            }
            if (canMove)
            {
                foreach (KeyValuePair<string, string> kvp in mOldNewDependencyFileDictionary)
                {
                    if (File.Exists(kvp.Value))
                    {
                        MessageBox.Show("Can't move the file because a dependency will be moved to\n\n" + kvp.Value + "\n\nand a file already exists there.");
                        canMove = false;
                        break;
                    }
                }

            }

            if (canMove)
            {
                // 1 Move the TreeNode from one parent TreeNode to another            
                //treeNodeMoving.Parent.Nodes.Remove(treeNodeMoving);
                //targetNode.Nodes.Add(treeNodeMoving);
                // This is updated at the bottom of this method



                // 2 Move the file from one folder to another
                File.Move(oldFileName, targetFile);
                foreach (KeyValuePair<string, string> kvp in mOldNewDependencyFileDictionary)
                {
                    File.Move(kvp.Key, kvp.Value);
                }


                // 3 Remove the BuildItems from the project and add them back in the VisualStudio project
                ProjectBase projectBase = ProjectManager.ProjectBase;
                if (ProjectManager.ContentProject != null)
                {
                    projectBase = ProjectManager.ContentProject;
                }

                ProjectManager.RemoveItemFromProject(projectBase, oldNodeText, false);
                projectBase.AddContentBuildItem(targetFile);
                foreach (KeyValuePair<string, string> kvp in mOldNewDependencyFileDictionary)
                {
                    string fileFileRelativeToProject = FileManager.MakeRelative(kvp.Key, projectBase.Directory);

                    ProjectManager.RemoveItemFromProject(projectBase, fileFileRelativeToProject, false);
                    projectBase.AddContentBuildItem(kvp.Value);
                }
                // TODO:  This should also check to see if something else is referencing this content.
                // I'm going to write it to not make this check now since I'm just getting the initial system set up



                // 4 Change the ReferencedFileSave's name
                referencedFileSave.SetNameNoCall(newNodeText.Replace("\\", "/"));
                // No need for this, it'll get updated automatically
                // treeNodeMoving.Text = newNodeText;



                // 5 Re-generate the containing Element (Screen or Entity)
                if (EditorLogic.CurrentElement != null)
                {
                    CodeWriter.GenerateCode(EditorLogic.CurrentElement);
                }
                else
                {
                    ContentLoadWriter.UpdateLoadGlobalContentCode();
                }


                // The new 1:  Update 
                if (EditorLogic.CurrentElement != null)
                {
                    EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
                }
                else
                {
                    ElementViewWindow.UpdateGlobalContentTreeNodes(false);
                }


                // 6 Save everything
                GluxCommands.Self.SaveGlux();
                ProjectManager.SaveProjects();
            }
        }

        private static void DragAddFileToGlobalContent(TreeNode treeNodeMoving, ReferencedFileSave referencedFileSave)
        {
            if (referencedFileSave.GetContainerType() == ContainerType.None)
            {
                // This means the user dragged a file from global content onto the global content tree node - 
                // we shouldn't do anything here.  It's not a valid operation, but at the same time, it may have
                // happened accidentally and we don't want to burden the user with popups.
            }
            else
            {
                bool isAlreadyPartOfReferencedFiles = false;
                // If the file is already part of GlobalContent, then warn the user and do nothing
                foreach (ReferencedFileSave fileInGlobalContent in ObjectFinder.Self.GlueProject.GlobalFiles)
                {
                    if (fileInGlobalContent.Name == referencedFileSave.Name)
                    {
                        isAlreadyPartOfReferencedFiles = true;
                        break;
                    }
                }


                if (isAlreadyPartOfReferencedFiles)
                {

                    MessageBox.Show("The file\n\n" + referencedFileSave.Name + "\n\nis already a Global Content File");
                }
                else
                {
                    // If we got here, that means that the file that
                    // the user is dragging in to Global Content Files
                    // can be added to Global Content Files; however, the
                    // owner of the file may not be using global content.  We
                    // should ask the user if the containing IElement should use
                    // global content
                    IElement container = referencedFileSave.GetContainer();



                    if (!container.UseGlobalContent)
                    {
                        string screenOrEntity = "Screen";

                        if (container is EntitySave)
                        {
                            screenOrEntity = "Entity";
                        }

                        DialogResult result = MessageBox.Show("The " + screenOrEntity + " " + container.ToString() +
                            "does not UseGlobalContent.  Would you like " +
                            " to set UseGlobalContent to true?", "Set UseGlobalContent to true?", MessageBoxButtons.YesNo);

                        if (result == DialogResult.Yes)
                        {
                            container.UseGlobalContent = true;
                        }
                    }

                    bool useFullPathAsName = true;
                    ElementCommands.Self.AddReferencedFileToGlobalContent(referencedFileSave.Name, useFullPathAsName);

                    ContentLoadWriter.UpdateLoadGlobalContentCode();

                    ProjectManager.SaveProjects();
                    GluxCommands.Self.SaveGlux();
                }

            }

        }

        private static void MoveNamedObject(TreeNode treeNodeMoving, TreeNode targetNode)
        {
            if (targetNode != null)
            {
                NamedObjectSave targetNos = targetNode.Tag as NamedObjectSave;
                NamedObjectSave movingNos = treeNodeMoving.Tag as NamedObjectSave;

                bool succeeded = false;
                if (targetNode == null)
                {
                    // Didn't move on to anything
                }
                else if (targetNode.IsRootNamedObjectNode())
                {
                    succeeded = MoveObjectOnObjectsRoot(treeNodeMoving, targetNode, movingNos, succeeded);
                }
                else if (targetNode.IsRootCustomVariablesNode())
                {
                    MoveObjectOnRootCustomVariablesNode(treeNodeMoving, targetNode);
                }
                else if (targetNode.Tag is IElement)
                {
                    succeeded = DragDropNosIntoElement(movingNos, targetNode.Tag as IElement);
                }
                else if (targetNode.IsRootEventsNode())
                {
                    succeeded = DragDropNosOnRootEventsNode(treeNodeMoving, targetNode);
                }
                else if (targetNos != null && targetNos.SourceType == SourceType.FlatRedBallType)
                {
                    string targetClassType = targetNos.SourceClassType;

                    #region Failure cases

                    if (string.IsNullOrEmpty(targetClassType))
                    {
                        MessageBox.Show("The target Object does not have a defined type.  This operation is not valid");
                    }

                    #endregion

                    #region Moving on to a Layer

                    else if (targetClassType == "Layer")
                    {
                        // Only allow this if the NOS's are on the same object
                        if (ObjectFinder.Self.GetElementContaining(movingNos) ==
                            ObjectFinder.Self.GetElementContaining(targetNos))
                        {
                            succeeded = true;
                            movingNos.LayerOn = targetNos.InstanceName;
                            MainGlueWindow.Self.PropertyGrid.Refresh();
                        }
                    }

                    #endregion

                    #region Moving on to a PositionedObjectList

                    else if (targetClassType == "PositionedObjectList<T>")
                    {
                        succeeded = HandleDropOnList(treeNodeMoving, targetNode, targetNos, movingNos);
                    }
                    #endregion
                }
                else
                {
                    MessageBox.Show("Invalid movement");
                }


                if (succeeded)
                {
                    if (EditorLogic.CurrentElement != null)
                    {
                        ElementViewWindow.GenerateSelectedElementCode();

                    }
                    else
                    {
                        ContentLoadWriter.UpdateLoadGlobalContentCode();
                    }
                    ProjectManager.SaveProjects();
                    GluxCommands.Self.SaveGlux();
                }
            }
        }

        private static bool DragDropNosOnRootEventsNode(TreeNode treeNodeMoving, TreeNode targetNode)
        {
            bool succeeded = true;

            
            if (treeNodeMoving.GetContainingElementTreeNode() != targetNode.GetContainingElementTreeNode())
            {
                succeeded = false;
            }

            if (succeeded)
            {
                // show the add new variable window and select this object
                RightClickHelper.ShowAddEventWindow(treeNodeMoving.Tag as NamedObjectSave);

            }

            return succeeded;
        }

        private static bool MoveObjectOnRootCustomVariablesNode(TreeNode treeNodeMoving, TreeNode targetNode)
        {
            bool succeeded = true;

            if (treeNodeMoving.GetContainingElementTreeNode() != targetNode.GetContainingElementTreeNode())
            {
                succeeded = false;
            }

            if (succeeded)
            {
                // show the add new variable window and select this object
                RightClickHelper.AddVariableClick( CustomVariableType.Tunneled, ((NamedObjectSave) treeNodeMoving.Tag).InstanceName);
            }

            return succeeded;
        }

        private static bool MoveObjectOnObjectsRoot(TreeNode treeNodeMoving, TreeNode targetNode, NamedObjectSave movingNos, bool succeeded)
        {
            // Dropped it on the "Objects" tree node

            // Let's see if it's the Objects that contains node or another one

            IElement parentOfMovingNos = movingNos.GetContainer();
            IElement elementMovingInto = ((BaseElementTreeNode)targetNode.Parent).SaveObjectAsElement;

            if (parentOfMovingNos == elementMovingInto)
            {

                if (treeNodeMoving.Parent.IsNamedObjectNode())
                {
                    succeeded = true;

                    // removing from a list
                    NamedObjectSave container = treeNodeMoving.Parent.Tag as NamedObjectSave;



                    IElement elementToAddTo = movingNos.GetContainer();
                    container.ContainedObjects.Remove(movingNos);
                    NamedObjectSaveExtensionMethodsGlue.AddNewNamedObjectToElementTreeNode(EditorLogic.CurrentElementTreeNode, movingNos, false);
                    EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();

                    IElement elementToRegenerate = targetNode.Parent.Tag as IElement;
                }
            }
            else
            {
                succeeded = DragDropNosIntoElement(movingNos, elementMovingInto);
            }
            return succeeded;
        }

        private static bool HandleDropOnList(TreeNode treeNodeMoving, TreeNode targetNode, NamedObjectSave targetNos, NamedObjectSave movingNos)
        {
            bool succeeded = true;

            #region Failure cases

            if (string.IsNullOrEmpty(targetNos.SourceClassGenericType))
            {
                MessageBox.Show("The target Object has not been given a list type yet");
            }
            else if (movingNos.CanBeInList(targetNos) == false)
            {
                MessageBox.Show("The Object you are moving is of type " + movingNos.SourceClassType +
                    " but the list is of type " + targetNos.SourceClassGenericType);

            }
            else if (treeNodeMoving.Parent.IsRootNamedObjectNode() == false)
            {
                MessageBox.Show("The Object you are moving is already part of a list, so it can't be moved");
            }

            #endregion

            else
            {
                succeeded = true;
                // Get the old parent of the moving NOS
                TreeNode parentTreeNode = treeNodeMoving.Parent;
                if (parentTreeNode.IsNamedObjectNode())
                {
                    NamedObjectSave parentNos = parentTreeNode.Tag as NamedObjectSave;

                    parentNos.ContainedObjects.Remove(movingNos);
                }
                else
                {
                    EditorLogic.CurrentElement.NamedObjects.Remove(movingNos);
                }
                parentTreeNode.Nodes.Remove(treeNodeMoving);
                targetNode.Nodes.Add(treeNodeMoving);
                // Add the NOS to the tree node moving on
                targetNos.ContainedObjects.Add(movingNos);

            }
            return succeeded;
        }



        private static bool DragDropNosIntoElement(NamedObjectSave movingNos, IElement elementMovingInto)
        {
            bool succeeded = true;

            // moving to another element, so let's copy
            NamedObjectSave clonedNos = movingNos.Clone();

            UpdateNosAfterDragDrop(clonedNos, elementMovingInto);

            elementMovingInto.NamedObjects.Add(clonedNos);

            var referenceCheck = ProjectManager.VerifyReferenceGraph(elementMovingInto);

            if (referenceCheck == ProjectManager.CheckResult.Failed)
            {
                succeeded = false;
                // VerifyReferenceGraph (currently) shows a popup so we don't have to here
                //MessageBox.Show("This movement would result in a circular reference");

                elementMovingInto.NamedObjects.Remove(clonedNos);
            }

            if (succeeded)
            {
                // If an object which was on a Layer
                // is moved into another Element, then
                // the cloned object probably shouldn't
                // be on a layer.  Not sure if we want to 
                // see if there is a Layer with the same-name
                // but we maybe shouldn't assume that they mean
                // the same thing.
                clonedNos.LayerOn = null;

                BaseElementTreeNode treeNodeForElementMovedInto = GlueState.Self.Find.ElementTreeNode(elementMovingInto);
                treeNodeForElementMovedInto.UpdateReferencedTreeNodes();
                CodeGeneratorIElement.GenerateElementDerivedAndReferenced(elementMovingInto);

                MessageBox.Show("Copied\n" + movingNos + "\n\nto\n" + clonedNos);
            }
            return succeeded;
        }


        private static void UpdateNosAfterDragDrop(NamedObjectSave clonedNos, IElement elementMovingInto)
        {
            if (elementMovingInto is EntitySave)
            {
                clonedNos.AttachToCamera = false;
                clonedNos.AttachToContainer = true;
            }
            else if (elementMovingInto is ScreenSave)
            {
                clonedNos.AttachToContainer = false;
            }
        }

        public static TreeNode MoveEntityOn(EntityTreeNode treeNodeMoving, TreeNode targetNode)
        {
            TreeNode newTreeNode = null;

            #region Moving the Entity into (or out of) a directory
            if (targetNode.IsDirectoryNode() || targetNode.IsRootEntityNode())
            {
                MoveEntityToDirectory(treeNodeMoving, targetNode);
            }

            #endregion

            #region Moving an Entity onto another element to create an instance

            else if (targetNode.IsEntityNode() || targetNode.IsScreenNode() || targetNode.IsRootNamedObjectNode())
            {
                bool isValidDrop = true;
                // Make sure that we don't drop an Entity into its own Objects
                if (targetNode.IsRootNamedObjectNode())
                {
                    if(treeNodeMoving == targetNode.GetContainingElementTreeNode())
                    {

                        isValidDrop = false;
                    }
                }
                if (isValidDrop)
                {
                    newTreeNode = MoveEntityOntoElement(treeNodeMoving, targetNode, newTreeNode);
                }
            }

            #endregion

            #region Moving an Entity onto a NamedObject (currently supports only Lists)

            else if (targetNode.IsNamedObjectNode())
            {
                // Allow drop only if it's a list or Layer
                NamedObjectSave targetNamedObjectSave = targetNode.Tag as NamedObjectSave;

                if (!targetNamedObjectSave.IsList && !targetNamedObjectSave.IsLayer)
                {
                    MessageBox.Show("The target is not a List or Layer so we can't add an Object to it.", "Target not valid");
                }
                if (targetNamedObjectSave.IsLayer)
                {
                    TreeNode parent = targetNode.Parent;

                    newTreeNode = MoveEntityOn(treeNodeMoving, parent);

                    // this created a new NamedObjectSave.  Let's put that on the Layer
                    MoveNamedObject(newTreeNode, targetNode);
                }
                else
                {
                    // Make sure that the two types match
                    string listType = targetNamedObjectSave.SourceClassGenericType;

                    bool isOfTypeOrInherits =
                        listType == treeNodeMoving.EntitySave.Name ||
                        treeNodeMoving.EntitySave.InheritsFrom(listType);

                    if (isOfTypeOrInherits == false)
                    {
                        MessageBox.Show("The target list type is of type\n\n" +
                            listType +
                            "\n\nBut the Entity is of type\n\n" +
                            treeNodeMoving.EntitySave.Name +
                            "\n\nCould not add an instance to the list", "Could not add instance");
                    }
                    else
                    {
                        NamedObjectSave namedObject = new NamedObjectSave();
                        namedObject.InstanceName =
                            FileManager.RemovePath(treeNodeMoving.EntitySave.Name) + "1";

                        StringFunctions.MakeNameUnique<NamedObjectSave>(
                            namedObject, targetNamedObjectSave.ContainedObjects);

                        // Not sure if we need to set this or not, but I think 
                        // any instance added to a list will not be defined by base
                        namedObject.DefinedByBase = false;

                        NamedObjectSaveExtensionMethodsGlue.AddNamedObjectToCurrentNamedObjectList(namedObject);

                        ElementViewWindow.GenerateSelectedElementCode();
                        // Don't save the Glux, the caller of this method will take care of it
                        // GluxCommands.Self.SaveGlux();
                    }

                }
            }

            #endregion

            else if (targetNode.IsGlobalContentContainerNode())
            {
                AskAndAddAllContainedRfsToGlobalContent(treeNodeMoving.SaveObjectAsElement);
            }

            return newTreeNode;
        }

        private static TreeNode MoveEntityOntoElement(EntityTreeNode treeNodeMoving, TreeNode targetNode, TreeNode newTreeNode)
        {
            EntitySave entitySaveMoved = treeNodeMoving.EntitySave;

            #region Get the IElement elementToCreateIn

            IElement elementToCreateIn = null;

            if (targetNode.IsRootNamedObjectNode())
            {
                BaseElementTreeNode baseElementTreeNode = targetNode.Parent as BaseElementTreeNode;
                elementToCreateIn = baseElementTreeNode.SaveObjectAsElement;
            }
            else
            {
                elementToCreateIn = ((BaseElementTreeNode)targetNode).SaveObjectAsElement;
            }

            #endregion

            // We used to ask the user if they're sure, but this isn't a destructive action so just do it:
            //DialogResult result =
            //    MessageBox.Show("Create a new Object in\n\n" + elementToCreateIn.Name + "\n\nusing\n\n\t" + entitySaveMoved.Name + "?", "Create new Object?", MessageBoxButtons.YesNo);

            NamedObjectSave newNamedObject = CreateNewNamedObjectInElement(elementToCreateIn, entitySaveMoved);
            newTreeNode = GlueState.Self.Find.NamedObjectTreeNode(newNamedObject);

            return newTreeNode;
        }

        internal static NamedObjectSave CreateNewNamedObjectInElement(IElement elementToCreateIn, EntitySave blueprintEntity, bool createList = false)
        {
            if (elementToCreateIn is EntitySave && ((EntitySave)elementToCreateIn).ImplementsIVisible && !blueprintEntity.ImplementsIVisible)
            {
                MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                mbmb.MessageText = "The Entity\n\n" + blueprintEntity + "\n\nDoes not Implement IVisible, but the Entity it is being dropped in does.  " +
                    "What would you like to do?";

                mbmb.AddButton("Make " + blueprintEntity.Name + " implement IVisible", DialogResult.OK);
                mbmb.AddButton("Nothing (your code will not compile until this problem is resolved manually)", DialogResult.Cancel);

                DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);
                if (result == DialogResult.OK)
                {
                    blueprintEntity.ImplementsIVisible = true;
                    CodeGeneratorIElement.GenerateElementDerivedAndReferenced(blueprintEntity);
                }

            }

            BaseElementTreeNode elementTreeNode = GlueState.Self.Find.ElementTreeNode(elementToCreateIn);

            //EntityTreeNode entityTreeNode =
            //    ElementViewWindow.GetEntityTreeNode(entityToCreateIn); 

            NamedObjectSave newNamedObject = new NamedObjectSave();

            // We'll add "List" or "Instance" below
            string newName = FileManager.RemovePath(blueprintEntity.Name);

            #region Set the source type properties for the new NamedObject

            if (createList)
            {
                newName += "List";
                newNamedObject.SourceType = SourceType.FlatRedBallType;
                newNamedObject.SourceClassType = "PositionedObjectList<T>";
                newNamedObject.SourceClassGenericType = blueprintEntity.Name;
                newNamedObject.UpdateCustomProperties();
            }
            else
            {
                newName += "Instance";
                newNamedObject.SourceType = SourceType.Entity;
                newNamedObject.SourceClassType = blueprintEntity.Name;
                newNamedObject.UpdateCustomProperties();
            }

            #endregion

            #region Set the name for the new NamedObject

            // get an acceptable name for the new object
            if (elementToCreateIn.GetNamedObjectRecursively(newName) != null)
            {
                newName += "2";
            }

            while (elementToCreateIn.GetNamedObjectRecursively(newName) != null)
            {
                newName = StringFunctions.IncrementNumberAtEnd(newName);
            }

            newNamedObject.InstanceName = newName;


            #endregion

            // We need to add to managers here.  Why?  Because normally when the type of a NamedObject is changed, 
            // the PropertyGrid handles setting whether it should be added or not. But in this case, we're not changing
            // the type of the new NamedObject through the PropertyGrid - instead it's being set programatically to be an
            // Entity.  So, we should add to managers programatically since the PropertyGrid won't do it for us.
            // Update December 11, 2011
            // AddToManagers defaults to
            // true on new NamedObjectSaves
            // so there's no need to explicitly
            // set it to true here.
            //newNamedObject.AddToManagers = true;


            NamedObjectSaveExtensionMethodsGlue.AddNewNamedObjectToElementTreeNode(elementTreeNode, newNamedObject, true);

            Plugins.PluginManager.ReceiveOutput($"Created {newNamedObject}");

            return newNamedObject;
        }

        static void MoveEntityToDirectory(EntityTreeNode treeNodeMoving, TreeNode targetNode)
        {
            bool succeeded = true;

            EntitySave entitySave = treeNodeMoving.EntitySave;

            string newRelativeDirectory = targetNode.GetRelativePath();

            string newName = newRelativeDirectory.Replace("/", "\\") + entitySave.ClassName;

            // modify data and files
            succeeded = GlueCommands.Self.GluxCommands.MoveEntityToDirectory(entitySave, newRelativeDirectory);

            // adjust the UI
            if (succeeded)
            {
                treeNodeMoving.Parent.Nodes.Remove(treeNodeMoving);
                targetNode.Nodes.Add(treeNodeMoving);


                treeNodeMoving.GeneratedCodeFile = newName + ".Generated.cs";
                treeNodeMoving.CodeFile = newName + ".cs";
                treeNodeMoving.Text = FileManager.RemovePath(newName);

            }

            // Generate and save
            if (succeeded)
            {
                // 7: Save it!
                ProjectLoader.Self.MakeGeneratedItemsNested();
                CodeWriter.GenerateCode(entitySave);

                GluxCommands.Self.SaveGlux();
                ProjectManager.SaveProjects();

                GlueState.Self.CurrentElement = entitySave;
            }
        }

        

    }
}
