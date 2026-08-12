using System;
using FlatRedBall.Glue.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Tasks;

// Pins GitHub issue #2053: exitwhenquiet never fired for projects whose queued work finished
// draining before the watcher's first poll ever observed a busy tick.
public class QuietExitWatcherTests
{
    [Fact]
    public void Tick_ReturnsTrue_WhenIdleForQuietDuration_EvenIfNeverObservedBusy()
    {
        var current = new DateTime(2026, 1, 1, 0, 0, 0);
        var watcher = new QuietExitWatcher(TimeSpan.FromSeconds(12), () => current);

        // All of the project's work ran (and drained) before the watcher started polling - every
        // tick sees an already-idle TaskManager, exactly like the GlueLoaderScratch repro in #2053.
        watcher.Tick(isBusy: false).ShouldBeFalse();

        current = current.AddSeconds(12);

        watcher.Tick(isBusy: false).ShouldBeTrue();
    }

    [Fact]
    public void Tick_ReturnsFalse_WhileBusy()
    {
        var current = new DateTime(2026, 1, 1, 0, 0, 0);
        var watcher = new QuietExitWatcher(TimeSpan.FromSeconds(12), () => current);

        watcher.Tick(isBusy: true).ShouldBeFalse();

        current = current.AddSeconds(20);

        watcher.Tick(isBusy: true).ShouldBeFalse();
    }

    [Fact]
    public void Tick_ResetsQuietClock_WhenBusyAgainAfterIdle()
    {
        var current = new DateTime(2026, 1, 1, 0, 0, 0);
        var watcher = new QuietExitWatcher(TimeSpan.FromSeconds(12), () => current);

        watcher.Tick(isBusy: false).ShouldBeFalse();

        current = current.AddSeconds(6);
        watcher.Tick(isBusy: true).ShouldBeFalse();

        current = current.AddSeconds(6);
        watcher.Tick(isBusy: false).ShouldBeFalse();

        current = current.AddSeconds(12);
        watcher.Tick(isBusy: false).ShouldBeTrue();
    }
}
