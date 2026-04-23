using AnimationEditor.Core.DragDrop;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class TextureDropProcessorTests
{
    [Fact]
    public void ApplyPngDrop_OnFrame_AssignsOnlyThatFrameTexture()
    {
        var chain = new AnimationChainSave();
        var frameA = new AnimationFrameSave { TextureName = "oldA.png" };
        var frameB = new AnimationFrameSave { TextureName = "oldB.png" };
        chain.Frames.Add(frameA);
        chain.Frames.Add(frameB);

        var result = TextureDropProcessor.ApplyPngDrop(
            chain,
            frameA,
            @"C:\Project\Content\NewTex.png",
            @"C:\Project\Animations\Player.achx",
            createFrameOnCtrl: false);

        Assert.Equal(TextureDropResult.UpdatedFrame, result);
        Assert.Equal("../Content/NewTex.png", frameA.TextureName);
        Assert.Equal("oldB.png", frameB.TextureName);
    }

    [Fact]
    public void ApplyPngDrop_OnChainWithoutCtrl_AssignsAllFrames()
    {
        var chain = new AnimationChainSave();
        chain.Frames.Add(new AnimationFrameSave { TextureName = "oldA.png" });
        chain.Frames.Add(new AnimationFrameSave { TextureName = "oldB.png" });

        var result = TextureDropProcessor.ApplyPngDrop(
            chain,
            null,
            @"C:\Project\Content\Shared.png",
            @"C:\Project\Animations\Player.achx",
            createFrameOnCtrl: false);

        Assert.Equal(TextureDropResult.UpdatedChainFrames, result);
        Assert.All(chain.Frames, frame => Assert.Equal("../Content/Shared.png", frame.TextureName));
    }

    [Fact]
    public void ApplyPngDrop_OnChainWithCtrl_CreatesNewFrame()
    {
        var chain = new AnimationChainSave();
        chain.Frames.Add(new AnimationFrameSave { TextureName = "oldA.png" });

        var result = TextureDropProcessor.ApplyPngDrop(
            chain,
            null,
            @"C:\Project\Content\NewFrameTex.png",
            @"C:\Project\Animations\Player.achx",
            createFrameOnCtrl: true);

        Assert.Equal(TextureDropResult.CreatedFrame, result);
        Assert.Equal(2, chain.Frames.Count);
        Assert.Equal("../Content/NewFrameTex.png", chain.Frames.Last().TextureName);
    }

    [Fact]
    public void ApplyPngDrop_OnEmptyChainWithoutCtrl_CreatesNewFrame()
    {
        var chain = new AnimationChainSave();

        var result = TextureDropProcessor.ApplyPngDrop(
            chain,
            null,
            @"C:\Project\Content\FirstFrameTex.png",
            @"C:\Project\Animations\Player.achx",
            createFrameOnCtrl: false);

        Assert.Equal(TextureDropResult.CreatedFrame, result);
        Assert.Single(chain.Frames);
        Assert.Equal("../Content/FirstFrameTex.png", chain.Frames[0].TextureName);
    }

    [Fact]
    public void ApplyPngDrop_NonPng_IsIgnored()
    {
        var chain = new AnimationChainSave();
        chain.Frames.Add(new AnimationFrameSave { TextureName = "oldA.png" });

        var result = TextureDropProcessor.ApplyPngDrop(
            chain,
            null,
            @"C:\Project\Content\NotTexture.jpg",
            @"C:\Project\Animations\Player.achx",
            createFrameOnCtrl: false);

        Assert.Equal(TextureDropResult.NotApplied, result);
        Assert.Single(chain.Frames);
        Assert.Equal("oldA.png", chain.Frames[0].TextureName);
    }
}
