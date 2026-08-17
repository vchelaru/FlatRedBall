using System.Collections.Generic;
using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using Shouldly;

namespace EngineUnitTests.Math.Collision;

// Pins #2084: an ICollidable instance may intentionally have a null Collision (e.g. it doesn't
// need one), so CollisionRelationship's DoCollisions should skip it rather than NRE inside
// ShapeCollection.CollideAgainst.
public class CollisionRelationshipNullCollisionTests
{
    class NullableCollisionEntity : PositionedObject, ICollidable
    {
        public ShapeCollection Collision { get; set; }
        public HashSet<string> ItemsCollidedAgainst { get; } = new HashSet<string>();
        public HashSet<string> LastFrameItemsCollidedAgainst { get; } = new HashSet<string>();
        public HashSet<object> ObjectsCollidedAgainst { get; } = new HashSet<object>();
        public HashSet<object> LastFrameObjectsCollidedAgainst { get; } = new HashSet<object>();
    }

    static NullableCollisionEntity CreateEntityWithCollision()
    {
        var entity = new NullableCollisionEntity { Name = "WithCollision" };
        entity.Collision = new ShapeCollection();
        entity.Collision.Add(new Circle { Radius = 10 });
        return entity;
    }

    static ShapeCollection CreateOverlappingShapeCollection()
    {
        var shapeCollection = new ShapeCollection();
        shapeCollection.Add(new Circle { Radius = 10 });
        return shapeCollection;
    }

    [Fact]
    public void PositionedObjectVsPositionedObjectRelationship_DoCollisions_ShouldNotThrow_WhenSecondCollisionIsNull()
    {
        var first = CreateEntityWithCollision();
        var second = new NullableCollisionEntity { Name = "NullCollision", Collision = null };

        var relationship = new PositionedObjectVsPositionedObjectRelationship<NullableCollisionEntity, NullableCollisionEntity>(first, second);

        var didCollide = relationship.DoCollisions();

        didCollide.ShouldBeFalse();
    }

    [Fact]
    public void PositionedObjectVsShapeCollection_DoCollisions_ShouldNotThrow_WhenEntityCollisionIsNull()
    {
        var entity = new NullableCollisionEntity { Name = "NullCollision", Collision = null };
        var shapeCollection = CreateOverlappingShapeCollection();

        var relationship = new PositionedObjectVsShapeCollection<NullableCollisionEntity>(entity, shapeCollection);

        var didCollide = relationship.DoCollisions();

        didCollide.ShouldBeFalse();
    }

    [Fact]
    public void ListVsShapeCollectionRelationship_DoCollisions_ShouldNotThrow_WhenListItemCollisionIsNull()
    {
        var list = new PositionedObjectList<NullableCollisionEntity>
        {
            new NullableCollisionEntity { Name = "NullCollision", Collision = null }
        };
        var shapeCollection = CreateOverlappingShapeCollection();

        var relationship = new ListVsShapeCollectionRelationship<NullableCollisionEntity>(list, shapeCollection);

        var didCollide = relationship.DoCollisions();

        didCollide.ShouldBeFalse();
    }

    // Pins #2118: the entity-level check above doesn't cover sub-collisions set via
    // SetFirstSubCollision/SetSecondSubCollision - those delegates can return null for a
    // particular instance (e.g. a Composite whose optional sub-object isn't created this time)
    // even though the entity's own Collision is non-null, and previously NRE'd inside
    // AxisAlignedRectangle.CollideAgainst instead of being skipped.
    [Theory]
    [InlineData(CollisionRelationshipTestCollisionType.EventOnly)]
    [InlineData(CollisionRelationshipTestCollisionType.Move)]
    [InlineData(CollisionRelationshipTestCollisionType.Bounce)]
    public void PositionedObjectVsPositionedObjectRelationship_DoCollisions_ShouldNotThrow_WhenSecondSubCollisionRectangleIsNull(CollisionRelationshipTestCollisionType collisionType)
    {
        var first = CreateEntityWithCollision();
        var second = CreateEntityWithCollision();
        var firstRectangle = new AxisAlignedRectangle { Width = 10, Height = 10 };

        var relationship = new PositionedObjectVsPositionedObjectRelationship<NullableCollisionEntity, NullableCollisionEntity>(first, second);
        relationship.SetFirstSubCollision((Func<NullableCollisionEntity, AxisAlignedRectangle>)(e => firstRectangle));
        relationship.SetSecondSubCollision((Func<NullableCollisionEntity, AxisAlignedRectangle>)(e => null));

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

        didCollide.ShouldBeFalse();
    }

    public enum CollisionRelationshipTestCollisionType
    {
        EventOnly,
        Move,
        Bounce
    }
}
