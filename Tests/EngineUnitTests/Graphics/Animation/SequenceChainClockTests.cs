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

    [Fact]
    public void Locate_NonLoopingReverse_HoldsTheFirstFrameAtTheEnd()
    {
        var chain = Chain(loop: false, reverse: true, lengths: new[] { 1f, 1f });

        var (frame, elapsed) = SequenceChainClock.Locate(chain, 5);

        frame.ShouldBe(0);
        elapsed.ShouldBe(0, 1e-6);
    }
}
