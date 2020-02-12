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



    }
}
