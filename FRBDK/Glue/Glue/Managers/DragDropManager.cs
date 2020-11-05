using EditorObjects.Parsing;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.StandardTypes;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using Glue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Managers
{
    public class DragDropManager : Singleton<DragDropManager>
    {
        #region Named Object

        public void MoveNamedObject(TreeNode treeNodeMoving, TreeNode targetNode)
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
                    var ati = targetNos.GetAssetTypeInfo();
                    string targetClassType = ati?.FriendlyName;

                    #region Failure cases

                    if (string.IsNullOrEmpty(targetClassType))
                    {
                        MessageBox.Show("The target Object does not have a defined type.  This operation is not valid");
                    }

                    #endregion

                    else if (ati == AvailableAssetTypes.CommonAtis.Layer)
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

                    else if(targetClassType == "ShapeCollection")
                    {
                        var response = HandleDropOnShapeCollection(treeNodeMoving, targetNode, targetNos, movingNos);

                        if(!response.Succeeded && IsCollidableOrCollidableList(movingNos))
                        {
                            response = HandleCreateCollisionRelationship(movingNos, targetNos);
                        }

                        if(!response.Succeeded)
                        {
                            MessageBox.Show($"Could not drop {movingNos} on {targetNos}");

                        }
                        succeeded = response.Succeeded;
                    }

                    else if(IsCollidableOrCollidableList(movingNos) && IsCollidableOrCollidableList(targetNos))
                    {
                        var response = HandleCreateCollisionRelationship(movingNos, targetNos);

                        if(!response.Succeeded)
                        {
                            MessageBox.Show(response.Message);
                        }

                        succeeded = response.Succeeded;
                    }
                    //else if(IsCollidable(movingNos) && IsCollidableList(targetNos) && movingNos.CanBeInList(targetNos) == false)
                    //{
                    //    var response = HandleCreateCollisionRelationship(movingNos, targetNos);

                    //    if (!response.Succeeded)
                    //    {
                    //        MessageBox.Show(response.Message);
                    //    }

                    //    succeeded = response.Succeeded;
                    //}
                    else if (ati == AvailableAssetTypes.CommonAtis.PositionedObjectList)
                    {

                        var response = HandleDropOnList(treeNodeMoving, targetNode, targetNos, movingNos);
                        if(!response.Succeeded)
                        {
                            MessageBox.Show(response.Message);
                        }
                        succeeded = response.Succeeded;
                    }

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
                        GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();
                    }
                    ProjectManager.SaveProjects();
                    GluxCommands.Self.SaveGlux();
                }
            }
        }

        private GeneralResponse HandleCreateCollisionRelationship(NamedObjectSave movingNos, NamedObjectSave targetNos)
        {
            PluginManager.ReactToCreateCollisionRelationshipsBetween(movingNos, targetNos);
            return GeneralResponse.SuccessfulResponse;
        }

        // if both are lists, and both are ICollidable, then bring up the collision relationship 
        static bool IsCollidableOrCollidableList(NamedObjectSave nos)
        {
            if (nos.IsList)
            {
                var type = nos.SourceClassGenericType;

                // For a more complete impl, see:
                // CollisionRelationshipViewModelController
                return !string.IsNullOrEmpty(nos.SourceClassGenericType) &&
                    ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType)?.ImplementsICollidable == true;
            }
            else if(nos.GetAssetTypeInfo()?.RuntimeTypeName == "FlatRedBall.TileCollisions.TileShapeCollection" ||
                nos.GetAssetTypeInfo()?.RuntimeTypeName == "TileShapeCollection")
            {
                return true;
            }
            else if(nos.GetAssetTypeInfo()?.RuntimeTypeName == "FlatRedBall.Math.Geometry.ShapeCollection" ||
                nos.GetAssetTypeInfo()?.RuntimeTypeName == "ShapeCollection")
            {
                return true;
            }
            else if(nos.SourceType == SourceType.Entity && 
                ObjectFinder.Self.GetEntitySave( nos.SourceClassType)?.ImplementsICollidable == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool MoveObjectOnObjectsRoot(TreeNode treeNodeMoving, TreeNode targetNode, NamedObjectSave movingNos, bool succeeded)
        {
            // Dropped it on the "Objects" tree node

            // Let's see if it's the Objects that contains node or another one

            IElement parentOfMovingNos = movingNos.GetContainer();
            IElement elementMovingInto = ((BaseElementTreeNode)targetNode.Parent).SaveObject;

            if (parentOfMovingNos == elementMovingInto)
            {

                if (treeNodeMoving.Parent.IsNamedObjectNode())
                {
                    succeeded = true;

                    // removing from a list
                    NamedObjectSave container = treeNodeMoving.Parent.Tag as NamedObjectSave;

                    IElement elementToAddTo = movingNos.GetContainer();
                    container.ContainedObjects.Remove(movingNos);
                    AddExistingNamedObjectToElement(
                        GlueState.Self.CurrentElement, movingNos);
                    EditorLogic.CurrentElementTreeNode.RefreshTreeNodes();

                    IElement elementToRegenerate = targetNode.Parent.Tag as IElement;
                }
            }
            else
            {
                succeeded = DragDropNosIntoElement(movingNos, elementMovingInto);
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
                treeNodeForElementMovedInto.RefreshTreeNodes();
                GlueCommands.Self.GenerateCodeCommands
                    .GenerateElementAndReferencedObjectCodeTask(elementMovingInto);

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
                GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog(
                    CustomVariableType.Tunneled, 
                    ((NamedObjectSave)treeNodeMoving.Tag).InstanceName);
            }

            return succeeded;
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

        private static GeneralResponse HandleDropOnList(TreeNode treeNodeMoving, TreeNode targetNode, NamedObjectSave targetNos, NamedObjectSave movingNos)
        {
            var toReturn = GeneralResponse.SuccessfulResponse;

            #region Failure cases

            if (string.IsNullOrEmpty(targetNos.SourceClassGenericType))
            {
                toReturn.Succeeded = false;
                toReturn.Message = "The target Object has not been given a list type yet";
            }
            else if (movingNos.CanBeInList(targetNos) == false)
            {
                toReturn.Succeeded = false;
                toReturn.Message = "The Object you are moving is of type " + movingNos.SourceClassType +
                    " but the list is of type " + targetNos.SourceClassGenericType;

            }
            else if (treeNodeMoving.Parent.IsRootNamedObjectNode() == false)
            {
                toReturn.Succeeded = false;
                toReturn.Message = "The Object you are moving is already part of a list, so it can't be moved";
            }

            #endregion

            else
            {
                toReturn.Succeeded = true;

                // Get the old parent of the moving NOS
                TreeNode parentTreeNode = treeNodeMoving.Parent;
                if (parentTreeNode.IsNamedObjectNode())
                {
                    NamedObjectSave parentNos = parentTreeNode.Tag as NamedObjectSave;

                    parentNos.ContainedObjects.Remove(movingNos);
                }
                else
                {
                    GlueState.Self.CurrentElement.NamedObjects.Remove(movingNos);
                }
                parentTreeNode.Nodes.Remove(treeNodeMoving);
                targetNode.Nodes.Add(treeNodeMoving);
                // Add the NOS to the tree node moving on
                targetNos.ContainedObjects.Add(movingNos);

            }
            return toReturn;
        }

        private GeneralResponse HandleDropOnShapeCollection(TreeNode treeNodeMoving, TreeNode targetNode, NamedObjectSave targetNos, NamedObjectSave movingNos)
        {
            var toReturn = GeneralResponse.SuccessfulResponse;

            if(movingNos.CanBeInShapeCollection() == false)
            {
                toReturn.Succeeded = false;
                toReturn.Message = "The Object you are moving is of type " + movingNos.SourceClassType +
                    " which cannot be contained in a ShapeCollection";
            }
            else
            {
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
                targetNos.ContainedObjects.Add(movingNos);

            }
            return toReturn;
        }

        #endregion

        #region Entity

        public TreeNode MoveEntityOn(EntityTreeNode treeNodeMoving, TreeNode targetNode)
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
                    if (treeNodeMoving == targetNode.GetContainingElementTreeNode())
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
                    DragDropManager.Self.MoveNamedObject(newTreeNode, targetNode);
                }
                else
                {
                    // Make sure that the two types match
                    string listType = targetNamedObjectSave.SourceClassGenericType;

                    var entity = treeNodeMoving.EntitySave;

                    bool isOfTypeOrInherits =
                        listType == entity.Name ||
                        treeNodeMoving.EntitySave.InheritsFrom(listType);

                    if (isOfTypeOrInherits == false)
                    {
                        MessageBox.Show("The target list type is of type\n\n" +
                            listType +
                            "\n\nBut the Entity is of type\n\n" +
                            entity.Name +
                            "\n\nCould not add an instance to the list", "Could not add instance");
                    }
                    else
                    {
                        NamedObjectSave namedObject = new NamedObjectSave();

                        if(GlueState.Self.CurrentGlueProject.FileVersion >= 
                            (int)GlueProjectSave.GluxVersions.ListsHaveAssociateWithFactoryBool)
                        {
                            namedObject.AssociateWithFactory = true;
                        }
                        namedObject.InstanceName =
                            FileManager.RemovePath(entity.Name) + "1";

                        StringFunctions.MakeNameUnique<NamedObjectSave>(
                            namedObject, targetNamedObjectSave.ContainedObjects);

                        // Not sure if we need to set this or not, but I think 
                        // any instance added to a list will not be defined by base
                        namedObject.DefinedByBase = false;

                        NamedObjectSaveExtensionMethodsGlue.AddNamedObjectToCurrentNamedObjectList(namedObject);

                        if(namedObject.SourceClassType != entity.Name)
                        {
                            namedObject.SourceClassType = entity.Name;
                            namedObject.UpdateCustomProperties();
                        }

                        ElementViewWindow.GenerateSelectedElementCode();
                        // Don't save the Glux, the caller of this method will take care of it
                        // GluxCommands.Self.SaveGlux();
                    }

                }
            }

            #endregion

            else if (targetNode.IsGlobalContentContainerNode())
            {
                AskAndAddAllContainedRfsToGlobalContent(treeNodeMoving.SaveObject);
            }

            return newTreeNode;
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
                GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
                CodeWriter.GenerateCode(entitySave);

                GluxCommands.Self.SaveGlux();
                GlueCommands.Self.ProjectCommands.SaveProjects();

                GlueState.Self.CurrentElement = entitySave;
            }
        }

        private TreeNode MoveEntityOntoElement(EntityTreeNode treeNodeMoving, TreeNode targetNode, TreeNode newTreeNode)
        {
            EntitySave entitySaveMoved = treeNodeMoving.EntitySave;

            #region Get the IElement elementToCreateIn

            IElement elementToCreateIn = null;

            if (targetNode.IsRootNamedObjectNode())
            {
                BaseElementTreeNode baseElementTreeNode = targetNode.Parent as BaseElementTreeNode;
                elementToCreateIn = baseElementTreeNode.SaveObject;
            }
            else
            {
                elementToCreateIn = ((BaseElementTreeNode)targetNode).SaveObject;
            }

            #endregion

            // We used to ask the user if they're sure, but this isn't a destructive action so just do it:
            //DialogResult result =
            //    MessageBox.Show("Create a new Object in\n\n" + elementToCreateIn.Name + "\n\nusing\n\n\t" + entitySaveMoved.Name + "?", "Create new Object?", MessageBoxButtons.YesNo);

            NamedObjectSave newNamedObject = CreateNewNamedObjectInElement(elementToCreateIn, entitySaveMoved);
            newTreeNode = GlueState.Self.Find.NamedObjectTreeNode(newNamedObject);
            GlueState.Self.CurrentNamedObjectSave = newNamedObject;

            return newTreeNode;
        }

        #endregion

        public NamedObjectSave CreateNewNamedObjectInElement(IElement elementToCreateIn, 
            EntitySave blueprintEntity, bool createList = false)
        {
            if (blueprintEntity == null)
            {
                throw new ArgumentNullException($"{nameof(blueprintEntity)} cannot be null");
            }

            if (elementToCreateIn is EntitySave && ((EntitySave)elementToCreateIn).ImplementsIVisible && !blueprintEntity.ImplementsIVisible)
            {
                var mbmb = new MultiButtonMessageBoxWpf();
                mbmb.MessageText = "The Entity\n\n" + blueprintEntity + 
                    "\n\nDoes not Implement IVisible, but the Entity it is being dropped in does.  " +
                    "What would you like to do?";

                mbmb.AddButton("Make " + blueprintEntity.Name + " implement IVisible", DialogResult.OK);
                mbmb.AddButton("Nothing (your code will not compile until this problem is resolved manually)", DialogResult.Cancel);

                var dialogResult = mbmb.ShowDialog();

                DialogResult result = DialogResult.Cancel;

                if(mbmb.ClickedResult != null  && dialogResult == true)
                {
                    result = (DialogResult)mbmb.ClickedResult;
                }

                if (result == DialogResult.OK)
                {
                    blueprintEntity.ImplementsIVisible = true;
                    GlueCommands.Self.GenerateCodeCommands
                        .GenerateElementAndReferencedObjectCodeTask(blueprintEntity);
                }

            }

            var addObjectViewModel = new AddObjectViewModel();
            // We'll add "List" or "Instance" below
            //string newName = FileManager.RemovePath(blueprintEntity.Name);

            #region Set the source type properties for the new NamedObject

            if (createList)
            {
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;

                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.PositionedObjectList;

                addObjectViewModel.SourceClassGenericType = blueprintEntity.Name;
                addObjectViewModel.ObjectName = FileManager.RemovePath(blueprintEntity.Name);
                addObjectViewModel.ObjectName += "List";
            }
            else
            {
                addObjectViewModel.SourceType = SourceType.Entity;
                addObjectViewModel.SelectedEntitySave = blueprintEntity;
                addObjectViewModel.ObjectName = FileManager.RemovePath(blueprintEntity.Name);
                addObjectViewModel.ObjectName += "Instance";
            }

            #endregion

            #region Set the name for the new NamedObject

            // get an acceptable name for the new object
            if (elementToCreateIn.GetNamedObjectRecursively(addObjectViewModel.ObjectName) != null)
            {
                addObjectViewModel.ObjectName += "2";
            }

            while (elementToCreateIn.GetNamedObjectRecursively(addObjectViewModel.ObjectName) != null)
            {
                addObjectViewModel.ObjectName = StringFunctions.IncrementNumberAtEnd(addObjectViewModel.ObjectName);
            }


            #endregion

            return GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectViewModel,
                elementToCreateIn, null);
        }

        private static void AddExistingNamedObjectToElement(IElement element, NamedObjectSave newNamedObject)
        {
            element.NamedObjects.Add(newNamedObject);
            GlueCommands.Self.RefreshCommands.RefreshUi(element);
            PluginManager.ReactToNewObject(newNamedObject);
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeTask(element);

        }

        private static void AskAndAddAllContainedRfsToGlobalContent(IElement element)
        {
            string message = "Add all contained files in " + element.ToString() + " to Global Content Files?  Files will still be referenced by " + element.ToString();

            DialogResult dialogResult = MessageBox.Show(message, "Add to Global Content?", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {

                if (!element.UseGlobalContent)
                {
                    string screenOrEntity = "Screen";

                    if (element is EntitySave)
                    {
                        screenOrEntity = "Entity";
                    }

                    DialogResult result = MessageBox.Show("The " + screenOrEntity + " " + element.ToString() +
                        "does not UseGlobalContent.  Would you like " +
                        " to set UseGlobalContent to true?", "Set UseGlobalContent to true?", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        element.UseGlobalContent = true;
                    }
                }

                foreach (ReferencedFileSave rfs in element.ReferencedFiles)
                {
                    bool alreadyExists = false;
                    foreach (ReferencedFileSave existingRfs in ObjectFinder.Self.GlueProject.GlobalFiles)
                    {
                        if (existingRfs.Name.ToLower() == rfs.Name.ToLower())
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        bool useFullPathAsName = true;
                        GlueCommands.Self.GluxCommands.AddReferencedFileToGlobalContent(rfs.Name, useFullPathAsName);
                    }
                }


                GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

                ProjectManager.SaveProjects();
                GluxCommands.Self.SaveGlux();
            }
        }

        public void MoveReferencedFile(TreeNode treeNodeMoving, TreeNode targetNode)
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
                referencedFileSave.SourceFileCache?.Count > 0)
            {
                response.Fail("Can't move the file\n\n" + referencedFileSave.Name + "\n\nbecause it has source-referencing files.  These sources will be broken " +
                    "if the file is moved.  You will need to manually move the file, modify the source references, remove this file, then add the newly-created file.");
            }

            if (response.Succeeded)
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
                else if (targetNode.IsRootNamedObjectNode())
                {
                    AddObjectViewModel viewModel = new AddObjectViewModel();
                    viewModel.SourceType = SourceType.File;
                    viewModel.SourceFile = (treeNodeMoving.Tag as ReferencedFileSave);
                    GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog(viewModel);
                }
                else if (targetNode.IsNamedObjectNode() &&
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

                        if (areNamedTheSame)
                        {
                            response.Fail($"The file {referencedFileSave.GetInstanceName()} has the same name as the target screen. it will not be added since this is not allowed.");
                        }

                        if (response.Succeeded)
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
                            else if (newlyCreatedFile != null)
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

            if (!string.IsNullOrEmpty(response.Message))
            {
                MessageBox.Show(response.Message);
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
                    GlueCommands.Self.GluxCommands.AddReferencedFileToGlobalContent(referencedFileSave.Name, useFullPathAsName);

                    GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

                    GlueCommands.Self.ProjectCommands.SaveProjects();
                    GluxCommands.Self.SaveGlux();
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
                var projectBase = ProjectManager.ProjectBase;
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
                    EditorLogic.CurrentElementTreeNode.RefreshTreeNodes();
                }
                else
                {
                    ElementViewWindow.UpdateGlobalContentTreeNodes(false);
                }


                // 6 Save everything
                GluxCommands.Self.SaveGlux();
                GlueCommands.Self.ProjectCommands.SaveProjects();
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
                    if (namedObject.Instantiate == false)
                    {
                        // If an entire object is dropped on this, it's likely that the user wants to create (aka instantiate)
                        // the object using the entire file. The user may not realize that the object is set to not initialize,
                        // so let's ask them and offer to set Initialize to true
                        string message = $"The object {namedObject.InstanceName} has its 'Instantiate' variable set to 'false'. " +
                            $"This needs to be set to 'true' for the object to be created from the file. Set it to true?";

                        var setToTrueResponse = MessageBox.Show(message,
                            "Set Instantiate to true?",
                            MessageBoxButtons.YesNo);

                        if (setToTrueResponse == DialogResult.Yes)
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

    }
}
