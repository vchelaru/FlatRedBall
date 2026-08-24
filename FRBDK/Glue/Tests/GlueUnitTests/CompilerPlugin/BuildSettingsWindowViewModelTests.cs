using CompilerPlugin.Models;
using CompilerPlugin.ViewModels;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CompilerPlugin
{
    /// <summary>
    /// Covers issue #2200: the "Use MSBuild Server" checkbox must round-trip through
    /// BuildSettingsUser (the JSON persisted to BuildSettings.user.json) same as the existing
    /// CustomMsBuildLocation setting.
    /// </summary>
    public class BuildSettingsWindowViewModelTests
    {
        [Fact]
        public void SetFrom_ThenApplyTo_RoundTripsUseMsBuildServer()
        {
            var source = new BuildSettingsUser { UseMsBuildServer = true };
            var viewModel = new BuildSettingsWindowViewModel();

            viewModel.SetFrom(source);

            viewModel.UseMsBuildServer.ShouldBeTrue();

            var target = new BuildSettingsUser { UseMsBuildServer = false };
            viewModel.ApplyTo(target);

            target.UseMsBuildServer.ShouldBeTrue();
        }
    }
}
