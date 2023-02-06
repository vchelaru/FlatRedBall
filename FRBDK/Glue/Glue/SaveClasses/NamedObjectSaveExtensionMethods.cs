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
        /// <summary>
        /// Updates the InstructionSaves in the argument NamedObject
        /// according to the source type.  This method will never remove
        /// instructions, but will add them if the NOS comes from a type that
        /// has properties that are currently not represented in the NOS's Instructions.
        /// </summary>
        /// <param name="instance">The NamedObject to update properties on.</param>
        public static void UpdateCustomProperties(this NamedObjectSave instance)
        {
            // If the project hasn't been loaded yet, then this doesn't know what
            // members it has from its Entity.  Therefore, we shouldn't do anything here
            // or else we'll wipe out the set values.

            if (ObjectFinder.Self.GlueProject != null)
            {
                UpdateTypedMembers(instance);

                instance.InstructionSaves.Sort((first, second) => first.Member?.CompareTo(second.Member) ?? 0);

                #region Note about removing variables - why we don't do it anymore.
                // October 3, 2011
                // Victor Chelaru
                // We used to remove
                // unused variables, but
                // now we're going to keep
                // them.  This will allow users
                // to switch between different source
                // types and return without losing data.
                // Sure, it means a tiny bit of bloat in the
                // glux and at runtime in the ram, but I think
                // we can live with it.
                //List<string> keysToRemove = new List<string>();
                //// Are there any removed members?
                //for (int index = InstructionSaves.Count - 1; index > -1; index--)
                //{
                //    InstructionSave instruction = InstructionSaves[index];

                //    string key = instruction.Member;
                //    bool isKeyContained = false;

                //    for (int i = 0; i < mTypedMembers.Count; i++)
                //    {
                //        string member = mTypedMembers[i].MemberName;
                //        if (key == member)
                //        {
                //            isKeyContained = true;
                //            break;
                //        }
                //    }

                //    if (isKeyContained == false)
                //    {
                //        InstructionSaves.RemoveAt(index);
                //    }
                //}
                #endregion

            }
        }

        private static void UpdateTypedMembers(this NamedObjectSave instance)
        {
            if (instance.SourceType == SourceType.Entity)
            {
                if (string.IsNullOrEmpty(instance.SourceClassType) || instance.SourceClassType == "<NONE>")
                {
                    instance.TypedMembers.Clear();
                }
                else
                {
                    EntitySave entitySave = ObjectFinder.Self.GetEntitySave(
                        instance.SourceClassType);
                    if (entitySave != null)
                    {
                        instance.TypedMembers.Clear();
                        // This is null if a property that calls
                        // UpdateProperties is called before the project
                        // is loaded - as is the case when the GLUX is 
                        // deserialized.
                        instance.TypedMembers.AddRange(entitySave.GetTypedMembers());
                    }
                }
            }
            else if (string.IsNullOrEmpty(instance.ClassType) || instance.ClassType.Contains("PositionedObjectList<"))
            {
                instance.TypedMembers.Clear();
            }
            else if (instance.IsList)
            {
                // do nothing.
            }
            else
            {                
                instance.TypedMembers.Clear();
                // We used to only include members in the
                // ATI.  Now we want to include every possible
                // variable so that they all show up in the PropertyGrid.
                //AssetTypeInfo ati = instance.GetAssetTypeInfo();

                //if (ati == null)
                //{
                //    throw new NullReferenceException("Could not find an AssetType for the type " +
                //        instance.SourceClassType + ".  This either means that your ContenTypes CSV is corrupt, out of date, missing, or that you have not loaded a content types CSV if you are using teh GluxViewManager in a custom app.");
                //    instance.TypedMembers.Clear();
                //}
                //else
                //{
                //    instance.TypedMembers.Clear();
                //    instance.TypedMembers.AddRange(ati.GetTypedMembers());
                //}

                List<MemberWithType> variables = ExposedVariableManager.GetExposableMembersFor(instance);

                foreach (var member in variables)
                {
                    int errorCode = 0;
                    try
                    {
                        errorCode = 0;
                        string memberType = member.Type;
                        errorCode = 1;
                        memberType = TypeManager.GetCommonTypeName(memberType);
                        errorCode = 2;

                        Type type = TypeManager.GetTypeFromString(memberType);
                        errorCode = 3;
                        // Glue can't do anything with generic properties (yet)
                        // Update: I'm adding support for it now
                        //if (type != null && type.IsGenericType == false)
                        TypedMemberBase typedMember = null;

                        if (type != null )
                        {
                            try
                            {
                                typedMember = TypedMemberBase.GetTypedMemberUnequatable(member.Member, type);
                            }
                            catch
                            {
                                // oh well, I think be getting rid of these anyway
                            }
                        }
                        else
                        {
                            typedMember = TypedMemberBase.GetTypedMemberUnequatable(member.Member, typeof(object));
                            typedMember.CustomTypeName = memberType;
                        }
                        if(typedMember != null)
                        {
                            instance.TypedMembers.Add(typedMember);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Error trying to fix member " + member + " in object " + instance + ". Error code: " + errorCode + "Additional info:\n\n\n" + e.ToString(), e);

                    }
                }

                var ati = instance.GetAssetTypeInfo();

                if(ati != null)
                {
                    foreach(var member in ati.VariableDefinitions)
                    {
                        // Only consider this if it's not already handled:
                        bool isAlreadyHandled = instance.TypedMembers.Any(item => item.MemberName == member.Name);

                        if(!isAlreadyHandled)
                        {
                            string memberType = member.Type;
                            memberType = TypeManager.GetCommonTypeName(memberType);

                            Type type = TypeManager.GetTypeFromString(memberType);
                            // Glue can't do anything with generic properties (yet)
                            // Update: I'm adding support for it now
                            //if (type != null && type.IsGenericType == false)
                            TypedMemberBase typedMember = null;

                            if (type != null)
                            {
                                typedMember = TypedMemberBase.GetTypedMemberUnequatable(member.Name, type);
                            }
                            else
                            {
                                typedMember = TypedMemberBase.GetTypedMemberUnequatable(member.Name, typeof(object));
                                typedMember.CustomTypeName = memberType;
                            }
                            instance.TypedMembers.Add(typedMember);
                        }
                    }
                }
            }
        }
        public static NamedObjectSave Clone(this NamedObjectSave instance)
        {
            // This doesn't work as well in XML due to enum values, so let's use Json instead
            //NamedObjectSave newNamedObjectSave = FileManager.CloneObject(instance);
            var serialized = JsonConvert.SerializeObject(instance);
            var newNamedObjectSave = JsonConvert.DeserializeObject<NamedObjectSave>(serialized);
            newNamedObjectSave.TypedMembers.Clear();

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

        public static AssetTypeInfo GetAssetTypeInfo(this NamedObjectSave instance)
        {
            if(instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (string.IsNullOrEmpty(instance.ClassType))
            {
                return null;
            }

            // If this NOS uses an EntireFile, then we should ask the file for its AssetTypeInfo,
            // as there may be multiple file types that produce the same class type.
            // For example 
            AssetTypeInfo returnAti = null;

            if (instance.IsEntireFile)
            {
                var container = instance.GetContainer();

                var rfs = container?.GetReferencedFileSave(instance.SourceFile);

                if (rfs != null)
                {
                    var candidateAti = rfs.GetAssetTypeInfo();

                    // The user may use a file, but may change the runtime type through the 
                    // SourceName property, so we need to make sure they match:
                    if (candidateAti != null && candidateAti.RuntimeTypeName == instance.ClassType)
                    {
                        returnAti = candidateAti;
                    }
                }
            }

            if (returnAti == null)
            {
                returnAti =
                    // September 14, 2022
                    // We used to check only
                    // ClassType. Let's check
                    // both class type and name
                    // in case the ATI is qualified:
                    AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(instance.ClassType, instance, isObject: true);

                if(returnAti == null && instance.SourceClassType != null)
                {
                    returnAti = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(instance.SourceClassType, instance, isObject: true);
                }
            }

            if (returnAti == null && instance.IsList)
            {
                return AvailableAssetTypes.CommonAtis.PositionedObjectList;
            }
            else
            {
                // Vic says: I don't think this should throw an exception anymore
                //if (returnAti == null)
                //{
                //    throw new InvalidOperationException("You probably need to add the class type " + this.ClassType +
                //        " to the ContentTypes.csv");
                //}

                return returnAti;
            }
        }

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
                FixAllTypes(property);
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
                variableValue = CustomVariableExtensionMethods.FixValue(variableValue, type);
                instruction.Value = variableValue;
            }
        }


        private static void FixAllTypes(PropertySave property)
        {
            if (!string.IsNullOrEmpty(property.Type) && property.Value != null)
            {
                object variableValue = property.Value;
                var type = property.Type;

                variableValue = CustomVariableExtensionMethods.FixValue(variableValue, type);

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

        public static void ConvertEnumerationValuesToInts(this NamedObjectSave instance)
        {
            foreach (CustomVariableInNamedObject instruction in instance.InstructionSaves)
            {
                if (instruction.Value != null && instruction.Value.GetType().IsEnum)
                {
                    instruction.Value = (int)instruction.Value;
                }
            }
            // to prevent some threading issues:
            foreach(var property in instance.Properties.ToArray())
            {
                if(property.Value != null && property.Value.GetType().IsEnum)
                {
                    property.Value = (int)property.Value;
                }
            }

            foreach (NamedObjectSave contained in instance.ContainedObjects)
            {
                contained.ConvertEnumerationValuesToInts();
            }
        }

        public static void PostLoadLogic(this NamedObjectSave instance)
        {
            for (int i = instance.InstructionSaves.Count - 1; i > -1; i--)
            {
                if (instance.InstructionSaves[i].Value == null)
                {
                    instance.InstructionSaves.RemoveAt(i);
                }
            }
        }
        public static string NamedObjectSaveToString(NamedObjectSave nos)
        {
            IElement container = nos.GetContainer();

            string containerName = " (Uncontained)";
            if (container != null)
            {
                containerName = " in " + container.ToString();
            }

            return nos.ClassType + " " + nos.InstanceName + containerName;

        }

        public static ContainerType GetContainerType(this NamedObjectSave instance)
        {
            IElement container = instance.GetContainer();

            if (container == null)
            {
                return SaveClasses.ContainerType.None;
            }
            else if (container is EntitySave)
            {
                return SaveClasses.ContainerType.Entity;
            }
            else
            {
                return SaveClasses.ContainerType.Screen;
            }
        }

        public static GlueElement GetContainer(this NamedObjectSave instance)
        {
            if (ObjectFinder.Self.GlueProject != null)
            {
                return ObjectFinder.Self.GetElementContaining(instance);
            }
            else
            {
                return null;
            }
        }

        public static GlueElement GetReferencedElement(this NamedObjectSave instance)
        {
            if(instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (string.IsNullOrEmpty(instance.SourceClassType))
            {
                return null;
            }
            else
            {
                return ObjectFinder.Self.GetEntitySave(instance.SourceClassType);
            }
        }

        public static void GetAdditionsNeededForChangingType(string oldType, string newType, List<PropertyValuePair> valuesToBeSet,
            List<CustomVariable> neededVariables, List<StateSave> neededStates, List<StateSaveCategory> neededCategories)
        {
            IElement oldElement = ObjectFinder.Self.GetIElement(oldType);
            IElement newElement = ObjectFinder.Self.GetIElement(newType);

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


        public static void SetProperty(this NamedObjectSave instance, string propertyName, object value)
        {
            instance.Properties.SetValue(propertyName, value);
        }

        public static void SetVariable(this NamedObjectSave instance, string variableName, object value)
        {
            var instruction = instance.GetCustomVariable(variableName);

            if (instruction == null)
            {
                TypedMemberBase tmb = instance.TypedMembers.FirstOrDefault(member=>member.MemberName == variableName);

                Type type = tmb?.MemberType ?? value?.GetType();
                var customTypeName = tmb?.CustomTypeName;

                instruction = instance.AddNewGenericInstructionFor(variableName, type);

                if(customTypeName != null)
                {
                    instruction.Type = tmb.CustomTypeName;
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

        public static CustomVariableInNamedObject AddNewGenericInstructionFor(this NamedObjectSave instance, string member, Type type)
        {
            CustomVariableInNamedObject instructionSave = new CustomVariableInNamedObject();
            instructionSave.Value = null; // make it the default

            // April 2, 2018
            // This used to just assign type.Name, but that can cause ambiguity between 
            // different systems like FRB's HorizontalAlignment and Gum's HorizontalAlignment,
            // so we need to have the values be fully qualified.
            //instructionSave.Type = type.Name;

            if(type == typeof(List<string>))
            {
                instructionSave.Type = "List<string>";
            }
            else
            {
                instructionSave.Type = type.FullName;
            }

            // special case - if it's a list, then handle it here

            instructionSave.Type = TypeManager.GetCommonTypeName(instructionSave.Type);
            instructionSave.Member = member;
            // Create a new instruction

            instance.InstructionSaves.Add(instructionSave);
            return instructionSave;
        }

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

        public static bool CanBeInList(this NamedObjectSave instance, NamedObjectSave listNos )
        {
            if (listNos.SourceClassGenericType == instance.SourceClassType ||
                listNos.SourceClassGenericType == instance.InstanceType ||
                listNos.SourceClassGenericType == instance.GetAssetTypeInfo()?.QualifiedRuntimeTypeName.QualifiedType)
            {
                return true;
            }

            if (instance.SourceType == SourceType.Entity)
            {
                EntitySave instanceElement = instance.GetReferencedElement() as EntitySave;

                var listElementType = ObjectFinder.Self.GetElement(listNos.SourceClassGenericType);

                if (instanceElement == null || listElementType == null)
                {
                    return false;
                }

                if (instanceElement.InheritsFrom(listNos.SourceClassGenericType))
                {
                    return true;
                }
            }


            return false;
        }

        public static bool CanBeInShapeCollection(this NamedObjectSave instance)
        {
            var ati = instance.GetAssetTypeInfo();
            var isOfCorrectType = instance.SourceType == SourceType.FlatRedBallType &&
                (
                    ati == AvailableAssetTypes.CommonAtis.CapsulePolygon ||
                    ati == AvailableAssetTypes.CommonAtis.Circle ||
                    ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle ||
                    ati == AvailableAssetTypes.CommonAtis.Polygon
                );

            return isOfCorrectType;
        }

        public static NamedObjectSave GetDefiningNamedObjectSave(this NamedObjectSave instance, IElement container)
        {
            if (instance.DefinedByBase == false)
            {
                return instance;
            }
            else
            {
                // it's defined by base
                if (string.IsNullOrEmpty(container.BaseElement))
                {
                    throw new Exception("The instance is DefinedByBase, but the container doesn't have a BaseElement");
                }

                NamedObjectSave foundNos = null;

                var currentElement = ObjectFinder.Self.GetElement(container.BaseElement);

                while (currentElement != null)
                {
                    foundNos = currentElement.NamedObjects.FirstOrDefault(
                        item => item.InstanceName == instance.InstanceName);

                    if (foundNos != null && foundNos.SetByDerived)
                    {
                        break;
                    }
                    else
                    {
                        currentElement = ObjectFinder.Self.GetElement(currentElement.BaseElement);

                        if (currentElement == null)
                        {
                            if (foundNos == null || (foundNos.ExposedInDerived == false && foundNos.SetByDerived == false))
                            {
                                foundNos = null;
                            }
                        }
                    }

                }

                return foundNos;
            }

        }

        public static bool IsCollisionRelationship(this NamedObjectSave namedObjectSave)
        {

            return
                namedObjectSave.SourceClassType == "CollisionRelationship" ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollisionRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsPositionedObjectRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsListRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsPositionedObjectRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.AlwaysCollidingListCollisionRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsShapeCollection") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsListRelationship") == true ||

                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableListVsTileShapeCollectionRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableVsTileShapeCollectionRelationship") == true ||

                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateCollisionRelationship<") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateCollisionRelationshipBase<") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateSingleVsListRelationship<") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateListVsListRelationship<") == true ||

                namedObjectSave.SourceClassType?.StartsWith("CollisionRelationship<") == true;
        }

        public static bool IsCollidableOrCollidableList(this NamedObjectSave namedObjectSave)
        {
            if (namedObjectSave.IsList)
            {
                var type = namedObjectSave.SourceClassGenericType;

                // For a more complete impl, see:
                // CollisionRelationshipViewModelController
                return !string.IsNullOrEmpty(namedObjectSave.SourceClassGenericType) &&
                    ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassGenericType)?.ImplementsICollidable == true;
            }
            else if (namedObjectSave.GetAssetTypeInfo()?.RuntimeTypeName == "FlatRedBall.TileCollisions.TileShapeCollection" ||
                namedObjectSave.GetAssetTypeInfo()?.RuntimeTypeName == "TileShapeCollection")
            {
                return true;
            }
            else if (namedObjectSave.GetAssetTypeInfo()?.RuntimeTypeName == "FlatRedBall.Math.Geometry.ShapeCollection" ||
                namedObjectSave.GetAssetTypeInfo()?.RuntimeTypeName == "ShapeCollection")
            {
                return true;
            }
            else if (namedObjectSave.SourceType == SourceType.Entity &&
                ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType)?.ImplementsICollidable == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ShouldInstantiateInConstructor(this NamedObjectSave namedObjectSave)
        {
            return
                (namedObjectSave.IsList || namedObjectSave.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection) &&
                namedObjectSave.Instantiate &&
                !namedObjectSave.InstantiatedByBase;
        }

        public static NamedObjectSave GetNamedObject(this INamedObjectContainer namedObjectContainer, string namedObjectName)
        {
            return GetNamedObjectInList(namedObjectContainer.NamedObjects, namedObjectName);
        }

        public static NamedObjectSave GetNamedObjectInList(List<NamedObjectSave> namedObjectList, string namedObjectName)
        {
            for (int i = 0; i < namedObjectList.Count; i++)
            {
                NamedObjectSave nos = namedObjectList[i];

                if (nos.InstanceName == namedObjectName)
                {
                    return nos;
                }

                if (nos.ContainedObjects != null && nos.ContainedObjects.Count != 0)
                {
                    NamedObjectSave foundNos = GetNamedObjectInList(nos.ContainedObjects, namedObjectName);

                    if (foundNos != null)
                    {
                        return foundNos;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Searches the argument container for any named object, and searches recursively through inheritance.
        /// </summary>
        /// <param name="namedObjectContainer"></param>
        /// <param name="namedObjectName"></param>
        /// <returns></returns>
        public static NamedObjectSave GetNamedObjectRecursively(this INamedObjectContainer namedObjectContainer, string namedObjectName)
        {
            List<NamedObjectSave> namedObjectList = namedObjectContainer.NamedObjects;

            NamedObjectSave foundNos = GetNamedObjectInList(namedObjectList, namedObjectName);

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
