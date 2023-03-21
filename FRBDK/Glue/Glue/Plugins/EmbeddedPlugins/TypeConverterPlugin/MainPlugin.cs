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

        private TypeConverter HandleGetTypeConverter(IElement containerAsIElement, NamedObjectSave instance, Type memberType, string memberName, string customType)
        {
            if(memberType == null)
            {
                throw new ArgumentNullException(nameof(memberType));
            }

            if(instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            GlueElement container = containerAsIElement as GlueElement;
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
                else if (memberType?.Name == "Sprite")
                {
                    var nosTypeConverter = new AvailableNamedObjectsAndFiles(container);
                    nosTypeConverter.NamedObjectTypeRestriction = "FlatRedBall.Sprite";
                    typeConverter = nosTypeConverter;
                    handled = true;
                }

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
                    availableNamedObjectsAndFiles.NamedObjectTypeRestriction = memberType.FullName;
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
                else if (IsTypeFile(memberType, customType))
                {
                    AvailableFileStringConverter availableFileStringConverter = new AvailableFileStringConverter(container);
                    availableFileStringConverter.QualifiedRuntimeTypeNameFilter = memberType.FullName;
                    if (!string.IsNullOrEmpty(customType))
                    {
                        availableFileStringConverter.QualifiedRuntimeTypeNameFilter = customType;
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
                            var baseVariable = ObjectFinder.Self.GetBaseCustomVariable(customVariable);
                            typeConverter = baseVariable.GetTypeConverter(entity);
                        }
                    }
                }
            }
            //else if (this.SourceType == SaveClasses.SourceType.FlatRedBallType &&
            //    typedMember != null && typedMember.MemberType != null)
            //{

            //}

            // November 25, 2021
            // Vic asks = what does this code do? Nothing...is it a bug?
            if (wasTypeModified)
            {
                memberType = oldType;
            }

            return typeConverter;
        }

        private static bool IsTypeFile(Type memberType, string customTypeName)
        {
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

                if(!string.IsNullOrEmpty(customTypeName) && 
                    ati.QualifiedRuntimeTypeName.QualifiedType == customTypeName)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
