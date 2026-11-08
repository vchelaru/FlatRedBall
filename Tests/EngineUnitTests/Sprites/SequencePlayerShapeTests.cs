using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math.Geometry;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// SequencePlayer drives the entity's own shapes by name from the active frames' shapes. A shape named in some chain
/// of the playing sequence is "managed": enabled (in Collision) while an active frame names it, disabled otherwise.
/// A name with no matching entity shape is ignored, and an entity shape no chain names is never touched.
/// </summary>
public class SequencePlayerShapeTests
{
    class CollidableEntity : PositionedObject, ICollidable
    {
        public ShapeCollection Collision { get; } = new ShapeCollection();
        public HashSet<string> ItemsCollidedAgainst { get; } = new();
        public HashSet<string> LastFrameItemsCollidedAgainst { get; } = new();
        public HashSet<object> ObjectsCollidedAgainst { get; } = new();
        public HashSet<object> LastFrameObjectsCollidedAgainst { get; } = new();
    }

    static AxisAlignedRectangleSave RectSave(string name, float x, float y, float scaleX = 3, float scaleY = 4) =>
        new() { Name = name, X = x, Y = y, ScaleX = scaleX, ScaleY = scaleY };

    static AnimationFrame Frame(float length, params AxisAlignedRectangleSave[] rectangles)
    {
        var frame = new AnimationFrame { FrameLength = length };
        if (rectangles.Length > 0)
        {
            frame.ShapeCollectionSave = new ShapeCollectionSave();
            frame.ShapeCollectionSave.AxisAlignedRectangleSaves.AddRange(rectangles);
        }
        return frame;
    }

