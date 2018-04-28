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
        public TileShapeCollection TileShapeCollection {  get { return tileShapeCollection; } }

        public Func<FirstCollidableT, Circle> firstSubCollisionCircle;
        public Func<FirstCollidableT, AxisAlignedRectangle> firstSubCollisionRectangle;
        public Func<FirstCollidableT, Polygon> firstSubCollisionPolygon;
        public Func<FirstCollidableT, ICollidable> firstSubCollisionCollidable;

        public CollidableVsTileShapeCollectionData(TileShapeCollection tileShapeCollection)
        {
            this.tileShapeCollection = tileShapeCollection;
        }

        public bool CollideAgainstConsiderSubCollisionEventOnly(FirstCollidableT singleObject)
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
    }


    public class CollidableVsTileShapeCollectionRelationship<FirstCollidableT> : CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable
    {
        CollidableVsTileShapeCollectionData<FirstCollidableT> data;

        public void SetFirstSubCollision(Func<FirstCollidableT, Circle> subCollisionFunc) { data.firstSubCollisionCircle = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, AxisAlignedRectangle> subCollisionFunc) { data.firstSubCollisionRectangle = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Polygon> subCollisionFunc) { data.firstSubCollisionPolygon = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, ICollidable> subCollisionFunc) { data.firstSubCollisionCollidable = subCollisionFunc; }

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
                        didCollide = data.CollideAgainstConsiderSubCollisionEventOnly(singleObject);
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

                    if(didCollide)
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
