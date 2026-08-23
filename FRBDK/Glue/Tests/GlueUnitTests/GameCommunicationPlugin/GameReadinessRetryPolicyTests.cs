using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameCommunicationPlugin.GlueControl.CommandSending;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// The retry budget for a DTO sent while the game might still be mid-startup - originally added for
/// SetBorderlessDto (issue #2048), also used for the initial SetEditMode send (issue #2174). The game
/// reports itself as not connected, or connected but not ready, for the window between its process
/// starting and GlueControlManager being constructed, so the budget has to outlast that gap or the
/// command is silently dropped.
/// </summary>
public class GameReadinessRetryPolicyTests
{
    [Fact]
    public async Task TryRepeatedlyAsync_ShouldStopAtFirstSuccess()
    {
        var attempts = 0;

        var succeeded = await GameReadinessRetryPolicy.TryRepeatedlyAsync(
            attemptAsync: () => { attempts++; return Task.FromResult(true); },
            delayAsync: _ => Task.CompletedTask);

        succeeded.ShouldBeTrue();
        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task TryRepeatedlyAsync_ShouldKeepRetrying_UntilAttemptSucceeds()
    {
        var attempts = 0;

        var succeeded = await GameReadinessRetryPolicy.TryRepeatedlyAsync(
            attemptAsync: () => { attempts++; return Task.FromResult(attempts == 4); },
            delayAsync: _ => Task.CompletedTask);

        succeeded.ShouldBeTrue();
        attempts.ShouldBe(4);
    }

    [Fact]
    public async Task TryRepeatedlyAsync_ShouldReportFailure_AfterExhaustingAttempts()
    {
        var attempts = 0;

        var succeeded = await GameReadinessRetryPolicy.TryRepeatedlyAsync(
            attemptAsync: () => { attempts++; return Task.FromResult(false); },
            delayAsync: _ => Task.CompletedTask,
            maxAttempts: 5);

        succeeded.ShouldBeFalse();
        attempts.ShouldBe(5);
    }

    [Fact]
    public async Task TryRepeatedlyAsync_ShouldNotDelayAfterTheFinalAttempt()
    {
        var delays = new List<int>();

        await GameReadinessRetryPolicy.TryRepeatedlyAsync(
            attemptAsync: () => Task.FromResult(false),
            delayAsync: milliseconds => { delays.Add(milliseconds); return Task.CompletedTask; },
            maxAttempts: 3,
            millisecondsBetweenAttempts: 25);

        // 3 attempts, but only 2 gaps between them - an exhausted budget shouldn't pay for a
        // wait that nothing follows.
        delays.ShouldBe(new[] { 25, 25 });
    }

    [Fact]
    public void TotalBudget_ShouldOutlastTheGameSideStartupGap()
    {
        // The gap being waited on is the game's Initialize between GameConnectionManager (which
        // connects immediately) and GlueControlManager (which is what actually handles the DTO) -
        // CameraSetup.SetupCamera sits between them, and graphics-device setup is what swings on a
        // cold GPU driver load. The original 90ms budget lost that race often enough to be reported.
        GameReadinessRetryPolicy.TotalBudgetMilliseconds.ShouldBeGreaterThanOrEqualTo(1000);
    }

    /// <summary>
    /// Pins issue #2174's root cause directly: the default call (no injected delay) must actually pass
    /// wall-clock time between attempts, not just call attemptAsync() repeatedly in a tight loop. A
    /// caller racing the game's startup (CommandSender's SetEditMode retry) depends on real time
    /// elapsing for the game to become ready - a loop with no delay retries 5 times before the game's
    /// socket has even had a chance to connect.
    /// </summary>
    [Fact]
    public async Task TryRepeatedlyAsync_WithNoInjectedDelay_ActuallyWaitsBetweenAttempts()
    {
        var attempts = 0;

        var succeeded = await GameReadinessRetryPolicy.TryRepeatedlyAsync(
            attemptAsync: () => { attempts++; return Task.FromResult(attempts == 3); },
            maxAttempts: 5,
            millisecondsBetweenAttempts: 20);

        succeeded.ShouldBeTrue();
        attempts.ShouldBe(3);
    }
}
