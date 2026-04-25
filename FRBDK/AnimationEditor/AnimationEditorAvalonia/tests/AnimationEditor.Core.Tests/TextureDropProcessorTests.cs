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

    // ── TD06: relative path computation ──────────────────────────────────────

    [Fact]
    public void ApplyPngDrop_SameDirectory_ReturnsFileNameOnly()
    {
        var chain = new AnimationChainSave();
        chain.Frames.Add(new AnimationFrameSave());

        TextureDropProcessor.ApplyPngDrop(
            chain,
            chain.Frames[0],
            @"C:\Project\Tex.png",
            @"C:\Project\Player.achx",
            createFrameOnCtrl: false);

        // Same folder → just the filename, no directory prefix
        Assert.Equal("Tex.png", chain.Frames[0].TextureName);
    }

    [Fact]
    public void ApplyPngDrop_Subdirectory_ReturnsRelativeSubPath()
    {
        var chain = new AnimationChainSave();
        chain.Frames.Add(new AnimationFrameSave());

        TextureDropProcessor.ApplyPngDrop(
            chain,
            chain.Frames[0],
            @"C:\Project\Content\Sprites\Hero.png",
            @"C:\Project\Animations\Player.achx",
            createFrameOnCtrl: false);

        // FRB FileManager.MakeRelative normalises to forward slashes
        Assert.Equal("../Content/Sprites/Hero.png", chain.Frames[0].TextureName);
    }

    [Fact]
    public void ApplyPngDrop_RelativePathUsesForwardSlashes()
    {
        var chain = new AnimationChainSave();
        chain.Frames.Add(new AnimationFrameSave());

        TextureDropProcessor.ApplyPngDrop(
            chain,
            chain.Frames[0],
            @"C:\Project\Content\NewTex.png",
            @"C:\Project\Animations\Player.achx",
            createFrameOnCtrl: false);

        var tex = chain.Frames[0].TextureName;
        Assert.DoesNotContain(@"\", tex);
        Assert.Contains("/", tex);
    }

    // ── Unsaved project (achxFileName == null) ────────────────────────────────
    // Root cause: these all previously returned NotApplied, silently breaking
    // drag-drop on projects that hadn't been saved yet.

    [Fact]
    public void ApplyPngDrop_NullAchx_EmptyChain_CreatesFrame()
    {
        var chain = new AnimationChainSave();

        var result = TextureDropProcessor.ApplyPngDrop(chain, null,
            @"D:\Downloads\sprite.png", null, false);

        Assert.Equal(TextureDropResult.CreatedFrame, result);
    }

    [Fact]
    public void ApplyPngDrop_NullAchx_EmptyChain_UsesAbsolutePath()
    {
        var chain = new AnimationChainSave();
        TextureDropProcessor.ApplyPngDrop(chain, null,
            @"D:\Downloads\sprite.png", null, false);

        Assert.Equal(@"D:\Downloads\sprite.png", chain.Frames[0].TextureName);
    }

    [Fact]
    public void ApplyPngDrop_NullAchx_ChainWithFrames_UpdatesAllFrames()
    {
        var chain = new AnimationChainSave();
        chain.Frames.Add(new AnimationFrameSave { TextureName = "old.png" });

        var result = TextureDropProcessor.ApplyPngDrop(chain, null,
            @"D:\Downloads\sprite.png", null, false);

        Assert.Equal(TextureDropResult.UpdatedChainFrames, result);
        Assert.Equal(@"D:\Downloads\sprite.png", chain.Frames[0].TextureName);
    }

    [Fact]
    public void ApplyPngDrop_NullAchx_TargetFrame_UpdatesThatFrame()
    {
        var frame = new AnimationFrameSave();

        var result = TextureDropProcessor.ApplyPngDrop(null, frame,
            @"D:\Downloads\sprite.png", null, false);

        Assert.Equal(TextureDropResult.UpdatedFrame, result);
        Assert.Equal(@"D:\Downloads\sprite.png", frame.TextureName);
    }

    [Fact]
    public void ApplyPngDrop_NullAchx_NonPng_IsIgnored()
    {
        var chain = new AnimationChainSave();

        var result = TextureDropProcessor.ApplyPngDrop(chain, null,
            @"D:\Downloads\sprite.jpg", null, false);

        Assert.Equal(TextureDropResult.NotApplied, result);
    }

    [Fact]
    public void ApplyPngDrop_NullAchx_NullChainAndFrame_IsIgnored()
    {
        var result = TextureDropProcessor.ApplyPngDrop(null, null,
            @"D:\Downloads\sprite.png", null, false);

        Assert.Equal(TextureDropResult.NotApplied, result);
    }
}
