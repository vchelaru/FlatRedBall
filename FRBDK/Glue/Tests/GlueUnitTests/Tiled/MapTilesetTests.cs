using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Reproduces a crash reported against the AnimationEditor: Glue's file watcher reacts to a .tsx file
    /// changing on disk (e.g. an external editor saving over it) and re-reads it while it's still mid-write.
    /// A truncated/empty read fails XML parsing with "Root element is missing" (XmlException wrapped in
    /// InvalidOperationException), which previously propagated straight out of LoadValuesFromSource and
    /// crashed Glue. The fix retries a short-lived parse failure instead of treating it as a permanent one.
    /// </summary>
    [Fact]
    public void Source_ShouldRetryAndSucceed_WhenFileIsTransientlyEmptyDuringAnExternalWrite()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "MapTilesetRacyTsxTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDirectory);

        var originalRelativeDirectory = FileManager.RelativeDirectory;
        var originalShouldLoadValuesFromSource = Tileset.ShouldLoadValuesFromSource;
        var originalShouldThrowOnMissingSource = Tileset.ShouldThrowOnMissingSource;
        try
        {
            var tsxPath = Path.Combine(tempDirectory, "Racy.tsx");
            // Starts empty, standing in for the moment an external tool (like the AnimationEditor) has
            // truncated the file but not yet flushed its new content - this is what the OS-level file
            // watcher can observe mid-write.
            File.WriteAllText(tsxPath, "");

            FileManager.RelativeDirectory = tempDirectory + "\\";
            Tileset.ShouldLoadValuesFromSource = true;
            Tileset.ShouldThrowOnMissingSource = true;

            var writerTask = Task.Run(() =>
            {
                Thread.Sleep(50);
                File.WriteAllText(tsxPath,
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                    "<tileset name=\"Racy\" tilewidth=\"16\" tileheight=\"16\" tilecount=\"1\" columns=\"1\">\n" +
                    " <image source=\"racy.png\" width=\"16\" height=\"16\"/>\n" +
                    "</tileset>");
            });

            var tileset = new Tileset();
            var exception = Record.Exception(() => tileset.Source = "Racy.tsx");
            writerTask.Wait();

            exception.ShouldBeNull("a transient empty read should be retried, not treated as a permanent parse failure");
            tileset.Name.ShouldBe("Racy");
            tileset.Images.ShouldNotBeNull();
            tileset.Images.Length.ShouldBe(1);
        }
        finally
        {
            FileManager.RelativeDirectory = originalRelativeDirectory;
            Tileset.ShouldLoadValuesFromSource = originalShouldLoadValuesFromSource;
            Tileset.ShouldThrowOnMissingSource = originalShouldThrowOnMissingSource;
            Directory.Delete(tempDirectory, true);
        }
    }

    /// <summary>
    /// The retry is bounded - a .tsx that never becomes valid XML (genuinely corrupt, not just mid-write)
    /// must still throw, not retry forever.
    /// </summary>
    [Fact]
    public void Source_ShouldStillThrow_WhenFileContentIsPermanentlyMalformed()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "MapTilesetMalformedTsxTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDirectory);

        var originalRelativeDirectory = FileManager.RelativeDirectory;
        var originalShouldLoadValuesFromSource = Tileset.ShouldLoadValuesFromSource;
        var originalShouldThrowOnMissingSource = Tileset.ShouldThrowOnMissingSource;
        try
        {
            var tsxPath = Path.Combine(tempDirectory, "Malformed.tsx");
            File.WriteAllText(tsxPath, "");

            FileManager.RelativeDirectory = tempDirectory + "\\";
            Tileset.ShouldLoadValuesFromSource = true;
            Tileset.ShouldThrowOnMissingSource = true;

            var tileset = new Tileset();
            var exception = Record.Exception(() => tileset.Source = "Malformed.tsx");

            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<InvalidOperationException>();
        }
        finally
        {
            FileManager.RelativeDirectory = originalRelativeDirectory;
            Tileset.ShouldLoadValuesFromSource = originalShouldLoadValuesFromSource;
            Tileset.ShouldThrowOnMissingSource = originalShouldThrowOnMissingSource;
            Directory.Delete(tempDirectory, true);
        }
    }

    /// <summary>
    /// Same race as Source_ShouldRetryAndSucceed_WhenFileIsTransientlyEmptyDuringAnExternalWrite, but for
    /// the .tmx itself rather than a referenced .tsx - TiledMapSave.FromFile has its own direct
    /// FileManager.XmlDeserialize read of the map file.
    /// </summary>
    [Fact]
    public void FromFile_ShouldRetryAndSucceed_WhenFileIsTransientlyEmptyDuringAnExternalWrite()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "TiledMapSaveRacyTmxTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDirectory);

        var originalRelativeDirectory = FileManager.RelativeDirectory;
        try
        {
            var tmxPath = Path.Combine(tempDirectory, "Racy.tmx");
            // Starts empty, standing in for the moment an external tool has truncated the file but not yet
            // flushed its new content - this is what the OS-level file watcher can observe mid-write.
            File.WriteAllText(tmxPath, "");

            var writerTask = Task.Run(() =>
            {
                Thread.Sleep(50);
                File.WriteAllText(tmxPath,
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                    "<map version=\"1.4\" tiledversion=\"1.4.3\" orientation=\"orthogonal\" renderorder=\"right-down\" " +
                    "width=\"1\" height=\"1\" tilewidth=\"16\" tileheight=\"16\" infinite=\"0\" nextlayerid=\"1\" nextobjectid=\"1\">\n" +
                    "</map>");
            });

            TiledMapSave tms = null;
            var exception = Record.Exception(() => tms = TiledMapSave.FromFile(tmxPath));
            writerTask.Wait();

            exception.ShouldBeNull("a transient empty read should be retried, not treated as a permanent parse failure");
            tms.ShouldNotBeNull();
        }
        finally
        {
            FileManager.RelativeDirectory = originalRelativeDirectory;
            Directory.Delete(tempDirectory, true);
        }
    }

    /// <summary>
    /// The retry is bounded - a .tmx that never becomes valid XML (genuinely corrupt, not just mid-write)
    /// must still throw, not retry forever.
    /// </summary>
    [Fact]
    public void FromFile_ShouldStillThrow_WhenFileContentIsPermanentlyMalformed()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "TiledMapSaveMalformedTmxTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDirectory);

        var originalRelativeDirectory = FileManager.RelativeDirectory;
        try
        {
            var tmxPath = Path.Combine(tempDirectory, "Malformed.tmx");
            File.WriteAllText(tmxPath, "");

            var exception = Record.Exception(() => TiledMapSave.FromFile(tmxPath));

            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<InvalidOperationException>();
        }
        finally
        {
            FileManager.RelativeDirectory = originalRelativeDirectory;
            Directory.Delete(tempDirectory, true);
        }
    }
}
