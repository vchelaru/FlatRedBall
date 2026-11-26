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
        new() { Continuous = continuous, References = [new SequenceReferenceSave { Sequence = "T", Start = start, Length = length }] };

    private static double? Time(SequenceTrackSave track, bool targetLoops, double parentTime, int pass) =>
        SequenceTimeline.ReferenceTimeAtPass(track, track.References![0], new SequenceSave { Name = "T", Loop = targetLoops }, TargetLength,
            parentTime, pass, ParentLength, _ => TargetLength, out _);

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
        var track = Track(continuous);
        SequenceTimeline.ReferenceTimeAtPass(track, track.References![0], new SequenceSave { Name = "T", Loop = true }, TargetLength, 0.3, pass,
            ParentLength, _ => TargetLength, out int targetPass);

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
    public void ReferenceCarryOver_AutoLengthClip_CountsAsOneTargetCycle()
    {
        var autoLength = Track(true, length: null);

        SequenceTimeline.ReferenceCarryOver(autoLength, ParentLength, 1, _ => ParentLength, _ => true).ShouldBe(ParentLength, 1e-6);
        SequenceTimeline.ReferenceCarryOver(autoLength, ParentLength, 1, _ => TargetLength, _ => true).ShouldBe(0);
    }

    [Fact]
    public void ReferenceCarryOver_SameSequencePlayingAtTheWrap_CarriesFromThatClipsStart()
    {
        // First clip plays T from 0, a second clip plays T again from 6 to the wrap: the first clip resumes where the second left off.
        var track = new SequenceTrackSave
        {
            Continuous = true,
            References = [new SequenceReferenceSave { Sequence = "T", Start = 0, Length = 4 }, new SequenceReferenceSave { Sequence = "T", Start = 6, Length = 2.6 }],
        };

        SequenceTimeline.ReferenceCarryOver(track, ParentLength, 1, _ => TargetLength, _ => true).ShouldBe(2.6, 1e-6);
    }

    [Fact]
    public void ReferenceCarryOver_OtherSequencePlayingAtTheWrap_Restarts()
    {
        var track = new SequenceTrackSave
        {
            Continuous = true,
            References = [new SequenceReferenceSave { Sequence = "T", Start = 0 }, new SequenceReferenceSave { Sequence = "U", Start = 6, Length = 2.6 }],
        };

        SequenceTimeline.ReferenceCarryOver(track, ParentLength, 1, _ => TargetLength, _ => true).ShouldBe(0);
    }

    [Fact]
    public void ReferenceTimeAtPass_SecondClip_RestartsAtEveryWrap()
    {
        // Only the first clip carries: the second one restarts at its own start each pass.
        var track = new SequenceTrackSave
        {
            Continuous = true,
            References = [new SequenceReferenceSave { Sequence = "T", Start = 0, Length = 4 }, new SequenceReferenceSave { Sequence = "T", Start = 6, Length = 2.6 }],
        };

        SequenceTimeline.ReferenceTimeAtPass(track, track.References[1], new SequenceSave { Name = "T", Loop = true }, TargetLength, 6.3, 1,
            ParentLength, _ => TargetLength, out _)!.Value.ShouldBe(0.3, 1e-6);
    }
}
