using EngineUnitTests.TestSupport;
using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// PlayAsync completes when a non-looping sequence reaches its end. It is canceled, not completed, when the sequence is
/// replaced or removed first, so code after the await never runs as though the sequence had finished.
/// </summary>
[Collection(nameof(EngineStatefulTestCollection))]
public class SequencePlayerAsyncTests
{
    public SequencePlayerAsyncTests()
    {
        EngineTestBootstrap.EnsureInitialized();
    }

    // "Attack" and "Other" each play Walk (two 0.25 s frames) once, so they last 0.5 s. "Loop" repeats forever.
    static SequencePlayer Player()
    {
        var walk = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Walk" };
        walk.Add(new AnimationFrame { FrameLength = 0.25f });
        walk.Add(new AnimationFrame { FrameLength = 0.25f });
        var source = new AnimationChainList { walk };

        var file = new SequenceListSave();
        foreach (string name in new[] { "Attack", "Other", "Loop", "Empty" })
        {
            var sequence = new SequenceSave { Name = name, Loop = name == "Loop" };
            if (name != "Empty")
            {
                var track = new SequenceTrackSave { Name = "Body" };
                track.Chains.Add(new SequenceChainReferenceSave { Source = "s", Chain = "Walk", Start = 0, Loops = 1 });
                sequence.Tracks.Add(track);
            }
            file.Sequences.Add(sequence);
        }
        return new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => source));
    }

    // One game frame: the player advances, then TimeManager checks the tasks waiting on it
    // (FlatRedBallServices.Update does the same at the end of a frame).
    static void Frame(SequencePlayer player, double seconds)
    {
        player.Update(seconds);
        TimeManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(seconds)));
        TimeManager.DoTaskLogic();
    }

    static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task PlayAsync_CompletesWhenTheSequenceReachesItsEnd()
    {
        var player = Player();

        var task = player.PlayAsync("Attack");
        Frame(player, 0.3);
        task.IsCompleted.ShouldBeFalse();
        Frame(player, 0.3);

        await task.WaitAsync(Wait);
        player.IsFinished.ShouldBeTrue();
    }

    [Fact]
    public async Task PlayAsync_ASequenceWithNothingToPlay_CompletesAtOnce()
    {
        var player = Player();

        await player.PlayAsync("Empty").WaitAsync(Wait);
    }

    [Fact]
    public async Task PlayAsync_ReplacedByAnotherPlay_IsCanceled()
    {
        var player = Player();
        var task = player.PlayAsync("Attack");
        Frame(player, 0.1);

        player.Play("Other");
        Frame(player, 0.1);

        await Should.ThrowAsync<OperationCanceledException>(() => task.WaitAsync(Wait));
    }

    [Fact]
    public async Task PlayAsync_RemovedFromManagersFirst_IsCanceled()
    {
        var player = Player();
        var task = player.PlayAsync("Attack");

        player.RemoveFromManagers();
        Frame(player, 0.1);

        await Should.ThrowAsync<OperationCanceledException>(() => task.WaitAsync(Wait));
    }

    [Fact]
    public async Task PlayAsync_TheTokenIsCanceled_IsCanceled()
    {
        var player = Player();
        using var source = new CancellationTokenSource();
        var task = player.PlayAsync("Attack", source.Token);

        source.Cancel();
        Frame(player, 0.1);

        await Should.ThrowAsync<OperationCanceledException>(() => task.WaitAsync(Wait));
    }

    [Fact]
    public async Task PlayAsync_PlayingTheNextSequenceRightAfterTheAwait_NeverLeavesTheSpritesHiddenAtDrawTime()
    {
        // A finished sequence hides its sprites, so a hand-off ("await the attack, then play the idle") could show a
        // gap if the code after the await ran a frame late. This runs the real frame order with the engine's own
        // synchronization context: player update, task logic, then queued continuations, then "draw".
        var previousContext = SynchronizationContext.Current;
        var context = new FlatRedBall.Managers.SingleThreadSynchronizationContext();
        try
        {
            var player = Player();
            int handOffs = 0;
            async Task RunAttackThenOtherForever()
            {
                while (true)
                {
                    await player.PlayAsync("Attack");
                    handOffs++;
                    // The next sequence starts in the continuation, as game code would write it.
                    await player.PlayAsync("Other");
                    handOffs++;
                }
            }
            _ = RunAttackThenOtherForever();

            int hiddenAtDrawTime = 0;
            for (int frame = 0; frame < 60; frame++)
            {
                player.Update(0.1);
                TimeManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
                TimeManager.DoTaskLogic();
                context.Update();

                // End of the frame is when the game draws.
                if (player.Sprites.Count > 0 && !player.Sprites[0].Visible)
                {
                    hiddenAtDrawTime++;
                }
            }

            handOffs.ShouldBeGreaterThan(4);
            hiddenAtDrawTime.ShouldBe(0);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task PlayAsync_ALoopingSequence_IsRefusedBecauseItNeverFinishes()
    {
        var player = Player();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => player.PlayAsync("Loop"));

        exception.Message.ShouldContain("Loop");
        player.CurrentSequenceName.ShouldBeNull();
    }

    [Fact]
    public async Task PlayAsync_UnknownName_Throws()
    {
        var player = Player();

        await Should.ThrowAsync<InvalidOperationException>(() => player.PlayAsync("Nope"));
    }
}
