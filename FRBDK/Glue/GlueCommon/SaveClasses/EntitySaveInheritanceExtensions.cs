using System.Collections.Generic;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>EntitySaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): these
    /// methods are pure logic over an <see cref="EntitySave"/>, needing only <see cref="IObjectFinderCore"/>/
    /// <see cref="IAvailableAssetTypesCore"/> (narrow seams over <c>ObjectFinder.Self</c>/
    /// <c>AvailableAssetTypes.Self</c>, see those interfaces' doc comments) instead of the singletons
    /// directly. Lives here (net8.0, no WPF) so it and its tests can build and run on Linux/macOS. See
    /// issue #2276. Named differently from the original class (not a forwarding stub) to avoid a
    /// duplicate-type clash now that both assemblies are visible together via <c>Glue.csproj</c>'s
    /// <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution doesn't care which class
    /// declares it, so existing call sites are unaffected.
    ///
    /// Not moved, and why: <c>GetTypedMembers</c> (and the private helpers it alone uses) stays in
    /// Glue.csproj - it calls <c>AssetTypeInfoExtensionMethods.GetTypedMemberBase</c>, which calls
    /// <c>TypeManager.GetTypeFromString</c>, the type-resolution half of <c>TypeManager</c> #2279 already
    /// found coupled to <c>PluginManager</c>/<c>AvailableAssetTypes</c>/<c>DialogService</c>.
    /// </summary>
    public static class EntitySaveInheritanceExtensions
    {
        public static EntitySave GetRootBaseEntitySave(this EntitySave instance)
        {
            if (string.IsNullOrEmpty(instance.BaseEntity) || instance.InheritsFromFrbType())
            {
                return instance;
            }
            else
            {
                EntitySave entitySave = ObjectFinderCore.Self.GetEntitySave(instance.BaseEntity);

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

        public static bool GetImplementsITiledTileMetadataRecursively(this EntitySave instance)
        {
            return instance.ImplementsITiledTileMetadata || instance.GetInheritsFromITiledTileMetadata();
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
                EntitySave entitySave = ObjectFinderCore.Self.GetEntitySave(instance.BaseEntity);

                return entitySave != null && (entitySave.ImplementsIWindow || entitySave.GetInheritsFromIWindow());
            }
        }

        public static bool GetInheritsFromITiledTileMetadata(this EntitySave instance)
        {
            if (string.IsNullOrEmpty(instance.BaseEntity))
            {
                return false;
            }
            else
            {
                EntitySave entitySave = ObjectFinderCore.Self.GetEntitySave(instance.BaseEntity);
                return entitySave != null && (entitySave.ImplementsITiledTileMetadata || entitySave.GetInheritsFromITiledTileMetadata());
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
                    AssetTypeInfo ati = AvailableAssetTypesCore.Self.GetAssetTypeFromRuntimeType(instance.BaseEntity, instance);

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
                    AssetTypeInfo ati = AvailableAssetTypesCore.Self.GetAssetTypeFromRuntimeType(instance.BaseEntity, instance);

                    if (ati != null)
                    {
                        return ati.HasVisibleProperty;
                    }
                }
                else
                {
                    EntitySave entitySave = ObjectFinderCore.Self.GetEntitySave(instance.BaseEntity);

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
                EntitySave entitySave = ObjectFinderCore.Self.GetEntitySave(instance.BaseEntity);

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
                EntitySave entitySave = ObjectFinderCore.Self.GetEntitySave(instance.BaseEntity);

                return entitySave != null &&
                    (entitySave.ImplementsIWindow ||
                    entitySave.ImplementsIClickable ||
                    entitySave.GetInheritsFromIWindowOrIClickable());
            }
        }

        /// <summary>
        /// Returns the entire inheritance chain of this element, with the most derived first.
        /// </summary>
        /// <param name="element">The element to return the inheritance chain for.</param>
        /// <returns>An IEnumerable of all base elements.</returns>
        public static IEnumerable<IElement> BaseElements(this IElement element)
        {
            if (!string.IsNullOrEmpty(element.BaseElement))
            {
                var baseElement = ObjectFinderCore.Self.GetElement(element.BaseElement);
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
                EntitySave baseEntity = ObjectFinderCore.Self.GetEntitySave(instance.BaseEntity);

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
                EntitySave baseEntity = ObjectFinderCore.Self.GetEntitySave(instance.BaseEntity);
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
    }
}
