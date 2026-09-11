using FlatRedBall;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Math.Geometry;
using Shouldly;
using Xunit;

namespace EngineUnitTests.Content.Math.Geometry;

public class ShapeCollectionSaveSyncChildrenTests
{
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
