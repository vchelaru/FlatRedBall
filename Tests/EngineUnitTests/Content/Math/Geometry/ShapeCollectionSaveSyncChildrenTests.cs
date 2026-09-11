using System.Collections.Generic;
using FlatRedBall;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Math.Geometry;
using Shouldly;
using Xunit;

namespace EngineUnitTests.Content.Math.Geometry;

public class ShapeCollectionSaveSyncChildrenTests
{
    class CollidableEntity : PositionedObject, ICollidable
    {
        public ShapeCollection Collision { get; set; } = new ShapeCollection();
        public HashSet<string> ItemsCollidedAgainst { get; } = new HashSet<string>();
        public HashSet<string> LastFrameItemsCollidedAgainst { get; } = new HashSet<string>();
        public HashSet<object> ObjectsCollidedAgainst { get; } = new HashSet<object>();
        public HashSet<object> LastFrameObjectsCollidedAgainst { get; } = new HashSet<object>();
    }

    [Fact]
    public void SetValuesOn_PositionedObject_ExistingShape_UpdatesValuesButNotCount()
    {
        var container = new PositionedObject();
        var existingCircle = new Circle { Name = "BulletOrigin" };
        existingCircle.AttachTo(container);

        var save = new ShapeCollectionSave();
        save.CircleSaves.Add(new CircleSave { Name = "BulletOrigin", X = 12, Y = 34, Radius = 5 });

        save.SetValuesOn(container, createMissingShapes: false);

        container.Children.Count.ShouldBe(1, "because the existing shape should be reused, not duplicated");
        existingCircle.RelativeX.ShouldBe(12);
        existingCircle.RelativeY.ShouldBe(34);
        existingCircle.Radius.ShouldBe(5);
    }

    [Fact]
    public void SetValuesOn_PositionedObject_MissingShape_CreateMissingShapesFalse_DoesNotCreate()
    {
        var container = new PositionedObject();

        var save = new ShapeCollectionSave();
        save.CircleSaves.Add(new CircleSave { Name = "BulletOrigin", X = 12, Y = 34, Radius = 5 });

        save.SetValuesOn(container, createMissingShapes: false);

        container.Children.Count.ShouldBe(0, "because createMissingShapes is false");
    }

    [Fact]
    public void SetValuesOn_PositionedObject_MissingShape_CreateMissingShapesTrue_CreatesAndAttaches()
    {
        var container = new PositionedObject();

        var save = new ShapeCollectionSave();
        save.CircleSaves.Add(new CircleSave { Name = "BulletOrigin", X = 12, Y = 34, Radius = 5 });

        save.SetValuesOn(container, createMissingShapes: true);

        container.Children.Count.ShouldBe(1);
        var created = container.Children[0].ShouldBeOfType<Circle>();
        created.Name.ShouldBe("BulletOrigin");
        created.Parent.ShouldBe(container);
        created.RelativeX.ShouldBe(12);
        created.RelativeY.ShouldBe(34);
        created.Radius.ShouldBe(5);
    }

    [Fact]
    public void SetValuesOn_PositionedObject_NonICollidableContainer_CreatedShape_IsNotAddedToAnyCollision()
    {
        var container = new PositionedObject();

        var save = new ShapeCollectionSave();
        save.CircleSaves.Add(new CircleSave { Name = "BulletOrigin", X = 12, Y = 34, Radius = 5 });

        // Should not throw just because there's no Collision to add to.
        save.SetValuesOn(container, createMissingShapes: true);

        container.Children.Count.ShouldBe(1, "because it's still attached as a plain child");
    }

    [Fact]
    public void SetValuesOn_PositionedObject_ICollidableContainer_CreatedShape_IsAlsoAddedToCollision()
    {
        var container = new CollidableEntity { Name = "Enemy" };

        var save = new ShapeCollectionSave();
        save.CircleSaves.Add(new CircleSave { Name = "RealHitbox", X = 12, Y = 34, Radius = 5 });

        save.SetValuesOn(container, createMissingShapes: true);

        container.Children.Count.ShouldBe(1, "because it's attached as a child, same as on a non-ICollidable container");
        container.Collision.Circles.Count.ShouldBe(1, "because a newly-created shape on an ICollidable container defaults into Collision");
        container.Collision.Circles[0].ShouldBeSameAs(container.Children[0]);
    }

    [Fact]
    public void SetValuesOn_PositionedObject_ICollidableContainer_ExistingShape_DoesNotDuplicateIntoCollision()
    {
        var container = new CollidableEntity { Name = "Enemy" };
        var existingCircle = new Circle { Name = "BulletOrigin" };
        existingCircle.AttachTo(container);
        // Deliberately NOT added to container.Collision - this simulates a shape the user placed as a
        // plain child (e.g. IncludeInICollidable = false), which should stay that way.

        var save = new ShapeCollectionSave();
        save.CircleSaves.Add(new CircleSave { Name = "BulletOrigin", X = 12, Y = 34, Radius = 5 });

        save.SetValuesOn(container, createMissingShapes: true);

        container.Collision.Circles.Count.ShouldBe(0, "because an existing shape's membership is never touched, even on an ICollidable container");
        existingCircle.RelativeX.ShouldBe(12);
    }

    [Fact]
    public void SetValuesOn_PositionedObject_ShapeAlsoInAShapeCollection_DoesNotChangeMembership()
    {
        var container = new PositionedObject();
        var circleInCollision = new Circle { Name = "RealHitbox" };
        circleInCollision.AttachTo(container);

        var collision = new ShapeCollection();
        collision.Circles.Add(circleInCollision);

        var save = new ShapeCollectionSave();
        save.CircleSaves.Add(new CircleSave { Name = "RealHitbox", X = 1, Y = 2, Radius = 3 });

        save.SetValuesOn(container, createMissingShapes: false);

        collision.Circles.Count.ShouldBe(1, "because syncing children should never add or remove ShapeCollection membership");
        collision.Circles[0].ShouldBeSameAs(circleInCollision);
        circleInCollision.RelativeX.ShouldBe(1);
        circleInCollision.RelativeY.ShouldBe(2);
        circleInCollision.Radius.ShouldBe(3);
    }

    [Fact]
    public void SetValuesOn_PositionedObject_ExistingRectangle_UpdatesValues()
    {
        var container = new PositionedObject();
        var existingRectangle = new AxisAlignedRectangle { Name = "Hurtbox" };
        existingRectangle.AttachTo(container);

        var save = new ShapeCollectionSave();
        save.AxisAlignedRectangleSaves.Add(new AxisAlignedRectangleSave { Name = "Hurtbox", X = 7, Y = 8, ScaleX = 2, ScaleY = 3 });

        save.SetValuesOn(container, createMissingShapes: false);

        container.Children.Count.ShouldBe(1);
        existingRectangle.RelativeX.ShouldBe(7);
        existingRectangle.RelativeY.ShouldBe(8);
    }
}
