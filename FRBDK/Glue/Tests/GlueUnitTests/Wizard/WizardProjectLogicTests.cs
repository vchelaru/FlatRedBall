using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.Wizard;

// GitHub issue #1894 asked for unit coverage of WizardProjectLogic.Apply, using the TaskManager
// synchronous-mode / UI-thread-marshaller seams added in this same change (see
// GlueUnitTests.Tasks.TaskManagerSynchronousModeTests). Those seams are real and directly tested.
//
// But driving Apply() itself - even for the plugin-free "bare AddGameScreen" step suggested as a
// fallback - turns out to be blocked by a deeper coupling than plugins: HandleAddGameScreen ends up in
// ProjectCommands.CreateAndAddCodeFile, which throws NullReferenceException("Main Project") unless
// GlueState.CurrentMainProject is a real, MSBuild-backed VisualStudioProject (abstract, no fakeable
// interface) - the same blocker already called out in REFACTORING.md's "Known Areas Needing Improvement"
// section. Building an IVisualStudioProject seam to unblock that is a separate, larger refactor, out of
// scope here.
//
// What's covered instead: the pure, side-effect-free decision logic inside an Apply step that doesn't
// touch GlueState/TaskManager/plugins - GetDisplaySettingsFor, extracted from ApplyMainCameraSettings.
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
