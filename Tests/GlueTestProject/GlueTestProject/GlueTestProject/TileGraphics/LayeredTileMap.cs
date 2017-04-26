using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content;
using FlatRedBall.IO;
using FlatRedBall.Debugging;
using FlatRedBall.Performance.Measurement;
using System.IO;
using TMXGlueLib.DataTypes;
using TMXGlueLib;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;

namespace FlatRedBall.TileGraphics
{


    public class LayeredTileMap : PositionedObject, IVisible
    {
        #region Fields


        FlatRedBall.Math.PositionedObjectList<MapDrawableBatch> mMapLists = new FlatRedBall.Math.PositionedObjectList<MapDrawableBatch>();

        float mRenderingScale = 1;

        float mZSplit = 1;

        float? mNumberTilesWide;
        float? mNumberTilesTall;

        public float? WidthPerTile { get; private set; }
        public float? HeightPerTile { get; private set; }

        #endregion

        #region Properties

        public Dictionary<string, List<NamedValue>> TileProperties
        {
            get;
            private set;
        } = new Dictionary<string, List<NamedValue>>();


        public Dictionary<string, List<NamedValue>> ShapeProperties
        {
            get;
            private set;
        } = new Dictionary<string, List<NamedValue>>();


        public float RenderingScale
        {
            get
            {
                return mRenderingScale;
            }
            set
            {
                mRenderingScale = value;
                foreach (var map in mMapLists)
                {
                    map.RenderingScale = value;
                }

            }
        }

        public float ZSplit
        {
            get
            {
                return mZSplit;
            }
            set
            {
                for (int i = 0; i < this.mMapLists.Count; i++)
                {
                    mMapLists[i].RelativeZ = mZSplit * i;
                }
            }
        }

        public List<FlatRedBall.Math.Geometry.ShapeCollection> ShapeCollections { get; private set; } = new List<FlatRedBall.Math.Geometry.ShapeCollection>();


        public FlatRedBall.Math.PositionedObjectList<MapDrawableBatch> MapLayers
        {
            get
            {
                return mMapLists;
            }
        }

