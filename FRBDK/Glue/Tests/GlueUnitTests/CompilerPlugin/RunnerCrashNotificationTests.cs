using CompilerLibrary.ViewModels;
using CompilerPlugin.Managers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using Shouldly;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.CompilerPlugin
{
    /// <summary>
    /// Covers issue #2113: a game crashing during a background "Run in Edit Mode" used to pop a
    /// blocking MessageBox even when the user wasn't looking at the editor. Runner.NotifyIfCrashed is
    /// what Runner.HandleProcessExit (subscribed to the real child game Process's Exited event) calls
    /// with the process's real exit code, so driving it directly here - with a real GlueState project
    /// and a real CrashInfo.txt on disk - exercises the actual crash-message-building path
    /// (Runner.GetCrashMessage) end to end, not just the injected-notifier seam. Only the OS-level
    /// Process.Exited subscription itself (a single `+=` in Runner.Run/HandleTimerTick) is left
    /// untested here - see the maintenance-mode-workflow skill on when a live-Process harness is worth
    /// building vs. leaving that one wire-up to manual verification.
    /// </summary>
    [Collection(nameof(TaskManagerSequentialCollection))]
    public class RunnerCrashNotificationTests : IDisposable
    {
        readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
        readonly bool _originalSynchronousMode;
        readonly IUiThreadMarshaller _originalMarshaller;
        readonly string _tempProjectDirectory;
        readonly string _gameOutputDirectory;

        public RunnerCrashNotificationTests()
        {
            GlueTestBootstrap.EnsureInitialized();

            _originalMainProject = GlueState.Self.CurrentMainProject;
            _originalSynchronousMode = TaskManager.SynchronousMode;
            _originalMarshaller = TaskManager.UiThreadMarshaller;

            var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
            GlueState.Self.CurrentMainProject = vsProject;

            // Matches VisualStudioProject.GetOutputDirectory()'s fallback ("bin/Debug/") for a project
            // with no explicit OutputPath - the same layout GetCrashMessage's real exeLocation -> logFile
            // resolution walks.
            _gameOutputDirectory = Path.Combine(_tempProjectDirectory, "bin", "Debug");
            Directory.CreateDirectory(_gameOutputDirectory);

            TaskManager.SynchronousMode = true;
            TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();
        }

        public void Dispose()
        {
            GlueState.Self.CurrentMainProject = _originalMainProject;
            TaskManager.SynchronousMode = _originalSynchronousMode;
            TaskManager.UiThreadMarshaller = _originalMarshaller;
        }

        Runner CreateRunner(Action<string> notifyCrash) =>
            new Runner((_, __) => { }, CompilerViewModel.Self, notifyCrash);

        [Fact]
        public async Task NotifyIfCrashed_WithCrashInfoFile_NotifiesWithBuildTabMessage_NotABlockingMessageBox()
        {
            File.WriteAllText(Path.Combine(_gameOutputDirectory, "CrashInfo.txt"), "at SomeMethod() line 1");

            string notifiedMessage = null;
            var runner = CreateRunner(message => notifiedMessage = message);

            // A blocking System.Windows.MessageBox.Show would hang this test waiting for a user click -
            // completing at all, with the message below, is proof no MessageBox was shown.
            await runner.NotifyIfCrashed(exitCode: 1);

            notifiedMessage.ShouldBe("The game has crashed. See the Build tab for a callstack");
        }

        [Fact]
        public async Task NotifyIfCrashed_AlsoLogsCrashInfoContentsToTheBuildTab()
        {
            File.WriteAllText(Path.Combine(_gameOutputDirectory, "CrashInfo.txt"), "at SomeMethod() line 1");

            string loggedContents = null;
            var runner = CreateRunner(_ => { });
            runner.ErrorReceived += contents => loggedContents = contents;

            await runner.NotifyIfCrashed(exitCode: 1);

            loggedContents.ShouldBe("at SomeMethod() line 1");
        }

        [Fact]
        public async Task NotifyIfCrashed_WithoutCrashInfoFile_NotifiesWithFallbackMessage()
        {
            string notifiedMessage = null;
            var runner = CreateRunner(message => notifiedMessage = message);

            await runner.NotifyIfCrashed(exitCode: 1);

            notifiedMessage.ShouldBe("Oh no! The game crashed. Run from Visual Studio for more information on the error.");
        }

        [Fact]
        public async Task NotifyIfCrashed_WithZeroExitCode_DoesNotNotify()
        {
            var wasNotified = false;
            var runner = CreateRunner(_ => wasNotified = true);

            await runner.NotifyIfCrashed(exitCode: 0);

            wasNotified.ShouldBeFalse();
        }

        private class InlineUiThreadMarshaller : IUiThreadMarshaller
        {
            public void Invoke(Action action) => action();
            public T Invoke<T>(Func<T> func) => func();
            public Task Invoke(Func<Task> func) => func();
            public Task<T> Invoke<T>(Func<Task<T>> func) => func();
            public void BeginInvoke(Action action) => action();
        }
    }
}
