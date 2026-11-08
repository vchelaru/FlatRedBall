using System;
using System.Linq;
using EngineUnitTests.TestSupport;
using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// The player switches a Sprite's chain when a block starts or ends; between switches the Sprite animates itself in the
/// manager's update like any other Sprite. A frame here is the game's order: the Sprites animate, then the player updates.
/// body.achj: Walk (two 0.25 s frames, loops), Hold (two 0.25 s frames, Loop off).
/// </summary>
[Collection(nameof(EngineStatefulTestCollection))]
public class SequencePlayerAnimationTests
{
    static AnimationChainList Body()
    {
        var list = new AnimationChainList();
        list.Add(Chain("Walk", true));
        list.Add(Chain("Hold", false));
        return list;
    }

    static FlatRedBall.Graphics.Animation.AnimationChain Chain(string name, bool loop)
    {
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = name, Loop = loop };
        chain.Add(new AnimationFrame { FrameLength = 0.25f });
        chain.Add(new AnimationFrame { FrameLength = 0.25f });
        return chain;
    }

    static SequenceChainReferenceSave Ref(string chain, double start, float loops) =>
        new SequenceChainReferenceSave { Source = "body.achj", Chain = chain, Start = start, Loops = loops };

    static SequencePlayer Player(bool loop, params SequenceChainReferenceSave[] chains)
    {
        EngineTestBootstrap.EnsureInitialized();
        var animations = Body();
        var track = new SequenceTrackSave { Name = "Body" };
        track.Chains.AddRange(chains);
        var sequence = new SequenceSave { Name = "S", Loop = loop };
        sequence.Tracks.Add(track);
        var file = new SequenceListSave();
        file.Sequences.Add(sequence);
        return new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => animations));
    }

    static Sprite SpriteOf(SequencePlayer player) => player["S"].Tracks["Body"].Sprite;

    // One game frame: the manager animates the Sprites (step 1), then the entity updates the player (step 2).
    static void Frame(SequencePlayer player, double seconds)
    {
        TimeManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(seconds)));
        foreach (var track in player["S"].Tracks)
        {
            track.Sprite.AnimateSelf(TimeManager.CurrentTime);
        }
        player.Update(seconds);
    }

    static void Frames(SequencePlayer player, int count, double seconds = 0.1)
    {
        for (int i = 0; i < count; i++)
        {
            Frame(player, seconds);
        }
    }

    [Fact]
    public void Update_DoesNotWriteTheSpritesTimeBetweenChainSwitches()
    {
        var player = Player(false, Ref("Walk", 0, 4));
        player["S"].Play();
        var sprite = SpriteOf(player);

        player.Update(0.3);

        sprite.CurrentFrameIndex.ShouldBe(0);
        sprite.Animate.ShouldBeTrue();
    }

    [Fact]
    public void BetweenSwitches_TheSpriteAnimatesAndLoopsItself()
    {
        var player = Player(false, Ref("Walk", 0, 4));
        player["S"].Play();
        var sprite = SpriteOf(player);

        Frames(player, 3, 0.1);   // 0.3 s
        sprite.CurrentFrameIndex.ShouldBe(1);

        Frames(player, 3, 0.1);   // 0.6 s: wrapped past the 0.5 s cycle
        sprite.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void AMatchingNextBlock_SwitchesTheChain_AndRestartsIt()
    {
        var player = Player(false, Ref("Walk", 0, 1), Ref("Hold", 0.6, 1));
        player["S"].Play();
        var sprite = SpriteOf(player);
        sprite.CurrentChain.Name.ShouldBe("Walk");

        Frames(player, 5, 0.1);   // 0.5 s: Walk's block is over, nothing plays until 0.6
        sprite.Visible.ShouldBeFalse();

        Frames(player, 2, 0.1);   // 0.7 s: Hold started at 0.6
        sprite.Visible.ShouldBeTrue();
        sprite.CurrentChain.Name.ShouldBe("Hold");
        sprite.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void ABlockThatEnds_HidesTheSprite()
    {
        var player = Player(false, Ref("Walk", 0, 1));
        player["S"].Play();
        var sprite = SpriteOf(player);

        Frames(player, 4, 0.1);
        sprite.Visible.ShouldBeTrue();

        Frames(player, 2, 0.1);
        sprite.Visible.ShouldBeFalse();
    }

    [Fact]
    public void AChainWithLoopOff_HoldsItsLastFrame_InsteadOfLoopingWithTheSprite()
    {
        var player = Player(false, Ref("Hold", 0, 4));
        player["S"].Play();
        var sprite = SpriteOf(player);

        Frames(player, 7, 0.1);   // 0.7 s, past the 0.5 s cycle

        sprite.CurrentFrameIndex.ShouldBe(1);
        sprite.Animate.ShouldBeFalse();

        Frames(player, 5, 0.1);
        sprite.CurrentFrameIndex.ShouldBe(1);
    }

    [Fact]
    public void ALoopingSequenceWrapping_PutsTheSpriteBackAtTheStart()
    {
        var player = Player(true, Ref("Walk", 0, 1));
        player["S"].Play();
        var sprite = SpriteOf(player);

        Frames(player, 6, 0.1);   // 0.6 s: the 0.5 s sequence wrapped and is 0.1 s in again

        sprite.Visible.ShouldBeTrue();
        sprite.CurrentFrameIndex.ShouldBe(0);
        sprite.TimeIntoAnimation.ShouldBe(0.1, 0.02);
    }

    [Fact]
    public void SetTime_WritesTheExactTimeIntoTheChain()
    {
        var player = Player(false, Ref("Walk", 0, 4));
        player["S"].Play();

        player["S"].SetTime(0.3);

        SpriteOf(player).CurrentFrameIndex.ShouldBe(1);
    }

    [Fact]
    public void Speed_IsGivenToTheSprite()
    {
        var player = Player(false, Ref("Walk", 0, 4));
        player["S"].Speed = 2;
        player["S"].Play();

        SpriteOf(player).AnimationSpeed.ShouldBe(2);
    }
}
