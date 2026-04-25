using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

/// <summary>
/// Tests for <see cref="WireframeTransform"/> — pure pan/zoom coordinate math.
/// No UI, no SkiaSharp, no singleton dependencies.
/// </summary>
public class WireframeTransformTests
{
    // ── ScreenToTexture ───────────────────────────────────────────────────────

    [Fact]
    public void ScreenToTexture_NoPanZoom1_ReturnsSameCoords()
    {
        var (tx, ty) = WireframeTransform.ScreenToTexture(10f, 20f, 0f, 0f, 1f);
        Assert.Equal(10f, tx);
        Assert.Equal(20f, ty);
    }

    [Fact]
    public void ScreenToTexture_WithPan_SubtractsPanBeforeDivide()
    {
        // screen(15, 25), pan(5, 5), zoom=1 → texture(10, 20)
        var (tx, ty) = WireframeTransform.ScreenToTexture(15f, 25f, 5f, 5f, 1f);
        Assert.Equal(10f, tx);
        Assert.Equal(20f, ty);
    }

    [Fact]
    public void ScreenToTexture_WithZoom2_HalvesCoordinates()
    {
        var (tx, ty) = WireframeTransform.ScreenToTexture(20f, 40f, 0f, 0f, 2f);
        Assert.Equal(10f, tx);
        Assert.Equal(20f, ty);
    }

    [Fact]
    public void ScreenToTexture_WithZoomHalf_DoublesCoordinates()
    {
        var (tx, ty) = WireframeTransform.ScreenToTexture(10f, 20f, 0f, 0f, 0.5f);
        Assert.Equal(20f, tx);
        Assert.Equal(40f, ty);
    }

    [Fact]
    public void ScreenToTexture_WithPanAndZoom_CombinesBoth()
    {
        // screen(30, 50), pan(10, 10), zoom=2 → texture((30-10)/2, (50-10)/2) = (10, 20)
        var (tx, ty) = WireframeTransform.ScreenToTexture(30f, 50f, 10f, 10f, 2f);
        Assert.Equal(10f, tx);
        Assert.Equal(20f, ty);
    }

    // ── TextureRectToScreen ───────────────────────────────────────────────────

    [Fact]
    public void TextureRectToScreen_NoPanZoom1_ReturnsSameRect()
    {
        var (l, t, r, b) = WireframeTransform.TextureRectToScreen(
            0f, 0f, 100f, 50f, 0f, 0f, 1f);
        Assert.Equal(0f,   l);
        Assert.Equal(0f,   t);
        Assert.Equal(100f, r);
        Assert.Equal(50f,  b);
    }

    [Fact]
    public void TextureRectToScreen_WithPan_ShiftsRect()
    {
        var (l, t, r, b) = WireframeTransform.TextureRectToScreen(
            10f, 20f, 30f, 40f, 5f, 8f, 1f);
        Assert.Equal(15f, l);
        Assert.Equal(28f, t);
        Assert.Equal(35f, r);
        Assert.Equal(48f, b);
    }

    [Fact]
    public void TextureRectToScreen_WithZoom2_ScalesRect()
    {
        var (l, t, r, b) = WireframeTransform.TextureRectToScreen(
            0f, 0f, 10f, 10f, 0f, 0f, 2f);
        Assert.Equal(0f,  l);
        Assert.Equal(0f,  t);
        Assert.Equal(20f, r);
        Assert.Equal(20f, b);
    }

    [Fact]
    public void TextureRectToScreen_ZeroSizeRect_PreservesDegenerate()
    {
        var (l, t, r, b) = WireframeTransform.TextureRectToScreen(
            5f, 5f, 5f, 5f, 0f, 0f, 1f);
        Assert.Equal(5f, l);
        Assert.Equal(5f, t);
        Assert.Equal(5f, r);
        Assert.Equal(5f, b);
    }

    [Fact]
    public void TextureRectToScreen_RoundTrip_ScreenToTexture()
    {
        // Apply TextureRectToScreen then reverse with ScreenToTexture → same rect
        float lIn = 10f, tIn = 20f, rIn = 40f, bIn = 60f;
        float panX = 15f, panY = 25f, zoom = 1.5f;

        var (sl, st, sr, sb) = WireframeTransform.TextureRectToScreen(
            lIn, tIn, rIn, bIn, panX, panY, zoom);

        var (tlx, tty) = WireframeTransform.ScreenToTexture(sl, st, panX, panY, zoom);
        var (trx, tby) = WireframeTransform.ScreenToTexture(sr, sb, panX, panY, zoom);

        Assert.Equal(lIn, tlx, 4);
        Assert.Equal(tIn, tty, 4);
        Assert.Equal(rIn, trx, 4);
        Assert.Equal(bIn, tby, 4);
    }

    // ── ZoomToward ────────────────────────────────────────────────────────────

    [Fact]
    public void ZoomToward_Factor1_NoChange()
    {
        var (px, py, z) = WireframeTransform.ZoomToward(100f, 100f, 1f, 10f, 20f, 1f);
        Assert.Equal(10f, px);
        Assert.Equal(20f, py);
        Assert.Equal(1f,  z);
    }

    [Fact]
    public void ZoomToward_PivotPreservedInTextureSpace_AfterZoomIn()
    {
        // pan=(0,0), zoom=1, pivot screen=(50,50) → texture pivot = (50,50)
        // After zoom-in ×2 the same screen point should still map to texture (50,50)
        var (newPanX, newPanY, newZoom) =
            WireframeTransform.ZoomToward(50f, 50f, 2f, 0f, 0f, 1f);

        var (tx, ty) = WireframeTransform.ScreenToTexture(50f, 50f, newPanX, newPanY, newZoom);
        Assert.Equal(50f, tx, 4);
        Assert.Equal(50f, ty, 4);
    }

