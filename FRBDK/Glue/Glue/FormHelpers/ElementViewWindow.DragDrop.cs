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
                    MoveReferencedFile(nodeMoving, targetNode);
                    shouldSaveGlux = true;
                }
                else if (nodeMoving.IsNamedObjectNode())
                {
                    DragDropManager.Self.MoveNamedObject(nodeMoving, targetNode);
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

                var droppedFiles = (string[])e.Data.GetData("FileDrop");

                foreach (var fileName in droppedFiles)
                {
                    string extension = FileManager.GetExtension(fileName);

                    if (extension == "entz" || extension == "scrz")
                    {
                        ElementImporter.ImportElementFromFile(fileName, true);
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
                foreach (var fileName in (string[])e.Data.GetData("FileDrop"))
                {
                    // First select the entity
                    ElementViewWindow.SelectedNode = targetNode;


                    var element = GlueState.Self.CurrentElement;
                    if(string.IsNullOrEmpty(directoryPath))
                    {
                        directoryPath = EditorLogic.CurrentTreeNode.GetRelativePath();
                    }
            

                    FlatRedBall.Glue.Managers.TaskManager.Self.AddSync(() =>
                        {
                            var newRfs = AddExistingFileManager.Self.AddSingleFile(fileName, ref userCancelled, element, directoryPath);

                            GlueCommands.Self.DoOnUiThread(() => GlueCommands.Self.SelectCommands.Select(newRfs));
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
            var response = GeneralResponse.SuccessfulResponse;

            while (targetNode != null && targetNode.IsReferencedFile())
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

            if (!targetNode.IsFilesContainerNode() &&
                !targetNode.IsFolderInFilesContainerNode() &&
                !targetNode.IsFolderForGlobalContentFiles() &&
                !targetNode.IsNamedObjectNode() && 
                !targetNode.IsRootNamedObjectNode())
            {
                response.Fail(@"Can't drop this file here");
            }
            else if (!string.IsNullOrEmpty(referencedFileSave.SourceFile) ||
                referencedFileSave.SourceFileCache.Count != 0)
            {
                response.Fail("Can't move the file\n\n" + referencedFileSave.Name + "\n\nbecause it has source-referencing files.  These sources will be broken " +
                    "if the file is moved.  You will need to manually move the file, modify the source references, remove this file, then add the newly-created file.");
            }

            if(response.Succeeded)
            {

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
                else if(targetNode.IsNamedObjectNode() && 
                    // dropping on an object in the same element
                    targetNode.GetContainingElementTreeNode() == treeNodeMoving.GetContainingElementTreeNode())
                {
                    response = HandleDroppingFileOnObjectInSameElement(targetNode, referencedFileSave);

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
                            response.Message = "Shared files cannot be added to subfolders, so it will be added directly to \"Files\"";
                        }

                        elementDroppingIn = elementTreeNodeDroppingIn.Tag as IElement;
                    }

                    if (container != elementDroppingIn)
                    {
                        // Make sure the target element is not named the same as the file itself.
                        // For example, dropping a file called Level1.tmx in a screen called Level1. 
                        // This will not compile so we shouldn't allow it.

                        var areNamedTheSame = elementDroppingIn.GetStrippedName() == referencedFileSave.GetInstanceName();    

                        if(areNamedTheSame)
                        {
                            response.Fail($"The file {referencedFileSave.GetInstanceName()} has the same name as the target screen. it will not be added since this is not allowed.");
                        }

                        if(response.Succeeded)
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
                    }
                    else
                    {
                        // Not moving into or out of an element
                        string targetDirectory = ProjectManager.MakeAbsolute(targetNode.GetRelativePath(), true);
                        MoveReferencedFileToDirectory(referencedFileSave, targetDirectory);
                    }
                }
            }

            if(!string.IsNullOrEmpty(response.Message))
            {
                MessageBox.Show(response.Message);
            }
        }

        private static GeneralResponse HandleDroppingFileOnObjectInSameElement(TreeNode targetNode, ReferencedFileSave referencedFileSave)
        {
            // Dropping the file on an object. If the object's type matches the named object's
            // entire file, ask the user if they want to make the object come from the file...
            var rfsAti = referencedFileSave.GetAssetTypeInfo();
            var namedObject = ((NamedObjectSave)targetNode.Tag);
            var namedObjectAti = namedObject.GetAssetTypeInfo();

            var response = GeneralResponse.SuccessfulResponse;

            var shouldAskAboutChangingObjectToBeFromFile =
                rfsAti == namedObjectAti && rfsAti != null;


            if (shouldAskAboutChangingObjectToBeFromFile)
            {
                var dialogResult = MessageBox.Show(
                    $"Would you like to set the object {namedObject.InstanceName} to be created from the file {referencedFileSave.Name}?",
                    $"Set {namedObject.InstanceName} to be from file?",
                    MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    namedObject.SourceType = SourceType.File;
                    namedObject.SourceFile = referencedFileSave.Name;
                    namedObject.SourceName = $"Entire File ({rfsAti.RuntimeTypeName})";

                    // This might be the case if the base is SetbyDerived. 
                    if(namedObject.Instantiate == false)
                    {
                        // If an entire object is dropped on this, it's likely that the user wants to create (aka instantiate)
                        // the object using the entire file. The user may not realize that the object is set to not initialize,
                        // so let's ask them and offer to set Initialize to true
                        string message = $"The object {namedObject.InstanceName} has its 'Instantiate' variable set to 'false'. " +
                            $"This needs to be set to 'true' for the object to be created from the file. Set it to true?";

                        var setToTrueResponse = MessageBox.Show(message,
                            "Set Instantiate to true?",
                            MessageBoxButtons.YesNo);

                        if(setToTrueResponse == DialogResult.Yes)
                        {
                            namedObject.Instantiate = true;
                        }
                    }

                    GlueCommands.Self.GluxCommands.SaveGluxTask();
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCodeTask();
                }
            }
            else
            {
                response.Fail(
                    $"The object {namedObject.InstanceName} cannot be entirely set from {referencedFileSave.Name}." +
                    $"To set this object from an object contained within the file, select the object and change its source values.");
            }

            return response;
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



            string newNodeText = FlatRedBall.IO.FileManager.MakeRelative(targetDirectory, ProjectManager.ProjectBase.GetAbsoluteContentFolder()) + FileManager.RemovePath(referencedFileSave.Name);
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


                // In case it doesn't exist
                System.IO.Directory.CreateDirectory(FileManager.GetDirectory(targetFile));

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
                    GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();
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

                    GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

                    ProjectManager.SaveProjects();
                    GluxCommands.Self.SaveGlux();
                }

            }

        }
        
    }
}
