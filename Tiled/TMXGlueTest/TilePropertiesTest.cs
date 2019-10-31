using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TMXGlueLib;

namespace TMXGlueTest
{
    [TestClass]
    public class TilePropertiesTest
    {
        [TestMethod]
        public void ellipse_element_test()
        {
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<map version=""1.0"" orientation=""orthogonal"" renderorder=""right-down"" width=""5"" height=""5"" tilewidth=""16"" tileheight=""16"">
 <layer name=""Tile Layer 1"" width=""5"" height=""5"">
  <data encoding=""base64"" compression=""gzip"">
   H4sIAAAAAAAAC2NgoD0AAMrGiJlkAAAA
  </data>
 </layer>
 <objectgroup name=""Object Layer 1"">
  <object x=""0"" y=""0"" width=""16"" height=""16"" rotation=""45""/>
  <object x=""32"" y=""0"" rotation=""-45"">
   <polygon points=""0,0 16,0 16,16""/>
  </object>
  <object x=""32"" y=""32"" rotation=""-13.6293"">
   <polyline points=""0,0 16,0 32,16""/>
  </object>
  <object x=""0"" y=""0"" width=""16"" height=""16"">
   <ellipse/>
  </object>
 </objectgroup>
</map>";
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                var xs = new XmlSerializer(typeof(TiledMapSave));
                var tilemap = (TiledMapSave)xs.Deserialize(ms);
                Assert.AreEqual(4, tilemap.objectgroup[0].@object.Length);
                Assert.IsNull(tilemap.objectgroup[0].@object[0].ellipse);
                Assert.IsNull(tilemap.objectgroup[0].@object[1].ellipse);
                Assert.IsNull(tilemap.objectgroup[0].@object[2].ellipse);
                Assert.IsNotNull(tilemap.objectgroup[0].@object[3].ellipse);
            }
        }
        [TestMethod]
        public void terrain_attribute_does_not_break_properties_in_deserialization()
        {
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                        <map orientation=""orthogonal"" width=""25"" height=""25"" tilewidth=""64"" tileheight=""64"">
                        <tileset firstgid=""1"" name=""EnvironmentTiles"" tilewidth=""64"" tileheight=""64"">
<image source=""tilemapd3f7rwg.png"" width=""512"" height=""512""/>
<tile id=""0"" terrain=""0,0,0,"">
<properties>
<property name=""Name"" value=""CollisionTile""/>
</properties>
</tile>
</tileset>
<layer name=""EnvironmentBackground"" width=""25"" height=""25"">
  <data>
   <tile gid=""1""/>
  </data>
</layer>
                        </map>";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                var xs = new XmlSerializer(typeof(TiledMapSave));
                var tilemap = (TiledMapSave)xs.Deserialize(ms);
                Assert.AreEqual(1, tilemap.Tilesets[0].Tiles.Count);

                var tile = tilemap.Tilesets[0].Tiles[0];

                Assert.AreEqual(1, tile.properties.Count);
                Assert.AreEqual("Name", tile.properties[0].name);
                Assert.AreEqual("CollisionTile", tile.properties[0].value);
            }
        }

        [TestMethod]
        public void name_property_transfers_over_in_conversion()
        {
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                        <map orientation=""orthogonal"" width=""25"" height=""25"" tilewidth=""64"" tileheight=""64"">
                        <tileset firstgid=""1"" name=""EnvironmentTiles"" tilewidth=""64"" tileheight=""64"">
<image source=""tilemapd3f7rwg.png"" width=""512"" height=""512""/>
<terraintypes>
    <terrain name=""Grass"" tile=""-1""/>
</terraintypes>
<tile id=""0"" terrain=""0,0,0,"">
<properties>
<property name=""Name"" value=""CollisionTile""/>
</properties>
</tile>
</tileset>
<layer name=""EnvironmentBackground"" width=""25"" height=""25"">
  <data>
   <tile gid=""1""/>
  </data>
</layer>
                        </map>";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                var xs = new XmlSerializer(typeof(TiledMapSave));
                var tilemap = (TiledMapSave)xs.Deserialize(ms);
                var tile = tilemap.Tilesets[0].Tiles[0];

                var sceneSave = tilemap.ToSceneSave(1.0f);
                Assert.AreEqual(1, sceneSave.SpriteList.Count);
                Assert.AreEqual("CollisionTile", sceneSave.SpriteList[0].Name);

            }
        }

    }
}
