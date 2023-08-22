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
        public static float GetAutomaticCollisionWidthHeight(NamedObjectSave namedObject, Axis sortAxis)
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
                    GetBigSmallForEntity(sortAxis, entity, ref small, ref big);
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

        private static void GetBigSmallForEntity(Axis sortAxis, EntitySave entity, ref float small, ref float big)
        {
            var allNamedObjects = entity?.GetAllNamedObjectsRecurisvely();

            var allCollisionObjects = allNamedObjects.Where(IsCollisionShape);

            if (allCollisionObjects != null)
            {
                foreach (var item in allCollisionObjects)
                {
                    float smallInner;
                    float bigInner;
                    (smallInner, bigInner) = GetDimensionFor(item, sortAxis);

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
                                (smallInner, bigInner) = GetDimensionFor(frame, sortAxis);

                                small = Math.Min(small, smallInner);
                                big = Math.Max(big, bigInner);
                            }
                        }
                    }
                    
                }
            }
        }

        private static (float small, float big) GetDimensionFor(AnimationFrameSave frame, Axis sortAxis)
        {
            if(frame.ShapeCollectionSave == null)
            {
                return (0, 0);
            }

            float big = 0;
            float small = 0;

            if(sortAxis == Axis.X)
            {
                foreach(var circle in frame.ShapeCollectionSave.CircleSaves)
                {
                    big = Math.Max(big, circle.X + circle.Radius);
                    small = Math.Min(small, circle.X - circle.Radius);
                }
                foreach(var rectangle in frame.ShapeCollectionSave.AxisAlignedRectangleSaves)
                {
                    big = Math.Max(big, rectangle.X + rectangle.ScaleX);
                    small = Math.Min(small, rectangle.X - rectangle.ScaleX);
                }
                foreach(var polygon in frame.ShapeCollectionSave.PolygonSaves)
                {
                    foreach(var point in polygon.Points)
                    {
                        big = Math.Max(big, (float)(polygon.X + point.X));
                        small = Math.Min(small, (float)(polygon.X - point.X));
                    }
                }
                // todo:
                //foreach(var capsule in frame.ShapeCollectionSave.CapsuleSaves)
                //{
                //    big = Math.Max(big, capsule.X + capsule.Radius);
                //    small = Math.Min(small, capsule.X - capsule.Radius);
                //}
            }
            else
            {
                // Do the same code as above, but his time use Y and ScaleY instead of X and ScaleX
                foreach (var circle in frame.ShapeCollectionSave.CircleSaves)
                {
                    big = Math.Max(big, circle.Y + circle.Radius);
                    small = Math.Min(small, circle.Y - circle.Radius);
                }
                foreach (var rectangle in frame.ShapeCollectionSave.AxisAlignedRectangleSaves)
                {
                    big = Math.Max(big, rectangle.Y + rectangle.ScaleY);
                    small = Math.Min(small, rectangle.Y - rectangle.ScaleY);
                }
                foreach (var polygon in frame.ShapeCollectionSave.PolygonSaves)
                {
                    foreach (var point in polygon.Points)
                    {
                        big = Math.Max(big, (float)(polygon.Y + point.Y));
                        small = Math.Min(small, (float)(polygon.Y - point.Y));
                    }
                }
            }

            return (small, big);
        }


        private static (float small, float big) GetDimensionFor(NamedObjectSave nos, Axis sortAxis)
        {
            var ati = nos.GetAssetTypeInfo();

            float small = 0;
            float big = 0;

            if(sortAxis == Axis.X)
            {
                var x = Get("X");
                if(ati == AvailableAssetTypes.CommonAtis.Circle)
                {
                    small = x - Get("Radius");
                    big = x + Get("Radius");
                }
                else if (ati == AvailableAssetTypes.CommonAtis.CapsulePolygon)
                {
                    small = x - Get("Width") / 2.0f;
                    big = x + Get("Width")/2.0f;
                }
                else if (ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle)
                {
                    small = x - Get("Width")/2.0f;
                    big = x + Get("Width")/2.0f;
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
                        }
                    }
                }
            }
            else // height
            {
                var y = Get("Y");
                if (ati == AvailableAssetTypes.CommonAtis.Circle)
                {
                    small = y - Get("Radius");
                    big = y + Get("Radius");
                }
                else if (ati == AvailableAssetTypes.CommonAtis.CapsulePolygon)
                {
                    small = y - Get("Height") / 2;
                    big = y + Get("Height") / 2;
                }
                else if (ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle)
                {
                    small = y - Get("Height")/2;
                    big = y + Get("Height")/2;
                }
                else if (ati == AvailableAssetTypes.CommonAtis.Polygon)
                {
                    
                    var points = nos.GetCustomVariable("Points")?.Value as List<Vector2>;

                    small = y;
                    big = y;
                    if (points != null)
                    {
                        foreach (var point in points)
                        {
                            small = Math.Min(y + point.Y, small);
                            big = Math.Max(y + point.Y, big);
                        }
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
