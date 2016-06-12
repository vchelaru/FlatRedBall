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
