using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Threading.Tasks;
using FlatRedBall;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Content;
using FlatRedBall.Content.AI.Pathfinding;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Polygon;
using FlatRedBall.Content.Scene;
using FlatRedBall.IO;
using FlatRedBall.Math.Geometry;
using System.Globalization;

namespace TMXGlueLib
{
    #region FileReferenceType enum
    public enum FileReferenceType
    {
        NoDirectory,
        Absolute,
        Relative
    }
    #endregion

    public partial class TiledMapSave
    {
        #region Enums

        public enum CSVPropertyType { Tile, Layer, Map, Object };

        enum LessOrGreaterDesired
        {
            Less,
            Greater,
            NoChange
        }
        #endregion

        #region Fields

        public static LayerVisibleBehavior LayerVisibleBehaviorValue = LayerVisibleBehavior.Ignore;
        public static int MaxDegreeOfParallelism = 1;

        const string animationColumnName = "EmbeddedAnimation (List<FlatRedBall.Content.AnimationChain.AnimationFrameSaveBase>)";


        private static Tuple<float, float, float> _offset = new Tuple<float, float, float>(0f, 0f, 0f);

        #endregion

        #region Properties

        public static Tuple<float, float, float> Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        #endregion

        public Scene ToScene(string contentManagerName, float scale)
        {
            var scene = ToSceneSave(scale);
            return scene.ToScene(contentManagerName);
        }

        public void NameUnnamedTilesetTiles()
        {
            foreach (var tileset in this.Tilesets)
            {
                foreach (var tileDictionary in tileset.TileDictionary)
                {
                    var propertyList = tileDictionary.Value.properties;
                    var nameProperty = propertyList.FirstOrDefault(item => item.StrippedNameLower == "name");

                    if (nameProperty == null)
                    {
                        // create a new property:
                        var newNameProperty = new property();
                        newNameProperty.name = "Name";
                        newNameProperty.value = tileset.Name + tileDictionary.Key + "_autoname";

                        propertyList.Add(newNameProperty);

                        tileDictionary.Value.ForceRebuildPropertyDictionary();
                    }
                }
            }
        }

        public void NameUnnamedObjects()
        {
            int index = 0;
            foreach (var objectLayer in this.objectgroup)
            {
                // Seems like this can be null, not sure why...
                if (objectLayer.@object != null)
                {

                    foreach (var objectInstance in objectLayer.@object)
                    {
                        bool hasName = string.IsNullOrEmpty(objectInstance.Name) == false;
                        bool hasNameProperty = objectInstance.properties.Any(item => item.StrippedNameLower == "name");

                        if (!hasName && !hasNameProperty)
                        {
                            objectInstance.Name = $"object{index}_autoname";
                            objectInstance.properties.Add(new TMXGlueLib.property { name = "name", value = objectInstance.Name });
                            index++;

                        }
                        else if (hasName && !hasNameProperty)
                        {
                            objectInstance.properties.Add(new TMXGlueLib.property { name = "name", value = objectInstance.Name });
                        }
                    }
                }
            }
        }

        public string ToCSVString(CSVPropertyType type = CSVPropertyType.Tile, string layerName = null)
        {
            var sb = new StringBuilder();
            IEnumerable<string> columnsAsEnumerable = GetColumnNames(type, layerName);
            var columnList = columnsAsEnumerable as IList<string> ?? columnsAsEnumerable.ToList();
            WriteColumnHeader(sb, columnList);
            WriteColumnValues(sb, columnList, type, layerName);

            return sb.ToString();
        }

        private void WriteColumnValues(StringBuilder sb, IList<string> columnNames, CSVPropertyType type, string layerName)
        {
            columnNames = columnNames.Select(item => property.GetStrippedName(item)).ToList();


            // TODO: There is probably a good way to refactor this code
            switch (type)
            {
                case CSVPropertyType.Tile:
                    WriteColumnValuesForTile(sb, columnNames);
                    break;
                case CSVPropertyType.Layer:

                    WriteColumnValuesForLayer(sb, columnNames, layerName);
                    break;
                case CSVPropertyType.Map:
                    WriteValuesFromDictionary(sb, null, PropertyDictionary, columnNames, null);
                    break;
                case CSVPropertyType.Object:
                    this.objectgroup.Where(
                        og =>
                        layerName == null ||
                        (((AbstractMapLayer)og).Name != null && ((AbstractMapLayer)og).Name.Equals(layerName, StringComparison.OrdinalIgnoreCase)))
                        .SelectMany(o => o.@object, (o, c) => new { group = o, obj = c, X = c.x, Y = c.y })
                        .Where(o => o.obj.gid != null)
                        .ToList()
                        .ForEach(o => WriteValuesFromDictionary(sb, o.group.PropertyDictionary, o.obj.PropertyDictionary, columnNames, null));
                    break;
            }
        }

        private void WriteColumnValuesForLayer(StringBuilder sb, IList<string> columnNames, string layerName)
        {
            var availableItems =
            this.Layers.Where(
                l =>
                layerName == null ||
                (l.Name != null && l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase))).ToList();

            foreach (var l in availableItems)
            {
                WriteValuesFromDictionary(sb, null, l.PropertyDictionary, columnNames, null);
            }

        }

