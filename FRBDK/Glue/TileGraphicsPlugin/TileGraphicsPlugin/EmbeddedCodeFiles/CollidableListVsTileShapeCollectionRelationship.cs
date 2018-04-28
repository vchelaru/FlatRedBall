using FlatRedBall.Math.Geometry;
using FlatRedBall.TileCollisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Math.Collision
{
    public class CollidableListVsTileShapeCollectionRelationship<FirstCollidableT> :
        CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable
    {
        CollidableVsTileShapeCollectionData<FirstCollidableT> data;

        public void SetFirstSubCollision(Func<FirstCollidableT, Circle> subCollisionFunc) { data.firstSubCollisionCircle = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, AxisAlignedRectangle> subCollisionFunc) { data.firstSubCollisionRectangle = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Polygon> subCollisionFunc) { data.firstSubCollisionPolygon = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, ICollidable> subCollisionFunc) { data.firstSubCollisionCollidable = subCollisionFunc; }

        public Action<FirstCollidableT, TileShapeCollection> CollisionOccurred;

        PositionedObjectList<FirstCollidableT> list;

        public override object FirstAsObject => list;
        public override object SecondAsObject => data.TileShapeCollection;

        public CollidableListVsTileShapeCollectionRelationship(PositionedObjectList<FirstCollidableT> list, TileShapeCollection tileShapeCollection)
        {
            data = new CollidableVsTileShapeCollectionData<FirstCollidableT>(tileShapeCollection);
            this.list = list;
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

                    for(int i = list.Count - 1 ; i > -1; i--)
                    {
                        var singleObject = list[i];

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

                        if (didCollide)
                        {
                            didCollisionOccur = true;
                            CollisionOccurred?.Invoke(singleObject, data.TileShapeCollection);
                        }
                    }
                }
            }
            return didCollisionOccur;
        }
    }
}
