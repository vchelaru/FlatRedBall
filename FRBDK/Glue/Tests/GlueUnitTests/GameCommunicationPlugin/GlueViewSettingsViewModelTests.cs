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
}
