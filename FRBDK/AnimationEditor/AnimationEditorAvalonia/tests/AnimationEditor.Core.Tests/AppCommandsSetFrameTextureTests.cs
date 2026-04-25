using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class AppCommandsSetFrameTextureTests
{
    // ── SetFrameTextureName — basic assignment ────────────────────────────────

    [Fact]
    public void SetFrameTextureName_SetsTextureNameOnFrame()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave();

        AppCommands.Self.SetFrameTextureName(frame, "hero.png");

        Assert.Equal("hero.png", frame.TextureName);
    }

    [Fact]
    public void SetFrameTextureName_CanOverwriteExistingName()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave { TextureName = "old.png" };

        AppCommands.Self.SetFrameTextureName(frame, "new.png");

        Assert.Equal("new.png", frame.TextureName);
    }

    [Fact]
    public void SetFrameTextureName_CanClearToNull()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave { TextureName = "hero.png" };

        AppCommands.Self.SetFrameTextureName(frame, null);

        Assert.Null(frame.TextureName);
    }

    [Fact]
    public void SetFrameTextureName_CanClearToEmptyString()
    {
        TestHelpers.SetupFreshAcls();
        var frame = new AnimationFrameSave { TextureName = "hero.png" };

        AppCommands.Self.SetFrameTextureName(frame, "");

        Assert.Equal("", frame.TextureName);
    }

    // ── Guard: null frame ─────────────────────────────────────────────────────

    [Fact]
    public void SetFrameTextureName_NullFrame_DoesNotThrow()
    {
        TestHelpers.SetupFreshAcls();
        // Should be a silent no-op
        AppCommands.Self.SetFrameTextureName(null!, "hero.png");
    }

    // ── Event firing ──────────────────────────────────────────────────────────

    [Fact]
    public void SetFrameTextureName_FiresAnimationChainsChanged()
    {
        TestHelpers.SetupFreshAcls();
        var frame   = new AnimationFrameSave();
        bool fired  = false;
        ApplicationEvents.Self.AnimationChainsChanged += () => fired = true;

        AppCommands.Self.SetFrameTextureName(frame, "run.png");

        Assert.True(fired);
    }

    [Fact]
    public void SetFrameTextureName_FiresRefreshFrameNodeRequested_WithCorrectFrame()
    {
        TestHelpers.SetupFreshAcls();
        var frame             = new AnimationFrameSave();
        AnimationFrameSave? received = null;
        AppCommands.Self.RefreshFrameNodeRequested += f => received = f;

        AppCommands.Self.SetFrameTextureName(frame, "idle.png");

        Assert.Same(frame, received);
    }

    [Fact]
    public void SetFrameTextureName_NullFrame_DoesNotFireEvents()
    {
        TestHelpers.SetupFreshAcls();
        bool changed = false;
        ApplicationEvents.Self.AnimationChainsChanged += () => changed = true;

        AppCommands.Self.SetFrameTextureName(null!, "hero.png");

        Assert.False(changed);
    }
}
