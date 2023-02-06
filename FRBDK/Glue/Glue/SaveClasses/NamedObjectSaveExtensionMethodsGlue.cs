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
            if (GlueState.Self.CurrentScreenSave != null)
            {
                if (GlueState.Self.CurrentScreenSave.HasMemberWithName(memberName))
                {
                    return MembershipInfo.ContainedInThis;
                }
            }
            else if (GlueState.Self.CurrentEntitySave != null)
            {
                return GlueState.Self.CurrentEntitySave.GetMemberMembershipInfo(memberName);
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


            // Since it's part of a list we know its type
            string typeOfNewObject = namedObjectList.SourceClassGenericType;

            if (ObjectFinder.Self.GetEntitySave(typeOfNewObject) != null)
            {
                namedObject.SourceType = SourceType.Entity;
                if(string.IsNullOrEmpty( namedObject.SourceClassType))
                {
                    namedObject.SourceClassType = typeOfNewObject;
                }
                namedObject.UpdateCustomProperties();
            }
            else if (AvailableClassTypeConverter.IsFlatRedBallType(typeOfNewObject) ||
                namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle ||
                namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.CapsulePolygon ||
                namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Circle ||
                namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Line ||
                namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Polygon
                )
            {
                namedObject.SourceType = SourceType.FlatRedBallType;
                //namedObject.SourceClassType = typeOfNewObject;
                namedObject.UpdateCustomProperties();
            }
        }
    }
}
