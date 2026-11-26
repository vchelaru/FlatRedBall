using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// A reference track plays another sequence of the same .seqj: the target's tracks are inlined under the reference track's
/// name ("Earth/Body") in its place, on the target's own clock starting at the clip's start. Same rules as FRB2's
/// SequenceReferences. Source: Walk (two 0.25 s frames, loops, frame 0 raises "Step"), Spark (one 0.5 s frame).
/// </summary>
public class SequencePlayerReferenceTests
{
    static AnimationChainList Source()
    {
        var walk = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Walk", Loop = true };
        var step = new AnimationFrame { FrameLength = 0.25f };
        step.Events.Add(new AnimationFrameEvent { Name = "Step" });
        walk.Add(step);
        walk.Add(new AnimationFrame { FrameLength = 0.25f });
        var spark = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Spark", Loop = true };
        spark.Add(new AnimationFrame { FrameLength = 0.5f });
        return new AnimationChainList { walk, spark };
    }

    static SequenceTrackSave Chain(string name, string chain, double start = 0, double? length = null)
    {
        var track = new SequenceTrackSave { Name = name };
        track.Chains.Add(new SequenceChainReferenceSave { Source = "s", Chain = chain, Start = start, Length = length });
        return track;
    }

    static SequenceTrackSave Ref(string name, string sequence, double start = 0, double? length = null, bool visible = true) =>
        new() { Name = name, Visible = visible, References = [new SequenceReferenceSave { Sequence = sequence, Start = start, Length = length }] };
    static SequenceSave Sequence(string name, bool loop, params SequenceTrackSave[] tracks)
    {
        var sequence = new SequenceSave { Name = name, Loop = loop };
        sequence.Tracks.AddRange(tracks);
        return sequence;
    }

