using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
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

        static bool ShouldGenerateCollisionCodeFor(IElement element)
        {
            var asEntitySave = element as EntitySave;

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

            if (ShouldGenerateCollisionCodeFor(element))
            {

                codeBlock.Line("private FlatRedBall.Math.Geometry.ShapeCollection mGeneratedCollision;");
                var propBlock = codeBlock.Property("public FlatRedBall.Math.Geometry.ShapeCollection", "Collision");

                propBlock.Get().Line("return mGeneratedCollision;");
            }
            return codeBlock;
        }

        public override ICodeBlock GeneratePostInitialize(ICodeBlock codeBlock, IElement element)
        {
            if (ShouldGenerateCollisionCodeFor(element))
            {
                codeBlock.Line("mGeneratedCollision = new FlatRedBall.Math.Geometry.ShapeCollection();");

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
                    string addCall = item.GetAddToShapeCollection();

                    if (!string.IsNullOrEmpty(addCall))
                    {
                        // use Collision instead of mGeneratedCollision since
                        // mGeneratedCollision is private
                        codeBlock.Line("Collision." + addCall + "(" + item.FieldName + ");");
                    }
                }
            }

            return codeBlock;
        }

        public override void GenerateRemoveFromManagers(ICodeBlock codeBlock, IElement element)
        {
            TryGenerateRemoveShapeCollectionFromManagers(codeBlock, element);
        }

        private static void TryGenerateRemoveShapeCollectionFromManagers(ICodeBlock codeBlock, IElement element)
        {
            if (ShouldGenerateCollisionCodeFor(element))
            {
                codeBlock.Line("mGeneratedCollision.RemoveFromManagers(clearThis: false);");
            }
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            TryGenerateRemoveShapeCollectionFromManagers(codeBlock, element);

            return codeBlock;
        }

        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {
            if (ShouldGenerateCollisionCodeFor(element))
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
                    type == "Circle" ||
                    type == "Line" ||
                    type == "Polygon" ||
                    type == "Sphere" ||
                    type == "AxisAlignedCube" ||
                    type == "Capsule2D";

                if (hasAdd)
                {
                    return type + "s.AddOneWay";
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
