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
            line.LastCollisionPoint = new Point(double.NaN, double.NaN);

            Segment a = line.AsSegment();

            if (tileShapeCollection.SortAxis == Axis.X)
            {
                var leftmost = (float)System.Math.Min(line.AbsolutePoint1.X, line.AbsolutePoint2.X);
                var rightmost = (float)System.Math.Max(line.AbsolutePoint1.X, line.AbsolutePoint2.X);

                float clampedPosition = line.Position.X;

                bool isPositionOnEnd = false;
                if (clampedPosition <= leftmost)
                {
                    clampedPosition = leftmost;
                    isPositionOnEnd = true;
                }
                else if (clampedPosition >= rightmost)
                {
                    clampedPosition = rightmost;
                    isPositionOnEnd = true;
                }

                // only support rectangles for now (maybe forever)
                var rectangles = tileShapeCollection.Rectangles;

                var firstIndex = rectangles.GetFirstAfter(leftmost - tileShapeCollection.GridSize, Axis.X, 0, rectangles.Count);
                var lastIndex = rectangles.GetFirstAfter(rightmost + tileShapeCollection.GridSize, Axis.X, firstIndex, rectangles.Count);

                if (isPositionOnEnd)
                {
                    FlatRedBall.Math.Geometry.AxisAlignedRectangle collidedRectangle = null;
                    Point? intersectionPoint = null;
                    if (clampedPosition < rightmost)
                    {

                        // start at the beginning of the list, go up
                        for (int i = firstIndex; i < lastIndex; i++)
                        {
                            var rectangle = tileShapeCollection.Rectangles[i];

                            if (collidedRectangle != null)
                            {
                                if (rectangle.X > collidedRectangle.X)
                                {
                                    break;
                                }

                                if (rectangle.Y > collidedRectangle.Y && collidedRectangle.Y > line.Position.Y)
                                {
                                    break;
                                }
                                if (rectangle.Y < collidedRectangle.Y && collidedRectangle.Y < line.Position.Y)
                                {
                                    break;
                                }
                            }


                            Point tl = new Point(
                                rectangle.Position.X - rectangle.ScaleX,
                                rectangle.Position.Y + rectangle.ScaleY);
                            Point tr = new Point(
                                rectangle.Position.X + rectangle.ScaleX,
                                rectangle.Position.Y + rectangle.ScaleY);
                            Point bl = new Point(
                                rectangle.Position.X - rectangle.ScaleX,
                                rectangle.Position.Y - rectangle.ScaleY);
                            Point br = new Point(
                                rectangle.Position.X + rectangle.ScaleX,
                                rectangle.Position.Y - rectangle.ScaleY);

                            Point tempPoint;

                            // left gets priority
                            // left
                            var intersects = a.Intersects(new Segment(tl, bl), out tempPoint);

                            if (rectangle.Y > line.Y)
                            {
                                // bottom gets priority over top
                                if (!intersects)
                                {
                                    // bottom
                                    intersects = a.Intersects(new Segment(bl, br), out tempPoint);
                                }
                                if (!intersects)
                                {
                                    // top
                                    intersects = a.Intersects(new Segment(tl, tr), out tempPoint);
                                }
                            }
                            else
                            {
                                // top gets priority over top
                                if (!intersects)
                                {
                                    // top
                                    intersects = a.Intersects(new Segment(tl, tr), out tempPoint);
                                }
                                if (!intersects)
                                {
                                    // bottom
                                    intersects = a.Intersects(new Segment(bl, br), out tempPoint);
                                }
                            }
                            if (!intersects)
                            {
                                // right
                                intersects = a.Intersects(new Segment(tr, br), out tempPoint);
                            }

                            if (intersects)
                            {
                                intersectionPoint = tempPoint;
                                collidedRectangle = rectangle;
                            }
                        }
                    }
                    else
                    {
                        // start at the end of the list, go down
                        for (int i = lastIndex - 1; i >= firstIndex; i--)
                        {
                            var rectangle = tileShapeCollection.Rectangles[i];

                            if (collidedRectangle != null)
                            {
                                if (rectangle.X < collidedRectangle.X)
                                {
                                    break;
                                }

                                if (rectangle.Y > collidedRectangle.Y && collidedRectangle.Y > line.Position.Y)
                                {
                                    break;
                                }
                                if (rectangle.Y < collidedRectangle.Y && collidedRectangle.Y < line.Position.Y)
                                {
                                    break;
                                }
                            }



                            Point tl = new Point(
                                rectangle.Position.X - rectangle.ScaleX,
                                rectangle.Position.Y + rectangle.ScaleY);
                            Point tr = new Point(
                                rectangle.Position.X + rectangle.ScaleX,
                                rectangle.Position.Y + rectangle.ScaleY);
                            Point bl = new Point(
                                rectangle.Position.X - rectangle.ScaleX,
                                rectangle.Position.Y - rectangle.ScaleY);
                            Point br = new Point(
                                rectangle.Position.X + rectangle.ScaleX,
                                rectangle.Position.Y - rectangle.ScaleY);

                            Point tempPoint;

                            // right gets priority
                            // right
                            var intersects = a.Intersects(new Segment(tr, br), out tempPoint);

                            if (rectangle.Y > line.Y)
                            {
                                // bottom gets priority over top
                                if (!intersects)
                                {
                                    // bottom
                                    intersects = a.Intersects(new Segment(bl, br), out tempPoint);
                                }
                                if (!intersects)
                                {
                                    // top
                                    intersects = a.Intersects(new Segment(tl, tr), out tempPoint);
                                }
                            }
                            else
                            {
                                // top gets priority over top
                                if (!intersects)
                                {
                                    // top
                                    intersects = a.Intersects(new Segment(tl, tr), out tempPoint);
                                }
                                if (!intersects)
                                {
                                    // bottom
                                    intersects = a.Intersects(new Segment(bl, br), out tempPoint);
                                }
                            }
                            if (!intersects)
                            {
                                // left
                                intersects = a.Intersects(new Segment(tl, bl), out tempPoint);
                            }

                            if (intersects)
                            {
                                intersectionPoint = tempPoint;
                                collidedRectangle = rectangle;
                            }
                        }
                    }

                    if (collidedRectangle != null)
                    {
                        line.LastCollisionPoint = intersectionPoint ?? new Point(double.NaN, double.NaN);

                    }
                    return collidedRectangle != null;
                }
                else
                {
                    throw new NotImplementedException("The argument line's position is not on either endpoint. This is a requirement for this type of collision.");
                }
            }
            else if (tileShapeCollection.SortAxis == Axis.Y)
            {
                throw new NotImplementedException("Bug Vic to do Y. Currently just X is done");
            }
            return false;
        }
    }


    public class CollidableVsTileShapeCollectionRelationship<FirstCollidableT> : CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable
    {
        CollidableVsTileShapeCollectionData<FirstCollidableT> data;

        public void SetFirstSubCollision(Func<FirstCollidableT, Circle> subCollisionFunc) { data.firstSubCollisionCircle = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, AxisAlignedRectangle> subCollisionFunc) { data.firstSubCollisionRectangle = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Polygon> subCollisionFunc) { data.firstSubCollisionPolygon = subCollisionFunc; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Line> subCollisionFunc) { data.firstSubCollisionLine = subCollisionFunc; }
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
