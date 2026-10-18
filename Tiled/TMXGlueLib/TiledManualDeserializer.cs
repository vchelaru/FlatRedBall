using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FlatRedBall.IO;

namespace TMXGlueLib
{
    public partial class TiledMapSave
    {
        /// <summary>
        /// When true, .tmx and .tsx files are read with a hand-written XElement loader instead of XmlSerializer.
        /// XmlSerializer needs runtime code generation and reflection over members the trimmer removes, so it
        /// cannot be used under Native AOT. This defaults to true when the runtime reports that dynamic code is
        /// not supported (Native AOT) and false everywhere else. Assigning the field overrides the default.
        /// </summary>
#if NET
        public static bool ManualDeserialization = !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;
#else
        public static bool ManualDeserialization = false;
#endif
    }

    /// <summary>
    /// Reflection-free replacement for XmlSerializer when reading .tmx and .tsx files. It reads exactly the
    /// members the XmlSerializer path reads, in the same order and with the same quirks, so the two produce the
    /// same object graph. Anything the Save classes have no member for (renderorder, chunks, text objects, ...)
    /// is ignored here exactly as XmlSerializer ignores it. A parity test loads every .tmx and .tsx in the repo
    /// through both paths and compares the results.
    /// </summary>
    public static class TiledManualDeserializer
    {
        #region Entry points

        public static TiledMapSave LoadMap(string fileName)
        {
            XElement root = LoadRoot(fileName, "map");

            try
            {
                return ReadMap(root);
            }
            catch (Exception e)
            {
                throw WrapLikeXmlSerializer(e);
            }
        }

        public static tileset LoadTileset(string fileName)
        {
            XElement root = LoadRoot(fileName, "tileset");

            try
            {
                return ReadExternalTileset(root);
            }
            catch (Exception e)
            {
                throw WrapLikeXmlSerializer(e);
            }
        }

        #endregion

        #region File and exception handling

        static XElement LoadRoot(string fileName, string expectedRootName)
        {
            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.RelativeDirectory + fileName;
            }

            FileManager.ThrowExceptionIfFileDoesntExist(fileName);

            XDocument document;
            try
            {
                using (Stream stream = FileManager.GetStreamForFile(fileName))
                {
                    // Whitespace-only text is dropped, as XmlSerializer's reader does, so a <data> element holding
                    // only indentation between <tile> children has no text.
                    document = XDocument.Load(stream);
                }
            }
            catch (XmlException e)
            {
                throw WrapLikeXmlSerializer(e);
            }

            XElement root = document.Root;
            // The Save classes declare an empty namespace, so a root in any other namespace is rejected too.
            if (root == null || root.Name.LocalName != expectedRootName || root.Name.NamespaceName.Length != 0)
            {
                throw new InvalidOperationException(
                    "There is an error in XML document. <" + (root == null ? "" : root.Name.LocalName) + "> was not expected, expected <" + expectedRootName + ">.");
            }

            return root;
        }

        // XmlSerializer reports every failure while reading (malformed XML, a property setter that throws such as
        // Tileset.Source not finding its tsx, a bad number) as an InvalidOperationException wrapping the real
        // exception. Callers key off that shape (Tileset retries a half-written file when the inner exception is an
        // XmlException), so the manual path reports failures the same way.
        static InvalidOperationException WrapLikeXmlSerializer(Exception e)
        {
            return new InvalidOperationException("There is an error in XML document. " + e.Message, e);
        }

        #endregion

        #region Value conversion (XmlSerializer uses XmlConvert for all of these)

        static int ToInt(string value) { return XmlConvert.ToInt32(value); }

        static uint ToUInt(string value) { return XmlConvert.ToUInt32(value); }

        static float ToFloat(string value) { return XmlConvert.ToSingle(value); }

        static double ToDouble(string value) { return XmlConvert.ToDouble(value); }