    [Fact]
    public void ZoomToward_PivotAtOrigin_PanRemainsZero()
    {
        // pan=(0,0), pivot screen=(0,0) → no matter the factor the pan stays (0,0)
        var (px, py, z) = WireframeTransform.ZoomToward(0f, 0f, 3f, 0f, 0f, 1f);
        Assert.Equal(0f, px);
        Assert.Equal(0f, py);
        Assert.Equal(3f, z);
    }

    [Fact]
    public void ZoomToward_ClampedAtMaxZoom()
    {
        // Start near max, zoom in with large factor
        var (_, _, z) = WireframeTransform.ZoomToward(0f, 0f, 1.5f, 0f, 0f, 30f);
        Assert.Equal(WireframeTransform.MaxZoom, z);
    }

    [Fact]
    public void ZoomToward_ClampedAtMinZoom()
    {
        // Start near min, zoom out
        var (_, _, z) = WireframeTransform.ZoomToward(0f, 0f, 0.4f, 0f, 0f, 0.1f);
        Assert.Equal(WireframeTransform.MinZoom, z);
    }

    // ── CenterFit ─────────────────────────────────────────────────────────────

    [Fact]
    public void CenterFit_SquareBitmapSquareControl_ProducesExpectedZoomAndPan()
    {
        // 100×100 in 200×200 → zoom = min(2,2)*0.85 = 1.7; pan = (200-170)/2 = 15
        var (panX, panY, zoom) = WireframeTransform.CenterFit(100f, 100f, 200f, 200f);
        Assert.Equal(1.7f, zoom,  4);
        Assert.Equal(15f,  panX, 4);
        Assert.Equal(15f,  panY, 4);
    }

    [Fact]
    public void CenterFit_WideBitmap_LimitedByWidth()
    {
        // 400×100 in 200×200 → zoom = min(0.5, 2)*0.85 = 0.425
        // panX = (200 - 400*0.425)/2 = 15, panY = (200 - 100*0.425)/2 = 78.75
        var (panX, panY, zoom) = WireframeTransform.CenterFit(400f, 100f, 200f, 200f);
        Assert.Equal(0.425f, zoom,  4);
        Assert.Equal(15f,    panX, 4);
        Assert.Equal(78.75f, panY, 4);
    }

    [Fact]
    public void CenterFit_TallBitmap_LimitedByHeight()
    {
        // 100×400 in 200×200 → zoom = min(2, 0.5)*0.85 = 0.425
        var (panX, panY, zoom) = WireframeTransform.CenterFit(100f, 400f, 200f, 200f);
        Assert.Equal(0.425f, zoom,  4);
        Assert.Equal(78.75f, panX, 4);
        Assert.Equal(15f,    panY, 4);
    }

    [Fact]
    public void CenterFit_VeryLargeControl_ZoomClampedAt4()
    {
        // 10×10 in 10000×10000 → raw zoom = 1000*0.85 = 850 → clamped to 4
        var (panX, panY, zoom) = WireframeTransform.CenterFit(10f, 10f, 10000f, 10000f);
        Assert.Equal(4f,    zoom);
        Assert.Equal(4980f, panX, 4);
        Assert.Equal(4980f, panY, 4);
    }

    [Fact]
    public void CenterFit_VerySmallControl_ZoomClampedAtMinZoom()
    {
        // 1000×1000 in 1×1 → raw zoom = (1/1000)*0.85 = 0.00085 → clamped to 0.05
        var (_, _, zoom) = WireframeTransform.CenterFit(1000f, 1000f, 1f, 1f);
        Assert.Equal(WireframeTransform.MinZoom, zoom, 4);
    }

    // ── Pan ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Pan_NoDelta_ReturnsSameAnchorPan()
    {
        var (px, py) = WireframeTransform.Pan(100f, 200f, 50f, 60f, 50f, 60f);
        Assert.Equal(100f, px);
        Assert.Equal(200f, py);
    }

    [Fact]
    public void Pan_PositiveDelta_AddsToAnchorPan()
    {
        var (px, py) = WireframeTransform.Pan(0f, 0f, 0f, 0f, 30f, 40f);
        Assert.Equal(30f, px);
        Assert.Equal(40f, py);
    }

    [Fact]
    public void Pan_NegativeDelta_SubtractsFromAnchorPan()
    {
        var (px, py) = WireframeTransform.Pan(100f, 100f, 80f, 80f, 50f, 60f);
        Assert.Equal(70f, px);  // 100 + (50-80) = 70
        Assert.Equal(80f, py);  // 100 + (60-80) = 80
    }

    [Fact]
    public void Pan_AnchorPanOffsetPreserved()
    {
        // Starting pan (50, 75), anchor at (200, 100), current at (210, 90)
        var (px, py) = WireframeTransform.Pan(50f, 75f, 200f, 100f, 210f, 90f);
        Assert.Equal(60f, px);   // 50 + (210-200) = 60
        Assert.Equal(65f, py);   // 75 + (90-100)  = 65
    }

    [Fact]
    public void Pan_IsDeltaBasedNotAccumulated()
    {
        // Two successive moves from the same anchor should produce independent results
        var (px1, py1) = WireframeTransform.Pan(0f, 0f, 100f, 100f, 110f, 120f);
        var (px2, py2) = WireframeTransform.Pan(0f, 0f, 100f, 100f, 150f, 160f);
        Assert.True(px2 > px1);
        Assert.True(py2 > py1);
    }
}
