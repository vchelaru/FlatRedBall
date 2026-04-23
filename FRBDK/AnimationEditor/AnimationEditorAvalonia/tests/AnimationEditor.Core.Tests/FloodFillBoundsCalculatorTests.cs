using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class FloodFillBoundsCalculatorTests
{
    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Build an isOpaque delegate from a flat 0/1 pixel array.</summary>
    private static System.Func<int, int, bool> Pixels(int[] px, int width)
        => (x, y) => px[y * width + x] > 0;

    // ── Basic single-pixel ────────────────────────────────────────────────────

    [Fact]
    public void TryGetBounds_SingleOpaquePixel_ReturnsThatPixel()
    {
        // 3×3, only centre pixel is opaque
        int[] px = { 0,0,0,  0,1,0,  0,0,0 };
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            1, 1, 3, 3, Pixels(px, 3), out int l, out int t, out int r, out int b);
        Assert.True(ok);
        Assert.Equal(1, l); Assert.Equal(1, t);
        Assert.Equal(1, r); Assert.Equal(1, b);
    }

    // ── Rectangular region ────────────────────────────────────────────────────

    [Fact]
    public void TryGetBounds_SolidInnerBlock_ReturnsBlockBounds()
    {
        // 5×5 grid, 3×3 solid block at (1,1)–(3,3)
        int[] px =
        {
            0,0,0,0,0,
            0,1,1,1,0,
            0,1,1,1,0,
            0,1,1,1,0,
            0,0,0,0,0
        };
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            2, 2, 5, 5, Pixels(px, 5), out int l, out int t, out int r, out int b);
        Assert.True(ok);
        Assert.Equal(1, l); Assert.Equal(1, t);
        Assert.Equal(3, r); Assert.Equal(3, b);
    }

    // ── Transparent seed ─────────────────────────────────────────────────────

    [Fact]
    public void TryGetBounds_TransparentSeed_ReturnsFalse()
    {
        int[] px = { 0,0,0, 0,0,0, 0,0,0 };
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            1, 1, 3, 3, Pixels(px, 3), out _, out _, out _, out _);
        Assert.False(ok);
    }

    // ── Out-of-bounds seed ────────────────────────────────────────────────────

    [Fact]
    public void TryGetBounds_SeedXNegative_ReturnsFalse()
    {
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            -1, 0, 3, 3, (x, y) => true, out _, out _, out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryGetBounds_SeedYBeyondHeight_ReturnsFalse()
    {
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            0, 5, 3, 3, (x, y) => true, out _, out _, out _, out _);
        Assert.False(ok);
    }

    // ── Zero bitmap ──────────────────────────────────────────────────────────

    [Fact]
    public void TryGetBounds_ZeroDimension_ReturnsFalse()
    {
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            0, 0, 0, 0, (x, y) => true, out _, out _, out _, out _);
        Assert.False(ok);
    }

    // ── Flood does NOT cross gap ──────────────────────────────────────────────

    [Fact]
    public void TryGetBounds_TwoIslandsWithGap_OnlyFillsSeedIsland()
    {
        // 1×5 row: island [0,1] gap [2] island [3,4]
        int[] px = { 1, 1, 0, 1, 1 };
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            0, 0, 5, 1, Pixels(px, 5), out int l, out _, out int r, out _);
        Assert.True(ok);
        Assert.Equal(0, l);
        Assert.Equal(1, r); // must NOT reach x=3 or x=4
    }

    // ── Entire bitmap opaque ──────────────────────────────────────────────────

    [Fact]
    public void TryGetBounds_EntirelyOpaqueBitmap_ReturnsBitmapBounds()
    {
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            0, 0, 4, 4, (x, y) => true, out int l, out int t, out int r, out int b);
        Assert.True(ok);
        Assert.Equal(0, l); Assert.Equal(0, t);
        Assert.Equal(3, r); Assert.Equal(3, b);
    }

    // ── L-shaped region ──────────────────────────────────────────────────────

    [Fact]
    public void TryGetBounds_LShapedRegion_ReturnsCoveringBoundingBox()
    {
        // 4×4, L-shape: first column + bottom row
        // col 0: all rows, row 3: all cols
        // 1 0 0 0
        // 1 0 0 0
        // 1 0 0 0
        // 1 1 1 1
        int[] px =
        {
            1,0,0,0,
            1,0,0,0,
            1,0,0,0,
            1,1,1,1
        };
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            0, 0, 4, 4, Pixels(px, 4), out int l, out int t, out int r, out int b);
        Assert.True(ok);
        Assert.Equal(0, l); Assert.Equal(0, t);
        Assert.Equal(3, r); Assert.Equal(3, b);
    }

    // ── 1×1 bitmap ───────────────────────────────────────────────────────────

    [Fact]
    public void TryGetBounds_SinglePixelBitmap_OpaqueSeed_ReturnsZeroZero()
    {
        var ok = FloodFillBoundsCalculator.TryGetBounds(
            0, 0, 1, 1, (x, y) => true, out int l, out int t, out int r, out int b);
        Assert.True(ok);
        Assert.Equal(0, l); Assert.Equal(0, t);
        Assert.Equal(0, r); Assert.Equal(0, b);
    }
}
