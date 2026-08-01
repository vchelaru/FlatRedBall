using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.SaveClasses;

// Assigns ObjectFinder.Self.GlueProject, which is process-wide, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class NameVerifierTests
{
    NameVerifier _sut;
    EntitySave _entity;

    public NameVerifierTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        ObjectFinder.Self.GlueProject = new GlueProjectSave();

        _entity = new EntitySave { Name = "Entities\\Piece" };
        ObjectFinder.Self.GlueProject.Entities.Add(_entity);

        _sut = new NameVerifier();
    }

    [Theory]
    [InlineData("X")]
    [InlineData("Y")]
    [InlineData("Z")]
    [InlineData("RotationZ")]
    // Velocity isn't exposable as a Glue variable, but the generated Entity still has it, so a file
    // using that name hides it just the same.
    [InlineData("Velocity")]
    public void IsReferencedFileNameValid_ShouldReturnFalse_WhenNameMatchesInheritedEntityMember(string name)
    {
        // A file added to an Entity generates a static field named after the file. If that name matches
        // a member the generated Entity already has (PositionedObject.X, for example), the field hides
        // the member and generated code like "X = value.X;" no longer compiles.
        var isValid = _sut.IsReferencedFileNameValid(name, null, null, _entity, out string whyItIsntValid);

        isValid.ShouldBeFalse();
        whyItIsntValid.ShouldContain(name);
    }

    [Fact]
    public void IsReferencedFileNameValid_ShouldReturnTrue_WhenNameDoesNotMatchInheritedEntityMember()
    {
        var isValid = _sut.IsReferencedFileNameValid("Empty", null, null, _entity, out string whyItIsntValid);

        isValid.ShouldBeTrue();
        whyItIsntValid.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void IsReferencedFileNameValid_ShouldReturnTrue_WhenNameMatchesPositionedObjectMemberButContainerIsScreen()
    {
        // Screens don't inherit from PositionedObject, so there's nothing for the generated field to hide.
        var screen = new ScreenSave { Name = "Screens\\GameScreen" };
        ObjectFinder.Self.GlueProject.Screens.Add(screen);

        var isValid = _sut.IsReferencedFileNameValid("X", null, null, screen, out string whyItIsntValid);

        isValid.ShouldBeTrue();
        whyItIsntValid.ShouldBeNullOrEmpty();
    }
}
