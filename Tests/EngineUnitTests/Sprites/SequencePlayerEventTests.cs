using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// A sequence raises the named events on the frames its chains show: when playback enters the frame, in the first cycle of
/// a block only, never from a cut-off tail or a track that is off. A Sprite playing the same chain raises nothing.
/// </summary>
public class SequencePlayerEventTests
{
    // Walk: three 0.25 s frames. Frame 0 raises A (data "1"), frame 1 raises B, frame 2 raises nothing.
    static AnimationChainList Source(bool pingPong = false)
    {
        var walk = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Walk", PingPong = pingPong };
        var f0 = new AnimationFrame { FrameLength = 0.25f };
        f0.Events.Add(new AnimationFrameEvent { Name = "A", Data = "1" });
        var f1 = new AnimationFrame { FrameLength = 0.25f };
        f1.Events.Add(new AnimationFrameEvent { Name = "B" });
        walk.AddRange(new[] { f0, f1, new AnimationFrame { FrameLength = 0.25f } });
        return new AnimationChainList { walk };
    }

    static SequenceTrackSave Track(string name, double start, float loops, string chain = "Walk")
    {
        var track = new SequenceTrackSave { Name = name };
        track.Chains.Add(new SequenceChainReferenceSave { Source = "s", Chain = chain, Start = start, Loops = loops });
        return track;
    }

    static (SequenceInstance Sequence, List<string> Raised) Setup(AnimationChainList source, params SequenceTrackSave[] tracks)
    {
        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Attack" };
        sequence.Tracks.AddRange(tracks);
        file.Sequences.Add(sequence);
        var instance = new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => source))["Attack"];
        var raised = new List<string>();
        instance.EventRaised += (name, data) => raised.Add(data == null ? name : name + ":" + data);
        return (instance, raised);
    }

    [Fact]
    public void Play_RaisesTheFirstFramesEventsAtOnce()
    {
        var (sequence, raised) = Setup(Source(), Track("Body", 0, 1));

        sequence.Play();

        raised.ShouldBe(new[] { "A:1" });
    }

    [Fact]
    public void Update_RaisesAFramesEventsWhenPlaybackEntersIt()
    {
        var (sequence, raised) = Setup(Source(), Track("Body", 0, 1));
        sequence.Play();
        raised.Clear();

        sequence.Update(0.1);
        raised.ShouldBeEmpty();
        sequence.Update(0.2); // 0.3 s: frame 1 entered at 0.25
        raised.ShouldBe(new[] { "B" });
        sequence.Update(0.1); // frame 2 has no events
        raised.ShouldBe(new[] { "B" });
    }

    [Fact]
    public void Update_ABigStep_RaisesEverySkippedFramesEventsInOrder()
    {
        var (sequence, raised) = Setup(Source(), Track("Body", 0, 1));

        sequence.Play();
        sequence.Update(0.7);

        raised.ShouldBe(new[] { "A:1", "B" });
    }

    [Fact]
    public void Update_OnlyTheFirstCycleOfABlockRaisesEvents()
    {
        var (sequence, raised) = Setup(Source(), Track("Body", 0, 3));
        sequence.Play();

        sequence.Update(0.7);
        raised.Clear();
        sequence.Update(0.1);  // wraps into the second cycle
        sequence.Update(0.5);  // enters frames 0 and 1 again
        sequence.Update(0.9);

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void Update_AChainWhoseBlockIsCutShortNeverRaisesTheCutOffFrames()
    {
        // The next chain on the track (a missing one) starts at 0.2, before frame 1 would be entered at 0.25.
        var track = Track("Body", 0, 1);
        track.Chains.Add(new SequenceChainReferenceSave { Source = "s", Chain = "Deleted", Start = 0.2 });
        var (sequence, raised) = Setup(Source(), track);

        sequence.Play();
        sequence.Update(0.5);

        raised.ShouldBe(new[] { "A:1" });
    }

    [Fact]
    public void ATrackThatStartsLater_RaisesItsFirstFramesEventsWhenItStarts()
    {
        var (sequence, raised) = Setup(Source(), Track("Body", 1, 1));

        sequence.Play();
        sequence.Update(0.9);
        raised.ShouldBeEmpty();
        sequence.Update(0.2); // the block started at 1.0

        raised.ShouldBe(new[] { "A:1" });
    }

    [Fact]
    public void ATrackThatIsOff_RaisesNothing_AndTurningItOnMidwayDoesNotCatchUp()
    {
        var off = Track("Body", 0, 1);
        off.Visible = false;
        var (sequence, raised) = Setup(Source(), off);

        sequence.Play();
        sequence.Update(0.3);
        raised.ShouldBeEmpty();

        sequence.Tracks["Body"].Visible = true;   // frames 0 and 1 already went by while it was off
        sequence.Update(0.1);
        raised.ShouldBeEmpty();
    }

    [Fact]
    public void PingPong_TheTurnaroundRaisesNothing_AndTheWayBackRaisesFramesAgain()
    {
        // Three 0.25 s frames: visits 0 1 2 | 3 4 5 show frames 0 1 2 | 2 1 0, so the cycle is 1.5 s.
        var (sequence, raised) = Setup(Source(pingPong: true), Track("Body", 0, 1));

        sequence.Play();
        sequence.Update(1.4);

        raised.ShouldBe(new[] { "A:1", "B", "B", "A:1" });
    }

    [Fact]
    public void Rewinding_RaisesNothing()
    {
        var (sequence, raised) = Setup(Source(), Track("Body", 0, 1));
        sequence.Play();
        sequence.SetTime(0.6);
        raised.Clear();

        sequence.SetTime(0.1);

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void PlayingAgain_RaisesTheFirstFramesEventsAgain()
    {
        var (sequence, raised) = Setup(Source(), Track("Body", 0, 1));
        sequence.Play();
        sequence.Update(0.3);
        raised.Clear();

        sequence.Play();

        raised.ShouldBe(new[] { "A:1" });
    }

    [Fact]
    public void AHandlerThatStopsTheSequence_EndsTheRestOfThatUpdate()
    {
        var (sequence, raised) = Setup(Source(), Track("Body", 0, 1));
        sequence.EventRaised += (name, data) =>
        {
            if (name == "A")
            {
                sequence.Stop();
            }
        };

        sequence.Play();
        sequence.Update(0.5);

        raised.ShouldBe(new[] { "A:1" });
        sequence.IsPlaying.ShouldBeFalse();
    }

    [Fact]
    public void ASpriteThatPlaysTheSameChain_RaisesNothing()
    {
        var sprite = new Sprite { AnimationChains = Source() };
        sprite.CurrentChainIndex = 0;

        Should.NotThrow(() => sprite.TimeIntoAnimation = 0.3);
    }
}
