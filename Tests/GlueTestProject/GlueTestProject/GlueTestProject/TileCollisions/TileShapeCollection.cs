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

        public string Name { get; set; }

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

        public bool CollideAgainst(AxisAlignedRectangle rectangle)
        {
            return mShapes.CollideAgainst(rectangle, true, mSortAxis);
        }

        public bool CollideAgainst(Circle circle)
        {
            return mShapes.CollideAgainst(circle, true, mSortAxis);
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

        public AxisAlignedRectangle GetTileAt(float x, float y)
        {
            float roundedX = MathFunctions.RoundFloat(x, GridSize, mLeftSeedX + GridSize / 2.0f);
            float roundedY = MathFunctions.RoundFloat(y, GridSize, mBottomSeedY + GridSize / 2.0f);
            float keyValue = GetKeyValue(roundedX, roundedY);

            float keyValueBefore = keyValue - GridSize / 2.0f;
            float keyValueAfter = keyValue + GridSize / 2.0f;

            int startInclusive = mShapes.AxisAlignedRectangles.GetFirstAfter(keyValueBefore, mSortAxis,
                0, mShapes.AxisAlignedRectangles.Count);


            int endExclusive = mShapes.AxisAlignedRectangles.GetFirstAfter(keyValueAfter, mSortAxis,
                0, mShapes.AxisAlignedRectangles.Count);

            AxisAlignedRectangle toReturn = GetTileAt(x, y, startInclusive, endExclusive);

            return toReturn;
        }

        private AxisAlignedRectangle GetTileAt(float x, float y, int startInclusive, int endExclusive)
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
            if (GetTileAt(x, y) == null)
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

                float keyValue = GetKeyValue(roundedX, roundedY);

                int index = mShapes.AxisAlignedRectangles.GetFirstAfter(keyValue, mSortAxis,
                    0, mShapes.AxisAlignedRectangles.Count);

                mShapes.AxisAlignedRectangles.Insert(index, newAar);

                UpdateRepositionDirectionsFor(newAar);
            }
        }

        public void RemoveCollisionAtWorld(float x, float y)
        {
            AxisAlignedRectangle existing = GetTileAt(x, y);
            if (existing != null)
            {
                ShapeManager.Remove(existing);

                float keyValue = GetKeyValue(existing.X, existing.Y);

                float keyValueBefore = keyValue - GridSize * 3 / 2.0f;
                float keyValueAfter = keyValue + GridSize * 3 / 2.0f;

                int before = Rectangles.GetFirstAfter(keyValueBefore, mSortAxis, 0, Rectangles.Count);
                int after = Rectangles.GetFirstAfter(keyValueAfter, mSortAxis, 0, Rectangles.Count);

                AxisAlignedRectangle leftOf = GetTileAt(existing.X - GridSize, existing.Y, before, after);
                AxisAlignedRectangle rightOf = GetTileAt(existing.X + GridSize, existing.Y, before, after);
                AxisAlignedRectangle above = GetTileAt(existing.X, existing.Y + GridSize, before, after);
                AxisAlignedRectangle below = GetTileAt(existing.X, existing.Y - GridSize, before, after);

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

        private float GetKeyValue(float x, float y)
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

        private void UpdateRepositionDirectionsFor(AxisAlignedRectangle newAar)
        {
            // Let's see what is surrounding this rectangle and update it and the surrounding rects appropriately
            float keyValue = GetKeyValue(newAar.X, newAar.Y);

            float keyValueBefore = keyValue - GridSize * 3 / 2.0f;
            float keyValueAfter = keyValue + GridSize * 3 / 2.0f;

            int before = Rectangles.GetFirstAfter(keyValueBefore, mSortAxis, 0, Rectangles.Count);
            int after = Rectangles.GetFirstAfter(keyValueAfter, mSortAxis, 0, Rectangles.Count);

            AxisAlignedRectangle leftOf = GetTileAt(newAar.X - GridSize, newAar.Y, before, after);
            AxisAlignedRectangle rightOf = GetTileAt(newAar.X + GridSize, newAar.Y, before, after);
            AxisAlignedRectangle above = GetTileAt(newAar.X, newAar.Y + GridSize, before, after);
            AxisAlignedRectangle below = GetTileAt(newAar.X, newAar.Y - GridSize, before, after);

            RepositionDirections directions = RepositionDirections.All;
            if (leftOf != null)
            {
                directions -= RepositionDirections.Left;
                if ((leftOf.RepositionDirections & RepositionDirections.Right) == RepositionDirections.Right)
                {
                    leftOf.RepositionDirections -= RepositionDirections.Right;
                }
            }
            if (rightOf != null)
            {
                directions -= RepositionDirections.Right;

                if ((rightOf.RepositionDirections & RepositionDirections.Left) == RepositionDirections.Left)
                {
                    rightOf.RepositionDirections -= RepositionDirections.Left;
                }
            }
            if (above != null)
            {
                directions -= RepositionDirections.Up;

                if ((above.RepositionDirections & RepositionDirections.Down) == RepositionDirections.Down)
                {
                    above.RepositionDirections -= RepositionDirections.Down;
                }
            }
            if (below != null)
            {
                directions -= RepositionDirections.Down;
                if ((below.RepositionDirections & RepositionDirections.Up) == RepositionDirections.Up)
                {
                    below.RepositionDirections -= RepositionDirections.Up;
                }
            }

            newAar.RepositionDirections = directions;

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
                    break;
                case Axis.Y:
                    mShapes.AxisAlignedRectangles.SortYInsertionAscending();
                    break;
                case Axis.Z:
                    mShapes.AxisAlignedRectangles.SortZInsertionAscending();
                    break;
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

            Dictionary<int, List<int>> rectangleIndexes = new Dictionary<int, List<int>>();

            foreach (var kvp in properties)
            {
                string name = kvp.Key;
                var namedValues = kvp.Value;

                if (predicate(namedValues))
                {
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
