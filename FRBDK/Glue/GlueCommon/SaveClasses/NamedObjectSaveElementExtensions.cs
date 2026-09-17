using System;
using System.Linq;
using System.Collections.Generic;

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
    }
}
