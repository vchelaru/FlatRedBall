using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Math.Geometry
{
    public interface IStackable
    {
        List<Microsoft.Xna.Framework.Vector3> LockVectors { get; }
        List<Microsoft.Xna.Framework.Vector3> LockVectorsTemp { get; }
    }

    public static class IStackableExtensionMethods
    {
        public static bool CollideAgainstBounceStackable<T, U>(T item1, U item2, float firstMass, float secondMass) where T : PositionedObject, IStackable, ICollidable
    where U : PositionedObject, IStackable, ICollidable
        {
            if (item1.CollideAgainst(item2))
            {
                var item1PositionBefore = item1.Position;
                var item2PositionBefore = item2.Position;

                var item1VelocityBefore = item1.Velocity;
                var item2VelocityBefore = item2.Velocity;

                item1.CollideAgainstBounce(item2, firstMass, secondMass, 0.0f);

                var item1Reposition = (item1.Position - item1PositionBefore).NormalizedOrZero();
                var item2Reposition = (item2.Position - item2PositionBefore).NormalizedOrZero();

                if (item1.LockVectors.Contains(item2Reposition))
                {
                    item1.Position = item1PositionBefore;
                    item2.Position = item2PositionBefore;

                    item1.Velocity = item1VelocityBefore;
                    item2.Velocity = item2VelocityBefore;

                    item2.LockVectorsTemp.Add(item2Reposition);

                    item1.ForceUpdateDependenciesDeep();
                    item2.ForceUpdateDependenciesDeep();

                    // move item 1 back, redo collision with 0,1
                    // so item1 doesn't move:
                    item1.CollideAgainstBounce(item2, 1, 0, 0.0f);
                }
                else if (item2.LockVectors.Contains(item1Reposition))
                {

                    item1.Position = item1PositionBefore;
                    item2.Position = item2PositionBefore;

                    item1.Velocity = item1VelocityBefore;
                    item2.Velocity = item2VelocityBefore;
                    
                    item1.LockVectorsTemp.Add(item1Reposition);
                    
                    item1.ForceUpdateDependenciesDeep();
                    item2.ForceUpdateDependenciesDeep();

                    // move item 1 back, redo collision with 0,1
                    // so item2 doesn't move:
                    item1.CollideAgainstBounce(item2, 0, 1, 0.0f);
                }
                return true;

            }
            return false;
        }

    }

}
