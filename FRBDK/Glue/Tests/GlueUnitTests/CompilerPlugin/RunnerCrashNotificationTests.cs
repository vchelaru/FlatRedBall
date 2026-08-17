using CompilerLibrary.ViewModels;
using CompilerPlugin.Managers;
using Shouldly;
using System;
using Xunit;

namespace GlueUnitTests.CompilerPlugin
{
    /// <summary>
    /// Covers issue #2113: a game crashing during a background "Run in Edit Mode" used to pop a
    /// blocking MessageBox even when the user wasn't looking at the editor. Runner.NotifyCrash is the
    /// seam that call goes through, so these tests pin that it's invoked with the crash message and
    /// nothing else, without touching the real (WPF-backed) toast implementation.
    /// </summary>
    public class RunnerCrashNotificationTests
    {
        static Runner CreateRunner(Action<string> notifyCrash) =>
            new Runner((_, __) => { }, CompilerViewModel.Self, notifyCrash);

        [Fact]
        public void NotifyCrash_InvokesInjectedNotifier_WithTheMessage()
        {
            string received = null;
            var runner = CreateRunner(message => received = message);

            runner.NotifyCrash("The game has crashed. See the Build tab for a callstack");

            received.ShouldBe("The game has crashed. See the Build tab for a callstack");
        }

        [Fact]
        public void NotifyCrash_DoesNotShowABlockingMessageBox()
        {
            // A blocking System.Windows.MessageBox.Show would hang this test waiting for a user click.
            // Completing at all (and receiving the message below) is proof no MessageBox was shown.
            var invocationCount = 0;
            var runner = CreateRunner(_ => invocationCount++);

            runner.NotifyCrash("crash");

            invocationCount.ShouldBe(1);
        }
    }
}
