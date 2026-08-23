using System;
using System.IO;
using FlatRedBall.Glue.Tiled;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Shouldly;
using TMXGlueLib;
using Xunit;

namespace GlueUnitTests.Tiled;

/// <summary>
/// TiledMapSave.FromFile eagerly loads every referenced tileset's tsx file by default
/// (Tileset.ShouldLoadValuesFromSource == true), which throws FileNotFoundException the moment a TMX
/// references a tsx that does not exist on disk - a real, unremarkable situation (a moved/renamed/deleted
/// shared tileset) that Glue needs to tolerate while loading a project, not crash on. Every other
/// production TiledMapSave.FromFile call site (TmxCreationManager, FileReferenceManager, TmxCodeGenerator)
/// sets Tileset.ShouldLoadValuesFromSource = false around the call; TiledCache's two call sites
/// (RefreshCache, GetTiledMap) were the one place that didn't.
/// </summary>
public class TiledCacheTests
{
    public TiledCacheTests() => GlueTestBootstrap.EnsureInitialized();

    [Fact]
    public void GetTiledMap_ShouldNotThrow_WhenReferencedTilesetTsxIsMissing()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "TiledCacheMissingTsxTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var tmxPath = Path.Combine(tempDirectory, "Map.tmx");
            File.WriteAllText(tmxPath,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<map version=\"1.4\" tiledversion=\"1.4.3\" orientation=\"orthogonal\" renderorder=\"right-down\" " +
                "width=\"1\" height=\"1\" tilewidth=\"16\" tileheight=\"16\" infinite=\"0\" nextlayerid=\"1\" nextobjectid=\"1\">\n" +
                " <tileset firstgid=\"1\" source=\"DoesNotExist.tsx\"/>\n" +
                "</map>");

            var sut = new TiledCache();

            var exception = Record.Exception(() => sut.GetTiledMap(new FilePath(tmxPath)));

            exception.ShouldBeNull();
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }
}
