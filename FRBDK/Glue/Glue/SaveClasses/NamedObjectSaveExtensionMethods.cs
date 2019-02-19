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

                instance.InstructionSaves.Sort(
                    (first, second) =>
                    {
                        var firstTypedMember = instance.TypedMembers.FirstOrDefault((item) => item.MemberName == first.Member);
                        var secondTypedMember = instance.TypedMembers.FirstOrDefault((item) => item.MemberName == second.Member);

                        if (firstTypedMember != null && secondTypedMember == null)
                        {
                            return -1;
                        }
                        else if (firstTypedMember == null && secondTypedMember != null)
                        {
                            return 1;
                        }
                        else if (firstTypedMember == null && secondTypedMember == null)
                        {
                            if (first.Member != null)
                            {
                                return first.Member.CompareTo(second.Member);
                            }
                            else
                            {
                                return 1;
                            }
                        }
                        else
                        {
                            int firstIndex = instance.TypedMembers.IndexOf(firstTypedMember);
                            int secondIndex = instance.TypedMembers.IndexOf(secondTypedMember);

                            return firstIndex.CompareTo(secondIndex);

                        }

                    }
                );



                //// Are there any new members?
                //int desiredIndexOfInstruction = 0;

                //for (int i = 0; i < instance.TypedMembers.Count; i++)
                //{
                //    string member = instance.TypedMembers[i].MemberName;

                //    var instruction = instance.GetInstructionFromMember(member);
                //    if (instruction == null)
                //    {
                //        // Victor Chelaru November 26, 2012
                //        // Why do we create instructions for all values?  This bloats the .glux and makes for
                //        // a TON of conflicts.
                //        //instance.AddNewGenericInstructionFor(member, instance.TypedMembers[i].MemberType);
                //    }
                //    else
                //    {
                //        int indexOfInstruction = instance.InstructionSaves.IndexOf(instruction);
                //        if (indexOfInstruction != desiredIndexOfInstruction)
                //        {
                //            instance.InstructionSaves.Remove(instruction);
                //            if (desiredIndexOfInstruction >= instance.InstructionSaves.Count)
                //            {
                //                instance.InstructionSaves.Add(instruction);
                //            }
                //            else
                //            {
                //                instance.InstructionSaves.Insert(desiredIndexOfInstruction, instruction);
                //            }
                //        }
                //        desiredIndexOfInstruction++;

                //    }
                //}

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

        private static void UpdateTypedMembers(NamedObjectSave instance)
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
                        memberType = TypeManager.ConvertToCommonType(memberType);
                        errorCode = 2;

                        Type type = TypeManager.GetTypeFromString(memberType);
                        errorCode = 3;
                        // Glue can't do anything with generic properties (yet)
                        // Update: I'm adding support for it now
                        //if (type != null && type.IsGenericType == false)
                        TypedMemberBase typedMember = null;

                        if (type != null )
                        {
                            typedMember = TypedMemberBase.GetTypedMemberUnequatable(member.Member, type);
                        }
                        else
                        {
                            typedMember = TypedMemberBase.GetTypedMemberUnequatable(member.Member, typeof(object));
                            typedMember.CustomTypeName = memberType;
                        }
                        instance.TypedMembers.Add(typedMember);
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
                            memberType = TypeManager.ConvertToCommonType(memberType);

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
            NamedObjectSave newNamedObjectSave = FileManager.CloneObject(instance);

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
            // Therefore, we shouldn't fill the instruction saves this way, intstead
            // let's just have the instruction saves bee Added.
            newNamedObjectSave.InstructionSaves = new List<CustomVariableInNamedObject>();

            newNamedObjectSave.ContainedObjects = new List<NamedObjectSave>(instance.ContainedObjects.Count);

            for (int i = 0; i < instance.InstructionSaves.Count; i++)
            {
                //var duplicateInstruction =
                //    instance.InstructionSaves[i].Clone<CustomVariableInNamedObject>();

                var duplicateInstruction =
                    FileManager.CloneObject(instance.InstructionSaves[i]);

                // Events are instance-specific so we prob don't want to copy those
                duplicateInstruction.EventOnSet = null;

                newNamedObjectSave.InstructionSaves.Add(duplicateInstruction);
            }

            foreach (NamedObjectSave containedNamedObject in instance.ContainedObjects)
            {
                newNamedObjectSave.ContainedObjects.Add(containedNamedObject.Clone());
            }

            return newNamedObjectSave;
        }

        public static AssetTypeInfo GetAssetTypeInfo(this NamedObjectSave instance)
        {
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
                returnAti = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(instance.ClassType, isObject:true);
            }

            if (instance.ClassType.Contains("PositionedObjectList"))
            {
                return null;
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

        public static void FixEnumerationTypes(this NamedObjectSave instance)
        {
            foreach (CustomVariableInNamedObject instruction in instance.InstructionSaves)
            {
                if (!string.IsNullOrEmpty(instruction.Type))
                {
                    Type type = TypeManager.GetTypeFromString(instruction.Type);

                    if (type != null && instruction.Value != null && type.IsEnum && instruction.Value.GetType() == typeof(int))
                    {
                        Array array = Enum.GetValues(type);
                        
                            // The enumerations may not necessarily be
                            // 0,1,2,3,4
                            // They may skip values or start at non-zero values.
                            // Therefore, we need to compare the int values
                            for (int i = 0; i < array.Length; i++)
                            {
                                if ((int)(array.GetValue(i)) == (int)instruction.Value)
                                {
                                    instruction.Value = array.GetValue(i);
                                    break;
                                }
                            }
                    }
                }
            }

            foreach (NamedObjectSave contained in instance.ContainedObjects)
            {
                contained.FixEnumerationTypes();
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

            return nos.ClassType + " " + nos.FieldName + containerName;

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

        public static IElement GetContainer(this NamedObjectSave instance)
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

        public static IElement GetReferencedElement(this NamedObjectSave instance)
        {
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

        public static InstructionSave SetPropertyValue(this NamedObjectSave instance, string propertyName, object valueToSet)
        {
            InstructionSave instruction = instance.GetInstructionFromMember(propertyName);

            if (instruction == null)
            {
                TypedMemberBase tmb = instance.TypedMembers.FirstOrDefault(member=>member.MemberName == propertyName);

                if(tmb != null)
                {
                    instruction = instance.AddNewGenericInstructionFor(propertyName, tmb.MemberType);

                }
                else
                {
                    instruction = instance.AddNewGenericInstructionFor(propertyName, valueToSet.GetType());
                }

                if(tmb.CustomTypeName != null)
                {
                    instruction.Type = tmb.CustomTypeName;
                }
            }

            instruction.Value = valueToSet;
            return instruction;
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

            instructionSave.Type = type.FullName;
            instructionSave.Type = TypeManager.ConvertToCommonType(instructionSave.Type);
            instructionSave.Member = member;
            // Create a new instruction

            instance.InstructionSaves.Add(instructionSave);
            return instructionSave;
        }

        /// <summary>
        /// Returns the value of a variable either set in the NamedObjectSave (if it is set there) or in the underlying Entity if not
        /// </summary>
        /// <param name="instance">The NamedObjectSave to get the variable from.</param>
        /// <param name="variableName">The name of the variable, such as "ScaleX"</param>
        /// <returns>The value of the variable in either the NamedObjectSave or underlying Entity.  Returns null if varible isn't found.</returns>
        public static object GetEffectiveValue(this NamedObjectSave instance, string variableName)
        {
            CustomVariableInNamedObject cvino = instance.GetCustomVariable(variableName);

            if (cvino == null || cvino.Value == null)
            {
                if (instance.SourceType == SourceType.Entity && !string.IsNullOrEmpty(instance.SourceClassType))
                {
                    EntitySave entitySave = ObjectFinder.Self.GetEntitySave(instance.SourceClassType);

                    if (entitySave != null)
                    {
                        CustomVariable variable = entitySave.GetCustomVariableRecursively(variableName);

                        if (variable != null)
                        {
                            return variable.DefaultValue;
                        }
                    }
                }
            }
            else
            {
                return cvino.Value;
            }
            return null;
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

        public static CustomVariableInNamedObject GetCustomVariable(this NamedObjectSave namedObject, string variableName)
        {
            foreach (var variable in namedObject.InstructionSaves)
            {
                if (variable.Member == variableName)
                {
                    return variable;
                }
            }
            return null;
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

                IElement listElementType = ObjectFinder.Self.GetIElement(listNos.SourceClassGenericType);

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
            var isOfCorrectType = instance.SourceType == SourceType.FlatRedBallType &&
                (
                    instance.SourceClassType == "Circle" ||
                    instance.SourceClassType == "AxisAlignedRectangle" ||
                    instance.SourceClassType == "Polygon"
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

                IElement currentElement = ObjectFinder.Self.GetIElement(container.BaseElement);

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
                        currentElement = ObjectFinder.Self.GetIElement(currentElement.BaseElement);

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

#if GLUE
        public static string GetQualifiedClassType(this NamedObjectSave instance)
        {
            if (instance.SourceType == SourceType.FlatRedBallType && !string.IsNullOrEmpty(instance.InstanceType) &&
                instance.InstanceType.Contains("<T>"))
            {
                string genericType = instance.SourceClassGenericType;

                if (genericType == null)
                {
                    return null;
                }
                else
                {
                    // For now we are going to try to qualify this by using the ATIs, but eventually we may want to change the source class generic type to be fully qualified
                    var ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(genericType, true);
                    if(ati != null)
                    {
                        genericType = ati.QualifiedRuntimeTypeName.QualifiedType;
                    }

                    string instanceType = instance.InstanceType;

                    if(instanceType == "List<T>")
                    {
                        instanceType = "System.Collections.Generic.List<T>";
                    }
                    else if (instanceType == "PositionedObjectList<T>")
                    {
                        instanceType = "FlatRedBall.Math.PositionedObjectList<T>";
                    }

                    if (genericType.StartsWith("Entities\\") || genericType.StartsWith("Entities/"))
                    {
                        genericType =
                            ProjectManager.ProjectNamespace + '.' + genericType.Replace('\\', '.');
                        return instanceType.Replace("<T>", "<" + genericType + ">");

                    }
                    else
                    {
                        if (genericType.Contains("\\"))
                        {
                            // The namespace is part of it, so let's remove it
                            int lastSlash = genericType.LastIndexOf('\\');
                            genericType = genericType.Substring(lastSlash + 1);
                        }


                        return instanceType.Replace("<T>", "<" + genericType + ">");
                    }
                }
            }
            else
            {
                return instance.InstanceType;
            }

        }
#endif
    }


}
