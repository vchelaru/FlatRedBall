using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using System.Reflection;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using Microsoft.Xna.Framework;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.SaveClasses.Helpers
{
    public enum InterpolationCharacteristic
    {
        CanInterpolate,
        NeedsVelocityVariable,
        CantInterpolate
    }

    public static class CustomVariableHelper
    {
        public static void SetDefaultValueFor(CustomVariable customVariable, GlueElement element)
        {
            object defaultValue = GetDefaultValueFor(customVariable, element);
            if(defaultValue != null)
            {
                customVariable.DefaultValue = defaultValue;
            }
        }

        private static object GetDefaultValueFor(CustomVariable customVariable, GlueElement element)
        {
            object valueToReturn = null;

            // Lets handle all file types here:
            //if (customVariable.Type == "Texture2D")
            if(customVariable.GetIsFile())
            {
                // If we don't return 
                // null then this will
                // return the name of the
                // file (like "redball.bmp").
                // This will screw up code generation
                // because texture variables can only be
                // set to Texture2D instances, not to the
                // name of the image.
                valueToReturn = null;
            }
            else
            {



                try
                {
                    if (!string.IsNullOrEmpty(customVariable.SourceObject) && !string.IsNullOrEmpty(customVariable.SourceObjectProperty))
                    {
                        NamedObjectSave nos = element.GetNamedObjectRecursively(customVariable.SourceObject);

                        if (nos != null)
                        {
                            object defaultValue = GetDefaultValueFor(nos, customVariable.SourceObjectProperty, customVariable.OverridingPropertyType);

                            valueToReturn = defaultValue;
                        }
                    }
                    else if (customVariable.Name == "Visible" && element is EntitySave && ((EntitySave)element).ImplementsIVisible)
                    {
                        // This shouldn't be true, we don't want to set the value
                        // because we don't want to kick off events
                        //valueToReturn = true;
                        valueToReturn = null;
                    }
                    else if (customVariable.Name == "Enabled" && element is EntitySave && ((EntitySave)element).ImplementsIWindow)
                    {
                        // This shouldn't be true, we don't want to set the value
                        // because we don't want to kick off events
                        //valueToReturn = true;
                        valueToReturn = null;
                    }
                    else
                    {

                        valueToReturn = GetDefaultValueForPropertyInType(customVariable.Name, "PositionedObject");
                    }
                }
                catch
                {
                    // do nothing.
                }

                if (valueToReturn is Microsoft.Xna.Framework.Color)
                {
                    string standardName =
                        AvailableColorTypeConverter.GetStandardColorNameFrom((Color)valueToReturn);

                    if (string.IsNullOrEmpty(standardName))
                    {
                        return "Red";
                    }
                    else
                    {
                        return standardName;
                    }
                }
            }
            return valueToReturn;
        }

        private static object GetDefaultValueFor(NamedObjectSave nos, string property, string overridingType)
        {
            if (!string.IsNullOrEmpty(overridingType))
            {


                Type overridingTypeAsType = TypeManager.GetTypeFromString(overridingType);


                string valueAsString = TypeManager.GetDefaultForType(overridingType);

                return PropertyValuePair.ConvertStringToType(valueAsString, overridingTypeAsType);

            }




            switch (nos.SourceType)
            {
                case SourceType.File:

                    if (!string.IsNullOrEmpty(nos.SourceFile) && !string.IsNullOrEmpty(nos.SourceNameWithoutParenthesis))
                    {
                        string absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(nos.SourceFile, true);



                        return ContentParser.GetValueForProperty(absoluteFileName, nos.SourceNameWithoutParenthesis, property);

                    }



                    break;

                case SourceType.Entity:
                    if (!string.IsNullOrEmpty(nos.SourceClassType))
                    {
                        var element = ObjectFinder.Self.GetElement(nos.SourceClassType);

                        if (element != null)
                        {
                            CustomVariable customVariable = element.GetCustomVariable(property);

                            if (customVariable != null)
                            {
                                return GetDefaultValueFor(customVariable, element);
                            }
                            else if (property == "Visible" && element is EntitySave &&
                                ((EntitySave)element).ImplementsIVisible)
                            {
                                return true;
                            }
                            else if (property == "Enabled" && element is EntitySave &&
                                ((EntitySave)element).ImplementsIWindow)
                            {
                                return true;
                            }
                        }

                    }
                    break;
                case SourceType.FlatRedBallType:
                    if (!string.IsNullOrEmpty(nos.SourceClassType))
                    {
                        // See if there is a variable set for this already - there may be
                        // now that we allow users to specify values right on FlatRedBall-type
                        // NamedObjectSaves
                        InstructionSave instructionSave = nos.GetCustomVariable(property);
                        if (instructionSave != null && instructionSave.Value != null)
                        {
                            return instructionSave.Value;
                        }
                        else
                        {

                            object value = GetExceptionForFlatRedBallTypeDefaultValue(nos, property);

                            if (value != null)
                            {
                                return value;
                            }
                            else
                            {
                                string classType = nos.SourceClassType;

                                value = GetDefaultValueForPropertyInType(property, classType);

                                return value;
                            }
                        }
                    }

                    break;

            }

            return null;
        }

        private static object GetDefaultValueForPropertyInType(string property, string classType)
        {
            object value = null;

            // Instantiate this to get a type
            Type type = TypeManager.GetTypeFromString(classType);
            object newObject = Activator.CreateInstance(type);

            PropertyInfo[] properties = type.GetProperties();
            bool found = false;

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.Name == property)
                {
                    value = propertyInfo.GetValue(newObject, null);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                FieldInfo[] fields = type.GetFields();
                foreach (FieldInfo fieldInfo in fields)
                {
                    if (fieldInfo.Name == property)
                    {
                        value = fieldInfo.GetValue(newObject);
                    }
                }
            }
            return value;
        }

        private static object GetExceptionForFlatRedBallTypeDefaultValue(NamedObjectSave nos, string property)
        {
            if (property == "Visible" || property == "Enabled")
            {
                return true;
            }

            return null;
        }

        public static InterpolationCharacteristic GetInterpolationCharacteristic(CustomVariable customVariable, GlueElement container)
        {
            var nosReferencing = ObjectFinder.Self.GetNamedObjectFor(customVariable);

            if(nosReferencing?.SourceType == SourceType.Gum)
            {
                var ati = nosReferencing.GetAssetTypeInfo();

                var variable = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == customVariable.SourceObjectProperty);

                // This is a quick test to see if it's a state. States generate nullable types (as of Feb 2, 2022)
                if(variable?.Type.EndsWith("?") == true)
                {
                    // this is a state so don't let interpolation happen. Technically we could, but this would require a lot of plugin work, so let's just mark it
                    // as CantInterpolate to prevent errors
                    return InterpolationCharacteristic.CantInterpolate;
                }
            }

            string variableType = null;
            if (customVariable != null)
            {
                if (!string.IsNullOrEmpty(customVariable.OverridingPropertyType))
                {
                    variableType = customVariable.OverridingPropertyType;
                }
                else
                {
                    variableType = customVariable.Type;
                }
            }


            if (customVariable != null && customVariable.GetIsVariableState(container))
            {
                return InterpolationCharacteristic.CanInterpolate;
            }

            if (customVariable == null ||
                variableType == null ||
                variableType == "string" ||
                variableType == "bool" ||
                variableType == "Color" ||
                customVariable.GetIsFile() ||
                customVariable.GetIsCsv() ||
                customVariable.GetIsBaseElementType() ||
                (customVariable.GetRuntimeType() != null && customVariable.GetRuntimeType().IsEnum)
                )
            {
                return InterpolationCharacteristic.CantInterpolate;
            }

            string velocityMember = null;

            if(!string.IsNullOrEmpty(customVariable.SourceObject))
            {
                velocityMember = FlatRedBall.Instructions.InstructionManager.GetVelocityForState(customVariable.SourceObjectProperty);
            }
            else
            {
                velocityMember = FlatRedBall.Instructions.InstructionManager.GetVelocityForState(customVariable.Name);

            }
            if (!string.IsNullOrEmpty(velocityMember))
            {
                // There's a velocity variable for this, but we need to make sure
                // it's actually available
                var exposableMembers = ExposedVariableManager.GetExposableMembersFor(container, false);

                if (exposableMembers.Any(item=>item.Member ==velocityMember))
                {
                    return InterpolationCharacteristic.CanInterpolate;
                }
            }            
            
            // December 26, 2013
            // This used to not pass
            // a value for maxDepth which
            // means a maxDepth of 0.  Not
            // sure why, but we do want to look
            // at tunneling at any depth.
            int maxDepth = int.MaxValue;
            if(customVariable.HasAccompanyingVelocityConsideringTunneling(container, maxDepth) )
            {
                return InterpolationCharacteristic.CanInterpolate;
            }

            else
            {
                return InterpolationCharacteristic.NeedsVelocityVariable;
            }


        }

        public static bool IsStateMissingFor(CustomVariable customVariable, IElement container)
        {
            // We can't use the CustomVariable's check because
            // the state may not exist.  Therefore we have to look
            // at the properties and determine if it should be associated
            // with a state.
            bool isState = IsCustomVariableReferencingState(customVariable);
            if (isState)
            {
                // This variable is an exposed state, so let's make sure the state exists for it
                StateSaveCategory existingCategory = null;
                if (customVariable.Type == "VariableState")
                {
                    if (container.States.Count != 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    bool returnValue = container.StateCategoryList.FirstOrDefault(
                        category => 
                            category.Name == customVariable.Type) == null;

                    return returnValue;
                }
            }
            else
            {
                return false;
            }

        }

        private static bool IsCustomVariableReferencingState(CustomVariable customVariable)
        {
            bool toReturn = string.IsNullOrEmpty(customVariable.SourceObject) &&
                TypeManager.GetTypeFromString(customVariable.Type) == null &&
                !customVariable.GetIsCsv() &&
                customVariable.Name.StartsWith("Current") &&
                customVariable.Name.EndsWith("State");

            return toReturn;
        }
    }
}
