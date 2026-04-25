using AnimationEditor.Core.Rendering;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class PixelFrameEditorTests
{
    // ── Helper ───────────────────────────────────────────────────────────────

    static AnimationFrameSave MakeFrame(float left, float right, float top, float bottom)
        => new AnimationFrameSave
        {
            LeftCoordinate   = left,
            RightCoordinate  = right,
            TopCoordinate    = top,
            BottomCoordinate = bottom
        };

    // ── SetX ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SetX_MovesLeftAndRightByDelta_PreservesWidth()
    {
        // Frame starts at pixel 16–48 on a 256px texture  (width = 32px)
        var frame = MakeFrame(16f / 256f, 48f / 256f, 0f, 1f);

        PixelFrameEditor.SetX(frame, 32, 256);   // move to pixel 32

        Assert.Equal(32f / 256f, frame.LeftCoordinate,  precision: 4);
        Assert.Equal(64f / 256f, frame.RightCoordinate, precision: 4);
    }

    [Fact]
    public void SetX_SamePosition_NoChange()
    {
        var frame = MakeFrame(32f / 256f, 64f / 256f, 0f, 1f);

        PixelFrameEditor.SetX(frame, 32, 256);

        Assert.Equal(32f / 256f, frame.LeftCoordinate,  precision: 4);
        Assert.Equal(64f / 256f, frame.RightCoordinate, precision: 4);
    }

    [Fact]
    public void SetX_MoveToZero_BothCoordsShiftLeft()
    {
        var frame = MakeFrame(64f / 256f, 96f / 256f, 0f, 1f);

        PixelFrameEditor.SetX(frame, 0, 256);

        Assert.Equal(0f,        frame.LeftCoordinate,  precision: 4);
        Assert.Equal(32f / 256f, frame.RightCoordinate, precision: 4);
    }

    [Fact]
    public void SetX_WidthIsPreservedAfterMove()
    {
        var frame = MakeFrame(10f / 100f, 40f / 100f, 0f, 1f);
        float originalWidth = frame.RightCoordinate - frame.LeftCoordinate;

        PixelFrameEditor.SetX(frame, 20, 100);

        float newWidth = frame.RightCoordinate - frame.LeftCoordinate;
        Assert.Equal(originalWidth, newWidth, precision: 4);
    }

    // ── SetY ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SetY_MovesTopAndBottomByDelta_PreservesHeight()
    {
        // Frame starts at row 0–32 on a 128px texture
        var frame = MakeFrame(0f, 1f, 0f / 128f, 32f / 128f);

        PixelFrameEditor.SetY(frame, 64, 128);

        Assert.Equal(64f / 128f, frame.TopCoordinate,    precision: 4);
        Assert.Equal(96f / 128f, frame.BottomCoordinate, precision: 4);
    }

    [Fact]
    public void SetY_HeightPreservedAfterMove()
    {
        var frame = MakeFrame(0f, 1f, 16f / 128f, 48f / 128f);
        float originalH = frame.BottomCoordinate - frame.TopCoordinate;

        PixelFrameEditor.SetY(frame, 0, 128);

        float newH = frame.BottomCoordinate - frame.TopCoordinate;
        Assert.Equal(originalH, newH, precision: 4);
    }

    // ── SetWidth ──────────────────────────────────────────────────────────────

    [Fact]
    public void SetWidth_AdjustsRightCoord_KeepsLeftFixed()
    {
        var frame = MakeFrame(32f / 256f, 64f / 256f, 0f, 1f);

        PixelFrameEditor.SetWidth(frame, 48, 256);

        Assert.Equal(32f / 256f, frame.LeftCoordinate,  precision: 4);  // unchanged
        Assert.Equal(80f / 256f, frame.RightCoordinate, precision: 4);  // 32 + 48 = 80
    }

    [Fact]
    public void SetWidth_FullTextureWidth_RightIsOne()
    {
        var frame = MakeFrame(0f, 0.5f, 0f, 1f);

        PixelFrameEditor.SetWidth(frame, 128, 128);

        Assert.Equal(0f, frame.LeftCoordinate,  precision: 4);
        Assert.Equal(1f, frame.RightCoordinate, precision: 4);
    }

    [Fact]
    public void SetWidth_SinglePixel_RightIsLeftPlusTiny()
    {
        var frame = MakeFrame(0f, 0.5f, 0f, 1f);

        PixelFrameEditor.SetWidth(frame, 1, 256);

        Assert.Equal(1f / 256f, frame.RightCoordinate, precision: 4);
    }

    // ── SetHeight ─────────────────────────────────────────────────────────────

    [Fact]
    public void SetHeight_AdjustsBottomCoord_KeepsTopFixed()
    {
        var frame = MakeFrame(0f, 1f, 16f / 128f, 48f / 128f);

        PixelFrameEditor.SetHeight(frame, 64, 128);

        Assert.Equal(16f / 128f, frame.TopCoordinate,    precision: 4);  // unchanged
        Assert.Equal(80f / 128f, frame.BottomCoordinate, precision: 4);  // 16 + 64 = 80
    }

    [Fact]
    public void SetHeight_FullTextureHeight_BottomIsOne()
    {
        var frame = MakeFrame(0f, 1f, 0f, 0.5f);

        PixelFrameEditor.SetHeight(frame, 256, 256);

        Assert.Equal(0f, frame.TopCoordinate,    precision: 4);
        Assert.Equal(1f, frame.BottomCoordinate, precision: 4);
    }

    // ── Round helper ──────────────────────────────────────────────────────────

    [Fact]
    public void Round_SnapsToNearestPixelBoundary()
    {
        // 0.1256 * 256 = 32.1536 → rounds to 32 → 32/256
        float coord = 0.1256f;
        float result = PixelFrameEditor.Round(coord, 256);
        Assert.Equal(32f / 256f, result, precision: 4);
    }

    // ── Guard conditions ──────────────────────────────────────────────────────

    [Fact]
    public void SetX_NullFrame_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PixelFrameEditor.SetX(null!, 0, 256));
    }

    [Fact]
    public void SetWidth_ZeroTextureWidth_Throws()
    {
        var frame = MakeFrame(0f, 1f, 0f, 1f);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PixelFrameEditor.SetWidth(frame, 32, 0));
    }

    [Fact]
    public void SetHeight_ZeroTextureHeight_Throws()
    {
        var frame = MakeFrame(0f, 1f, 0f, 1f);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PixelFrameEditor.SetHeight(frame, 32, 0));
    }
}
