using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ViewModels;

/// <summary>
/// GitHub issue #2043: MainCodePreviewPlugin and MainPropertiesTabOldPlugin each add a tab to the right
/// panel and immediately hide it again during their own StartUp, before anything is selected. That
/// transient churn drives TabControlViewModel.RightPanelWidth to exactly 0 via AdjustGrid's "no tabs"
/// branch. If the app is closed while that 0 is still in effect, GlueCommands.UpdateGlueSettingsFromCurrentGlueStateImmediately
/// persists it into GlueSettingsSave.RightTabWidthPixels. On every later launch, AdjustGrid's fallback to
/// a default width only kicks in for a *missing* (null) saved width - a persisted 0 is a valid double, so
/// it is treated as a deliberate preference forever, collapsing the panel to the MinPanelConverter floor
/// until the user manually drags the splitter to a positive width.
/// </summary>
public class TabControlViewModelPanelWidthTests
{
    [Fact]
    public void RightPanelWidth_ShouldFallBackToDefault_WhenSavedWidthIsZero()
    {
        GlueTestBootstrap.EnsureInitialized();

        var originalSettings = GlueState.Self.GlueSettingsSave;
        try
        {
            GlueState.Self.GlueSettingsSave = new GlueSettingsSave { RightTabWidthPixels = 0 };

            var tabControlViewModel = new TabControlViewModel();
            tabControlViewModel.RightTabItems.Add(new PluginTab());

            tabControlViewModel.RightPanelWidth.Value.ShouldBeGreaterThan(0);
        }
        finally
        {
            GlueState.Self.GlueSettingsSave = originalSettings;
        }
    }

    [Fact]
    public void RightPanelWidth_ShouldStillCollapseToZero_WhenAllTabsAreRemoved()
    {
        GlueTestBootstrap.EnsureInitialized();

        var originalSettings = GlueState.Self.GlueSettingsSave;
        try
        {
            GlueState.Self.GlueSettingsSave = new GlueSettingsSave { RightTabWidthPixels = 386 };

            var tabControlViewModel = new TabControlViewModel();
            var tab = new PluginTab();
            tabControlViewModel.RightTabItems.Add(tab);
            tabControlViewModel.RightTabItems.Remove(tab);

            tabControlViewModel.RightPanelWidth.Value.ShouldBe(0);
        }
        finally
        {
            GlueState.Self.GlueSettingsSave = originalSettings;
        }
    }
}
