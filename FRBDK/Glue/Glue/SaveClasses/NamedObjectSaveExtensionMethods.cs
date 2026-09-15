using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using System.Windows.Forms;
using FlatRedBall.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class NamedObjectSaveExtensionMethods
    {
        // UpdateCustomProperties moved to GlueCommon.SaveClasses.NamedObjectSaveCommonExtensions (#2276).

        public static NamedObjectSave Clone(this NamedObjectSave instance)
        {
            // This doesn't work as well in XML due to enum values, so let's use Json instead
            //NamedObjectSave newNamedObjectSave = FileManager.CloneObject(instance);
            var serialized = JsonConvert.SerializeObject(instance);
            var newNamedObjectSave = JsonConvert.DeserializeObject<NamedObjectSave>(serialized);

            newNamedObjectSave.UpdateCustomProperties();
            // March 6, 2012
            // UpdateCustomProperties
            // creates the InstructionSaves
            // for the NamedObjectSave according
            // to the variables for this object; however,
            // an object may have InstructionSaves for variables
            // that aren't part of its type - they may exist because
            // the user has switched from an old type and Glue is holding
            // on to those old values in case the user wants to switch back.
            // Therefore, we shouldn't fill the instruction saves this way, instead
            // let's just have the instruction saves be Added.
            newNamedObjectSave.InstructionSaves = new List<CustomVariableInNamedObject>();

            newNamedObjectSave.ContainedObjects = new List<NamedObjectSave>(instance.ContainedObjects.Count);

            for (int i = 0; i < instance.InstructionSaves.Count; i++)
            {
                // See above on why we use json 
                var instructionSerialized = JsonConvert.SerializeObject(instance.InstructionSaves[i]);

                var duplicateInstruction = JsonConvert.DeserializeObject<CustomVariableInNamedObject>(instructionSerialized);
                    //FileManager.CloneObject(instance.InstructionSaves[i]);

                // Events are instance-specific so we prob don't want to copy those
                duplicateInstruction.EventOnSet = null;

                newNamedObjectSave.InstructionSaves.Add(duplicateInstruction);
            }
            newNamedObjectSave.FixAllTypes();

            foreach (NamedObjectSave containedNamedObject in instance.ContainedObjects)
            {
                newNamedObjectSave.ContainedObjects.Add(containedNamedObject.Clone());
            }

            return newNamedObjectSave;
        }

        // GetAssetTypeInfo moved to GlueCommon.SaveClasses.NamedObjectSaveAssetTypeExtensions (#2276) -
        // needed the new IAvailableAssetTypesCore seam over AvailableAssetTypes.Self.

        public static AssetTypeInfo GetContainedListItemAssetTypeInfo(this NamedObjectSave instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if(instance.IsList == false)
            {
                throw new InvalidOperationException($"The instance {instance?.InstanceName} is not of list type");
            }
            if (string.IsNullOrEmpty(instance.SourceClassGenericType))
            {
                return null;
            }

            return AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(instance.SourceClassGenericType, instance, isObject: true);
        }

        public static void FixAllTypes(this NamedObjectSave instance)
        {
            var ati = instance.GetAssetTypeInfo();
            foreach (CustomVariableInNamedObject instruction in instance.InstructionSaves)
            {
                if(instruction.Type == null)
                {
                    var existingVariableDefinition = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == instruction.Member);

                    instruction.Type = existingVariableDefinition?.Type;
                }
                FixAllTypes(instruction);
            }

            foreach(var property in instance.Properties)
            {
                // special case it:
                if(property.Name == "DestinationRectangle" && property.Value is string asString)
                {
                    property.Value = CustomVariableCommonExtensions.FixValue(asString, "FloatRectangle?");

                }
                else
                {
                    FixAllTypes(property);
                }
            }

            foreach (NamedObjectSave contained in instance.ContainedObjects)
            {
                contained.FixAllTypes();
            }
        }

        public static void FixEnumerationTypes(this NamedObjectSave instance)
        {
            foreach (CustomVariableInNamedObject instruction in instance.InstructionSaves)
            {
                FixEnumerationType(instruction);
            }

            foreach (NamedObjectSave contained in instance.ContainedObjects)
            {
                contained.FixEnumerationTypes();
            }
        }

        private static void FixAllTypes(CustomVariableInNamedObject instruction)
        {
            FixEnumerationType(instruction);

            if (!string.IsNullOrEmpty(instruction.Type) && instruction.Value != null)
            {
                object variableValue = instruction.Value;
                var type = instruction.Type;
                variableValue = CustomVariableCommonExtensions.FixValue(variableValue, type);
                instruction.Value = variableValue;
            }
        }


        private static void FixAllTypes(PropertySave property)
        {
            if (!string.IsNullOrEmpty(property.Type) && property.Value != null)
            {
                object variableValue = property.Value;
                var type = property.Type;

                variableValue = CustomVariableCommonExtensions.FixValue(variableValue, type);

                property.Value = variableValue;
            }
        }

        private static void FixEnumerationType(CustomVariableInNamedObject instruction)
        {
            if (!string.IsNullOrEmpty(instruction.Type))
            {
                Type type = TypeManager.GetTypeFromString(instruction.Type);

                if (type != null && instruction.Value != null && type.IsEnum
                    // it may already be an enum:
                    && instruction.Value.GetType() != type)
                {
                    int valueAsInt = 0;
                    if (instruction.Value is int asInt)
                    {
                        valueAsInt = asInt;
                    }
                    else if (instruction.Value is long asLong)
                    {
                        valueAsInt = (int)asLong;
                    }
                    Array array = Enum.GetValues(type);

                    // The enumerations may not necessarily be
                    // 0,1,2,3,4
                    // They may skip values or start at non-zero values.
                    // Therefore, we need to compare the int values
                    for (int i = 0; i < array.Length; i++)
                    {
                        if ((int)(array.GetValue(i)) == valueAsInt)
                        {
                            instruction.Value = array.GetValue(i);
                            break;
                        }
                    }
                }
            }
        }

        // ConvertEnumerationValuesToInts and PostLoadLogic moved to
        // GlueCommon.SaveClasses.NamedObjectSaveCommonExtensions (#2276).

        // NamedObjectSaveToString, GetContainerType, GetContainer, and GetReferencedElement moved to
        // GlueCommon.SaveClasses.NamedObjectSaveElementExtensions (#2276) - they only needed the
        // IObjectFinderCore seam over ObjectFinder.Self, not ObjectFinder.Self itself.

        public static void GetAdditionsNeededForChangingType(string oldType, string newType, List<PropertyValuePair> valuesToBeSet,
            List<CustomVariable> neededVariables, List<StateSave> neededStates, List<StateSaveCategory> neededCategories)
        {
            var oldElement = ObjectFinder.Self.GetElement(oldType);
            var newElement = ObjectFinder.Self.GetElement(newType);

            if (oldElement != null && newElement != null)
            {
                #region Compare CustomVariables
                foreach (CustomVariable customVariable in oldElement.CustomVariables)
                {
                    string name = customVariable.Name;
                    string type = customVariable.Type;

                    // Is there a custom variable in the type to change to?
                    // We used to only call GetCustomVariable, but this needs
                    // to be recursive, because the object will get variables from
                    // the immediate type as well as all base types.
                    //CustomVariable customVariableInNewType = newElement.GetCustomVariable(name);
                    CustomVariable customVariableInNewType = newElement.GetCustomVariableRecursively(name);
                    
                    if (customVariableInNewType == null || customVariableInNewType.Type != type)
                    {
                        neededVariables.Add(customVariable);
                    }
                }
                #endregion

                #region Compare interfaces like IClickable

                if (oldElement is EntitySave && newElement is EntitySave)
                {
                    EntitySave oldEntity = oldElement as EntitySave;
                    EntitySave newEntity = newElement as EntitySave;

                    if (oldEntity.GetImplementsIClickableRecursively() && !newEntity.GetImplementsIClickableRecursively())
                    {
                        valuesToBeSet.Add(new PropertyValuePair("ImplementsIClickable", true));
                    }
                    if (oldEntity.GetImplementsIVisibleRecursively() && !newEntity.GetImplementsIVisibleRecursively())
                    {
                        valuesToBeSet.Add(new PropertyValuePair("ImplementsIVisible", true));
                    }
                    if (oldEntity.GetImplementsIWindowRecursively() && !newEntity.GetImplementsIWindowRecursively())
                    {
                        valuesToBeSet.Add(new PropertyValuePair("ImplementsIWindow", true));
                    }
                    if(oldEntity.GetImplementsITiledTileMetadataRecursively() && !newEntity.GetImplementsITiledTileMetadataRecursively())
                    {
                        valuesToBeSet.Add(new PropertyValuePair("ImplementsITiledTileMetadata", true));
                    }
                }

                #endregion

                #region Compare States

                // Don't use AllStates because we want
                // states that belong to categories to be
                // identified as being in categories.
                foreach (StateSave state in oldElement.States)
                {
                    if (newElement.GetUncategorizedStateRecursively(state.Name) == null)
                    {
                        neededStates.Add(state);
                    }
                }

                #endregion

                #region Compare Categories

                foreach (StateSaveCategory category in oldElement.StateCategoryList)
                {
                    StateSaveCategory cloneOfCategory = null;
                    StateSaveCategory categoryInNew = newElement.GetStateCategoryRecursively(category.Name);
                    if (categoryInNew == null)
                    {
                        cloneOfCategory = new StateSaveCategory { Name = category.Name };
                        neededCategories.Add(cloneOfCategory);
                    }

                    List<StateSave> statesMissingInNewCategory = new List<StateSave>();

                    foreach (StateSave state in category.States)
                    {
                        if (categoryInNew == null || categoryInNew.GetState(state.Name) == null)
                        {
                            if (cloneOfCategory == null)
                            {
                                cloneOfCategory = new StateSaveCategory { Name = category.Name };
                            }
                            cloneOfCategory.States.Add(state);
                        }
                    }

                    if (cloneOfCategory != null)
                    {
                        neededCategories.Add(cloneOfCategory);
                    }

                }


                #endregion
            }
        }

        public static bool DoesMemberNeedToBeSetByContainer(this NamedObjectSave instance, string memberName)
        {
            if (instance.SourceType == SourceType.Entity)
            {
                EntitySave sourceEntity = ObjectFinder.Self.GetEntitySave(instance.SourceClassType);

                if (sourceEntity != null)
                {

                    return sourceEntity.DoesMemberNeedToBeSetByContainer(memberName);
                }
            }

            return false;
        }


        // SetProperty moved to GlueCommon.SaveClasses.NamedObjectSaveCommonExtensions (#2276).

        public static void SetVariable(this NamedObjectSave instance, string variableName, object value)
        {
            var instruction = instance.GetCustomVariable(variableName);

            if (instruction == null)
            {
                var variableDefinition = instance.GetAssetTypeInfo()?.VariableDefinitions.FirstOrDefault(item => item.Name == variableName);

                if(variableDefinition != null)
                {
                    instruction = instance.AddInstruction(variableName, variableDefinition.Type);
                }
                else
                {
                    // If it comes from an entity, try to assign the type from the entity. This is needed if the variable
                    // is an Entity.Type property
                    var nosEntity = ObjectFinder.Self.GetEntitySave(instance);
                    var variable = nosEntity?.GetCustomVariableRecursively(variableName);

                    if(variable != null)
                    {
                        instruction = instance.AddInstruction(variableName, variable.Type);
                    }
                    else
                    {
                        var type = value?.GetType();
                        instruction = instance.AddNewGenericInstructionFor(variableName, type);
                    }
                }
            }

            instruction.Value = value;
        }


        public static bool GetIsScalableEntity(this NamedObjectSave instance)
        {
            if (instance.SourceType == SourceType.Entity && !string.IsNullOrEmpty(instance.SourceClassType))
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(instance.SourceClassType);

                return entitySave.GetCustomVariableRecursively("ScaleX") != null && entitySave.GetCustomVariableRecursively("ScaleY") != null;
            }
            return false;
        }

        // AddInstruction and AddNewGenericInstructionFor moved to
        // GlueCommon.SaveClasses.NamedObjectSaveCommonExtensions (#2276).

        public static string GetMessageWhySwitchMightCauseProblems(this NamedObjectSave namedObjectSave, string oldType)
        {
            List<CustomVariable> neededVariables = new List<CustomVariable>();
            List<PropertyValuePair> neededProperties = new List<PropertyValuePair>();
            List<StateSave> neededUncategoriedStates = new List<StateSave>();
            List<StateSaveCategory> neededCategories = new List<StateSaveCategory>();
            NamedObjectSaveExtensionMethods.GetAdditionsNeededForChangingType(oldType, namedObjectSave.SourceClassType, neededProperties, neededVariables,
                neededUncategoriedStates, neededCategories);

            string message = null;

            if (neededVariables.Count != 0)
            {
                message = "The type " + namedObjectSave.SourceClassType + " is missing the following variables:\n" + message;

                foreach (CustomVariable variable in neededVariables)
                {
                    message += string.Format("\n{0} ({1})", variable.Name, variable.Type);
                }

                message += "\n";
            }

            if (neededProperties.Count != 0)
            {
                if (message != null)
                {
                    message += "\n";
                }
                message += "The type " + namedObjectSave.SourceClassType + " is missing the following properties:\n";

                foreach (PropertyValuePair pvp in neededProperties)
                {
                    message += "\n" + pvp.Property;
                }
                message += "\n";
            }

            if (neededUncategoriedStates.Count != 0)
            {
                if (message != null)
                {
                    message += "\n";
                }
                message += "The type " + namedObjectSave.SourceClassType + " is missing the following states:\n";

                foreach (StateSave state in neededUncategoriedStates)
                {
                    message += string.Format("\n{0} ({1})", state.Name, "Uncategorized");
                }
                message += "\n";

            }

            if (neededCategories.Count != 0)
            {
                if (message != null)
                {
                    message += "\n";
                }
                message += "The type " + namedObjectSave.SourceClassType + " is needs the following categoires and categoried states:\n";

                foreach (StateSaveCategory category in neededCategories)
                {
                    if (category.States.Count == 0)
                    {
                        message += string.Format("\n{0} (Category) is missing", category.Name);
                    }
                    else
                    {
                        foreach (StateSave state in category.States)
                        {
                            message += string.Format("\n{0} ({1})", state.Name, "in category " + category.Name);
                        }
                    }
                }
                message += "\n";

            }
            return message;
        }


        public static void ResetVariablesReferencing(this NamedObjectSave namedObject, ReferencedFileSave rfs)
        {
            for(int i = namedObject.InstructionSaves.Count - 1; i > -1 ; i--)
            {
                var variable = namedObject.InstructionSaves[i];

                if (CustomVariableExtensionMethods.GetIsFile(variable.Type) && (string)(variable.Value) == rfs.GetInstanceName())
                {
                    // We're going to make it null, but
                    // we don't save null instructions in 
                    // NOS's so that our .glux stays small
                    // and so there's less chances of conflicts
                    // occurring because of undefined sorting behavior.
                    namedObject.InstructionSaves.RemoveAt(i);
                }
            }

        }

        // CanBeInList, CanBeInShapeCollection, IsCollidableOrCollidableList, and
        // ShouldInstantiateInConstructor moved to GlueCommon.SaveClasses.NamedObjectSaveElementExtensions
        // / NamedObjectSaveAssetTypeExtensions (#2276) - widened IObjectFinderCore
        // (GetAllBaseElementsRecursively) and IAvailableAssetTypesCore (CapsulePolygon/Circle/
        // AxisAlignedRectangle/Polygon/ShapeCollection) to unblock them, per the two seams' own pattern.

        // GetDefiningNamedObjectSave moved to GlueCommon.SaveClasses.NamedObjectSaveElementExtensions
        // (#2276) - same IObjectFinderCore seam as GetContainer/GetReferencedElement above.

        // IsCollisionRelationship moved to GlueCommon.SaveClasses.NamedObjectSaveCollisionExtensions
        // (net8.0, no WPF) - see #2276. Still resolves as an extension method for existing callers.

        // GetNamedObject and GetNamedObjectInList moved to
        // GlueCommon.SaveClasses.NamedObjectSaveCommonExtensions (#2276).

        /// <summary>
        /// Searches the argument container for any named object, and searches recursively through inheritance.
        /// </summary>
        /// <param name="namedObjectContainer"></param>
        /// <param name="namedObjectName"></param>
        /// <returns></returns>
        public static NamedObjectSave? GetNamedObjectRecursively(this INamedObjectContainer namedObjectContainer, string namedObjectName)
        {
            ////////////////////////early out////////////////////////
            if(string.IsNullOrEmpty(namedObjectName))
            {
                return null;
            }
            //////////////////////end early out//////////////////////
            List<NamedObjectSave> namedObjectList = namedObjectContainer.NamedObjects;

            NamedObjectSave foundNos = NamedObjectSaveCommonExtensions.GetNamedObjectInList(namedObjectList, namedObjectName);

            if (foundNos != null)
            {
                return foundNos;
            }

            // These methods need to check if the baseScreen/baseEntity is not null.
            // They can be null if the user deletes a base Screen/Entity and the tool
            // managing the Glux doesn't handle the changes.

            if (!string.IsNullOrEmpty(namedObjectContainer.BaseObject))
            {
                if (namedObjectContainer is EntitySave)
                {
                    EntitySave baseEntity = ObjectFinder.Self.GetEntitySave(namedObjectContainer.BaseObject);
                    if (baseEntity != null)
                    {
                        return GetNamedObjectRecursively(baseEntity, namedObjectName);
                    }
                }

                else if (namedObjectContainer is ScreenSave)
                {
                    ScreenSave baseScreen = ObjectFinder.Self.GetScreenSave(namedObjectContainer.BaseObject);

                    if (baseScreen != null)
                    {
                        return GetNamedObjectRecursively(baseScreen, namedObjectName);
                    }
                }
            }

            return null;
        }





    }


}
