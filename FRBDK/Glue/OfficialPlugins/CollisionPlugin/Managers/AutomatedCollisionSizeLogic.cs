using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace OfficialPlugins.CollisionPlugin.Managers
{
    public static class AutomatedCollisionSizeLogic
    {
        /// <summary>
        /// Returns the max collision width or height for the argument NamedObjectSave. This value is
        /// used on the primary axis for partitioning, but also on the secondary axis for early-outs, so it
        /// must consider both X and Y.
        /// </summary>
        /// <param name="namedObject">The argument NamedObjectSave.</param>
        /// <returns>The max width or height (the larger of the two)</returns>
        public static float GetAutomaticCollisionWidthHeight(NamedObjectSave namedObject)
        {
            var entityForNos = GetEntityFor(namedObject);

            float small = 0;
            float big = 0;

            if(entityForNos != null)
            {
                List<EntitySave> entities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entityForNos);
                entities.Add(entityForNos);

                foreach(var entity in entities)
                {
                    GetBigSmallForEntity(entity, ref small, ref big);
                }

            }

            if (Math.Abs(small) > Math.Abs(big))
            {
                return Math.Abs(small) * 2;
            }
            else
            {
                return Math.Abs(big) * 2;
            }
        }

        private static void GetBigSmallForEntity(EntitySave entity, ref float small, ref float big)
        {
            var allNamedObjects = entity?.GetAllNamedObjectsRecurisvely();

            var allCollisionObjects = allNamedObjects.Where(IsCollisionShape);

            if (allCollisionObjects != null)
            {
                foreach (var item in allCollisionObjects)
                {
                    float smallInner;
                    float bigInner;
                    (smallInner, bigInner) = GetDimensionFor(item);

                    small = Math.Min(small, smallInner);
                    big = Math.Max(big, bigInner);
                }
            }

            var spriteAti = AvailableAssetTypes.CommonAtis.Sprite;


            foreach (var item in allNamedObjects)
            {
                if(item.GetAssetTypeInfo() == spriteAti && item.GetCustomVariable("SetCollisionFromAnimation")?.Value as bool? == true)
                {
                    // This uses collision, so let's try to get the AnimationChainSave
                    AnimationChainListSave animationChainListSave =
                        AvailableAnimationChainsStringConverter.GetReferencedAclsThroughSetVariables(entity, item, stateSave:null);

                    if(animationChainListSave != null)
                    {
                        foreach(var animation in animationChainListSave.AnimationChains)
                        {
                            foreach(var frame in animation.Frames)
                            {
                                float smallInner;
                                float bigInner;
                                (smallInner, bigInner) = GetDimensionFor(frame);

                                small = Math.Min(small, smallInner);
                                big = Math.Max(big, bigInner);
                            }
                        }
                    }
                    
                }
            }
        }

        /// <summary>
        /// Returns the big/small values for all shape collections in the argument frame. 
        /// </summary>
        /// <param name="frame">The frame which may have a shape collection</param>
        /// <returns>The big/small values used for partitioning</returns>
        private static (float small, float big) GetDimensionFor(AnimationFrameSave frame)
        {
            if(frame.ShapeCollectionSave == null)
            {
                return (0, 0);
            }

            float big = 0;
            float small = 0;

            foreach(var circle in frame.ShapeCollectionSave.CircleSaves)
            {
                big = Math.Max(big, circle.X + circle.Radius);
                small = Math.Min(small, circle.X - circle.Radius);
                big = Math.Max(big, circle.Y + circle.Radius);
                small = Math.Min(small, circle.Y - circle.Radius);
            }
            foreach(var rectangle in frame.ShapeCollectionSave.AxisAlignedRectangleSaves)
            {
                big = Math.Max(big, rectangle.X + rectangle.ScaleX);
                small = Math.Min(small, rectangle.X - rectangle.ScaleX);
                big = Math.Max(big, rectangle.Y + rectangle.ScaleY);
                small = Math.Min(small, rectangle.Y - rectangle.ScaleY);
            }
            foreach(var polygon in frame.ShapeCollectionSave.PolygonSaves)
            {
                foreach(var point in polygon.Points)
                {
                    big = Math.Max(big, (float)(polygon.X + point.X));
                    small = Math.Min(small, (float)(polygon.X - point.X));
                    big = Math.Max(big, (float)(polygon.Y + point.Y));
                    small = Math.Min(small, (float)(polygon.Y - point.Y));
                }
            }
            // todo:
            //foreach(var capsule in frame.ShapeCollectionSave.CapsuleSaves)
            //{
            //    big = Math.Max(big, capsule.X + capsule.Radius);
            //    small = Math.Min(small, capsule.X - capsule.Radius);
            //}

            return (small, big);
        }


        private static (float small, float big) GetDimensionFor(NamedObjectSave nos)
        {
            var ati = nos.GetAssetTypeInfo();

            float small = 0;
            float big = 0;

            var x = Get("X");
            var y = Get("Y");
            if(ati == AvailableAssetTypes.CommonAtis.Circle)
            {
                small = Math.Min( x - Get("Radius"),  y - Get("Radius"));
                big = Math.Min(x + Get("Radius"), y + Get("Radius"));
            }
            else if (ati == AvailableAssetTypes.CommonAtis.CapsulePolygon)
            {
                small = Math.Min(x - Get("Width") / 2.0f, y - Get("Height") / 2);
                big = Math.Min(x + Get("Width")/2.0f, y + Get("Height") / 2);
            }
            else if (ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle)
            {
                small = Math.Min(x - Get("Width")/2.0f, y - Get("Height") / 2);
                big = Math.Min(x + Get("Width")/2.0f, y + Get("Height") / 2);
            }
            else if (ati == AvailableAssetTypes.CommonAtis.Polygon)
            {
                var pointsVariable = nos.GetCustomVariable("Points");
                var points = pointsVariable?.Value as List<Vector2>;

                small = x;
                big = x;
                if(points != null)
                {
                    foreach(var point in points)
                    {
                        small = Math.Min(x + point.X, small);
                        big = Math.Max(x + point.X, big);
                        small = Math.Min(y + point.Y, small);
                        big = Math.Max(y + point.Y, big);
                    }
                }
            }

            return (small, big);

            float Get(string name)
            {
                var variable = nos.GetCustomVariable(name);
                if(variable == null)
                {
                    return default;
                }
                else if(variable.Value is float)
                {
                    return (float) variable.Value;
                }
                else
                {
                    return default;
                }

            }
        }

        private static bool IsCollisionShape(NamedObjectSave nos)
        {
            var ati = nos.GetAssetTypeInfo();

            return ati == AvailableAssetTypes.CommonAtis.Circle ||
                ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle ||
                ati == AvailableAssetTypes.CommonAtis.CapsulePolygon ||
                ati == AvailableAssetTypes.CommonAtis.Polygon;
        }

        private static EntitySave GetEntityFor(NamedObjectSave namedObject)
        {
            string genericType = null;
            if(namedObject.IsList)
            {
                genericType = namedObject.SourceClassGenericType;
            }
            else
            {
                genericType = namedObject.SourceClassType;
            }

            var entitySave = ObjectFinder.Self.GetEntitySave(genericType);

            return entitySave;
        }
    }
}
