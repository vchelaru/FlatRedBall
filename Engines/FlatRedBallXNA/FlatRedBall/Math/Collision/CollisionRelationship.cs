using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Math.Collision
{
    #region Enums

    public enum CollisionLimit
    {
        All,
        First,
        Closest
    }

    public enum CollisionType
    {
        EventOnlyCollision,
        MoveCollision,
        BounceCollision
            //,PlatformerCollision
    }

    #endregion

    #region Base Implementations

    public abstract class CollisionRelationship
    {
        public int DeepCollisionsThisFrame { get; set; }

        public CollisionLimit CollisionLimit { get; set; }

        public abstract object FirstAsObject { get; }
        public abstract object SecondAsObject { get; }

        protected float? firstPartitioningSize;
        protected float? secondPartitioningSize;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// The number of frames to skip after performing collision logic. Default value is 0, which means no values are skipped.
        /// </summary>
        /// <remarks>
        /// A value of 1 will result in 1 frame being skipped after performing collisio logic resulting in collision being performed every other frame.
        /// </remarks>
        public int FrameSkip { get; set; }

        protected int skippedFrames;

        // The list of partitioned lists. This is the same
        // reference as what the CollisionMangaer holds on to.
        // I want the relationship to know what is partitioned so
        // the user can call DoCollision and have it partition automatically.
        public IEnumerable<PartitionedValuesBase> Partitions { get; set; }

        public abstract void DoCollisions();
    }

    public abstract class CollisionRelationship<FirstCollidableT, SecondCollidableT> : CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
    {
        #region Fields/Properties
        protected Func<FirstCollidableT, Circle> firstSubCollisionCircle;
        protected Func<FirstCollidableT, AxisAlignedRectangle> firstSubCollisionRectangle;
        protected Func<FirstCollidableT, Polygon> firstSubCollisionPolygon;
        protected Func<FirstCollidableT, ICollidable> firstSubCollisionCollidable;

        protected Func<SecondCollidableT, Circle> secondSubCollisionCircle;
        protected Func<SecondCollidableT, AxisAlignedRectangle> secondSubCollisionRectangle;
        protected Func<SecondCollidableT, Polygon> secondSubCollisionPolygon;
        protected Func<SecondCollidableT, ICollidable> secondSubCollisionCollidable;

        CollisionType CollisionType = CollisionType.EventOnlyCollision;

        float moveFirstMass;
        float moveSecondMass;
        float bounceElasticity;

        public Action<FirstCollidableT, SecondCollidableT> CollisionOccurred;

        #endregion

        #region Set SubCollision Methods

        public void SetFirstSubCollision(Func<FirstCollidableT, Circle> subCollisionFunc) { firstSubCollisionCircle = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, AxisAlignedRectangle> subCollisionFunc) { firstSubCollisionRectangle = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Polygon> subCollisionFunc) { firstSubCollisionPolygon = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, ICollidable> subCollisionFunc) { firstSubCollisionCollidable = subCollisionFunc; }

        public void SetSecondSubCollision(Func<SecondCollidableT, Circle> subCollisionFunc) { secondSubCollisionCircle = subCollisionFunc; }
        public void SetSecondSubCollision(Func<SecondCollidableT, AxisAlignedRectangle> subCollisionFunc) { secondSubCollisionRectangle = subCollisionFunc; }
        public void SetSecondSubCollision(Func<SecondCollidableT, Polygon> subCollisionFunc) { secondSubCollisionPolygon = subCollisionFunc; }
        public void SetSecondSubCollision(Func<SecondCollidableT, ICollidable> subCollisionFunc) { secondSubCollisionCollidable = subCollisionFunc; }

        #endregion

        #region Set Collision Type Methods

        // todo:
        // SetPlatformerCollision 

        public void SetMoveCollision(float firstMass, float secondMass)
        {
            this.CollisionType = CollisionType.MoveCollision;
            this.moveFirstMass = firstMass;
            this.moveSecondMass = secondMass;
        }

        public void SetBounceCollision(float firstMass, float secondMass, float elasticity)
        {
            this.CollisionType = CollisionType.BounceCollision;
            this.moveFirstMass = firstMass;
            this.moveSecondMass = secondMass;
            this.bounceElasticity = elasticity;
        }

        public void SetEventOnlyCollision()
        {
            this.CollisionType = CollisionType.EventOnlyCollision;
        }

        #endregion

        protected bool CollideConsideringSubCollisions(FirstCollidableT first, SecondCollidableT second)
        {
            this.DeepCollisionsThisFrame++;

            if(CollisionType == CollisionType.EventOnlyCollision)
            {
                return CollideAgainstConsiderSubCollisionEventOnly(first, second);
            }
            else if(CollisionType == CollisionType.MoveCollision)
            {
                return CollideAgainstConsiderSubCollisionMove(first, second);
            }
            else if(CollisionType == CollisionType.BounceCollision)
            {
                return CollideAgainstConsiderSubCollisionBounce(first, second);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private bool CollideAgainstConsiderSubCollisionEventOnly(FirstCollidableT first, SecondCollidableT second)
        {
            if (firstSubCollisionCircle != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainst(secondSubCollisionCircle(second));
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainst(secondSubCollisionRectangle(second));
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainst(secondSubCollisionPolygon(second));
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainst(firstSubCollisionCircle(first));
                }
                else
                {
                    return second.CollideAgainst(firstSubCollisionCircle(first));
                }
            }
            else if (firstSubCollisionRectangle != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionRectangle(first).CollideAgainst(secondSubCollisionCircle(second));
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionRectangle(first).CollideAgainst(secondSubCollisionRectangle(second));
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionRectangle(first).CollideAgainst(secondSubCollisionPolygon(second));
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainst(firstSubCollisionRectangle(first));
                }
                else
                {
                    return second.CollideAgainst(firstSubCollisionRectangle(first));
                }
            }
            else if (firstSubCollisionPolygon != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionPolygon(first).CollideAgainst(secondSubCollisionCircle(second));
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionPolygon(first).CollideAgainst(secondSubCollisionRectangle(second));
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionPolygon(first).CollideAgainst(secondSubCollisionPolygon(second));
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainst(firstSubCollisionPolygon(first));
                }
                else
                {
                    return second.CollideAgainst(firstSubCollisionPolygon(first));
                }
            }
            else if (firstSubCollisionCollidable != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionCollidable(first).CollideAgainst(secondSubCollisionCircle(second));
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionCollidable(first).CollideAgainst(secondSubCollisionRectangle(second));
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionCollidable(first).CollideAgainst(secondSubCollisionPolygon(second));
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainst(firstSubCollisionCollidable(first));
                }
                else
                {
                    return second.CollideAgainst(firstSubCollisionCollidable(first));
                }
            }
            else
            {
                if (secondSubCollisionCircle != null)
                {
                    return first.CollideAgainst(secondSubCollisionCircle(second));
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return first.CollideAgainst(secondSubCollisionRectangle(second));
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return first.CollideAgainst(secondSubCollisionPolygon(second));
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainst(first);
                }
                else
                {
                    return second.CollideAgainst(first);
                }
            }
        }

        private bool CollideAgainstConsiderSubCollisionMove(FirstCollidableT first, SecondCollidableT second)
        {
            if (firstSubCollisionCircle != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainstMove(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainstMove(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainstMove(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstMove(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass);
                }
                else
                {
                    return second.CollideAgainstMove(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass);
                }
            }
            else if (firstSubCollisionRectangle != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionRectangle(first).CollideAgainstMove(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionRectangle(first).CollideAgainstMove(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionRectangle(first).CollideAgainstMove(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstMove(firstSubCollisionRectangle(first), moveSecondMass, moveFirstMass);
                }
                else
                {
                    return second.CollideAgainstMove(firstSubCollisionRectangle(first), moveSecondMass, moveFirstMass);
                }
            }
            else if (firstSubCollisionPolygon != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionPolygon(first).CollideAgainstMove(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionPolygon(first).CollideAgainstMove(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionPolygon(first).CollideAgainstMove(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstMove(firstSubCollisionPolygon(first), moveSecondMass, moveFirstMass);
                }
                else
                {
                    return second.CollideAgainstMove(firstSubCollisionPolygon(first), moveSecondMass, moveFirstMass);
                }
            }
            else if (firstSubCollisionCollidable != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionCollidable(first).CollideAgainstMove(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionCollidable(first).CollideAgainstMove(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionCollidable(first).CollideAgainstMove(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstMove(firstSubCollisionCollidable(first), moveSecondMass, moveFirstMass);
                }
                else
                {
                    return second.CollideAgainstMove(firstSubCollisionCollidable(first), moveSecondMass, moveFirstMass);
                }
            }
            else
            {
                if (secondSubCollisionCircle != null)
                {
                    return first.CollideAgainstMove(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return first.CollideAgainstMove(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return first.CollideAgainstMove(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstMove(first, moveSecondMass, moveFirstMass);
                }
                else
                {
                    return second.CollideAgainstMove(first, moveSecondMass, moveFirstMass);
                }
            }
        }

        private bool CollideAgainstConsiderSubCollisionBounce(FirstCollidableT first, SecondCollidableT second)
        {
            if (firstSubCollisionCircle != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainstBounce(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainstBounce(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainstBounce(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstBounce(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass, bounceElasticity);
                }
                else
                {
                    return second.CollideAgainstBounce(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass, bounceElasticity);
                }
            }
            else if (firstSubCollisionRectangle != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionRectangle(first).CollideAgainstBounce(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionRectangle(first).CollideAgainstBounce(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionRectangle(first).CollideAgainstBounce(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstBounce(firstSubCollisionRectangle(first), moveSecondMass, moveFirstMass, bounceElasticity);
                }
                else
                {
                    return second.CollideAgainstBounce(firstSubCollisionRectangle(first), moveSecondMass, moveFirstMass, bounceElasticity);
                }
            }
            else if (firstSubCollisionPolygon != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionPolygon(first).CollideAgainstBounce(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionPolygon(first).CollideAgainstBounce(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionPolygon(first).CollideAgainstBounce(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstBounce(firstSubCollisionPolygon(first), moveSecondMass, moveFirstMass, bounceElasticity);
                }
                else
                {
                    return second.CollideAgainstBounce(firstSubCollisionPolygon(first), moveSecondMass, moveFirstMass, bounceElasticity);
                }
            }
            else if (firstSubCollisionCollidable != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionCollidable(first).CollideAgainstBounce(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionCollidable(first).CollideAgainstBounce(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionCollidable(first).CollideAgainstBounce(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstBounce(firstSubCollisionCollidable(first), moveSecondMass, moveFirstMass, bounceElasticity);
                }
                else
                {
                    return second.CollideAgainstBounce(firstSubCollisionCollidable(first), moveSecondMass, moveFirstMass, bounceElasticity);
                }
            }
            else
            {
                if (secondSubCollisionCircle != null)
                {
                    return first.CollideAgainstBounce(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return first.CollideAgainstBounce(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return first.CollideAgainstBounce(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstBounce(first, moveSecondMass, moveFirstMass, bounceElasticity);
                }
                else
                {
                    return second.CollideAgainstBounce(first, moveSecondMass, moveFirstMass, bounceElasticity);
                }
            }
        }


    }

    #endregion

    #region Single vs Single

    public class PositionedObjectVsPositionedObjectRelationship<FirstCollidableT, SecondCollidableT> : 
        CollisionRelationship<FirstCollidableT, SecondCollidableT>
        where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
    {
        FirstCollidableT first;
        SecondCollidableT second;

        public override object FirstAsObject => first;
        public override object SecondAsObject => second;

        public PositionedObjectVsPositionedObjectRelationship(FirstCollidableT first, SecondCollidableT second)
        {
            this.first = first;
            this.second = second;
        }


        public override void DoCollisions()
        {
            if(skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                skippedFrames = 0;
                // collision limit doesn't do anything here, since it's only 1 vs 1
                if(CollideConsideringSubCollisions(first, second))
                {
                    CollisionOccurred?.Invoke(first, second);
                }
            }
        }
    }

    #endregion

    #region Single vs List
    public class PositionedObjectVsListRelationship<FirstCollidableT, SecondCollidableT> :
        CollisionRelationship<FirstCollidableT, SecondCollidableT>
        where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
    {
        FirstCollidableT singleObject;
        PositionedObjectList<SecondCollidableT> list;

        public override object FirstAsObject => singleObject;
        public override object SecondAsObject => list;

        public void SetPartitioningSize(FirstCollidableT partitionedObject, float widthOrHeight)
        {
            firstPartitioningSize = widthOrHeight;
        }

        public void SetPartitioningSize(PositionedObjectList<SecondCollidableT> partitionedObject, float widthOrHeight)
        {
            secondPartitioningSize = widthOrHeight;
        }

        public PositionedObjectVsListRelationship(FirstCollidableT singleObject, PositionedObjectList<SecondCollidableT> list)
        {
            this.singleObject = singleObject;
            this.list = list;
        }
        
        private void GetCollisionStartAndInt(out int startInclusive, out int endExclusive)
        {
            PartitionedValuesBase firstPartition = null;
            PartitionedValuesBase secondPartition = null;

            foreach(var partition in Partitions)
            {
                if(partition.PartitionedObject == singleObject)
                {
                    firstPartition = partition;
                }
                else if(partition.PartitionedObject == list)
                {
                    secondPartition = partition;
                }
            }

            float firstSize = 0;
            float secondSize = 0;

            if (firstPartition != null && secondPartition != null)
            {
                firstSize = this.firstPartitioningSize ?? firstPartition.MaxWidthOrHeight;
                secondSize = this.secondPartitioningSize ?? secondPartition.MaxWidthOrHeight;
            }

            if (firstPartition?.axis == Axis.X && firstPartition?.axis == Axis.X)
            {
                // -1 to make it exclusive
                endExclusive = list.GetFirstAfter(singleObject.X - firstSize / 2 - secondSize/2, Axis.X, 0, list.Count) - 1;
                startInclusive = list.GetFirstAfter(singleObject.X + firstSize / 2 + secondSize / 2, Axis.X, 0, list.Count);

                if(startInclusive > 0 && startInclusive == list.Count)
                {
                    startInclusive--;
                }

            }
            else if (firstPartition?.axis == Axis.Y && firstPartition?.axis == Axis.Y)
            {
                // -1 to make it exclusive
                endExclusive = list.GetFirstAfter(singleObject.Y - firstSize / 2 - secondSize / 2, Axis.Y, 0, list.Count) - 1;
                startInclusive = list.GetFirstAfter(singleObject.Y + firstSize / 2 + secondSize / 2, Axis.Y, 0, list.Count);
                if (startInclusive > 0 && startInclusive == list.Count)
                {
                    startInclusive--;
                }
            }
            else
            {
                startInclusive = list.Count - 1;
                endExclusive = -1;
            }
        }

        public override void DoCollisions()
        {
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                skippedFrames = 0;
                if (CollisionLimit == CollisionLimit.Closest)
                {
                    DoClosestCollision();
                }
                else
                {
                    int startInclusive;
                    int endExclusive;
                    GetCollisionStartAndInt(out startInclusive, out endExclusive);

                    for (int i = startInclusive; i > endExclusive; i--)
                    {
                        var atI = list[i];
                        if (CollideConsideringSubCollisions(singleObject, atI))
                        {
                            CollisionOccurred?.Invoke(singleObject, atI);
                            if (CollisionLimit == CollisionLimit.First)
                            {
                                break;
                            }
                        }
                    }
                }
            }

        }

        private void DoClosestCollision()
        {
            FirstCollidableT closestFirst = null;
            SecondCollidableT closestSecond = null;
            var closestDistanceSquared = float.PositiveInfinity;
            int startInclusive;
            int endExclusive;
            GetCollisionStartAndInt(out startInclusive, out endExclusive);

            for (int i = startInclusive; i > endExclusive; i--)
            {
                var atI = list[i];
                if (CollideConsideringSubCollisions(singleObject, atI))
                {
                    var distanceVector = singleObject.Position - atI.Position;
                    var distanceSquared = distanceVector.X * distanceVector.X + distanceVector.Y * distanceVector.Y;

                    if(distanceSquared < closestDistanceSquared)
                    {
                        closestDistanceSquared = distanceSquared;
                        closestFirst = singleObject;
                        closestSecond = atI;
                    }

                }
            }

            if(closestFirst != null)
            {
                CollisionOccurred?.Invoke(closestFirst, closestSecond);
            }
        }
    }
    #endregion

    #region List vs Single

    public class ListVsPositionedObjectRelationship<FirstCollidableT, SecondCollidableT> :
        CollisionRelationship<FirstCollidableT, SecondCollidableT>
        where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
    {
        PositionedObjectList<FirstCollidableT> list;
        SecondCollidableT singleObject;

        public override object FirstAsObject => list;
        public override object SecondAsObject => singleObject;


        public void SetPartitioningSize(PositionedObjectList<FirstCollidableT> partitionedObject, float widthOrHeight)
        {
            firstPartitioningSize = widthOrHeight;
        }

        public void SetPartitioningSize(SecondCollidableT partitionedObject, float widthOrHeight)
        {
            secondPartitioningSize = widthOrHeight;
        }

        public ListVsPositionedObjectRelationship(PositionedObjectList<FirstCollidableT> list, SecondCollidableT singleObject)
        {
            this.list = list;
            this.singleObject = singleObject;
        }

        private void GetCollisionStartAndInt(out int startInclusive, out int endExclusive)
        {
            PartitionedValuesBase firstPartition = null;
            PartitionedValuesBase secondPartition = null;

            foreach (var partition in Partitions)
            {
                if (partition.PartitionedObject == list)
                {
                    firstPartition = partition;
                }
                else if (partition.PartitionedObject == singleObject)
                {
                    secondPartition = partition;
                }
            }

            float firstSize = 0;
            float secondSize = 0;

            if(firstPartition != null && secondPartition != null)
            {
                firstSize = this.firstPartitioningSize ?? firstPartition.MaxWidthOrHeight;
                secondSize = this.secondPartitioningSize ?? secondPartition.MaxWidthOrHeight;
            }

            if (firstPartition?.axis == Axis.X && firstPartition?.axis == Axis.X)
            {
                // -1 to make it exclusive
                endExclusive = list.GetFirstAfter(singleObject.X - firstSize / 2 - secondSize / 2, Axis.X, 0, list.Count) - 1;
                startInclusive = list.GetFirstAfter(singleObject.X + firstSize / 2 + secondSize / 2, Axis.X, 0, list.Count);

                if (startInclusive > 0 && startInclusive == list.Count)
                {
                    startInclusive--;
                }
            }
            else if (firstPartition?.axis == Axis.Y && firstPartition?.axis == Axis.Y)
            {
                // -1 to make it exclusive
                endExclusive = list.GetFirstAfter(singleObject.Y - firstSize / 2 - secondSize / 2, Axis.Y, 0, list.Count) - 1;
                startInclusive = list.GetFirstAfter(singleObject.Y + firstSize / 2 + secondSize / 2, Axis.Y, 0, list.Count);
                if (startInclusive > 0 && startInclusive == list.Count)
                {
                    startInclusive--;
                }
            }
            else
            {
                startInclusive = list.Count - 1;
                endExclusive = -1;
            }
        }


        public override void DoCollisions()
        {
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                skippedFrames = 0;
                int startInclusive;
                int endExclusive;

                GetCollisionStartAndInt(out startInclusive, out endExclusive);

                for (int i = startInclusive; i > endExclusive; i--)
                {
                    var atI = list[i];
                    if (CollideConsideringSubCollisions(atI, singleObject))
                    {
                        CollisionOccurred?.Invoke(atI, singleObject);

                        // Collision Limit doesn't do anything here
                    }
                }
            }
        }
    }

    #endregion

    #region List vs. List

    public class ListVsListRelationship<FirstCollidableT, SecondCollidableT> :
        CollisionRelationship<FirstCollidableT, SecondCollidableT>
        where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
    {
        PositionedObjectList<FirstCollidableT> firstList;
        PositionedObjectList<SecondCollidableT> secondList;

        public override object FirstAsObject => firstList;
        public override object SecondAsObject => secondList;

        public void SetPartitioningSize(PositionedObjectList<FirstCollidableT> partitionedObject, float widthOrHeight)
        {
            firstPartitioningSize = widthOrHeight;
        }

        public void SetPartitioningSize(PositionedObjectList<SecondCollidableT> partitionedObject, float widthOrHeight)
        {
            secondPartitioningSize = widthOrHeight;
        }

        public ListVsListRelationship(PositionedObjectList<FirstCollidableT> firstList, PositionedObjectList<SecondCollidableT> secondList)
        {
            this.firstList = firstList;
            this.secondList = secondList;
        }

        public override void DoCollisions()
        {
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                skippedFrames = 0;
                if (CollisionLimit == CollisionLimit.Closest)
                {
                    DoClosestCollision();
                }
                else
                {
                    for (int i = firstList.Count - 1; i > -1; i--)
                    {
                        var first = firstList[i];

                        int startInclusive;
                        int endExclusive;

                        GetCollisionStartAndInt(first, out startInclusive, out endExclusive);

                        for (int j = startInclusive; j > endExclusive; j--)
                        {
                            var second = secondList[j];

                            if (CollideConsideringSubCollisions(first, second))
                            {
                                CollisionOccurred?.Invoke(first, second);

                                if (CollisionLimit == CollisionLimit.First)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GetCollisionStartAndInt(FirstCollidableT first, out int startInclusive, out int endExclusive)
        {
            PartitionedValuesBase firstPartition = null;
            PartitionedValuesBase secondPartition = null;

            foreach (var partition in Partitions)
            {
                if (partition.PartitionedObject == firstList)
                {
                    firstPartition = partition;
                }
                else if (partition.PartitionedObject == secondList)
                {
                    secondPartition = partition;
                }
            }

            float firstSize = 0;
            float secondSize = 0;

            if (firstPartition != null && secondPartition != null)
            {
                firstSize = this.firstPartitioningSize ?? firstPartition.MaxWidthOrHeight;
                secondSize = this.secondPartitioningSize ?? secondPartition.MaxWidthOrHeight;
            }

            if (firstPartition?.axis == Axis.X && firstPartition?.axis == Axis.X)
            {
                // -1 to make it exclusive
                endExclusive = secondList.GetFirstAfter(first.X - firstSize / 2 - secondSize / 2, Axis.X, 0, secondList.Count) - 1;
                startInclusive = secondList.GetFirstAfter(first.X + firstSize / 2 + secondSize / 2, Axis.X, 0, secondList.Count);

                if (startInclusive > 0 && startInclusive == secondList.Count)
                {
                    startInclusive--;
                }

            }
            else if (firstPartition?.axis == Axis.Y && firstPartition?.axis == Axis.Y)
            {
                // -1 to make it exclusive
                endExclusive = secondList.GetFirstAfter(first.Y - firstSize / 2 - secondSize / 2, Axis.Y, 0, secondList.Count) - 1;
                startInclusive = secondList.GetFirstAfter(first.Y + firstSize / 2 + secondSize / 2, Axis.Y, 0, secondList.Count);
                if (startInclusive > 0 && startInclusive == secondList.Count)
                {
                    startInclusive--;
                }
            }
            else
            {
                startInclusive = secondList.Count - 1;
                endExclusive = -1;
            }
        }

        private void DoClosestCollision()
        {

            for (int i = firstList.Count - 1; i > -1; i--)
            {
                FirstCollidableT closestFirst = null;
                SecondCollidableT closestSecond = null;
                var closestDistanceSquared = float.PositiveInfinity;

                var first = firstList[i];
                int startInclusive;
                int endExclusive;
                GetCollisionStartAndInt(first, out startInclusive, out endExclusive);


                for (int j = startInclusive; j > endExclusive; j--)
                {
                    var second = secondList[j];

                    if (CollideConsideringSubCollisions(first, second))
                    {
                        var distanceVector = first.Position - second.Position;
                        var distanceSquared = distanceVector.X * distanceVector.X + distanceVector.Y * distanceVector.Y;

                        if(distanceSquared < closestDistanceSquared)
                        {
                            closestDistanceSquared = distanceSquared;
                            closestFirst = first;
                            closestSecond = second;
                        }
                    }
                }

                if(closestFirst != null)
                {
                    CollisionOccurred?.Invoke(closestFirst, closestSecond);
                }
            }
        }
    }

    #endregion
}
