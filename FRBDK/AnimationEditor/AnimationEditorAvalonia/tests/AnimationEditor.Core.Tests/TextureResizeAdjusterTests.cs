using AnimationEditor.Core.IO;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class TextureResizeAdjusterTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    static AnimationFrameSave MakeFrame(
        float left, float right, float top, float bottom,
        string textureName = "Tex.png")
        => new AnimationFrameSave
        {
            TextureName      = textureName,
            LeftCoordinate   = left,
            RightCoordinate  = right,
            TopCoordinate    = top,
            BottomCoordinate = bottom,
            ShapeCollectionSave = new ShapeCollectionSave()
        };

    // ── AdjustFrame ───────────────────────────────────────────────────────

    [Fact]
    public void AdjustFrame_NoChange_WhenDimensionsIdentical()
    {
        var frame = MakeFrame(0.25f, 0.75f, 0f, 0.5f);
        TextureResizeAdjuster.AdjustFrame(frame, 64, 64, 64, 64);
        Assert.Equal(0.25f, frame.LeftCoordinate,   precision: 4);
        Assert.Equal(0.75f, frame.RightCoordinate,  precision: 4);
        Assert.Equal(0f,    frame.TopCoordinate,    precision: 4);
        Assert.Equal(0.5f,  frame.BottomCoordinate, precision: 4);
    }

    [Fact]
    public void AdjustFrame_DoubleWidth_HalvesHorizontalCoordinates()
    {
        // Pixel region: left=16, right=48 on old 64px texture
        // New texture: 128px wide  →  left=16/128, right=48/128
        var frame = MakeFrame(16f / 64f, 48f / 64f, 0f, 1f);
        TextureResizeAdjuster.AdjustFrame(frame, oldWidth: 64, oldHeight: 64, newWidth: 128, newHeight: 64);
        Assert.Equal(16f / 128f, frame.LeftCoordinate,  precision: 4);
        Assert.Equal(48f / 128f, frame.RightCoordinate, precision: 4);
        // Vertical unchanged
        Assert.Equal(0f, frame.TopCoordinate,    precision: 4);
        Assert.Equal(1f, frame.BottomCoordinate, precision: 4);
    }

    [Fact]
    public void AdjustFrame_DoubleHeight_HalvesVerticalCoordinates()
    {
        var frame = MakeFrame(0f, 1f, 8f / 32f, 24f / 32f);
        TextureResizeAdjuster.AdjustFrame(frame, oldWidth: 32, oldHeight: 32, newWidth: 32, newHeight: 64);
        Assert.Equal(0f,        frame.LeftCoordinate,   precision: 4);
        Assert.Equal(1f,        frame.RightCoordinate,  precision: 4);
        Assert.Equal(8f / 64f,  frame.TopCoordinate,    precision: 4);
        Assert.Equal(24f / 64f, frame.BottomCoordinate, precision: 4);
    }

    [Fact]
    public void AdjustFrame_PadFromOdd_PreservesPixelCounts()
    {
        // Old texture 100px wide, right coordinate = 0.5 → pixel = 50
        // New texture 128px (next power-of-two) → new coord = 50/128
        var frame = MakeFrame(0f, 0.5f, 0f, 1f);
        TextureResizeAdjuster.AdjustFrame(frame, oldWidth: 100, oldHeight: 1, newWidth: 128, newHeight: 1);
        Assert.Equal(0f,         frame.LeftCoordinate,  precision: 4);
        Assert.Equal(50f / 128f, frame.RightCoordinate, precision: 4);
    }

    [Fact]
    public void AdjustFrame_FullTextureCoverage_PreservesPixelBoundary()
    {
        // A frame that covers the full 64×64 texture has right=1.0 → pixel=64.
        // After padding to 128×128, that same pixel edge is now at 64/128 = 0.5.
        // UV coverage shrinks; the pixel content does NOT stretch.
        var frame = MakeFrame(0f, 1f, 0f, 1f);
        TextureResizeAdjuster.AdjustFrame(frame, 64, 64, 128, 128);
        Assert.Equal(0f,   frame.LeftCoordinate,   precision: 4);
        Assert.Equal(0.5f, frame.RightCoordinate,  precision: 4);
        Assert.Equal(0f,   frame.TopCoordinate,    precision: 4);
        Assert.Equal(0.5f, frame.BottomCoordinate, precision: 4);
    }

    // ── AdjustAll ─────────────────────────────────────────────────────────

    static AnimationChainListSave MakeAcls(params AnimationFrameSave[] frames)
    {
        var chain = new AnimationChainSave { Name = "Chain" };
        foreach (var f in frames) chain.Frames.Add(f);
        var acls = new AnimationChainListSave();
        acls.AnimationChains.Add(chain);
        return acls;
    }

    [Fact]
    public void AdjustAll_OnlyAffectsMatchingTexture()
    {
        var frameA = MakeFrame(0f, 0.5f, 0f, 1f, "hero.png");
        var frameB = MakeFrame(0f, 1f,   0f, 1f, "other.png");
        var acls   = MakeAcls(frameA, frameB);

        TextureResizeAdjuster.AdjustAll(acls,
            aclsDirectory: "c:/content/",
            absoluteTextureFileName: "c:/content/hero.png",
            oldWidth: 64, oldHeight: 64, newWidth: 128, newHeight: 64);

        // frameA's right coordinate should be halved (pixel 32 / 128)
        Assert.Equal(0.25f, frameA.RightCoordinate, precision: 4);
        // frameB unchanged
        Assert.Equal(1f,    frameB.RightCoordinate, precision: 4);
    }

    [Fact]
    public void AdjustAll_ReturnsListOfModifiedFrames()
    {
        var frameA = MakeFrame(0f, 0.5f, 0f, 1f, "hero.png");
        var frameB = MakeFrame(0f, 1f,   0f, 1f, "other.png");
        var acls   = MakeAcls(frameA, frameB);

        var modified = TextureResizeAdjuster.AdjustAll(acls,
            "c:/content/", "c:/content/hero.png",
            64, 64, 128, 64);

        Assert.Single(modified);
        Assert.Same(frameA, modified[0]);
    }

    [Fact]
    public void AdjustAll_CaseInsensitivePathMatch()
    {
        var frame = MakeFrame(0f, 0.5f, 0f, 1f, "Hero.png");
        var acls  = MakeAcls(frame);

        // Provide the absolute path in a different case
        TextureResizeAdjuster.AdjustAll(acls,
            "C:/Content/", "c:/content/hero.png",
            64, 64, 128, 64);

        Assert.Equal(0.25f, frame.RightCoordinate, precision: 4);
    }

    [Fact]
    public void AdjustAll_ForwardAndBackslashEquivalent()
    {
        var frame = MakeFrame(0f, 0.5f, 0f, 1f, "hero.png");
        var acls  = MakeAcls(frame);

        // Mix of forward and backslash
        TextureResizeAdjuster.AdjustAll(acls,
            "c:\\content\\", "c:/content/hero.png",
            64, 64, 128, 64);

        Assert.Equal(0.25f, frame.RightCoordinate, precision: 4);
    }

    [Fact]
    public void AdjustAll_EmptyAcls_ReturnsEmptyList()
    {
        var acls = new AnimationChainListSave();
        var result = TextureResizeAdjuster.AdjustAll(acls,
            "", "c:/tex.png", 64, 64, 128, 64);
        Assert.Empty(result);
    }
}
