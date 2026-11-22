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

    [Fact]
    public void CacheContainingElements_GivesTheSameAnswersAsTheSearch()
    {
        var sprite = FrbObject("Sprite1", "Sprite");
        _entity.NamedObjects.Add(sprite);
        var list = FrbObject("List", "FlatRedBall.Math.PositionedObjectList<T>");
        var inList = FrbObject("InList", "Circle");
        list.ContainedObjects.Add(inList);
        _screen.NamedObjects.Add(list);
        var notAList = FrbObject("NotAList", "Sprite");
        var underNotAList = FrbObject("UnderNotAList", "Circle");
        notAList.ContainedObjects.Add(underNotAList);
        _entity.NamedObjects.Add(notAList);
        var nowhere = FrbObject("Nowhere", "Sprite");

        using (ObjectFinder.Self.CacheContainingElements())
        {
            ObjectFinder.Self.GetElementContaining(sprite).ShouldBe(_entity);
            ObjectFinder.Self.GetElementContaining(list).ShouldBe(_screen);
            ObjectFinder.Self.GetElementContaining(inList).ShouldBe(_screen);
            ObjectFinder.Self.GetElementContaining(underNotAList).ShouldBeNull();
            ObjectFinder.Self.GetElementContaining(nowhere).ShouldBeNull();
        }
    }

    [Fact]
    public void CacheContainingElements_ObjectAddedOrMovedInsideTheScope_IsFoundWhereItIsNow()
    {
        var moved = FrbObject("Moved", "Sprite");
        _entity.NamedObjects.Add(moved);

        using (ObjectFinder.Self.CacheContainingElements())
        {
            var added = FrbObject("Added", "Sprite");
            _screen.NamedObjects.Add(added);
            _entity.NamedObjects.Remove(moved);
            _screen.NamedObjects.Add(moved);

            ObjectFinder.Self.GetElementContaining(added).ShouldBe(_screen);
            ObjectFinder.Self.GetElementContaining(moved).ShouldBe(_screen);
        }
    }

    [Fact]
    public void CacheContainingElements_Variables_GiveTheSameAnswersAsTheSearch()
    {
        var onEntity = new CustomVariable { Name = "Speed", Type = "float" };
        _entity.CustomVariables.Add(onEntity);
        var onScreen = new CustomVariable { Name = "Score", Type = "int" };
        _screen.CustomVariables.Add(onScreen);
        var nowhere = new CustomVariable { Name = "Nowhere", Type = "int" };

        using (ObjectFinder.Self.CacheContainingElements())
        {
            ObjectFinder.Self.GetElementContaining(onEntity).ShouldBe(_entity);
            ObjectFinder.Self.GetElementContaining(onScreen).ShouldBe(_screen);
            ObjectFinder.Self.GetElementContaining(nowhere).ShouldBeNull();
        }
    }

    [Fact]
    public void CacheContainingElements_VariableAddedMovedOrRemovedInsideTheScope_IsFoundWhereItIsNow()
    {
        var moved = new CustomVariable { Name = "Moved", Type = "float" };
        _entity.CustomVariables.Add(moved);
        var removed = new CustomVariable { Name = "Removed", Type = "float" };
        _entity.CustomVariables.Add(removed);

        using (ObjectFinder.Self.CacheContainingElements())
        {
            var added = new CustomVariable { Name = "Added", Type = "float" };
            _screen.CustomVariables.Add(added);
            _entity.CustomVariables.Remove(moved);
            _screen.CustomVariables.Add(moved);
            _entity.CustomVariables.Remove(removed);

            ObjectFinder.Self.GetElementContaining(added).ShouldBe(_screen);
            ObjectFinder.Self.GetElementContaining(moved).ShouldBe(_screen);
            ObjectFinder.Self.GetElementContaining(removed).ShouldBeNull();
        }
    }

    [Fact]
    public void CacheContainingElements_ObjectRemovedInsideTheScope_IsNotFound()
    {
        var removed = FrbObject("Removed", "Sprite");
        _entity.NamedObjects.Add(removed);

        using (ObjectFinder.Self.CacheContainingElements())
        {
            _entity.NamedObjects.Remove(removed);

            ObjectFinder.Self.GetElementContaining(removed).ShouldBeNull();
        }
    }
}
