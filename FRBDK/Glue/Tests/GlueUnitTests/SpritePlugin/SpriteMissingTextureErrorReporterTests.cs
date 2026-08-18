using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using OfficialPlugins.SpritePlugin.Errors;
using Shouldly;
using System;

namespace GlueUnitTests.SpritePlugin;

// GitHub issue #2110: a Sprite NamedObjectSave with a texture-coordinate CustomVariable
// (LeftTexturePixel/RightTexturePixel/TopTexturePixel/BottomTexturePixel) set but no Texture throws
// "You must have a Texture set before setting this value" at runtime (Sprite.set_LeftTexturePixel) -
// codegen emits the coordinate assignment unconditionally, with nothing in the editor warning that the
// combination is invalid. SpriteMissingTextureErrorReporter surfaces this in the Error tab instead of
// letting it reach a runtime crash.
public class SpriteMissingTextureErrorReporterTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public SpriteMissingTextureErrorReporterTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    [Fact]
    public void GetIfHasError_ShouldReturnTrue_WhenTextureCoordinateIsSetButTextureIsNot()
    {
        var container = new EntitySave();
        var sprite = AddSprite(container, "SpriteInstance");
        SetInstruction(sprite, nameof(Sprite.LeftTexturePixel), 10f);

        SpriteMissingTextureErrorReporter.GetIfHasError(sprite, container).ShouldBeTrue();
    }

    [Fact]
    public void GetIfHasError_ShouldReturnFalse_WhenTextureIsAlsoSet()
    {
        var container = new EntitySave();
        var sprite = AddSprite(container, "SpriteInstance");
        SetInstruction(sprite, nameof(Sprite.LeftTexturePixel), 10f);
        SetInstruction(sprite, nameof(Sprite.Texture), "SomeTexture.png");

        SpriteMissingTextureErrorReporter.GetIfHasError(sprite, container).ShouldBeFalse();
    }

    [Fact]
    public void GetIfHasError_ShouldReturnFalse_WhenNoTextureCoordinatesAreSet()
    {
        var container = new EntitySave();
        var sprite = AddSprite(container, "SpriteInstance");

        SpriteMissingTextureErrorReporter.GetIfHasError(sprite, container).ShouldBeFalse();
    }

    [Fact]
    public void GetIfHasError_ShouldReturnFalse_WhenNamedObjectIsNotASprite()
    {
        var container = new EntitySave();
        var nos = new NamedObjectSave
        {
            InstanceName = "TextInstance",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Graphics.Text"
        };
        container.NamedObjects.Add(nos);
        SetInstruction(nos, nameof(Sprite.LeftTexturePixel), 10f);

        SpriteMissingTextureErrorReporter.GetIfHasError(nos, container).ShouldBeFalse();
    }

    [Fact]
    public void GetAllErrors_ShouldIncludeSprite_WhenTextureCoordinateIsSetButTextureIsNot()
    {
        var container = new EntitySave { Name = "Entities\\SomeEntity\\SomeEntity" };
        var sprite = AddSprite(container, "SpriteInstance");
        SetInstruction(sprite, nameof(Sprite.TopTexturePixel), 4f);
        ObjectFinder.Self.GlueProject.Entities.Add(container);

        var reporter = new SpriteMissingTextureErrorReporter();

        var errors = reporter.GetAllErrors();

        errors.ShouldContain(error => error is SpriteMissingTextureErrorViewModel);
    }

    [Fact]
    public void GetIfIsFixed_ShouldReturnTrue_AfterATextureIsAssigned()
    {
        var container = new EntitySave { Name = "Entities\\SomeEntity\\SomeEntity" };
        var sprite = AddSprite(container, "SpriteInstance");
        SetInstruction(sprite, nameof(Sprite.LeftTexturePixel), 10f);
        ObjectFinder.Self.GlueProject.Entities.Add(container);

        var error = new SpriteMissingTextureErrorViewModel(sprite);
        error.GetIfIsFixed().ShouldBeFalse();

        SetInstruction(sprite, nameof(Sprite.Texture), "SomeTexture.png");

        error.GetIfIsFixed().ShouldBeTrue();
    }

    static NamedObjectSave AddSprite(EntitySave container, string instanceName)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = instanceName,
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite"
        };
        container.NamedObjects.Add(nos);
        return nos;
    }

    static void SetInstruction(NamedObjectSave nos, string member, object value)
    {
        nos.InstructionSaves.Add(new CustomVariableInNamedObject
        {
            Member = member,
            Value = value
        });
    }
}
