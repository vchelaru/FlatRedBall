using System;
using System.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Shouldly;
using TiledPlugin.Errors;
using TMXGlueLib;
using Xunit;

namespace GlueUnitTests.TiledPlugin;

// The Tiled error reporter checks every layer of every map. It already holds each map, so it passes that
// map in instead of having GetIfHasError parse the file again per layer.
public class MultipleTilesetPerLayerErrorTests : IDisposable
{
    readonly GlueProjectSave originalProject;
    readonly string directory = Path.Combine(Path.GetTempPath(), "MultipleTilesetPerLayerErrorTests_" + Guid.NewGuid().ToString("N"));
    readonly FilePath mapPath;

    public MultipleTilesetPerLayerErrorTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        originalProject = ObjectFinder.Self.GlueProject;
        Directory.CreateDirectory(directory);
        mapPath = new FilePath(Path.Combine(directory, "Map.tmx"));

        var screen = new ScreenSave { Name = "Screens\\MapScreen" };
        // FilePath set directly: there is no main project here to resolve Name against.
        screen.ReferencedFiles.Add(new ReferencedFileSave { Name = "Map.tmx", FilePath = mapPath });
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        ObjectFinder.Self.GlueProject.Screens.Add(screen);
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = originalProject;
        try { Directory.Delete(directory, recursive: true); } catch (Exception) { }
    }

    [Fact]
    public void GetIfHasError_ShouldFindTheError_WhenReadingTheFile()
    {
        File.WriteAllText(mapPath.FullPath, Map(secondTileGid: 2));

        MultipleTilesetPerLayerErrorViewModel.GetIfHasError(mapPath, "Ground", out var tileIndex, out var columnCount)
            .ShouldBeTrue();
        tileIndex.ShouldBe(1);
        columnCount.ShouldBe(2);
    }

    [Fact]
    public void GetIfHasError_ShouldUseTheLoadedMap_InsteadOfReadingTheFile()
    {
        File.WriteAllText(mapPath.FullPath, Map(secondTileGid: 2));
        var cleanMapPath = Path.Combine(directory, "Clean.tmx");
        File.WriteAllText(cleanMapPath, Map(secondTileGid: 1));
        var cleanMap = TiledMapSave.FromFile(cleanMapPath);

        MultipleTilesetPerLayerErrorViewModel.GetIfHasError(mapPath, "Ground", cleanMap, out _, out _)
            .ShouldBeFalse();
    }

    // A 2x1 layer. Tileset A holds gid 1, tileset B gid 2, so a second tile of 2 mixes tilesets.
    static string Map(int secondTileGid) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<map version=\"1.4\" tiledversion=\"1.4.3\" orientation=\"orthogonal\" renderorder=\"right-down\" " +
        "width=\"2\" height=\"1\" tilewidth=\"16\" tileheight=\"16\" infinite=\"0\" nextlayerid=\"2\" nextobjectid=\"1\">\n" +
        " <tileset firstgid=\"1\" name=\"A\" tilewidth=\"16\" tileheight=\"16\" tilecount=\"1\" columns=\"1\">\n" +
        "  <image source=\"a.png\" width=\"16\" height=\"16\"/>\n" +
        " </tileset>\n" +
        " <tileset firstgid=\"2\" name=\"B\" tilewidth=\"16\" tileheight=\"16\" tilecount=\"1\" columns=\"1\">\n" +
        "  <image source=\"b.png\" width=\"16\" height=\"16\"/>\n" +
        " </tileset>\n" +
        " <layer id=\"1\" name=\"Ground\" width=\"2\" height=\"1\">\n" +
        $"  <data encoding=\"csv\">1,{secondTileGid}</data>\n" +
        " </layer>\n" +
        "</map>";
}
