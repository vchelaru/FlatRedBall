using System;
using System.IO;
using System.Linq;
using FlatRedBall.IO;
using Shouldly;
using TMXGlueLib;
using Xunit;

namespace GlueUnitTests.Tiled;

/// <summary>
/// A TMX can reference several tilesets. Previously, one tileset's tsx being missing threw
/// FileNotFoundException out of TiledMapSave.FromFile entirely - aborting the whole map's load/codegen,
/// including everything tied to the other, still-valid tilesets. Tileset.ShouldThrowOnMissingSource lets a
/// caller (TmxCodeGenerator) opt out of that: the broken tileset degrades to its default (empty)
/// Tiles/Images and the rest of the map loads normally.
/// </summary>
public class MapTilesetTests
{
    [Fact]
    public void FromFile_ShouldNotThrow_AndShouldStillLoadOtherTilesets_WhenOneTilesetTsxIsMissing_AndShouldThrowOnMissingSourceIsFalse()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "MapTilesetMissingTsxTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDirectory);

        var originalRelativeDirectory = FileManager.RelativeDirectory;
        var originalShouldLoadValuesFromSource = Tileset.ShouldLoadValuesFromSource;
        var originalShouldThrowOnMissingSource = Tileset.ShouldThrowOnMissingSource;
        try
        {
            File.WriteAllText(Path.Combine(tempDirectory, "Valid.tsx"),
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<tileset name=\"Valid\" tilewidth=\"16\" tileheight=\"16\" tilecount=\"1\" columns=\"1\">\n" +
                " <image source=\"valid.png\" width=\"16\" height=\"16\"/>\n" +
                "</tileset>");

            var tmxPath = Path.Combine(tempDirectory, "Map.tmx");
            File.WriteAllText(tmxPath,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<map version=\"1.4\" tiledversion=\"1.4.3\" orientation=\"orthogonal\" renderorder=\"right-down\" " +
                "width=\"1\" height=\"1\" tilewidth=\"16\" tileheight=\"16\" infinite=\"0\" nextlayerid=\"1\" nextobjectid=\"1\">\n" +
                " <tileset firstgid=\"1\" source=\"Valid.tsx\"/>\n" +
                " <tileset firstgid=\"2\" source=\"DoesNotExist.tsx\"/>\n" +
                "</map>");

            FileManager.RelativeDirectory = tempDirectory + "\\";
            Tileset.ShouldLoadValuesFromSource = true;
            Tileset.ShouldThrowOnMissingSource = false;

            TiledMapSave tms = null;
            var exception = Record.Exception(() => tms = TiledMapSave.FromFile(tmxPath));

            exception.ShouldBeNull();
            tms.ShouldNotBeNull();
            tms.Tilesets.Count.ShouldBe(2);

            var validTileset = tms.Tilesets.Single(t => t.Source.Contains("Valid"));
            validTileset.Images.ShouldNotBeNull();
            validTileset.Images.Length.ShouldBe(1, "the valid tileset should have loaded fully despite the other tileset's missing tsx");

            var brokenTileset = tms.Tilesets.Single(t => t.Source.Contains("DoesNotExist"));
            brokenTileset.Tiles.ShouldBeEmpty("the broken tileset should have degraded to its default rather than throwing");
        }
        finally
        {
            FileManager.RelativeDirectory = originalRelativeDirectory;
            Tileset.ShouldLoadValuesFromSource = originalShouldLoadValuesFromSource;
            Tileset.ShouldThrowOnMissingSource = originalShouldThrowOnMissingSource;
            Directory.Delete(tempDirectory, true);
        }
    }
}
