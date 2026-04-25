using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class TileCoordinateCalculatorTests
{
    // ── GetLeftRight ─────────────────────────────────────────────────────────

    [Fact]
    public void GetLeftRight_FirstTile_ReturnsZeroToOneTileWidth()
    {
        // tile 0, 32px wide tile, 256px texture
        var (left, right) = TileCoordinateCalculator.GetLeftRight(0, 32, 256);
        Assert.Equal(0f,        left,  precision: 4);
        Assert.Equal(32f / 256f, right, precision: 4);
    }

    [Fact]
    public void GetLeftRight_SecondTile_ShiftsByOneTileWidth()
    {
        // tile 1, 32px, 256px texture
        var (left, right) = TileCoordinateCalculator.GetLeftRight(1, 32, 256);
        Assert.Equal(32f / 256f, left,  precision: 4);
        Assert.Equal(64f / 256f, right, precision: 4);
    }

    [Fact]
    public void GetLeftRight_LastTileInRow_RightEdgeIsOne()
    {
        // 8 tiles × 32px = 256px texture; last tile index 7
        var (left, right) = TileCoordinateCalculator.GetLeftRight(7, 32, 256);
        Assert.Equal(224f / 256f, left,  precision: 4);
        Assert.Equal(1f,          right, precision: 4);
    }

    [Fact]
    public void GetLeftRight_NonSquareTileWidth_Correct()
    {
        // 16px tile on 128px texture → tile 2
        var (left, right) = TileCoordinateCalculator.GetLeftRight(2, 16, 128);
        Assert.Equal(32f / 128f, left,  precision: 4);
        Assert.Equal(48f / 128f, right, precision: 4);
    }

    [Fact]
    public void GetLeftRight_TileWidthEqualsTextureWidth_TileZeroCoversAll()
    {
        // single tile equals full texture
        var (left, right) = TileCoordinateCalculator.GetLeftRight(0, 64, 64);
        Assert.Equal(0f, left,  precision: 4);
        Assert.Equal(1f, right, precision: 4);
    }

    // ── GetTopBottom ──────────────────────────────────────────────────────────

    [Fact]
    public void GetTopBottom_FirstTile_ReturnsZeroToOneTileHeight()
    {
        var (top, bottom) = TileCoordinateCalculator.GetTopBottom(0, 32, 256);
        Assert.Equal(0f,        top,    precision: 4);
        Assert.Equal(32f / 256f, bottom, precision: 4);
    }

    [Fact]
    public void GetTopBottom_ThirdTile_CorrectRange()
    {
        // tile 2, 48px tile, 192px texture
        var (top, bottom) = TileCoordinateCalculator.GetTopBottom(2, 48, 192);
        Assert.Equal(96f  / 192f, top,    precision: 4);
        Assert.Equal(144f / 192f, bottom, precision: 4);
    }

    [Fact]
    public void GetTopBottom_LastTile_BottomIsOne()
    {
        // 4 rows × 16px = 64px
        var (_, bottom) = TileCoordinateCalculator.GetTopBottom(3, 16, 64);
        Assert.Equal(1f, bottom, precision: 4);
    }

    // ── Symmetric round-trip via index ────────────────────────────────────────

    [Fact]
    public void GetLeftRight_And_GetTopBottom_ProduceSameSizedCells()
    {
        // Square tiles — width and height spans should be equal
        int tileSize    = 32;
        int textureSize = 256;

        var (l, r) = TileCoordinateCalculator.GetLeftRight (3, tileSize, textureSize);
        var (t, b) = TileCoordinateCalculator.GetTopBottom(3, tileSize, textureSize);

        Assert.Equal(r - l, b - t, precision: 4);
    }

    // ── Guard conditions ──────────────────────────────────────────────────────

    [Fact]
    public void GetLeftRight_ZeroTextureWidth_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TileCoordinateCalculator.GetLeftRight(0, 32, 0));
    }

    [Fact]
    public void GetTopBottom_ZeroTileHeight_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TileCoordinateCalculator.GetTopBottom(0, 0, 128));
    }

    // ── CellSizeFromCount ────────────────────────────────────────────────────
    // Derives the pixel cell size from "N cells across a texture of X pixels".
    // Mirrors the WinForms "Set cell height to 2 cells → 64 px" workflow.

    [Fact]
    public void CellSizeFromCount_TwoCells_HalfTextureSize()
        => Assert.Equal(64, TileCoordinateCalculator.CellSizeFromCount(2, 128));

    [Fact]
    public void CellSizeFromCount_FourCells_QuarterTextureSize()
        => Assert.Equal(64, TileCoordinateCalculator.CellSizeFromCount(4, 256));

    [Fact]
    public void CellSizeFromCount_EightCells_EighthTextureSize()
        => Assert.Equal(16, TileCoordinateCalculator.CellSizeFromCount(8, 128));

    [Fact]
    public void CellSizeFromCount_OneCell_ReturnsFullTextureSize()
        => Assert.Equal(64, TileCoordinateCalculator.CellSizeFromCount(1, 64));

    [Fact]
    public void CellSizeFromCount_EvenlyDivisible_NoPrecisionLoss()
        => Assert.Equal(100, TileCoordinateCalculator.CellSizeFromCount(3, 300));

    [Fact]
    public void CellSizeFromCount_NonEvenDivision_FloorsToInteger()
        // 100 / 3 = 33.33... → floor → 33
        => Assert.Equal(33, TileCoordinateCalculator.CellSizeFromCount(3, 100));

    [Fact]
    public void CellSizeFromCount_ZeroCellCount_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            TileCoordinateCalculator.CellSizeFromCount(0, 128));

    [Fact]
    public void CellSizeFromCount_ZeroTextureSize_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            TileCoordinateCalculator.CellSizeFromCount(4, 0));

    [Fact]
    public void CellSizeFromCount_NegativeCellCount_Throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            TileCoordinateCalculator.CellSizeFromCount(-1, 128));
}
