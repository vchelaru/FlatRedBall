using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using Glue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Managers
{
    public class DragDropManager : Singleton<DragDropManager>
    {
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
                    string targetClassType = targetNos.SourceClassType;

                    #region Failure cases

                    if (string.IsNullOrEmpty(targetClassType))
                    {
                        MessageBox.Show("The target Object does not have a defined type.  This operation is not valid");
                    }

                    #endregion

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

                    else if(targetClassType == "ShapeCollection")
                    {
                        succeeded = HandleDropOnShapeCollection(treeNodeMoving, targetNode, targetNos, movingNos);
                    }

                    else if (targetClassType == "PositionedObjectList<T>")
                    {
                        succeeded = HandleDropOnList(treeNodeMoving, targetNode, targetNos, movingNos);
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
                RightClickHelper.AddVariableClick(CustomVariableType.Tunneled, ((NamedObjectSave)treeNodeMoving.Tag).InstanceName);
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

        private bool HandleDropOnShapeCollection(TreeNode treeNodeMoving, TreeNode targetNode, NamedObjectSave targetNos, NamedObjectSave movingNos)
        {
            bool succeeded = true;

            if(movingNos.CanBeInShapeCollection() == false)
            {
                MessageBox.Show("The Object you are moving is of type " + movingNos.SourceClassType +
                    " which cannot be contained in a ShapeCollection");
            }
            else
            {
                succeeded = true;
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
            return succeeded;
        }

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

        private TreeNode MoveEntityOntoElement(EntityTreeNode treeNodeMoving, TreeNode targetNode, TreeNode newTreeNode)
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

        public NamedObjectSave CreateNewNamedObjectInElement(IElement elementToCreateIn, EntitySave blueprintEntity, bool createList = false)
        {
            if (blueprintEntity == null)
            {
                throw new ArgumentNullException($"{nameof(blueprintEntity)} cannot be null");
            }

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
                        ElementCommands.Self.AddReferencedFileToGlobalContent(rfs.Name, useFullPathAsName);
                    }
                }


                GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

                ProjectManager.SaveProjects();
                GluxCommands.Self.SaveGlux();
            }
        }


    }
}
