using FlatRedBall;
using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using FlatRedBall.TileCollisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Math.Collision
{
    class CollidableVsTileShapeCollectionData<FirstCollidableT>
        where FirstCollidableT : PositionedObject, ICollidable
    {
        TileShapeCollection tileShapeCollection;
        public TileShapeCollection TileShapeCollection { get { return tileShapeCollection; } }

        public Func<FirstCollidableT, Circle> firstSubCollisionCircle;
        public Func<FirstCollidableT, AxisAlignedRectangle> firstSubCollisionRectangle;
        public Func<FirstCollidableT, Polygon> firstSubCollisionPolygon;
        public Func<FirstCollidableT, Line> firstSubCollisionLine;
        public Func<FirstCollidableT, ICollidable> firstSubCollisionCollidable;

        /// <summary>
        /// Stores the name of the first sub object name used in sub collision. If null, no object. Setting this does not
        /// change the functionality of the collidable object, it is only used by the level editor to determine if a collision
        /// relationship has changed. This should not be changed unless the sub collision has changed, and this usually is done
        /// in generated code.
        /// </summary>
        public string FirstSubObjectName { get; set; }

        public CollidableVsTileShapeCollectionData(TileShapeCollection tileShapeCollection)
        {
            if (tileShapeCollection == null)
            {
                throw new ArgumentNullException("The tileShapeCollection for the relationship cannot be null");
            }
            this.tileShapeCollection = tileShapeCollection;
        }

        public bool CollideAgainstConsiderSubCollisionEventOnly(FirstCollidableT singleObject, CollisionLimit collisionLimit)
        {
            if (firstSubCollisionCircle != null)
            {
                var circle = firstSubCollisionCircle(singleObject);
                return this.tileShapeCollection.CollideAgainst(circle);
            }
            else if (firstSubCollisionRectangle != null)
            {
                var rectangle = firstSubCollisionRectangle(singleObject);
                return this.tileShapeCollection.CollideAgainst(rectangle);
            }
            else if (firstSubCollisionPolygon != null)
            {
                var polygon = firstSubCollisionPolygon(singleObject);
                return this.tileShapeCollection.CollideAgainst(polygon);
            }
            else if (firstSubCollisionLine != null)
            {
                var line = firstSubCollisionLine(singleObject);
                if (collisionLimit == CollisionLimit.Closest)
                {
                    return DoFirstCollisionLineVsShapeCollection(line, tileShapeCollection);
                }
                else
                {
                    return this.tileShapeCollection.CollideAgainst(line);
                }
            }
            else if (firstSubCollisionCollidable != null)
            {
                var collidable = firstSubCollisionCollidable(singleObject);
                return this.tileShapeCollection.CollideAgainst(collidable);
            }
            else
            {
                return this.tileShapeCollection.CollideAgainst(singleObject);
            }
        }

        public bool CollideAgainstConsiderSubCollisionMove(FirstCollidableT singleObject)
        {
            if (firstSubCollisionCircle != null)
            {
                var circle = firstSubCollisionCircle(singleObject);
                return this.tileShapeCollection.CollideAgainstSolid(circle);
            }
            else if (firstSubCollisionRectangle != null)
            {
                var rectangle = firstSubCollisionRectangle(singleObject);
                return this.tileShapeCollection.CollideAgainstSolid(rectangle);
            }
            else if (firstSubCollisionPolygon != null)
            {
                var polygon = firstSubCollisionPolygon(singleObject);
                return this.tileShapeCollection.CollideAgainstSolid(polygon);
            }
            else if (firstSubCollisionLine != null)
            {
                var line = firstSubCollisionLine(singleObject);
                return this.tileShapeCollection.CollideAgainstSolid(line);
            }
            else if (firstSubCollisionCollidable != null)
            {
                var collidable = firstSubCollisionCollidable(singleObject);
                return this.tileShapeCollection.CollideAgainstSolid(collidable);
            }
            else
            {
                return this.tileShapeCollection.CollideAgainstSolid(singleObject);
            }
        }

        public bool CollideAgainstConsiderSubCollisionBounce(FirstCollidableT singleObject, float bounceElasticity)
        {
            if (firstSubCollisionCircle != null)
            {
                var circle = firstSubCollisionCircle(singleObject);
                return this.tileShapeCollection.CollideAgainstBounce(circle, bounceElasticity);
            }
            else if (firstSubCollisionRectangle != null)
            {
                var rectangle = firstSubCollisionRectangle(singleObject);
                return this.tileShapeCollection.CollideAgainstBounce(rectangle, bounceElasticity);
            }
            else if (firstSubCollisionPolygon != null)
            {
                var polygon = firstSubCollisionPolygon(singleObject);
                return this.tileShapeCollection.CollideAgainstBounce(polygon, bounceElasticity);
            }
            else if (firstSubCollisionLine != null)
            {
                //var line = firstSubCollisionLine(singleObject);
                //return this.tileShapeCollection.CollideAgainstBounce(line, bounceElasticity);
                return false; // not implemented
            }
            else if (firstSubCollisionCollidable != null)
            {
                var collidable = firstSubCollisionCollidable(singleObject);
                return this.tileShapeCollection.CollideAgainstBounce(collidable, bounceElasticity);
            }
            else
            {
                return this.tileShapeCollection.CollideAgainstBounce(singleObject, bounceElasticity);
            }
        }

        /// <summary>
        /// Performs collision between the argument Line and TileShapeCollection, returning whether collision occurred. The Line's
        /// LastCollisionPoint will be set to the closest point where collision occurs between the line and the tile shape collection.
        /// If collision does not occur, LastCollisionPoint will be set to (NaN, NaN)
        /// </summary>
        /// <param name="line">The line to perform collision. The "closest point" is the closest point to the line's Position.</param>
        /// <param name="tileShapeCollection">The TileShapeCollection to collide against.</param>
        /// <returns></returns>
        public static bool DoFirstCollisionLineVsShapeCollection(Line line, TileShapeCollection tileShapeCollection)
        {
            return tileShapeCollection.CollideAgainstClosest(line);
        }
    }


    public class CollidableVsTileShapeCollectionRelationship<FirstCollidableT> : CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable
    {
        CollidableVsTileShapeCollectionData<FirstCollidableT> data;

        public void SetFirstSubCollision(Func<FirstCollidableT, Circle> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionCircle = subCollisionFunc; data.FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, AxisAlignedRectangle> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionRectangle = subCollisionFunc; data.FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Polygon> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionPolygon = subCollisionFunc; data.FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Line> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionLine = subCollisionFunc; data.FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, ICollidable> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionCollidable = subCollisionFunc; data.FirstSubObjectName = subObjectName; }

        public Action<FirstCollidableT, TileShapeCollection> CollisionOccurred;


        FirstCollidableT singleObject;

        public override object FirstAsObject => singleObject;
        public override object SecondAsObject => data.TileShapeCollection;

        public CollidableVsTileShapeCollectionRelationship(FirstCollidableT singleObject, TileShapeCollection tileShapeCollection)
        {
            data = new CollidableVsTileShapeCollectionData<FirstCollidableT>(tileShapeCollection);
            this.singleObject = singleObject;
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
                if (CollisionLimit == CollisionLimit.Closest || CollisionLimit == CollisionLimit.First)
                {
                    string message = $"{nameof(CollidableVsTileShapeCollectionRelationship<FirstCollidableT>)} does not implement CollisionLimit {CollisionLimit}";
                    throw new NotImplementedException();
                }
                else
                {
                    skippedFrames = 0;

                    var didCollide = false;
                    // todo - tile shape collections need to report their deep collision, they don't currently:
                    if (CollisionType == CollisionType.EventOnlyCollision)
                    {
                        didCollide = data.CollideAgainstConsiderSubCollisionEventOnly(singleObject, CollisionLimit);
                    }
                    else if (CollisionType == CollisionType.MoveCollision)
                    {
                        didCollide = data.CollideAgainstConsiderSubCollisionMove(singleObject);
                    }
                    else if (CollisionType == CollisionType.BounceCollision)
                    {
                        didCollide = data.CollideAgainstConsiderSubCollisionBounce(singleObject, bounceElasticity);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    if (didCollide)
                    {
                        CollisionOccurred?.Invoke(singleObject, data.TileShapeCollection);

                        didCollisionOccur = true;
                    }
                }
            }

            return didCollisionOccur;
        }
    }
}
