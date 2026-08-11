using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameCommunicationPlugin.GlueControl.Views;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// The retry budget for the SetBorderlessDto sent when the game is embedded in the Game tab
/// (issue #2048). The game answers with an empty payload - which reads as failure - for the window
/// between its socket connecting and GlueControlManager being constructed, so the budget has to
/// outlast that gap or the game keeps its window frame for the rest of the session.
/// </summary>
public class BorderlessRetryPolicyTests
{
    [Fact]
    public async Task TryRepeatedlyAsync_ShouldStopAtFirstSuccess()
    {
        var attempts = 0;

        var succeeded = await BorderlessRetryPolicy.TryRepeatedlyAsync(
            attemptAsync: () => { attempts++; return Task.FromResult(true); },
            delayAsync: _ => Task.CompletedTask);

        succeeded.ShouldBeTrue();
        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task TryRepeatedlyAsync_ShouldKeepRetrying_UntilAttemptSucceeds()
    {
        var attempts = 0;

        var succeeded = await BorderlessRetryPolicy.TryRepeatedlyAsync(
            attemptAsync: () => { attempts++; return Task.FromResult(attempts == 4); },
            delayAsync: _ => Task.CompletedTask);

        succeeded.ShouldBeTrue();
        attempts.ShouldBe(4);
    }

    [Fact]
    public async Task TryRepeatedlyAsync_ShouldReportFailure_AfterExhaustingAttempts()
    {
        var attempts = 0;

        var succeeded = await BorderlessRetryPolicy.TryRepeatedlyAsync(
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

        await BorderlessRetryPolicy.TryRepeatedlyAsync(
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
        BorderlessRetryPolicy.TotalBudgetMilliseconds.ShouldBeGreaterThanOrEqualTo(1000);
    }
}
