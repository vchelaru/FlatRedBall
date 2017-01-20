using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Instructions.Reflection;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class EntitySaveExtensionMethods
    {
        public static EntitySave GetRootBaseEntitySave(this EntitySave instance)
        {
            if (string.IsNullOrEmpty(instance.BaseEntity) || instance.InheritsFromFrbType())
            {
                return instance;
            }
            else
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(instance.BaseEntity);

                if (entitySave == null)
                {
                    // The user will get errors for this in other parts of Glue.
                    return null;
                }
                else
                {
                    return entitySave.GetRootBaseEntitySave();
                }
            }
        }

        public static bool GetImplementsIWindowRecursively(this EntitySave instance)
        {
            return instance.ImplementsIWindow || instance.GetInheritsFromIWindow();
        }

        /// <summary>
        /// Returns whether this or any base objects of this implement IVisible.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>Whether IVislble was found here or in the inheritance chain.</returns>
        public static bool GetImplementsIVisibleRecursively(this EntitySave instance)
        {
            return instance.ImplementsIVisible || instance.GetInheritsFromIVisible();
        }

        public static bool GetImplementsIClickableRecursively(this EntitySave instance)
        {
            return instance.ImplementsIClickable || instance.GetInheritsFromIClickable();
        }

        /// <summary>
        /// Returns whether the calling Entity inherits from another Entity that implements IWindow
        /// </summary>
        /// <param name="instance">The calling Entity</param>
        /// <returns>Whether the implementation is found in a base Entity.</returns>
        public static bool GetInheritsFromIWindow(this EntitySave instance)
        {
            if (string.IsNullOrEmpty(instance.BaseEntity))
            {
                return false;
            }
            else
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(instance.BaseEntity);

                
                return entitySave != null && ( entitySave.ImplementsIWindow || entitySave.GetInheritsFromIWindow());
            }
        }

        /// <summary>
        /// Returns whether the calling Entity inherits from another class that implements ICollidable.
        /// Whether the calling Entity itself implements ICollidable doesn't matter.
        /// </summary>
        /// <param name="instance">The calling Entity</param>
        /// <returns>Whether the implementation is found in a base Entity.</returns>
        public static bool GetHasImplementsCollidableProperty(this EntitySave instance)
        {
            if (string.IsNullOrEmpty(instance.BaseEntity))
            {
                return true;
            }
            else
            {
                if (instance.InheritsFromFrbType())
                {

                    AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(instance.BaseEntity);

                    if (ati != null)
                    {
                        return !ati.ImplementsICollidable;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Returns whether the calling Entity inherits from another Entity that implements IVisible.
        /// Whether the calling Entity itself implements IVisible doesn't matter.
        /// </summary>
        /// <param name="instance">The calling Entity</param>
        /// <returns>Whether the implementation is found in a base Entity.</returns>
        public static bool GetInheritsFromIVisible(this EntitySave instance)
        {
            if (string.IsNullOrEmpty(instance.BaseEntity))
            {
                return false;
            }
            else
            {
                if (instance.InheritsFromFrbType())
                {

                    AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(instance.BaseEntity);

                    if (ati != null)
                    {
                        return ati.HasVisibleProperty;
                    }
                }
                else
                {
                    EntitySave entitySave = ObjectFinder.Self.GetEntitySave(instance.BaseEntity);

                    return entitySave != null && (entitySave.ImplementsIVisible || entitySave.GetInheritsFromIVisible());
                }
            }

            return false;


        }

        /// <summary>
        /// Returns whether the calling Entity inherits from another Entity that implements IClickable
        /// </summary>
        /// <param name="instance">The calling Entity</param>
        /// <returns>Whether the implementation is found in a base Entity.</returns>
        public static bool GetInheritsFromIClickable(this EntitySave instance)
        {
            if (string.IsNullOrEmpty(instance.BaseEntity))
            {
                return false;
            }
            else
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(instance.BaseEntity);

                return entitySave != null && (entitySave.ImplementsIClickable || entitySave.GetInheritsFromIClickable());
            }

        }

        public static bool GetInheritsFromIWindowOrIClickable(this EntitySave instance)
        {
            if (string.IsNullOrEmpty(instance.BaseEntity))
            {
                return false;
            }
            else
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(instance.BaseEntity);

                return entitySave != null && 
                    (entitySave.ImplementsIWindow ||
                    entitySave.ImplementsIClickable ||
                    entitySave.GetInheritsFromIWindowOrIClickable());
            }
        }

        public static IEnumerable<IElement> BaseElements(this IElement element)
        {
            if (!string.IsNullOrEmpty(element.BaseElement))
            {
                IElement baseElement = ObjectFinder.Self.GetIElement(element.BaseElement);
                yield return baseElement;

                foreach (var found in baseElement.BaseElements())
                {
                    yield return found;
                }
            }

            yield break;
        }

        public static List<EntitySave> GetAllBaseEntities(this EntitySave instance)
        {
            List<EntitySave> listToReturn = new List<EntitySave>();

            instance.GetAllBaseEntities(listToReturn);

            return listToReturn;
        }

        public static void GetAllBaseEntities(this EntitySave instance, List<EntitySave> entityListToFill)
        {
            if (!string.IsNullOrEmpty(instance.BaseEntity))
            {
                EntitySave baseEntity = ObjectFinder.Self.GetEntitySave(instance.BaseEntity);

                if (baseEntity != null)
                {
                    entityListToFill.Add(baseEntity);

                    baseEntity.GetAllBaseEntities(entityListToFill);
                }
            }
        }

        public static MembershipInfo GetMemberMembershipInfo(this EntitySave instance, string memberName)
        {
            for (int i = 0; i < instance.ReferencedFiles.Count; i++)
            {
                if (instance.ReferencedFiles[i].Name == memberName || instance.ReferencedFiles[i].GetInstanceName() == memberName)
                {
                    return MembershipInfo.ContainedInThis;
                }
            }

            MembershipInfo namedObjectMembershipInfo = instance.GetMemberMembershipInfoForNamedObjectList(memberName, instance.NamedObjects);
            if (namedObjectMembershipInfo != MembershipInfo.NotContained)
            {
                return namedObjectMembershipInfo;
            }

            if (!string.IsNullOrEmpty(instance.BaseEntity))
            {
                EntitySave baseEntity = ObjectFinder.Self.GetEntitySave(instance.BaseEntity);
                if (baseEntity != null)
                {
                    bool value = baseEntity.HasMemberWithName(memberName);

                    if (value)
                    {
                        return MembershipInfo.ContainedInBase;
                    }
                }
            }

            return MembershipInfo.NotContained;

        }

        public static MembershipInfo GetMemberMembershipInfoForNamedObjectList(this EntitySave instance, string memberName, List<NamedObjectSave> namedObjectList)
        {
            for (int i = 0; i < namedObjectList.Count; i++)
            {
                if (namedObjectList[i].FieldName == memberName)
                {
                    return MembershipInfo.ContainedInThis;
                }

                MembershipInfo membershipInfo = instance.GetMemberMembershipInfoForNamedObjectList(memberName, namedObjectList[i].ContainedObjects);

                if (membershipInfo != MembershipInfo.NotContained)
                {
                    return membershipInfo;
                }
            }



            return MembershipInfo.NotContained;
        }

        public static bool HasMemberWithName(this EntitySave instance, string memberName)
        {
            return instance.GetMemberMembershipInfo(memberName) != MembershipInfo.NotContained;
        }


        public static bool InheritsFrom(this EntitySave instance, string entity)
        {
            if (instance.BaseEntity == entity)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(instance.BaseEntity))
            {
                EntitySave baseEntity = ObjectFinder.Self.GetEntitySave(instance.BaseEntity);

                if (baseEntity != null)
                {
                    return baseEntity.InheritsFrom(entity);
                }
            }

            return false;
        }

        public static bool UpdateFromBaseType(this EntitySave instance)
        {
            bool haveChangesOccurred = false;
            if (ObjectFinder.Self.GlueProject != null)
            {
                haveChangesOccurred |= NamedObjectContainerHelper.UpdateNamedObjectsFromBaseType(instance);

                IBehaviorContainerHelper.UpdateCustomVariablesFromBaseType(instance);
            }
            return haveChangesOccurred;


        }


        static void AddRangeUnique(this List<TypedMemberBase> listToAddTo, List<TypedMemberBase> whatToAdd)
        {
            foreach (var item in whatToAdd)
            {
                if (!listToAddTo.ContainsMatch(item))
                {
                    listToAddTo.Add(item);
                }
            }

        }

        static void AddUnique(this List<TypedMemberBase> listToAddTo, TypedMemberBase itemToAdd)
        {
            if (!listToAddTo.ContainsMatch(itemToAdd))
            {
                listToAddTo.Add(itemToAdd);
            }
        }

        static bool DoTypedMemberBasesMatch(TypedMemberBase item1, TypedMemberBase item2)
        {
            return item1.MemberName == item2.MemberName &&
                item1.MemberType == item2.MemberType;
        }

        static bool ContainsMatch(this List<TypedMemberBase> listToAddTo, TypedMemberBase itemToCheck)
        {
            foreach (var item in listToAddTo)
            {
                if (DoTypedMemberBasesMatch(item, itemToCheck))
                {
                    return true;
                }
            }

            return false;
        }


        public static List<FlatRedBall.Instructions.Reflection.TypedMemberBase> GetTypedMembers(this EntitySave instance)
        {
            List<TypedMemberBase> typedMembers = new List<TypedMemberBase>();

            for (int i = 0; i < instance.CustomVariables.Count; i++)
            {
                CustomVariable customVariable = instance.CustomVariables[i];

                string type = customVariable.Type;

                if (!string.IsNullOrEmpty(customVariable.OverridingPropertyType))
                {
                    type = customVariable.OverridingPropertyType;
                }

                TypedMemberBase typedMemberBase =
                    AssetTypeInfoExtensionMethods.GetTypedMemberBase(
                    type,
                    customVariable.Name);

                typedMembers.Add(typedMemberBase);

            }

            // Add any variables that are set by container
            for (int i = 0; i < instance.NamedObjects.Count; i++)
            {
                NamedObjectSave nos = instance.NamedObjects[i];

                if (nos.SetByContainer && !string.IsNullOrEmpty(nos.InstanceType))
                {
                    if (nos.SourceType == SourceType.Entity)
                    {
                        TypedMemberBase typedMemberBase = TypedMemberBase.GetTypedMember(nos.InstanceName, typeof(string));
                        typedMembers.Add(typedMemberBase);
                    }
                    else
                    {
                        if (!nos.IsList)
                        {
                            TypedMemberBase typedMemberBase =
                            AssetTypeInfoExtensionMethods.GetTypedMemberBase(
                                nos.InstanceType,
                                nos.InstanceName);

                            typedMembers.Add(typedMemberBase);
                        }
                    }
                }

            }

            if (!string.IsNullOrEmpty(instance.BaseEntity))
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(
                    instance.BaseEntity);

                // This may be null if the project improperly references
                // an EntitySave that really doesn't exist.
                if (entitySave != null)
                {
                    // We used to call "AddRange" but we don't want duplicates 
                    // (I don't think) so we're going to use the custom extension
                    // method to prevent duplicates:
                    //typedMembers.AddRange(entitySave.GetTypedMembers());
                    typedMembers.AddRangeUnique(entitySave.GetTypedMembers());
                }
            }

            return typedMembers;
        }

        public static List<StateSave> GetAllStatesReferencingObject(this EntitySave instance, string objectName)
        {
            return IElementHelper.GetAllStatesReferencingObject(instance, objectName);
        }
    }
}
