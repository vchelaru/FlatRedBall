using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class GridSnapperTests
{
    // ── Basic positive snapping ───────────────────────────────────────────────

    [Fact]
    public void Snap_ValueAtGridBoundary_ReturnsSameValue()
    {
        Assert.Equal(16, GridSnapper.Snap(16f, 16));
        Assert.Equal(32, GridSnapper.Snap(32f, 16));
        Assert.Equal(0,  GridSnapper.Snap(0f,  16));
    }

    [Fact]
    public void Snap_ValueBetweenBoundaries_SnapsDown()
    {
        Assert.Equal(16, GridSnapper.Snap(20f,   16));
        Assert.Equal(16, GridSnapper.Snap(31.9f, 16));
        Assert.Equal(0,  GridSnapper.Snap(15.9f, 16));
    }

    [Fact]
    public void Snap_ValueJustAboveZero_ReturnsZero()
    {
        Assert.Equal(0, GridSnapper.Snap(0.5f, 16));
        Assert.Equal(0, GridSnapper.Snap(1f,   16));
    }

    [Fact]
    public void Snap_LargeValue_SnapsToCorrectMultiple()
    {
        // 256 / 16 = 16 exactly → 256
        Assert.Equal(256, GridSnapper.Snap(256f, 16));
        // 260 → 256
        Assert.Equal(256, GridSnapper.Snap(260f, 16));
    }

    [Fact]
    public void Snap_GridSizeOne_ReturnsTruncatedInt()
    {
        Assert.Equal(7, GridSnapper.Snap(7.9f, 1));
        Assert.Equal(0, GridSnapper.Snap(0.9f, 1));
    }

    // ── Negative values ───────────────────────────────────────────────────────

    [Fact]
    public void Snap_NegativeValue_SnapsTowardNegativeInfinity()
    {
        // -5 with gridSize=16 → floor(-5/16) = floor(-0.3125) = -1 → -16
        Assert.Equal(-16, GridSnapper.Snap(-5f,  16));
        // -16 exactly → -16
        Assert.Equal(-16, GridSnapper.Snap(-16f, 16));
        // -17 → -32
        Assert.Equal(-32, GridSnapper.Snap(-17f, 16));
    }

    // ── Guard: gridSize ≤ 0 ───────────────────────────────────────────────────

    [Fact]
    public void Snap_GridSizeZero_ReturnsTruncatedValue()
    {
        Assert.Equal(7,  GridSnapper.Snap(7.9f,  0));
        Assert.Equal(-1, GridSnapper.Snap(-1.5f, 0));
    }

    [Fact]
    public void Snap_GridSizeNegative_ReturnsTruncatedValue()
    {
        Assert.Equal(5, GridSnapper.Snap(5.7f, -1));
    }

    // ── Typical usage (mimics WireframeControl click handler) ─────────────────

    [Theory]
    [InlineData(0f,   8, 0)]
    [InlineData(7f,   8, 0)]
    [InlineData(8f,   8, 8)]
    [InlineData(9f,   8, 8)]
    [InlineData(15f,  8, 8)]
    [InlineData(16f,  8, 16)]
    [InlineData(100f, 8, 96)]
    public void Snap_GridSize8_Theory(float input, int gridSize, int expected)
    {
        Assert.Equal(expected, GridSnapper.Snap(input, gridSize));
    }
}
