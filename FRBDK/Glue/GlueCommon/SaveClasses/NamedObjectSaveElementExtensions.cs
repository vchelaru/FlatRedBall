using System;
using System.Linq;
using System.Collections.Generic;
using FlatRedBall.Instructions.Reflection;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>NamedObjectSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): these
    /// methods are pure logic over a <see cref="NamedObjectSave"/>/<see cref="IElement"/>, but need to
    /// resolve elements or entities by name, so they depend on <see cref="IObjectFinderCore"/> (a narrow
    /// seam over <c>ObjectFinder.Self</c>, see that interface's doc comment) instead of reaching for
    /// <c>ObjectFinder.Self</c> directly. Lives here (net8.0, no WPF) so it and its tests can build and run
    /// on Linux/macOS. See issue #2276. Named differently from the original class (not a forwarding stub)
    /// to avoid a duplicate-type clash now that both assemblies are visible together via
    /// <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution
    /// doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class NamedObjectSaveElementExtensions
    {
        public static GlueElement GetContainer(this NamedObjectSave instance)
        {
            if (ObjectFinderCore.Self.GlueProject != null)
            {
                return ObjectFinderCore.Self.GetElementContaining(instance);
            }
            else
            {
                return null;
            }
        }

        public static GlueElement GetReferencedElement(this NamedObjectSave instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (string.IsNullOrEmpty(instance.SourceClassType))
            {
                return null;
            }
            else
            {
                return ObjectFinderCore.Self.GetEntitySave(instance.SourceClassType);
            }
        }

        public static ContainerType GetContainerType(this NamedObjectSave instance)
        {
            IElement container = instance.GetContainer();

            if (container == null)
            {
                return ContainerType.None;
            }
            else if (container is EntitySave)
            {
                return ContainerType.Entity;
            }
            else
            {
                return ContainerType.Screen;
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

                var currentElement = ObjectFinderCore.Self.GetElement(container.BaseElement);

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
                        currentElement = ObjectFinderCore.Self.GetElement(currentElement.BaseElement);

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

        public static bool InheritsFrom(this EntitySave instance, string entity)
        {
            if (instance.BaseEntity == entity)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(instance.BaseEntity))
            {
                EntitySave baseEntity = ObjectFinderCore.Self.GetEntitySave(instance.BaseEntity);

                if (baseEntity != null)
                {
                    return baseEntity.InheritsFrom(entity);
                }
            }

            return false;
        }

        public static bool IsICollidableRecursive(this IElement element)
        {
            if (element is EntitySave entitySave)
            {
                if (entitySave.ImplementsICollidable)
                {
                    return true;
                }
                else
                {
                    var baseEntities = ObjectFinderCore.Self.GetAllBaseElementsRecursively(entitySave);
                    return baseEntities.Any(item => (item as EntitySave).ImplementsICollidable);
                }
            }
            return false;
        }

        public static bool CanBeInList(this NamedObjectSave instance, NamedObjectSave listNos)
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

                var listElementType = ObjectFinderCore.Self.GetElement(listNos.SourceClassGenericType);

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

        public static bool IsCollidableOrCollidableList(this NamedObjectSave namedObjectSave)
        {
            if (namedObjectSave.IsList)
            {
                var type = namedObjectSave.SourceClassGenericType;

                // For a more complete impl, see:
                // CollisionRelationshipViewModelController

                if (!string.IsNullOrEmpty(namedObjectSave.SourceClassGenericType))
                {
                    var entitySave = ObjectFinderCore.Self.GetEntitySave(namedObjectSave.SourceClassGenericType);

                    if (entitySave != null)
                    {
                        return entitySave.IsICollidableRecursive();
                    }
                }
                return false;
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
                ObjectFinderCore.Self.GetEntitySave(namedObjectSave.SourceClassType)?.ImplementsICollidable == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool DoesMemberNeedToBeSetByContainer(this NamedObjectSave instance, string memberName)
        {
            if (instance.SourceType == SourceType.Entity)
            {
                EntitySave sourceEntity = ObjectFinderCore.Self.GetEntitySave(instance.SourceClassType);

                if (sourceEntity != null)
                {

                    return sourceEntity.DoesMemberNeedToBeSetByContainer(memberName);
                }
            }

            return false;
        }

        public static bool GetIsScalableEntity(this NamedObjectSave instance)
        {
            if (instance.SourceType == SourceType.Entity && !string.IsNullOrEmpty(instance.SourceClassType))
            {
                EntitySave entitySave = ObjectFinderCore.Self.GetEntitySave(instance.SourceClassType);

                return entitySave.GetCustomVariableRecursively("ScaleX") != null && entitySave.GetCustomVariableRecursively("ScaleY") != null;
            }
            return false;
        }

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
                    EntitySave baseEntity = ObjectFinderCore.Self.GetEntitySave(namedObjectContainer.BaseObject);

                    if (baseEntity != null)
                    {
                        return GetNamedObjectRecursively(baseEntity, namedObjectName);
                    }
                }

                else if (namedObjectContainer is ScreenSave)
                {
                    ScreenSave baseScreen = ObjectFinderCore.Self.GetScreenSave(namedObjectContainer.BaseObject);

                    if (baseScreen != null)
                    {
                        return GetNamedObjectRecursively(baseScreen, namedObjectName);
                    }
                }
            }

            return null;
        }

        public static void GetAdditionsNeededForChangingType(string oldType, string newType, List<PropertyValuePair> valuesToBeSet,
            List<CustomVariable> neededVariables, List<StateSave> neededStates, List<StateSaveCategory> neededCategories)
        {
            var oldElement = ObjectFinderCore.Self.GetElement(oldType);
            var newElement = ObjectFinderCore.Self.GetElement(newType);

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

        public static string GetMessageWhySwitchMightCauseProblems(this NamedObjectSave namedObjectSave, string oldType)
        {
            List<CustomVariable> neededVariables = new List<CustomVariable>();
            List<PropertyValuePair> neededProperties = new List<PropertyValuePair>();
            List<StateSave> neededUncategoriedStates = new List<StateSave>();
            List<StateSaveCategory> neededCategories = new List<StateSaveCategory>();
            GetAdditionsNeededForChangingType(oldType, namedObjectSave.SourceClassType, neededProperties, neededVariables,
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
                    var nosEntity = ObjectFinderCore.Self.GetEntitySave(instance);
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
    }
}
