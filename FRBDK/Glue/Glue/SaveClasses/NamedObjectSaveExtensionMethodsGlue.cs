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
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin;

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

        public static void AddNamedObjectToList(NamedObjectSave namedObject, NamedObjectSave namedObjectList)
        {
            namedObject.AddToManagers = true;


            if (namedObjectList == null)
            {
                throw new InvalidOperationException("No object is currently selected");
            }

            namedObjectList.ContainedObjects.Add(namedObject);

            EditorLogic.CurrentElementTreeNode?.RefreshTreeNodes();

            // Since it's part of a list we know its type
            string typeOfNewObject = namedObjectList.SourceClassGenericType;

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
        }
    }
}
