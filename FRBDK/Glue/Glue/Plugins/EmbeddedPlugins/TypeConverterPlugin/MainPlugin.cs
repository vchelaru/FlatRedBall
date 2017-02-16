using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.TypeConverterPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.GetTypeConverter += HandleGetTypeConverter;
        }

        private TypeConverter HandleGetTypeConverter(IElement container, NamedObjectSave instance, TypedMemberBase typedMember)
        {
            Type memberType = typedMember.MemberType;
            string memberName = typedMember.MemberName;

            TypeConverter typeConverter = null;

            // If the NOS references a FRB type, we need to adjust the type appropriately

            bool wasTypeModified = false;
            Type oldType = memberType;

            bool handled = false;






            if (instance.SourceType == SourceType.FlatRedBallType)
            {
                Type type = TypeManager.GetTypeFromString(instance.SourceClassType);

                if (type == typeof(Sprite) && memberName == "CurrentChainName")
                {
                    // special case handling for CurrentChainName
                    typeConverter = new AvailableAnimationChainsStringConverter(container, instance);

                    handled = true;
                }

                // Victor Chelaru
                // October 26, 2015:
                // I don't think we need this anymore - we should just let the CSVs do their job
                // defining variables and not rely on reflection. That way plugins will be able to
                // handle everything without relying on reflection
                //if (!handled && type != null)
                //{
                //    FieldInfo fieldInfo = type.GetField(memberName);
                //    if (fieldInfo != null)
                //    {
                //        memberType = fieldInfo.FieldType;
                //        wasTypeModified = true;
                //    }

                //    if (wasTypeModified == false)
                //    {
                //        PropertyInfo propertyInfo = type.GetProperty(memberName);
                //        if (propertyInfo != null)
                //        {
                //            memberType = propertyInfo.PropertyType;
                //            wasTypeModified = true;
                //        }

                //    }
                //}
            }
            else if (instance.SourceType == SourceType.File)
            {
                if (instance.ClassType == "Sprite" && memberName == "CurrentChainName")
                {
                    // special case handling for CurrentChainName
                    typeConverter = new AvailableAnimationChainsStringConverter(container, instance);

                    handled = true;
                }

            }

            if (!handled)
            {
                if (instance.DoesMemberNeedToBeSetByContainer(memberName))
                {
                    var availableNamedObjectsAndFiles = new AvailableNamedObjectsAndFiles(container);
                    availableNamedObjectsAndFiles.NamedObjectTypeRestriction = typedMember.MemberType.FullName;
                    typeConverter = availableNamedObjectsAndFiles;
                }
                else if (memberType.IsEnum)
                {
                    typeConverter = new EnumConverter(memberType);
                }
                else if (memberType == typeof(Microsoft.Xna.Framework.Color))
                {
                    typeConverter = new AvailableColorTypeConverter();
                    memberType = typeof(string);
                }
                else if (IsTypeFile(typedMember))
                {
                    AvailableFileStringConverter availableFileStringConverter = new AvailableFileStringConverter(container);
                    availableFileStringConverter.QualifiedRuntimeTypeName = memberType.FullName;
                    if(!string.IsNullOrEmpty( typedMember.CustomTypeName ))
                    {
                        availableFileStringConverter.QualifiedRuntimeTypeName = typedMember.CustomTypeName;
                    }
                    availableFileStringConverter.RemovePathAndExtension = true;
                    typeConverter = availableFileStringConverter;

                    memberType = typeof(string);
                }
                else if (instance.SourceType == SourceType.Entity && !string.IsNullOrEmpty(instance.SourceClassType))
                {

                    EntitySave entity = ObjectFinder.Self.GetEntitySave(instance.SourceClassType);

                    if (entity != null)
                    {
                        CustomVariable customVariable = entity.GetCustomVariable(memberName);

                        if (customVariable != null)
                        {
                            typeConverter = customVariable.GetTypeConverter(entity);
                        }
                    }
                }
            }
            //else if (this.SourceType == SaveClasses.SourceType.FlatRedBallType &&
            //    typedMember != null && typedMember.MemberType != null)
            //{

            //}

            if (wasTypeModified)
            {
                memberType = oldType;
            }

            return typeConverter;

        }


        private static bool IsTypeFile(TypedMemberBase typedMember)
        {
            var memberType = typedMember.MemberType;

            // old (hardcoded) code:
            //return memberType == typeof(Microsoft.Xna.Framework.Graphics.Texture2D) ||
            //        memberType == typeof(FlatRedBall.Graphics.BitmapFont) ||
            //        memberType == typeof(FlatRedBall.Graphics.Animation.AnimationChainList) ||
            //        memberType == typeof(Scene);
            // But we could just check for extensions in all of the ATIs

            //We can have types that go to strings, and we dont' want a drop-down for all strings:
            if (memberType == typeof(string))
            {
                return false;
            }

            foreach (var ati in AvailableAssetTypes.Self.AllAssetTypes.Where(item => !string.IsNullOrEmpty(item.Extension)))
            {
                if (ati.QualifiedRuntimeTypeName.QualifiedType == memberType.FullName || ati.FriendlyName == memberType.Name)
                {
                    return true;
                }

                if(!string.IsNullOrEmpty(typedMember.CustomTypeName) && 
                    ati.QualifiedRuntimeTypeName.QualifiedType == typedMember.CustomTypeName)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
