using Microsoft.Xna.Framework;
using OfficialPlugins.PointEditingPlugin;
using Shouldly;
using Xunit;

namespace GlueUnitTests.PointEditingPlugin;

/// <summary>
/// Moving the last point down (or the first point up) used to call ObservableCollection.Move
/// unconditionally, which throws ArgumentOutOfRangeException and crashes Glue. See GitHub issue #2189.
/// </summary>
public class PointEditingViewModelTests
{
    [Fact]
    public void MoveSelectedPointDown_WhenSelectedIsLastPoint_ShouldNotThrowAndShouldNotChangePoints()
    {
        var viewModel = new PointEditingViewModel();
        viewModel.Points.Add(new Vector2(0, 0));
        viewModel.Points.Add(new Vector2(1, 1));
        viewModel.Points.Add(new Vector2(2, 2));
        viewModel.SelectedIndex = 2;

        Should.NotThrow(() => viewModel.MoveSelectedPointDown());

        viewModel.Points.ShouldBe(new[] { new Vector2(0, 0), new Vector2(1, 1), new Vector2(2, 2) });
        viewModel.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void MoveSelectedPointUp_WhenSelectedIsFirstPoint_ShouldNotThrowAndShouldNotChangePoints()
    {
        var viewModel = new PointEditingViewModel();
        viewModel.Points.Add(new Vector2(0, 0));
        viewModel.Points.Add(new Vector2(1, 1));
        viewModel.Points.Add(new Vector2(2, 2));
        viewModel.SelectedIndex = 0;

        Should.NotThrow(() => viewModel.MoveSelectedPointUp());

        viewModel.Points.ShouldBe(new[] { new Vector2(0, 0), new Vector2(1, 1), new Vector2(2, 2) });
        viewModel.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void MoveSelectedPointDown_WhenSelectedIsNotLast_ShouldMovePointAndUpdateSelectedIndex()
    {
        var viewModel = new PointEditingViewModel();
        viewModel.Points.Add(new Vector2(0, 0));
        viewModel.Points.Add(new Vector2(1, 1));
        viewModel.Points.Add(new Vector2(2, 2));
        viewModel.SelectedIndex = 0;

        viewModel.MoveSelectedPointDown();

        viewModel.Points.ShouldBe(new[] { new Vector2(1, 1), new Vector2(0, 0), new Vector2(2, 2) });
        viewModel.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void MoveSelectedPointUp_WhenSelectedIsNotFirst_ShouldMovePointAndUpdateSelectedIndex()
    {
        var viewModel = new PointEditingViewModel();
        viewModel.Points.Add(new Vector2(0, 0));
        viewModel.Points.Add(new Vector2(1, 1));
        viewModel.Points.Add(new Vector2(2, 2));
        viewModel.SelectedIndex = 2;

        viewModel.MoveSelectedPointUp();

        viewModel.Points.ShouldBe(new[] { new Vector2(0, 0), new Vector2(2, 2), new Vector2(1, 1) });
        viewModel.SelectedIndex.ShouldBe(1);
    }
}
