using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// A reference track holds several clips, placed like chain blocks: ordered by start, each lasting its length (one run of
/// its target when unset) or until the next clip starts, hidden outside every clip. Same-named tracks of different clips
/// are one sprite. Mirrors FRB2's SequenceMultiClipTests. Source: Walk (two 0.25 s frames, loops), Spark (one 0.5 s frame).
/// </summary>
public class SequencePlayerMultiClipTests
{
    static AnimationChainList Source()
    {
        var walk = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Walk", Loop = true };
        walk.Add(new AnimationFrame { FrameLength = 0.25f });
        walk.Add(new AnimationFrame { FrameLength = 0.25f });
        var spark = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Spark", Loop = true };
        spark.Add(new AnimationFrame { FrameLength = 0.5f });
        return new AnimationChainList { walk, spark };
    }

    static SequenceTrackSave Chain(string name, string chain, double? length = null, float x = 0)
    {
        var track = new SequenceTrackSave { Name = name, X = x };
        track.Chains.Add(new SequenceChainReferenceSave { Source = "s", Chain = chain, Length = length });
        return track;
    }

    static SequenceTrackSave Clips(string name, params SequenceReferenceSave[] clips) =>
        new() { Name = name, References = clips.ToList() };

    static SequenceReferenceSave Clip(string sequence, double start, double? length = null) =>
        new() { Sequence = sequence, Start = start, Length = length };

    static SequenceSave Sequence(string name, bool loop, params SequenceTrackSave[] tracks)
    {
        var sequence = new SequenceSave { Name = name, Loop = loop };
        sequence.Tracks.AddRange(tracks);
        return sequence;
    }

    // "A" walks its Body at x 0; "B" sparks its Body at x 5; "C" has a Wing and a Body.
    static SequenceInstance Play(SequenceTrackSave holderTrack, bool loop = false, SequenceTrackSave ownTrack = null)
    {
        var file = new SequenceListSave();
        var holder = ownTrack == null ? Sequence("P", loop, holderTrack) : Sequence("P", loop, holderTrack, ownTrack);
        file.Sequences.Add(holder);
        file.Sequences.Add(Sequence("A", true, Chain("Body", "Walk")));
        file.Sequences.Add(Sequence("B", true, Chain("Body", "Spark", x: 5)));
        file.Sequences.Add(Sequence("C", true, Chain("Wing", "Spark"), Chain("Body", "Spark")));
        var instance = new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => Source()))["P"];
        instance.Play();
        return instance;
    }

    [Fact]
    public void NextClip_CutsThePreviousOne_AndSwitchesTheSharedSprite()
    {
        var instance = Play(Clips("Earth", Clip("A", 0, length: 2), Clip("B", 1)));
        var track = instance.Tracks.ShouldHaveSingleItem();
        track.Name.ShouldBe("Earth/Body");

        instance.SetTime(0.3);
        track.Sprite.Visible.ShouldBeTrue();
        track.Sprite.CurrentChain.Name.ShouldBe("Walk");
        track.Sprite.CurrentFrameIndex.ShouldBe(1);
        track.Pivot.RelativeX.ShouldBe(0);

        instance.SetTime(1.1); // A's clip was cut at 1; B's target starts at its clip's start
        track.Sprite.Visible.ShouldBeTrue();
        track.Sprite.CurrentChain.Name.ShouldBe("Spark");
        track.Pivot.RelativeX.ShouldBe(5);

        instance.SetTime(0.3); // back on A
        track.Sprite.CurrentChain.Name.ShouldBe("Walk");
        track.Pivot.RelativeX.ShouldBe(0);
    }

    [Fact]
    public void Length_IsTheLatestClipEnd()
    {
        Play(Clips("Earth", Clip("A", 0, length: 2), Clip("B", 1))).Length.ShouldBe(1.5, 1e-6);
        Play(Clips("Earth", Clip("B", 3), Clip("A", 0, length: 2))).Length.ShouldBe(3.5, 1e-6);
    }

    [Fact]
    public void GapBetweenClips_ShowsNothing()
    {
        var instance = Play(Clips("Earth", Clip("A", 0), Clip("B", 1)));
        var sprite = instance.Tracks["Earth/Body"].Sprite;

        instance.SetTime(0.4);
        sprite.Visible.ShouldBeTrue();
        instance.SetTime(0.7); // A's one run ended at 0.5
        sprite.Visible.ShouldBeFalse();
        instance.SetTime(1.2);
        sprite.Visible.ShouldBeTrue();
        sprite.CurrentChain.Name.ShouldBe("Spark");
    }

    [Fact]
    public void MissingTarget_PlaysNothingButCutsTheClipBeforeIt()
    {
        // The holder's own 3 s track keeps it running past the missing clip.
        var instance = Play(Clips("Earth", Clip("A", 0, length: 2), Clip("Nope", 1)), ownTrack: Chain("Fx", "Spark", length: 3));
        var sprite = instance.Tracks["Earth/Body"].Sprite;

        instance.SetTime(0.6);
        sprite.Visible.ShouldBeTrue();
        instance.SetTime(1.2);
        sprite.Visible.ShouldBeFalse();
    }

    [Fact]
    public void SameNamedTracksOfDifferentClips_AreOneTrack_AtTheFirstOnesPlace()
    {
        var instance = Play(Clips("Earth", Clip("A", 0), Clip("C", 1)));

        instance.Tracks.Select(track => track.Name).ShouldBe(new[] { "Earth/Body", "Earth/Wing" });

        instance.SetTime(1.2);
        instance.Tracks["Earth/Body"].Sprite.Visible.ShouldBeTrue();
        instance.Tracks["Earth/Body"].Sprite.CurrentChain.Name.ShouldBe("Spark");
        instance.Tracks["Earth/Wing"].Sprite.Visible.ShouldBeTrue();
        instance.SetTime(0.2);
        instance.Tracks["Earth/Wing"].Sprite.Visible.ShouldBeFalse();
    }

    [Theory]
    // The holder loops every 1.2 s; at the wrap the second clip plays A again, so the first clip resumes 0.6 s into A:
    // 1.35 is 0.15 into pass 1, A's clock 0.75, wrapped by its 0.5 s to 0.25, frame 1. Not continuous: 0.15, frame 0.
    [InlineData(true, "A", 1)]
    [InlineData(false, "A", 0)]
    // A different sequence plays at the wrap, so the first clip restarts.
    [InlineData(true, "B", 0)]
    public void Continuous_CarriesTheFirstClip_OnlyWhenTheClipAtTheWrapPlaysTheSameSequence(bool continuous, string secondTarget, int expectedFrame)
    {
        var track = Clips("Legs", Clip("A", 0, length: 0.6), Clip(secondTarget, 0.6, length: 0.6));
        track.Continuous = continuous;
        var instance = Play(track, loop: true);

        instance.Update(1.35);

        instance.Tracks["Legs/Body"].Sprite.CurrentFrameIndex.ShouldBe(expectedFrame);
    }
}
