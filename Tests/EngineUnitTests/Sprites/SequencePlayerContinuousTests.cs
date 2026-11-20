using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// A continuous track keeps its chain's playback phase across a looping sequence's wrap when the chain playing at the
/// wrap is the same chain as its first block (starting at 0). Walk is a 0.5 s cycle (two 0.25 s frames); Hold is the same
/// length with Loop off.
/// </summary>
public class SequencePlayerContinuousTests
{
    static AnimationChainList Body()
    {
        var list = new AnimationChainList();
        list.Add(Chain("Walk", loop: true));
        list.Add(Chain("Hold", loop: false));
        return list;
    }

    static FlatRedBall.Graphics.Animation.AnimationChain Chain(string name, bool loop)
    {
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = name, Loop = loop };
        chain.Add(new AnimationFrame { FrameLength = 0.25f });
        chain.Add(new AnimationFrame { FrameLength = 0.25f });
        return chain;
    }

    static SequenceTrackSave Track(bool continuous, params (string Chain, double Start, float Loops)[] chains)
    {
        var track = new SequenceTrackSave { Name = "Body", Continuous = continuous };
        foreach (var (chain, start, loops) in chains)
        {
            track.Chains.Add(new SequenceChainReferenceSave { Source = "body.achj", Chain = chain, Start = start, Loops = loops });
        }
        return track;
    }

    static SequenceInstance Looping(SequenceTrackSave track)
    {
        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Idle", Loop = true };
        sequence.Tracks.Add(track);
        file.Sequences.Add(sequence);
        var instance = new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => Body()))["Idle"];
        instance.Play();
        return instance;
    }

    [Fact]
    public void Update_ContinuousFractionalBlock_KeepsItsPhaseOverThreeWraps()
    {
        // Walk x 2.4 = 1.2 s, the whole sequence, so every wrap lands mid-cycle.
        var idle = Looping(Track(true, ("Walk", 0, 2.4f)));
        var sprite = idle.Tracks[0].Sprite;

        idle.Update(0.15);
        sprite.CurrentFrameIndex.ShouldBe(0);   // 0.15
        idle.Update(1.2);
        sprite.CurrentFrameIndex.ShouldBe(1);   // 1.35 -> 0.35 into the cycle
        idle.Update(1.2);
        sprite.CurrentFrameIndex.ShouldBe(0);   // 2.55 -> 0.05
        idle.Update(1.2);
        sprite.CurrentFrameIndex.ShouldBe(1);   // 3.75 -> 0.25
    }

    [Fact]
    public void Update_UnflaggedTrack_RestartsAtEveryWrap()
    {
        var idle = Looping(Track(false, ("Walk", 0, 2.4f)));
        var sprite = idle.Tracks[0].Sprite;

        idle.Update(1.35);

        sprite.CurrentFrameIndex.ShouldBe(0);   // restarted: 0.15 into the cycle
    }

    [Fact]
    public void Update_ContinuousTrack_ADifferentChainAtTheWrap_Restarts()
    {
        // Walk 0 to 0.5 s, then Hold x 1.4 = 0.7 s: Hold is playing when the 1.2 s sequence wraps.
        var idle = Looping(Track(true, ("Walk", 0, 1), ("Hold", 0.5, 1.4f)));
        var sprite = idle.Tracks[0].Sprite;

        idle.Update(1.3);

        sprite.CurrentChain.Name.ShouldBe("Walk");
        sprite.CurrentFrameIndex.ShouldBe(0);   // 0.1 s into Walk, not carried to 0.3
    }

    [Fact]
    public void SetTime_ResetsThePass_SoACarriedPhaseIsNotKept()
    {
        var idle = Looping(Track(true, ("Walk", 0, 2.4f)));
        var sprite = idle.Tracks[0].Sprite;
        idle.Update(1.35);

        idle.SetTime(0.15);

        sprite.CurrentFrameIndex.ShouldBe(0);
    }
}
