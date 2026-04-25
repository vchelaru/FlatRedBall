using AnimationEditor.Core.Rendering;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class AdjustOffsetCalculatorTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    static AnimationFrameSave MakeFrame(
        float top, float bottom, float relX = 0f, float relY = 0f)
        => new AnimationFrameSave
        {
            LeftCoordinate   = 0f,
            RightCoordinate  = 1f,
            TopCoordinate    = top,
            BottomCoordinate = bottom,
            RelativeX        = relX,
            RelativeY        = relY,
            ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave()
        };

    // ── ApplyJustifyBottom ────────────────────────────────────────────────

    [Fact]
    public void JustifyBottom_FullHeightFrame_SetsRelativeYToHalfTextureHeight()
    {
        // Frame covers full texture height (top=0, bottom=1) on a 64-px tall texture.
        // Expected: RelativeY = (64 * 1) / 2 / 1 = 32
        var frame = MakeFrame(top: 0f, bottom: 1f);
        AdjustOffsetCalculator.ApplyJustifyBottom([frame], textureHeightPixels: 64f);
        Assert.Equal(32f, frame.RelativeY, precision: 4);
    }

    [Fact]
    public void JustifyBottom_HalfHeightFrame_SetsRelativeYToQuarterTextureHeight()
    {
        // Frame covers top half (top=0, bottom=0.5) on 64-px texture.
        // spriteHeight = 64 * 0.5 = 32 → RelativeY = 32 / 2 = 16
        var frame = MakeFrame(top: 0f, bottom: 0.5f);
        AdjustOffsetCalculator.ApplyJustifyBottom([frame], textureHeightPixels: 64f);
        Assert.Equal(16f, frame.RelativeY, precision: 4);
    }

    [Fact]
    public void JustifyBottom_WithOffsetMultiplierTwo_HalvesRelativeY()
    {
        // Full height frame, texture=64, multiplier=2 → RelativeY = 32/2 = 16
        var frame = MakeFrame(top: 0f, bottom: 1f);
        AdjustOffsetCalculator.ApplyJustifyBottom([frame], textureHeightPixels: 64f, offsetMultiplier: 2f);
        Assert.Equal(16f, frame.RelativeY, precision: 4);
    }

    [Fact]
    public void JustifyBottom_ZeroMultiplier_TreatedAsOne()
    {
        // Guard: multiplier=0 must not divide-by-zero; falls back to 1
        var frame = MakeFrame(top: 0f, bottom: 1f);
        AdjustOffsetCalculator.ApplyJustifyBottom([frame], textureHeightPixels: 64f, offsetMultiplier: 0f);
        Assert.Equal(32f, frame.RelativeY, precision: 4);
    }

    [Fact]
    public void JustifyBottom_MultipleFrames_EachCalculatedIndependently()
    {
        var f1 = MakeFrame(top: 0f, bottom: 1f);   // full height on 64px → 32
        var f2 = MakeFrame(top: 0f, bottom: 0.25f); // 1/4 height on 64px → 8
        AdjustOffsetCalculator.ApplyJustifyBottom([f1, f2], textureHeightPixels: 64f);
        Assert.Equal(32f, f1.RelativeY, precision: 4);
        Assert.Equal(8f,  f2.RelativeY, precision: 4);
    }

    [Fact]
    public void JustifyBottom_DoesNotChangeRelativeX()
    {
        var frame = MakeFrame(top: 0f, bottom: 1f, relX: 10f);
        AdjustOffsetCalculator.ApplyJustifyBottom([frame], textureHeightPixels: 64f);
        Assert.Equal(10f, frame.RelativeX, precision: 4);
    }

    [Fact]
    public void JustifyBottom_EmptyList_NoException()
        => AdjustOffsetCalculator.ApplyJustifyBottom([], textureHeightPixels: 64f); // just should not throw

    // ── ApplyAdjustAll — Relative mode ────────────────────────────────────

    [Fact]
    public void AdjustAll_RelativeMode_AddsToExistingValues()
    {
        var frame = MakeFrame(top: 0f, bottom: 1f, relX: 5f, relY: 10f);
        AdjustOffsetCalculator.ApplyAdjustAll([frame], deltaX: 3f, deltaY: -4f, relative: true);
        Assert.Equal(8f,  frame.RelativeX, precision: 4);
        Assert.Equal(6f,  frame.RelativeY, precision: 4);
    }

    [Fact]
    public void AdjustAll_RelativeMode_NullDeltaX_LeavesXUnchanged()
    {
        var frame = MakeFrame(top: 0f, bottom: 1f, relX: 5f, relY: 10f);
        AdjustOffsetCalculator.ApplyAdjustAll([frame], deltaX: null, deltaY: 2f, relative: true);
        Assert.Equal(5f,  frame.RelativeX, precision: 4);
        Assert.Equal(12f, frame.RelativeY, precision: 4);
    }

    [Fact]
    public void AdjustAll_RelativeMode_NullDeltaY_LeavesYUnchanged()
    {
        var frame = MakeFrame(top: 0f, bottom: 1f, relX: 5f, relY: 10f);
        AdjustOffsetCalculator.ApplyAdjustAll([frame], deltaX: 1f, deltaY: null, relative: true);
        Assert.Equal(6f,  frame.RelativeX, precision: 4);
        Assert.Equal(10f, frame.RelativeY, precision: 4);
    }

    // ── ApplyAdjustAll — Absolute mode ────────────────────────────────────

    [Fact]
    public void AdjustAll_AbsoluteMode_OverwritesExistingValues()
    {
        var frame = MakeFrame(top: 0f, bottom: 1f, relX: 99f, relY: 99f);
        AdjustOffsetCalculator.ApplyAdjustAll([frame], deltaX: 5f, deltaY: 7f, relative: false);
        Assert.Equal(5f, frame.RelativeX, precision: 4);
        Assert.Equal(7f, frame.RelativeY, precision: 4);
    }

    [Fact]
    public void AdjustAll_AbsoluteMode_NullX_LeavesXUnchanged()
    {
        var frame = MakeFrame(top: 0f, bottom: 1f, relX: 99f, relY: 0f);
        AdjustOffsetCalculator.ApplyAdjustAll([frame], deltaX: null, deltaY: 3f, relative: false);
        Assert.Equal(99f, frame.RelativeX, precision: 4);
        Assert.Equal(3f,  frame.RelativeY, precision: 4);
    }

    [Fact]
    public void AdjustAll_AbsoluteMode_AppliesToAllFrames()
    {
        var frames = new[]
        {
            MakeFrame(top: 0f, bottom: 1f, relX: 1f, relY: 2f),
            MakeFrame(top: 0f, bottom: 1f, relX: 3f, relY: 4f),
            MakeFrame(top: 0f, bottom: 1f, relX: 5f, relY: 6f),
        };
        AdjustOffsetCalculator.ApplyAdjustAll(frames, deltaX: 0f, deltaY: 0f, relative: false);
        foreach (var f in frames)
        {
            Assert.Equal(0f, f.RelativeX, precision: 4);
            Assert.Equal(0f, f.RelativeY, precision: 4);
        }
    }

    [Fact]
    public void AdjustAll_EmptyList_NoException()
        => AdjustOffsetCalculator.ApplyAdjustAll([], deltaX: 1f, deltaY: 1f, relative: true);
}
