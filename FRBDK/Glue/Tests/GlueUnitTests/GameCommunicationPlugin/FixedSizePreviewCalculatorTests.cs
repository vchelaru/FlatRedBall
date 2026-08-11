using GameCommunicationPlugin.GlueControl.Views;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// Computes the embedded game window size for the Game tab's "fixed-size preview" toggle
/// (issue #2035): scale the project's target resolution down to fit the available panel,
/// but never scale it up past 100% - excess panel space becomes letterbox bars instead.
/// </summary>
public class FixedSizePreviewCalculatorTests
{
    [Fact]
    public void GetEmbeddedWindowSize_ShouldReturnExactTarget_WhenPanelIsLarger()
    {
        var size = FixedSizePreviewCalculator.GetEmbeddedWindowSize(
            panelWidth: 1920, panelHeight: 1080, targetWidth: 800, targetHeight: 600);

        size.Width.ShouldBe(800);
        size.Height.ShouldBe(600);
    }

    [Fact]
    public void GetEmbeddedWindowSize_ShouldScaleDownPreservingAspectRatio_WhenWidthConstrained()
    {
        // Panel is narrower than the target resolution, so width is the binding constraint.
        var size = FixedSizePreviewCalculator.GetEmbeddedWindowSize(
            panelWidth: 400, panelHeight: 600, targetWidth: 800, targetHeight: 600);

        size.Width.ShouldBe(400);
        size.Height.ShouldBe(300);
    }

    [Fact]
    public void GetEmbeddedWindowSize_ShouldScaleDownPreservingAspectRatio_WhenHeightConstrained()
    {
        // Panel is shorter than the target resolution, so height is the binding constraint.
        var size = FixedSizePreviewCalculator.GetEmbeddedWindowSize(
            panelWidth: 800, panelHeight: 300, targetWidth: 800, targetHeight: 600);

        size.Width.ShouldBe(400);
        size.Height.ShouldBe(300);
    }

    [Fact]
    public void GetEmbeddedWindowSize_ShouldFallBackToPanelSize_WhenTargetResolutionIsInvalid()
    {
        var size = FixedSizePreviewCalculator.GetEmbeddedWindowSize(
            panelWidth: 640, panelHeight: 480, targetWidth: 0, targetHeight: 0);

        size.Width.ShouldBe(640);
        size.Height.ShouldBe(480);
    }
}
