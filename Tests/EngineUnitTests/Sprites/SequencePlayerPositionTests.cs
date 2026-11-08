using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// The player applies frames during entity activity, after the managers have updated dependencies for the frame. The
/// Sprite's world position must follow the new frame's offset right away, not a frame later than its texture.
/// </summary>
public class SequencePlayerPositionTests
{
    [Fact]
    public void ANewFramesOffsetReachesTheWorldPositionInTheSameFrame()
    {
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Bob", Loop = true };
        chain.Add(new AnimationFrame { FrameLength = 0.5f, RelativeY = 3 });
        chain.Add(new AnimationFrame { FrameLength = 0.5f, RelativeY = 7 });
        var animations = new AnimationChainList { chain };

        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Idle", Loop = true };
        var track = new SequenceTrackSave { Name = "Body" };
        track.Chains.Add(new SequenceChainReferenceSave { Source = "body.achj", Chain = "Bob", Start = 0, Loops = 1 });
        sequence.Tracks.Add(track);
        file.Sequences.Add(sequence);

        var owner = new PositionedObject { Y = 100 };
        var player = new SequencePlayer(owner, new SequenceList(file, _ => animations));
        var idle = player["Idle"];
        idle.Play();
        var sprite = idle.Tracks["Body"].Sprite;
        sprite.Y.ShouldBe(103);

        // The managers have already updated this Sprite for the frame by the time entity activity runs.
        sprite.LastDependencyUpdate = TimeManager.CurrentTime;

        idle.SetTime(0.6);

        sprite.RelativeY.ShouldBe(7);
        sprite.Y.ShouldBe(107);
    }
}