        bool visible = true;
        public bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                visible = value;
                foreach (var item in this.mMapLists)
                {
                    item.Visible = visible;
                }
            }
        }

        /// <summary>
        /// Returns the width in world units of the entire map.  The map may not visibly extend to this width;
        /// </summary>
        public float Width
        {
            get
            {
                if (mNumberTilesWide.HasValue && WidthPerTile.HasValue)
                {
                    return mNumberTilesWide.Value * WidthPerTile.Value;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Returns the height in world units of the entire map.  The map may not visibly extend to this height;
        /// </summary>
        public float Height
        {
            get
            {
                if (mNumberTilesTall.HasValue && HeightPerTile.HasValue)
                {
                    return mNumberTilesTall.Value * HeightPerTile.Value;
                }
                else
                {
                    return 0;
                }
            }
        }

        public LayeredTileMapAnimation Animation { get; set; }

        public List<NamedValue> MapProperties { get; set; }


        IVisible IVisible.Parent
        {
            get
            {
                return Parent as IVisible;
            }
        }

        public bool AbsoluteVisible
        {
            get
            {
                if (this.Visible)
                {
                    var parentAsIVisible = this.Parent as IVisible;

                    if (parentAsIVisible == null || IgnoresParentVisibility)
                    {
                        return true;
                    }
                    else
                    {
                        // this is true, so return if the parent is visible:
                        return parentAsIVisible.AbsoluteVisible;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IgnoresParentVisibility
        {
            get;
            set;
        }


        #endregion

        public IEnumerable<string> TileNamesWith(string propertyName)
        {
            foreach (var item in TileProperties.Values)
            {
                if (item.Any(item2 => item2.Name == propertyName))
                {
                    var hasName = item.Any(item2 => item2.Name == "Name");

                    if (hasName)
                    {
                        yield return item.First(item2 => item2.Name == "Name").Value as string;
                    }
                }
            }
        }


        public static LayeredTileMap FromScene(string fileName, string contentManagerName)
        {
            return FromScene(fileName, contentManagerName, false);
        }

        public static LayeredTileMap FromScene(string fileName, string contentManagerName, bool verifySameTexturePerLayer)
        {
            Section.GetAndStartContextAndTime("Initial FromScene");
            LayeredTileMap toReturn = new LayeredTileMap();

            string absoluteFileName = FileManager.MakeAbsolute(fileName);
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("FromFile");
            SceneSave ses = SceneSave.FromFile(absoluteFileName);
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("BreaksNStuff");

            string oldRelativeDirectory = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = FileManager.GetDirectory(absoluteFileName);

            var breaks = GetZBreaks(ses.SpriteList);

            int valueBefore = 0;

            MapDrawableBatch mdb;
            int valueAfter;

            float zValue = 0;
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Create MDBs");

            for (int i = 0; i < breaks.Count; i++)
            {
                valueAfter = breaks[i];

                int count = valueAfter - valueBefore;

                mdb = MapDrawableBatch.FromSpriteSaves(ses.SpriteList, valueBefore, count, contentManagerName, verifySameTexturePerLayer);
                mdb.AttachTo(toReturn, false);
                mdb.RelativeZ = zValue;
                toReturn.mMapLists.Add(mdb);
                zValue += toReturn.mZSplit;
                valueBefore = valueAfter;
            }

            valueAfter = ses.SpriteList.Count;
            if (valueBefore != valueAfter)
            {
                int count = valueAfter - valueBefore;

                mdb = MapDrawableBatch.FromSpriteSaves(ses.SpriteList, valueBefore, count, contentManagerName, verifySameTexturePerLayer);
                mdb.AttachTo(toReturn, false);
                mdb.RelativeZ = zValue;

                toReturn.mMapLists.Add(mdb);
            }
            Section.EndContextAndTime();
            FileManager.RelativeDirectory = oldRelativeDirectory;
            return toReturn;
        }

        public static LayeredTileMap FromReducedTileMapInfo(string fileName, string contentManagerName)
        {
            using (Stream inputStream = FileManager.GetStreamForFile(fileName))
            using (BinaryReader binaryReader = new BinaryReader(inputStream))
            {
                ReducedTileMapInfo rtmi = ReducedTileMapInfo.ReadFrom(binaryReader);



                string fullFileName = fileName;
                if (FileManager.IsRelative(fullFileName))
                {
                    fullFileName = FileManager.RelativeDirectory + fileName;
                }

                var toReturn = FromReducedTileMapInfo(rtmi, contentManagerName, fileName);
                toReturn.Name = fullFileName;
                return toReturn;
            }
        }

        public static LayeredTileMap FromReducedTileMapInfo(TMXGlueLib.DataTypes.ReducedTileMapInfo rtmi, string contentManagerName, string tilbToLoad)
        {
            var toReturn = new LayeredTileMap();

            string oldRelativeDirectory = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = FileManager.GetDirectory(tilbToLoad);

            MapDrawableBatch mdb;

            if (rtmi.NumberCellsWide != 0)
            {
                toReturn.mNumberTilesWide = rtmi.NumberCellsWide;
            }

            if (rtmi.NumberCellsTall != 0)
            {
                toReturn.mNumberTilesTall = rtmi.NumberCellsTall;
            }

            toReturn.WidthPerTile = rtmi.QuadWidth;
            toReturn.HeightPerTile = rtmi.QuadHeight;

            for (int i = 0; i < rtmi.Layers.Count; i++)
            {
                var reducedLayer = rtmi.Layers[i];

                mdb = MapDrawableBatch.FromReducedLayer(reducedLayer, toReturn, rtmi, contentManagerName);

                mdb.AttachTo(toReturn, false);
                mdb.RelativeZ = reducedLayer.Z;
                toReturn.mMapLists.Add(mdb);

            }
            FileManager.RelativeDirectory = oldRelativeDirectory;

            return toReturn;
        }

        public static LayeredTileMap FromTiledMapSave(string fileName, string contentManager)
        {
            TiledMapSave tms = TiledMapSave.FromFile(fileName);

            // Ultimately properties are tied to tiles by the tile name.
            // If a tile has no name but it has properties, those properties
            // will be lost in the conversion. Therefore, we have to add name properties.
            tms.NameUnnamedTilesetTiles();
            tms.NameUnnamedObjects();


            string directory = FlatRedBall.IO.FileManager.GetDirectory(fileName);

            var rtmi = ReducedTileMapInfo.FromTiledMapSave(
                tms, 1, 0, directory, FileReferenceType.Absolute);

            var toReturn = FromReducedTileMapInfo(rtmi, contentManager, fileName);


            foreach (var mapObjectgroup in tms.objectgroup)
            {
                int indexInAllLayers = tms.MapLayers.IndexOf(mapObjectgroup);

                var shapeCollection = tms.ToShapeCollection(mapObjectgroup.Name);
                if (shapeCollection != null && shapeCollection.IsEmpty == false)
                {
                    // This makes all shapes have the same Z as the index layer, which is useful if instantiating objects, so they're layered properly
                    shapeCollection.Shift(new Microsoft.Xna.Framework.Vector3(0, 0, indexInAllLayers));

                    shapeCollection.Name = mapObjectgroup.Name;
                    toReturn.ShapeCollections.Add(shapeCollection);
                }
            }

            foreach (var layer in tms.MapLayers)
            {
                var matchingLayer = toReturn.MapLayers.FirstOrDefault(item => item.Name == layer.Name);


                if (matchingLayer != null)
                {
                    if (layer is MapLayer)
                    {
                        var mapLayer = layer as MapLayer;
                        foreach (var propertyValues in mapLayer.properties)
                        {
                            matchingLayer.Properties.Add(new NamedValue
                            {
                                Name = propertyValues.StrippedName,
                                Value = propertyValues.value,
                                Type = propertyValues.Type
                            });
                        }

                        matchingLayer.Visible = mapLayer.visible == 1;
                    }
                }
            }


            foreach (var tileset in tms.Tilesets)
            {
                foreach (var tile in tileset.TileDictionary.Values)
                {
                    if (tile.properties.Count != 0)
                    {
                        // this needs a name:
                        string name = tile.properties.FirstOrDefault(item => item.StrippedName.ToLowerInvariant() == "name")?.value;
                        AddPropertiesToMap(tms, toReturn.TileProperties, tile.properties, name);
                    }
                }
            }

            foreach (var objectLayer in tms.objectgroup)
            {
                if (objectLayer.@object != null)
                {
                    foreach (var objectInstance in objectLayer.@object)
                    {
                        if (objectInstance.properties.Count != 0)
                        {
                            string name = objectInstance.Name;
                            // if name is null, check the properties:
                            if (string.IsNullOrEmpty(name))
                            {
                                name = objectInstance.properties.FirstOrDefault(item => item.StrippedNameLower == "name")?.value;
                            }
                            var properties = objectInstance.properties;

                            var objectInstanceIsTile = objectInstance.gid != null;

                            if (objectInstanceIsTile)
                            {
                                AddPropertiesToMap(tms, toReturn.TileProperties, properties, name);
                            }
                            else
                            {
                                AddPropertiesToMap(tms, toReturn.ShapeProperties, properties, name);
                            }
                        }
                    }
                }
            }

            var tmxDirectory = FileManager.GetDirectory(fileName);

            var animationDictionary = new Dictionary<string, AnimationChain>();

            // add animations
            foreach (var tileset in tms.Tilesets)
            {

                string tilesetImageFile = tmxDirectory + tileset.Images[0].Source;

                if (tileset.SourceDirectory != ".")
                {
                    tilesetImageFile = tmxDirectory + tileset.SourceDirectory + tileset.Images[0].Source;
                }

                var texture = FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(tilesetImageFile);

                foreach (var tile in tileset.Tiles.Where(item => item.Animation != null && item.Animation.Frames.Count != 0))
                {
                    var animation = tile.Animation;

                    var animationChain = new AnimationChain();
                    foreach (var frame in animation.Frames)
                    {
                        var animationFrame = new AnimationFrame();
                        animationFrame.FrameLength = frame.Duration / 1000.0f;
                        animationFrame.Texture = texture;

                        int tileIdRelative = frame.TileId;
                        int globalTileId = (int)(tileIdRelative + tileset.Firstgid);

                        int leftPixel;
                        int rightPixel;
                        int topPixel;
                        int bottomPixel;
                        TiledMapSave.GetPixelCoordinatesFromGid((uint)globalTileId, tileset, out leftPixel, out topPixel, out rightPixel, out bottomPixel);

                        animationFrame.LeftCoordinate = MapDrawableBatch.CoordinateAdjustment + leftPixel / (float)texture.Width;
                        animationFrame.RightCoordinate = -MapDrawableBatch.CoordinateAdjustment + rightPixel / (float)texture.Width;

                        animationFrame.TopCoordinate = MapDrawableBatch.CoordinateAdjustment + topPixel / (float)texture.Height;
                        animationFrame.BottomCoordinate = -MapDrawableBatch.CoordinateAdjustment + bottomPixel / (float)texture.Height;


                        animationChain.Add(animationFrame);
                    }

                    var property = tile.properties.FirstOrDefault(item => item.StrippedNameLower == "name");

                    if (property == null)
                    {
                        throw new InvalidOperationException(
                            $"The tile with ID {tile.id} has an animation, but it doesn't have a Name property, which is required for animation.");
                    }
                    else
                    {
                        animationDictionary.Add(property.value, animationChain);
                    }

                }

            }

            toReturn.Animation = new LayeredTileMapAnimation(animationDictionary);

            toReturn.MapProperties = tms.properties
                .Select(propertySave => new NamedValue
                { Name = propertySave.name, Value = propertySave.value, Type = propertySave.Type })
                .ToList();


            return toReturn;
        }

        private static void AddPropertiesToMap(TiledMapSave tms, Dictionary<string, List<NamedValue>> dictionaryToAddTo, List<property> properties, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                List<NamedValue> namedValues = new List<NamedValue>();
                foreach (var prop in properties)
                {
                    namedValues.Add(new NamedValue()
                    { Name = prop.StrippedName, Value = prop.value, Type = prop.Type });
                }


#if DEBUG
                if (dictionaryToAddTo.Any(item => item.Key == name))
                {
                    // Assume it was a duplicate tile name, but it may not be
                    string message = $"The tileset contains more than one tile with the name {name}. Names must be unique in a tileset.";
                    bool hasDuplicateObject = tms.objectgroup.Any(item => item.@object.Any(objectInstance => objectInstance.Name == name));
                    if (hasDuplicateObject)
                    {
                        message = $"The tileset contains a tile with the name {name}, but this name is already used in an object layer";
                    }
                    throw new InvalidOperationException(message);
                }
#endif

                dictionaryToAddTo.Add(name, namedValues);

            }
        }



        public void AnimateSelf()
        {
            if (Animation != null)
            {
                Animation.Activity(this);
            }
        }

        public void AddToManagers()
        {
            AddToManagers(null);
        }

        public void AddToManagers(FlatRedBall.Graphics.Layer layer)
        {
            bool isAlreadyManaged = SpriteManager.ManagedPositionedObjects
                .Contains(this);

            // This allows AddToManagers to be called multiple times, so it can be added to multiple layers
            if (!isAlreadyManaged)
            {
                SpriteManager.AddPositionedObject(this);
            }
            foreach (var item in this.mMapLists)
            {
                item.AddToManagers(layer);
            }
        }

        //Write some addtomanagers and remove methods

        static List<int> GetZBreaks(List<SpriteSave> list)
        {
            List<int> zBreaks = new List<int>();

            GetZBreaks(list, zBreaks);

            return zBreaks;

        }

        static void GetZBreaks(List<SpriteSave> list, List<int> zBreaks)
        {
            zBreaks.Clear();

            if (list.Count == 0 || list.Count == 1)
                return;

            for (int i = 1; i < list.Count; i++)
            {
                if (list[i].Z != list[i - 1].Z)
                    zBreaks.Add(i);
            }
        }

        public LayeredTileMap Clone()
        {
            var toReturn = base.Clone<LayeredTileMap>();

            toReturn.mMapLists = new Math.PositionedObjectList<MapDrawableBatch>();

            foreach (var item in this.MapLayers)
            {
                var clonedLayer = item.Clone();
                if (item.Parent == this)
                {
                    clonedLayer.AttachTo(toReturn, false);
                }
                toReturn.mMapLists.Add(clonedLayer);
            }

            toReturn.ShapeCollections = new List<Math.Geometry.ShapeCollection>();
            foreach (var shapeCollection in this.ShapeCollections)
            {
                toReturn.ShapeCollections.Add(shapeCollection.Clone());
            }

            return toReturn;
        }

        public void RemoveFromManagersOneWay()
        {
            this.mMapLists.MakeOneWay();
            for (int i = 0; i < mMapLists.Count; i++)
            {
                SpriteManager.RemoveDrawableBatch(this.mMapLists[i]);
            }

            SpriteManager.RemovePositionedObject(this);

            this.mMapLists.MakeTwoWay();
        }

        public void Destroy()
        {
            // Make it one-way because we want the 
            // contained objects to persist after a destroy
            mMapLists.MakeOneWay();

            for (int i = 0; i < mMapLists.Count; i++)
            {
                SpriteManager.RemoveDrawableBatch(mMapLists[i]);
            }

            SpriteManager.RemovePositionedObject(this);

            mMapLists.MakeTwoWay();
        }
    }



    public static class LayeredTileMapExtensions
    {
        public static void RemoveTiles(this LayeredTileMap map,
            Func<List<NamedValue>, bool> predicate,
            Dictionary<string, List<NamedValue>> Properties)
        {
            // Force execution now for performance reasons
            var filteredInfos = Properties.Values.Where(predicate).ToList();

            foreach (var layer in map.MapLayers)
            {
                RemoveTilesFromLayer(filteredInfos, layer);
            }
        }

        public static void RemoveTiles(this MapDrawableBatch layer,
            Func<List<NamedValue>, bool> predicate,
            Dictionary<string, List<NamedValue>> Properties)
        {
            // Force execution now for performance reasons
            var filteredInfos = Properties.Values.Where(predicate).ToList();

            RemoveTilesFromLayer(filteredInfos, layer);
        }

        private static void RemoveTilesFromLayer(List<List<NamedValue>> filteredInfos, MapDrawableBatch layer)
        {
            List<int> indexes = new List<int>();

            foreach (var itemThatPasses in filteredInfos)
            {
                string tileName = itemThatPasses
                    .FirstOrDefault(item => item.Name.ToLowerInvariant() == "name")
                    .Value as string;


                if (layer.NamedTileOrderedIndexes.ContainsKey(tileName))
                {
                    var intsOnThisLayer =
                        layer.NamedTileOrderedIndexes[tileName];

                    indexes.AddRange(intsOnThisLayer);
                }
            }


            layer.RemoveQuads(indexes);
        }
    }
}
