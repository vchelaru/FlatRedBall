using System;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CodeGeneration;

// Generated comments name an object with its element. The shortcut that skips the project-wide container search
// must produce exactly the text NamedObjectSave.ToString() does.
[Collection(nameof(TaskManagerSequentialCollection))]
public class NamedObjectCommentDescriptionTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;
    private readonly GlueProjectSave _project = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
    private readonly ScreenSave _screen = new ScreenSave { Name = "Screens\\GameScreen" };
    private readonly EntitySave _entity = new EntitySave { Name = "Entities\\Enemy" };

    public NamedObjectCommentDescriptionTests()
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
    public void DescribeForComment_TopLevelObjectOfTheElement_MatchesToString()
    {
        var sprite = FrbObject("Sprite1", "Sprite");
        _entity.NamedObjects.Add(sprite);

        NamedObjectSaveCodeGenerator.DescribeForComment(sprite, _entity).ShouldBe(sprite.ToString());
        sprite.ToString().ShouldContain(_entity.ToString());
    }

    [Fact]
    public void DescribeForComment_ObjectInsideAList_MatchesToString()
    {
        var list = FrbObject("CircleList", "FlatRedBall.Math.PositionedObjectList<T>");
        var circle = FrbObject("Circle1", "Circle");
        list.ContainedObjects.Add(circle);
        _screen.NamedObjects.Add(list);

        NamedObjectSaveCodeGenerator.DescribeForComment(circle, _screen).ShouldBe(circle.ToString());
    }

    [Fact]
    public void DescribeForComment_ObjectFromAnotherElement_MatchesToString()
    {
        var sprite = FrbObject("Sprite1", "Sprite");
        _entity.NamedObjects.Add(sprite);

        NamedObjectSaveCodeGenerator.DescribeForComment(sprite, _screen).ShouldBe(sprite.ToString());
        sprite.ToString().ShouldContain(_entity.ToString());
    }

    [Fact]
    public void DescribeForComment_UncontainedObject_MatchesToString()
    {
        var sprite = FrbObject("Loose", "Sprite");

        NamedObjectSaveCodeGenerator.DescribeForComment(sprite, _screen).ShouldBe(sprite.ToString());
    }
}
