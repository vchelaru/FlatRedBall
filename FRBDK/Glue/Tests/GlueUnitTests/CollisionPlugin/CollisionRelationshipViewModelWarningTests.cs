using OfficialPlugins.CollisionPlugin.ViewModels;
using Shouldly;

namespace GlueUnitTests.CollisionPlugin;

// GitHub issue #2115: setting both masses to 0 on a Move/Bounce/MoveSoft collision relationship
// crashes at runtime (ArgumentException in the shape-level CollideAgainst* methods, DEBUG builds only -
// see FlatRedBall.Math.Geometry.AxisAlignedRectangle.CollideAgainstMove and siblings). Glue can't stop a
// user from typing 0 into both fields, so instead it should surface this as a WarningText banner, the
// same mechanism CollisionRelationshipViewModel already uses for other invalid relationship configs
// (e.g. "Cannot create relationship for collidable X against itself").
//
// These tests use SetInternal (the same seam PropertyListContainerViewModel.UpdateFromGlueObject uses to
// load values without triggering codegen/persistence) so the view model can be exercised directly, without
// a GlueObject or any GlueState bootstrap.
public class CollisionRelationshipViewModelWarningTests
{
    [Theory]
    [InlineData(CollisionType.MoveCollision)]
    [InlineData(CollisionType.BounceCollision)]
    [InlineData(CollisionType.MoveSoftCollision)]
    public void WarningText_ShouldWarn_WhenBothMassesAreZero(CollisionType collisionType)
    {
        var vm = new CollisionRelationshipViewModel();
        vm.SetInternal<int>((int)collisionType, nameof(CollisionRelationshipViewModel.CollisionType), null);
        vm.SetInternal<float>(0f, nameof(CollisionRelationshipViewModel.FirstCollisionMass), null);
        vm.SetInternal<float>(0f, nameof(CollisionRelationshipViewModel.SecondCollisionMass), null);

        vm.WarningText.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void WarningText_ShouldNotWarn_WhenOnlyOneMassIsZero()
    {
        var vm = new CollisionRelationshipViewModel();
        vm.SetInternal<int>((int)CollisionType.MoveCollision, nameof(CollisionRelationshipViewModel.CollisionType), null);
        vm.SetInternal<float>(1f, nameof(CollisionRelationshipViewModel.FirstCollisionMass), null);
        vm.SetInternal<float>(0f, nameof(CollisionRelationshipViewModel.SecondCollisionMass), null);

        vm.WarningText.ShouldBeNull();
    }

    [Fact]
    public void WarningText_ShouldNotWarn_WhenBothMassesAreZeroButCollisionTypeDoesNotUseMass()
    {
        var vm = new CollisionRelationshipViewModel();
        vm.SetInternal<int>((int)CollisionType.NoPhysics, nameof(CollisionRelationshipViewModel.CollisionType), null);
        vm.SetInternal<float>(0f, nameof(CollisionRelationshipViewModel.FirstCollisionMass), null);
        vm.SetInternal<float>(0f, nameof(CollisionRelationshipViewModel.SecondCollisionMass), null);

        vm.WarningText.ShouldBeNull();
    }
}
