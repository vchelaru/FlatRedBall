using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// SequencePlayer gives each sequence of a .seqj its own SequenceInstance with a Sprite per track. Sources are built in
/// memory, so no textures or files are needed.
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

    static SequenceListSave SeqFile(params (string Name, bool Loop, SequenceTrackSave[] Tracks)[] sequences)
    {
        var file = new SequenceListSave();
        foreach (var (name, loop, tracks) in sequences)
        {
            var sequence = new SequenceSave { Name = name, Loop = loop };
            sequence.Tracks.AddRange(tracks);
            file.Sequences.Add(sequence);
        }
        return file;
    }

    // A player whose file has one sequence, "Attack", already playing.
    static SequenceInstance Attack(bool loop = false, params SequenceTrackSave[] tracks)
    {
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(SeqFile(("Attack", loop, tracks)), Resolve!));
        var attack = player["Attack"];
        attack.Play();
        return attack;
    }

    [Fact]
    public void Constructing_MakesOneSpriteAndPivotPerTrack_HiddenUntilPlayed()
    {
        var body = Track("Body", 1, ("body.achj", "Walk", 0, 1));
        body.X = 10;
        body.Y = -4;
        var entity = new PositionedObject();
        var player = new SequencePlayer(entity, new SequenceList(SeqFile(("Attack", false, new[] { body, Track("Fx", 5, ("fx.achj", "Spark", 0, 1)) })), Resolve!));

        var tracks = player["Attack"].Tracks;

        tracks.Count.ShouldBe(2);
        tracks[0].Pivot.RelativeX.ShouldBe(10);
        tracks[0].Pivot.RelativeY.ShouldBe(-4);
        tracks[0].Sprite.Parent.ShouldBe(tracks[0].Pivot);
        tracks[0].Pivot.Parent.ShouldBe(entity);
        tracks[0].Sprite.Visible.ShouldBeFalse();
        tracks["Fx"].ShouldBeSameAs(tracks[1]);
    }

    [Fact]
    public void Tracks_TheSpriteIsTheSameObjectBeforeAndAfterEveryPlay()
    {
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Walk", 0, 1)));
        var sprite = attack.Tracks["Body"].Sprite;

        attack.Play();
        attack.Stop();
        attack.Play();

        attack.Tracks["Body"].Sprite.ShouldBeSameAs(sprite);
    }

    [Fact]
    public void SetTime_HiddenBeforeItsChainStarts_VisibleDuring_HiddenAfter()
    {
        // Walk is 0.5 s, so this chain plays from 1 s to 1.5 s.
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Walk", 1, 1)));
        var sprite = attack.Tracks[0].Sprite;

        attack.SetTime(0.5);
        sprite.Visible.ShouldBeFalse();
        attack.SetTime(1.2);
        sprite.Visible.ShouldBeTrue();
        sprite.CurrentFrameIndex.ShouldBe(0);
        attack.SetTime(1.3);
        sprite.CurrentFrameIndex.ShouldBe(1);
        attack.SetTime(1.5);
        sprite.Visible.ShouldBeFalse();
    }

    [Fact]
    public void SetTime_LoopingChain_RepeatsAcrossTheBlock()
    {
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Walk", 0, 3)));

        attack.SetTime(0.6);

        attack.Tracks[0].Sprite.CurrentFrameIndex.ShouldBe(0);
        attack.Tracks[0].Sprite.TimeIntoAnimation.ShouldBe(0.1, 1e-4);
    }

    [Fact]
    public void SetTime_NonLoopingChain_PlaysOnceThenHoldsItsLastFrame_UntilTheBlockEnds()
    {
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Hold", 0, 3)));
        var sprite = attack.Tracks[0].Sprite;

        attack.SetTime(0.6);
        sprite.CurrentFrameIndex.ShouldBe(1);
        attack.SetTime(1.4);
        sprite.CurrentFrameIndex.ShouldBe(1);
        sprite.Visible.ShouldBeTrue();
        attack.SetTime(1.5);
        sprite.Visible.ShouldBeFalse();
    }

    [Fact]
    public void SetTime_ACutChain_SwitchesToTheNextChainAtItsStart()
    {
        // Walk asks for 2 s (four cycles) but Spark starts at 0.6 s on the same track.
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Walk", 0, 4), ("fx.achj", "Spark", 0.6, 1)));
        var sprite = attack.Tracks[0].Sprite;

        attack.SetTime(0.55);
        sprite.CurrentChain.Name.ShouldBe("Walk");
        attack.SetTime(0.65);
        sprite.CurrentChain.Name.ShouldBe("Spark");
    }

    [Fact]
    public void SetTime_MissingSourceOrChain_HidesTheSpriteWithoutThrowing()
    {
        var attack = Attack(false,
            Track("NoSource", 0, ("gone.achj", "Walk", 0, 1)),
            Track("NoChain", 0, ("body.achj", "Deleted", 0, 1)));

        Should.NotThrow(() => attack.SetTime(0.1));

        attack.Tracks[0].Sprite.Visible.ShouldBeFalse();
        attack.Tracks[1].Sprite.Visible.ShouldBeFalse();
    }

    [Fact]
    public void SetTime_AMissingChainStillCutsTheChainBeforeIt()
    {
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Walk", 0, 4), ("body.achj", "Deleted", 0.6, 1)));

        attack.SetTime(0.55);
        attack.Tracks[0].Sprite.Visible.ShouldBeTrue();
        attack.SetTime(0.65);
        attack.Tracks[0].Sprite.Visible.ShouldBeFalse();
    }

    [Fact]
    public void Visible_AFilesVisibleFalseIsTheStart_AndTheTrackShowsNothingWhileItIsOff()
    {
        var off = Track("2_Fx", 5, ("fx.achj", "Spark", 0, 1));
        off.Visible = false;
        var attack = Attack(false, off, Track("1_Body", 1, ("body.achj", "Walk", 0, 1)));

        attack.SetTime(0.1);

        attack.Tracks["2_Fx"].Visible.ShouldBeFalse();
        attack.Tracks["2_Fx"].Sprite.Visible.ShouldBeFalse();
        attack.Tracks["1_Body"].Sprite.Visible.ShouldBeTrue();
    }

    [Fact]
    public void Visible_SettingATrackOnOrOff_ChangesItAtOnce()
    {
        var off = Track("2_Fx", 5, ("fx.achj", "Spark", 0, 1));
        off.Visible = false;
        var attack = Attack(false, Track("1_Body", 1, ("body.achj", "Walk", 0, 1)), off);
        attack.SetTime(0.1);

        attack.Tracks["2_Fx"].Visible = true;
        attack.Tracks["2_Fx"].Sprite.Visible.ShouldBeTrue();

        attack.Tracks["1_Body"].Visible = false;
        attack.Tracks["1_Body"].Sprite.Visible.ShouldBeFalse();
        attack.Time.ShouldBe(0.1, 1e-9);
    }

    [Fact]
    public void Visible_BeforeItsChainStarts_ShowsItWhenTheChainStarts()
    {
        var late = Track("Fx", 0, ("fx.achj", "Spark", 1, 1));
        late.Visible = false;
        var attack = Attack(false, late);

        attack.Tracks["Fx"].Visible = true;
        attack.SetTime(0.5);
        attack.Tracks["Fx"].Sprite.Visible.ShouldBeFalse();
        attack.SetTime(1.1);
        attack.Tracks["Fx"].Sprite.Visible.ShouldBeTrue();
    }

    [Fact]
    public void Visible_StaysWhatItWasSetTo_AcrossStopAndPlay()
    {
        var off = Track("2_Fx", 0, ("fx.achj", "Spark", 0, 1));
        off.Visible = false;
        var attack = Attack(false, off);
        attack.Tracks["2_Fx"].Visible = true;

        attack.Stop();
        attack.Play();

        attack.Tracks["2_Fx"].Visible.ShouldBeTrue();
        attack.Tracks["2_Fx"].Sprite.Visible.ShouldBeTrue();
    }

    [Fact]
    public void AnUnknownTrackOrSequenceName_ThrowsAndNamesIt()
    {
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(SeqFile(("Attack", false, new[] { Track("1_Body", 0, ("body.achj", "Walk", 0, 1)) })), Resolve!));

        Should.Throw<InvalidOperationException>(() => player["Attack"].Tracks["1_Bodyy"]).Message.ShouldContain("1_Bodyy");
        Should.Throw<InvalidOperationException>(() => player["Atack"]).Message.ShouldContain("Atack");
    }

    [Fact]
    public void SetTime_AFlippedTrack_MirrorsTheFrameFlipAndOffsetAcrossTheTrack()
    {
        var body = Track("Body", 0, ("body.achj", "Walk", 0, 1));
        body.X = 10;
        body.FlipHorizontal = true;
        var list = Body();
        list["Walk"][0].RelativeX = 3;
        list["Walk"][0].FlipHorizontal = false;
        list["Walk"][1].FlipHorizontal = true;
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(SeqFile(("Attack", false, new[] { body })), _ => list));
        var attack = player["Attack"];
        attack.Play();
        var track = attack.Tracks[0];

        attack.SetTime(0);
        track.Sprite.RelativeX.ShouldBe(-3);
        track.Sprite.FlipHorizontal.ShouldBeTrue();
        track.Pivot.RelativeX.ShouldBe(10);

        attack.SetTime(0.3);
        track.Sprite.FlipHorizontal.ShouldBeFalse();
    }

    [Fact]
    public void SetTime_AnUnflippedTrack_LeavesTheSpritesOwnFlipAndOffsetAlone()
    {
        var list = Body();
        list["Walk"][0].RelativeX = 3;
        list["Walk"][0].FlipHorizontal = true;
        var player = new SequencePlayer(new PositionedObject(),
            new SequenceList(SeqFile(("Attack", false, new[] { Track("Body", 0, ("body.achj", "Walk", 0, 1)) })), _ => list));
        var attack = player["Attack"];
        attack.Play();

        attack.SetTime(0);

        attack.Tracks[0].Sprite.RelativeX.ShouldBe(3);
        attack.Tracks[0].Sprite.FlipHorizontal.ShouldBeTrue();
    }

    [Fact]
    public void SetTime_PastTheEnd_LoopsWhenTheSequenceLoops_AndHoldsHiddenWhenItDoesNot()
    {
        // Walk plays 0 to 0.5 s.
        var looping = Attack(true, Track("Body", 0, ("body.achj", "Walk", 0, 1)));
        looping.SetTime(0.6);
        looping.Tracks[0].Sprite.Visible.ShouldBeTrue();

        var once = Attack(false, Track("Body", 0, ("body.achj", "Walk", 0, 1)));
        once.SetTime(0.6);
        once.Tracks[0].Sprite.Visible.ShouldBeFalse();
    }

    [Fact]
    public void Update_AdvancesTime_AndFinishesANonLoopingSequenceAtItsLength()
    {
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Walk", 0, 1)));

        attack.Update(0.3);
        attack.Time.ShouldBe(0.3, 1e-9);
        attack.IsFinished.ShouldBeFalse();
        attack.Update(0.3);
        attack.IsFinished.ShouldBeTrue();
        attack.IsPlaying.ShouldBeTrue();
    }

    [Fact]
    public void Play_AgainRestartsFromTimeZero()
    {
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Walk", 0, 1)));
        attack.Update(0.4);

        attack.Play();

        attack.Time.ShouldBe(0);
        attack.Tracks[0].Sprite.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void Stop_HidesTheSprites_AndNothingAdvancesUntilItPlaysAgain()
    {
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Walk", 0, 3)));
        attack.Update(0.1);

        attack.Stop();
        attack.Update(0.1);

        attack.IsPlaying.ShouldBeFalse();
        attack.Tracks[0].Sprite.Visible.ShouldBeFalse();
        attack.Time.ShouldBe(0.1, 1e-9);
    }

    [Fact]
    public void PlayingOneSequence_NeverStopsAnother()
    {
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(SeqFile(
            ("Idle", true, new[] { Track("Body", 0, ("body.achj", "Walk", 0, 1)) }),
            ("Wings", true, new[] { Track("Wing", 0, ("fx.achj", "Spark", 0, 1)) })), Resolve!));

        player["Idle"].Play();
        player["Wings"].Play();
        player.Update(0.1);

        player["Idle"].IsPlaying.ShouldBeTrue();
        player["Wings"].IsPlaying.ShouldBeTrue();
        player["Idle"].Time.ShouldBe(0.1, 1e-9);
        player["Wings"].Time.ShouldBe(0.1, 1e-9);
        player["Idle"].Tracks[0].Sprite.Visible.ShouldBeTrue();
        player["Wings"].Tracks[0].Sprite.Visible.ShouldBeTrue();

        player["Idle"].Stop();
        player["Wings"].Tracks[0].Sprite.Visible.ShouldBeTrue();
    }

    [Fact]
    public void RemoveFromManagers_StopsEverything_AndThePlayerCantPlayAgain()
    {
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(SeqFile(
            ("Attack", false, new[] { Track("Body", 0, ("body.achj", "Walk", 0, 1)) })), Resolve!));
        player["Attack"].Play();

        player.RemoveFromManagers();

        player["Attack"].IsPlaying.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => player["Attack"].Play());
    }

    [Fact]
    public void TwoPlayersOnOneSequenceList_ShareTheLoadedChains_AndPlayIndependently()
    {
        var file = SeqFile(("Attack", false, new[] { Track("Body", 0, ("body.achj", "Walk", 0, 3)) }));
        int loads = 0;
        var list = new SequenceList(file, source => { loads++; return Resolve(source); });
        var first = new SequencePlayer(new PositionedObject(), list)["Attack"];
        var second = new SequencePlayer(new PositionedObject(), list)["Attack"];
        first.Play();
        second.Play();

        first.SetTime(0.1);
        second.SetTime(0.3);

        loads.ShouldBe(1);
        first.Tracks[0].Sprite.AnimationChains.ShouldBeSameAs(second.Tracks[0].Sprite.AnimationChains);
        first.Tracks[0].Sprite.CurrentFrameIndex.ShouldBe(0);
        second.Tracks[0].Sprite.CurrentFrameIndex.ShouldBe(1);
    }

    [Fact]
    public void FromFile_PlaysARealSeqjAndAchjFromDisk_AndASourceThatFailsToLoadIsMissing()
    {
        string directory = Path.Combine(Path.GetTempPath(), "seqj-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(directory, "body"));
        try
        {
            System.IO.File.WriteAllText(Path.Combine(directory, "body", "body.achj"), """
                { "animationChains": [
                  { "name": "Walk", "loop": true, "frames": [ { "frameLength": 0.25 }, { "frameLength": 0.25 } ] },
                  { "name": "Hold", "loop": false, "frames": [ { "frameLength": 0.25 }, { "frameLength": 0.25 } ] } ] }
                """);
            System.IO.File.WriteAllText(Path.Combine(directory, "boss.seqj"), """
                { "version": 1, "sources": [ "body/body.achj", "gone/gone.achj" ],
                  "sequences": [ { "name": "Attack", "tracks": [
                    { "name": "Body", "x": 8, "chains": [ { "source": "body/body.achj", "chain": "Hold", "start": 0, "loops": 3 } ] },
                    { "name": "Lost", "chains": [ { "source": "gone/gone.achj", "chain": "Walk", "start": 0 } ] } ] } ] }
                """);

            var player = new SequencePlayer(new PositionedObject(), SequenceList.FromFile(Path.Combine(directory, "boss.seqj"), "test"));
            var attack = player["Attack"];
            attack.Play();

            attack.SetTime(0.6);
            attack.Tracks["Body"].Sprite.Visible.ShouldBeTrue();
            attack.Tracks["Body"].Sprite.CurrentFrameIndex.ShouldBe(1);
            attack.Tracks["Body"].Pivot.RelativeX.ShouldBe(8);
            attack.Tracks["Lost"].Sprite.Visible.ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SetTime_MatchesTheClockAtEveryFrameBoundary()
    {
        // The Sprite does the frame lookup; the clock is the reference. They must agree, boundaries included.
        var attack = Attack(false, Track("Body", 0, ("body.achj", "Walk", 0, 4)));
        var chain = Body()["Walk"];

        for (double time = 0; time < 1.99; time += 0.05)
        {
            attack.SetTime(time);

            attack.Tracks[0].Sprite.CurrentFrameIndex.ShouldBe(SequenceChainClock.Locate(chain, time).Frame, $"at {time}");
        }
    }
}
