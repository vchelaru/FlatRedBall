using System.Collections.Generic;
using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// A track whose name matches an entry of existingSprites drives that Sprite instead of making its own: the player never
/// attaches, adds, removes or reorders it.
/// </summary>
public class SequencePlayerBoundSpriteTests
{
    static AnimationChainList Source()
    {
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Loop", Loop = true };
        chain.Add(new AnimationFrame { FrameLength = 0.5f });
        return new AnimationChainList { chain };
    }

    static SequenceSave Sequence(string name, string track)
    {
        var sequence = new SequenceSave { Name = name, Loop = true };
        var trackSave = new SequenceTrackSave { Name = track };
        trackSave.Chains.Add(new SequenceChainReferenceSave { Source = "a.achj", Chain = "Loop", Loops = 1 });
        sequence.Tracks.Add(trackSave);
        return sequence;
    }

    [Fact]
    public void BoundTrack_UsesTheEntitysSprite_AndLeavesItsParentAlone()
    {
        var owner = new PositionedObject();
        var shadow = new Sprite { Visible = true };
        shadow.AttachTo(owner);
        shadow.RelativeZ = -0.1f;
        var file = new SequenceListSave();
        file.Sequences.Add(Sequence("Grow", "Shadow"));
        file.Sequences.Add(Sequence("Idle", "Shadow"));
        var player = new SequencePlayer(owner, new SequenceList(file, _ => Source()),
            new Dictionary<string, Sprite> { ["Shadow"] = shadow });

        player["Grow"].Tracks["Shadow"].IsBound.ShouldBeTrue();
        player["Grow"].Tracks["Shadow"].Sprite.ShouldBeSameAs(shadow);
        player["Idle"].Tracks["Shadow"].Sprite.ShouldBeSameAs(shadow);
        shadow.Visible.ShouldBeFalse();

        player["Grow"].Play();
        shadow.Visible.ShouldBeTrue();
        shadow.Parent.ShouldBeSameAs(owner);
        shadow.RelativeZ.ShouldBe(-0.1f);

        player["Grow"].Stop();
        shadow.Visible.ShouldBeFalse();

        // Removing the player leaves the entity's Sprite attached.
        player.RemoveFromManagers();
        shadow.Parent.ShouldBeSameAs(owner);
    }

    [Fact]
    public void UnmatchedTrack_StillGetsItsOwnSprite()
    {
        var file = new SequenceListSave();
        file.Sequences.Add(Sequence("Idle", "Body"));
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => Source()),
            new Dictionary<string, Sprite> { ["Other"] = new Sprite() });

        player["Idle"].Tracks["Body"].IsBound.ShouldBeFalse();
        player["Idle"].Tracks["Body"].Pivot.ShouldNotBeNull();
    }
}
