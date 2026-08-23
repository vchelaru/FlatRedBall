using System;
using System.Threading.Tasks;

namespace GameCommunicationPlugin.GlueControl.CommandSending
{
    /// <summary>
    /// Retry budget for a DTO sent while the game might still be mid-startup (originally added for
    /// SetBorderlessDto, issue #2048; also used for the initial SetEditMode send, issue #2174).
    /// Runner_GameStarted fires as soon as the game process has a window handle - which can be before
    /// GameConnectionManager has even connected the socket - and the DTO is only actually handled once
    /// the game's Initialize has gotten as far as constructing GlueControlManager. Until then either the
    /// socket isn't connected yet, or it is but GlueControlManager.Self is null and the receive loop
    /// reports itself as not ready, so the command is dropped either way. Retrying past that gap is the
    /// whole point of this type.
    /// </summary>
    public static class GameReadinessRetryPolicy
    {
        public const int MaxAttempts = 40;

        public const int MillisecondsBetweenAttempts = 25;

        /// <summary>
        /// Total wall-clock time spent before giving up. Has to comfortably outlast the game's
        /// graphics-device setup (CameraSetup.SetupCamera runs between the connection being made and
        /// GlueControlManager being constructed), which is the step whose duration swings most
        /// between a warm machine and a cold GPU driver load.
        /// </summary>
        public static int TotalBudgetMilliseconds => MaxAttempts * MillisecondsBetweenAttempts;

        /// <summary>
        /// Runs <paramref name="attemptAsync"/> until it reports success or the budget is exhausted.
        /// Returns whether it ever succeeded. Delays only *between* attempts, never after the last
        /// one, so an exhausted budget doesn't cost an extra wait for nothing.
        /// </summary>
        /// <param name="attemptAsync">One attempt; true means it took effect.</param>
        /// <param name="delayAsync">How to wait between attempts. Injected so tests don't sleep.</param>
        public static async Task<bool> TryRepeatedlyAsync(
            Func<Task<bool>> attemptAsync,
            Func<int, Task> delayAsync = null,
            int maxAttempts = MaxAttempts,
            int millisecondsBetweenAttempts = MillisecondsBetweenAttempts)
        {
            if (attemptAsync == null)
            {
                throw new ArgumentNullException(nameof(attemptAsync));
            }

            delayAsync = delayAsync ?? (milliseconds => Task.Delay(milliseconds));

            for (int i = 0; i < maxAttempts; i++)
            {
                if (await attemptAsync())
                {
                    return true;
                }

                var isLastAttempt = i == maxAttempts - 1;

                if (!isLastAttempt)
                {
                    await delayAsync(millisecondsBetweenAttempts);
                }
            }

            return false;
        }
    }
}
