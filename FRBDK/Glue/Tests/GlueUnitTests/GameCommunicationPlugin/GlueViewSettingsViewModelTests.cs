using CompilerLibrary.Models;
using GameCommunicationPlugin.GlueControl.ViewModels;
using Newtonsoft.Json;
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

    // Issue #2339: Gum interaction in edit mode is saved per project in CompilerSettings.json and sent to
    // the game, where it decides whether Gum UI under the cursor blocks selecting world objects.
    [Fact]
    public void IsGumInteractionEnabled_SetModelThenSetFrom_RoundTrips()
    {
        var source = new GlueViewSettingsViewModel { IsGumInteractionEnabled = true };
        var model = new CompilerSettingsModel();

        source.SetModel(model);
        var loaded = new GlueViewSettingsViewModel();
        loaded.SetFrom(model);

        loaded.IsGumInteractionEnabled.ShouldBeTrue();
    }

    [Fact]
    public void IsGumInteractionEnabled_SettingsFileWithoutTheValue_DefaultsToFalse()
    {
        var model = JsonConvert.DeserializeObject<CompilerSettingsModel>("{ \"ShowGrid\": true }");

        model.IsGumInteractionEnabled.ShouldBeFalse(
            "projects saved before this setting existed must not have Gum UI blocking edit-mode selection");
    }

    [Fact]
    public void CreateGlueViewSettingsDto_IsGumInteractionEnabled_IsSentToGame()
    {
        var viewModel = new GlueViewSettingsViewModel { IsGumInteractionEnabled = true };

        viewModel.CreateGlueViewSettingsDto().IsGumInteractionEnabled.ShouldBeTrue();
    }
}
