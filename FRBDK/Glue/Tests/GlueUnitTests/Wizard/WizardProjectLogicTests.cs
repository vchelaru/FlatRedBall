using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.Wizard;

// GitHub issue #1894 asked for unit coverage of WizardProjectLogic.Apply, using the TaskManager
// synchronous-mode / UI-thread-marshaller seams added in this same change (see
// GlueUnitTests.Tasks.TaskManagerSynchronousModeTests). Those seams are real and directly tested.
//
// This file covers the pure, side-effect-free decision logic inside an Apply step that doesn't touch
// GlueState/TaskManager/plugins - GetDisplaySettingsFor, extracted from ApplyMainCameraSettings.
//
// The "bare AddGameScreen" step is covered separately, in
// GlueUnitTests.Wizard.WizardProjectLogicAddGameScreenTests - see that file and REFACTORING.md's "Unblock
// WizardProjectLogic AddGameScreen testing" entry for how the VisualStudioProject construction blocker
// mentioned in earlier versions of this comment was actually unblocked (no IVisualStudioProject seam
// needed). Apply() as a whole is still not covered end-to-end - see the comment above
// WizardProjectLogic.Apply for what's left and why.
public class WizardProjectLogicTests
{
    [Theory]
    [InlineData(CameraResolution._256x224, 256, 224, 8, 7)]
    [InlineData(CameraResolution._360x240, 360, 240, 3, 2)]
    [InlineData(CameraResolution._480x360, 480, 360, 4, 3)]
    [InlineData(CameraResolution._640x480, 640, 480, 4, 3)]
    [InlineData(CameraResolution._800x600, 800, 600, 4, 3)]
    [InlineData(CameraResolution._1024x768, 1024, 768, 4, 3)]
    [InlineData(CameraResolution._1920x1080, 1920, 1080, 16, 9)]
    public void GetDisplaySettingsFor_ShouldMapResolutionToWidthHeightAndAspectRatio(
        CameraResolution resolution, int expectedWidth, int expectedHeight, decimal expectedAspectWidth, decimal expectedAspectHeight)
    {
        var settings = WizardProjectLogic.GetDisplaySettingsFor(resolution, scalePercent: 100);

        settings.Width.ShouldBe(expectedWidth);
        settings.Height.ShouldBe(expectedHeight);
        settings.AspectRatioWidth.ShouldBe(expectedAspectWidth);
        settings.AspectRatioHeight.ShouldBe(expectedAspectHeight);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void GetDisplaySettingsFor_ShouldDefaultScalePercentTo100_WhenNotPositive(int scalePercent)
    {
        var settings = WizardProjectLogic.GetDisplaySettingsFor(CameraResolution._800x600, scalePercent);

        settings.ScalePercent.ShouldBe(100);
    }

    [Fact]
    public void GetDisplaySettingsFor_ShouldPassThroughScalePercent_WhenPositive()
    {
        var settings = WizardProjectLogic.GetDisplaySettingsFor(CameraResolution._800x600, scalePercent: 150);

        settings.ScalePercent.ShouldBe(150);
    }
}
