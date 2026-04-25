using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class AppCommandsFrameFromPixelBoundsTests
{
    public AppCommandsFrameFromPixelBoundsTests()
    {
        TestHelpers.SetupFreshAcls();
    }

    private static AnimationChainSave MakeChain(string name = "Chain")
    {
        var chain = new AnimationChainSave { Name = name };
        ProjectManager.Self.AnimationChainListSave.AnimationChains.Add(chain);
        return chain;
    }

    // ── Frame content ─────────────────────────────────────────────────────

    [Fact]
    public void AddsFrameToChain()
    {
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "tex.png", 0, 0, 64, 64, 128, 128);
        Assert.Single(chain.Frames);
    }

    [Fact]
    public void Frame_HasCorrectTextureName()
    {
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "sprites/hero.png", 0, 0, 32, 32, 64, 64);
        Assert.Equal("sprites/hero.png", chain.Frames[0].TextureName);
    }

    [Fact]
    public void Frame_LeftCoordinate_IsPixelOverWidth()
    {
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "t.png", 32, 0, 96, 64, 128, 128);
        Assert.Equal(32f / 128f, chain.Frames[0].LeftCoordinate, precision: 5);
    }

    [Fact]
    public void Frame_RightCoordinate_IsMaxXOverWidth()
    {
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "t.png", 0, 0, 96, 64, 128, 128);
        Assert.Equal(96f / 128f, chain.Frames[0].RightCoordinate, precision: 5);
    }

    [Fact]
    public void Frame_TopCoordinate_IsMinYOverHeight()
    {
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "t.png", 0, 16, 64, 80, 128, 128);
        Assert.Equal(16f / 128f, chain.Frames[0].TopCoordinate, precision: 5);
    }

    [Fact]
    public void Frame_BottomCoordinate_IsMaxYOverHeight()
    {
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "t.png", 0, 0, 64, 80, 128, 128);
        Assert.Equal(80f / 128f, chain.Frames[0].BottomCoordinate, precision: 5);
    }

    [Fact]
    public void Frame_FrameLength_IsDefaultPointOne()
    {
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "t.png", 0, 0, 64, 64, 128, 128);
        Assert.Equal(0.1f, chain.Frames[0].FrameLength, precision: 5);
    }

    [Fact]
    public void Frame_ShapeCollectionSave_IsInitialised()
    {
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "t.png", 0, 0, 64, 64, 128, 128);
        Assert.NotNull(chain.Frames[0].ShapeCollectionSave);
    }

    // ── Selection ─────────────────────────────────────────────────────────

    [Fact]
    public void NewFrame_IsSelected()
    {
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "t.png", 0, 0, 64, 64, 128, 128);
        Assert.Same(chain.Frames[0], SelectedState.Self.SelectedFrame);
    }

    // ── Events ───────────────────────────────────────────────────────────

    [Fact]
    public void FiresAnimationChainsChanged()
    {
        bool fired = false;
        ApplicationEvents.Self.AnimationChainsChanged += () => fired = true;
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "t.png", 0, 0, 64, 64, 128, 128);
        Assert.True(fired);
    }

    [Fact]
    public void FiresRefreshChainNodeRequested()
    {
        AnimationChainSave? received = null;
        AppCommands.Self.RefreshChainNodeRequested += c => received = c;
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "t.png", 0, 0, 64, 64, 128, 128);
        Assert.Same(chain, received);
    }

    // ── Non-square texture ────────────────────────────────────────────────

    [Fact]
    public void NonSquareTexture_UVsComputedCorrectly()
    {
        // 200 wide × 100 tall; region from (50,25) to (150,75)
        var chain = MakeChain();
        AppCommands.Self.AddFrameFromPixelBounds(chain, "wide.png", 50, 25, 150, 75, 200, 100);
        var f = chain.Frames[0];
        Assert.Equal(50f / 200f, f.LeftCoordinate,   precision: 5);
        Assert.Equal(150f / 200f, f.RightCoordinate,  precision: 5);
        Assert.Equal(25f / 100f,  f.TopCoordinate,    precision: 5);
        Assert.Equal(75f / 100f,  f.BottomCoordinate, precision: 5);
    }
}
