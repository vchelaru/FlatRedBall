using CompilerLibrary.ViewModels;
using CompilerPlugin.Models;
using CompilerPlugin.ViewModels;
using Shouldly;
using System;
using Xunit;

namespace GlueUnitTests.CompilerPlugin
{
    /// <summary>
    /// Covers issue #2200: the "Use MSBuild Server" and "Print MSBuild Command" settings, both shown
    /// in the Build Settings dialog behind the Build tab's gear icon, must round-trip correctly even
    /// though they're backed by different stores - UseMsBuildServer by BuildSettingsUser (persisted to
    /// BuildSettings.user.json), IsPrintMsBuildCommandChecked by CompilerViewModel.Self (session-only,
    /// same as before it moved into this dialog from the Build tab's toolbar).
    /// </summary>
    public class BuildSettingsWindowViewModelTests : IDisposable
    {
        readonly bool _originalIsPrintMsBuildCommandChecked;

        public BuildSettingsWindowViewModelTests()
        {
            _originalIsPrintMsBuildCommandChecked = CompilerViewModel.Self.IsPrintMsBuildCommandChecked;
        }

        public void Dispose()
        {
            CompilerViewModel.Self.IsPrintMsBuildCommandChecked = _originalIsPrintMsBuildCommandChecked;
        }

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

        [Fact]
        public void SetFrom_ThenApplyTo_RoundTripsIsPrintMsBuildCommandChecked()
        {
            CompilerViewModel.Self.IsPrintMsBuildCommandChecked = true;
            var viewModel = new BuildSettingsWindowViewModel();

            viewModel.SetFrom(new BuildSettingsUser());

            viewModel.IsPrintMsBuildCommandChecked.ShouldBeTrue();

            viewModel.IsPrintMsBuildCommandChecked = false;
            viewModel.ApplyTo(new BuildSettingsUser());

            CompilerViewModel.Self.IsPrintMsBuildCommandChecked.ShouldBeFalse();
        }
    }
}
