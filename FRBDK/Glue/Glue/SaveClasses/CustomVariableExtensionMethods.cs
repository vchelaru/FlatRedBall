using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using System.ComponentModel;
using System.Drawing;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using Microsoft.Xna.Framework;
using FlatRedBall.Glue.Plugins;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace FlatRedBall.Glue.SaveClasses
{
    public static partial class CustomVariableExtensionMethods
    {
        static IGlueState GlueState => EditorObjects.IoC.Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => EditorObjects.IoC.Container.Get<IGlueCommands>();


        public static bool GetIsCsv(this CustomVariable customVariable)
        {
            if (customVariable.Type == null)
            {
                return false;
            }
            if (customVariable.Type.EndsWith(".csv"))
            {
                return true;
            }
            else if (customVariable.Type.EndsWith(".txt"))
            {
                //ReferencedFileSave rfs = ObjectFinder.Self.GetReferencedFileSaveFromFile(customVariable.Type);
                throw new NotImplementedException("Need to implement checking if a custom variable is CSV from Text");

            }
            else if (GlueState.GetAllReferencedFiles().Any(item =>
                    item.IsCsvOrTreatedAsCsv && item.GetTypeForCsvFile() == customVariable.Type))
            {
                return true;
            }
            return false;
        }

        public static bool GetIsListCsv(this CustomVariable customVariable)
        {
            if (customVariable.GetIsCsv())
            {
                string fullFileName = GlueState.ContentDirectory + customVariable.Type;
                ReferencedFileSave foundRfs = GlueCommands.FileCommands.GetReferencedFile(fullFileName);

                if (foundRfs != null)
                {
                    return foundRfs.CreatesDictionary == false;
                }
            }

            return false;
        }

        // GetIsVariableState, GetIsVariableStateAndCategory, GetIsBaseElementType, GetEntityNameDefiningThisTypeCategory,
        // SetDefaultValueAccordingToType and GetDefaultValueAccordingToType moved to
        // GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        public static void FixAllTypes(this CustomVariable customVariable)
        {
            customVariable.FixEnumerationTypes();

            var type = customVariable.OverridingPropertyType;
            if(string.IsNullOrEmpty(type))
            {
                type = customVariable.Type;
            }
            if (!string.IsNullOrEmpty(type) && customVariable.DefaultValue != null)
            {
                object variableValue = customVariable.DefaultValue;
                variableValue = CustomVariableCommonExtensions.FixValue(variableValue, type);
                customVariable.DefaultValue = variableValue;
            }

            if(!string.IsNullOrEmpty( customVariable.VariableDefinition?.PreferredDisplayerName))
            {
                // Since variable displayers can be handled by plugins, then the plugin must also handle converting the name to type
                // since the type is not necessarily known here.:
                PluginManager.TryAssignPreferredDisplayerFromName(customVariable);
            }
        }


        // FixValue moved to GlueCommon.SaveClasses.CustomVariableCommonExtensions (#2276) - pure
        // type-conversion logic, zero coupling.

        // FixEnumerationTypes, ConvertEnumerationValuesToInts, GetIsEnumeration, GetIsAnimationChain, GetIsFile,
        // GetIsObjectType and GetRuntimeType moved to GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        // GetDefiningCustomVariable moved to GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        public static bool GetIsExposingVariable(this CustomVariable customVariable, IElement container)
        {
            bool isExposedExistingMember = false;

            if (container is EntitySave)
            {
                isExposedExistingMember =
                    ExposedVariableManager.IsMemberDefinedByEntity(customVariable.Name, container as EntitySave);
            }
            else if (container is ScreenSave)
            {
                isExposedExistingMember = customVariable.Name == "CurrentState";
            }

            return isExposedExistingMember;
        }

        // GetIsTunneling moved to GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        public static bool GetIsNewVariable(this CustomVariable customVariable, IElement container)
        {
            return customVariable.GetIsTunneling() == false && customVariable.GetIsExposingVariable(container) == false;
        }

        // CustomVariableToString moved to GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        public static bool HasAccompanyingVelocityConsideringTunneling(this CustomVariable variable, IElement container, int maxDepth = 0)
        {
            if (variable.HasAccompanyingVelocityProperty)
            {
                return true;
            }
            else if (!string.IsNullOrEmpty(variable.SourceObject) && !string.IsNullOrEmpty(variable.SourceObjectProperty) && maxDepth > 0)
            {
                NamedObjectSave nos = container.GetNamedObjectRecursively(variable.SourceObject);

                if (nos != null)
                {
                    // If it's a FRB 
                    if (nos.SourceType == SourceType.FlatRedBallType || nos.SourceType == SourceType.File)
                    {
                        return !string.IsNullOrEmpty(InstructionManager.GetVelocityForState(variable.SourceObjectProperty));
                    }
                    else if(nos.SourceType == SourceType.Entity)
                    {
                        EntitySave entity = GlueState.CurrentGlueProject.GetEntitySave(nos.SourceClassType);

                        if (entity != null)
                        {
                            CustomVariable variableInEntity = entity.GetCustomVariable(variable.SourceObjectProperty);

                            if (variableInEntity != null)
                            {
                                if (!string.IsNullOrEmpty(InstructionManager.GetVelocityForState(variableInEntity.Name)))
                                {
                                    return true;
                                }
                                else
                                {

                                    return variableInEntity.HasAccompanyingVelocityConsideringTunneling(entity, maxDepth - 1);
                                }
                            }
                            else
                            {
                                // There's no variable for this, so let's see if it's a variable that has velocity in FRB
                                return !string.IsNullOrEmpty(InstructionManager.GetVelocityForState(variable.SourceObjectProperty));

                            }
                        }
                    }
                }
            }
            return false;

        }

        public static bool GetIsSourceFile(this CustomVariable customVariable, IElement container)
        {
            NamedObjectSave referencedNos = null;
            if(!string.IsNullOrEmpty(customVariable.SourceObject) )
            {
                referencedNos = container.GetNamedObjectRecursively(customVariable.SourceObject);
            }

            return referencedNos != null && customVariable.SourceObjectProperty == "SourceFile" && referencedNos.SourceType == SourceType.FlatRedBallType;


        }
    }
    

}
