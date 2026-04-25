using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class PlaybackControllerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AnimationChainSave MakeChain(int frameCount, float frameLength = 0.1f)
    {
        var chain = new AnimationChainSave { Name = "Test" };
        for (int i = 0; i < frameCount; i++)
            chain.Frames.Add(new AnimationFrameSave { FrameLength = frameLength });
        return chain;
    }

    // ── Null / single-frame guard ─────────────────────────────────────────────

    [Fact]
    public void Advance_WithNoChainSet_DoesNothing()
    {
        var ctrl = new PlaybackController();
        ctrl.Advance(1.0);
        Assert.Equal(0, ctrl.CurrentFrameIndex);
        Assert.Equal(0.0, ctrl.AnimTime);
    }

    [Fact]
    public void Advance_WithSingleFrameChain_DoesNotChangeIndex()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(1));
        ctrl.Advance(999.0);
        Assert.Equal(0, ctrl.CurrentFrameIndex);
    }

    // ── Frame advancement ─────────────────────────────────────────────────────

    [Fact]
    public void Advance_MovesToNextFrame_WhenTimeExceedsFirstFrameLength()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f)); // 3 frames × 0.1 s = 0.3 s total
        ctrl.Advance(0.15);                // 0.15 s > frame[0]=0.1 s → frame 1
        Assert.Equal(1, ctrl.CurrentFrameIndex);
    }

    [Fact]
    public void Advance_StaysOnFirstFrame_WhenTimeLessThanFrameLength()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.Advance(0.05); // 0.05 s < 0.1 s → still frame 0
        Assert.Equal(0, ctrl.CurrentFrameIndex);
    }

    [Fact]
    public void Advance_ReachesLastFrame_WhenTimeInLastFrameWindow()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f)); // total = 0.3 s
        ctrl.Advance(0.25);                // frame 0=0-0.1, frame 1=0.1-0.2, frame 2=0.2-0.3 → idx 2
        Assert.Equal(2, ctrl.CurrentFrameIndex);
    }

    // ── Looping ───────────────────────────────────────────────────────────────

    [Fact]
    public void Advance_LoopsBackToFirstFrame_AfterTotalDuration()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f)); // total = 0.3 s
        ctrl.Advance(0.35);                // 0.35 % 0.3 = 0.05 → frame 0
        Assert.Equal(0, ctrl.CurrentFrameIndex);
    }

    [Fact]
    public void Advance_AccumulatesTimeAcrossMultipleCalls()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.Advance(0.05); // frame 0
        ctrl.Advance(0.09); // 0.14 total → still frame 1
        Assert.Equal(1, ctrl.CurrentFrameIndex);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    [Fact]
    public void Advance_FiresFrameIndexChanged_WhenFrameChanges()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        int firedIdx = -1;
        ctrl.FrameIndexChanged += i => firedIdx = i;

        ctrl.Advance(0.15);

        Assert.Equal(1, firedIdx);
    }

    [Fact]
    public void Advance_DoesNotFireEvent_WhenFrameDoesNotChange()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        int eventCount = 0;
        ctrl.FrameIndexChanged += _ => eventCount++;

        ctrl.Advance(0.05); // stays in frame 0

        Assert.Equal(0, eventCount);
    }

    // ── Reset / SetChain ──────────────────────────────────────────────────────

    [Fact]
    public void SetChain_ResetsAnimTimeAndFrameIndex()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.Advance(0.25);
        Assert.Equal(2, ctrl.CurrentFrameIndex);

        ctrl.SetChain(MakeChain(3, 0.1f)); // new chain → reset
        Assert.Equal(0, ctrl.CurrentFrameIndex);
        Assert.Equal(0.0, ctrl.AnimTime);
    }

    [Fact]
    public void Reset_ResetsWithoutChangingChain()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.Advance(0.25);

        ctrl.Reset();

        Assert.Equal(0, ctrl.CurrentFrameIndex);
        Assert.Equal(0.0, ctrl.AnimTime);
    }

    [Fact]
    public void Reset_FiresFrameIndexChangedWithZero()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.Advance(0.25);
        int? lastFired = null;
        ctrl.FrameIndexChanged += i => lastFired = i;

        ctrl.Reset();

        Assert.Equal(0, lastFired);
    }

    // ── Zero FrameLength defaults ─────────────────────────────────────────────

    [Fact]
    public void Advance_WithZeroFrameLength_UsesDefaultOf100ms()
    {
        var ctrl = new PlaybackController();
        var chain = new AnimationChainSave { Name = "Z" };
        chain.Frames.Add(new AnimationFrameSave { FrameLength = 0 });
        chain.Frames.Add(new AnimationFrameSave { FrameLength = 0 });
        ctrl.SetChain(chain);

        ctrl.Advance(0.15); // 0.15 > 0.1 default → frame 1

        Assert.Equal(1, ctrl.CurrentFrameIndex);
    }

    // ── Pause / Play (PL02) ───────────────────────────────────────────────────

    [Fact]
    public void IsPlaying_DefaultsToTrue()
    {
        var ctrl = new PlaybackController();
        Assert.True(ctrl.IsPlaying);
    }

    [Fact]
    public void Pause_SetsIsPlayingFalse()
    {
        var ctrl = new PlaybackController();
        ctrl.Pause();
        Assert.False(ctrl.IsPlaying);
    }

    [Fact]
    public void Play_SetsIsPlayingTrue_AfterPause()
    {
        var ctrl = new PlaybackController();
        ctrl.Pause();
        ctrl.Play();
        Assert.True(ctrl.IsPlaying);
    }

    [Fact]
    public void Advance_DoesNotMoveFrame_WhenPaused()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.Pause();

        ctrl.Advance(0.5); // would skip to frame 2 if playing

        Assert.Equal(0, ctrl.CurrentFrameIndex);
        Assert.Equal(0.0, ctrl.AnimTime);
    }

    [Fact]
    public void Advance_ResumesMovement_AfterPlay()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.Pause();
        ctrl.Advance(0.5); // no-op while paused
        ctrl.Play();
        ctrl.Advance(0.15); // now playing → frame 1

        Assert.Equal(1, ctrl.CurrentFrameIndex);
    }

    [Fact]
    public void Pause_DoesNotFireFrameIndexChanged()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        int eventCount = 0;
        ctrl.FrameIndexChanged += _ => eventCount++;

        ctrl.Pause();
        ctrl.Advance(0.5); // paused → no advancement → no event

        Assert.Equal(0, eventCount);
    }

    // ── SpeedMultiplier (PL04) ────────────────────────────────────────────────

    [Fact]
    public void SpeedMultiplier_DefaultsToOne()
    {
        var ctrl = new PlaybackController();
        Assert.Equal(1.0, ctrl.SpeedMultiplier);
    }

    [Fact]
    public void Advance_WithDoubleSpeed_AdvancesTwiceAsFast()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.SpeedMultiplier = 2.0;

        // At 2× speed, 0.075 s of real time = 0.15 s of anim time → frame 1
        ctrl.Advance(0.075);

        Assert.Equal(1, ctrl.CurrentFrameIndex);
    }

    [Fact]
    public void Advance_WithHalfSpeed_AdvancesHalfAsFast()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.SpeedMultiplier = 0.5;

        // At 0.5× speed, 0.3 s of real time = 0.15 s of anim time → frame 1
        ctrl.Advance(0.3);

        Assert.Equal(1, ctrl.CurrentFrameIndex);
    }

    [Fact]
    public void Advance_WithZeroSpeed_DoesNotAdvanceFrame()
    {
        var ctrl = new PlaybackController();
        ctrl.SetChain(MakeChain(3, 0.1f));
        ctrl.SpeedMultiplier = 0.0;

        ctrl.Advance(999.0); // any real delta → 0 anim delta

        Assert.Equal(0, ctrl.CurrentFrameIndex);
    }
}
