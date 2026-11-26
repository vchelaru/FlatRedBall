using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// A continuous reference track keeps its target playing across the holder's loop wrap instead of restarting it
/// (<see cref="SequenceTimeline.ReferenceCarryOver"/>). Target "T" plays Walk (two 0.25 s frames, loops) for 0.5 s, so its
/// frame tells where its clock is: 0 to 0.25 is frame 0, 0.25 to 0.5 frame 1. The holder "P" loops every 1.2 s.
/// </summary>
public class SequencePlayerReferenceContinuousTests
{
    static AnimationChainList Source()
    {
        var walk = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Walk", Loop = true };
        walk.Add(new AnimationFrame { FrameLength = 0.25f });
        walk.Add(new AnimationFrame { FrameLength = 0.25f });
        return new AnimationChainList { walk };
    }

    static SequenceTrackSave Chain(string name, double? length = null, bool continuous = false)
    {
        var track = new SequenceTrackSave { Name = name, Continuous = continuous };
        track.Chains.Add(new SequenceChainReferenceSave { Source = "s", Chain = "Walk", Start = 0, Length = length });
        return track;
    }

    static SequenceTrackSave Ref(bool continuous, double start = 0, double? length = 1.2) =>
        new() { Name = "Legs", Continuous = continuous, References = [new SequenceReferenceSave { Sequence = "T", Start = start, Length = length }] };

    static SequenceInstance Play(SequenceTrackSave reference, bool targetLoops = true, SequenceTrackSave targetTrack = null, SequenceTrackSave holderTrack = null)
    {
        var file = new SequenceListSave();
        var holder = new SequenceSave { Name = "P", Loop = true };
        holder.Tracks.Add(reference);
        if (holderTrack != null)
        {
            holder.Tracks.Add(holderTrack);
        }
        var target = new SequenceSave { Name = "T", Loop = targetLoops };
        target.Tracks.Add(targetTrack ?? Chain("Body", length: 0.5));
        file.Sequences.Add(holder);
        file.Sequences.Add(target);
        var instance = new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => Source()))["P"];
        instance.Play();
        return instance;
    }

    static int FrameAfter(SequenceInstance instance, double seconds)
    {
        instance.Update(seconds);
        return instance.Tracks["Legs/Body"].Sprite.CurrentFrameIndex;
    }

    [Fact]
    public void FlagOff_TargetRestartsAtTheWrap()
    {
        // 1.35 is 0.15 into pass 1: the target restarted, so 0.15 into Walk.
        FrameAfter(Play(Ref(continuous: false)), 1.35).ShouldBe(0);
    }

    [Fact]
    public void FlagOn_TargetKeepsItsPhaseAcrossTheWrap()
    {
        // The target's clock is 1.35, wrapped by its 0.5 s length: 0.35, frame 1.
        FrameAfter(Play(Ref(continuous: true)), 1.35).ShouldBe(1);
    }

    [Fact]
    public void FlagOn_ClipStartsAfterZero_Ignored()
    {
        // Restarted: 0.15 into pass 1 is 0.05 into the clip.
        FrameAfter(Play(Ref(continuous: true, start: 0.1, length: 1.1)), 1.35).ShouldBe(0);
    }

    [Fact]
    public void FlagOn_TargetDoesNotLoop_Ignored()
    {
        FrameAfter(Play(Ref(continuous: true), targetLoops: false), 1.35).ShouldBe(0);
    }

    [Fact]
    public void FlagOn_ClipEndsBeforeTheHoldersEnd_Ignored()
    {
        // The holder's own 1.2 s track makes it outlast the 1.0 s clip.
        FrameAfter(Play(Ref(continuous: true, length: 1.0), holderTrack: Chain("Own", length: 1.2)), 1.35).ShouldBe(0);
    }

    [Fact]
    public void FlagOn_TargetsOwnWrapsCarryItsContinuousChainTrack()
    {
        // T plays Walk x 1.2 = 0.6 s and loops; its Body track is continuous. At 0.8 the target has wrapped once and is
        // 0.2 into its second pass, but Body carries its 0.6 s: 0.8 into Walk, 0.3 into the cycle, frame 1.
        var reference = Ref(continuous: true, length: 1.8);
        FrameAfter(Play(reference, targetTrack: Chain("Body", length: 0.6, continuous: true)), 0.8).ShouldBe(1);
    }

    [Fact]
    public void FlagOff_TargetsContinuousChainTrackRunsAtPassZero()
    {
        // Not carried, so the target runs at pass 0 and Body restarts with each target wrap: 0.2 into Walk, frame 0.
        var reference = Ref(continuous: false, length: 1.8);
        FrameAfter(Play(reference, targetTrack: Chain("Body", length: 0.6, continuous: true)), 0.8).ShouldBe(0);
    }
}
