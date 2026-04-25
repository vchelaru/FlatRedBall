using AnimationEditor.Core.Rendering;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class BatchFrameBuilderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    static AnimationFrameSave MakeLastFrame(
        float left, float right, float top, float bottom,
        string texture = "Tex.png", float frameLength = 0.1f)
        => new AnimationFrameSave
        {
            TextureName      = texture,
            FrameLength      = frameLength,
            LeftCoordinate   = left,
            RightCoordinate  = right,
            TopCoordinate    = top,
            BottomCoordinate = bottom,
            ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave()
        };

    // ── Count / no-op ─────────────────────────────────────────────────────

    [Fact]
    public void ZeroCount_ReturnsEmpty()
    {
        var result = BatchFrameBuilder.BuildBatch(null, 0, false);
        Assert.Empty(result.Frames);
    }

    [Fact]
    public void Count_Returns_CorrectNumberOfFrames()
    {
        var result = BatchFrameBuilder.BuildBatch(null, 5, false);
        Assert.Equal(5, result.Frames.Count);
    }

    // ── No lastFrame (null) ───────────────────────────────────────────────

    [Fact]
    public void NoLastFrame_EachFrameHasDefaultUV()
    {
        var result = BatchFrameBuilder.BuildBatch(null, 3, false);
        foreach (var f in result.Frames)
        {
            Assert.Equal(0f, f.LeftCoordinate,   precision: 5);
            Assert.Equal(1f, f.RightCoordinate,  precision: 5);
            Assert.Equal(0f, f.TopCoordinate,    precision: 5);
            Assert.Equal(1f, f.BottomCoordinate, precision: 5);
            Assert.Equal(0.1f, f.FrameLength,    precision: 5);
        }
    }

    // ── Without increment (copy UV) ───────────────────────────────────────

    [Fact]
    public void WithoutIncrement_AllFramesCopyLastFrameUV()
    {
        var last = MakeLastFrame(0.25f, 0.5f, 0f, 0.5f);
        var result = BatchFrameBuilder.BuildBatch(last, 3, incrementUV: false);

        foreach (var f in result.Frames)
        {
            Assert.Equal(0.25f, f.LeftCoordinate,   precision: 5);
            Assert.Equal(0.5f,  f.RightCoordinate,  precision: 5);
            Assert.Equal(0f,    f.TopCoordinate,    precision: 5);
            Assert.Equal(0.5f,  f.BottomCoordinate, precision: 5);
        }
    }

    [Fact]
    public void WithoutIncrement_CopiesTextureAndFrameLength()
    {
        var last = MakeLastFrame(0f, 1f, 0f, 1f, texture: "Hero.png", frameLength: 0.25f);
        var result = BatchFrameBuilder.BuildBatch(last, 2, incrementUV: false);
        foreach (var f in result.Frames)
        {
            Assert.Equal("Hero.png", f.TextureName);
            Assert.Equal(0.25f, f.FrameLength, precision: 5);
        }
    }

    // ── With increment — horizontal traversal ────────────────────────────

    [Fact]
    public void WithIncrement_4x4Sheet_AdvancesRightThenWraps()
    {
        // 4×4 sprite sheet: each cell is 0.25 wide and 0.25 tall
        // Last frame is cell (0,0): L=0, R=0.25, T=0, B=0.25
        // Adding 3 more: cell(1,0), cell(2,0), cell(3,0)
        var last = MakeLastFrame(0f, 0.25f, 0f, 0.25f);
        var result = BatchFrameBuilder.BuildBatch(last, 3, incrementUV: true);

        Assert.Equal(3, result.Frames.Count);

        Assert.Equal(0.25f, result.Frames[0].LeftCoordinate,  precision: 4);
        Assert.Equal(0.50f, result.Frames[0].RightCoordinate, precision: 4);
        Assert.Equal(0f,    result.Frames[0].TopCoordinate,   precision: 4);

        Assert.Equal(0.50f, result.Frames[1].LeftCoordinate,  precision: 4);
        Assert.Equal(0.75f, result.Frames[1].RightCoordinate, precision: 4);
        Assert.Equal(0f,    result.Frames[1].TopCoordinate,   precision: 4);

        Assert.Equal(0.75f, result.Frames[2].LeftCoordinate,  precision: 4);
        Assert.Equal(1.00f, result.Frames[2].RightCoordinate, precision: 4);
        Assert.Equal(0f,    result.Frames[2].TopCoordinate,   precision: 4);
    }

    [Fact]
    public void WithIncrement_WrapsToNextRow_WhenEndOfRowReached()
    {
        // 4×4 sheet; last frame is cell (3,0): L=0.75, R=1.0, T=0, B=0.25
        // Next frame should be cell (0,1): L=0, R=0.25, T=0.25, B=0.50
        var last = MakeLastFrame(0.75f, 1.0f, 0f, 0.25f);
        var result = BatchFrameBuilder.BuildBatch(last, 1, incrementUV: true);

        Assert.Single(result.Frames);
        Assert.Equal(0f,    result.Frames[0].LeftCoordinate,   precision: 4);
        Assert.Equal(0.25f, result.Frames[0].RightCoordinate,  precision: 4);
        Assert.Equal(0.25f, result.Frames[0].TopCoordinate,    precision: 4);
        Assert.Equal(0.50f, result.Frames[0].BottomCoordinate, precision: 4);
    }

    [Fact]
    public void WithIncrement_BeyondBottomOfTexture_SetsExceededTextureBounds()
    {
        // 2×2 sheet; last frame is bottom-right cell (1,1): L=0.5, R=1, T=0.5, B=1
        // One more frame would go beyond the texture (B > 1)
        var last = MakeLastFrame(0.5f, 1.0f, 0.5f, 1.0f);
        var result = BatchFrameBuilder.BuildBatch(last, 1, incrementUV: true);
        Assert.True(result.ExceededTextureBounds);
    }

    [Fact]
    public void WithIncrement_StillCreatesFramesEvenWhenExceedingBounds()
    {
        var last = MakeLastFrame(0.5f, 1.0f, 0.5f, 1.0f);
        var result = BatchFrameBuilder.BuildBatch(last, 3, incrementUV: true);
        Assert.Equal(3, result.Frames.Count); // frames created, warning is caller's job
    }

    [Fact]
    public void WithIncrement_WithinBounds_DoesNotSetExceededFlag()
    {
        // 4×4 sheet; first cell, adding 1 more — still within bounds
        var last = MakeLastFrame(0f, 0.25f, 0f, 0.25f);
        var result = BatchFrameBuilder.BuildBatch(last, 1, incrementUV: true);
        Assert.False(result.ExceededTextureBounds);
    }

    // ── ShapeCollectionSave initialised ──────────────────────────────────

    [Fact]
    public void AllFrames_HaveInitialisedShapeCollectionSave()
    {
        var result = BatchFrameBuilder.BuildBatch(null, 4, incrementUV: false);
        Assert.All(result.Frames, f => Assert.NotNull(f.ShapeCollectionSave));
    }
}
