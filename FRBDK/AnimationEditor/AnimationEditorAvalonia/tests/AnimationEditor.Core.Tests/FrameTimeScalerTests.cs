using AnimationEditor.Core.Rendering;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class FrameTimeScalerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    static AnimationFrameSave Frame(float length)
        => new AnimationFrameSave { FrameLength = length };

    static List<AnimationFrameSave> Frames(params float[] lengths)
        => lengths.Select(Frame).ToList();

    // ── ApplyKeepProportional ─────────────────────────────────────────────

    [Fact]
    public void KeepProportional_ScalesToTargetTotal()
    {
        // 0.1 + 0.1 + 0.1 = 0.3 → scale to 0.6 ⇒ each becomes 0.2
        var frames = Frames(0.1f, 0.1f, 0.1f);
        FrameTimeScaler.ApplyKeepProportional(frames, 0.6f);
        Assert.Equal(0.6f, frames.Sum(f => f.FrameLength), precision: 4);
    }

    [Fact]
    public void KeepProportional_PreservesRatiosBetweenFrames()
    {
        // 0.1, 0.2, 0.3 → total 0.6; scale to 1.2 ⇒ 0.2, 0.4, 0.6
        var frames = Frames(0.1f, 0.2f, 0.3f);
        FrameTimeScaler.ApplyKeepProportional(frames, 1.2f);
        Assert.Equal(0.2f, frames[0].FrameLength, precision: 4);
        Assert.Equal(0.4f, frames[1].FrameLength, precision: 4);
        Assert.Equal(0.6f, frames[2].FrameLength, precision: 4);
    }

    [Fact]
    public void KeepProportional_SingleFrame_SetsToTarget()
    {
        var frames = Frames(0.5f);
        FrameTimeScaler.ApplyKeepProportional(frames, 2f);
        Assert.Equal(2f, frames[0].FrameLength, precision: 4);
    }

    [Fact]
    public void KeepProportional_EmptyList_NoException()
        => FrameTimeScaler.ApplyKeepProportional([], 1f); // just should not throw

    [Fact]
    public void KeepProportional_ZeroTotalDuration_IsNoOp()
    {
        // All frames have FrameLength 0 → can't scale, leave as-is
        var frames = Frames(0f, 0f);
        FrameTimeScaler.ApplyKeepProportional(frames, 1f);
        Assert.All(frames, f => Assert.Equal(0f, f.FrameLength));
    }

    [Fact]
    public void KeepProportional_ScaleDownToHalf_HalvesEachFrame()
    {
        var frames = Frames(0.4f, 0.6f);  // total 1.0 → target 0.5
        FrameTimeScaler.ApplyKeepProportional(frames, 0.5f);
        Assert.Equal(0.2f, frames[0].FrameLength, precision: 4);
        Assert.Equal(0.3f, frames[1].FrameLength, precision: 4);
    }

    // ── ApplySetAllSame ───────────────────────────────────────────────────

    [Fact]
    public void SetAllSame_ThreeFrames_EachGetsOneThird()
    {
        var frames = Frames(0.1f, 0.2f, 0.9f); // different lengths
        FrameTimeScaler.ApplySetAllSame(frames, 0.9f);
        Assert.All(frames, f => Assert.Equal(0.3f, f.FrameLength, precision: 4));
    }

    [Fact]
    public void SetAllSame_SingleFrame_SetsToTarget()
    {
        var frames = Frames(0.05f);
        FrameTimeScaler.ApplySetAllSame(frames, 1f);
        Assert.Equal(1f, frames[0].FrameLength, precision: 4);
    }

    [Fact]
    public void SetAllSame_TotalMatchesTarget()
    {
        var frames = Frames(0.1f, 0.2f, 0.3f, 0.4f);
        FrameTimeScaler.ApplySetAllSame(frames, 2f);
        Assert.Equal(2f, frames.Sum(f => f.FrameLength), precision: 4);
    }

    [Fact]
    public void SetAllSame_EmptyList_NoException()
        => FrameTimeScaler.ApplySetAllSame([], 1f);

    [Fact]
    public void SetAllSame_ZeroTarget_SetsAllToZero()
    {
        var frames = Frames(0.1f, 0.2f);
        FrameTimeScaler.ApplySetAllSame(frames, 0f);
        Assert.All(frames, f => Assert.Equal(0f, f.FrameLength));
    }
}
