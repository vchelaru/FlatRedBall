using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Graphics.Animation;

/// <summary>The sequence timing rules. The cases are the FRB2 SequenceTimelineTests vectors, so both engines agree.</summary>
public class SequenceTimelineTests
{
    static SequenceChainReferenceSave Ref(string chain, double start, float loops = 1) =>
        new() { Source = "a.achj", Chain = chain, Start = start, Loops = loops };

    static SequenceTrackSave Track(params SequenceChainReferenceSave[] chains)
    {
        var track = new SequenceTrackSave();
        track.Chains.AddRange(chains);
        return track;
    }

    [Fact]
    public void ActiveAt_LaterChainStartsInsideEarlier_CutsTheEarlierAndNeverResumesIt()
    {
        var charge = Ref("Charge", start: 0); // 3 s cycle
        var hurt = Ref("Hurt", start: 1);     // 0.5 s cycle
        var track = Track(charge, hurt);
        double Cycle(SequenceChainReferenceSave r) => r == charge ? 3 : 0.5;

        SequenceTimeline.ActiveAt(track, 0.5, Cycle, out double localTime).ShouldBe(charge);
        localTime.ShouldBe(0.5, 1e-9);
        SequenceTimeline.ActiveAt(track, 1.2, Cycle, out _).ShouldBe(hurt);
        // Hurt ended at 1.5; Charge's tail (to 3.0) is cut, so the sprite is hidden.
        SequenceTimeline.ActiveAt(track, 2.0, Cycle, out _).ShouldBeNull();
        SequenceTimeline.PlayedLength(track, charge, cycleLength: 3).ShouldBe(1, 1e-9);
    }

    [Fact]
    public void PlayedLength_InterrupterMovesAway_EarlierChainPlaysInFullAgain()
    {
        var charge = Ref("Charge", start: 0, loops: 2);
        var hurt = Ref("Hurt", start: 1);
        var track = Track(charge, hurt);

        hurt.Start = 10;

        SequenceTimeline.PlayedLength(track, charge, cycleLength: 3).ShouldBe(6, 1e-9);
    }

    [Fact]
    public void PlayedLength_TwoChainsStartTogether_EarlierInListPlaysZeroSeconds()
    {
        var first = Ref("First", start: 2);
        var second = Ref("Second", start: 2);
        var track = Track(first, second);

        SequenceTimeline.PlayedLength(track, first, cycleLength: 1).ShouldBe(0);
        SequenceTimeline.PlayedLength(track, second, cycleLength: 1).ShouldBe(1, 1e-9);
        SequenceTimeline.ActiveAt(track, 2.5, _ => 1, out _).ShouldBe(second);
    }

    [Fact]
    public void ActiveAt_MissingChain_NeverPlaysButStillCutsTheChainBeforeIt()
    {
        var walk = Ref("Walk", start: 0);
        var missing = Ref("Deleted", start: 1);
        var track = Track(walk, missing);
        // A chain that can't be resolved has a cycle length of 0.
        double Cycle(SequenceChainReferenceSave r) => r == walk ? 3 : 0;

        SequenceTimeline.ActiveAt(track, 1.5, Cycle, out _).ShouldBeNull();
        SequenceTimeline.PlayedLength(track, walk, cycleLength: 3).ShouldBe(1, 1e-9);
    }

    [Fact]
    public void Length_FloatLoops_EndsPartWayThroughTheLastCycle()
    {
        var sequence = new SequenceSave();
        sequence.Tracks.Add(Track(Ref("Walk", start: 1, loops: 2.5f)));

        // A ping-pong chain of 0.4 s forward has a 0.8 s cycle.
        SequenceTimeline.Length(sequence, _ => 0.8).ShouldBe(3, 1e-6);
    }

    [Theory]
    [InlineData(0.5, 2, false, 0.5)]   // inside the sequence: shown as is
    [InlineData(2.5, 2, true, 0.5)]    // past the end with Loop: wraps
    [InlineData(2.5, 2, false, 2)]     // past the end without Loop: held at the length
    [InlineData(-1, 2, false, 0)]      // negative elapsed time shows 0
    [InlineData(-1, 2, true, 0)]
    [InlineData(1, 0, true, 0)]        // an empty sequence always shows 0
    public void WrapTime_WrapsOrHoldsAtTheEnd_AndNeverGoesNegative(double time, double length, bool loop, double expected)
    {
        SequenceTimeline.WrapTime(time, length, loop).ShouldBe(expected, 1e-9);
    }

    [Fact]
    public void FullLength_NegativeLoops_IsZero()
    {
        SequenceTimeline.FullLength(loops: -2, cycleLength: 3).ShouldBe(0);
    }

    [Fact]
    public void ActiveAt_ExactlyAtTheEndOrBeforeTheFirstStart_IsHidden()
    {
        var walk = Ref("Walk", start: 1);
        var track = Track(walk);

        SequenceTimeline.ActiveAt(track, 0.99, _ => 2, out _).ShouldBeNull();
        SequenceTimeline.ActiveAt(track, 1.0, _ => 2, out double atStart).ShouldBe(walk);
        atStart.ShouldBe(0, 1e-9);
        SequenceTimeline.ActiveAt(track, 3.0, _ => 2, out _).ShouldBeNull();
    }

    [Fact]
    public void ActiveAt_AfterTheLastChainEnds_ReturnsNull()
    {
        // Hidden after the last chain on purpose: this is how a sprite disappears partway through a sequence.
        var walk = Ref("Walk", start: 1);
        var track = Track(walk);

        SequenceTimeline.ActiveAt(track, 2.9, _ => 2, out _).ShouldBe(walk);
        SequenceTimeline.ActiveAt(track, 3, _ => 2, out _).ShouldBeNull();
        SequenceTimeline.ActiveAt(track, 10, _ => 2, out _).ShouldBeNull();
    }

    [Fact]
    public void Length_AMissingChainCutsTheOneBeforeIt_AndNoChainsIsZero()
    {
        var long_ = Ref("Long", start: 0, loops: 10);
        var missing = Ref("Deleted", start: 1);
        var sequence = new SequenceSave();
        sequence.Tracks.Add(Track(long_, missing));
        double Cycle(SequenceChainReferenceSave r) => r == long_ ? 1 : 0;

        // Long asks for 10 s but is cut at 1 s by the missing chain, which itself plays 0 s.
        SequenceTimeline.Length(sequence, Cycle).ShouldBe(1, 1e-9);
        SequenceTimeline.Length(new SequenceSave(), _ => 1).ShouldBe(0);
    }
}
