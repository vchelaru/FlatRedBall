using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.Controllers;
using OfficialPlugins.CollisionPlugin.Errors;
using OfficialPlugins.CollisionPlugin.ExtensionMethods;
using OfficialPlugins.CollisionPlugin.ViewModels;
using Shouldly;

namespace GlueUnitTests.CollisionPlugin;

// GitHub issue #2115: setting both masses to 0 on a Move/Bounce/MoveSoft collision relationship
// crashes at runtime (ArgumentException in the shape-level CollideAgainst* methods, DEBUG builds only -
// see FlatRedBall.Math.Geometry.AxisAlignedRectangle.CollideAgainstMove and siblings). Glue can't stop a
// user from typing 0 into both fields, so this is caught by the same project-wide Error window mechanism
// (ErrorReporterBase/ErrorViewModel) CollisionRelationshipErrorViewModel already uses for other invalid
// relationship configs (missing/empty collidable references).
//
// Properties are set via SetValuePersistIfDefault, the same method CollisionRelationshipViewModel's
// SetAndPersist calls in production, rather than SetValue - SetValue special-cases 0/false/empty-string as
// "the default" and removes the property instead of storing it, which would make a real 0-mass value
// indistinguishable from a mass that was simply never set (and therefore defaults to 1 - see
// CollisionCodeGenerator).
public class CollisionRelationshipZeroMassErrorTests
{
    [Theory]
    [InlineData(CollisionType.MoveCollision)]
    [InlineData(CollisionType.BounceCollision)]
    [InlineData(CollisionType.MoveSoftCollision)]
    public void TryGetErrorMessageFor_ShouldReturnError_WhenBothMassesAreZero(CollisionType collisionType)
    {
        var container = new EntitySave();
        var first = AddCollidable(container, "FirstInstance");
        var second = AddCollidable(container, "SecondInstance");

        var relationship = CreateRelationship(collisionType, first.InstanceName, second.InstanceName,
            firstMass: 0f, secondMass: 0f);

        var error = CollisionRelationshipErrorViewModel.TryGetErrorMessageFor(relationship, container);

        error.ShouldNotBeNullOrEmpty();
        error.ShouldContain("mass");
    }

    [Fact]
    public void TryGetErrorMessageFor_ShouldReturnNull_WhenOnlyOneMassIsZero()
    {
        var container = new EntitySave();
        var first = AddCollidable(container, "FirstInstance");
        var second = AddCollidable(container, "SecondInstance");

        var relationship = CreateRelationship(CollisionType.MoveCollision, first.InstanceName, second.InstanceName,
            firstMass: 1f, secondMass: 0f);

        var error = CollisionRelationshipErrorViewModel.TryGetErrorMessageFor(relationship, container);

        error.ShouldBeNull();
    }

    [Fact]
    public void TryGetErrorMessageFor_ShouldReturnNull_WhenBothMassesAreZeroButCollisionTypeDoesNotUseMass()
    {
        var container = new EntitySave();
        var first = AddCollidable(container, "FirstInstance");
        var second = AddCollidable(container, "SecondInstance");

        var relationship = CreateRelationship(CollisionType.NoPhysics, first.InstanceName, second.InstanceName,
            firstMass: 0f, secondMass: 0f);

        var error = CollisionRelationshipErrorViewModel.TryGetErrorMessageFor(relationship, container);

        error.ShouldBeNull();
    }

    [Fact]
    public void TryGetErrorMessageFor_ShouldReturnNull_WhenMassesAreNeverExplicitlySet()
    {
        // Neither mass property was ever set on this relationship (e.g. it was created via API code, not
        // through the Glue property grid). CollisionCodeGenerator defaults an absent mass property to 1,
        // so this must not be flagged as a 0/0 crash.
        var container = new EntitySave();
        var first = AddCollidable(container, "FirstInstance");
        var second = AddCollidable(container, "SecondInstance");

        var relationship = new NamedObjectSave
        {
            InstanceName = "Relationship",
            SourceType = SourceType.FlatRedBallType
        };
        relationship.SetFirstCollidableObjectName(first.InstanceName);
        relationship.SetSecondCollidableObjectName(second.InstanceName);
        relationship.Properties.SetValuePersistIfDefault(nameof(CollisionRelationshipViewModel.CollisionType),
            (int)CollisionType.MoveCollision);

        var error = CollisionRelationshipErrorViewModel.TryGetErrorMessageFor(relationship, container);

        error.ShouldBeNull();
    }

