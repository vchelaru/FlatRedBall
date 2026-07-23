using System;
using System.Threading;

namespace FlatRedBall.Glue.Utilities
{
    /// <summary>
    /// Small reusable retry-with-backoff helper. Intended for transient failures such as a file
    /// being briefly locked by another process (e.g. a just-exited subprocess whose file handle
    /// hasn't fully released yet, an antivirus scan, or an IDE file watcher).
    /// </summary>
    public static class RetryHelper
    {
        public const int DefaultMaxAttempts = 3;
        public static readonly TimeSpan DefaultInitialDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Invokes <paramref name="operation"/>, retrying up to <paramref name="maxAttempts"/> times
        /// total if it throws an exception that <paramref name="isTransient"/> identifies as transient.
        /// The delay between attempts starts at <paramref name="initialDelay"/> and doubles each retry.
        /// A non-transient exception, or the exception from the final attempt, propagates to the caller.
        /// </summary>
        public static T Retry<T>(
            Func<T> operation,
            Func<Exception, bool> isTransient,
            int maxAttempts = DefaultMaxAttempts,
            TimeSpan? initialDelay = null)
        {
            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "maxAttempts must be at least 1.");
            }

            var delay = initialDelay ?? DefaultInitialDelay;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex) when (attempt < maxAttempts && isTransient(ex))
                {
                    Thread.Sleep(delay);
                    delay += delay;
                }
            }

            // Unreachable: the loop above either returns on success, or - once attempt == maxAttempts -
            // the `when` clause is false so the exception propagates out of the loop instead of being caught.
            throw new InvalidOperationException("RetryHelper.Retry reached unreachable code.");
        }

        /// <summary>
        /// Action overload of <see cref="Retry{T}"/>.
        /// </summary>
        public static void Retry(
            Action operation,
            Func<Exception, bool> isTransient,
            int maxAttempts = DefaultMaxAttempts,
            TimeSpan? initialDelay = null)
        {
            Retry<object>(
                () => { operation(); return null; },
                isTransient,
                maxAttempts,
                initialDelay);
        }
    }
}
