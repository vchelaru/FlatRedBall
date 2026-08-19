using GameCommunicationPlugin.GlueControl.ViewModels;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// PolygonPointSnapSize=0 silently disables all polygon-point snapping (PolygonPointHandles.ApplyDrag's
/// Snap() only rounds when PointSnapSize > 0), and unlike GridSize this field had no minimum-value guard
/// - so an existing project with 0 already persisted in CompilerSettings.json drags points completely
/// unsnapped, with no error or visual indication anything is wrong.
/// </summary>
public class GlueViewSettingsViewModelTests
{
    [Fact]
    public void PolygonPointSnapSize_SetToZero_ClampsToAMinimumThatStillSnaps()
    {
        var viewModel = new GlueViewSettingsViewModel();

        viewModel.PolygonPointSnapSize = 0;

        viewModel.PolygonPointSnapSize.ShouldBeGreaterThan(0,
            "0 disables snapping entirely and silently - it must never be reachable through this setter");
    }

    [Fact]
    public void PolygonPointSnapSize_SetToNegative_ClampsToAMinimumThatStillSnaps()
    {
        var viewModel = new GlueViewSettingsViewModel();

        viewModel.PolygonPointSnapSize = -5;

        viewModel.PolygonPointSnapSize.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(-3999, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(255, 255)]
    [InlineData(256, 255)]
    [InlineData(10000, 255)]
    public void BackgroundRed_OutOfRangeValue_ClampsTo0To255(int input, int expected)
    {
        var viewModel = new GlueViewSettingsViewModel();

        viewModel.BackgroundRed = input;

        viewModel.BackgroundRed.ShouldBe(expected);
    }

    [Theory]
    [InlineData(-3999, 0)]
    [InlineData(10000, 255)]
    public void BackgroundGreen_OutOfRangeValue_ClampsTo0To255(int input, int expected)
    {
        var viewModel = new GlueViewSettingsViewModel();

        viewModel.BackgroundGreen = input;

        viewModel.BackgroundGreen.ShouldBe(expected);
    }

    [Theory]
    [InlineData(-3999, 0)]
    [InlineData(10000, 255)]
    public void BackgroundBlue_OutOfRangeValue_ClampsTo0To255(int input, int expected)
    {
        var viewModel = new GlueViewSettingsViewModel();

        viewModel.BackgroundBlue = input;

        viewModel.BackgroundBlue.ShouldBe(expected);
    }
}
