using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// SequencePlayer drives one Sprite per track. Sources are built in memory, so no textures or files are needed.
/// body: Walk (two 0.25 s frames, loops), Hold (two 0.25 s frames, Loop off). fx: Spark (one 0.5 s frame).
/// </summary>
public class SequencePlayerTests
{
    static AnimationChainList Body()
    {
        var list = new AnimationChainList();
        list.Add(Chain("Walk", loop: true, 0.25f, 0.25f));
        list.Add(Chain("Hold", loop: false, 0.25f, 0.25f));
        return list;
    }

    static AnimationChainList Fx()
    {
        var list = new AnimationChainList();
        list.Add(Chain("Spark", loop: true, 0.5f));
        return list;
    }

    static FlatRedBall.Graphics.Animation.AnimationChain Chain(string name, bool loop, params float[] lengths)
    {
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = name, Loop = loop };
        foreach (float length in lengths)
        {
            chain.Add(new AnimationFrame { FrameLength = length });
        }
        return chain;
    }

    static AnimationChainList? Resolve(string source) => source switch
    {
        "body.achj" => Body(),
        "fx.achj" => Fx(),
        _ => null,
    };

    static SequenceTrackSave Track(string name, int z, params (string Source, string Chain, double Start, float Loops)[] chains)
    {
        var track = new SequenceTrackSave { Name = name, Z = z };
        foreach (var (source, chain, start, loops) in chains)
        {
            track.Chains.Add(new SequenceChainReferenceSave { Source = source, Chain = chain, Start = start, Loops = loops });
        }
        return track;
    }

    static SequencePlayer Player(bool loop = false, params SequenceTrackSave[] tracks)
    {
        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Attack", Loop = loop };
        sequence.Tracks.AddRange(tracks);
        file.Sequences.Add(sequence);
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(file, Resolve!));
        player.Play("Attack").ShouldBeTrue();
        return player;
    }

    [Fact]
    public void Play_MakesOneSpriteAndPivotPerTrack_WithTheTrackOffsetOnThePivot()
    {
        var body = Track("Body", 1, ("body.achj", "Walk", 0, 1));
        body.X = 10;
        body.Y = -4;
        var player = Player(false, body, Track("Fx", 5, ("fx.achj", "Spark", 0, 1)));

        player.Sprites.Count.ShouldBe(2);
        player.Pivots[0].RelativeX.ShouldBe(10);
        player.Pivots[0].RelativeY.ShouldBe(-4);
        player.Sprites[0].Parent.ShouldBe(player.Pivots[0]);
        player.Pivots[0].Parent.ShouldBe(player.Owner);
    }

    [Fact]
    public void SetTime_HiddenBeforeItsChainStarts_VisibleDuring_HiddenAfter()
    {
        // Walk is 0.5 s, so this chain plays from 1 s to 1.5 s.
        var player = Player(false, Track("Body", 0, ("body.achj", "Walk", 1, 1)));

        player.SetTime(0.5);
        player.Sprites[0].Visible.ShouldBeFalse();
        player.SetTime(1.2);
        player.Sprites[0].Visible.ShouldBeTrue();
        player.Sprites[0].CurrentFrameIndex.ShouldBe(0);
        player.SetTime(1.3);
        player.Sprites[0].CurrentFrameIndex.ShouldBe(1);
        player.SetTime(1.5);
        player.Sprites[0].Visible.ShouldBeFalse();
    }

    [Fact]
    public void SetTime_LoopingChain_RepeatsAcrossTheBlock()
    {
        var player = Player(false, Track("Body", 0, ("body.achj", "Walk", 0, 3)));

        player.SetTime(0.6);

        player.Sprites[0].CurrentFrameIndex.ShouldBe(0);
        player.Sprites[0].TimeIntoAnimation.ShouldBe(0.1, 1e-4);
    }

    [Fact]
    public void SetTime_NonLoopingChain_PlaysOnceThenHoldsItsLastFrame_UntilTheBlockEnds()
    {
        var player = Player(false, Track("Body", 0, ("body.achj", "Hold", 0, 3)));

        player.SetTime(0.6);
        player.Sprites[0].CurrentFrameIndex.ShouldBe(1);
        player.SetTime(1.4);
        player.Sprites[0].CurrentFrameIndex.ShouldBe(1);
        player.Sprites[0].Visible.ShouldBeTrue();
        player.SetTime(1.5);
        player.Sprites[0].Visible.ShouldBeFalse();
    }

    [Fact]
    public void SetTime_ACutChain_SwitchesToTheNextChainAtItsStart()
    {
        // Walk asks for 2 s (four cycles) but Spark starts at 0.6 s on the same track.
        var player = Player(false, Track("Body", 0, ("body.achj", "Walk", 0, 4), ("fx.achj", "Spark", 0.6, 1)));

        player.SetTime(0.55);
        player.Sprites[0].CurrentChain.Name.ShouldBe("Walk");
        player.SetTime(0.65);
        player.Sprites[0].CurrentChain.Name.ShouldBe("Spark");
    }

    [Fact]
    public void SetTime_MissingSourceOrChain_HidesTheSpriteWithoutThrowing()
    {
        var player = Player(false,
            Track("NoSource", 0, ("gone.achj", "Walk", 0, 1)),
            Track("NoChain", 0, ("body.achj", "Deleted", 0, 1)));

        Should.NotThrow(() => player.SetTime(0.1));

        player.Sprites[0].Visible.ShouldBeFalse();
        player.Sprites[1].Visible.ShouldBeFalse();
    }

    [Fact]
    public void SetTime_AMissingChainStillCutsTheChainBeforeIt()
    {
        var player = Player(false, Track("Body", 0, ("body.achj", "Walk", 0, 4), ("body.achj", "Deleted", 0.6, 1)));

        player.SetTime(0.55);
        player.Sprites[0].Visible.ShouldBeTrue();
        player.SetTime(0.65);
        player.Sprites[0].Visible.ShouldBeFalse();
    }

    [Fact]
    public void SetTime_ATrackThatIsNotVisible_ShowsNothing()
    {
        var hidden = Track("Fx", 5, ("fx.achj", "Spark", 0, 1));
        hidden.Visible = false;
        var player = Player(false, hidden, Track("Body", 1, ("body.achj", "Walk", 0, 1)));

        player.SetTime(0.1);

        player.Sprites[0].Visible.ShouldBeFalse();
        player.Sprites[1].Visible.ShouldBeTrue();
    }

    [Fact]
    public void SetTime_AFlippedTrack_MirrorsTheFrameFlipAndOffsetAcrossTheTrack()
    {
        var body = Track("Body", 0, ("body.achj", "Walk", 0, 1));
        body.X = 10;
        body.FlipHorizontal = true;
        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Attack" };
        sequence.Tracks.Add(body);
        file.Sequences.Add(sequence);
        var list = Body();
        list["Walk"][0].RelativeX = 3;
        list["Walk"][0].FlipHorizontal = false;
        list["Walk"][1].FlipHorizontal = true;
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => list));
        player.Play("Attack");

        player.SetTime(0);
        player.Sprites[0].RelativeX.ShouldBe(-3);
        player.Sprites[0].FlipHorizontal.ShouldBeTrue();
        player.Pivots[0].RelativeX.ShouldBe(10);

        player.SetTime(0.3);
        player.Sprites[0].FlipHorizontal.ShouldBeFalse();
    }

    [Fact]
    public void SetTime_AnUnflippedTrack_LeavesTheSpritesOwnFlipAndOffsetAlone()
    {
        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Attack" };
        sequence.Tracks.Add(Track("Body", 0, ("body.achj", "Walk", 0, 1)));
        file.Sequences.Add(sequence);
        var list = Body();
        list["Walk"][0].RelativeX = 3;
        list["Walk"][0].FlipHorizontal = true;
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => list));
        player.Play("Attack");

        player.SetTime(0);

        player.Sprites[0].RelativeX.ShouldBe(3);
        player.Sprites[0].FlipHorizontal.ShouldBeTrue();
    }

    [Fact]
    public void SetTime_PastTheEnd_LoopsWhenTheSequenceLoops_AndHoldsHiddenWhenItDoesNot()
    {
        // Walk plays 0 to 0.5 s.
        var looping = Player(true, Track("Body", 0, ("body.achj", "Walk", 0, 1)));
        looping.SetTime(0.6);
        looping.Sprites[0].Visible.ShouldBeTrue();

        var once = Player(false, Track("Body", 0, ("body.achj", "Walk", 0, 1)));
        once.SetTime(0.6);
        once.Sprites[0].Visible.ShouldBeFalse();
    }

    [Fact]
    public void Update_AdvancesTime_AndFinishesANonLoopingSequenceAtItsLength()
    {
        var player = Player(false, Track("Body", 0, ("body.achj", "Walk", 0, 1)));

        player.Update(0.3);
        player.Time.ShouldBe(0.3, 1e-9);
        player.IsFinished.ShouldBeFalse();
        player.Update(0.3);
        player.IsFinished.ShouldBeTrue();
    }

    [Fact]
    public void Play_UnknownName_ReturnsFalseAndKeepsThePlayingSequence()
    {
        var player = Player(false, Track("Body", 0, ("body.achj", "Walk", 0, 1)));
        player.Update(0.2);

        player.Play("Nope").ShouldBeFalse();

        player.CurrentSequenceName.ShouldBe("Attack");
        player.Time.ShouldBe(0.2, 1e-9);
    }

    [Fact]
    public void Play_ASecondSequence_ReplacesTheTracksAndRestartsTime()
    {
        var file = new SequenceListSave();
        var one = new SequenceSave { Name = "One" };
        one.Tracks.Add(Track("A", 0, ("body.achj", "Walk", 0, 1)));
        var two = new SequenceSave { Name = "Two" };
        two.Tracks.Add(Track("A", 0, ("body.achj", "Walk", 0, 1)));
        two.Tracks.Add(Track("B", 0, ("fx.achj", "Spark", 0, 1)));
        file.Sequences.Add(one);
        file.Sequences.Add(two);
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(file, Resolve!));
        player.Play("One");
        player.Update(0.3);

        player.Play("Two");

        player.Sprites.Count.ShouldBe(2);
        player.Time.ShouldBe(0);
    }

    [Fact]
    public void FromFile_PlaysARealSeqjAndAchjFromDisk_AndASourceThatFailsToLoadIsMissing()
    {
        string directory = Path.Combine(Path.GetTempPath(), "seqj-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(directory, "body"));
        try
        {
            File.WriteAllText(Path.Combine(directory, "body", "body.achj"), """
                { "animationChains": [
                  { "name": "Walk", "loop": true, "frames": [ { "frameLength": 0.25 }, { "frameLength": 0.25 } ] },
                  { "name": "Hold", "loop": false, "frames": [ { "frameLength": 0.25 }, { "frameLength": 0.25 } ] } ] }
                """);
            File.WriteAllText(Path.Combine(directory, "boss.seqj"), """
                { "version": 1, "sources": [ "body/body.achj", "gone/gone.achj" ],
                  "sequences": [ { "name": "Attack", "tracks": [
                    { "name": "Body", "x": 8, "chains": [ { "source": "body/body.achj", "chain": "Hold", "start": 0, "loops": 3 } ] },
                    { "name": "Lost", "chains": [ { "source": "gone/gone.achj", "chain": "Walk", "start": 0 } ] } ] } ] }
                """);

            var player = new SequencePlayer(new PositionedObject(), SequenceList.FromFile(Path.Combine(directory, "boss.seqj"), "test"));
            player.Play("Attack").ShouldBeTrue();

            player.SetTime(0.6);
            player.Sprites[0].Visible.ShouldBeTrue();
            player.Sprites[0].CurrentFrameIndex.ShouldBe(1);
            player.Pivots[0].RelativeX.ShouldBe(8);
            player.Sprites[1].Visible.ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TwoPlayersOnOneSequenceList_ShareTheLoadedChains_AndPlayIndependently()
    {
        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Attack" };
        sequence.Tracks.Add(Track("Body", 0, ("body.achj", "Walk", 0, 3)));
        file.Sequences.Add(sequence);
        int loads = 0;
        var list = new SequenceList(file, source => { loads++; return Resolve(source); });
        var first = new SequencePlayer(new PositionedObject(), list);
        var second = new SequencePlayer(new PositionedObject(), list);
        first.Play("Attack");
        second.Play("Attack");

        first.SetTime(0.1);
        second.SetTime(0.3);

        loads.ShouldBe(1);
        first.Sprites[0].AnimationChains.ShouldBeSameAs(second.Sprites[0].AnimationChains);
        first.Sprites[0].CurrentFrameIndex.ShouldBe(0);
        second.Sprites[0].CurrentFrameIndex.ShouldBe(1);
    }

    [Fact]
    public void SetTime_MatchesTheClockAtEveryFrameBoundary()
    {
        // The Sprite does the frame lookup; the clock is the reference. They must agree, boundaries included.
        var player = Player(false, Track("Body", 0, ("body.achj", "Walk", 0, 4)));
        var chain = Body()["Walk"];

        for (double time = 0; time < 1.99; time += 0.05)
        {
            player.SetTime(time);

            player.Sprites[0].CurrentFrameIndex.ShouldBe(SequenceChainClock.Locate(chain, time).Frame, $"at {time}");
        }
    }
}
