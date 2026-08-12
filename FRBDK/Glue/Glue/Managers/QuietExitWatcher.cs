using System;

namespace FlatRedBall.Glue.Managers
{
    /// <summary>
    /// Decides when TaskManager has been idle long enough for the "exitwhenquiet" automation hook
    /// (see <see cref="CommandLineManager.ExitWhenQuietSeconds"/>) to close Glue. Extracted from
    /// MainGlueWindow's WinForms timer so the idle/quiet bookkeeping can be unit tested without a
    /// real window.
    /// </summary>
    public class QuietExitWatcher
    {
        readonly TimeSpan quietDuration;
        readonly Func<DateTime> now;
        DateTime lastBusyTime;

        public QuietExitWatcher(TimeSpan quietDuration, Func<DateTime> now)
        {
            this.quietDuration = quietDuration;
            this.now = now;
            // Seed the clock as of construction rather than requiring a busy tick to be observed
            // first: by the time this watcher is created, the load it's watching for has already
            // been kicked off (and, for a fast-loading project, may have already fully drained
            // before the first Tick ever runs) - see #2053.
            lastBusyTime = now();
        }

        /// <summary>
        /// Reports the current busy state. Returns true once TaskManager has been continuously idle
        /// for at least <see cref="quietDuration"/> since the last observed busy tick (or since
        /// construction, if it has never been busy).
        /// </summary>
        public bool Tick(bool isBusy)
        {
            if (isBusy)
            {
                lastBusyTime = now();
                return false;
            }

            return (now() - lastBusyTime) >= quietDuration;
        }
    }
}