        private void WriteColumnValuesForTile(StringBuilder sb, IList<string> columnNames)
        {
            for (int i = 0; i < this.Tilesets.Count; i++)
            {
                Tileset tileSet = this.Tilesets[i];

                if (tileSet.Tiles != null)
                {
                    Func<mapTilesetTile, bool> predicate =
                        t => t.PropertyDictionary.Count > 0 ||
                        (t.Animation != null && t.Animation.Frames != null && t.Animation.Frames.Count > 0);

                    foreach (mapTilesetTile tile in tileSet.Tiles.Where(predicate))
                    {
                        Dictionary<string, string> propertyDictionary = new Dictionary<string, string>(tile.PropertyDictionary);

                        bool needsName = propertyDictionary.Count != 0 ||
                            (tile.Animation != null && tile.Animation.Frames != null && tile.Animation.Frames.Count != 0);

                        if (needsName && propertyDictionary.Keys.Any(item => property.GetStrippedName(item).ToLowerInvariant() == "name") == false)
                        {
                            var globalId = tile.id + tileSet.Firstgid;
                            // This has properties, but no name, so let's give it a name!
                            propertyDictionary.Add("Name (required, string)", "Unnamed" + globalId);
                        }
                        WriteValuesFromDictionary(sb, null, propertyDictionary, columnNames, tile.Animation, i);
                    }
                }
            }
            foreach (var objectGroup in this.objectgroup)
            {
                foreach (var @object in objectGroup.@object)
                {
                    if (@object.gid != null)
                    {
                        WriteValuesFromDictionary(sb, null, @object.PropertyDictionary, columnNames, null);
                    }
                }
            }
        }

        static int numberOfUnnamedTiles = 0;

        private void WriteValuesFromDictionary(StringBuilder sb, IDictionary<string, string> pDictionary,
            IDictionary<string, string> iDictionary, IEnumerable<string> columnNames, TileAnimation animation, int tilesetIndex = 0)
        {


            ///////////////////// Early out //////////////////////

            if (tilesetIndex >= Tilesets.Count)
            {
                return;
            }
            ////////////////// End early out ////////////////////
            uint startGid = Tilesets[tilesetIndex].Firstgid;

            string nameValue = GetNameValue(iDictionary);

            List<string> row = new List<string>();
            row.Add(nameValue);

            int layerIndex = -1;



            uint endIdExclusive = uint.MaxValue;
            if (tilesetIndex < Tilesets.Count - 1)
            {
                endIdExclusive = Tilesets[tilesetIndex + 1].Firstgid;
            }


            for (int i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                // see if any layers reference this tile:
                foreach (var data in layer.data)
                {
                    foreach (var tile in data.tiles)
                    {
                        if (tile >= startGid && tile < endIdExclusive)
                        {
                            layerIndex = i;
                            break;
                        }
                    }

                    if (layerIndex != -1)
                    {
                        break;
                    }
                }

                if (layerIndex != -1)
                {
                    break;
                }
            }

            bool hasAnimation = columnNames.Contains("EmbeddedAnimation");

            if (hasAnimation)
            {
                AddAnimationFrameAtIndex(animation, row, 0, layerIndex, tilesetIndex);
            }
            AppendCustomProperties(pDictionary, iDictionary, columnNames, row, false);

            AppendRowToStringBuilder(sb, row);

            if (animation != null && animation.Frames != null)
            {

                for (int i = 1; i < animation.Frames.Count; i++)
                {
                    row = new List<string>();
                    row.Add(""); // Name column


                    if (hasAnimation)
                    {
                        AddAnimationFrameAtIndex(animation, row, i, layerIndex, tilesetIndex);
                    }
                    AppendCustomProperties(pDictionary, iDictionary, columnNames, row, true);
                    AppendRowToStringBuilder(sb, row);
                }
            }
        }

