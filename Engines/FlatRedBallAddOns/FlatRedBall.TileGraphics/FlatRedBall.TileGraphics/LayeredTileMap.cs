using $PROJECT_NAMESPACE$.DataTypes;
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

namespace FlatRedBall.TileGraphics
{
    public struct NamedValue
    {
        public string Name;
        public object Value;

        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }

    public class LayeredTileMap : PositionedObject, IVisible
    {
        #region Fields


        FlatRedBall.Math.PositionedObjectList<MapDrawableBatch> mMapLists = new FlatRedBall.Math.PositionedObjectList<MapDrawableBatch>();

        float mRenderingScale = 1;

        float mZSplit = 1;

        float? mNumberTilesWide;
        float? mNumberTilesTall;

        float? mWidthPerTile;
        float? mHeightPerTile;

        #endregion

        #region Properties

        public Dictionary<string, List<NamedValue>> Properties
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
                if (mNumberTilesWide.HasValue && mWidthPerTile.HasValue)
                {
                    return mNumberTilesWide.Value * mWidthPerTile.Value;
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
                if (mNumberTilesTall.HasValue && mHeightPerTile.HasValue)
                {
                    return mNumberTilesTall.Value * mHeightPerTile.Value;
                }
                else
                {
                    return 0;
                }
            }
        }

        public LayeredTileMapAnimation Animation { get; set; }


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
            foreach (var item in Properties.Values)
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

            toReturn.mWidthPerTile = rtmi.QuadWidth;
            toReturn.mHeightPerTile = rtmi.QuadHeight;

            for (int i = 0; i < rtmi.Layers.Count; i++)
            {
                var reducedLayer = rtmi.Layers[i];

                mdb = MapDrawableBatch.FromReducedLayer(reducedLayer, rtmi, contentManagerName);

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


            string directory = FlatRedBall.IO.FileManager.GetDirectory(fileName);

            var rtmi = ReducedTileMapInfo.FromTiledMapSave(
                tms, 1, 0, directory, FileReferenceType.Absolute);

            var toReturn = FromReducedTileMapInfo(rtmi, contentManager, fileName);

            foreach (var layer in tms.Layers)
            {
                var matchingLayer = toReturn.MapLayers.FirstOrDefault(item => item.Name == layer.Name);


                if (matchingLayer != null)
                {
                    foreach (var propertyValues in layer.properties)
                    {
                        matchingLayer.Properties.Add(new NamedValue { Name = propertyValues.StrippedName, Value = propertyValues.value });
                    }

                    matchingLayer.Visible = layer.visible == 1;

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

                        if (!string.IsNullOrEmpty(name))
                        {
                            List<NamedValue> namedValues = new List<NamedValue>();
                            foreach (var prop in tile.properties)
                            {
                                namedValues.Add(new NamedValue() { Name = prop.StrippedName, Value = prop.value });
                            }

                            toReturn.Properties.Add(name, namedValues);

                        }
                    }
                }
            }

            return toReturn;
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
            foreach (var item in this.mMapLists)
            {
                item.AddToManagers();
            }
        }

        public void AddToManagers(FlatRedBall.Graphics.Layer layer)
        {
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


        public void RemoveFromManagersOneWay()
        {
            this.mMapLists.MakeOneWay();
            for (int i = 0; i < mMapLists.Count; i++)
            {
                SpriteManager.RemoveDrawableBatch(this.mMapLists[i]);
            }

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
}
