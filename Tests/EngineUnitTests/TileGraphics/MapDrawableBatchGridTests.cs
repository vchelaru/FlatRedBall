using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using EngineUnitTests.TestSupport;
using FlatRedBall.TileGraphics;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;

namespace EngineUnitTests.TileGraphics;

[Collection(nameof(EngineStatefulTestCollection))]
public class MapDrawableBatchGridTests
{
    // Every map here is loaded from a real .tmx through LayeredTileMap.FromTiledMapSave, so column/row is
    // checked against Tiled's own grid. The tileset texture is an uninitialized Texture2D registered under
    // the image's path, since there is no GraphicsDevice; nothing on this path reads its pixels.
    const int CellSize = 16;

    static LayeredTileMap LoadMap(string[] layerCsvs, int width, int height,
        int tilesetTileSize = CellSize, string orientation = "orthogonal")
    {
        EngineTestBootstrap.EnsureInitialized();

        var directory = Path.Combine(Path.GetTempPath(), "MapDrawableBatchGridTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var imagePath = Path.Combine(directory, "tiles.png");
        var tmxPath = Path.Combine(directory, "map.tmx");

        var layers = string.Concat(layerCsvs.Select((csv, i) =>
            $"""
             <layer id="{i + 1}" name="Layer{i}" width="{width}" height="{height}">
              <data encoding="csv">
            {csv}
            </data>
             </layer>
            """));

        File.WriteAllText(tmxPath,
            $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <map version="1.10" orientation="{orientation}" renderorder="right-down" width="{width}" height="{height}" tilewidth="{CellSize}" tileheight="{CellSize}" infinite="0" nextlayerid="{layerCsvs.Length + 1}" nextobjectid="1">
             <tileset firstgid="1" name="tiles" tilewidth="{tilesetTileSize}" tileheight="{tilesetTileSize}" tilecount="16" columns="4">
              <image source="tiles.png" width="{tilesetTileSize * 4}" height="{tilesetTileSize * 4}"/>
             </tileset>
            {layers}
            </map>
            """);

        var contentManager = "GridTests" + Guid.NewGuid().ToString("N");
        var texture = (Texture2D)FormatterServices.GetUninitializedObject(typeof(Texture2D));
        FlatRedBall.FlatRedBallServices.AddDisposable(imagePath, texture, contentManager);

        return LayeredTileMap.FromTiledMapSave(tmxPath, contentManager);
    }

    static HashSet<(int column, int row)> CellsOf(MapDrawableBatch layer)
    {
        var cells = new HashSet<(int, int)>();
        for (int i = 0; i < layer.QuadCount; i++)
        {
            layer.GetTileColumnAndRow(i, out var column, out var row);
            cells.Add((column, row));
        }
        return cells;
    }

    // A 6x3 map is wider than tall, so the loader sorts quads on X: ordered index != TMX order.
    static readonly string GroundCsv =
        "1,0,0,0,0,0,\n" +
        "0,0,2,0,0,0,\n" +
        "0,0,0,0,0,3";

    [Fact]
    public void LoadedLayer_ShouldExposeMapCellSize()
    {
        var layer = LoadMap([GroundCsv], 6, 3).MapLayers[0];

        layer.WidthPerTile.ShouldBe(CellSize);
        layer.HeightPerTile.ShouldBe(CellSize);
    }

    [Fact]
    public void LoadedLayer_ShouldExposeMapCellSize_WhenTilesetTilesAreLarger()
    {
        var layer = LoadMap([GroundCsv], 6, 3, tilesetTileSize: 32).MapLayers[0];

        layer.WidthPerTile.ShouldBe(CellSize);
        layer.HeightPerTile.ShouldBe(CellSize);
    }

    [Fact]
    public void GetTileColumnAndRow_ShouldMatchTmxGrid()
    {
        var layer = LoadMap([GroundCsv], 6, 3).MapLayers[0];

        CellsOf(layer).ShouldBe(new[] { (0, 0), (2, 1), (5, 2) }, ignoreOrder: true);
    }

    [Fact]
    public void GetTileColumnAndRow_ShouldUseTheCellTheTileSitsIn_WhenTilesetTilesAreLarger()
    {
        // Tiled anchors a tile larger than the grid at its cell's bottom-left, so it reports that cell.
        var layer = LoadMap([GroundCsv], 6, 3, tilesetTileSize: 32).MapLayers[0];

        CellsOf(layer).ShouldBe(new[] { (0, 0), (2, 1), (5, 2) }, ignoreOrder: true);
    }

    [Fact]
    public void GetTileColumnAndRow_ShouldMatchTmxGrid_AfterSortQuadsOnAxis()
    {
        var layer = LoadMap([GroundCsv], 6, 3).MapLayers[0];

        layer.SortQuadsOnAxis(SortAxis.Y);

        CellsOf(layer).ShouldBe(new[] { (0, 0), (2, 1), (5, 2) }, ignoreOrder: true);
    }

    [Fact]
    public void GetTileColumnAndRow_ShouldMatchTmxGrid_AfterMergeOntoThis()
    {
        var map = LoadMap([GroundCsv, "0,1,0,0,0,0,\n0,0,0,0,0,0,\n0,0,0,4,0,0"], 6, 3);
        var bottom = map.MapLayers[0];

        bottom.MergeOntoThis(new[] { map.MapLayers[1] });

        CellsOf(bottom).ShouldBe(new[] { (0, 0), (2, 1), (5, 2), (1, 0), (3, 2) }, ignoreOrder: true);
    }

    [Fact]
    public void MergeOntoThis_ShouldTakeCellSize_WhenThisHasNone()
    {
        var layer = LoadMap([GroundCsv], 6, 3).MapLayers[0];
        var empty = new MapDrawableBatch(0, layer.Texture);

        empty.MergeOntoThis(new[] { layer });

        empty.WidthPerTile.ShouldBe(CellSize);
        empty.HeightPerTile.ShouldBe(CellSize);
        CellsOf(empty).ShouldBe(new[] { (0, 0), (2, 1), (5, 2) }, ignoreOrder: true);
    }

    [Fact]
    public void GetQuadIndexAtColumnAndRow_ShouldReturnTheQuadInThatCell()
    {
        var layer = LoadMap([GroundCsv], 6, 3).MapLayers[0];

        foreach (var (column, row) in new[] { (0, 0), (2, 1), (5, 2) })
        {
            var index = layer.GetQuadIndexAtColumnAndRow(column, row);

            index.ShouldNotBeNull();
            layer.GetTileColumnAndRow(index.Value, out var foundColumn, out var foundRow);
            (foundColumn, foundRow).ShouldBe((column, row));
        }
    }

    [Fact]
    public void GetQuadIndexAtColumnAndRow_ShouldReturnTheQuadInThatCell_WhenSortedOnY()
    {
        // 3x6 is taller than wide, so the loader sorts on Y.
        var layer = LoadMap(["1,0,0,\n0,0,0,\n0,2,0,\n0,0,0,\n0,0,0,\n0,0,3"], 3, 6).MapLayers[0];

        layer.SortAxis.ShouldBe(SortAxis.Y);
        foreach (var (column, row) in new[] { (0, 0), (1, 2), (2, 5) })
        {
            var index = layer.GetQuadIndexAtColumnAndRow(column, row);

            index.ShouldNotBeNull();
            layer.GetTileColumnAndRow(index.Value, out var foundColumn, out var foundRow);
            (foundColumn, foundRow).ShouldBe((column, row));
        }
    }

    [Fact]
    public void GetQuadIndexAtColumnAndRow_ShouldReturnNull_ForAnEmptyCell()
    {
        var layer = LoadMap([GroundCsv], 6, 3).MapLayers[0];

        layer.GetQuadIndexAtColumnAndRow(1, 1).ShouldBeNull();
        layer.GetQuadIndexAtColumnAndRow(2, 0).ShouldBeNull();
        layer.GetQuadIndexAtColumnAndRow(2, 2).ShouldBeNull();
    }

    [Fact]
    public void GetQuadIndexAtColumnAndRow_ShouldNotReturnALargeTileFromTheCellBelow()
    {
        // A 32x32 tile in cell (2,1) also covers the cell above it; it still belongs only to (2,1).
        var layer = LoadMap([GroundCsv], 6, 3, tilesetTileSize: 32).MapLayers[0];

        layer.GetQuadIndexAtColumnAndRow(2, 0).ShouldBeNull();
        layer.GetQuadIndexAtColumnAndRow(2, 1).ShouldNotBeNull();
    }

    [Fact]
    public void GetTileColumnAndRow_ShouldThrow_WhenCellSizeIsUnknown()
    {
        var layer = LoadMap(["1,0,\n0,2"], 2, 2, orientation: "isometric").MapLayers[0];

        layer.WidthPerTile.ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => layer.GetTileColumnAndRow(0, out _, out _));
        Should.Throw<InvalidOperationException>(() => layer.GetQuadIndexAtColumnAndRow(0, 0));
    }
}
