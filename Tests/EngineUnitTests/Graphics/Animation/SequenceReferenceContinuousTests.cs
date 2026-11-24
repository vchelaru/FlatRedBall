using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Graphics.Animation;

/// <summary>
/// A continuous reference track keeps the target's playback phase across the holder's loop wrap, when the clip starts at 0,
/// the target loops and the clip is still playing at the wrap. Otherwise the wrap restarts the target. Mirrors FRB2's
/// SequenceReferenceContinuousTests.
/// </summary>
public class SequenceReferenceContinuousTests
{
    private const double ParentLength = 8.6;
    private const double TargetLength = 1.0;

    private static SequenceTrackSave Track(bool continuous, double start = 0, double? length = ParentLength) =>
        new() { Continuous = continuous, Reference = new SequenceReferenceSave { Sequence = "T", Start = start, Length = length } };

    private static double? Time(SequenceTrackSave track, bool targetLoops, double parentTime, int pass) =>
        SequenceTimeline.ReferenceTimeAtPass(track, new SequenceSave { Name = "T", Loop = targetLoops }, TargetLength, parentTime, pass, ParentLength, out _);

    [Theory]
    [InlineData(0, 0.3)]
    [InlineData(1, 0.9)] // 8.6 + 0.3 = 8.9, wrapped by the 1 s target
    [InlineData(2, 0.5)] // 17.2 + 0.3 = 17.5
    public void ReferenceTimeAtPass_ContinuousLoopingTarget_CarriesPhaseAcrossWraps(int pass, double expected)
    {
        Time(Track(true), targetLoops: true, parentTime: 0.3, pass)!.Value.ShouldBe(expected, 1e-6);
    }

    [Theory]
    [InlineData(true, 1, 8)] // 8.6 + 0.3 = 8.9: the 1 s target has wrapped 8 times
    [InlineData(true, 0, 0)]
    [InlineData(false, 1, 0)] // not carried: the target runs at pass 0
    public void ReferenceTimeAtPass_CountsTheTargetsOwnWraps_OnlyWhenCarried(bool continuous, int pass, int expectedTargetPass)
    {
        SequenceTimeline.ReferenceTimeAtPass(Track(continuous), new SequenceSave { Name = "T", Loop = true }, TargetLength, 0.3, pass, ParentLength, out int targetPass);

        targetPass.ShouldBe(expectedTargetPass);
    }

    [Fact]
    public void ReferenceTimeAtPass_NotContinuous_RestartsAtEveryWrap()
    {
        Time(Track(false), targetLoops: true, parentTime: 0.3, pass: 3)!.Value.ShouldBe(0.3, 1e-6);
    }

    [Fact]
    public void ReferenceTimeAtPass_TargetDoesNotLoop_FlagIgnored()
    {
        Time(Track(true), targetLoops: false, parentTime: 0.3, pass: 1)!.Value.ShouldBe(0.3, 1e-6);
    }

    [Fact]
    public void ReferenceTimeAtPass_ClipStartsAfterZero_FlagIgnored()
    {
        Time(Track(true, start: 0.5, length: ParentLength - 0.5), targetLoops: true, parentTime: 1.0, pass: 1)!.Value.ShouldBe(0.5, 1e-6);
    }

    [Fact]
    public void ReferenceTimeAtPass_ClipEndsBeforeTheWrap_FlagIgnored()
    {
        Time(Track(true, length: 2), targetLoops: true, parentTime: 0.3, pass: 1)!.Value.ShouldBe(0.3, 1e-6);
    }

    [Fact]
    public void ReferenceTimeAtPass_ClipWindowStillAppliesWithinEachPass()
    {
        // The clip is 8.6 s long: it is over at the end of each pass, never earlier because of the carry.
        Time(Track(true), targetLoops: true, parentTime: 8.5, pass: 2).ShouldNotBeNull();
        Time(Track(true), targetLoops: true, parentTime: ParentLength, pass: 2).ShouldBeNull();
    }

    [Fact]
    public void CarriesAcrossWrap_AutoLengthClip_CountsAsOneTargetCycle()
    {
        var autoLength = Track(true, length: null);
        var target = new SequenceSave { Name = "T", Loop = true };

        SequenceTimeline.CarriesAcrossWrap(autoLength, target, ParentLength, ParentLength).ShouldBeTrue();
        SequenceTimeline.CarriesAcrossWrap(autoLength, target, TargetLength, ParentLength).ShouldBeFalse();
    }
}