    [Fact]
    public void GetIfIsFixed_ShouldReturnTrue_AfterAZeroMassIsChangedToNonZero()
    {
        // Regression: RefreshCommands.RefreshErrorsFor (the scoped per-reporter refresh MainCollisionPlugin
        // uses) only removes an error from the Error window when GetIfIsFixed() returns true - see
        // GlueErrorManager.ClearFixedErrors(List<ErrorViewModel>). The base ErrorViewModel.GetIfIsFixed()
        // defaults to false, so without this override the zero-mass error would never clear once the user
        // fixed the mass, even though a fresh TryGetErrorMessageFor call would correctly return null.
        var container = new EntitySave();
        var first = AddCollidable(container, "FirstInstance");
        var second = AddCollidable(container, "SecondInstance");

        var relationship = CreateRelationship(CollisionType.MoveCollision, first.InstanceName, second.InstanceName,
            firstMass: 0f, secondMass: 0f);

        var error = new CollisionRelationshipErrorViewModel(relationship, container);
        error.GetIfIsFixed().ShouldBeFalse();

        relationship.Properties.SetValuePersistIfDefault(
            nameof(CollisionRelationshipViewModel.SecondCollisionMass), 1f);

        error.GetIfIsFixed().ShouldBeTrue();
    }

    [Theory]
    [InlineData(nameof(CollisionRelationshipViewModel.CollisionType))]
    [InlineData(nameof(CollisionRelationshipViewModel.FirstCollisionMass))]
    [InlineData(nameof(CollisionRelationshipViewModel.SecondCollisionMass))]
    public void CanChangeAffectRelationshipErrors_ShouldReturnTrue_ForMassAffectingPropertiesOnARelationship(string changedMember)
    {
        var relationship = CreateRelationship(CollisionType.MoveCollision, "First", "Second", firstMass: 1f, secondMass: 1f);

        CollisionRelationshipViewModelController.CanChangeAffectRelationshipErrors(changedMember, relationship)
            .ShouldBeTrue();
    }

    [Fact]
    public void CanChangeAffectRelationshipErrors_ShouldReturnFalse_ForUnrelatedProperty()
    {
        var relationship = CreateRelationship(CollisionType.MoveCollision, "First", "Second", firstMass: 1f, secondMass: 1f);

        CollisionRelationshipViewModelController.CanChangeAffectRelationshipErrors(
            nameof(NamedObjectSave.InstanceName), relationship).ShouldBeFalse();
    }

    [Fact]
    public void CanChangeAffectRelationshipErrors_ShouldReturnFalse_WhenNamedObjectIsNotACollisionRelationship()
    {
        var notARelationship = new NamedObjectSave
        {
            InstanceName = "SomeSprite",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite"
        };

        CollisionRelationshipViewModelController.CanChangeAffectRelationshipErrors(
            nameof(CollisionRelationshipViewModel.FirstCollisionMass), notARelationship).ShouldBeFalse();
    }

    static NamedObjectSave AddCollidable(EntitySave container, string instanceName)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = instanceName,
            SourceType = SourceType.FlatRedBallType
        };
        container.NamedObjects.Add(nos);
        return nos;
    }

    static NamedObjectSave CreateRelationship(CollisionType collisionType, string firstInstanceName,
        string secondInstanceName, float firstMass, float secondMass)
    {
        var relationship = new NamedObjectSave
        {
            InstanceName = "Relationship",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Math.Collision.CollisionRelationship"
        };
        relationship.SetFirstCollidableObjectName(firstInstanceName);
        relationship.SetSecondCollidableObjectName(secondInstanceName);
        relationship.Properties.SetValuePersistIfDefault(nameof(CollisionRelationshipViewModel.CollisionType),
            (int)collisionType);
        relationship.Properties.SetValuePersistIfDefault(nameof(CollisionRelationshipViewModel.FirstCollisionMass),
            firstMass);
        relationship.Properties.SetValuePersistIfDefault(nameof(CollisionRelationshipViewModel.SecondCollisionMass),
            secondMass);

        return relationship;
    }
}
