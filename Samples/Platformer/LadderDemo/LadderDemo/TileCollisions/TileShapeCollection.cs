using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.TileGraphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AARect = FlatRedBall.Math.Geometry.AxisAlignedRectangle;

namespace FlatRedBall.TileCollisions
{
    public partial class TileShapeCollection
    {
        #region Fields

        ShapeCollection mShapes;
        Axis mSortAxis = Axis.X;
        /// <summary>
        /// The leftmost edge of the map. This will correspond with the left edge of an AxisAlignedRectangle.
        /// </summary>
        public float LeftSeedX = 0;

        /// <summary>
        /// The bottommost edge of the map. This will correspond with the bottom edge of an AxisAlignedRectangle.
        /// </summary>
        public float BottomSeedY = 0;
        float mGridSize;
        bool mVisible = true;


        bool mFirstTimeSortAxisSet = true;

        #endregion

        #region Properties

        public Axis SortAxis
        {
            get
            {
                return mSortAxis;
            }
            set
            {
                bool hasChanged = value != mSortAxis;
                if (hasChanged || mFirstTimeSortAxisSet)
                {
                    mSortAxis = value;
                    PerformSort();
                }
            }
        }

        public float GridSize
        {
            get { return mGridSize; }
            set
            {
#if DEBUG
                if (value < 0)
                {
                    throw new Exception("GridSize needs to be positive");
                }
#endif


                mGridSize = value;
                mShapes.MaxAxisAlignedRectanglesScale = mGridSize;
                mShapes.MaxPolygonRadius = mGridSize;
            }
        }

        public PositionedObjectList<AxisAlignedRectangle> Rectangles
        {
            get { return mShapes.AxisAlignedRectangles; }
        }


        public PositionedObjectList<Polygon> Polygons
        {
            get { return mShapes.Polygons; }
        }

        public string Name { get; set; }


        public List<Polygon> LastCollisionPolygons => mShapes.LastCollisionPolygons;
        public List<AxisAlignedRectangle> LastCollisionAxisAlignedRectangles => mShapes.LastCollisionAxisAlignedRectangles;

        public bool Visible
        {
            get { return mVisible; }
            set
            {
                mVisible = value;
                for (int i = 0; i < mShapes.AxisAlignedRectangles.Count; i++)
                {
                    mShapes.AxisAlignedRectangles[i].Visible = value;
                }
                for (int i = 0; i < mShapes.Polygons.Count; i++)
                {
                    if (value)
                    {
                        // to get the verts to show up
                        mShapes.Polygons[i].ForceUpdateDependencies();
                    }
                    mShapes.Polygons[i].Visible = value;
                }
            }
        }

        Microsoft.Xna.Framework.Color mColor = Microsoft.Xna.Framework.Color.White;
        public Microsoft.Xna.Framework.Color Color
        {
            get => mColor;
            set
            {
                mColor = value;
                for (int i = 0; i < mShapes.AxisAlignedRectangles.Count; i++)
                {
                    mShapes.AxisAlignedRectangles[i].Color = value;
                }
                for (int i = 0; i < mShapes.Circles.Count; i++)
                {
                    mShapes.Circles[i].Color = value;
                }
                for (int i = 0; i < mShapes.Polygons.Count; i++)
                {
                    mShapes.Polygons[i].Color = value;
                }
            }
        }

        public bool AdjustRepositionDirectionsOnAddAndRemove { get; set; } = true;

        #endregion

        public TileShapeCollection()
        {
            mShapes = new ShapeCollection();
            GridSize = 16;
        }


        public void AddToLayer(FlatRedBall.Graphics.Layer layer)
        {
            this.mShapes.AddToManagers(layer);
        }

        public void AttachTo(PositionedObject newParent, bool changeRelative = true)
        {
            mShapes.AttachTo(newParent, changeRelative);
        }

        public void CopyAbsoluteToRelative()
        {
            mShapes.CopyAbsoluteToRelative();
        }

        public bool CollideAgainstSolid(AxisAlignedRectangle movableObject)
        {
            bool toReturn = false;

            toReturn = mShapes.CollideAgainstBounce(movableObject, true, mSortAxis, 1, 0, 0);

            return toReturn;
        }

        public bool CollideAgainstSolid(Circle movableObject)
        {
            bool toReturn = false;

            toReturn = mShapes.CollideAgainstBounce(movableObject, true, mSortAxis, 1, 0, 0);

            return toReturn;
        }

        public bool CollideAgainstSolid(Polygon movableObject)
        {
            bool toReturn = false;

            toReturn = mShapes.CollideAgainstBounce(movableObject, true, mSortAxis, 1, 0, 0);

            return toReturn;
        }

        public bool CollideAgainstSolid(Line movableObject)
        {
            bool toReturn = false;

            toReturn = mShapes.CollideAgainstBounce(movableObject, true, mSortAxis, 1, 0, 0);

            return toReturn;
        }

        public bool CollideAgainst(AxisAlignedRectangle rectangle)
        {
            return mShapes.CollideAgainst(rectangle, true, mSortAxis);
        }

        public bool CollideAgainst(Circle circle)
        {
            return mShapes.CollideAgainst(circle, true, mSortAxis);
        }

        public bool CollideAgainst(Polygon polygon)
        {
            return mShapes.CollideAgainst(polygon, true, mSortAxis);
        }

        public bool CollideAgainst(Line line)
        {
            return mShapes.CollideAgainst(line, true, mSortAxis);
        }