    static SequencePlayer Player(params SequenceSave[] sequences)
    {
        var file = new SequenceListSave();
        file.Sequences.AddRange(sequences);
        return new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => Source()));
    }

    [Fact]
    public void NestedReference_InlinesInTrackOrder_AndMapsTheClockThroughEveryClip()
    {
        // Inner: 0.5 s of Walk, looping. Mid plays Inner twice from 0.5 s (ends at 1.5). Outer plays Mid once from 1 s (ends at 2.5).
        var player = Player(
            Sequence("Inner", true, Chain("Body", "Walk")),
            Sequence("Mid", true, Ref("Dust", "Inner", start: 0.5, length: 1.0)),
            Sequence("Outer", false, Chain("Front", "Spark"), Ref("Earth", "Mid", start: 1), Chain("Back", "Spark")));
        var outer = player["Outer"];

        outer.Play();

        outer.Tracks.Select(track => track.Name).ShouldBe(new[] { "Front", "Earth/Dust/Body", "Back" });
        outer.Length.ShouldBe(2.5, 1e-6);
        var body = outer.Tracks["Earth/Dust/Body"].Sprite;

        outer.SetTime(1.2); // Mid at 0.2: Inner's clip hasn't started
        body.Visible.ShouldBeFalse();
        outer.SetTime(1.8); // Mid 0.8, Inner 0.3
        body.Visible.ShouldBeTrue();
        body.CurrentFrameIndex.ShouldBe(1);
        outer.SetTime(2.1); // Mid 1.1, Inner 0.6 wraps to 0.1
        body.CurrentFrameIndex.ShouldBe(0);
        outer.SetTime(2.6); // Outer's clip on Mid has ended
        body.Visible.ShouldBeFalse();
    }

    [Fact]
    public void LoopOffTarget_PlaysOnceThenHoldsItsFinalPoseUntilTheClipEnds()
    {
        var player = Player(
            Sequence("Once", false, Chain("Body", "Walk")),
            Sequence("Outer", false, Ref("Ref", "Once", length: 1.5)));
        var outer = player["Outer"];
        outer.Play();
        var body = outer.Tracks["Ref/Body"].Sprite;

        outer.Update(0.6);
        body.Visible.ShouldBeTrue();
        body.CurrentFrameIndex.ShouldBe(1);
        outer.Update(0.6); // 1.2 s: a looping target would be back on frame 0
        body.Visible.ShouldBeTrue();
        body.CurrentFrameIndex.ShouldBe(1);
        body.Animate.ShouldBeFalse();
        outer.Update(0.4); // 1.6 s: the clip (3 cycles of 0.5 s) has ended
        body.Visible.ShouldBeFalse();
    }

    [Fact]
    public void LoopingTarget_RestartsEachCycle_AndRaisesItsEventsAgain()
    {
        var player = Player(
            Sequence("Inner", true, Chain("Body", "Walk")),
            Sequence("Outer", false, Ref("Ref", "Inner", length: 1.5)));
        var outer = player["Outer"];
        var raised = new List<string>();
        outer.EventRaised += (_, name, _) => raised.Add(name);

        outer.Play();
        outer.Update(0.3);
        outer.Update(0.3); // 0.6 s: the target wrapped to 0.1
        outer.Update(0.45); // 1.05 s: wrapped again, to 0.05

        raised.ShouldBe(new[] { "Step", "Step", "Step" });
        outer.Tracks[0].Sprite.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void AnEventFromAReferencedSequence_ReportsTheOuterSequencesTrack()
    {
        var player = Player(
            Sequence("Inner", true, Chain("Body", "Walk")),
            Sequence("Outer", false, Ref("Ref", "Inner")));
        var outer = player["Outer"];
        var tracks = new List<SequenceTrack>();
        outer.EventRaised += (track, _, _) => tracks.Add(track);

        outer.Play();

        var raiser = tracks.ShouldHaveSingleItem();
        raiser.ShouldBeSameAs(outer.Tracks["Ref/Body"]);
        raiser.Sprite.Visible.ShouldBeTrue();
        player["Inner"].Tracks["Body"].Sprite.Visible.ShouldBeFalse();
    }

    [Fact]
    public void MissingTarget_PlaysNothingAndAddsNoLength()
    {
        var player = Player(Sequence("Outer", false, Chain("Fx", "Spark"), Ref("Ghost", "NoSuchSequence", length: 4)));
        var outer = player["Outer"];

        outer.Play();

        outer.Tracks.Select(track => track.Name).ShouldBe(new[] { "Fx" });
        outer.Length.ShouldBe(0.5, 1e-6);
    }

    [Fact]
    public void Cycle_TheReferenceThatClosesItPlaysNothing()
    {
        var a = Sequence("A", true, Chain("Body", "Walk"), Ref("ToB", "B", length: 1.0));
        var b = Sequence("B", true, Chain("Fx", "Spark"), Ref("ToA", "A"));
        var player = Player(a, b);

        SequenceReferences.FindCycle(new[] { a, b }).ShouldBe(new[] { "A", "B", "A" });

        // From A, the reference to B closes the cycle (B plays A back), so A plays only its own track.
        player["A"].Tracks.Select(track => track.Name).ShouldBe(new[] { "Body" });
        player["A"].Play();
        player["A"].Length.ShouldBe(0.5, 1e-6);
        player["B"].Tracks.Select(track => track.Name).ShouldBe(new[] { "Fx" });
    }

    [Fact]
    public void HiddenReferenceTrack_HidesEveryTrackItInlines_UntilTurnedOn()
    {
        var player = Player(
            Sequence("Inner", true, Chain("Body", "Walk"), Chain("Shadow", "Spark")),
            Sequence("Outer", false, Ref("Earth", "Inner", visible: false)));
        var outer = player["Outer"];
        outer.Play();
        outer.SetTime(0.1);

        outer.Tracks["Earth/Body"].Visible.ShouldBeFalse();
        outer.Tracks["Earth/Body"].Sprite.Visible.ShouldBeFalse();
        outer.Tracks["Earth/Shadow"].Sprite.Visible.ShouldBeFalse();

        outer.Tracks["Earth/Body"].Visible = true;

        outer.Tracks["Earth/Body"].Sprite.Visible.ShouldBeTrue();
        outer.Tracks["Earth/Shadow"].Sprite.Visible.ShouldBeFalse();
    }

    [Fact]
    public void Play_RestartsTheTargetWithTheHolder()
    {
        var player = Player(
            Sequence("Inner", false, Chain("Body", "Walk")),
            Sequence("Outer", false, Ref("Ref", "Inner", start: 0.5)));
        var outer = player["Outer"];
        outer.Play();
        outer.SetTime(0.8);
        outer.Tracks[0].Sprite.CurrentFrameIndex.ShouldBe(1);

        outer.Play();

        outer.Tracks[0].Sprite.Visible.ShouldBeFalse();
        outer.SetTime(0.6);
        outer.Tracks[0].Sprite.CurrentFrameIndex.ShouldBe(0);
    }
}
