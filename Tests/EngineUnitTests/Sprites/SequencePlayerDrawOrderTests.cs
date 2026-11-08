using System.Linq;
using EngineUnitTests.TestSupport;
using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// SequencePlayer.AddToManagers fixes the draw order once: the first track in the file draws on top and the last at the bottom, across sequences, with no Z involved.
/// </summary>
[Collection(nameof(EngineStatefulTestCollection))]
public class SequencePlayerDrawOrderTests
{
    static SequenceSave Sequence(string name, params string[] tracks)
    {
        var sequence = new SequenceSave { Name = name };
        foreach (var track in tracks)
        {
            sequence.Tracks.Add(new SequenceTrackSave { Name = track });
        }
        return sequence;
    }

    [Fact]
    public void AddToManagers_DrawsTracksInFileOrderAcrossSequences_AndLeavesZAlone()
    {
        EngineTestBootstrap.EnsureInitialized();
        var file = new SequenceListSave();
        file.Sequences.Add(Sequence("Wings", "WingLeft", "WingRight"));
        file.Sequences.Add(Sequence("Idle", "Body"));
        file.Sequences.Add(Sequence("Action", "Flame", "Moon"));
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => null));

        player.AddToManagers();
        try
        {
            var tracks = new[] { "Wings", "Idle", "Action" }.SelectMany(name => player[name].Tracks).ToList();
            var drawn = SpriteManager.OrderedByDistanceFromCameraSprites.ToList();

            var indexes = tracks.Select(track => drawn.IndexOf(track.Sprite)).ToList();
            indexes.ShouldAllBe(index => index >= 0);
            // The first track draws on top (latest in the list), the last at the bottom.
            indexes.ShouldBe(indexes.OrderByDescending(index => index).ToList());
            tracks.ShouldAllBe(track => track.Sprite.Z == 0);
        }
        finally
        {
            player.RemoveFromManagers();
        }
    }
}
