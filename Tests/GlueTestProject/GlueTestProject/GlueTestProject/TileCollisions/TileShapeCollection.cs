using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.TileGraphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.TileCollisions
{
    public partial class TileShapeCollection
    {
        #region Fields

        ShapeCollection mShapes;
        Axis mSortAxis = Axis.X;
        float mLeftSeedX = 0;
        float mBottomSeedY = 0;
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
            float middleOfTileX = MathFunctions.RoundFloat(worldX, GridSize, mLeftSeedX + GridSize / 2.0f);
            float middleOfTileY = MathFunctions.RoundFloat(worldY, GridSize, mBottomSeedY + GridSize / 2.0f);
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
            float middleOfTileX = MathFunctions.RoundFloat(worldX, GridSize, mLeftSeedX + GridSize / 2.0f);
            float middleOfTileY = MathFunctions.RoundFloat(worldY, GridSize, mBottomSeedY + GridSize / 2.0f);
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
            float middleOfTileX = MathFunctions.RoundFloat(worldX, GridSize, mLeftSeedX + GridSize / 2.0f);
            float middleOfTileY = MathFunctions.RoundFloat(worldY, GridSize, mBottomSeedY + GridSize / 2.0f);

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
                float roundedX = MathFunctions.RoundFloat(x - GridSize / 2.0f, GridSize, mLeftSeedX);
                float roundedY = MathFunctions.RoundFloat(y - GridSize / 2.0f, GridSize, mBottomSeedY);

                AxisAlignedRectangle newAar = new AxisAlignedRectangle();
                newAar.Width = GridSize;
                newAar.Height = GridSize;
                newAar.Left = roundedX;
                newAar.Bottom = roundedY;

                if (this.mVisible)
                {
                    newAar.Visible = true;
                }

                float keyValue = GetCoordinateValueForPartitioning(roundedX, roundedY);

                int index = mShapes.AxisAlignedRectangles.GetFirstAfter(keyValue, mSortAxis,
                    0, mShapes.AxisAlignedRectangles.Count);

                mShapes.AxisAlignedRectangles.Insert(index, newAar);

                var directions = UpdateRepositionForNeighborsAndGetThisRepositionDirection(newAar);

                newAar.RepositionDirections = directions;
            }
        }

        public void RemoveCollisionAtWorld(float x, float y)
        {
            AxisAlignedRectangle existing = GetTileAt(x, y);
            if (existing != null)
            {
                ShapeManager.Remove(existing);

                float keyValue = GetCoordinateValueForPartitioning(existing.X, existing.Y);

                float keyValueBefore = keyValue - GridSize * 3 / 2.0f;
                float keyValueAfter = keyValue + GridSize * 3 / 2.0f;

                int before = Rectangles.GetFirstAfter(keyValueBefore, mSortAxis, 0, Rectangles.Count);
                int after = Rectangles.GetFirstAfter(keyValueAfter, mSortAxis, 0, Rectangles.Count);

                AxisAlignedRectangle leftOf = GetRectangleAtPosition(existing.X - GridSize, existing.Y, before, after);
                AxisAlignedRectangle rightOf = GetRectangleAtPosition(existing.X + GridSize, existing.Y, before, after);
                AxisAlignedRectangle above = GetRectangleAtPosition(existing.X, existing.Y + GridSize, before, after);
                AxisAlignedRectangle below = GetRectangleAtPosition(existing.X, existing.Y - GridSize, before, after);

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

        private RepositionDirections UpdateRepositionForNeighborsAndGetThisRepositionDirection(PositionedObject positionedObject)
        {
            // Let's see what is surrounding this rectangle and update it and the surrounding rects appropriately
            float keyValue = GetCoordinateValueForPartitioning(positionedObject.Position.X, positionedObject.Position.Y);

            float keyValueBefore = keyValue - GridSize * 3 / 2.0f;
            float keyValueAfter = keyValue + GridSize * 3 / 2.0f;

            int rectanglesBeforeIndex = Rectangles.GetFirstAfter(keyValueBefore, mSortAxis, 0, Rectangles.Count);
            int rectanglesAfterIndex = Rectangles.GetFirstAfter(keyValueAfter, mSortAxis, 0, Rectangles.Count);

            int polygonsBeforeIndex = Polygons.GetFirstAfter(keyValueBefore, mSortAxis, 0, Polygons.Count);
            int polygonsAfterIndex = Rectangles.GetFirstAfter(keyValueAfter, mSortAxis, 0, Polygons.Count);


            float leftOfX = positionedObject.Position.X - GridSize;
            float rightOfX = positionedObject.Position.X + GridSize;
            float middleX = positionedObject.Position.X;

            float aboveY = positionedObject.Position.Y + GridSize;
            float belowY = positionedObject.Position.Y - GridSize;
            float middleY = positionedObject.Position.Y;

            AxisAlignedRectangle rectangleLeftOf = GetRectangleAtPosition(leftOfX, middleY, rectanglesBeforeIndex, rectanglesAfterIndex);
            AxisAlignedRectangle rectangleRightOf = GetRectangleAtPosition(rightOfX, middleY, rectanglesBeforeIndex, rectanglesAfterIndex);
            AxisAlignedRectangle rectangleAbove = GetRectangleAtPosition(middleX, aboveY, rectanglesBeforeIndex, rectanglesAfterIndex);
            AxisAlignedRectangle rectangleBelow = GetRectangleAtPosition(middleX, belowY, rectanglesBeforeIndex, rectanglesAfterIndex);

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

        public static void AddCollisionFrom(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap,
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate)
        {
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
        }

        public static void AddMergedCollisionFrom(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap,
            Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate)
        {
            var properties = layeredTileMap.TileProperties;
            float dimension = layeredTileMap.WidthPerTile.Value;
            float dimensionHalf = dimension / 2.0f;
            tileShapeCollection.GridSize = dimension;

            Dictionary<int, List<int>> rectangleIndexes = new Dictionary<int, List<int>>();

            foreach (var layer in layeredTileMap.MapLayers)
            {
                AddCollisionFromLayerInternal(tileShapeCollection, predicate, properties, dimension, dimensionHalf, rectangleIndexes, layer);
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

            Dictionary<int, List<int>> rectangleIndexes = new Dictionary<int, List<int>>();

            AddCollisionFromLayerInternal(tileShapeCollection, predicate, properties, dimension, dimensionHalf, rectangleIndexes, layer);

            ApplyMerging(tileShapeCollection, dimension, rectangleIndexes);
        }

        public static void AddCollisionFromTilesWithProperty(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap, string propertyName)
        {
            tileShapeCollection.AddCollisionFrom(
                layeredTileMap, (list) => list.Any(item => item.Name == propertyName));

        }

        public static void AddMergedCollisionFromTilesWithProperty(this TileShapeCollection tileShapeCollection, LayeredTileMap layeredTileMap, string propertyName)
        {
            tileShapeCollection.AddMergedCollisionFrom(
                layeredTileMap, (list) => list.Any(item => item.Name == propertyName));

        }

        private static void ApplyMerging(TileShapeCollection tileShapeCollection, float dimension, Dictionary<int, List<int>> rectangleIndexes)
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
                        CloseRectangle(tileShapeCollection, kvp.Key, dimension, firstValue, currentValue);

                        firstValue = rectanglePositionList[i];
                        currentValue = firstValue;
                    }
                    else
                    {
                        currentValue++;
                    }

                    expectedValue = currentValue + 1;
                }

                CloseRectangle(tileShapeCollection, kvp.Key, dimension, firstValue, currentValue);
            }
        }

        private static void AddCollisionFromLayerInternal(TileShapeCollection tileShapeCollection, Func<List<TMXGlueLib.DataTypes.NamedValue>, bool> predicate, Dictionary<string, List<TMXGlueLib.DataTypes.NamedValue>> properties, float dimension, float dimensionHalf, Dictionary<int, List<int>> rectangleIndexes, MapDrawableBatch layer)
        {
            foreach (var kvp in properties)
            {
                string name = kvp.Key;
                var namedValues = kvp.Value;

                if (predicate(namedValues))
                {

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

                        }
                    }
                }
            }
        }

        private static void CloseRectangle(TileShapeCollection tileShapeCollection, int keyIndex, float dimension, int firstValue, int currentValue)
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

            AddRectangleStrip(tileShapeCollection, x, y, width, height);
        }

        private static void AddRectangleStrip(TileShapeCollection tileShapeCollection, float x, float y, float width, float height)
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

    }


}
