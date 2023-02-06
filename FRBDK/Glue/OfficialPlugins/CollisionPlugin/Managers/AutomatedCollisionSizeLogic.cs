using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var allCollisionObjects = entity?.GetAllNamedObjectsRecurisvely().Where(IsCollisionShape);

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
                    small = x - Get("Width");
                    big = x + Get("Width");
                }
                else if (ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle)
                {
                    small = x - Get("Width");
                    big = x + Get("Width");
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
