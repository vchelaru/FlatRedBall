using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class AppCommandsFrameTests
{
    // ── AddFrame ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddFrame_AddsFrameToChain()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run");

        AppCommands.Self.AddFrame(chain);

        Assert.Single(chain.Frames);
    }

    [Fact]
    public void AddFrame_DefaultsToFullUvCoordinates()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run");

        AppCommands.Self.AddFrame(chain);
        var frame = chain.Frames[0];

        Assert.Equal(0f, frame.LeftCoordinate);
        Assert.Equal(1f, frame.RightCoordinate);
        Assert.Equal(0f, frame.TopCoordinate);
        Assert.Equal(1f, frame.BottomCoordinate);
    }

    [Fact]
    public void AddFrame_DefaultsFrameLengthTo0Point1()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Idle");

        AppCommands.Self.AddFrame(chain);

        Assert.Equal(0.1f, chain.Frames[0].FrameLength);
    }

    [Fact]
    public void AddFrame_WithTextureName_SetsTextureOnNewFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk");

        AppCommands.Self.AddFrame(chain, "hero.png");

        Assert.Equal("hero.png", chain.Frames[0].TextureName);
    }

    [Fact]
    public void AddFrame_WithoutTextureName_SetsEmptyString()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Walk");

        AppCommands.Self.AddFrame(chain);

        Assert.Equal(string.Empty, chain.Frames[0].TextureName);
    }

    [Fact]
    public void AddFrame_InitializesShapeCollectionSave()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Attack");

        AppCommands.Self.AddFrame(chain);

        Assert.NotNull(chain.Frames[0].ShapeCollectionSave);
    }

    [Fact]
    public void AddFrame_SetsSelectedFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Jump");

        AppCommands.Self.AddFrame(chain);

        Assert.Same(chain.Frames[0], SelectedState.Self.SelectedFrame);
    }

    [Fact]
    public void AddFrame_FiresAnimationChainsChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "X");
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.AddFrame(chain);
            Assert.True(fired, "AnimationChainsChanged not raised after AddFrame.");
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }

    [Fact]
    public void AddFrame_MultipleFrames_AllAppendedInOrder()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run");

        AppCommands.Self.AddFrame(chain, "a.png");
        AppCommands.Self.AddFrame(chain, "b.png");
        AppCommands.Self.AddFrame(chain, "c.png");

        Assert.Equal(3, chain.Frames.Count);
        Assert.Equal("a.png", chain.Frames[0].TextureName);
        Assert.Equal("b.png", chain.Frames[1].TextureName);
        Assert.Equal("c.png", chain.Frames[2].TextureName);
    }

    // ── MoveFrame ────────────────────────────────────────────────────────────

    [Fact]
    public void MoveFrame_Delta1_MovesFrameDown()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var frameA = chain.Frames[0];
        var frameB = chain.Frames[1];

        AppCommands.Self.MoveFrame(frameA, chain, +1);

        Assert.Equal(frameB, chain.Frames[0]);
        Assert.Equal(frameA, chain.Frames[1]);
    }

    [Fact]
    public void MoveFrame_DeltaNeg1_MovesFrameUp()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var frameA = chain.Frames[0];
        var frameB = chain.Frames[1];

        AppCommands.Self.MoveFrame(frameB, chain, -1);

        Assert.Equal(frameB, chain.Frames[0]);
        Assert.Equal(frameA, chain.Frames[1]);
    }

    [Fact]
    public void MoveFrame_AtBottom_DoesNotMoveBelowEnd()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var lastFrame = chain.Frames[2];

        AppCommands.Self.MoveFrame(lastFrame, chain, +1);

        Assert.Equal(lastFrame, chain.Frames[2]);
    }

    [Fact]
    public void MoveFrame_AtTop_DoesNotMoveAboveStart()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var firstFrame = chain.Frames[0];

        AppCommands.Self.MoveFrame(firstFrame, chain, -1);

        Assert.Equal(firstFrame, chain.Frames[0]);
    }

    // ── MoveFrameToTop / MoveFrameToBottom ───────────────────────────────────

    [Fact]
    public void MoveFrameToTop_MovesFrameToFirstPosition()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var lastFrame = chain.Frames[2];

        AppCommands.Self.MoveFrameToTop(lastFrame, chain);

        Assert.Equal(lastFrame, chain.Frames[0]);
    }

    [Fact]
    public void MoveFrameToBottom_MovesFrameToLastPosition()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var firstFrame = chain.Frames[0];

        AppCommands.Self.MoveFrameToBottom(firstFrame, chain);

        Assert.Equal(firstFrame, chain.Frames[2]);
    }

    [Fact]
    public void MoveFrameToTop_AlreadyAtTop_IsIdempotent()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var firstFrame = chain.Frames[0];

        AppCommands.Self.MoveFrameToTop(firstFrame, chain);

        Assert.Equal(firstFrame, chain.Frames[0]);
        Assert.Equal(3, chain.Frames.Count);
    }

    [Fact]
    public void MoveFrameToBottom_AlreadyAtBottom_IsIdempotent()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 3);
        var lastFrame = chain.Frames[2];

        AppCommands.Self.MoveFrameToBottom(lastFrame, chain);

        Assert.Equal(lastFrame, chain.Frames[2]);
        Assert.Equal(3, chain.Frames.Count);
    }

    [Fact]
    public void MoveFrame_FiresAnimationChainsChanged()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 2);
        var frame = chain.Frames[0];
        bool fired = false;
        void Handler() => fired = true;
        ApplicationEvents.Self.AnimationChainsChanged += Handler;
        try
        {
            AppCommands.Self.MoveFrame(frame, chain, +1);
            Assert.True(fired);
        }
        finally
        {
            ApplicationEvents.Self.AnimationChainsChanged -= Handler;
        }
    }
}