        private void AddAnimationFrameAtIndex(TileAnimation animation, List<string> row, int animationIndex, int indexOfLayerReferencingTileset, int tilesetIndex)
        {
            if (animation != null && animation.Frames != null && animation.Frames.Count > animationIndex)
            {
                // public int TileId
                // public int Duration

                var frame = animation.Frames[animationIndex];

                int leftCoordinate = 0;
                int rightCoordinate = 16;
                int topCoordinate = 0;
                int bottomCoordinate = 16;

                var frameId = (uint)frame.TileId;
                // not sure why, but need to add 1:
                //frameId++;
                // Update - I know why, because the TileId
                // is relative to the Tileset.  I didn't try
                // this with multiple tilesets, and the first
                // tileset has a starting ID of 1. 
                frameId += this.Tilesets[tilesetIndex].Firstgid;

                GetPixelCoordinatesFromGid(frameId, this.Tilesets[tilesetIndex],
                    out leftCoordinate, out topCoordinate, out rightCoordinate, out bottomCoordinate);

                row.Add(string.Format(
                    "new FlatRedBall.Content.AnimationChain.AnimationFrameSaveBase(TextureName={0}, " +
                    "FrameLength={1}, LeftCoordinate={2}, RightCoordinate={3}, TopCoordinate={4}, BottomCoordinate={5})",
                    indexOfLayerReferencingTileset,
                    (frame.Duration / 1000.0f).ToString(CultureInfo.InvariantCulture),
                    leftCoordinate.ToString(CultureInfo.InvariantCulture),
                    rightCoordinate.ToString(CultureInfo.InvariantCulture),
                    topCoordinate.ToString(CultureInfo.InvariantCulture),
                    bottomCoordinate.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                row.Add(null);
            }
        }

        private static void AppendRowToStringBuilder(StringBuilder sb, List<string> row)
        {
            bool isFirst = true;
            foreach (var originalValue in row)
            {
                string value = originalValue;

                if (!isFirst)
                {
                    sb.Append(",");

                }
                if (value != null)
                {
                    value = value.Replace("\"", "\"\"");
                }

                sb.AppendFormat("\"{0}\"", value);
                isFirst = false;
            }
            sb.AppendLine();

        }

        private static string GetNameValue(IDictionary<string, string> iDictionary)
        {
            string nameValue = null;

            bool doesDictionaryContainNameValue =
                iDictionary.Any(p => property.GetStrippedName(p.Key).Equals("name", StringComparison.CurrentCultureIgnoreCase));

            if (doesDictionaryContainNameValue)
            {
                nameValue = iDictionary.First(p => property.GetStrippedName(p.Key).Equals("name", StringComparison.CurrentCultureIgnoreCase)).Value;
            }
            else
            {
                nameValue = "UnnamedTile" + numberOfUnnamedTiles;
                numberOfUnnamedTiles++;
            }
            return nameValue;
        }

        private static void AppendCustomProperties(IDictionary<string, string> pDictionary, IDictionary<string, string> iDictionary, IEnumerable<string> columnNames, List<string> row, bool forceEmpty)
        {
            foreach (string columnName in columnNames)
            {
                string strippedColumnName = property.GetStrippedName(columnName);

                bool isAnimation =
                    strippedColumnName.Equals("embeddedanimation", StringComparison.CurrentCultureIgnoreCase);

                bool isCustomProperty = !isAnimation &&
                    !strippedColumnName.Equals("name", StringComparison.CurrentCultureIgnoreCase);



                if (isCustomProperty)
                {
                    if (!forceEmpty && iDictionary.Any(p => property.GetStrippedName(p.Key).Equals(strippedColumnName, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        var value =
                            iDictionary.First(p => property.GetStrippedName(p.Key).Equals(strippedColumnName, StringComparison.CurrentCultureIgnoreCase)).Value;

                        row.Add(value);
                    }
                    // Victor Chelaru
                    // October 12, 2014
                    // Not sure what pDictionary
                    // is, but it looks like it's
                    // only used for "object" CSVs.
                    // My first question is - do we need
                    // to use stripped names here?  Also, do
                    // we even want to support object dictionaries
                    // in the future?  How does this fit in with the
                    // new "level" pattern.
                    else if (!forceEmpty && pDictionary != null && pDictionary.Any(p => p.Key.Equals(strippedColumnName, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        var value =
                            pDictionary.First(p => p.Key.Equals(strippedColumnName, StringComparison.CurrentCultureIgnoreCase)).Value;

                        row.Add(value);
                    }
                    else
                    {
                        row.Add(null);
                    }
                }
            }
        }

        private static void WriteColumnHeader(StringBuilder sb, IEnumerable<string> columnNames)
        {
            sb.Append("Name (required)");
            foreach (string columnName in columnNames)
            {
                string strippedName = property.GetStrippedName(columnName);


                bool isName = strippedName.Equals("name", StringComparison.CurrentCultureIgnoreCase);

                if (!isName)
                {
                    // Update August 27, 2012
                    // We can't just assume that
                    // all of the column names are
                    // going to be capitalized.  This
                    // was likely done to force the Name
                    // property to be capitalized, which we
                    // want, but we don't want to do it for everything.
                    //if (columnName.Length > 1)
                    //{
                    //    sb.AppendFormat(",{0}{1}", columnName.Substring(0, 1).ToUpper(), columnName.Substring(1));
                    //}
                    //else
                    //{
                    //    sb.AppendFormat(",{0}", columnName.ToUpper());
                    //}
                    sb.Append("," + columnName);
                }
            }
            sb.AppendLine();
        }

        /// <summary>
        /// Compares the stripped name of properties - removing the type
        /// </summary>
        public class CaseInsensitivePropertyEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {

                return property.GetStrippedName(x).Equals(property.GetStrippedName(y), StringComparison.CurrentCultureIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return property.GetStrippedName(obj).ToLowerInvariant().GetHashCode();
            }
        }

        private IEnumerable<string> GetColumnNames(CSVPropertyType type, string layerName)
        {
            var comparer = new CaseInsensitivePropertyEqualityComparer();

            var columnNames = new HashSet<string>();

            switch (type)
            {
                case CSVPropertyType.Tile:
                    return GetColumnNamesForTile(comparer);
                case CSVPropertyType.Layer:
                    return
                        this.Layers.Where(
                            l =>
                            layerName == null ||
                            (l.Name != null && l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase)))
                            .SelectMany(l => l.PropertyDictionary)
                            .Select(d => d.Key)
                            .Distinct(comparer);
                case CSVPropertyType.Map:
                    return this.PropertyDictionary.Select(d => d.Key).Distinct(comparer);
                case CSVPropertyType.Object:

                    List<string> toReturn = new List<string>();

                    toReturn.Add("X (int)");
                    toReturn.Add("Y (int)");

                    if (objectgroup != null)
                    {

                        var query1 =
                            objectgroup.Where(l =>
                                                     layerName == null ||
                                                     (l.Name != null &&
                                                      l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase)));
                        var query2 =
                            objectgroup.Where(l =>
                                                     layerName == null ||
                                                     (l.Name != null &&
                                                      l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase)));
                        return toReturn
                            .Union(query1
                                       .SelectMany(o => o.@object)
                                       .Where(o => o.gid != null) //November 2015 by Jesse Crafts-Finch: will ignore objects which are to be treated as sprites (they have a gid). 
                                       .SelectMany(o => o.PropertyDictionary)
                                       .Select(d => d.Key), comparer)
                            .Union(query2
                                       .SelectMany(o => o.PropertyDictionary)
                                       .Select(d => d.Key), comparer);
                    }
                    else
                    {
                        return toReturn;
                    }

            }
            return columnNames;
        }

        private IEnumerable<string> GetColumnNamesForTile(CaseInsensitivePropertyEqualityComparer comparer)
        {
            List<string> toReturn = new List<string>();

            // Name is required and always available
            toReturn.Add("Name (string, required)");

            // And animation is required too
            toReturn.Add(animationColumnName);

            toReturn.AddRange(this.Tilesets.SelectMany(t => t.Tiles)
                    .SelectMany(tile => tile.PropertyDictionary)
                    .Select(d => d.Key)
                    //.Distinct(comparer)
                    .ToList());

            foreach (var group in this.objectgroup)
            {
                bool addedGroup = false;
                foreach (var @object in group.@object)
                {
                    if (@object.gid != null)
                    {
                        addedGroup = true;
                        toReturn.AddRange(@object.PropertyDictionary.Keys);
                    }
                }
                if (addedGroup)
                {
                    toReturn.AddRange(group.PropertyDictionary.Keys);
                }
            }

            return toReturn.Distinct(comparer);
        }


        public NodeNetwork ToNodeNetwork(bool requireTile = true)
        {
            return ToNodeNetwork(true, true, true, requireTile);
        }

        public NodeNetworkSave ToNodeNetworkSave(bool linkHorizontally, bool linkVertically, bool linkDiagonally, bool requireTile)
        {
            NodeNetwork nodeNetwork = ToNodeNetwork(linkHorizontally, linkVertically, linkDiagonally, requireTile);
            return NodeNetworkSave.FromNodeNetwork(nodeNetwork);
        }

        public NodeNetworkSave ToNodeNetworkSave(bool requireTile = true)
        {
            return ToNodeNetworkSave(true, true, true, requireTile);
        }

        public NodeNetwork ToNodeNetwork(bool linkHorizontally, bool linkVertically, bool linkDiagonally, bool requireTile)
        {
            var toReturn = new NodeNetwork();


            int layercount = 0;
            foreach (MapLayer mapLayer in this.Layers)
            {
                if (!mapLayer.IsVisible)
                {
                    switch (mapLayer.VisibleBehavior)
                    {
                        case LayerVisibleBehavior.Ignore:
                            break;
                        case LayerVisibleBehavior.Skip:
                            continue;
                    }
                }
                var allNodes = new Dictionary<int, Dictionary<int, Dictionary<int, PositionedNode>>>();
                allNodes[layercount] = new Dictionary<int, Dictionary<int, PositionedNode>>();


                MapLayer mLayer = mapLayer;
                int mLayerCount = layercount;
                Parallel.For(0, mapLayer.data[0].tiles.Count, count =>
                {
                    uint gid = mLayer.data[0].tiles[count];

                    Tileset tileSet = GetTilesetForGid(gid);
                    if (tileSet != null || !requireTile)
                    {
                        var node = new PositionedNode();

                        //int tileWidth = requireTile ? tileSet.tilewidth : tilewidth;
                        //int tileHeight = requireTile ? tileSet.tileheight : tileheight;
                        int x = count % this.Width;
                        int y = count / this.Width;

                        float nodex;
                        float nodey;
                        float nodez;

                        CalculateWorldCoordinates(mLayerCount, count, tilewidth, tileheight, mLayer.width, out nodex, out nodey, out nodez);

                        node.X = nodex;
                        node.Y = nodey;
                        node.Z = nodez;

                        lock (allNodes)
                        {
                            if (!allNodes[mLayerCount].ContainsKey(x))
                            {
                                allNodes[mLayerCount][x] = new Dictionary<int, PositionedNode>();
                            }

                            allNodes[mLayerCount][x][y] = node;
                        }
                        node.Name = string.Format("Node {0}", count);
                        lock (toReturn)
                        {
                            toReturn.AddNode(node);
                        }
                    }
                });
                SetupNodeLinks(linkHorizontally, linkVertically, linkDiagonally, allNodes[layercount]);

                RemoveExcludedNodesViaPolygonLayer(toReturn, mapLayer, allNodes[layercount]);
                LowerNodesInNodesDownShapeCollection(mapLayer, allNodes[layercount]);
                RaiseNodesInNodesUpShapeCollection(mapLayer, allNodes[layercount]);

                ++layercount;
            }
            toReturn.UpdateShapes();

            return toReturn;
        }

        private void RaiseNodesInNodesUpShapeCollection(MapLayer mapLayer, Dictionary<int, Dictionary<int, PositionedNode>> allNodes)
        {
            ShapeCollection sc = this.ToShapeCollection(mapLayer.Name + " nodesup");
            List<PositionedNode> nodesToMoveUp = GetNodesThatCollideWithShapeCollection(sc, allNodes);

            foreach (var node in nodesToMoveUp)
            {
                node.Z += .001f;
            }
        }

        private void LowerNodesInNodesDownShapeCollection(MapLayer mapLayer, Dictionary<int, Dictionary<int, PositionedNode>> allNodes)
        {
            ShapeCollection sc = this.ToShapeCollection(mapLayer.Name + " nodesdown");
            List<PositionedNode> nodesToMoveDown = GetNodesThatCollideWithShapeCollection(sc, allNodes);

            foreach (var node in nodesToMoveDown)
            {
                node.Z -= .001f;
            }
        }

        private void RemoveExcludedNodesViaPolygonLayer(NodeNetwork nodeNetwork, MapLayer mapLayer, Dictionary<int, Dictionary<int, PositionedNode>> allNodes)
        {
            ShapeCollection sc = this.ToShapeCollection(mapLayer.Name + " nonodes");
            List<PositionedNode> nodesToRemove = GetNodesThatCollideWithShapeCollection(sc, allNodes);

            foreach (var node in nodesToRemove)
            {
                nodeNetwork.Remove(node);
            }
        }

        private List<PositionedNode> GetNodesThatCollideWithShapeCollection(ShapeCollection sc, Dictionary<int, Dictionary<int, PositionedNode>> allNodes)
        {
            var returnValue = new List<PositionedNode>();

            if (sc != null && sc.Polygons != null)
            {
                foreach (Polygon polygon in sc.Polygons)
                {
                    polygon.ForceUpdateDependencies();
                }

                foreach (var xpair in allNodes)
                {
                    foreach (var ypair in xpair.Value)
                    {
                        PositionedNode node = ypair.Value;
                        var rectangle = new AxisAlignedRectangle { Position = node.Position, ScaleX = 1, ScaleY = 1 };

                        if (sc.CollideAgainst(rectangle))
                        {
                            returnValue.Add(node);
                        }
                    }
                }
            }
            return returnValue;
        }

        private static void SetupNodeLinks(bool linkHorizontally, bool linkVertically, bool linkDiagonally, Dictionary<int, Dictionary<int, PositionedNode>> allNodes)
        {
            foreach (var xpair in allNodes)
            {
                int x = xpair.Key;
                foreach (var ypair in xpair.Value)
                {
                    int y = ypair.Key;

                    if (linkVertically && allNodes.ContainsKey(x - 1) && allNodes[x - 1].ContainsKey(y))
                    {
                        float cost = (ypair.Value.Position - allNodes[x - 1][y].Position).Length();
                        ypair.Value.LinkTo(allNodes[x - 1][y], cost);
                    }
                    if (linkHorizontally && xpair.Value.ContainsKey(y - 1))
                    {
                        float cost = (ypair.Value.Position - xpair.Value[y - 1].Position).Length();
                        ypair.Value.LinkTo(xpair.Value[y - 1], cost);
                    }
                    if (linkDiagonally && allNodes.ContainsKey(x - 1) && allNodes[x - 1].ContainsKey(y - 1))
                    {
                        float cost = (ypair.Value.Position - allNodes[x - 1][y - 1].Position).Length();
                        ypair.Value.LinkTo(allNodes[x - 1][y - 1], cost);
                    }
                    if (linkDiagonally && allNodes.ContainsKey(x + 1) && allNodes[x + 1].ContainsKey(y - 1))
                    {
                        float cost = (ypair.Value.Position - allNodes[x + 1][y - 1].Position).Length();
                        ypair.Value.LinkTo(allNodes[x + 1][y - 1], cost);
                    }
                }
            }
        }

        public SceneSave ToSceneSave(float scale, FileReferenceType referenceType = FileReferenceType.NoDirectory)
        {
            var toReturn = new SceneSave { CoordinateSystem = FlatRedBall.Math.CoordinateSystem.RightHanded };

            // TODO: Somehow add all layers separately
            for (int layercount = 0; layercount < this.MapLayers.Count; layercount++)
            {
                var abstractMapLayer = this.MapLayers[layercount];
                var mapLayer = abstractMapLayer as MapLayer;

                if (mapLayer != null)
                {
                    if (!mapLayer.IsVisible)
                    {
                        switch (mapLayer.VisibleBehavior)
                        {
                            case LayerVisibleBehavior.Ignore:
                                break;
                            case LayerVisibleBehavior.Skip:
                                continue;
                        }
                    }

                    MapLayer mLayer = mapLayer;
                    int mLayerCount = layercount;

                    for (int i = 0; i < mapLayer.data[0].tiles.Count; i++)
                    {
                        uint gid = mLayer.data[0].tiles[i];
                        if (gid > 0)
                        {
                            Tileset tileSet = GetTilesetForGid(gid);
                            if (tileSet != null)
                            {
                                SpriteSave sprite = CreateSpriteSaveFromMapTileset(scale, mLayerCount, mLayer, i, gid,
                                    tileSet, referenceType);
                                lock (toReturn)
                                {
                                    toReturn.SpriteList.Add(sprite);
                                }
                            }
                        }
                    }
                    continue;
                }

                var group = abstractMapLayer as mapObjectgroup;
                bool shouldProcess = group?.@object != null && group.Visible && !string.IsNullOrEmpty(group.Name);
                if (shouldProcess) //&& (string.IsNullOrEmpty(layerName) || group.name.Equals(layerName)))
                {
                    foreach (mapObjectgroupObject @object in @group.@object)
                    {
                        if (@object.gid != null)
                        {
                            SpriteSave sprite = CreateSpriteSaveFromObject(scale, @object, layercount, referenceType);
                            lock (toReturn)
                            {
                                toReturn.SpriteList.Add(sprite);
                            }
                        }
                    }
                }
            }

            return toReturn;
        }

        private SpriteSave CreateSpriteSaveFromObject(float scale, mapObjectgroupObject @object, int layerCount, FileReferenceType referenceType = FileReferenceType.NoDirectory)
        {

            if (@object.gid == null)
            {
                throw new NotSupportedException("CreateSpriteSaveFromObject called on a non image object. gid not set.");
            }

            var gid = @object.gid.Value;

            Tileset tileSet = GetTilesetForGid(gid);

            var sprite = new SpriteSave();
            //if (!mapLayer.IsVisible && mapLayer.VisibleBehavior == LayerVisibleBehavior.Match)
            //{
            //    sprite.Visible = false;
            //}

            int imageWidth = tileSet.Images[0].width;
            int imageHeight = tileSet.Images[0].height;
            int tileWidth = tileSet.Tilewidth;
            int spacing = tileSet.Spacing;
            int tileHeight = tileSet.Tileheight;
            int margin = tileSet.Margin;

            // TODO: only calculate these once per tileset. Perhaps it can be done in the deserialize method
            //int tilesWide = (imageWidth - margin) / (tileWidth + spacing);
            //int tilesHigh = (imageHeight - margin) / (tileHeight + spacing);

            if (referenceType == FileReferenceType.NoDirectory)
            {
                sprite.Texture = tileSet.Images[0].sourceFileName;
            }
            else if (referenceType == FileReferenceType.Absolute)
            {
                string directory = FileManager.GetDirectory(this.FileName);

                if (!string.IsNullOrEmpty(tileSet.SourceDirectory) && tileSet.SourceDirectory != ".")
                {
                    directory += tileSet.SourceDirectory;

                    directory = FileManager.RemoveDotDotSlash(directory);

                }

                sprite.Texture = FileManager.RemoveDotDotSlash(directory + tileSet.Images[0].Source);

            }
            else
            {
                throw new NotImplementedException();
            }

            uint tileTextureRelativeToStartOfTileset =
                (0x0fffffff & gid) - tileSet.Firstgid + 1;

            //if (tileSet.TileDictionary.ContainsKey(tileTextureRelativeToStartOfTileset))
            //{
            //    var dictionary = tileSet.TileDictionary[tileTextureRelativeToStartOfTileset].PropertyDictionary;

            //    foreach (var kvp in dictionary)
            //    {
            //        var key = kvp.Key;

            //        if (IsName(key))
            //        {
            //            sprite.Name = kvp.Value;
            //        }
            //    }
            //}

            // This is bad - we want to ue the same names as in Tiled so we don't accidentally
            // apply properties from the wrong tiles
            //if (string.IsNullOrEmpty(@object.Name))
            //{
            //    sprite.Name = "Unnamed" + gid;
            //}
            //else
            {
                sprite.Name = @object.Name;
            }
            SetSpriteTextureCoordinates(gid, sprite, tileSet, this.orientation);

            //CalculateWorldCoordinates(layercount, tileIndex, tileWidth, tileHeight, this.Width, out sprite.X, out sprite.Y, out sprite.Z);
            sprite.X = (float)@object.x;
            sprite.Y = -(float)@object.y;
            sprite.Z = layerCount;

            //sprite.ScaleX = tileWidth / 2.0f;
            //sprite.ScaleY = tileHeight / 2.0f;
            sprite.ScaleX = (float)@object.width / 2.0f;
            sprite.ScaleY = (float)@object.height / 2.0f;

            ///Is the tileset offset necessary for this?
            //if (tileSet.Tileoffset != null && tileSet.Tileoffset.Length == 1)
            //{
            //    sprite.X += tileSet.Tileoffset[0].x;
            //    sprite.Y -= tileSet.Tileoffset[0].y;
            //}

            sprite.X *= scale;
            sprite.Y *= scale;
            // Update August 28, 2012
            // The TMX converter splits
            // the Layers by their Z values.
            // We want each Layer to have its
            // own explicit Z value, so we don't
            // want to adjust the Z's when we scale:
            //sprite.Z *= scale;

            sprite.ScaleX *= scale;
            sprite.ScaleY *= scale;

            sprite.X += sprite.ScaleX;
            sprite.Y += sprite.ScaleY;
            return sprite;
        }
        private SpriteSave CreateSpriteSaveFromMapTileset(float scale, int layercount, MapLayer mapLayer, int tileIndex, uint gid, Tileset tileSet, FileReferenceType referenceType = FileReferenceType.NoDirectory)
        {
            var sprite = new SpriteSave();
            if (!mapLayer.IsVisible && mapLayer.VisibleBehavior == LayerVisibleBehavior.Match)
            {
                sprite.Visible = false;
            }

            int imageWidth = tileSet.Images[0].width;
            int imageHeight = tileSet.Images[0].height;
            int tileWidth = tileSet.Tilewidth;
            int spacing = tileSet.Spacing;
            int tileHeight = tileSet.Tileheight;
            int margin = tileSet.Margin;

            // TODO: only calculate these once per tileset. Perhaps it can be done in the deserialize method
            //int tilesWide = (imageWidth - margin) / (tileWidth + spacing);
            //int tilesHigh = (imageHeight - margin) / (tileHeight + spacing);

            if (referenceType == FileReferenceType.NoDirectory)
            {
                sprite.Texture = tileSet.Images[0].sourceFileName;
            }
            else if (referenceType == FileReferenceType.Absolute)
            {
                string directory = FileManager.GetDirectory(this.FileName);

                if (!string.IsNullOrEmpty(tileSet.SourceDirectory) && tileSet.SourceDirectory != ".")
                {
                    directory += tileSet.SourceDirectory;

                    directory = FileManager.RemoveDotDotSlash(directory);

                }

                sprite.Texture = FileManager.RemoveDotDotSlash(directory + tileSet.Images[0].Source);

            }
            else
            {
                throw new NotImplementedException();
            }

            uint tileTextureRelativeToStartOfTileset =
                (0x0fffffff & gid) - tileSet.Firstgid + 1;

            if (tileSet.TileDictionary.ContainsKey(tileTextureRelativeToStartOfTileset))
            {
                var dictionary = tileSet.TileDictionary[tileTextureRelativeToStartOfTileset].PropertyDictionary;

                foreach (var kvp in dictionary)
                {
                    var key = kvp.Key;

                    if (IsName(key))
                    {
                        sprite.Name = kvp.Value;
                    }
                }
            }

            // This can cause tiles to use properties from other tiles, so this is bad. If no name exists in Tiled, 
            // no name should be given here:
            //if (string.IsNullOrEmpty(sprite.Name))
            //{
            //    sprite.Name = "Unnamed" + gid;
            //}

            SetSpriteTextureCoordinates(gid, sprite, tileSet, this.orientation);
            CalculateWorldCoordinates(layercount, tileIndex, tileWidth, tileHeight, this.Width, out sprite.X, out sprite.Y, out sprite.Z);

            sprite.ScaleX = tileWidth / 2.0f;
            sprite.ScaleY = tileHeight / 2.0f;

            if (tileSet.Tileoffset != null && tileSet.Tileoffset.Length == 1)
            {
                sprite.X += tileSet.Tileoffset[0].x;
                sprite.Y -= tileSet.Tileoffset[0].y;
            }


            sprite.X *= scale;
            sprite.Y *= scale;
            // Update August 28, 2012
            // The TMX converter splits
            // the Layers by their Z values.
            // We want each Layer to have its
            // own explicit Z value, so we don't
            // want to adjust the Z's when we scale:
            //sprite.Z *= scale;

            sprite.ScaleX *= scale;
            sprite.ScaleY *= scale;
            return sprite;
        }

        private static bool IsName(string key)
        {
            return property.GetStrippedName(key).ToLower() == "name";
        }

        public void CalculateWorldCoordinates(int layerIndex, int tileIndex, int tileWidth, int tileHeight, int layerWidth, out float x, out float y, out float z)
        {
            int normalizedX = tileIndex % this.Width;
            int normalizedY = tileIndex / this.Width;
            CalculateWorldCoordinates(layerIndex, normalizedX, normalizedY, tileWidth, tileHeight, layerWidth, out x, out y, out z);
        }

        public void CalculateWorldCoordinates(int layerIndex, float normalizedX, float normalizedY, int tileWidth, int tileHeight, int layerWidth, out float x, out float y, out float z)
        {
            if (this.orientation == null || this.orientation.Equals("orthogonal"))
            {
                x = (normalizedX * this.tilewidth) + (this.tilewidth / 2.0f);
                x += (tileWidth - this.tilewidth) / 2.0f;
                y = -(normalizedY * this.tileheight) - (this.tileheight / 2.0f);
                y += (tileHeight - this.tileheight) / 2.0f;
                z = layerIndex;
            }
            else if (this.orientation != null && this.orientation.Equals("isometric"))
            {
                y = -((normalizedX * this.tilewidth / 2.0f) + (normalizedY * this.tilewidth / 2.0f)) / 2;
                y += tileHeight / 2.0f;
                x = -((normalizedY * this.tilewidth / 2.0f) - (normalizedX * this.tileheight / 2.0f) * 2);
                x += tileWidth / 2.0f;
                z = ((normalizedY * layerWidth + normalizedX) * .000001f) + layerIndex;
            }
            else
            {
                throw new NotImplementedException("Unknown orientation type");
            }

            x += Offset.Item1;
            y += Offset.Item2;
            z += Offset.Item3;
        }

        public static void SetSpriteTextureCoordinates(uint gid, SpriteSave sprite, Tileset tileSet, string orientation)
        {
            int imageWidth = tileSet.Images[0].width;
            int imageHeight = tileSet.Images[0].height;
            int tileWidth = tileSet.Tilewidth;
            int spacing = tileSet.Spacing;
            int tileHeight = tileSet.Tileheight;
            int margin = tileSet.Margin;


            int leftPixelCoord;
            int topPixelCoord;
            int rightPixelCoord;
            int bottomPixelCoord;
            GetPixelCoordinatesFromGid(gid, tileSet,
                out leftPixelCoord, out topPixelCoord, out rightPixelCoord, out bottomPixelCoord);


            bool flipDiagonally;
            var gidWithoutRotation = gid & 0x0fffffff;
            const uint FlippedDiagonallyFlag = 0x20000000;
            flipDiagonally = (gid & FlippedDiagonallyFlag) == FlippedDiagonallyFlag;


            //if (flipDiagonally)
            //{
            //    // this turns:
            //    // 1---2
            //    // |   |
            //    // 3---4

            //    // into:
            //    // 1---3
            //    // |   |
            //    // 2---4

            //    int newLeft = topPixelCoord;
            //    int newRight = bottomPixelCoord;
            //    int newTop = leftPixelCoord;
            //    int newBottom = rightPixelCoord;

            //    topPixelCoord = newTop;
            //    bottomPixelCoord = newBottom;
            //    leftPixelCoord = newLeft;
            //    rightPixelCoord = newRight;
            //}

            // Calculate relative texture coordinates based on pixel coordinates
            var changeVal = LessOrGreaterDesired.Greater;

            if (orientation == "isometric")
            {
                changeVal = LessOrGreaterDesired.NoChange;
            }

            sprite.TopTextureCoordinate = GetTextureCoordinate(topPixelCoord, imageHeight, changeVal);
            sprite.LeftTextureCoordinate = GetTextureCoordinate(leftPixelCoord, imageWidth, changeVal);

            changeVal = LessOrGreaterDesired.Less;
            if (orientation == "isometric")
            {
                changeVal = LessOrGreaterDesired.NoChange;
            }

            sprite.RightTextureCoordinate = GetTextureCoordinate(rightPixelCoord, imageWidth, changeVal);
            sprite.BottomTextureCoordinate = GetTextureCoordinate(bottomPixelCoord, imageHeight, changeVal);

            if (flipDiagonally)
            {
                sprite.RotationZ = Microsoft.Xna.Framework.MathHelper.PiOver2;
                sprite.FlipHorizontal = true;
            }
        }

        public static void GetPixelCoordinatesFromGid(uint gid, Tileset tileSet,
            out int leftPixelCoord, out int topPixelCoord, out int rightPixelCoord, out int bottomPixelCoord)
        {
            int imageWidth = tileSet.Images[0].width;
            int imageHeight = tileSet.Images[0].height;
            int tileWidth = tileSet.Tilewidth;
            int spacing = tileSet.Spacing;
            int tileHeight = tileSet.Tileheight;
            int margin = tileSet.Margin;


            var gidWithoutRotation = gid & 0x0fffffff;

            const uint FlippedHorizontallyFlag = 0x80000000;
            const uint FlippedVerticallyFlag = 0x40000000;
            const uint FlippedDiagonallyFlag = 0x20000000;

            bool flipHorizontally = (gid & FlippedHorizontallyFlag) == FlippedHorizontallyFlag;
            bool flipVertically = (gid & FlippedVerticallyFlag) == FlippedVerticallyFlag;
            bool flipDiagonally = (gid & FlippedDiagonallyFlag) == FlippedDiagonallyFlag;

            // Calculate pixel coordinates in the texture sheet
            leftPixelCoord = CalculateXCoordinate(gidWithoutRotation - tileSet.Firstgid, imageWidth, tileWidth, spacing, margin);
            topPixelCoord = CalculateYCoordinate(gidWithoutRotation - tileSet.Firstgid, imageWidth, tileWidth, tileHeight, spacing, margin);
            rightPixelCoord = leftPixelCoord + tileWidth;
            bottomPixelCoord = topPixelCoord + tileHeight;

            if ((flipHorizontally && flipDiagonally == false) ||
                (flipVertically && flipDiagonally))
            {
                var temp = rightPixelCoord;
                rightPixelCoord = leftPixelCoord;
                leftPixelCoord = temp;
            }

            if ((flipVertically && flipDiagonally == false) ||
                (flipHorizontally && flipDiagonally))
            {
                var temp = topPixelCoord;
                topPixelCoord = bottomPixelCoord;
                bottomPixelCoord = temp;

            }
        }

        public Tileset GetTilesetForGid(uint gid, bool shouldRemoveFlipFlags = true)
        {
            var effectiveGid = gid;

            if (shouldRemoveFlipFlags)
            {
                effectiveGid = 0x0fffffff & gid;
            }

            // Assuming tilesets are sorted by the firstgid value...
            // Resort with LINQ if not
            if (Tilesets != null)
            {
                for (int i = Tilesets.Count - 1; i >= 0; --i)
                {
                    Tileset tileSet = Tilesets[i];
                    if (effectiveGid >= tileSet.Firstgid)
                    {
                        return tileSet;
                    }
                }
            }
            return null;
        }

        private static float GetTextureCoordinate(int pixelCoord, int dimension, LessOrGreaterDesired lessOrGreaterDesired)
        {
            float asFloat = pixelCoord / (float)dimension;

            //const float modValue = .000001f;
            const float modValue = .000002f;
            //const float modValue = .00001f;
            switch (lessOrGreaterDesired)
            {
                case LessOrGreaterDesired.Greater:
                    return asFloat + modValue;
                case LessOrGreaterDesired.Less:
                    return asFloat - modValue;
                default:
                    return asFloat;
            }
        }

        public static int CalculateYCoordinate(uint gid, int imageWidth, int tileWidth, int tileHeight, int spacing, int margin)
        {

            int tilesWide = TilesetExtensionMethods.GetNumberOfTilesWide(
                imageWidth, margin, tileWidth, spacing);

            int normalizedy = (int)(gid / tilesWide);
            int pixely = normalizedy * (tileHeight + spacing) + margin;

            return pixely;
        }

        public static int CalculateXCoordinate(uint gid, int imageWidth, int tileWidth, int spacing, int margin)
        {
            var tilesWide = TilesetExtensionMethods.GetNumberOfTilesWide(
                imageWidth, margin, tileWidth, spacing);


            int normalizedX = (int)(gid % tilesWide);
            int pixelX = normalizedX * (tileWidth + spacing) + margin;

            return pixelX;
        }

        public static TiledMapSave FromFile(string fileName)
        {
            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.RelativeDirectory + fileName;
            }

            // I believe the relative directory of the TMS must be its own directory so that
            // image references can be tracked on XML deserialization
            string oldRelativeDirectory = FileManager.RelativeDirectory;
            try
            {
                FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);
            }
            catch
            {
            }
            var tms = FileManager.XmlDeserialize<TiledMapSave>(fileName);
            FileManager.RelativeDirectory = oldRelativeDirectory;


            tms.FileName = fileName;


            foreach (MapLayer layer in tms.Layers)
            {
                if (!layer.PropertyDictionary.ContainsKey("VisibleBehavior"))
                {
                    layer.VisibleBehavior = LayerVisibleBehaviorValue;
                }
                else
                {
                    if (!Enum.TryParse(layer.PropertyDictionary["VisibleBehavior"], out layer.VisibleBehavior))
                    {
                        layer.VisibleBehavior = LayerVisibleBehaviorValue;
                    }
                }
            }
            return tms;
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);

        }
    }
}
