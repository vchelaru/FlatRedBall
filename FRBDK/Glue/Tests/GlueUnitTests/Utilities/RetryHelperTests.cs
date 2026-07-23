using System;
using Shouldly;
using FlatRedBall.Glue.Utilities;
using Xunit;

namespace GlueUnitTests.Utilities;

// Pins the retry-with-backoff helper added to fix intermittent "file being used by another process"
// failures when adding projects to a .sln (VSSolution.cs mutates the same .sln file back-to-back in a
// loop, racing a just-exited dotnet.exe subprocess's file handle / AV scan / IDE file watcher).
public class RetryHelperTests
{
    [Fact]
    public void Retry_ShouldReturnResult_WhenOperationSucceedsOnFirstAttempt()
    {
        var callCount = 0;

        var result = RetryHelper.Retry<int>(
            operation: () => { callCount++; return 42; },
            isTransient: ex => true);

        result.ShouldBe(42);
        callCount.ShouldBe(1);
    }

    [Fact]
    public void Retry_ShouldRetryAndSucceed_WhenOperationFailsTransientlyThenSucceeds()
    {
        var callCount = 0;

        var result = RetryHelper.Retry<int>(
            operation: () =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new InvalidOperationException("transient failure");
                }
                return 99;
            },
            isTransient: ex => true,
            maxAttempts: 5,
            initialDelay: TimeSpan.Zero);

        result.ShouldBe(99);
        callCount.ShouldBe(3);
    }

    [Fact]
    public void Retry_ShouldGiveUpAndThrowRealException_WhenAttemptsExhausted()
    {
        var callCount = 0;

        var thrown = Should.Throw<InvalidOperationException>(() =>
        {
            RetryHelper.Retry<int>(
                operation: () =>
                {
                    callCount++;
                    throw new InvalidOperationException($"failure #{callCount}");
                },
                isTransient: ex => true,
                maxAttempts: 3,
                initialDelay: TimeSpan.Zero);
        });

        callCount.ShouldBe(3);
        thrown.Message.ShouldBe("failure #3");
    }

    [Fact]
    public void Retry_ShouldNotRetry_WhenExceptionIsNotTransient()
    {
        var callCount = 0;

        Should.Throw<InvalidOperationException>(() =>
        {
            RetryHelper.Retry<int>(
                operation: () =>
                {
                    callCount++;
                    throw new InvalidOperationException("not transient");
                },
                isTransient: ex => false,
                maxAttempts: 5,
                initialDelay: TimeSpan.Zero);
        });

        callCount.ShouldBe(1);
    }

    [Fact]
    public void Retry_Action_ShouldRetryAndSucceed_WhenOperationFailsTransientlyThenSucceeds()
    {
        var callCount = 0;

        RetryHelper.Retry(
            operation: () =>
            {
                callCount++;
                if (callCount < 2)
                {
                    throw new InvalidOperationException("transient failure");
                }
            },
            isTransient: ex => true,
            maxAttempts: 3,
            initialDelay: TimeSpan.Zero);

        callCount.ShouldBe(2);
    }
}