        public bool CollideAgainstClosest(Line line)
        {
            line.LastCollisionPoint = new Point(double.NaN, double.NaN);

            Segment a = line.AsSegment();

            if (SortAxis == Axis.X)
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
                var rectangles = Rectangles;

                var firstIndex = rectangles.GetFirstAfter(leftmost - GridSize, Axis.X, 0, rectangles.Count);
                var lastIndex = rectangles.GetFirstAfter(rightmost + GridSize, Axis.X, firstIndex, rectangles.Count);

                if (isPositionOnEnd)
                {
                    FlatRedBall.Math.Geometry.AxisAlignedRectangle collidedRectangle = null;
                    Point? intersectionPoint = null;
                    if (clampedPosition < rightmost)
                    {

                        // start at the beginning of the list, go up
                        for (int i = firstIndex; i < lastIndex; i++)
                        {
                            var rectangle = Rectangles[i];

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
                            var rectangle = Rectangles[i];

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
            else if (SortAxis == Axis.Y)
            {
                throw new NotImplementedException("Bug Vic to do Y. Currently just X is done");
            }
            return false;
        }


        public bool CollideAgainst(ICollidable collidable)
        {
            return mShapes.CollideAgainst(collidable.Collision, true, mSortAxis);
        }

        public bool CollideAgainstSolid(ICollidable collidable)
        {
            bool toReturn = false;

            toReturn = mShapes.CollideAgainstBounce(collidable.Collision, true, mSortAxis, 1, 0, 0);

            return toReturn;
        }

        public bool CollideAgainstBounce(ICollidable collidable, float elasticity)
        {
            bool toReturn = false;

            toReturn = mShapes.CollideAgainstBounce(collidable.Collision, true, mSortAxis, 1, 0, elasticity);

            return toReturn;
        }

        public bool CollideAgainstBounce(AxisAlignedRectangle rectangle, float elasticity)
        {
            bool toReturn = mShapes.CollideAgainstBounce(rectangle, true, mSortAxis, 1, 0, elasticity);

            return toReturn;
        }

        public bool CollideAgainstBounce(Circle circle, float elasticity)
        {
            bool toReturn = mShapes.CollideAgainstBounce(circle, true, mSortAxis, 1, 0, elasticity);

            return toReturn;
        }

        public bool CollideAgainstBounce(Polygon polygon, float elasticity)
        {
            bool toReturn = mShapes.CollideAgainstBounce(polygon, true, mSortAxis, 1, 0, elasticity);

            return toReturn;
        }

        [Obsolete("Use GetRectangleAtPosition instead as it more clearly indicates what the method does.")]
        public AxisAlignedRectangle GetTileAt(float x, float y)
        {
            return GetRectangleAtPosition(x, y);
        }

        public AxisAlignedRectangle GetRectangleAtPosition(float worldX, float worldY)
        {
            float middleOfTileX = MathFunctions.RoundFloat(worldX, GridSize, LeftSeedX + GridSize / 2.0f);
            float middleOfTileY = MathFunctions.RoundFloat(worldY, GridSize, BottomSeedY + GridSize / 2.0f);
            float keyValue = GetCoordinateValueForPartitioning(middleOfTileX, middleOfTileY);

            float keyValueBefore = keyValue - GridSize / 2.0f;
            float keyValueAfter = keyValue + GridSize / 2.0f;

            int startInclusive = mShapes.AxisAlignedRectangles.GetFirstAfter(keyValueBefore, mSortAxis,
                0, mShapes.AxisAlignedRectangles.Count);


            int endExclusive = mShapes.AxisAlignedRectangles.GetFirstAfter(keyValueAfter, mSortAxis,
                0, mShapes.AxisAlignedRectangles.Count);

            AxisAlignedRectangle toReturn = GetRectangleAtPosition(worldX, worldY, startInclusive, endExclusive);

            return toReturn;
        }

        public Polygon GetPolygonAtPosition(float worldX, float worldY)
        {
            float middleOfTileX = MathFunctions.RoundFloat(worldX, GridSize, LeftSeedX + GridSize / 2.0f);
            float middleOfTileY = MathFunctions.RoundFloat(worldY, GridSize, BottomSeedY + GridSize / 2.0f);
            float keyValue = GetCoordinateValueForPartitioning(middleOfTileX, middleOfTileY);

            var halfGridSize = GridSize / 2.0f;

            float keyValueBefore = keyValue - halfGridSize;
            float keyValueAfter = keyValue + halfGridSize;

            int startInclusive = mShapes.Polygons.GetFirstAfter(keyValueBefore, mSortAxis,
                0, mShapes.AxisAlignedRectangles.Count);


            int endExclusive = mShapes.Polygons.GetFirstAfter(keyValueAfter, mSortAxis,
                0, mShapes.AxisAlignedRectangles.Count);

            var left = middleOfTileX - halfGridSize;
            var right = middleOfTileX + halfGridSize;
            var top = middleOfTileY + halfGridSize;
            var bottom = middleOfTileY - halfGridSize;

            for (int i = startInclusive; i < endExclusive; i++)
            {
                var polygon = mShapes.Polygons[i];

                if (polygon.Position.X > left && polygon.Position.X < right &&
                    polygon.Position.Y > bottom && polygon.Position.Y < top)
                {
                    return polygon;
                }
            }

            return null;
        }

        private Polygon GetPolygonAtPosition(float worldX, float worldY, int startInclusive, int endExclusive)
        {
            float middleOfTileX = MathFunctions.RoundFloat(worldX, GridSize, LeftSeedX + GridSize / 2.0f);
            float middleOfTileY = MathFunctions.RoundFloat(worldY, GridSize, BottomSeedY + GridSize / 2.0f);

            var halfGridSize = GridSize / 2.0f;

            var left = middleOfTileX - halfGridSize;
            var right = middleOfTileX + halfGridSize;
            var top = middleOfTileY + halfGridSize;
            var bottom = middleOfTileY - halfGridSize;

            for (int i = startInclusive; i < endExclusive; i++)
            {
                var polygon = mShapes.Polygons[i];

                if (polygon.Position.X > left && polygon.Position.X < right &&
                    polygon.Position.Y > bottom && polygon.Position.Y < top)
                {
                    return polygon;
                }
            }

            return null;
        }

        private AxisAlignedRectangle GetRectangleAtPosition(float x, float y, int startInclusive, int endExclusive)
        {
            AxisAlignedRectangle toReturn = null;
            for (int i = startInclusive; i < endExclusive; i++)
            {
                if (mShapes.AxisAlignedRectangles[i].IsPointInside(x, y))
                {
                    toReturn = mShapes.AxisAlignedRectangles[i];
                    break;
                }
            }
            return toReturn;
        }

        public void AddCollisionAtWorld(float x, float y)
        {
            // Make sure there isn't already collision here
            if (GetRectangleAtPosition(x, y) == null)
            {
                // x and y
                // represent
                // the center
                // of the tile
                // where the user
                // may want to add 
                // collision.  Let's
                // subtract half width/
                // height so we can use the
                // bottom/left
                float roundedX = MathFunctions.RoundFloat(x - GridSize / 2.0f, GridSize, LeftSeedX);
                float roundedY = MathFunctions.RoundFloat(y - GridSize / 2.0f, GridSize, BottomSeedY);

                AxisAlignedRectangle newAar = new AxisAlignedRectangle();
                newAar.Width = GridSize;
                newAar.Height = GridSize;
                newAar.Left = roundedX;
                newAar.Bottom = roundedY;

                if (this.mVisible)
                {
                    newAar.Visible = true;
                }

                InsertRectangle(newAar);
            }
        }

        public void InsertRectangle(AARect rectangle)
        {
            float roundedX = rectangle.Left;
            float roundedY = rectangle.Bottom;

            float keyValue = GetCoordinateValueForPartitioning(roundedX, roundedY);

            int index = mShapes.AxisAlignedRectangles.GetFirstAfter(keyValue, mSortAxis,
                0, mShapes.AxisAlignedRectangles.Count);

            mShapes.AxisAlignedRectangles.Insert(index, rectangle);

            if(AdjustRepositionDirectionsOnAddAndRemove)
            {
                var directions = UpdateRepositionForNeighborsAndGetThisRepositionDirection(rectangle);

                rectangle.RepositionDirections = directions;
            }
        }

        public void InsertShapes(TileShapeCollection source)
        {
            foreach(var rectangle in source.Rectangles)
            {
                this.InsertRectangle(rectangle);
            }

            if(source.Polygons.Count > 0)
            {
                throw new InvalidOperationException("Inserting does not currently support TileShapeCollections with polygons");
            }
        }

        public void InsertCollidables<T>(IList<T> collidables) where T : FlatRedBall.Math.Geometry.ICollidable
        {
            foreach(var collidable in collidables)
            {
                foreach(var rectangle in collidable.Collision.AxisAlignedRectangles)
                {
                    rectangle.ForceUpdateDependencies();
                    InsertRectangle(rectangle);
                }
            }
        }

        public void RemoveCollisionAtWorld(float x, float y)
        {
            AxisAlignedRectangle existing = GetRectangleAtPosition(x, y);
            if (existing != null)
            {
                RemoveRectangle(existing);
            }


        }

        public void RemoveRectangle(AARect existing)
        {
            ShapeManager.Remove(existing);

            if(AdjustRepositionDirectionsOnAddAndRemove)
            {
                float keyValue = GetCoordinateValueForPartitioning(existing.X, existing.Y);

                float keyValueBefore = keyValue - GridSize * 3 / 2.0f;
                float keyValueAfter = keyValue + GridSize * 3 / 2.0f;

                int rectanglesBeforeIndex = Rectangles.GetFirstAfter(keyValueBefore, mSortAxis, 0, Rectangles.Count);
                int rectanglesAfterIndex = Rectangles.GetFirstAfter(keyValueAfter, mSortAxis, 0, Rectangles.Count);

                float leftOfX = existing.Position.X - GridSize;
                float rightOfX = existing.Position.X + GridSize;
                float middleX = existing.Position.X;

                float aboveY = existing.Position.Y + GridSize;
                float belowY = existing.Position.Y - GridSize;
                float middleY = existing.Position.Y;

                var leftOf = GetRectangleAtPosition(leftOfX, existing.Y, rectanglesBeforeIndex, rectanglesAfterIndex);
                var rightOf = GetRectangleAtPosition(rightOfX, existing.Y, rectanglesBeforeIndex, rectanglesAfterIndex);
                var above = GetRectangleAtPosition(existing.X, aboveY, rectanglesBeforeIndex, rectanglesAfterIndex);
                var below = GetRectangleAtPosition(existing.X, belowY, rectanglesBeforeIndex, rectanglesAfterIndex);

                var rectangleUpLeft = GetRectangleAtPosition(leftOfX, aboveY, rectanglesBeforeIndex, rectanglesAfterIndex);
                var rectangleUpRight = GetRectangleAtPosition(rightOfX, aboveY, rectanglesBeforeIndex, rectanglesAfterIndex);
                var rectangleDownLeft = GetRectangleAtPosition(leftOfX, belowY, rectanglesBeforeIndex, rectanglesAfterIndex);
                var rectangleDownRight = GetRectangleAtPosition(rightOfX, belowY, rectanglesBeforeIndex, rectanglesAfterIndex);

                if (leftOf != null && (leftOf.RepositionDirections & RepositionDirections.Right) != RepositionDirections.Right)
                {
                    leftOf.RepositionDirections |= RepositionDirections.Right;

                }
                if (rightOf != null && (rightOf.RepositionDirections & RepositionDirections.Left) != RepositionDirections.Left)
                {
                    rightOf.RepositionDirections |= RepositionDirections.Left;
                }

                if (above != null && (above.RepositionDirections & RepositionDirections.Down) != RepositionDirections.Down)
                {
                    above.RepositionDirections |= RepositionDirections.Down;
                }

                if (below != null && (below.RepositionDirections & RepositionDirections.Up) != RepositionDirections.Up)
                {
                    below.RepositionDirections |= RepositionDirections.Up;
                }

                void UpdateLShaped(AARect center)
                {
                    if (center != null)
                    {
                        var left = GetRectangleAtPosition(center.X - GridSize, center.Y);
                        var upLeft = GetRectangleAtPosition(center.X - GridSize, center.Y + GridSize);
                        var up = GetRectangleAtPosition(center.X, center.Y + GridSize);
                        var upRight = GetRectangleAtPosition(center.X + GridSize, center.Y + GridSize);
                        var right = GetRectangleAtPosition(center.X + GridSize, center.Y);
                        var downRight = GetRectangleAtPosition(center.X + GridSize, center.Y - GridSize);
                        var down = GetRectangleAtPosition(center.X, center.Y - GridSize);
                        var downLeft = GetRectangleAtPosition(center.X - GridSize, center.Y - GridSize);

                        UpdateLShapedPassNeighbors(center, left, upLeft, up, upRight, right, downRight, down, downLeft);
                    }
                }

                UpdateLShaped(leftOf);
                UpdateLShaped(rectangleUpLeft);
                UpdateLShaped(above);
                UpdateLShaped(rectangleUpRight);
                UpdateLShaped(rightOf);
                UpdateLShaped(rectangleDownRight);
                UpdateLShaped(below);
                UpdateLShaped(rectangleDownLeft);
            }
        }

        public void RemoveSurroundedCollision()
        {
            for (int i = Rectangles.Count - 1; i > -1; i--)
            {
                var rectangle = Rectangles[i];
                if (rectangle.RepositionDirections == RepositionDirections.None)
                {
                    rectangle.Visible = false;
                    this.Rectangles.Remove(rectangle);
                }
            }
        }


        private float GetCoordinateValueForPartitioning(float x, float y)
        {
            float keyValue = 0;

            switch (mSortAxis)
            {
                case Axis.X:
                    keyValue = x;
                    break;
                case Axis.Y:
                    keyValue = y;
                    break;
                case Axis.Z:
                    throw new NotImplementedException("Sorting on Z not supported");
            }
            return keyValue;
        }

        public static void UpdateLShapedPassNeighbors(AARect center, AARect left, AARect upLeft, AARect up, AARect upRight, AARect right, AARect downRight, AARect down, AARect downLeft)
        {
            center.RepositionHalfSize =
                left != null && up != null && upLeft == null ||
                up != null && right != null && upRight == null ||
                right != null && down != null && downRight == null ||
                down != null && left != null && downLeft == null;
        }

        private RepositionDirections UpdateRepositionForNeighborsAndGetThisRepositionDirection(PositionedObject positionedObject)
        {
            // Let's see what is surrounding this rectangle and update it and the surrounding rects appropriately
            float keyValue = GetCoordinateValueForPartitioning(positionedObject.Position.X, positionedObject.Position.Y);

            float keyValueBefore = keyValue - GridSize * 3 / 2.0f;
            float keyValueAfter = keyValue + GridSize * 3 / 2.0f;

            int rectanglesBeforeIndex = Rectangles.GetFirstAfter(keyValueBefore, mSortAxis, 0, Rectangles.Count);
            int rectanglesAfterIndex = Rectangles.GetFirstAfter(keyValueAfter, mSortAxis, 0, Rectangles.Count);

            int polygonsBeforeIndex = Polygons.GetFirstAfter(keyValueBefore, mSortAxis, 0, Polygons.Count);
            int polygonsAfterIndex = Polygons.GetFirstAfter(keyValueAfter, mSortAxis, 0, Polygons.Count);


            float leftOfX = positionedObject.Position.X - GridSize;
            float rightOfX = positionedObject.Position.X + GridSize;
            float middleX = positionedObject.Position.X;

            float aboveY = positionedObject.Position.Y + GridSize;
            float belowY = positionedObject.Position.Y - GridSize;
            float middleY = positionedObject.Position.Y;

            var rectangleLeftOf = GetRectangleAtPosition(leftOfX, middleY, rectanglesBeforeIndex, rectanglesAfterIndex);
            var rectangleRightOf = GetRectangleAtPosition(rightOfX, middleY, rectanglesBeforeIndex, rectanglesAfterIndex);
            var rectangleAbove = GetRectangleAtPosition(middleX, aboveY, rectanglesBeforeIndex, rectanglesAfterIndex);
            var rectangleBelow = GetRectangleAtPosition(middleX, belowY, rectanglesBeforeIndex, rectanglesAfterIndex);

            var rectangleUpLeft = GetRectangleAtPosition(leftOfX, aboveY, rectanglesBeforeIndex, rectanglesAfterIndex);
            var rectangleUpRight = GetRectangleAtPosition(rightOfX, aboveY, rectanglesBeforeIndex, rectanglesAfterIndex);
            var rectangleDownLeft = GetRectangleAtPosition(leftOfX, belowY, rectanglesBeforeIndex, rectanglesAfterIndex);
            var rectangleDownRight = GetRectangleAtPosition(rightOfX, belowY, rectanglesBeforeIndex, rectanglesAfterIndex);

            void UpdateLShaped(AARect center)
            {
                if(center != null)
                {
                    var left = GetRectangleAtPosition(center.X - GridSize, center.Y);
                    var upLeft = GetRectangleAtPosition(center.X - GridSize, center.Y + GridSize);
                    var up = GetRectangleAtPosition(center.X, center.Y + GridSize);
                    var upRight = GetRectangleAtPosition(center.X + GridSize, center.Y + GridSize);
                    var right = GetRectangleAtPosition(center.X + GridSize, center.Y);
                    var downRight = GetRectangleAtPosition(center.X + GridSize, center.Y - GridSize);
                    var down = GetRectangleAtPosition(center.X, center.Y - GridSize);
                    var downLeft = GetRectangleAtPosition(center.X - GridSize, center.Y - GridSize);

                    UpdateLShapedPassNeighbors(center, left, upLeft, up, upRight, right, downRight, down, downLeft);
                }
            }

            UpdateLShapedPassNeighbors(positionedObject as AARect, rectangleLeftOf, rectangleUpLeft, rectangleAbove, rectangleUpRight, rectangleRightOf, rectangleDownRight, rectangleBelow, rectangleDownLeft);
            UpdateLShaped(rectangleLeftOf);
            UpdateLShaped(rectangleUpLeft);
            UpdateLShaped(rectangleAbove);
            UpdateLShaped(rectangleUpRight);
            UpdateLShaped(rectangleRightOf);
            UpdateLShaped(rectangleDownRight);
            UpdateLShaped(rectangleBelow);
            UpdateLShaped(rectangleDownLeft);

            RepositionDirections directions = RepositionDirections.All;
            if (rectangleLeftOf != null)
            {
                directions -= RepositionDirections.Left;
                if ((rectangleLeftOf.RepositionDirections & RepositionDirections.Right) == RepositionDirections.Right)
                {
                    rectangleLeftOf.RepositionDirections -= RepositionDirections.Right;
                }
            }
            else
            {
                var polygon = GetPolygonAtPosition(leftOfX, middleY, polygonsBeforeIndex, polygonsAfterIndex);

                if (polygon != null)
                {
                    directions -= RepositionDirections.Left;
                    if ((polygon.RepositionDirections & RepositionDirections.Right) == RepositionDirections.Right)
                    {
                        polygon.RepositionDirections -= RepositionDirections.Right;
                    }
                }
            }

            if (rectangleRightOf != null)
            {
                directions -= RepositionDirections.Right;

                if ((rectangleRightOf.RepositionDirections & RepositionDirections.Left) == RepositionDirections.Left)
                {
                    rectangleRightOf.RepositionDirections -= RepositionDirections.Left;
                }
            }
            else
            {
                var polygon = GetPolygonAtPosition(rightOfX, middleY, polygonsBeforeIndex, polygonsAfterIndex);

                if (polygon != null)
                {
                    directions -= RepositionDirections.Right;
                    if ((polygon.RepositionDirections & RepositionDirections.Left) == RepositionDirections.Left)
                    {
                        polygon.RepositionDirections -= RepositionDirections.Left;
                    }
                }
            }



            if (rectangleAbove != null)
            {
                directions -= RepositionDirections.Up;

                if ((rectangleAbove.RepositionDirections & RepositionDirections.Down) == RepositionDirections.Down)
                {
                    rectangleAbove.RepositionDirections -= RepositionDirections.Down;
                }
            }
            else
            {
                var polygon = GetPolygonAtPosition(middleX, aboveY, polygonsBeforeIndex, polygonsAfterIndex);

                if (polygon != null)
                {
                    directions -= RepositionDirections.Up;

                    if ((polygon.RepositionDirections & RepositionDirections.Down) == RepositionDirections.Down)
                    {
                        polygon.RepositionDirections -= RepositionDirections.Down;
                    }
                }
            }

            if (rectangleBelow != null)
            {
                directions -= RepositionDirections.Down;
                if ((rectangleBelow.RepositionDirections & RepositionDirections.Up) == RepositionDirections.Up)
                {
                    rectangleBelow.RepositionDirections -= RepositionDirections.Up;
                }
            }
            else
            {
                var polygon = GetPolygonAtPosition(middleX, belowY, polygonsBeforeIndex, polygonsAfterIndex);

                if (polygon != null)
                {
                    directions -= RepositionDirections.Down;

                    if ((polygon.RepositionDirections & RepositionDirections.Up) == RepositionDirections.Up)
                    {
                        polygon.RepositionDirections -= RepositionDirections.Up;
                    }
                }
            }

            return directions;
        }

        public void RemoveFromManagersOneWay()
        {
            this.mShapes.MakeOneWay();
            this.mShapes.RemoveFromManagers();
            this.mShapes.MakeTwoWay();
        }

        public void RemoveFromManagers()
        {
            this.mShapes.RemoveFromManagers();
        }

        private void PerformSort()
        {
            switch (mSortAxis)
            {
                case Axis.X:
                    mShapes.AxisAlignedRectangles.SortXInsertionAscending();
                    mShapes.Polygons.SortXInsertionAscending();
                    break;
                case Axis.Y:
                    mShapes.AxisAlignedRectangles.SortYInsertionAscending();
                    mShapes.Polygons.SortYInsertionAscending();
                    break;
                case Axis.Z:
                    mShapes.AxisAlignedRectangles.SortZInsertionAscending();
                    mShapes.Polygons.SortZInsertionAscending();
                    break;
            }
        }

        public void SetColor(Microsoft.Xna.Framework.Color color)
        {
            foreach (var rectangle in this.Rectangles)
            {
                rectangle.Color = color;
            }
            foreach (var polygon in this.Polygons)
            {
                polygon.Color = color;
            }

        }

        public void RefreshAllRepositionDirections()
        {
            var count = this.mShapes.AxisAlignedRectangles.Count;
            for (int i = 0; i < count; i++)
            {
                var rectangle = this.mShapes.AxisAlignedRectangles[i];

                var directions = UpdateRepositionForNeighborsAndGetThisRepositionDirection(rectangle);

                rectangle.RepositionDirections = directions;
            }

            count = this.mShapes.Polygons.Count;
            for (int i = 0; i < count; i++)
            {
                var polygon = this.mShapes.Polygons[i];

                var directions = UpdateRepositionForNeighborsAndGetThisRepositionDirection(polygon);

                polygon.RepositionDirections = directions;
            }
        }

        public void AssignAllShapesToRepositionOutward()
        {
            List<AxisAlignedRectangle> rectanglesWithNoneReposition = new List<AxisAlignedRectangle>();

            // fill it with any rectangles
            foreach (var rectangle in this.Rectangles)
            {
                if (rectangle.RepositionDirections == RepositionDirections.None)
                {
                    rectanglesWithNoneReposition.Add(rectangle);
                }
            }

            HashSet<AxisAlignedRectangle> rectanglesProcessedThisRound = new HashSet<AxisAlignedRectangle>();

            var width = this.Rectangles.FirstOrDefault()?.Width ?? 16;

            RepositionDirections RepLeftOf(AxisAlignedRectangle rectangle)
            {
                var found = this.GetRectangleAtPosition(rectangle.X - width, rectangle.Y);
                if (rectanglesProcessedThisRound.Contains(found)) return RepositionDirections.None;
                else return found?.RepositionDirections ?? RepositionDirections.None;
            }
            RepositionDirections RepRightOf(AxisAlignedRectangle rectangle)
            {
                var found = this.GetRectangleAtPosition(rectangle.X + width, rectangle.Y);
                if (rectanglesProcessedThisRound.Contains(found)) return RepositionDirections.None;
                else return found?.RepositionDirections ?? RepositionDirections.None;
            }
            RepositionDirections RepAbove(AxisAlignedRectangle rectangle)
            {
                var found = this.GetRectangleAtPosition(rectangle.X, rectangle.Y + width);
                if (rectanglesProcessedThisRound.Contains(found)) return RepositionDirections.None;
                else return found?.RepositionDirections ?? RepositionDirections.None;
            }
            RepositionDirections RepBelow(AxisAlignedRectangle rectangle)
            {
                var found = this.GetRectangleAtPosition(rectangle.X, rectangle.Y - width);
                if (rectanglesProcessedThisRound.Contains(found)) return RepositionDirections.None;
                else return found?.RepositionDirections ?? RepositionDirections.None;
            }

            while (rectanglesWithNoneReposition.Count > 0)
            {
                rectanglesProcessedThisRound.Clear();

                // see if any 
                // reverse loop to remove:
                for (int i = rectanglesWithNoneReposition.Count - 1; i > -1; i--)
                {
                    var rectangle = rectanglesWithNoneReposition[i];

                    rectanglesProcessedThisRound.Add(rectangle);

                    rectangle.RepositionDirections =
                        (RepLeftOf(rectangle) & RepositionDirections.Left) |
                        (RepRightOf(rectangle) & RepositionDirections.Right) |
                        (RepAbove(rectangle) & RepositionDirections.Up) |
                        (RepBelow(rectangle) & RepositionDirections.Down);

                    if (rectangle.RepositionDirections != RepositionDirections.None)
                    {
                        rectanglesWithNoneReposition.RemoveAt(i);
                    }
                    else
                    {
                        // this thing is still using "none" reposition, but if it is missing collisions in one of the corners, then we can 
                        // assign repositions appropriately.
                        var aboveRight = this.GetRectangleAtPosition(rectangle.X + width, rectangle.Y + width);
                        var aboveLeft = this.GetRectangleAtPosition(rectangle.X - width, rectangle.Y + width);
                        var belowRight = this.GetRectangleAtPosition(rectangle.X + width, rectangle.Y - width);
                        var belowLeft = this.GetRectangleAtPosition(rectangle.X - width, rectangle.Y - width);

                        if (aboveRight == null)
                        {
                            rectangle.RepositionDirections |= RepositionDirections.Right;
                            rectangle.RepositionDirections |= RepositionDirections.Up;
                        }
                        if (aboveLeft == null)
                        {
                            rectangle.RepositionDirections |= RepositionDirections.Left;
                            rectangle.RepositionDirections |= RepositionDirections.Up;
                        }
                        if (belowRight == null)
                        {
                            rectangle.RepositionDirections |= RepositionDirections.Right;
                            rectangle.RepositionDirections |= RepositionDirections.Down;
                        }
                        if (belowLeft == null)
                        {
                            rectangle.RepositionDirections |= RepositionDirections.Left;
                            rectangle.RepositionDirections |= RepositionDirections.Down;
                        }
                        if (rectangle.RepositionDirections != RepositionDirections.None)
                        {
                            rectanglesWithNoneReposition.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public void UpdateShapesForCloudCollision()
        {
            var count = this.mShapes.AxisAlignedRectangles.Count;
            for (int i = 0; i < count; i++)
            {
                var rectangle = this.mShapes.AxisAlignedRectangles[i];

                rectangle.RepositionHalfSize = true;

                rectangle.RepositionDirections = RepositionDirections.Up;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }



    public static class TileShapeCollectionLayeredTileMapExtensions
    {
        public static void AddCollisionFrom(this TileShapeCollection tileShapeCollection,
            LayeredTileMap layeredTileMap, string nameToUse)
        {
            AddCollisionFrom(tileShapeCollection, layeredTileMap,
                new List<string> { nameToUse });
        }

        public static void AddCollisionFrom(this TileShapeCollection tileShapeCollection,
            LayeredTileMap layeredTileMap, IEnumerable<string> namesToUse)
        {
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate = (list) =>
            {
                var nameProperty = list.FirstOrDefault(item => item.Name.ToLower() == "name");

                return namesToUse.Contains(nameProperty.Value);
            };

            AddCollisionFrom(tileShapeCollection, layeredTileMap, predicate);

        }

        public static void AddCollisionFrom(this TileShapeCollection tileShapeCollection,
            MapDrawableBatch layer, LayeredTileMap layeredTileMap, Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate)
        {
            tileShapeCollection.LeftSeedX = layeredTileMap.X;
            tileShapeCollection.BottomSeedY = layeredTileMap.Y - layeredTileMap.Height;

            var properties = layeredTileMap.TileProperties;

            foreach (var kvp in properties)
            {
                string name = kvp.Key;
                var namedValues = kvp.Value;

                if (predicate(namedValues))
                {
                    float dimension = layeredTileMap.WidthPerTile.Value;
                    float dimensionHalf = dimension / 2.0f;
                    tileShapeCollection.GridSize = dimension;

                    var dictionary = layer.NamedTileOrderedIndexes;

                    if (dictionary.ContainsKey(name))
                    {
                        var indexList = dictionary[name];

                        foreach (var index in indexList)
                        {
                            float left;
                            float bottom;
                            layer.GetBottomLeftWorldCoordinateForOrderedTile(index, out left, out bottom);

                            var centerX = left + dimensionHalf;
                            var centerY = bottom + dimensionHalf;
                            tileShapeCollection.AddCollisionAtWorld(centerX,
                                centerY);
                        }
                    }
                }
            }
        }

        public static void AddCollisionFrom(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap,
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate, bool removeTilesOnAdd = false)
        {
            tileShapeCollection.LeftSeedX = layeredTileMap.X;
            tileShapeCollection.BottomSeedY = layeredTileMap.Y - layeredTileMap.Height;

            var properties = layeredTileMap.TileProperties;

            foreach (var kvp in properties)
            {
                string name = kvp.Key;
                var namedValues = kvp.Value;

                if (predicate(namedValues))
                {
                    float dimension = layeredTileMap.WidthPerTile.Value;
                    float dimensionHalf = dimension / 2.0f;
                    tileShapeCollection.GridSize = dimension;

                    foreach (var layer in layeredTileMap.MapLayers)
                    {
                        List<int> indexesToRemove = null;
                        if (removeTilesOnAdd)
                        {
                            indexesToRemove = new List<int>();
                        }

                        var dictionary = layer.NamedTileOrderedIndexes;

                        if (dictionary.ContainsKey(name))
                        {
                            var indexList = dictionary[name];

                            foreach (var index in indexList)
                            {
                                float left;
                                float bottom;
                                layer.GetBottomLeftWorldCoordinateForOrderedTile(index, out left, out bottom);

                                var centerX = left + dimensionHalf;
                                var centerY = bottom + dimensionHalf;
                                tileShapeCollection.AddCollisionAtWorld(centerX,
                                    centerY);

                            }
                            if(removeTilesOnAdd)
                            {
                                indexesToRemove.AddRange(indexList);
                            }
                        }

                        if(removeTilesOnAdd && indexesToRemove.Count > 0)
                        {
                            layer.RemoveQuads(indexesToRemove);
                        }
                    }
                }
            }
        }

        public static void AddMergedCollisionFrom(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap,
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate, bool removeTilesOnAdd = false)
        {
            var properties = layeredTileMap.TileProperties;
            float dimension = layeredTileMap.WidthPerTile.Value;
            float dimensionHalf = dimension / 2.0f;
            tileShapeCollection.GridSize = dimension;

            tileShapeCollection.LeftSeedX = layeredTileMap.X;
            tileShapeCollection.BottomSeedY = layeredTileMap.Y - layeredTileMap.Height;

            Dictionary<int, List<int>> rectangleIndexes = new Dictionary<int, List<int>>();

            foreach (var layer in layeredTileMap.MapLayers)
            {
                AddCollisionFromLayerInternal(tileShapeCollection, predicate, properties, dimension, dimensionHalf, rectangleIndexes, layer, removeTilesOnAdd);
            }

            ApplyMerging(tileShapeCollection, dimension, rectangleIndexes);
        }

        public static void AddMergedCollisionFromLayer(this TileShapeCollection tileShapeCollection, MapDrawableBatch layer, LayeredTileMap layeredTileMap,
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate)
        {
            var properties = layeredTileMap.TileProperties;
            float dimension = layeredTileMap.WidthPerTile.Value;
            float dimensionHalf = dimension / 2.0f;
            tileShapeCollection.GridSize = dimension;

            tileShapeCollection.LeftSeedX = layeredTileMap.X;
            tileShapeCollection.BottomSeedY = layeredTileMap.Y - layeredTileMap.Height;

            Dictionary<int, List<int>> rectangleIndexes = new Dictionary<int, List<int>>();

            AddCollisionFromLayerInternal(tileShapeCollection, predicate, properties, dimension, dimensionHalf, rectangleIndexes, layer);

            ApplyMerging(tileShapeCollection, dimension, rectangleIndexes);
        }

        public static void AddCollisionFromTilesWithProperty(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap, string propertyName)
        {
            tileShapeCollection.AddCollisionFrom(
                layeredTileMap, (list) => list.Any(item => item.Name == propertyName));
        }

        public static void AddMergedCollisionFromTilesWithProperty(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap,
            string propertyName, bool removeTilesOnAdd = false)
        {
            tileShapeCollection.AddMergedCollisionFrom(
                layeredTileMap, (list) => list.Any(item => item.Name == propertyName), removeTilesOnAdd);
        }

        public static void AddCollisionFromTilesWithType(this TileShapeCollection tileShapeCollection, 
            LayeredTileMap layeredTileMap, string type, bool removeTilesOnAdd = false)
        {
            if(layeredTileMap != null)
            {
                tileShapeCollection.AddCollisionFrom(
                    layeredTileMap, 
                    (list) => list.Any(item => item.Name == "Type" && (item.Value as string) == type),
                    removeTilesOnAdd);
            }
        }

        public static void AddMergedCollisionFromTilesWithType(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap, string type)
        {
            if (layeredTileMap != null)
            {
                tileShapeCollection.AddMergedCollisionFrom(
                layeredTileMap, (list) => list.Any(item => item.Name == "Type" && (item.Value as string) == type));
            }
        }

        public static void MergeRectangles(this TileShapeCollection tileShapeCollection)
        {
            if (tileShapeCollection.Rectangles.Count > 1)
            {
                var z = tileShapeCollection.Rectangles[0].Z;

                var dimension = tileShapeCollection.GridSize;
                Dictionary<int, List<int>> rectangleIndexes = new Dictionary<int, List<int>>();



                for (int i = 0; i < tileShapeCollection.Rectangles.Count; i++)
                {
                    var rectangle = tileShapeCollection.Rectangles[i];

                    var centerX = rectangle.Position.X;
                    var centerY = rectangle.Position.Y;

                    int key;
                    int value;

                    if (tileShapeCollection.SortAxis == Axis.X)
                    {
                        key = (int)(centerX / dimension);
                        value = (int)(centerY / dimension);
                    }
                    else if (tileShapeCollection.SortAxis == Axis.Y)
                    {
                        key = (int)(centerY / dimension);
                        value = (int)(centerX / dimension);
                    }
                    else
                    {
                        throw new NotImplementedException("Cannot add tile collision on z-sorted shape collections");
                    }

                    List<int> listToAddTo = null;
                    if (rectangleIndexes.ContainsKey(key) == false)
                    {
                        listToAddTo = new List<int>();
                        rectangleIndexes.Add(key, listToAddTo);
                    }
                    else
                    {
                        listToAddTo = rectangleIndexes[key];
                    }

                    listToAddTo.Add(value);

                    if (rectangle.Visible)
                    {
                        rectangle.Visible = false;
                    }
                }

                tileShapeCollection.Rectangles.Clear();

                ApplyMerging(tileShapeCollection, dimension, rectangleIndexes, z);


            }
        }


        private static void ApplyMerging(TileShapeCollection tileShapeCollection, float dimension,
            Dictionary<int, List<int>> rectangleIndexes, float z = 0)
        {
            foreach (var kvp in rectangleIndexes.OrderBy(item => item.Key))
            {
                var rectanglePositionList = kvp.Value.OrderBy(item => item).ToList();

                var firstValue = rectanglePositionList[0];
                var currentValue = firstValue;
                var expectedValue = firstValue + 1;
                for (int i = 1; i < rectanglePositionList.Count; i++)
                {
                    if (rectanglePositionList[i] != expectedValue)
                    {
                        var innerRect = CloseRectangle(tileShapeCollection, kvp.Key, dimension, firstValue, currentValue);
                        innerRect.Z = z;
                        firstValue = rectanglePositionList[i];
                        currentValue = firstValue;
                    }
                    else
                    {
                        currentValue++;
                    }

                    expectedValue = currentValue + 1;
                }

                var outerRect = CloseRectangle(tileShapeCollection, kvp.Key, dimension, firstValue, currentValue);
                outerRect.Z = z;
            }
        }

        private static void AddCollisionFromLayerInternal(TileShapeCollection tileShapeCollection, Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate, Dictionary<string, List<TMXGlueLib.DataTypes.NamedValue>> properties, float dimension, float dimensionHalf, Dictionary<int, List<int>> rectangleIndexes, MapDrawableBatch layer, bool removeTilesOnAdd = false)
        {
            foreach (var kvp in properties)
            {
                string name = kvp.Key;
                var namedValues = kvp.Value;

                if (predicate(namedValues))
                {
                    List<int> indexesToRemove = null;
                    if (removeTilesOnAdd)
                    {
                        indexesToRemove = new List<int>();
                    }

                    var dictionary = layer.NamedTileOrderedIndexes;

                    if (dictionary.ContainsKey(name))
                    {
                        var indexList = dictionary[name];

                        foreach (var index in indexList)
                        {
                            float left;
                            float bottom;
                            layer.GetBottomLeftWorldCoordinateForOrderedTile(index, out left, out bottom);

                            var centerX = left + dimensionHalf;
                            var centerY = bottom + dimensionHalf;

                            int key;
                            int value;

                            if (tileShapeCollection.SortAxis == Axis.X)
                            {
                                key = (int)(centerX / dimension);
                                value = (int)(centerY / dimension);
                            }
                            else if (tileShapeCollection.SortAxis == Axis.Y)
                            {
                                key = (int)(centerY / dimension);
                                value = (int)(centerX / dimension);
                            }
                            else
                            {
                                throw new NotImplementedException("Cannot add tile collision on z-sorted shape collections");
                            }

                            List<int> listToAddTo = null;
                            if (rectangleIndexes.ContainsKey(key) == false)
                            {
                                listToAddTo = new List<int>();
                                rectangleIndexes.Add(key, listToAddTo);
                            }
                            else
                            {
                                listToAddTo = rectangleIndexes[key];
                            }
                            listToAddTo.Add(value);

                            if (removeTilesOnAdd)
                            {
                                indexesToRemove.AddRange(indexList);
                            }

                        }
                        if (removeTilesOnAdd && indexesToRemove.Count > 0)
                        {
                            layer.RemoveQuads(indexesToRemove);
                        }
                    }
                }
            }
        }

        private static AxisAlignedRectangle CloseRectangle(TileShapeCollection tileShapeCollection, int keyIndex, float dimension, int firstValue, int currentValue)
        {
            float x = 0;
            float y = 0;
            float width = dimension;
            float height = dimension;

            if (tileShapeCollection.SortAxis == Axis.X)
            {
                x = (keyIndex + .5f) * dimension;
            }
            else
            {
                // y moves down so we subtract
                y = (keyIndex - .5f) * dimension;
            }

            var centerIndex = (firstValue + currentValue) / 2.0f;

            if (tileShapeCollection.SortAxis == Axis.X)
            {
                y = (centerIndex - .5f) * dimension;
                height = (currentValue - firstValue + 1) * dimension;
            }
            else
            {
                x = (centerIndex + .5f) * dimension;
                width = (currentValue - firstValue + 1) * dimension;
            }

            return AddRectangleStrip(tileShapeCollection, x, y, width, height);
        }

        private static AxisAlignedRectangle AddRectangleStrip(TileShapeCollection tileShapeCollection, float x, float y, float width, float height)
        {
            AxisAlignedRectangle rectangle = new AxisAlignedRectangle();
            rectangle.X = x;
            rectangle.Y = y;
            rectangle.Width = width;
            rectangle.Height = height;

            if (tileShapeCollection.Visible)
            {
                rectangle.Visible = true;
            }

            tileShapeCollection.Rectangles.Add(rectangle);

            return rectangle;
        }

        static void AddCollisionFrom(this TileShapeCollection tileShapeCollection,
            Scene scene, IEnumerable<string> namesToUse)
        {
            // prob need to clear out the tileShapeCollection

            float dimension = float.NaN;
            float dimensionHalf = 0;

            for (int i = 0; i < scene.Sprites.Count; i++)
            {
                if (namesToUse.Contains(scene.Sprites[i].Name))
                {

                    if (float.IsNaN(dimension))
                    {
                        dimension = scene.Sprites[i].Width;
                        dimensionHalf = dimension / 2.0f;
                        tileShapeCollection.GridSize = dimension;
                    }

                    tileShapeCollection.AddCollisionAtWorld(scene.Sprites[i].X, scene.Sprites[i].Y);

                }

            }
        }

        public static void AddCollisionFrom(this TileShapeCollection tileShapeCollection,
            LayeredTileMap layeredTileMap)
        {

            var tilesWithCollision = layeredTileMap.TileProperties
                .Where(item => item.Value.Any(property => property.Name == "HasCollision" && (string)property.Value == "True"))
                .Select(item => item.Key).ToList();

            tileShapeCollection.AddCollisionFrom(layeredTileMap, tilesWithCollision);

        }

        public static void RemoveCollisionFrom(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap,
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate, bool removeTilesOnRemove = false)
        {
            tileShapeCollection.LeftSeedX = layeredTileMap.X;
            tileShapeCollection.BottomSeedY = layeredTileMap.Y - layeredTileMap.Height;

            var properties = layeredTileMap.TileProperties;

            foreach (var kvp in properties)
            {
                string name = kvp.Key;
                var namedValues = kvp.Value;

                if (predicate(namedValues))
                {
                    float dimension = layeredTileMap.WidthPerTile.Value;
                    float dimensionHalf = dimension / 2.0f;
                    tileShapeCollection.GridSize = dimension;

                    foreach (var layer in layeredTileMap.MapLayers)
                    {
                        List<int> indexesToRemove = null;
                        if (removeTilesOnRemove)
                        {
                            indexesToRemove = new List<int>();
                        }

                        var dictionary = layer.NamedTileOrderedIndexes;

                        if (dictionary.ContainsKey(name))
                        {
                            var indexList = dictionary[name];

                            foreach (var index in indexList)
                            {
                                float left;
                                float bottom;
                                layer.GetBottomLeftWorldCoordinateForOrderedTile(index, out left, out bottom);

                                var centerX = left + dimensionHalf;
                                var centerY = bottom + dimensionHalf;
                                //tileShapeCollection.AddCollisionAtWorld(centerX,
                                //    centerY);
                                tileShapeCollection.RemoveCollisionAtWorld(centerX, centerY);

                            }
                            if (removeTilesOnRemove)
                            {
                                indexesToRemove.AddRange(indexList);
                            }
                        }

                        if (removeTilesOnRemove && indexesToRemove.Count > 0)
                        {
                            layer.RemoveQuads(indexesToRemove);
                        }
                    }
                }
            }
        }

        public static void RemoveCollisionFrom(this TileShapeCollection tileShapeCollection, MapDrawableBatch layer,
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate, bool removeTilesOnRemove = false)
        {
            LayeredTileMap layeredTileMap = layer.Parent as LayeredTileMap;

            tileShapeCollection.LeftSeedX = layeredTileMap.X;
            tileShapeCollection.BottomSeedY = layeredTileMap.Y - layeredTileMap.Height;

            var properties = layeredTileMap.TileProperties;

            foreach (var kvp in properties)
            {
                string name = kvp.Key;
                var namedValues = kvp.Value;

                if (predicate(namedValues))
                {
                    float dimension = layeredTileMap.WidthPerTile.Value;
                    float dimensionHalf = dimension / 2.0f;
                    tileShapeCollection.GridSize = dimension;

                    List<int> indexesToRemove = null;
                    if (removeTilesOnRemove)
                    {
                        indexesToRemove = new List<int>();
                    }

                    var dictionary = layer.NamedTileOrderedIndexes;

                    if (dictionary.ContainsKey(name))
                    {
                        var indexList = dictionary[name];

                        foreach (var index in indexList)
                        {
                            float left;
                            float bottom;
                            layer.GetBottomLeftWorldCoordinateForOrderedTile(index, out left, out bottom);

                            var centerX = left + dimensionHalf;
                            var centerY = bottom + dimensionHalf;
                            //tileShapeCollection.AddCollisionAtWorld(centerX,
                            //    centerY);
                            tileShapeCollection.RemoveCollisionAtWorld(centerX, centerY);

                        }
                        if (removeTilesOnRemove)
                        {
                            indexesToRemove.AddRange(indexList);
                        }
                    }

                    if (removeTilesOnRemove && indexesToRemove.Count > 0)
                    {
                        layer.RemoveQuads(indexesToRemove);
                    }
                }
            }
        }

        public static void RemoveCollisionFromTilesWithType(this TileShapeCollection tileShapeCollection,
            LayeredTileMap layeredTileMap, string type, bool removeTilesOnAdd = false)
        {
            tileShapeCollection.RemoveCollisionFrom(
                layeredTileMap,
                (list) => list.Any(item => item.Name == "Type" && (item.Value as string) == type),
                removeTilesOnAdd);
        }

        public static void RemoveCollisionFromTilesWithType(this TileShapeCollection tileShapeCollection,
            MapDrawableBatch layer, string type, bool removeTilesOnAdd = false)
        {
            tileShapeCollection.RemoveCollisionFrom(
                layer,
                (list) => list.Any(item => item.Name == "Type" && (item.Value as string) == type),
                removeTilesOnAdd);
        }
    }


}
