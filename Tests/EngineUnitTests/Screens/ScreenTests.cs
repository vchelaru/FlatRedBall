using FlatRedBall;
using FlatRedBall.Screens;
using Shouldly;

namespace EngineUnitTests.Screens;

public class ScreenTests
{
    private class TestScreen : Screen
    {
        public Sprite ChildSprite { get; set; } = new Sprite();
    }

    [Fact]
    public void GetInstanceRecursive_ShouldResolveNestedInstance_NotReturnScreenItself()
    {
        var screen = new TestScreen();

        var result = screen.GetInstanceRecursive("this.ChildSprite");

        result.ShouldBe(screen.ChildSprite);
    }

    [Fact]
    public void GetInstanceRecursive_ShouldReturnNull_WhenInstanceNotFound()
    {
        var screen = new TestScreen();

        var result = screen.GetInstanceRecursive("this.DoesNotExist");

        result.ShouldBeNull();
    }
}
