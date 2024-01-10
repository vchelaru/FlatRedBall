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

namespace OfficialPlugins.CollisionPlugin.Managers;

public struct PartitionSize
{
    public static readonly PartitionSize Zero = new PartitionSize();
    public float HalfDimension;
    public string Source;
}

public static class AutomatedCollisionSizeLogic
{
    /// <summary>
    /// Returns the max collision width or height for the argument NamedObjectSave. This value is
    /// used on the primary axis for partitioning, but also on the secondary axis for early-outs, so it
    /// must consider both X and Y.
    /// </summary>
    /// <param name="namedObject">The argument NamedObjectSave.</param>
    /// <returns>The max width or height (the larger of the two)</returns>
    public static PartitionSize GetAutomaticCollisionWidthHeight(NamedObjectSave namedObject)
    {
        var entityForNos = GetEntityFor(namedObject);

        PartitionSize small = PartitionSize.Zero;
        PartitionSize big = PartitionSize.Zero;

        if(entityForNos != null)
        {
            List<EntitySave> entities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entityForNos);
            entities.Add(entityForNos);

            foreach(var entity in entities)
            {
                GetBigSmallForEntity(entity, ref small, ref big);
            }

        }

        if (Math.Abs(small.HalfDimension) > Math.Abs(big.HalfDimension))
        {
            return small;
        }
        else
        {
            return big;
        }
    }

    private static void GetBigSmallForEntity(EntitySave entity, ref PartitionSize small, ref PartitionSize big)
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

                if(smallInner < small.HalfDimension)
                {
                    small.HalfDimension = smallInner;
                    small.Source = item.InstanceName;;
                }
                if(bigInner > big.HalfDimension)
                {
                    big.HalfDimension = bigInner;
                    big.Source = item.InstanceName;
                }
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
                        for (int i = 0; i < animation.Frames.Count; i++)
                        {
                            AnimationFrameSave frame = animation.Frames[i];

                            (var smallInner, var bigInner) = GetDimensionFor(frame);

                            if(smallInner.HalfDimension < small.HalfDimension)
                            {
                                small.HalfDimension = smallInner.HalfDimension;
                                small.Source = $"{smallInner.Source} in frame {i} in {animation.Name}";
                            }
                            if(bigInner.HalfDimension > big.HalfDimension)
                            {
                                big.HalfDimension = bigInner.HalfDimension;
                                big.Source = $"{bigInner.Source} in frame {i} in {animation.Name}";
                            }
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
    private static (PartitionSize small, PartitionSize big) GetDimensionFor(AnimationFrameSave frame)
    {
        if(frame.ShapeCollectionSave == null)
        {
            return (PartitionSize.Zero, PartitionSize.Zero);
        }

        float big = 0;
        float small = 0;
        string bigName = null;
        string smallName = null;
        foreach(var circle in frame.ShapeCollectionSave.CircleSaves)
        {
            if(circle.X + circle.Radius > big)
            {
                big = circle.X + circle.Radius;
                bigName = $"Circle {circle.Name}";
            }
            if(circle.Y + circle.Radius > big)
            {
                big = circle.Y + circle.Radius;
                bigName = $"Circle {circle.Name}";
            }

            if(circle.X - circle.Radius < small)
            {
                small = circle.X - circle.Radius;
                smallName = $"Circle {circle.Name}";
            }

            if(circle.Y - circle.Radius < small)
            {
                small = circle.Y - circle.Radius;
                smallName = $"Circle {circle.Name}";
            }
        }
        foreach(var rectangle in frame.ShapeCollectionSave.AxisAlignedRectangleSaves)
        {
            // check the rectangle x and Y plus scaleX and Y to assign the big/small values and the names:
            if(rectangle.X + rectangle.ScaleX > big)
            {
                big = rectangle.X + rectangle.ScaleX;
                bigName = $"Rectangle {rectangle.Name}";
            }
            if(rectangle.Y + rectangle.ScaleY > big)
            {
                big = rectangle.Y + rectangle.ScaleY;
                bigName = $"Rectangle {rectangle.Name}";
            }
            if(rectangle.X - rectangle.ScaleX < small)
            {
                small = rectangle.X - rectangle.ScaleX;
                smallName = $"Rectangle {rectangle.Name}";
            }
            if(rectangle.Y - rectangle.ScaleY < small)
            {
                small = rectangle.Y - rectangle.ScaleY;
                smallName = $"Rectangle {rectangle.Name}";
            }
        }
        foreach(var polygon in frame.ShapeCollectionSave.PolygonSaves)
        {
            foreach(var point in polygon.Points)
            {
                if(polygon.X + point.X > big)
                {
                    big = (float)(polygon.X + point.X);
                    bigName = $"Polygon {polygon.Name}";
                }
                if(polygon.Y + point.Y > big)
                {
                    big = (float)(polygon.Y + point.Y);
                    bigName = $"Polygon {polygon.Name}";
                }
                if(polygon.X - point.X < small)
                {
                    small = (float)(polygon.X - point.X);
                    smallName = $"Polygon {polygon.Name}";
                }
                if(polygon.Y - point.Y < small)
                {
                    small = (float)(polygon.Y - point.Y);
                    smallName = $"Polygon {polygon.Name}";
                }
            }
        }
        // todo:
        //foreach(var capsule in frame.ShapeCollectionSave.CapsuleSaves)
        //{
        //    big = Math.Max(big, capsule.X + capsule.Radius);
        //    small = Math.Min(small, capsule.X - capsule.Radius);
        //}

        return (
            new PartitionSize { HalfDimension = big, Source = bigName},
            new PartitionSize { HalfDimension = small, Source = smallName }
            );
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
            big = Math.Max(x + Get("Radius"), y + Get("Radius"));
        }
        else if (ati == AvailableAssetTypes.CommonAtis.CapsulePolygon)
        {
            small = Math.Min(x - Get("Width") / 2.0f, y - Get("Height") / 2);
            big = Math.Max(x + Get("Width")/2.0f, y + Get("Height") / 2);
        }
        else if (ati == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle)
        {
            small = Math.Min(x - Get("Width")/2.0f, y - Get("Height") / 2);
            big = Math.Max(x + Get("Width")/2.0f, y + Get("Height") / 2);
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
