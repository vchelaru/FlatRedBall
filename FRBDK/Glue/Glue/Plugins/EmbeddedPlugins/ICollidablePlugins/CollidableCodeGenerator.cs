using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.ICollidablePlugins
{
    public class CollidableCodeGenerator : ElementComponentCodeGenerator
    {

        public override Interfaces.CodeLocation CodeLocation
        {
            get
            {
                return Interfaces.CodeLocation.AfterStandardGenerated;
            }
        }

        static bool ShouldGenerateCollisionCodeFor(EntitySave asEntitySave)
        {
            var shouldGenerateCollision = asEntitySave?.ImplementsICollidable == true;

            if (shouldGenerateCollision)
            {
                // see if any base entities do. If they don't, then don't generate
                var baseEntities = ObjectFinder.Self.GetAllBaseElementsRecursively(asEntitySave);

                var doAnyImplementICollidable = baseEntities.Any(item => (item as EntitySave).ImplementsICollidable);

                if (doAnyImplementICollidable)
                {
                    shouldGenerateCollision = false;
                }
            }
            return shouldGenerateCollision;
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            EntitySave asEntitySave = element as EntitySave;

            if (ShouldGenerateCollisionCodeFor(asEntitySave))
            {

                codeBlock.Line("private FlatRedBall.Math.Geometry.ShapeCollection mGeneratedCollision;");
                var propBlock = codeBlock.Property("public FlatRedBall.Math.Geometry.ShapeCollection", "Collision");

                propBlock.Get().Line("return mGeneratedCollision;");

                var glueProjectFileVersion = GlueState.Self.CurrentGlueProject.FileVersion;

                if(glueProjectFileVersion >= (int)GlueProjectSave.GluxVersions.ICollidableHasItemsCollidedAgainst 
                    // If referencing source, source requires this. This could cause problems if a project is referencing old source, but 
                    // that is probably easier to fix than to upgrade the glux:
                    || GlueState.Self.IsReferencingFrbSource)
                {
                    codeBlock.Line("public HashSet<string> ItemsCollidedAgainst { get; private set;} = new HashSet<string>();");
                    codeBlock.Line("public HashSet<string> LastFrameItemsCollidedAgainst { get; private set;} = new HashSet<string>();");
                }

                if (glueProjectFileVersion >= (int)GlueProjectSave.GluxVersions.ICollidableHasObjectsCollidedAgainst
                    // If referencing source, source requires this. This could cause problems if a project is referencing old source, but 
                    // that is probably easier to fix than to upgrade the glux:
                    || GlueState.Self.IsReferencingFrbSource)
                {
                    codeBlock.Line("public HashSet<object> ObjectsCollidedAgainst { get; private set;} = new HashSet<object>();");
                    codeBlock.Line("public HashSet<object> LastFrameObjectsCollidedAgainst { get; private set;} = new HashSet<object>();");
                }
            }
            return codeBlock;
        }

        public override ICodeBlock GeneratePostInitialize(ICodeBlock codeBlock, IElement iElement)
        {
            var element = iElement as EntitySave;
            if (ShouldGenerateCollisionCodeFor(element))
            {
                if(element.PooledByFactory)
                {
                    codeBlock.Line("mGeneratedCollision = mGeneratedCollision ?? new FlatRedBall.Math.Geometry.ShapeCollection();");
                    codeBlock.Line("mGeneratedCollision.Clear();");

                }
                else
                {
                    codeBlock.Line("mGeneratedCollision = new FlatRedBall.Math.Geometry.ShapeCollection();");
                }

            }

            // This should be done at any level in case derived
            // objects are adding their own collision
            if(element.IsICollidableRecursive())
            {
                // July 16 2019
                // This used to only
                // check top-level objects,
                // but shapes can exist in manually-
                // created ShapeCollections. Those shapes
                // should be added to the generated collision 
                // object too, so using AllNamedObjects instead

                var baseEntities = ObjectFinder.Self.GetAllBaseElementsRecursively(element);
                var inheritsFromICollidable =
                    baseEntities.Any(item => (item as EntitySave).ImplementsICollidable);

                var shapesToAdd = element.AllNamedObjects.Where(item =>
                {
                    return
                        item.IncludeInICollidable &&
                        !item.IsDisabled &&
                        // July 8, 2020
                        // We used to exclude objects
                        // which are defined by base, but
                        // we only want to do that if this
                        // entity inherits from another entity
                        // which implements ICollidable. If this
                        // entity does not inherit from a base that
                        // is ICollidable, then it should add all shapes
                        // including ones defined in the base entity
                        (!item.DefinedByBase || inheritsFromICollidable == false);
                });

                foreach (var item in shapesToAdd)
                {
                    if(item.GetAssetTypeInfo()?.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.TileCollisions.TileShapeCollection")
                    {
                        // normally this would be handled in the TileShapeCollectionCodeGenerator, but if we move the code there, it 
                        // runs before the creation of the mGerneatedCollision. There's no good way to force plugin order, so we'll have
                        // this plugin handle TileShapeCollections
                        codeBlock.If($"{item.InstanceName} != null")
                            .Line($"mGeneratedCollision.AxisAlignedRectangles.AddRange({item.InstanceName}.Rectangles);")
                            .Line($"mGeneratedCollision.Polygons.AddRange({item.InstanceName}.Polygons);");
                    }
                    else
                    {
                        string addCall = item.GetAddToShapeCollection();

                        if (!string.IsNullOrEmpty(addCall))
                        {
                            // use Collision instead of mGeneratedCollision since
                            // mGeneratedCollision is private
                            codeBlock.Line("Collision." + addCall + "(" + item.FieldName + ");");
                        }
                    }
                }
            }

            return codeBlock;
        }

        public override void GenerateRemoveFromManagers(ICodeBlock codeBlock, IElement element)
        {
            TryGenerateRemoveShapeCollectionFromManagers(codeBlock, element as GlueElement);
        }

        private static void TryGenerateRemoveShapeCollectionFromManagers(ICodeBlock codeBlock, GlueElement element, bool shouldClear = false)
        {
            if (ShouldGenerateCollisionCodeFor(element as EntitySave))
            {
                codeBlock.Line($"mGeneratedCollision.RemoveFromManagers(clearThis: {shouldClear.ToString().ToLowerInvariant()});");
            }
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            // Added shouldClear:true on Oct 30, 2021 so that
            // collision in a tight loop will effectively early-out
            TryGenerateRemoveShapeCollectionFromManagers(codeBlock, element as GlueElement, shouldClear:true);

            return codeBlock;
        }

        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {
            if (ShouldGenerateCollisionCodeFor(element as EntitySave))
            {

                listToAddTo.Add("FlatRedBall.Math.Geometry.ICollidable");
            }
        }


    }


    public static class CollidableExtensions
    {
        public static bool IsICollidable(this IElement element)
        {
            return element is EntitySave && (element as EntitySave).ImplementsICollidable;
        }

        public static bool IsICollidableRecursive(this IElement element)
        {
            if(element is EntitySave entitySave)
            {
                if (entitySave.ImplementsICollidable)
                {
                    return true;
                }
                else
                {
                    var baseEntities = ObjectFinder.Self.GetAllBaseElementsRecursively(entitySave);
                    return baseEntities.Any(item => (item as EntitySave).ImplementsICollidable);
                }
            }
            return false;
        }

        public static string GetAddToShapeCollection(this NamedObjectSave nos)
        {
            var type = nos.SourceClassType;

            if (nos.SourceType == SourceType.FlatRedBallType || nos.SourceType == SourceType.File)
            {
                bool hasAdd =
                    type == "AxisAlignedRectangle" ||
                    type == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle.QualifiedRuntimeTypeName.QualifiedType ||
                    type == "Circle" ||
                    type == AvailableAssetTypes.CommonAtis.Circle.QualifiedRuntimeTypeName.QualifiedType ||
                    type == "Line" ||
                    type == AvailableAssetTypes.CommonAtis.Line.QualifiedRuntimeTypeName.QualifiedType ||
                    type == "Polygon" ||
                    type == AvailableAssetTypes.CommonAtis.Polygon.QualifiedRuntimeTypeName.QualifiedType ||
                    type == AvailableAssetTypes.CommonAtis.CapsulePolygon.QualifiedRuntimeTypeName.QualifiedType ||
                    type == "Sphere" ||
                    type == "FlatRedBall.Math.Geometry.Sphere" ||

                    type == "AxisAlignedCube" ||
                    type == "FlatRedBall.Math.Geometry.AxisAlignedCube" ||
                    type == "Capsule2D" || 
                    type == "FlatRedBall.Math.Geometry.Capsule2D"
                    ;

                if (hasAdd)
                {
                    if(type == AvailableAssetTypes.CommonAtis.CapsulePolygon.QualifiedRuntimeTypeName.QualifiedType)
                    {
                        // treat this as a polygon, becuse capsules do not have their own add method:
                        type = AvailableAssetTypes.CommonAtis.Polygon.QualifiedRuntimeTypeName.QualifiedType;
                    }
                    var unqualifiedType = type;


                    if(type.Contains("."))
                    {
                        unqualifiedType = type.Substring(type.LastIndexOf('.') + 1);
                    }
                    return unqualifiedType + "s.AddOneWay";
                }
                else
                {
                    return null;
                }
            }
            else
            {
                // Since ICollidables use a ShapeCollection, we need to add all contained elements to the
                // mGeneratedShapeCollection to get proper collision.  We can't add other ICollidables (currently)
                return null;
            }

        }
    }

}