        static IEnumerable<XAttribute> AttributesOf(XElement element)
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                // Skips xmlns declarations and prefixed attributes such as xml:space.
                if (attribute.Name.NamespaceName.Length == 0)
                {
                    yield return attribute;
                }
            }
        }

        // The text of an element as XmlSerializer hands it to an [XmlText] member: only the element's own text
        // and CDATA nodes, null when it has none.
        static string DirectText(XElement element)
        {
            StringBuilder builder = null;
            foreach (XNode node in element.Nodes())
            {
                // XCData derives from XText.
                XText text = node as XText;
                if (text != null)
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder();
                    }
                    builder.Append(text.Value);
                }
            }
            return builder == null ? null : builder.ToString();
        }

        #endregion

        #region Map

        static TiledMapSave ReadMap(XElement element)
        {
            var map = new TiledMapSave();

            // XmlSerializer creates the list behind an XmlElement collection member before it reads anything,
            // so Group is empty rather than null when the map has no groups.
            map.Group = new List<LayerGroup>();

            foreach (XAttribute attribute in AttributesOf(element))
            {
                string value = attribute.Value;
                switch (attribute.Name.LocalName)
                {
                    case "version": map.version = value; break;
                    case "orientation": map.orientation = value; break;
                    case "width": map.Width = ToInt(value); break;
                    case "height": map.Height = ToInt(value); break;
                    case "tilewidth": map.tilewidth = ToInt(value); break;
                    case "tileheight": map.tileheight = ToInt(value); break;
                    case "infinite": map.Infinite = ToInt(value); break;
                }
            }

            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "properties":
                        ReadProperties(child, map.properties);
                        break;
                    case "tileset":
                        map.Tilesets.Add(ReadMapTileset(child));
                        break;
                    case "layer":
                    case "imagelayer":
                    case "objectgroup":
                        map.MapLayers.Add(ReadLayer(child));
                        break;
                    case "group":
                        map.Group.Add(ReadGroup(child));
                        break;
                }
            }

            return map;
        }

        static LayerGroup ReadGroup(XElement element)
        {
            var group = new LayerGroup();

            // Created up front for the same reason as TiledMapSave.Group.
            group.MapLayers = new List<AbstractMapLayer>();
            group.Group = new List<LayerGroup>();

            foreach (XAttribute attribute in AttributesOf(element))
            {
                switch (attribute.Name.LocalName)
                {
                    case "id": group.Id = ToInt(attribute.Value); break;
                    case "name": group.Name = attribute.Value; break;
                }
            }

            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "layer":
                    case "imagelayer":
                    case "objectgroup":
                        group.MapLayers.Add(ReadLayer(child));
                        break;
                    case "group":
                        group.Group.Add(ReadGroup(child));
                        break;
                }
            }

            return group;
        }

        static void ReadProperties(XElement wrapper, List<property> into)
        {
            if (into == null)
            {
                return;
            }

            foreach (XElement child in wrapper.Elements())
            {
                if (child.Name.LocalName == "property")
                {
                    var result = new property();
                    foreach (XAttribute attribute in AttributesOf(child))
                    {
                        switch (attribute.Name.LocalName)
                        {
                            case "name": result.name = attribute.Value; break;
                            case "value": result.value = attribute.Value; break;
                            case "type": result.Type = attribute.Value; break;
                        }
                    }
                    into.Add(result);
                }
            }
        }

        #endregion

        #region Layers

        static AbstractMapLayer ReadLayer(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "layer": return ReadTileLayer(element);
                case "imagelayer": return ReadImageLayer(element);
                default: return ReadObjectGroup(element);
            }
        }

        // Attributes every layer type shares (AbstractMapLayer). Returns whether the attribute was one of them.
        static bool ReadLayerAttribute(AbstractMapLayer layer, XAttribute attribute)
        {
            string value = attribute.Value;
            switch (attribute.Name.LocalName)
            {
                case "name": layer.Name = value; return true;
                case "id": layer.Id = ToInt(value); return true;
                case "tintcolor": layer.TintColor = value; return true;
                case "opacity": layer.Opacity = ToFloat(value); return true;
                case "visible": layer.visible = ToInt(value); return true;
                case "parallaxx": layer.ParallaxX = ToFloat(value); return true;
                case "parallaxy": layer.ParallaxY = ToFloat(value); return true;
                case "offsetx": layer.OffsetX = ToFloat(value); return true;
                case "offsety": layer.OffsetY = ToFloat(value); return true;
            }
            return false;
        }

        static MapLayer ReadTileLayer(XElement element)
        {
            var layer = new MapLayer();

            foreach (XAttribute attribute in AttributesOf(element))
            {
                if (ReadLayerAttribute(layer, attribute))
                {
                    continue;
                }

                switch (attribute.Name.LocalName)
                {
                    case "width": layer.width = ToInt(attribute.Value); break;
                    case "height": layer.height = ToInt(attribute.Value); break;
                }
            }

            List<mapLayerData> data = null;
            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "properties":
                        ReadProperties(child, layer.properties);
                        break;
                    case "data":
                        if (data == null)
                        {
                            data = new List<mapLayerData>();
                        }
                        data.Add(ReadLayerData(child));
                        break;
                }
            }

            // Assigned after the attributes so the setter sees the final width and height.
            layer.data = data == null ? null : data.ToArray();

            return layer;
        }

        static mapLayerData ReadLayerData(XElement element)
        {
            var data = new mapLayerData();

            foreach (XAttribute attribute in AttributesOf(element))
            {
                switch (attribute.Name.LocalName)
                {
                    case "encoding": data.encoding = attribute.Value; break;
                    case "compression": data.compression = attribute.Value; break;
                }
            }

            List<mapLayerDataTile> tiles = null;
            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "tile":
                        if (tiles == null)
                        {
                            tiles = new List<mapLayerDataTile>();
                        }
                        var tile = new mapLayerDataTile();
                        foreach (XAttribute attribute in AttributesOf(child))
                        {
                            if (attribute.Name.LocalName == "gid")
                            {
                                tile.gid = attribute.Value;
                            }
                        }
                        tiles.Add(tile);
                        break;
                    case "length":
                        // length is a public read/write property, so XmlSerializer writes and reads it.
                        data.length = ToInt(child.Value);
                        break;
                }
            }
            data.dataTiles = tiles == null ? null : tiles.ToArray();

            string text = DirectText(element);
            if (text != null)
            {
                data.Value = text;
            }

            return data;
        }

        static MapImageLayer ReadImageLayer(XElement element)
        {
            var layer = new MapImageLayer();

            foreach (XAttribute attribute in AttributesOf(element))
            {
                ReadLayerAttribute(layer, attribute);
            }

            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "properties":
                        ReadProperties(child, layer.properties);
                        break;
                    case "image":
                        var image = new MapImageLayerImage();
                        foreach (XAttribute attribute in AttributesOf(child))
                        {
                            switch (attribute.Name.LocalName)
                            {
                                case "source": image.Source = attribute.Value; break;
                                case "width": image.Width = ToFloat(attribute.Value); break;
                                case "height": image.Height = ToFloat(attribute.Value); break;
                            }
                        }
                        layer.ImageObject = image;
                        break;
                }
            }

            return layer;
        }

        static mapObjectgroup ReadObjectGroup(XElement element)
        {
            var group = new mapObjectgroup();

            foreach (XAttribute attribute in AttributesOf(element))
            {
                if (ReadLayerAttribute(group, attribute))
                {
                    continue;
                }

                if (attribute.Name.LocalName == "draworder")
                {
                    group.DrawOrder = attribute.Value;
                }
            }

            List<mapObjectgroupObject> objects = null;
            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "properties":
                        ReadProperties(child, group.properties);
                        break;
                    case "object":
                        if (objects == null)
                        {
                            objects = new List<mapObjectgroupObject>();
                        }
                        objects.Add(ReadObject(child));
                        break;
                }
            }
            group.@object = objects == null ? null : objects.ToArray();

            return group;
        }

        static mapObjectgroupObject ReadObject(XElement element)
        {
            var result = new mapObjectgroupObject();

            foreach (XAttribute attribute in AttributesOf(element))
            {
                string value = attribute.Value;
                switch (attribute.Name.LocalName)
                {
                    case "visible": result.Visible = ToInt(value); break;
                    case "name": result.Name = value; break;
                    case "gid": result.__proxygid = value; break;
                    case "type": result.Type = value; break;
                    // class and type are the same member, so whichever comes last in the file wins.
                    case "class": result.Class = value; break;
                    case "x": result.x = ToDouble(value); break;
                    case "y": result.y = ToDouble(value); break;
                    case "width": result.width = ToFloat(value); break;
                    case "height": result.height = ToFloat(value); break;
                    case "rotation": result.Rotation = ToDouble(value); break;
                }
            }

            List<mapObjectgroupObjectPolygon> polygons = null;
            List<mapObjectgroupObjectPolyline> polylines = null;
            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "properties":
                        ReadProperties(child, result.properties);
                        break;
                    case "ellipse":
                        var ellipse = new mapObjectgroupObjectEllipse();
                        foreach (XAttribute attribute in AttributesOf(child))
                        {
                            if (attribute.Name.LocalName == "visible")
                            {
                                ellipse.Visible = ToInt(attribute.Value);
                            }
                        }
                        result.ellipse = ellipse;
                        break;
                    case "polygon":
                        if (polygons == null)
                        {
                            polygons = new List<mapObjectgroupObjectPolygon>();
                        }
                        var polygon = new mapObjectgroupObjectPolygon();
                        foreach (XAttribute attribute in AttributesOf(child))
                        {
                            switch (attribute.Name.LocalName)
                            {
                                case "visible": polygon.Visible = ToInt(attribute.Value); break;
                                case "points": polygon.points = attribute.Value; break;
                            }
                        }
                        polygons.Add(polygon);
                        break;
                    case "polyline":
                        if (polylines == null)
                        {
                            polylines = new List<mapObjectgroupObjectPolyline>();
                        }
                        var polyline = new mapObjectgroupObjectPolyline();
                        foreach (XAttribute attribute in AttributesOf(child))
                        {
                            switch (attribute.Name.LocalName)
                            {
                                case "visible": polyline.Visible = ToInt(attribute.Value); break;
                                case "points": polyline.points = attribute.Value; break;
                            }
                        }
                        polylines.Add(polyline);
                        break;
                }
            }
            result.polygon = polygons == null ? null : polygons.ToArray();
            result.polyline = polylines == null ? null : polylines.ToArray();

            return result;
        }

        #endregion

        #region Tilesets

        static Tileset ReadMapTileset(XElement element)
        {
            var result = new Tileset();

            // XmlSerializer grabs the list behind a flat XmlElement collection before reading the attributes, so
            // when Source replaces Tiles with the tsx's list, tile children of the same element are added to the
            // abandoned original. Wrapped collections (wangsets) are looked up when their wrapper is reached.
            List<mapTilesetTile> tiles = result.Tiles;

            // Setting Source loads the referenced tsx immediately, overwriting whatever the attributes before
            // it set and being overwritten by the ones after it, so attributes are applied in document order.
            foreach (XAttribute attribute in AttributesOf(element))
            {
                string value = attribute.Value;
                switch (attribute.Name.LocalName)
                {
                    case "version": result.Version = value; break;
                    case "tiledversion": result.TiledVersion = value; break;
                    case "tilecount": result.TileCount = ToInt(value); break;
                    case "columns": result.Columns = ToInt(value); break;
                    case "source": result.Source = value; break;
                    case "firstgid": result.Firstgid = ToUInt(value); break;
                    case "name": result.Name = value; break;
                    case "tilewidth": result.Tilewidth = ToInt(value); break;
                    case "tileheight": result.Tileheight = ToInt(value); break;
                    case "spacing": result.Spacing = ToInt(value); break;
                    case "margin": result.Margin = ToInt(value); break;
                }
            }

            List<TilesetImage> images = null;
            List<mapTilesetTileOffset> offsets = null;
            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "image":
                        if (images == null)
                        {
                            images = new List<TilesetImage>();
                        }
                        images.Add(ReadTilesetImage(child));
                        break;
                    case "tileoffset":
                        if (offsets == null)
                        {
                            offsets = new List<mapTilesetTileOffset>();
                        }
                        var offset = new mapTilesetTileOffset();
                        foreach (XAttribute attribute in AttributesOf(child))
                        {
                            switch (attribute.Name.LocalName)
                            {
                                case "x": offset.x = ToInt(attribute.Value); break;
                                case "y": offset.y = ToInt(attribute.Value); break;
                            }
                        }
                        offsets.Add(offset);
                        break;
                    case "terraintypes":
                        if (result.terraintypes != null)
                        {
                            foreach (XElement terrainElement in child.Elements())
                            {
                                if (terrainElement.Name.LocalName == "mapTilesetTerrain")
                                {
                                    // mapTilesetTerrain's members carry no XmlAttribute, so XmlSerializer reads them
                                    // from child elements rather than attributes.
                                    var terrain = new mapTilesetTerrain();
                                    foreach (XElement terrainMember in terrainElement.Elements())
                                    {
                                        switch (terrainMember.Name.LocalName)
                                        {
                                            case "name": terrain.name = terrainMember.Value; break;
                                            case "tile": terrain.tile = ToInt(terrainMember.Value); break;
                                        }
                                    }
                                    result.terraintypes.Add(terrain);
                                }
                            }
                        }
                        break;
                    case "tile":
                        tiles.Add(ReadTile(child));
                        break;
                    case "wangsets":
                        ReadWangSets(child, result.wangsets);
                        break;
                }
            }

            // Both setters ignore the value when the tsx already supplied one.
            result.Images = images == null ? null : images.ToArray();
            result.Tileoffset = offsets == null ? null : offsets.ToArray();

            return result;
        }

        static TilesetImage ReadTilesetImage(XElement element)
        {
            var image = new TilesetImage();
            foreach (XAttribute attribute in AttributesOf(element))
            {
                switch (attribute.Name.LocalName)
                {
                    case "source": image.Source = attribute.Value; break;
                    case "width": image.width = ToInt(attribute.Value); break;
                    case "height": image.height = ToInt(attribute.Value); break;
                }
            }
            return image;
        }

        static mapTilesetTile ReadTile(XElement element)
        {
            var tile = new mapTilesetTile();

            foreach (XAttribute attribute in AttributesOf(element))
            {
                string value = attribute.Value;
                switch (attribute.Name.LocalName)
                {
                    case "id": tile.id = ToInt(value); break;
                    case "type": tile.Type = value; break;
                    // class and type are the same member, so whichever comes last in the file wins.
                    case "class": tile.Class = value; break;
                    case "probability": tile.Probability = ToDouble(value); break;
                }
            }

            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "properties":
                        ReadProperties(child, tile.properties);
                        break;
                    case "animation":
                        tile.Animation = ReadAnimation(child);
                        break;
                    case "objectgroup":
                        tile.Objects = ReadObjectGroup(child);
                        break;
                }
            }

            return tile;
        }

        static TileAnimation ReadAnimation(XElement element)
        {
            var animation = new TileAnimation();

            foreach (XElement child in element.Elements())
            {
                if (child.Name.LocalName == "frame")
                {
                    var frame = new TileAnimationFrame();
                    foreach (XAttribute attribute in AttributesOf(child))
                    {
                        switch (attribute.Name.LocalName)
                        {
                            case "tileid": frame.TileId = ToInt(attribute.Value); break;
                            case "duration": frame.Duration = ToInt(attribute.Value); break;
                        }
                    }
                    animation.Frames.Add(frame);
                }
            }

            return animation;
        }

        // <wangsets> is the wrapper element around the wangset children, for both a tsx root and a tmx tileset.
        static void ReadWangSets(XElement wrapper, List<wangset> into)
        {
            if (into == null)
            {
                return;
            }

            foreach (XElement child in wrapper.Elements())
            {
                if (child.Name.LocalName != "wangset")
                {
                    continue;
                }

                var set = new wangset();
                foreach (XAttribute attribute in AttributesOf(child))
                {
                    switch (attribute.Name.LocalName)
                    {
                        case "name": set.Name = attribute.Value; break;
                        case "type": set.Type = attribute.Value; break;
                        case "tile": set.Tile = ToInt(attribute.Value); break;
                    }
                }

                foreach (XElement member in child.Elements())
                {
                    switch (member.Name.LocalName)
                    {
                        case "wangcolor":
                            var color = new wangcolor();
                            foreach (XAttribute attribute in AttributesOf(member))
                            {
                                switch (attribute.Name.LocalName)
                                {
                                    case "name": color.name = attribute.Value; break;
                                    case "color": color.color = attribute.Value; break;
                                    case "tile": color.tile = ToInt(attribute.Value); break;
                                    case "probability": color.probability = ToDouble(attribute.Value); break;
                                }
                            }
                            set.WangColors.Add(color);
                            break;
                        case "wangtile":
                            var wangTile = new wangtile();
                            foreach (XAttribute attribute in AttributesOf(member))
                            {
                                switch (attribute.Name.LocalName)
                                {
                                    case "tileid": wangTile.tileid = ToInt(attribute.Value); break;
                                    case "wangid": wangTile.wangid = attribute.Value; break;
                                }
                            }
                            set.WangTiles.Add(wangTile);
                            break;
                    }
                }

                into.Add(set);
            }
        }

        // The root of a .tsx file.
        static tileset ReadExternalTileset(XElement element)
        {
            var result = new tileset();

            foreach (XAttribute attribute in AttributesOf(element))
            {
                string value = attribute.Value;
                switch (attribute.Name.LocalName)
                {
                    case "name": result.name = value; break;
                    case "tilewidth": result.tilewidth = ToInt(value); break;
                    case "tileheight": result.tileheight = ToInt(value); break;
                    case "spacing": result.spacing = ToInt(value); break;
                    case "margin": result.margin = ToInt(value); break;
                    case "version": result.Version = value; break;
                    case "tiledversion": result.TiledVersion = value; break;
                    case "tilecount": result.TileCount = ToInt(value); break;
                    case "columns": result.Columns = ToInt(value); break;
                }
            }

            List<tilesetImage> images = null;
            foreach (XElement child in element.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "tile":
                        if (result.tile == null)
                        {
                            result.tile = new List<mapTilesetTile>();
                        }
                        result.tile.Add(ReadTile(child));
                        break;
                    case "image":
                        if (images == null)
                        {
                            images = new List<tilesetImage>();
                        }
                        var image = new tilesetImage();
                        foreach (XAttribute attribute in AttributesOf(child))
                        {
                            switch (attribute.Name.LocalName)
                            {
                                case "source": image.source = attribute.Value; break;
                                case "width": image.width = ToInt(attribute.Value); break;
                                case "height": image.height = ToInt(attribute.Value); break;
                            }
                        }
                        images.Add(image);
                        break;
                    case "wangsets":
                        ReadWangSets(child, result.wangsets);
                        break;
                }
            }
            result.image = images == null ? null : images.ToArray();

            return result;
        }

        #endregion
    }
}
