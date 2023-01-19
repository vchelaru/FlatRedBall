using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Math.Geometry
{
    /// <summary>
    /// Interface requiring implementing objets to have a ShapeCollection Collision property for collision.
    /// </summary>
    public interface ICollidable : INameable
    {
        ShapeCollection Collision { get; }

        HashSet<string> ItemsCollidedAgainst { get; }
        HashSet<string> LastFrameItemsCollidedAgainst { get; } 

        HashSet<object> ObjectsCollidedAgainst { get; }
        HashSet<object> LastFrameObjectsCollidedAgainst { get; }
    }



    public static class ICollidableExtensionMethods
    {

        public static bool CollideAgainst(this ICollidable thisInstance, ICollidable other)
        {
            return other != null && thisInstance != null && thisInstance.Collision.CollideAgainst(other.Collision);
        }

        public static bool CollideAgainstMove(this ICollidable thisInstance, ICollidable other, float thisMass, float otherMass)
        {
            return other != null && thisInstance != null && thisInstance.Collision.CollideAgainstMove(other.Collision, thisMass, otherMass);
        }

        public static bool CollideAgainstBounce(this ICollidable thisInstance, ICollidable other, float thisMass, float otherMass, float elasticity)
        {
            return other != null && thisInstance != null && thisInstance.Collision.CollideAgainstBounce(other.Collision, thisMass, otherMass, elasticity);
        }

        public static bool CollideAgainst(this ICollidable thisInstance, AxisAlignedRectangle other)
        {
            return thisInstance.Collision.CollideAgainst(other);
        }

        public static bool CollideAgainstMove(this ICollidable thisInstance, AxisAlignedRectangle other, float thisMass, float otherMass)
        {
            return thisInstance.Collision.CollideAgainstMove(other, thisMass, otherMass);
        }

        public static bool CollideAgainstBounce(this ICollidable thisInstance, AxisAlignedRectangle other, float thisMass, float otherMass, float elasticity)
        {
            return thisInstance.Collision.CollideAgainstBounce(other, thisMass, otherMass, elasticity);
        }

        public static bool CollideAgainst(this ICollidable thisInstance, Circle other)
        {
            return thisInstance.Collision.CollideAgainst(other);
        }

        public static bool CollideAgainstMove(this ICollidable thisInstance, Circle other, float thisMass, float otherMass)
        {
            return thisInstance.Collision.CollideAgainstMove(other, thisMass, otherMass);
        }

        public static bool CollideAgainstBounce(this ICollidable thisInstance, Circle other, float thisMass, float otherMass, float elasticity)
        {
            return thisInstance.Collision.CollideAgainstBounce(other, thisMass, otherMass, elasticity);
        }


        public static bool CollideAgainst(this ICollidable thisInstance, Polygon other)
        {
            return thisInstance.Collision.CollideAgainst(other);
        }

        public static bool CollideAgainstMove(this ICollidable thisInstance, Polygon other, float thisMass, float otherMass)
        {
            return thisInstance.Collision.CollideAgainstMove(other, thisMass, otherMass);
        }

        public static bool CollideAgainstBounce(this ICollidable thisInstance, Polygon other, float thisMass, float otherMass, float elasticity)
        {
            return thisInstance.Collision.CollideAgainstBounce(other, thisMass, otherMass, elasticity);
        }

        public static bool CollideAgainst(this ICollidable thisInstance, Line other)
        {
            return thisInstance.Collision.CollideAgainst(other);
        }

        public static bool CollideAgainstMove(this ICollidable thisInstance, Line other, float thisMass, float otherMass)
        {
            return thisInstance.Collision.CollideAgainstMove(other, thisMass, otherMass);
        }

        public static bool CollideAgainstBounce(this ICollidable thisInstance, Line other, float thisMass, float otherMass, float elasticity)
        {
            return thisInstance.Collision.CollideAgainstBounce(other, thisMass, otherMass, elasticity);
        }

        public static bool CollideAgainst(this ICollidable thisInstance, ShapeCollection other)
        {
            return thisInstance.Collision.CollideAgainst(other);
        }

        public static bool CollideAgainstMove(this ICollidable thisInstance, ShapeCollection other, float thisMass, float otherMass)
        {
            return thisInstance.Collision.CollideAgainstMove(other, thisMass, otherMass);
        }

        public static bool CollideAgainstBounce(this ICollidable thisInstance, ShapeCollection other, float thisMass, float otherMass, float elasticity)
        {
            return thisInstance.Collision.CollideAgainstBounce(other, thisMass, otherMass, elasticity);
        }


        public static bool CollideAgainst<T>(this ICollidable thisInstance, PositionedObjectList<T> other) where T : PositionedObject, ICollidable
        {
            int count = other.Count;
            bool toReturn = false;
            for (int i = 0; i < count; i++)
            {
                // We can early out here:
                if (thisInstance != other[i] && thisInstance.Collision.CollideAgainst(other[i].Collision))
                {
                    toReturn = true;
                    break;
                }
            }
            return toReturn;
        }

        public static bool CollideAgainstMove<T>(this ICollidable thisInstance, PositionedObjectList<T> other, float thisMass, float otherMass) where T : PositionedObject, ICollidable
        {
            int count = other.Count;
            bool toReturn = false;
            for (int i = 0; i < count; i++)
            {
                if (thisInstance != other[i])
                {
                    // Don't early-out, the user may want all reactions to be applied
                    toReturn |= thisInstance.Collision.CollideAgainstMove(other[i].Collision, thisMass, otherMass);
                }
            }
            return toReturn;
        }

        public static bool CollideAgainstBounce<T>(this ICollidable thisInstance, PositionedObjectList<T> other, float thisMass, float otherMass, float elasticity) where T : PositionedObject, ICollidable
        {
            int count = other.Count;
            bool toReturn = false;
            for (int i = 0; i < count; i++)
            {
                if (thisInstance != other[i])
                {
                    // Don't early-out, the user may want all reactions to be applied
                    toReturn |= thisInstance.Collision.CollideAgainstBounce(other[i].Collision, thisMass, otherMass, elasticity);
                }
            }
            return toReturn;
        }

        public static bool IsPointInsideCollision(this ICollidable thisInstance, float x, float y)
        {
            return thisInstance.Collision.IsPointInside(x, y);
        }

    }



}
