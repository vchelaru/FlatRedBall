using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;
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
        BounceCollision,
        MoveSoftCollision
        //,PlatformerCollision
    }

    public enum LoopDirection
    {
        /// <summary>
        /// Indicates that loops will run from back to front (a reverse loop)
        /// </summary>
        BackToFront,
        /// <summary>
        /// Indicates that loops will run from front to back (normal loop)
        /// </summary>
        FrontToBack
    }

    #endregion

    #region Base Implementations

    /// <summary>
    /// Base implementation for object which provides collision logic between two objects.
    /// </summary>
    public abstract class CollisionRelationship : INameable
    {
        protected CollisionType CollisionType = CollisionType.EventOnlyCollision;

        protected float moveFirstMass;
        protected float moveSecondMass;
        protected float bounceElasticity;
        protected float softSeparationCoefficient;

        /// <summary>
        /// Whether a collision occurred this frame. This is set when the collision performs its collision logic in DoCollisions, which
        /// may get called automatically, or may get called manually if IsActive is set to false.
        /// </summary>
        public bool CollidedThisFrame { get; protected set; }

        public int DeepCollisionsThisFrame { get; set; }

        public CollisionLimit CollisionLimit { get; set; }

        public abstract object FirstAsObject { get; }
        public abstract object SecondAsObject { get; }

        protected internal float? firstPartitioningSize;
        protected internal float? secondPartitioningSize;

        public string FirstSubObjectName { get; protected set; }
        public string SecondSubObjectName { get; protected set; }


        /// <summary>
        /// Whether the CollisionManager autoamtically calls DoCollisions on this relationship every frame.
        /// If set to false, the CollisionManager will not call DoCollision, but DoCollision can be called manually
        /// to perform collision.
        /// </summary>
        public bool IsActive { get; set; } = true;


        public bool ArePhysicsAppliedAutomatically { get; set; } = true;

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
        // Update 4/27/2022 - we proably should replace the setter with a function that
        // takes a List<PartitionedValuesBase>. Furthermore, the setter is needed, but once
        // assigned, Partitions is only used internally in CollisionRelationship, so the public
        // property should be removed too. But...we don't want to break projects so we'll leave it
        // here for now, and have it store the internal value as a list so a for loop can be done without
        // allocating in a foreach.
        public IEnumerable<PartitionedValuesBase> Partitions
        {
            get => partitions;
            set
            {
                partitions = value as List<PartitionedValuesBase>;
            }
        }

        protected List<PartitionedValuesBase> partitions;

        public string Name { get; set; }

        #region Set Collision Type Methods

        // todo:
        // SetPlatformerCollision 

        public virtual void SetMoveCollision(float firstMass, float secondMass)
        {
            this.CollisionType = CollisionType.MoveCollision;
            this.moveFirstMass = firstMass;
            this.moveSecondMass = secondMass;
        }

        public virtual void SetBounceCollision(float firstMass, float secondMass, float elasticity)
        {
            this.CollisionType = CollisionType.BounceCollision;
            this.moveFirstMass = firstMass;
            this.moveSecondMass = secondMass;
            this.bounceElasticity = elasticity;
        }

        public virtual void SetMoveSoftCollision(float firstMass, float secondMass, float separationCoefficient)
        {
            this.CollisionType = CollisionType.MoveSoftCollision;
            this.moveFirstMass = firstMass;
            this.moveSecondMass = secondMass;
            this.softSeparationCoefficient = separationCoefficient;
        }

        public void SetEventOnlyCollision()
        {
            this.CollisionType = CollisionType.EventOnlyCollision;
        }

        #endregion

        /// <summary>
        /// Performs the collision logic and returns whether a collision has occurred. This method
        /// does not need to be called if the CollisionRelationship is part of the CollisionManager
        /// and if its IsActive is set to true (both of which are the most common setup).
        /// </summary>
        /// <returns>Whether collisions occurred this frame.</returns>
        public abstract bool DoCollisions();

        public override string ToString()
        {
            return Name;
        }
    }

    public abstract class CollisionRelationship<FirstCollidableT, SecondCollidableT> : CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
    {
        #region Fields/Properties
        protected Func<FirstCollidableT, Circle> firstSubCollisionCircle;
        protected Func<FirstCollidableT, AxisAlignedRectangle> firstSubCollisionRectangle;
        protected Func<FirstCollidableT, Polygon> firstSubCollisionPolygon;
        protected Func<FirstCollidableT, Line> firstSubCollisionLine;
        protected Func<FirstCollidableT, ICollidable> firstSubCollisionCollidable;

        protected Func<SecondCollidableT, Circle> secondSubCollisionCircle;
        protected Func<SecondCollidableT, AxisAlignedRectangle> secondSubCollisionRectangle;
        protected Func<SecondCollidableT, Polygon> secondSubCollisionPolygon;
        protected Func<SecondCollidableT, Line> secondSubCollisionLine;
        protected Func<SecondCollidableT, ICollidable> secondSubCollisionCollidable;

        public Action<FirstCollidableT, SecondCollidableT> CollisionOccurred;

        #endregion

        #region Set SubCollision Methods

        public void SetFirstSubCollision(Func<FirstCollidableT, Circle> subCollisionFunc, string subObjectName = null) { firstSubCollisionCircle = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, AxisAlignedRectangle> subCollisionFunc, string subObjectName = null) { firstSubCollisionRectangle = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Polygon> subCollisionFunc, string subObjectName = null) { firstSubCollisionPolygon = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Line> subCollisionFunc, string subObjectName = null) { firstSubCollisionLine = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, ICollidable> subCollisionFunc, string subObjectName = null) { firstSubCollisionCollidable = subCollisionFunc; FirstSubObjectName = subObjectName; }

        public void SetSecondSubCollision(Func<SecondCollidableT, Circle> subCollisionFunc, string subObjectName = null) { secondSubCollisionCircle = subCollisionFunc; SecondSubObjectName = subObjectName; }
        public void SetSecondSubCollision(Func<SecondCollidableT, AxisAlignedRectangle> subCollisionFunc, string subObjectName = null) { secondSubCollisionRectangle = subCollisionFunc; SecondSubObjectName = subObjectName; }
        public void SetSecondSubCollision(Func<SecondCollidableT, Polygon> subCollisionFunc, string subObjectName = null) { secondSubCollisionPolygon = subCollisionFunc; SecondSubObjectName = subObjectName; }
        // todo - I added support for the Line type for the first because it was needed in the June 2019 monthly. One day I may add the 2nd here, but for now
        // I'm going to just add the first
        //public void SetSecondSubCollision(Func<SecondCollidableT, Line> subCollisionLine) { secondSubCollisionLine = subCollisionFunc; }
        public void SetSecondSubCollision(Func<SecondCollidableT, ICollidable> subCollisionFunc, string subObjectName = null) { secondSubCollisionCollidable = subCollisionFunc; SecondSubObjectName = subObjectName; }

        #endregion

        public bool DoCollisionPhysics(FirstCollidableT first, SecondCollidableT second)
        {
            return DoCollisionPhysicsInner(first, second, false);
        }

        protected bool DoCollisionPhysicsInner(FirstCollidableT first, SecondCollidableT second, bool eventOnly)
        {
#if DEBUG
            if (first == second)
            {
                throw new InvalidOperationException($"Cannot collide a {first.GetType()} named {first.Name} against itself in a CollisionRelationship " +
                    $"named {this.Name}");
            }
#endif

            this.DeepCollisionsThisFrame++;

            if (CollisionType == CollisionType.EventOnlyCollision || eventOnly)
            {
                return CollideAgainstConsiderSubCollisionEventOnly(first, second);
            }
            else if (CollisionType == CollisionType.MoveCollision)
            {
                return CollideAgainstConsiderSubCollisionMove(first, second);
            }
            else if (CollisionType == CollisionType.BounceCollision)
            {
                return CollideAgainstConsiderSubCollisionBounce(first, second);
            }
            else if(CollisionType == CollisionType.MoveSoftCollision)
            {
                return CollideAgainstConsiderSubCollisionMoveSoft(first, second);
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
            else if(firstSubCollisionLine != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionLine(first).CollideAgainst(secondSubCollisionCircle(second));
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionLine(first).CollideAgainst(secondSubCollisionRectangle(second));
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionLine(first).CollideAgainst(secondSubCollisionPolygon(second));
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainst(firstSubCollisionLine(first));
                }
                else
                {
                    return second.CollideAgainst(firstSubCollisionLine(first));
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
            else if (firstSubCollisionLine != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionLine(first).CollideAgainstMove(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionLine(first).CollideAgainstMove(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionLine(first).CollideAgainstMove(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstMove(firstSubCollisionLine(first), moveSecondMass, moveFirstMass);
                }
                else
                {
                    return second.CollideAgainstMove(firstSubCollisionLine(first), moveSecondMass, moveFirstMass);
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

        private bool CollideAgainstConsiderSubCollisionMoveSoft(FirstCollidableT first, SecondCollidableT second)
        {
            if (firstSubCollisionCircle != null)
            {
                //return firstSubCollisionCircle(first).CollideAgainstMoveSoft(secondSub)
                //throw new NotImplementedException();
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionCircle(first).CollideAgainstMoveSoft(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass, softSeparationCoefficient);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    throw new NotImplementedException();
                    //return firstSubCollisionCircle(first).CollideAgainstMoveSoft(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass, softSeparationCoefficient);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    throw new NotImplementedException();
                    //return firstSubCollisionCircle(first).CollideAgainstMoveSoft(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass, softSeparationCoefficient);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    throw new NotImplementedException();
                    // invert it since ICollidable has to be the one calling it
                    //return secondSubCollisionCollidable(second).CollideAgainstMoveSoft(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass, softSeparationCoefficient);
                }
                else
                {
                    throw new NotImplementedException();
                    //return second.CollideAgainstMoveSoft(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass, softSeparationCoefficient);
                }

            }
            else if (firstSubCollisionRectangle != null)
            {
                throw new NotImplementedException();
            }
            else if (firstSubCollisionPolygon != null)
            {
                throw new NotImplementedException();
            }
            else if (firstSubCollisionLine != null)
            {
                throw new NotImplementedException();
            }
            else if (firstSubCollisionCollidable != null)
            {
                throw new NotImplementedException();
            }
            else
            {
                if (secondSubCollisionCircle != null)
                {
                    throw new NotImplementedException();
                }
                else if (secondSubCollisionRectangle != null)
                {
                    throw new NotImplementedException();
                }
                else if (secondSubCollisionPolygon != null)
                {
                    throw new NotImplementedException();
                }
                else if (secondSubCollisionCollidable != null)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    //return second.CollideAgainstMove(first, moveSecondMass, moveFirstMass);
                    return second.Collision.CollideAgainstMoveSoft(first.Collision, moveSecondMass, moveFirstMass, softSeparationCoefficient);
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
            else if (firstSubCollisionLine != null)
            {
                if (secondSubCollisionCircle != null)
                {
                    return firstSubCollisionLine(first).CollideAgainstBounce(secondSubCollisionCircle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionRectangle != null)
                {
                    return firstSubCollisionLine(first).CollideAgainstBounce(secondSubCollisionRectangle(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionPolygon != null)
                {
                    return firstSubCollisionLine(first).CollideAgainstBounce(secondSubCollisionPolygon(second), moveFirstMass, moveSecondMass, bounceElasticity);
                }
                else if (secondSubCollisionCollidable != null)
                {
                    // invert it since ICollidable has to be the one calling it
                    return secondSubCollisionCollidable(second).CollideAgainstBounce(firstSubCollisionLine(first), moveSecondMass, moveFirstMass, bounceElasticity);
                }
                else
                {
                    return second.CollideAgainstBounce(firstSubCollisionLine(first), moveSecondMass, moveFirstMass, bounceElasticity);
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


        public override bool DoCollisions()
        {
            var didCollisionOccur = false;
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                skippedFrames = 0;
                // collision limit doesn't do anything here, since it's only 1 vs 1
                if (DoCollisionPhysicsInner(first, second, !ArePhysicsAppliedAutomatically))
                {
                    CollisionOccurred?.Invoke(first, second);
                    didCollisionOccur = true;

                    first.ItemsCollidedAgainst.Add(second.Name);
                    first.ObjectsCollidedAgainst.Add(second);
                    second.ItemsCollidedAgainst.Add(first.Name);
                    second.ObjectsCollidedAgainst.Add(first);
                }
            }

            this.CollidedThisFrame = didCollisionOccur;

            return didCollisionOccur;
        }
    }

    #endregion

    #region Delegate-based Single vs Single

    public abstract class DelegateCollisionRelationshipBase<First, Second> : CollisionRelationship
    {
        protected First first;
        protected Second second;

        public override object FirstAsObject => first;
        public override object SecondAsObject => second;
    }

    public class DelegateCollisionRelationship<First, Second> : DelegateCollisionRelationshipBase<First, Second>
    {
        public Func<First, Second, bool> CollisionFunction { get; set; }

        public Action<First, Second> CollisionOccurred;


        public DelegateCollisionRelationship(First first, Second second)
        {
            this.first = first;
            this.second = second;
        }


        public override bool DoCollisions()
        {
            var collided = CollisionFunction(first, second);

            if(collided)
            {
                CollisionOccurred?.Invoke(first, second);

                if(first is ICollidable firstCollidable)
                {
                    firstCollidable.ObjectsCollidedAgainst.Add(second);
                    if ( second is INameable secondNameable)
                    {
                        firstCollidable.ItemsCollidedAgainst.Add(secondNameable.Name);
                    }
                }
                if(second is ICollidable secondCollidable )
                {
                    secondCollidable.ObjectsCollidedAgainst.Add(first);
                    if (first is INameable firstNameable)
                    {
                        secondCollidable.ItemsCollidedAgainst.Add(firstNameable.Name);
                    }
                }
            }

            return collided;
        }
    }

    #endregion

    #region Delegate-based Single vs List

    public class DelegateSingleVsListRelationship<First, SecondCollidableT> : CollisionRelationship 
        where SecondCollidableT : PositionedObject
    {
        First singleObject;
        PositionedObjectList<SecondCollidableT> list;

        public Func<First, SecondCollidableT, bool> CollisionFunction { get; set; }

        public override object FirstAsObject => singleObject;
        public override object SecondAsObject => list;

        public Action<First, SecondCollidableT> CollisionOccurred;

        public DelegateSingleVsListRelationship(First first, PositionedObjectList<SecondCollidableT> second)
        {
            this.singleObject = first;
            this.list = second;
        }

        public override bool DoCollisions()
        {
            bool didCollisionOccur = false;
            if(skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else if(list.Count > 0)
            {
                skippedFrames = 0;
                for (int i = list.Count - 1; i > -1; i--)
                {
                    var atI = list[i];


                    if (CollisionFunction(singleObject, atI))
                    {
                        CollisionOccurred?.Invoke(singleObject, atI);
                        didCollisionOccur = true;
                        // Collision Limit doesn't do anything here

                        if (singleObject is ICollidable firstCollidable)
                        {
                            firstCollidable.ObjectsCollidedAgainst.Add(atI);

                            if (atI is INameable secondNameable)
                            {
                                firstCollidable.ItemsCollidedAgainst.Add(secondNameable.Name);
                            }
                        }
                        if (atI is ICollidable secondCollidable)
                        {
                            secondCollidable.ObjectsCollidedAgainst.Add(singleObject);
                            if (singleObject is INameable firstNameable)
                            {
                                secondCollidable.ItemsCollidedAgainst.Add(firstNameable.Name);
                            }
                        }
                    }
                }
            }

            return didCollisionOccur;
        }
    }

    #endregion

    #region Delegate-based List vs Single

    public class DelegateListVsSingleRelationship<FirstCollidableT, Second> : CollisionRelationship
        where FirstCollidableT : PositionedObject
    {
        PositionedObjectList<FirstCollidableT> list;
        Second singleObject;

        public Func<FirstCollidableT, Second, bool> CollisionFunction { get; set; }

        public override object FirstAsObject => list;

        public override object SecondAsObject => singleObject;

        public Action<FirstCollidableT, Second> CollisionOccurred;

        public DelegateListVsSingleRelationship(PositionedObjectList<FirstCollidableT> first, Second second)
        {
            this.list = first;
            this.singleObject = second;
        }

        public override bool DoCollisions()
        {
            bool didCollisionOccur = false;
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else if (list.Count > 0)
            {
                skippedFrames = 0;
                //int startInclusive;
                //int endExclusive;

                //GetCollisionStartAndInt(out startInclusive, out endExclusive);

                for (int i = list.Count - 1; i > -1; i--)
                {
                    var atI = list[i];


                    if (CollisionFunction(atI, singleObject))
                    {
                        CollisionOccurred?.Invoke(atI, singleObject);
                        didCollisionOccur = true;
                        // Collision Limit doesn't do anything here


                        if (atI is ICollidable firstCollidable)
                        {
                            firstCollidable.ObjectsCollidedAgainst.Add(singleObject);
                            if(singleObject is INameable secondNameable)
                            {
                                firstCollidable.ItemsCollidedAgainst.Add(secondNameable.Name);
                            }
                        }
                        if (singleObject is ICollidable secondCollidable)
                        {
                            secondCollidable.ObjectsCollidedAgainst.Add(atI);

                            if (atI is INameable firstNameable)
                            {
                                secondCollidable.ItemsCollidedAgainst.Add(firstNameable.Name);
                            }
                        }
                    }
                }
            }

            return didCollisionOccur;
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

            for(int i = 0; i < partitions.Count; i++)
            {
                var partition = partitions[i];
                if (partition.PartitionedObject == singleObject)
                {
                    firstPartition = partition;
                }
                else if (partition.PartitionedObject == list)
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

            if (firstPartition?.axis == Axis.X && secondPartition?.axis == Axis.X)
            {
                // -1 to make it exclusive
                endExclusive = list.GetFirstAfter(singleObject.X - firstSize / 2 - secondSize / 2, Axis.X, 0, list.Count) - 1;
                startInclusive = list.GetFirstAfter(singleObject.X + firstSize / 2 + secondSize / 2, Axis.X, 0, list.Count);

                if (startInclusive > 0 && startInclusive == list.Count)
                {
                    startInclusive--;
                }

            }
            else if (firstPartition?.axis == Axis.Y && secondPartition?.axis == Axis.Y)
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

        public override bool DoCollisions()
        {
            var didCollisionOccur = false;

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
                        if (DoCollisionPhysicsInner(singleObject, atI, !ArePhysicsAppliedAutomatically))
                        {
                            CollisionOccurred?.Invoke(singleObject, atI);
                            didCollisionOccur = true;

                            singleObject.ItemsCollidedAgainst.Add(atI.Name);
                            singleObject.ObjectsCollidedAgainst.Add(atI);
                            atI.ItemsCollidedAgainst.Add(singleObject.Name);
                            atI.ObjectsCollidedAgainst.Add(singleObject);

                            if (CollisionLimit == CollisionLimit.First)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            this.CollidedThisFrame = didCollisionOccur;

            return didCollisionOccur;
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
                if (DoCollisionPhysicsInner(singleObject, atI, !ArePhysicsAppliedAutomatically))
                {
                    var distanceVector = singleObject.Position - atI.Position;
                    var distanceSquared = distanceVector.X * distanceVector.X + distanceVector.Y * distanceVector.Y;

                    if (distanceSquared < closestDistanceSquared)
                    {
                        closestDistanceSquared = distanceSquared;
                        closestFirst = singleObject;
                        closestSecond = atI;
                    }

                }
            }

            if (closestFirst != null)
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

            for (int i = 0; i < partitions.Count; i++)
            {
                var partition = partitions[i];
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

            if (firstPartition != null && secondPartition != null)
            {
                firstSize = this.firstPartitioningSize ?? firstPartition.MaxWidthOrHeight;
                secondSize = this.secondPartitioningSize ?? secondPartition.MaxWidthOrHeight;
            }

            if (firstPartition?.axis == Axis.X && secondPartition?.axis == Axis.X)
            {
                // -1 to make it exclusive
                endExclusive = list.GetFirstAfter(singleObject.X - firstSize / 2 - secondSize / 2, Axis.X, 0, list.Count) - 1;
                startInclusive = list.GetFirstAfter(singleObject.X + firstSize / 2 + secondSize / 2, Axis.X, 0, list.Count);

                if (startInclusive > 0 && startInclusive == list.Count)
                {
                    startInclusive--;
                }
            }
            else if (firstPartition?.axis == Axis.Y && secondPartition?.axis == Axis.Y)
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


        public override bool DoCollisions()
        {
            bool didCollisionOccur = false;
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else if(list.Count > 0)
            {
                skippedFrames = 0;
                int startInclusive;
                int endExclusive;

                GetCollisionStartAndInt(out startInclusive, out endExclusive);

                for (int i = startInclusive; i > endExclusive; i--)
                {
                    var atI = list[i];
                    if (DoCollisionPhysicsInner(atI, singleObject, !ArePhysicsAppliedAutomatically))
                    {
                        CollisionOccurred?.Invoke(atI, singleObject);
                        didCollisionOccur = true;
                        // Collision Limit doesn't do anything here


                        singleObject.ItemsCollidedAgainst.Add(atI.Name);
                        singleObject.ObjectsCollidedAgainst.Add(atI);
                        atI.ItemsCollidedAgainst.Add(singleObject.Name);
                        atI.ObjectsCollidedAgainst.Add(singleObject);
                    }
                }
            }

            this.CollidedThisFrame = didCollisionOccur;

            return didCollisionOccur;
        }
    }

    #endregion

    #region List Always Colliding 
    public class AlwaysCollidingListCollisionRelationship<FirstCollidableT> : CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable
    {
        PositionedObjectList<FirstCollidableT> list;
        public override object FirstAsObject => list;

        public override object SecondAsObject => null;

        public Action<FirstCollidableT> CollisionOccurred;

        public AlwaysCollidingListCollisionRelationship(PositionedObjectList<FirstCollidableT> list)
        {
            this.list = list;
        }

        public override bool DoCollisions()
        {
            for(int i = list.Count -1; i > -1; i--)
            {
                CollisionOccurred?.Invoke(list[i]);
            }
            return true;
        }
    }
    #endregion


    #region List vs. List

    public enum ListVsListLoopingMode
    {
        /// <summary>
        /// When a list is colliding against itself, the indexes will be set such
        /// that if object A collides against object B, then object B will not also try to collide
        /// against object A in the same frame. This is desirable when performing collision
        /// between two objects with no subcollisions, or between two objects with the same subcollision.
        /// </summary>
        PreventDoubleChecksPerFrame,
        /// <summary>
        /// When a list is colliding against itself, the indexes will be set such
        /// that if object A collides against object B, then object B will still attempt
        /// to collide against object A. This is desirable when performing collision between
        /// two objects with different subcollisions, such as a player weapon vs the player body.
        /// </summary>
        AllowDoubleChecksPerFrame

    }

    public class ListVsListRelationship<FirstCollidableT, SecondCollidableT> :
        CollisionRelationship<FirstCollidableT, SecondCollidableT>
        where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable
    {
        public class CollisionParameters
        {
            public FirstCollidableT ItemInFirstList;
            public int FirstIndex;
            public PartitionedValuesBase FirstPartition;
            public PartitionedValuesBase SecondPartition;

            public Axis? PartitioningAxis = null;
            public float FirstHalfSize;
            public float SecondHalfSize;

            public void CalculateValuesForCollision(CollisionRelationship relationship)
            {
                if (FirstPartition?.axis == Axis.X && SecondPartition?.axis == Axis.X)
                {
                    PartitioningAxis = Axis.X;
                }
                else if (FirstPartition?.axis == Axis.Y && SecondPartition?.axis == Axis.Y)
                {
                    PartitioningAxis = Axis.Y;
                }
                else
                {
                    PartitioningAxis = null;
                }

                FirstHalfSize = (relationship.firstPartitioningSize ?? FirstPartition?.MaxWidthOrHeight ?? 0) / 2.0f;
                SecondHalfSize = (relationship.secondPartitioningSize ?? SecondPartition?.MaxWidthOrHeight ?? 0) / 2.0f;

            }
        }
        // instantiate one at class scope to prevent tons of allocations if done in a activity
        CollisionParameters parameters = new CollisionParameters();

        public ListVsListLoopingMode ListVsListLoopingMode { get; set; } = ListVsListLoopingMode.PreventDoubleChecksPerFrame;

        PositionedObjectList<FirstCollidableT> firstList;
        PositionedObjectList<SecondCollidableT> secondList;
        private int i;

        public override object FirstAsObject => firstList;
        public override object SecondAsObject => secondList;

        public LoopDirection LoopDirection { get; set; } = LoopDirection.BackToFront;

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
#if DEBUG
            if (firstList == null)
            {
                throw new NullReferenceException($"The first list (of type {typeof(FirstCollidableT)}) is null. This needs to be set");
            }
            if (secondList == null)
            {
                throw new NullReferenceException($"The second list (of type {typeof(SecondCollidableT)}) is null. This needs to be set");
            }
#endif
            this.firstList = firstList;
            this.secondList = secondList;

        }

        public override bool DoCollisions()
        {
#if DEBUG
            if(firstList == null)
            {
                throw new NullReferenceException($"The first list (of type {typeof(FirstCollidableT)}) is null. This needs to be set");
            }
            if(secondList == null)
            {
                throw new NullReferenceException($"The second list (of type {typeof(SecondCollidableT)}) is null. This needs to be set");
            }
#endif

            bool collisionOccurred = false;
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                skippedFrames = 0;

                /////////////////early out here so we don't have to do 0-checks later
                if (firstList.Count == 0 || secondList.Count == 0)
                {
                    return false;
                }

                if (CollisionLimit == CollisionLimit.Closest)
                {
                    DoClosestCollision();
                }
                else
                {
                    GetPartitions(out parameters.FirstPartition, out parameters.SecondPartition);
                    parameters.CalculateValuesForCollision(this);

                    float? maxDistanceOnSecondaryAxis = null;
                    if (parameters.PartitioningAxis != null)
                    {
                        maxDistanceOnSecondaryAxis = parameters.FirstHalfSize + parameters.SecondHalfSize;
                        // 1.41421... is the sqrt 2, for diagonal
                        //maxDistanceSquared = ((parameters.FirstHalfSize + parameters.SecondHalfSize) * 1.422f);
                        //maxDistanceSquared *= maxDistanceSquared;
                    }

                    // lots of copy/pasted code here, unrolled for maximum performance
                    if (parameters.PartitioningAxis == Axis.X)
                    {
                        collisionOccurred = DoPartitionedXCollision(maxDistanceOnSecondaryAxis);
                    }
                    else if (parameters.PartitioningAxis == Axis.Y)
                    {
                        collisionOccurred = DoPartitionedYCollision(maxDistanceOnSecondaryAxis);
                    }
                    else
                    {
                        collisionOccurred = DoNoPartitionCollision(collisionOccurred);
                    }
                }
            }

            this.CollidedThisFrame = collisionOccurred;

            return collisionOccurred;
        }

        private bool DoNoPartitionCollision(bool collisionOccurred)
        {
            int j = 0;
            int secondListStartInclusive;
            int secondListEndExclusive;

            if(LoopDirection == LoopDirection.FrontToBack)
            {
                secondListStartInclusive = 0;
                secondListEndExclusive = secondList.Count;
                for (int i = 0; i < firstList.Count; i++)
                {
                    var first = firstList[i];

                    if (firstList == (object)secondList && ListVsListLoopingMode == ListVsListLoopingMode.PreventDoubleChecksPerFrame)
                    {
                        secondListStartInclusive = i + 1;
                    }

                    for (j = secondListStartInclusive; j < secondListEndExclusive; j++)
                    {
                        var second = secondList[j];

                        // first != second needed since the two may be checked if allowing duplicate collisions per frame
                        if (first != second && DoCollisionPhysicsInner(first, second, !ArePhysicsAppliedAutomatically))
                        {
                            CollisionOccurred?.Invoke(first, second);
                            collisionOccurred = true;

                            first.ItemsCollidedAgainst.Add(second.Name);
                            first.ObjectsCollidedAgainst.Add(second);
                            second.ItemsCollidedAgainst.Add(first.Name);
                            second.ObjectsCollidedAgainst.Add(first);
                            if (CollisionLimit == CollisionLimit.First)
                            {
                                break;
                            }
                        }
                    }
                }

            }
            else // back to front
            {
                secondListStartInclusive = secondList.Count - 1;
                secondListEndExclusive = -1;
                for (int i = firstList.Count - 1; i > -1; i--)
                {
                    var first = firstList[i];

                    if(firstList == (object)secondList && ListVsListLoopingMode == ListVsListLoopingMode.PreventDoubleChecksPerFrame)
                    {
                        secondListStartInclusive = i - 1;
                    }

                    for (j = secondListStartInclusive; j > secondListEndExclusive; j--)
                    {
                        // collision can remove more than one, so let's be safe:
                        if(j < secondList.Count)
                        {
                            var second = secondList[j];

                            // first != second needed since the two may be checked if allowing duplicate collisions per frame
                            if (first != second && DoCollisionPhysicsInner(first, second, !ArePhysicsAppliedAutomatically))
                            {
                                CollisionOccurred?.Invoke(first, second);
                                collisionOccurred = true;
                                if (CollisionLimit == CollisionLimit.First)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }


            return collisionOccurred;
        }

        private bool DoPartitionedYCollision(float? maxDistanceOnSecondaryAxis)
        {
            if (this.LoopDirection == LoopDirection.FrontToBack)
            {
                throw new NotImplementedException("Bug Vic!");
            }

            bool collisionOccurred = false;
            int j = 0;

            int startInclusive;
            int endExclusive;
            for (int i = firstList.Count - 1; i > -1; i--)
            {
                var first = firstList[i];
                parameters.ItemInFirstList = first;
                parameters.FirstIndex = i;

                GetSecondListCollisionStartAndInt(parameters, out startInclusive, out endExclusive);

                for (j = startInclusive; j > endExclusive; j--)
                {
                    var second = secondList[j];

                    var distanceX =
                        first.Position.X - second.Position.X;

                    if (distanceX < -maxDistanceOnSecondaryAxis || distanceX > maxDistanceOnSecondaryAxis)
                    {
                        continue;
                    }

                    if (DoCollisionPhysicsInner(first, second, !ArePhysicsAppliedAutomatically))
                    {
                        CollisionOccurred?.Invoke(first, second);
                        collisionOccurred = true;

                        first.ItemsCollidedAgainst.Add(second.Name);
                        first.ObjectsCollidedAgainst.Add(second);
                        second.ItemsCollidedAgainst.Add(first.Name);
                        second.ObjectsCollidedAgainst.Add(first);

                        if (CollisionLimit == CollisionLimit.First)
                        {
                            break;
                        }
                    }
                }
            }

            return collisionOccurred;
        }

        private bool DoPartitionedXCollision(float? maxDistanceOnSecondaryAxis)
        {
            bool collisionOccurred = false;
            int startInclusive;
            int endExclusive;

            int j = 0;

            if(this.LoopDirection == LoopDirection.FrontToBack)
            {
                throw new NotImplementedException("Bug Vic!");
            }

            for (int i = firstList.Count - 1; i > -1; i--)
            {
                var first = firstList[i];
                parameters.ItemInFirstList = first;
                parameters.FirstIndex = i;

                GetSecondListCollisionStartAndInt(parameters, out startInclusive, out endExclusive);

                for (j = startInclusive; j > endExclusive; j--)
                {
                    // a collision could destroy everything:
                    if(j >= secondList.Count)
                    {
                        continue;
                    }
                    var second = secondList[j];

                    var distanceY =
                        first.Position.Y - second.Position.Y;

                    if (distanceY < -maxDistanceOnSecondaryAxis || distanceY > maxDistanceOnSecondaryAxis || first == second)
                    {
                        continue;
                    }

                    if (DoCollisionPhysicsInner(first, second, !ArePhysicsAppliedAutomatically))
                    {
                        CollisionOccurred?.Invoke(first, second);
                        collisionOccurred = true;

                        first.ItemsCollidedAgainst.Add(second.Name);
                        first.ObjectsCollidedAgainst.Add(second);
                        second.ItemsCollidedAgainst.Add(first.Name);
                        second.ObjectsCollidedAgainst.Add(first);

                        if (CollisionLimit == CollisionLimit.First)
                        {
                            break;
                        }
                    }
                }
            }

            return collisionOccurred;
        }

        private void GetSecondListCollisionStartAndInt(CollisionParameters parameters, out int startInclusive, out int endExclusive)
        {
            // By default the last possible bound is set by the second list...
            int lastExclusiveBound = secondList.Count;
            // .. but if the two lists are the same, then we don't want to go past the current item
            // since we collide back-to-front
            if (this.firstList == (object)this.secondList && this.ListVsListLoopingMode == ListVsListLoopingMode.PreventDoubleChecksPerFrame)
            {
                lastExclusiveBound = parameters.FirstIndex;
            }

            if (parameters.PartitioningAxis == Axis.X)
            {
                // -1 to make it exclusive
                endExclusive = secondList.GetFirstAfter(parameters.ItemInFirstList.Position.X - parameters.FirstHalfSize - parameters.SecondHalfSize, Axis.X, 0, lastExclusiveBound) - 1;
                if (parameters.FirstPartition.PartitionedObject == parameters.SecondPartition.PartitionedObject && this.ListVsListLoopingMode == ListVsListLoopingMode.PreventDoubleChecksPerFrame)
                {
                    startInclusive = parameters.FirstIndex - 1;
                }
                else
                {
                    startInclusive = secondList.GetFirstAfter(parameters.ItemInFirstList.Position.X + parameters.FirstHalfSize + parameters.SecondHalfSize, Axis.X, 0, lastExclusiveBound);
                }

                if (startInclusive > 0 && startInclusive == secondList.Count)
                {
                    startInclusive--;
                }

            }
            else if (parameters.PartitioningAxis == Axis.Y)
            {
                // -1 to make it exclusive
                endExclusive = secondList.GetFirstAfter(parameters.ItemInFirstList.Position.Y - parameters.FirstHalfSize - parameters.SecondHalfSize, Axis.Y, 0, lastExclusiveBound) - 1;

                if (parameters.FirstPartition.PartitionedObject == parameters.SecondPartition.PartitionedObject && this.ListVsListLoopingMode == ListVsListLoopingMode.PreventDoubleChecksPerFrame)
                {
                    startInclusive = parameters.FirstIndex - 1;
                }
                else
                {
                    startInclusive = secondList.GetFirstAfter(parameters.ItemInFirstList.Position.Y + parameters.FirstHalfSize + parameters.SecondHalfSize, Axis.Y, 0, lastExclusiveBound);
                }
                if (startInclusive > 0 && startInclusive == secondList.Count)
                {
                    startInclusive--;
                }
            }
            else
            {
                startInclusive = lastExclusiveBound - 1;
                endExclusive = -1;
            }

        }

        private void DoClosestCollision()
        {
            int startInclusive, endExclusive;
            GetPartitions(out parameters.FirstPartition, out parameters.SecondPartition);
            parameters.CalculateValuesForCollision(this);

            for (int i = firstList.Count - 1; i > -1; i--)
            {
                var first = firstList[i];

                FirstCollidableT closestFirst = null;
                SecondCollidableT closestSecond = null;
                var closestDistanceSquared = float.PositiveInfinity;

                parameters.ItemInFirstList = first;

                GetSecondListCollisionStartAndInt(parameters, out startInclusive, out endExclusive);


                for (int j = startInclusive; j > endExclusive; j--)
                {
                    var second = secondList[j];
                    if (DoCollisionPhysicsInner(first, second, !ArePhysicsAppliedAutomatically))
                    {
                        var distanceVector = first.Position - second.Position;
                        var distanceSquared = distanceVector.X * distanceVector.X + distanceVector.Y * distanceVector.Y;

                        if (distanceSquared < closestDistanceSquared)
                        {
                            closestDistanceSquared = distanceSquared;
                            closestFirst = first;
                            closestSecond = second;
                        }
                    }
                }

                if (closestFirst != null)
                {
                    CollisionOccurred?.Invoke(closestFirst, closestSecond);

                    closestFirst.ItemsCollidedAgainst.Add(closestSecond.Name);
                    closestFirst.ObjectsCollidedAgainst.Add(closestSecond);
                    closestSecond.ItemsCollidedAgainst.Add(closestFirst.Name);
                    closestSecond.ObjectsCollidedAgainst.Add(closestFirst);
                }
            }
        }

        private void GetPartitions(out PartitionedValuesBase firstPartition, out PartitionedValuesBase secondPartition)
        {
            firstPartition = null;
            secondPartition = null;

            for (int i = 0; i < partitions.Count; i++)
            {
                var partition = partitions[i];
                if (partition.PartitionedObject == firstList)
                {
                    firstPartition = partition;
                }
                // Don't else if this, because it could be a list against itself, so
                // it should be both the first and second partition
                //else if (partition.PartitionedObject == secondList)
                if (partition.PartitionedObject == secondList)
                {
                    secondPartition = partition;
                }
            }
        }
    }

    #endregion

    #region Delegate-based List vs List

    public class DelegateListVsListRelationship<FirstCollidableT, SecondCollidableT> : 
        DelegateCollisionRelationshipBase<PositionedObjectList<FirstCollidableT>, PositionedObjectList<SecondCollidableT>>
        where FirstCollidableT : PositionedObject, ICollidable where SecondCollidableT : PositionedObject, ICollidable

    {
        public ListVsListLoopingMode ListVsListLoopingMode { get; set; } = ListVsListLoopingMode.PreventDoubleChecksPerFrame;

        public LoopDirection LoopDirection { get; set; }

        public Func<FirstCollidableT, SecondCollidableT, bool> CollisionFunction { get; set; }

        public Action<FirstCollidableT, SecondCollidableT> CollisionOccurred;


        public DelegateListVsListRelationship(PositionedObjectList<FirstCollidableT> first, PositionedObjectList<SecondCollidableT> second)
        {
            this.first = first;
            this.second = second;

        }

        public override bool DoCollisions()
        {
#if DEBUG
            if (first == null)
            {
                throw new NullReferenceException("The first list is null. This needs to be set");
            }
            if (second == null)
            {
                throw new NullReferenceException("The second list is null. This needs to be set");
            }
#endif
            bool collisionOccurred = false;
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                skippedFrames = 0;

                /////////////////early out here so we don't have to do 0-checks later
                if (first.Count == 0 || second.Count == 0)
                {
                    return false;
                }


                // todo - partition...

                if(LoopDirection == LoopDirection.FrontToBack)
                {
                    int j = 0;
                    int secondListStartInclusive = 0;
                    for (int i = 0; i < first.Count; i++)
                    {
                        // items could be removed, so need to do extra checks on front-to-back
                        var firstItem = first[i];


                        if (first == (object)second && ListVsListLoopingMode == ListVsListLoopingMode.PreventDoubleChecksPerFrame)
                        {
                            secondListStartInclusive = i + 1;
                        }


                        for (j = secondListStartInclusive; j < second.Count; j++)
                        {
                            var secondItem = second[j];

                            var collided = CollisionFunction(firstItem, secondItem);

                            if (collided)
                            {
                                CollisionOccurred?.Invoke(firstItem, secondItem);
                                collisionOccurred = true;

                                if(firstItem is ICollidable first)
                                {
                                    first.ObjectsCollidedAgainst.Add(secondItem);
                                    if (secondItem is INameable second)
                                    {
                                        first.ItemsCollidedAgainst.Add(second.Name);
                                    }
                                }
                                if(secondItem is ICollidable secondCollidable)
                                {
                                    secondCollidable.ObjectsCollidedAgainst.Add(firstItem);
                                    if (firstItem is INameable firstNameable)
                                    {
                                        secondCollidable.ItemsCollidedAgainst.Add(firstNameable.Name);
                                    }
                                }
                            }
                        }
                    }
                }
                else // back to front
                {
                    int j = 0;

                    var secondListStartInclusive = second.Count - 1;
                    var secondListEndExclusive = -1;

                    for (int i = first.Count - 1; i > -1; i--)
                    {
                        var firstItem = first[i];

                        if (first == (object)second && ListVsListLoopingMode == ListVsListLoopingMode.PreventDoubleChecksPerFrame)
                        {
                            secondListStartInclusive = i - 1;
                        }

                        for (j = secondListStartInclusive; j > secondListEndExclusive; j--)
                        {
                            var secondItem = second[j];
                            var collided = CollisionFunction(firstItem, secondItem);


                            if (collided)
                            {
                                CollisionOccurred?.Invoke(firstItem, secondItem);
                                collisionOccurred = true;

                                if (firstItem is ICollidable first)
                                {
                                    first.ObjectsCollidedAgainst.Add(secondItem);

                                    if (secondItem is INameable second)
                                    {
                                        first.ItemsCollidedAgainst.Add(second.Name);
                                    }
                                }
                                if (secondItem is ICollidable secondCollidable )
                                {
                                    secondCollidable.ObjectsCollidedAgainst.Add(firstItem);
                                    if (firstItem is INameable firstNameable)
                                    {
                                        secondCollidable.ItemsCollidedAgainst.Add(firstNameable.Name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return collisionOccurred;
        }
    }


    #endregion

    #region Single vs ShapeCollection

    public class PositionedObjectVsShapeCollection<FirstCollidableT> : CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable
    {
        FirstCollidableT singleObject;
        ShapeCollection shapeCollection;

        protected Func<FirstCollidableT, Circle> firstSubCollisionCircle;
        protected Func<FirstCollidableT, AxisAlignedRectangle> firstSubCollisionRectangle;
        protected Func<FirstCollidableT, Polygon> firstSubCollisionPolygon;
        protected Func<FirstCollidableT, ICollidable> firstSubCollisionCollidable;

        public Action<FirstCollidableT, ShapeCollection> CollisionOccurred;

        public void SetFirstSubCollision(Func<FirstCollidableT, Circle> subCollisionFunc, string subObjectName = null) { firstSubCollisionCircle = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, AxisAlignedRectangle> subCollisionFunc, string subObjectName = null) { firstSubCollisionRectangle = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Polygon> subCollisionFunc, string subObjectName = null) { firstSubCollisionPolygon = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, ICollidable> subCollisionFunc, string subObjectName = null) { firstSubCollisionCollidable = subCollisionFunc; FirstSubObjectName = subObjectName; }

        public override object FirstAsObject => singleObject;
        public override object SecondAsObject => shapeCollection;

        public void SetPartitioningSize(FirstCollidableT partitionedObject, float widthOrHeight)
        {
            firstPartitioningSize = widthOrHeight;
        }

        public PositionedObjectVsShapeCollection(FirstCollidableT singleObject, ShapeCollection shapeCollection)
        {
            this.singleObject = singleObject;
            this.shapeCollection = shapeCollection;
        }

        public override bool DoCollisions()
        {
            bool didCollisionOccur = false;
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                skippedFrames = 0;
                if (DoCollisionPhysicsInner(singleObject, shapeCollection, !ArePhysicsAppliedAutomatically))
                {
                    CollisionOccurred?.Invoke(singleObject, shapeCollection);
                    didCollisionOccur = true;
                    // Collision Limit doesn't do anything here

                    singleObject.ItemsCollidedAgainst.Add(shapeCollection.Name);
                    singleObject.ObjectsCollidedAgainst.Add(shapeCollection);
                    shapeCollection.ItemsCollidedAgainst.Add(singleObject.Name);
                    shapeCollection.ObjectsCollidedAgainst.Add(singleObject);
                }
            }
            return didCollisionOccur;
        }

        public bool DoCollisionPhysics(FirstCollidableT first, ShapeCollection second)
        {
            return DoCollisionPhysicsInner(first, second, false);
        }

        private bool DoCollisionPhysicsInner(FirstCollidableT first, ShapeCollection second, bool eventOnly)
        {
            this.DeepCollisionsThisFrame++;

            if (CollisionType == CollisionType.EventOnlyCollision || eventOnly)
            {
                return CollideAgainstConsiderSubCollisionEventOnly(first, second);
            }
            else if (CollisionType == CollisionType.MoveCollision)
            {
                return CollideAgainstConsiderSubCollisionMove(first, second);
            }
            else if (CollisionType == CollisionType.BounceCollision)
            {
                return CollideAgainstConsiderSubCollisionBounce(first, second);
            }
            else if(CollisionType == CollisionType.MoveSoftCollision)
            {
                return CollideAgainstConsiderSubCollisionMoveSoft(first, second);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private bool CollideAgainstConsiderSubCollisionEventOnly(FirstCollidableT first, ShapeCollection second)
        {
            if (firstSubCollisionCircle != null)
            {
                return second.CollideAgainst(firstSubCollisionCircle(first));
            }
            else if (firstSubCollisionRectangle != null)
            {
                return second.CollideAgainst(firstSubCollisionRectangle(first));
            }
            else if (firstSubCollisionPolygon != null)
            {
                return second.CollideAgainst(firstSubCollisionPolygon(first));
            }
            else if (firstSubCollisionCollidable != null)
            {
                return firstSubCollisionCollidable(first).CollideAgainst(second);
            }
            else
            {
                return first.CollideAgainst(second);
            }
        }
        private bool CollideAgainstConsiderSubCollisionMove(FirstCollidableT first, ShapeCollection second)
        {
            if (firstSubCollisionCircle != null)
            {
                return second.CollideAgainstMove(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass);
            }
            else if (firstSubCollisionRectangle != null)
            {
                return second.CollideAgainstMove(firstSubCollisionRectangle(first), moveSecondMass, moveFirstMass);
            }
            else if (firstSubCollisionPolygon != null)
            {
                return second.CollideAgainstMove(firstSubCollisionPolygon(first), moveSecondMass, moveFirstMass);
            }
            else if (firstSubCollisionCollidable != null)
            {
                return firstSubCollisionCollidable(first).CollideAgainstMove(second, moveFirstMass, moveSecondMass);
            }
            else
            {
                return first.CollideAgainstMove(second, moveFirstMass, moveSecondMass);
            }
        }
                
        private bool CollideAgainstConsiderSubCollisionMoveSoft(FirstCollidableT first, ShapeCollection second)
        {
            if(firstSubCollisionCircle != null)
            {
                throw new NotImplementedException("Circle vs shapecollection move soft collision is not yet implemented.");
            }
            else if(firstSubCollisionRectangle != null)
            {
                throw new NotImplementedException("AxisAlignedRectangle vs shapecollection move soft collision is not yet implemented.");
            }
            else if(firstSubCollisionPolygon != null)
            {
                throw new NotImplementedException("Polygon vs shapecollection move soft collision is not yet implemented.");

            }
            else if(firstSubCollisionCollidable != null)
            {
                return firstSubCollisionCollidable(first).Collision.CollideAgainstMoveSoft(second, moveFirstMass, moveSecondMass, softSeparationCoefficient);
            }
            else
            {
                return first.Collision.CollideAgainstMoveSoft(second, moveFirstMass, moveSecondMass, softSeparationCoefficient);
            }
        }
        private bool CollideAgainstConsiderSubCollisionBounce(FirstCollidableT first, ShapeCollection second)
        {
            if (firstSubCollisionCircle != null)
            {
                return second.CollideAgainstBounce(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass, bounceElasticity);
            }
            else if (firstSubCollisionRectangle != null)
            {
                return second.CollideAgainstBounce(firstSubCollisionRectangle(first), moveSecondMass, moveFirstMass, bounceElasticity);
            }
            else if (firstSubCollisionPolygon != null)
            {
                return second.CollideAgainstBounce(firstSubCollisionPolygon(first), moveSecondMass, moveFirstMass, bounceElasticity);
            }
            else if (firstSubCollisionCollidable != null)
            {
                return firstSubCollisionCollidable(first).CollideAgainstBounce(second, moveFirstMass, moveSecondMass, bounceElasticity);
            }
            else
            {
                return first.CollideAgainstBounce(second, moveFirstMass, moveSecondMass, bounceElasticity);
            }
        }
    }

    #endregion

    #region List vs ShapeCollection

    public class ListVsShapeCollectionRelationship<FirstCollidableT> : CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable
    {
        PositionedObjectList<FirstCollidableT> list;
        ShapeCollection shapeCollection;

        protected Func<FirstCollidableT, Circle> firstSubCollisionCircle;
        protected Func<FirstCollidableT, AxisAlignedRectangle> firstSubCollisionRectangle;
        protected Func<FirstCollidableT, Polygon> firstSubCollisionPolygon;
        protected Func<FirstCollidableT, ICollidable> firstSubCollisionCollidable;


        public Action<FirstCollidableT, ShapeCollection> CollisionOccurred;

        public void SetFirstSubCollision(Func<FirstCollidableT, Circle> subCollisionFunc, string subObjectName = null) { firstSubCollisionCircle = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, AxisAlignedRectangle> subCollisionFunc, string subObjectName = null) { firstSubCollisionRectangle = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Polygon> subCollisionFunc, string subObjectName = null) { firstSubCollisionPolygon = subCollisionFunc; FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, ICollidable> subCollisionFunc, string subObjectName = null) { firstSubCollisionCollidable = subCollisionFunc; FirstSubObjectName = subObjectName; }


        public override object FirstAsObject => list;
        public override object SecondAsObject => shapeCollection;

        public void SetPartitioningSize(PositionedObjectList<FirstCollidableT> partitionedObject, float widthOrHeight)
        {
            firstPartitioningSize = widthOrHeight;
        }

        public ListVsShapeCollectionRelationship(PositionedObjectList<FirstCollidableT> list, ShapeCollection shapeCollection)
        {
            this.list = list;
            this.shapeCollection = shapeCollection;
        }

        public override bool DoCollisions()
        {
            bool didCollisionOccur = false;
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                skippedFrames = 0;
                int startInclusive = list.Count-1;
                int endExclusive = -1;

                for (int i = startInclusive; i > endExclusive; i--)
                {
                    var atI = list[i];
                    if (DoCollisionPhysicsInner(atI, shapeCollection, !ArePhysicsAppliedAutomatically))
                    {
                        CollisionOccurred?.Invoke(atI, shapeCollection);
                        didCollisionOccur = true;
                        // Collision Limit doesn't do anything here

                        atI.ItemsCollidedAgainst.Add(shapeCollection.Name);
                        atI.ObjectsCollidedAgainst.Add(shapeCollection);
                        shapeCollection.ItemsCollidedAgainst.Add(atI.Name);
                        shapeCollection.ObjectsCollidedAgainst.Add(atI);
                    }
                }
            }

            this.CollidedThisFrame = didCollisionOccur;

            return didCollisionOccur;
        }

        public bool DoCollisionPhysics(FirstCollidableT first, ShapeCollection second)
        {
            return DoCollisionPhysicsInner(first, second, true);
        }

        private bool DoCollisionPhysicsInner(FirstCollidableT first, ShapeCollection second, bool eventOnly)
        {
            this.DeepCollisionsThisFrame++;

            if (CollisionType == CollisionType.EventOnlyCollision || eventOnly)
            {
                return CollideAgainstConsiderSubCollisionEventOnly(first, second);
            }
            else if (CollisionType == CollisionType.MoveCollision)
            {
                return CollideAgainstConsiderSubCollisionMove(first, second);
            }
            else if (CollisionType == CollisionType.BounceCollision)
            {
                return CollideAgainstConsiderSubCollisionBounce(first, second);
            }
            else if (CollisionType == CollisionType.MoveSoftCollision)
            {
                return CollideAgainstConsiderSubCollisionMoveSoft(first, second);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private bool CollideAgainstConsiderSubCollisionEventOnly(FirstCollidableT first, ShapeCollection second)
        {
            if (firstSubCollisionCircle != null)
            {
                return second.CollideAgainst(firstSubCollisionCircle(first));
            }
            else if (firstSubCollisionRectangle != null)
            {
                return second.CollideAgainst(firstSubCollisionRectangle(first));
            }
            else if (firstSubCollisionPolygon != null)
            {
                return second.CollideAgainst(firstSubCollisionPolygon(first));
            }
            else if (firstSubCollisionCollidable != null)
            {
                return firstSubCollisionCollidable(first).CollideAgainst(second);
            }
            else
            {
                return first.CollideAgainst(second);
            }
        }
        private bool CollideAgainstConsiderSubCollisionMove(FirstCollidableT first, ShapeCollection second)
        {
            if (firstSubCollisionCircle != null)
            {
                return second.CollideAgainstMove(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass);
            }
            else if (firstSubCollisionRectangle != null)
            {
                return second.CollideAgainstMove(firstSubCollisionRectangle(first), moveSecondMass, moveFirstMass);
            }
            else if (firstSubCollisionPolygon != null)
            {
                return second.CollideAgainstMove(firstSubCollisionPolygon(first), moveSecondMass, moveFirstMass);
            }
            else if (firstSubCollisionCollidable != null)
            {
                return firstSubCollisionCollidable(first).CollideAgainstMove(second, moveFirstMass, moveSecondMass);
            }
            else
            {
                return first.CollideAgainstMove(second, moveFirstMass, moveSecondMass);
            }
        }
        private bool CollideAgainstConsiderSubCollisionMoveSoft(FirstCollidableT first, ShapeCollection second)
        {
            {
                if (firstSubCollisionCircle != null)
                {
                    throw new NotImplementedException();
                }
                else if (firstSubCollisionRectangle != null)
                {
                    throw new NotImplementedException();
                }
                else if (firstSubCollisionPolygon != null)
                {
                    throw new NotImplementedException();
                }
                else if (firstSubCollisionCollidable != null)
                {
                    return firstSubCollisionCollidable(first).Collision.CollideAgainstMoveSoft(second, moveFirstMass, moveSecondMass, softSeparationCoefficient);
                }
                else
                {
                    return first.Collision.CollideAgainstMoveSoft(second, moveFirstMass, moveSecondMass, softSeparationCoefficient);
                }
            }
        }

        private bool CollideAgainstConsiderSubCollisionBounce(FirstCollidableT first, ShapeCollection second)
        {
            if (firstSubCollisionCircle != null)
            {
                return second.CollideAgainstBounce(firstSubCollisionCircle(first), moveSecondMass, moveFirstMass, bounceElasticity);
            }
            else if (firstSubCollisionRectangle != null)
            {
                return second.CollideAgainstBounce(firstSubCollisionRectangle(first), moveSecondMass, moveFirstMass, bounceElasticity);
            }
            else if (firstSubCollisionPolygon != null)
            {
                return second.CollideAgainstBounce(firstSubCollisionPolygon(first), moveSecondMass, moveFirstMass, bounceElasticity);
            }
            else if (firstSubCollisionCollidable != null)
            {
                return firstSubCollisionCollidable(first).CollideAgainstBounce(second, moveFirstMass, moveSecondMass, bounceElasticity);
            }
            else
            {
                return first.CollideAgainstBounce(second, moveFirstMass, moveSecondMass, bounceElasticity);
            }
        }

    }

    #endregion
}
