using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Controls;
using Glue;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.ViewModels;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class NamedObjectSaveExtensionMethodsGlue
    {
        static NamedObjectSaveExtensionMethodsGlue()
        {

        }

        public static void ShowCreateNamedObjectUi()
        {
            // add named object, add object, addnamedobject, add new object, addnewobject, createobject, addobject

            var addObjectViewModel = CreateAndShowAddNamedObjectWindow();

            if (addObjectViewModel.DialogResult == DialogResult.OK)
            {
                string whyItIsntValid = null;
                bool isValid = NameVerifier.IsNamedObjectNameValid(addObjectViewModel.ObjectName, out whyItIsntValid);

                if (isValid)
                {
                    if (addObjectViewModel.SourceType == SourceType.Entity && !RecursionManager.Self.CanContainInstanceOf(GlueState.Self.CurrentElement, addObjectViewModel.SourceClassType))
                    {
                        isValid = false;
                        whyItIsntValid = "This type would result in infinite recursion";
                    }
                }

                if (isValid)
                {
                    GlueCommands.Self.GluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                }
                else
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
            }

        }

        private static AddObjectViewModel CreateAndShowAddNamedObjectWindow()
        {
            AddObjectViewModel addObjectViewModel = new AddObjectViewModel();

            TextInputWindow tiw = new TextInputWindow();
            tiw.DisplayText = "Enter the new object's name";
            tiw.Text = "New Object";
            bool isTypePredetermined =  EditorLogic.CurrentNamedObject != null && EditorLogic.CurrentNamedObject.IsList;

            NewObjectTypeSelectionControl typeSelectControl = null;
            if (!isTypePredetermined)
            {
                tiw.Width = 400;


                typeSelectControl = new NewObjectTypeSelectionControl();
                typeSelectControl.Width = tiw.Width - 22;

                typeSelectControl.AfterStrongSelect += delegate
                {
                    tiw.ClickOk();
                };

                typeSelectControl.AfterSelect += delegate(object sender, EventArgs args)
                {
                    string result = tiw.Result;

                    bool isDefault = string.IsNullOrEmpty(result);

                    // Victor Chelaru November 3, 2012
                    // I don't know if we want to only re-assign when default.
                    // The downside is that the user may have already entered a
                    // name, an then changed the type.  This would result in the
                    // user-entered name being overwritten.  However, if we don't
                    // change the name, then an old name that the user entered which
                    // is specific to the type may not get reset.  I'm leaning towards
                    // always changing the name to help prevent misnaming, and it's also
                    // less programatically complex.
                    //if (isDefault)
                    {
                        string newName;

                        if(!string.IsNullOrEmpty(typeSelectControl.SourceFile) && !string.IsNullOrEmpty(typeSelectControl.SourceName))
                        {
                            newName = HandleObjectInFileSelected(typeSelectControl);
                        }
                        else if (string.IsNullOrEmpty(typeSelectControl.SourceClassType))
                        {
                            newName = "ObjectInstance";

                        }
                        else
                        {

                            string textToAssign = typeSelectControl.SourceClassType + "Instance";
                            if (textToAssign.Contains("/") || textToAssign.Contains("\\"))
                            {
                                textToAssign = FileManager.RemovePath(textToAssign);
                            }

                            newName = textToAssign.Replace("<T>", "");
                        }

                        // We need to make sure this is a unique name.
                        newName = StringFunctions.MakeStringUnique(newName, EditorLogic.CurrentElement.AllNamedObjects);
                        tiw.Result = newName;
                    }
                };
                tiw.AddControl(typeSelectControl, AboveOrBelow.Above);
            }

            addObjectViewModel.DialogResult = tiw.ShowDialog();

            addObjectViewModel.SourceType = SourceType.FlatRedBallType;
            addObjectViewModel.SourceClassType = null;
            addObjectViewModel.SourceFile = null;
            addObjectViewModel.SourceNameInFile = null;
            addObjectViewModel.SourceClassGenericType = null;
            addObjectViewModel.ObjectName = tiw.Result;

            if(isTypePredetermined)
            {
                var parentList = GlueState.Self.CurrentNamedObjectSave;

                var genericType = parentList.SourceClassGenericType;

                if(!string.IsNullOrEmpty(genericType))
                {
                    addObjectViewModel.SourceClassType = genericType;
                    
                    // the generic type will be fully qualified (like FlatRedBall.Sprite)
                    // but object types for FRB primitives are not qualified, so we need to remove
                    // any dots

                    if(addObjectViewModel.SourceClassType.Contains("."))
                    {
                        int lastDot = addObjectViewModel.SourceClassType.LastIndexOf('.');

                        addObjectViewModel.SourceClassType = addObjectViewModel.SourceClassType.Substring(lastDot + 1);
                    }

                    if(ObjectFinder.Self.GetEntitySave(genericType) != null)
                    {
                        addObjectViewModel.SourceType = SourceType.Entity;
                    }
                    else
                    {
                        addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                    }
                }
            }

            if (typeSelectControl != null)
            {
                if (!string.IsNullOrEmpty(typeSelectControl.SourceClassType) || typeSelectControl.SourceType == SourceType.File)
                {
                    addObjectViewModel.SourceType = typeSelectControl.SourceType;
                }
                addObjectViewModel.SourceFile = typeSelectControl.SourceFile;
                addObjectViewModel.SourceNameInFile = typeSelectControl.SourceName;

                addObjectViewModel.SourceClassType = typeSelectControl.SourceClassType;
                addObjectViewModel.SourceClassGenericType = typeSelectControl.SourceClassGenericType;
            }

            return addObjectViewModel;
        }

        private static string HandleObjectInFileSelected(NewObjectTypeSelectionControl typeSelectControl)
        {
            string newName;
            var spaceParen = typeSelectControl.SourceName.IndexOf(" (");

            if (spaceParen != -1)
            {
                newName = typeSelectControl.SourceName.Substring(0, spaceParen);
            }
            else
            {
                newName = typeSelectControl.SourceName;
            }

            // If the user selected "Entire File" we want to make sure the space doesn't show up:
            newName = newName.Replace(" ", "");

            string throwaway;
            bool isInvalid = NameVerifier.IsNamedObjectNameValid(newName, out throwaway);

            if (!isInvalid)
            {
                // let's get the type:
                var split = typeSelectControl.SourceName.Split('(', ')');

                var last = split.LastOrDefault(item=>!string.IsNullOrEmpty(item));

                if(last != null)
                {
                    var lastDot = last.LastIndexOf('.');

                    newName = last.Substring(lastDot + 1, last.Length - (lastDot + 1));
                }
            }

            return newName;
        }

        public static MembershipInfo GetMemberMembershipInfo(string memberName)
        {
            if (EditorLogic.CurrentScreenSave != null)
            {
                if (EditorLogic.CurrentScreenSave.HasMemberWithName(memberName))
                {
                    return MembershipInfo.ContainedInThis;
                }
            }
            else if (EditorLogic.CurrentEntitySave != null)
            {
                return EditorLogic.CurrentEntitySave.GetMemberMembershipInfo(memberName);
            }

            return MembershipInfo.NotContained;
        }

        public static NamedObjectSave AddNewNamedObjectToSelectedElement(string objectName, MembershipInfo membershipInfo, bool raisePluginResponse = true)
        {
            NamedObjectSave namedObject = new NamedObjectSave();
            namedObject.InstanceName = objectName;

            namedObject.DefinedByBase = membershipInfo == MembershipInfo.ContainedInBase;

            #region Adding to a NamedObject (PositionedObjectList)

            if (EditorLogic.CurrentNamedObject != null)
            {
                AddNamedObjectToCurrentNamedObjectList(namedObject);

            }
            #endregion

            #region else adding to Screen

            else if (EditorLogic.CurrentScreenTreeNode != null)
            {
                ScreenTreeNode screenTreeNode =
                    EditorLogic.CurrentScreenTreeNode;
                AddNewNamedObjectToElementTreeNode(screenTreeNode, namedObject, true);
            }

            #endregion

            #region else adding to an Entity
            else if (EditorLogic.CurrentEntityTreeNode != null)
            {
                EntityTreeNode entityTreeNode =
                    EditorLogic.CurrentEntityTreeNode;

                AddNewNamedObjectToElementTreeNode(entityTreeNode, namedObject, true);
            }
            #endregion

            
            if (raisePluginResponse)
            {
                PluginManager.ReactToNewObject(namedObject);
            }
            MainGlueWindow.Self.PropertyGrid.Refresh();
            ElementViewWindow.GenerateSelectedElementCode();
            GluxCommands.Self.SaveGlux();

            return namedObject;
        }

        public static void AddNamedObjectToCurrentNamedObjectList(NamedObjectSave namedObject)
        {
            namedObject.AddToManagers = true;

            NamedObjectSave parentNamedObject = EditorLogic.CurrentNamedObject;

            parentNamedObject.ContainedObjects.Add(namedObject);

            EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();

            // Since it's part of a list we know its type
            string typeOfNewObject = parentNamedObject.SourceClassGenericType;

            if (ObjectFinder.Self.GetEntitySave(typeOfNewObject) != null)
            {
                namedObject.SourceType = SourceType.Entity;

                namedObject.SourceClassType = typeOfNewObject;
                namedObject.UpdateCustomProperties();
            }
            else if (AvailableClassTypeConverter.IsFlatRedBallType(typeOfNewObject))
            {
                namedObject.SourceType = SourceType.FlatRedBallType;
                namedObject.SourceClassType = typeOfNewObject;
                namedObject.UpdateCustomProperties();
            }

            // Highlight the newly created object
            TreeNode newNode = GlueState.Self.Find.NamedObjectTreeNode(namedObject);

            if (newNode != null)
            {
                MainGlueWindow.Self.ElementTreeView.SelectedNode = newNode;
            }
        }

        internal static void AddNewNamedObjectToElementTreeNode(BaseElementTreeNode elementTreeNode, NamedObjectSave namedObject, bool modifyNamedObject)
        {
            // We no longer need to modify new named objects this way
            // AttachToContainer defaults to true and won't do anything
            // on Screens.  It looks like AddToManagers was always true.
            //if (modifyNamedObject)
            //{
            //    if (elementTreeNode.SaveObjectAsElement is EntitySave)
            //    {
            //        namedObject.AddToManagers = !(elementTreeNode.SaveObjectAsElement as EntitySave).IsUnique;

            //        namedObject.AddToManagers = true;
            //    }
            //    else
            //    {
            //        // Vic says - when a file is loaded in a Screen, 
            //        // it is added to managers.  When it is loaded in
            //        // Entities, it is not and components of it are cloned
            //        // Therefore, if we're in a Screen, we should assume that
            //        // we are going to load from a file for this new object and
            //        // not set the AddToManagers to true.  But IF the new object
            //        // is going to be an Entity, then the PropetyGrid will handle
            //        // setting its AddToManagers to true.
            //    }
            //}

            elementTreeNode.SaveObjectAsElement.NamedObjects.Add(namedObject);

            elementTreeNode.UpdateReferencedTreeNodes();

            CodeWriter.GenerateCode(elementTreeNode.SaveObjectAsElement);

            // Highlight the newly created object
            TreeNode treeNode = EditorLogic.CurrentElementTreeNode.GetTreeNodeFor(namedObject);
            if (treeNode != null)
            {
                MainGlueWindow.Self.ElementTreeView.SelectedNode = treeNode;
            }
        }



    }
}