    static AnimationChainList Source(string name, params AnimationFrame[] frames)
    {
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = name };
        chain.AddRange(frames);
        return new AnimationChainList { chain };
    }

    static SequenceTrackSave Track(string chain, double start = 0, float loops = 1)
    {
        var track = new SequenceTrackSave { Name = chain };
        track.Chains.Add(new SequenceChainReferenceSave { Source = "s", Chain = chain, Start = start, Loops = loops });
        return track;
    }

    // Sequence "Attack": track 0 plays chain "Swing". Frame 0 (0.5 s) names Hitbox, frame 1 (0.5 s) names nothing.
    static (SequencePlayer Player, CollidableEntity Entity, AxisAlignedRectangle Hitbox, AxisAlignedRectangle Body)
        Setup(params SequenceTrackSave[] tracks)
    {
        var swing = Source("Swing", Frame(0.5f, RectSave("Hitbox", 5, 6)), Frame(0.5f));
        var entity = new CollidableEntity();
        var hitbox = new AxisAlignedRectangle { Name = "Hitbox" };
        var body = new AxisAlignedRectangle { Name = "Body" };
        foreach (var rectangle in new[] { hitbox, body })
        {
            rectangle.AttachTo(entity);
            entity.Collision.Add(rectangle);
        }

        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Attack" };
        sequence.Tracks.AddRange(tracks.Length > 0 ? tracks : new[] { Track("Swing", loops: 2) });
        file.Sequences.Add(sequence);
        var player = new SequencePlayer(entity, new SequenceList(file, _ => swing));
        player.Play("Attack").ShouldBeTrue();
        return (player, entity, hitbox, body);
    }

    [Fact]
    public void SetTime_AppliesTheActiveFramesShapeValuesToTheMatchingEntityShape()
    {
        var (player, _, hitbox, _) = Setup();

        player.SetTime(0.1);

        hitbox.RelativeX.ShouldBe(5);
        hitbox.RelativeY.ShouldBe(6);
        hitbox.ScaleX.ShouldBe(3);
        hitbox.ScaleY.ShouldBe(4);
    }

    [Fact]
    public void SetTime_AManagedShapeThatNoActiveFrameNames_LeavesCollision_AndComesBack()
    {
        var (player, entity, hitbox, _) = Setup();

        player.SetTime(0.1);
        entity.Collision.Contains(hitbox).ShouldBeTrue();
        player.IsShapeEnabled("Hitbox").ShouldBeTrue();

        player.SetTime(0.6); // frame 1 names nothing
        entity.Collision.Contains(hitbox).ShouldBeFalse();
        player.IsShapeEnabled("Hitbox").ShouldBeFalse();

        player.SetTime(1.1); // second cycle, frame 0 again
        entity.Collision.Contains(hitbox).ShouldBeTrue();
        player.IsShapeEnabled("Hitbox").ShouldBeTrue();
        hitbox.RelativeX.ShouldBe(5);
    }

    [Fact]
    public void SetTime_AnEntityShapeNoChainNames_IsNeverTouched()
    {
        var (player, entity, _, body) = Setup();
        body.RelativeX = 42;

        player.SetTime(0.1);
        player.SetTime(0.6);

        entity.Collision.Contains(body).ShouldBeTrue();
        body.RelativeX.ShouldBe(42);
        player.IsShapeEnabled("Body").ShouldBeTrue();
    }

    [Fact]
    public void SetTime_AFrameShapeWithNoMatchingEntityShape_IsIgnored()
    {
        var swing = Source("Swing", Frame(1f, RectSave("Ghost", 1, 1)));
        var entity = new CollidableEntity();
        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Attack" };
        sequence.Tracks.Add(Track("Swing"));
        file.Sequences.Add(sequence);
        var player = new SequencePlayer(entity, new SequenceList(file, _ => swing));

        player.Play("Attack");

        Should.NotThrow(() => player.SetTime(0.1));
        entity.Children.OfType<AxisAlignedRectangle>().ShouldBeEmpty();
        entity.Collision.AxisAlignedRectangles.Count.ShouldBe(0);
    }

    [Fact]
    public void SetTime_TwoTracksNameTheSameShape_TheFirstTrackWins()
    {
        var first = Source("First", Frame(1f, RectSave("Hitbox", 1, 1)));
        var second = Source("Second", Frame(1f, RectSave("Hitbox", 9, 9)));
        var entity = new CollidableEntity();
        var hitbox = new AxisAlignedRectangle { Name = "Hitbox" };
        hitbox.AttachTo(entity);
        entity.Collision.Add(hitbox);
        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Attack" };
        sequence.Tracks.Add(Track("First"));
        sequence.Tracks.Add(Track("Second"));
        // Each track reads its own source.
        sequence.Tracks[0].Chains[0].Source = "a";
        sequence.Tracks[1].Chains[0].Source = "b";
        file.Sequences.Add(sequence);
        var player = new SequencePlayer(entity, new SequenceList(file, source => source == "a" ? first : second));
        player.Play("Attack");

        player.SetTime(0.1);

        hitbox.RelativeX.ShouldBe(1);
    }

    [Fact]
    public void SetTime_ACutChainsShapesDoNotDrive_AndAHiddenTracksShapesDoNotEither()
    {
        // Swing asks for 2 cycles (2 s) but is cut at 0.3 s by a missing chain on the same track.
        var cut = Track("Swing", loops: 2);
        cut.Chains.Add(new SequenceChainReferenceSave { Source = "s", Chain = "Deleted", Start = 0.3 });
        var (player, entity, hitbox, _) = Setup(cut);

        player.SetTime(0.1);
        entity.Collision.Contains(hitbox).ShouldBeTrue();
        player.SetTime(0.4);
        entity.Collision.Contains(hitbox).ShouldBeFalse();

        var hidden = Track("Swing");
        hidden.Visible = false;
        var (player2, entity2, hitbox2, _) = Setup(hidden);
        player2.SetTime(0.1);
        entity2.Collision.Contains(hitbox2).ShouldBeFalse();
    }

    [Fact]
    public void Play_ASecondSequence_PutsDisabledShapesBack()
    {
        var (player, entity, hitbox, _) = Setup();
        player.SetTime(0.6);
        entity.Collision.Contains(hitbox).ShouldBeFalse();

        player.Play("Attack");
        // Playing again starts at 0, where Hitbox is named.
        entity.Collision.Contains(hitbox).ShouldBeTrue();
    }

    [Fact]
    public void RemoveFromManagers_PutsDisabledShapesBack_SoTheEntityCanCleanUpItsCollision()
    {
        var (player, entity, hitbox, _) = Setup();
        player.SetTime(0.6);
        entity.Collision.Contains(hitbox).ShouldBeFalse();

        player.RemoveFromManagers();

        entity.Collision.Contains(hitbox).ShouldBeTrue();
        player.IsShapeEnabled("Hitbox").ShouldBeTrue();
    }

    [Fact]
    public void SetTime_AnOwnerThatIsNotCollidable_StillDrivesValuesAndReportsEnabled()
    {
        var swing = Source("Swing", Frame(0.5f, RectSave("Hitbox", 5, 6)), Frame(0.5f));
        var entity = new PositionedObject();
        var hitbox = new AxisAlignedRectangle { Name = "Hitbox" };
        hitbox.AttachTo(entity);
        var file = new SequenceListSave();
        var sequence = new SequenceSave { Name = "Attack" };
        sequence.Tracks.Add(Track("Swing"));
        file.Sequences.Add(sequence);
        var player = new SequencePlayer(entity, new SequenceList(file, _ => swing));
        player.Play("Attack");

        player.SetTime(0.1);
        hitbox.RelativeX.ShouldBe(5);
        player.IsShapeEnabled("Hitbox").ShouldBeTrue();

        Should.NotThrow(() => player.SetTime(0.6));
        player.IsShapeEnabled("Hitbox").ShouldBeFalse();
    }

    [Fact]
    public void IsShapeEnabled_BeforeAnythingPlays_IsTrueForEveryName()
    {
        var player = new SequencePlayer(new PositionedObject(), new SequenceList(new SequenceListSave(), _ => null));

        player.IsShapeEnabled("Anything").ShouldBeTrue();
    }
}
