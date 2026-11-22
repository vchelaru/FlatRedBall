using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Managers;

// Replaces ObjectFinder.Self.GlueProject, so it runs in the sequential collection.
[Collection(nameof(TaskManagerSequentialCollection))]
public class ObjectFinderGetElementContainingTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;
    private readonly GlueProjectSave _project = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
    private readonly ScreenSave _screen = new ScreenSave { Name = "Screens\\GameScreen" };
    private readonly EntitySave _entity = new EntitySave { Name = "Entities\\Enemy" };

    public ObjectFinderGetElementContainingTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _project.Screens.Add(_screen);
        _project.Entities.Add(_entity);
        ObjectFinder.Self.GlueProject = _project;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    private static NamedObjectSave FrbObject(string name, string sourceClassType) =>
        new NamedObjectSave { InstanceName = name, SourceType = SourceType.FlatRedBallType, SourceClassType = sourceClassType };

    [Fact]
    public void GetElementContaining_TopLevelObject_ReturnsItsElement()
    {
        var sprite = FrbObject("Sprite1", "Sprite");
        _entity.NamedObjects.Add(sprite);

        ObjectFinder.Self.GetElementContaining(sprite).ShouldBe(_entity);
    }

    [Theory]
    [InlineData("FlatRedBall.Math.PositionedObjectList<T>")]
    [InlineData("ShapeCollection")]
    public void GetElementContaining_ObjectInsideListOrShapeCollection_ReturnsTheElementOfTheList(string containerType)
    {
        var container = FrbObject("Container", containerType);
        var child = FrbObject("Child", "Circle");
        container.ContainedObjects.Add(child);
        _screen.NamedObjects.Add(FrbObject("Unrelated", "Sprite"));
        _screen.NamedObjects.Add(container);

        ObjectFinder.Self.GetElementContaining(child).ShouldBe(_screen);
    }

    [Fact]
    public void GetElementContaining_ObjectUnderANonListObject_IsNotFound()
    {
        // Only lists and shape collections are searched, matching how code generation treats contained objects.
        var notAList = FrbObject("Sprite1", "Sprite");
        var child = FrbObject("Child", "Circle");
        notAList.ContainedObjects.Add(child);
        _entity.NamedObjects.Add(notAList);

        ObjectFinder.Self.GetElementContaining(child).ShouldBeNull();
    }
}
