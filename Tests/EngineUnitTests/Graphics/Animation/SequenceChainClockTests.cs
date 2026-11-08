using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Graphics.Animation;

/// <summary>Which frame a chain shows at a time into its block: loop, hold, ping-pong, reverse.</summary>
public class SequenceChainClockTests
{
    static AnimationChain Chain(bool loop = true, bool pingPong = false, bool reverse = false, params float[] lengths)
    {
        var chain = new AnimationChain { Name = "c", Loop = loop, PingPong = pingPong, Reverse = reverse };
        foreach (float length in lengths)
        {
            chain.Add(new AnimationFrame { FrameLength = length });
        }
        return chain;
    }

    [Fact]
    public void CycleLength_IsTheSumOfFrames_DoubledForPingPong_AndZeroWhenEmpty()
    {
        SequenceChainClock.CycleLength(Chain(lengths: new[] { 0.25f, 0.25f })).ShouldBe(0.5, 1e-9);
        SequenceChainClock.CycleLength(Chain(pingPong: true, lengths: new[] { 0.25f, 0.25f })).ShouldBe(1, 1e-9);
        SequenceChainClock.CycleLength(Chain(lengths: System.Array.Empty<float>())).ShouldBe(0);
    }

    [Fact]
    public void Locate_LoopingChain_RepeatsAcrossTheBlock()
    {
        var (frame, elapsed) = SequenceChainClock.Locate(Chain(lengths: new[] { 0.25f, 0.25f }), 0.6);

        frame.ShouldBe(0);
        elapsed.ShouldBe(0.1, 1e-6);
    }

    [Fact]
    public void Locate_NonLoopingChain_PlaysOnceThenHoldsItsLastFrame()
    {
        var chain = Chain(loop: false, lengths: new[] { 0.25f, 0.25f });

        var (frame, elapsed) = SequenceChainClock.Locate(chain, 0.6);
        frame.ShouldBe(1);
        elapsed.ShouldBe(0.25, 1e-6);
        SequenceChainClock.Locate(chain, 1.4).Frame.ShouldBe(1);
    }

    [Fact]
    public void Locate_TimeOnAFrameBoundary_ShowsTheLaterFrame()
    {
        // 0.3 is a float; summed frame lengths land a hair off. The nudge keeps the boundary in the later frame.
        var chain = Chain(lengths: new[] { 0.1f, 0.2f, 0.3f });

        SequenceChainClock.Locate(chain, 0.3).Frame.ShouldBe(2);
        SequenceChainClock.Locate(chain, 0.1).Frame.ShouldBe(1);
    }

    [Fact]
    public void Locate_PingPong_PlaysForwardThenMirrored()
    {
        var chain = Chain(pingPong: true, lengths: new[] { 1f, 1f, 1f });

        SequenceChainClock.Locate(chain, 0.5).Frame.ShouldBe(0);
        SequenceChainClock.Locate(chain, 2.5).Frame.ShouldBe(2);
        SequenceChainClock.Locate(chain, 3.5).Frame.ShouldBe(2);
        SequenceChainClock.Locate(chain, 5.5).Frame.ShouldBe(0);
    }

    [Fact]
    public void Locate_Reverse_StartsOnTheLastFrame_AndReportsForwardElapsed()
    {
        var chain = Chain(reverse: true, lengths: new[] { 1f, 1f, 1f });

        var (frame, elapsed) = SequenceChainClock.Locate(chain, 0.25);

        frame.ShouldBe(2);
        elapsed.ShouldBe(0.75, 1e-6);
    }

    // Visits are the showings of a frame within one cycle, in playback order: one per frame, two per frame under ping-pong.
    [Theory]
    [InlineData(false, false, 0.1, 0, 0)]
    [InlineData(false, false, 1.5, 1, 1)]
    [InlineData(false, true, 0.1, 0, 2)]   // reverse starts on the last frame
    [InlineData(false, true, 2.5, 2, 0)]
    [InlineData(true, false, 3.5, 3, 2)]   // ping-pong: back across frame 2 after the turnaround
    [InlineData(true, false, 5.5, 5, 0)]
    [InlineData(true, true, 0.1, 0, 2)]    // ping-pong + reverse starts mirrored
    [InlineData(true, true, 3.5, 3, 0)]
    public void VisitAt_AndFrameOfVisit_AgreeWithPlaybackOrder(bool pingPong, bool reverse, double time, int visit, int frame)
    {
        var chain = Chain(pingPong: pingPong, reverse: reverse, lengths: new[] { 1f, 1f, 1f });

        SequenceChainClock.VisitAt(chain, time).ShouldBe(visit);
        SequenceChainClock.FrameOfVisit(chain, visit).ShouldBe(frame);
    }

    [Fact]
    public void VisitCount_IsOnePerFrame_TwoUnderPingPong()
    {
        SequenceChainClock.VisitCount(Chain(lengths: new[] { 1f, 1f, 1f })).ShouldBe(3);
        SequenceChainClock.VisitCount(Chain(pingPong: true, lengths: new[] { 1f, 1f, 1f })).ShouldBe(6);
    }

    [Fact]
    public void EntersNewFrame_APingPongTurnaroundAndLoopPointShowTheSameFrameTwice_SoTheyEnterNothing()
    {
        var chain = Chain(pingPong: true, lengths: new[] { 1f, 1f, 1f });

        // Visits 0 1 2 | 3 4 5 show frames 0 1 2 | 2 1 0. Visit 3 repeats frame 2; visit 0 follows visit 5's frame 0.
        SequenceChainClock.EntersNewFrame(chain, 3).ShouldBeFalse();
        SequenceChainClock.EntersNewFrame(chain, 0).ShouldBeFalse();
        SequenceChainClock.EntersNewFrame(chain, 1).ShouldBeTrue();
        SequenceChainClock.EntersNewFrame(chain, 4).ShouldBeTrue();
        // A plain chain re-enters its first frame on every wrap.
        SequenceChainClock.EntersNewFrame(Chain(lengths: new[] { 1f }), 0).ShouldBeTrue();
    }

    [Fact]
    public void Locate_NonLoopingReverse_HoldsTheFirstFrameAtTheEnd()
    {
        var chain = Chain(loop: false, reverse: true, lengths: new[] { 1f, 1f });

        var (frame, elapsed) = SequenceChainClock.Locate(chain, 5);

        frame.ShouldBe(0);
        elapsed.ShouldBe(0, 1e-6);
    }
}
