using System;
using System.Threading;
using FlatRedBall.Glue.Utilities;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Utilities;

// Glue runs `dotnet --list-sdks` through this so it overlaps startup. MSBUILD_EXE_PATH must still be set
// before plugins load, so EnsureCompleted has to wait for the work and apply it exactly once.
public class BackgroundStartupStepTests
{
    [Fact]
    public void EnsureCompleted_ShouldWaitForBackgroundWork_ThenApplyItsResult()
    {
        using var release = new ManualResetEventSlim();
        int? applied = null;
        var producerThread = 0;
        var step = new BackgroundStartupStep<int>(
            produce: () => { producerThread = Environment.CurrentManagedThreadId; release.Wait(); return 42; },
            consume: value => applied = value);

        step.Start();
        applied.ShouldBeNull();
        release.Set();
        step.EnsureCompleted();

        applied.ShouldBe(42);
        producerThread.ShouldNotBe(Environment.CurrentManagedThreadId);
    }

    [Fact]
    public void EnsureCompleted_ShouldApplyOnce_WhenCalledTwice()
    {
        var applyCount = 0;
        var step = new BackgroundStartupStep<int>(() => 1, _ => applyCount++);

        step.Start();
        step.EnsureCompleted();
        step.EnsureCompleted();

        applyCount.ShouldBe(1);
    }

    [Fact]
    public void EnsureCompleted_ShouldDoNothing_WhenNeverStarted()
    {
        var produced = false;
        var applied = false;
        var step = new BackgroundStartupStep<int>(() => { produced = true; return 1; }, _ => applied = true);

        step.EnsureCompleted();

        produced.ShouldBeFalse();
        applied.ShouldBeFalse();
    }

    [Fact]
    public void EnsureCompleted_ShouldRethrowBackgroundException()
    {
        var step = new BackgroundStartupStep<int>(() => throw new InvalidOperationException("no dotnet"), _ => { });

        step.Start();

        Should.Throw<InvalidOperationException>(() => step.EnsureCompleted()).Message.ShouldBe("no dotnet");
    }
}
