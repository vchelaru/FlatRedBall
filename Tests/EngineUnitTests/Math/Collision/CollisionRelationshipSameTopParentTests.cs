using System.Collections.Generic;
using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using Shouldly;

namespace EngineUnitTests.Math.Collision;

// Pins #2122: physics (Move/Bounce) should not reposition two objects that share a
// top parent (e.g. an object AttachTo'd to the player). Applying physics between them repositions
// the shared parent, which does not change their relative offset, so they still overlap next frame -
// this causes an endless repositioning loop. Events should still fire.
public class CollisionRelationshipSameTopParentTests
{
    class TopParentEntity : PositionedObject, ICollidable
    {
        public ShapeCollection Collision { get; set; }
        public HashSet<string> ItemsCollidedAgainst { get; } = new HashSet<string>();
        public HashSet<string> LastFrameItemsCollidedAgainst { get; } = new HashSet<string>();
        public HashSet<object> ObjectsCollidedAgainst { get; } = new HashSet<object>();
        public HashSet<object> LastFrameObjectsCollidedAgainst { get; } = new HashSet<object>();
    }

    static TopParentEntity CreateEntityWithCircle()
    {
        var entity = new TopParentEntity { Name = "Entity" };
        entity.Collision = new ShapeCollection();
        entity.Collision.Add(new Circle { Radius = 10 });
        return entity;
    }

    // MoveSoft is intentionally excluded here: it applies its separation via Velocity scaled by
    // TimeManager.SecondDifference, which is 0 outside of a running game loop, so a unit test can't
    // observe it move regardless of whether the fix is present - see the "nearby value" testability
    // pitfall in the maintenance-mode-workflow skill.
    [Theory]
    [InlineData(CollisionRelationshipTestCollisionType.Move)]
    [InlineData(CollisionRelationshipTestCollisionType.Bounce)]
    public void DoCollisions_ShouldNotApplyPhysics_ButShouldStillRaiseEvent_WhenObjectsShareTopParent(CollisionRelationshipTestCollisionType collisionType)
    {
        var player = CreateEntityWithCircle();
        player.Name = "Player";
        var grabbed = CreateEntityWithCircle();
        grabbed.Name = "Grabbed";
        // Overlaps the player's circle (radii sum to 20) without being centered on it.
        grabbed.Collision.Circles[0].X = 5;

        grabbed.AttachTo(player, changeRelative: true);

        var playerCircleXBefore = player.Collision.Circles[0].X;
        var grabbedCircleXBefore = grabbed.Collision.Circles[0].X;

        var relationship = new PositionedObjectVsPositionedObjectRelationship<TopParentEntity, TopParentEntity>(player, grabbed);
        switch (collisionType)
        {
            case CollisionRelationshipTestCollisionType.Move:
                relationship.SetMoveCollision(1, 1);
                break;
            case CollisionRelationshipTestCollisionType.Bounce:
                relationship.SetBounceCollision(1, 1, 1);
                break;
        }

        var didCollide = relationship.DoCollisions();

        didCollide.ShouldBeTrue();
        player.ItemsCollidedAgainst.ShouldContain(grabbed.Name);
        grabbed.ItemsCollidedAgainst.ShouldContain(player.Name);

        player.Collision.Circles[0].X.ShouldBe(playerCircleXBefore);
        grabbed.Collision.Circles[0].X.ShouldBe(grabbedCircleXBefore);
    }

    [Fact]
    public void DoCollisions_ShouldStillApplyPhysics_WhenObjectsDoNotShareTopParent()
    {
        var first = CreateEntityWithCircle();
        first.Name = "First";
        var second = CreateEntityWithCircle();
        second.Name = "Second";
        second.Collision.Circles[0].X = 5;

        var relationship = new PositionedObjectVsPositionedObjectRelationship<TopParentEntity, TopParentEntity>(first, second);
        relationship.SetMoveCollision(1, 1);

        var firstCircleXBefore = first.Collision.Circles[0].X;

        var didCollide = relationship.DoCollisions();

        didCollide.ShouldBeTrue();
        first.Collision.Circles[0].X.ShouldNotBe(firstCircleXBefore);
    }

    public enum CollisionRelationshipTestCollisionType
    {
        Move,
        Bounce
    }
}
