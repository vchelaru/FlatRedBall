using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameCommunicationPlugin.GlueControl.Views
{
    /// <summary>
    /// When the game's window is reparented into the Game tab, MonoGame/SDL keeps using the position it
    /// cached while the window was still top-level to turn the cursor's screen position into client
    /// coordinates. Until something moves the window relative to its new parent, clicks and drag
    /// rectangles land offset by exactly the reparent distance. `GameHostView.ForceRefreshGameArea` is
    /// what forces that move (see its "Victor Chelaru Oct 4" comment); this decides when to run it.
    /// </summary>
    public static class EmbedSettlePolicy
    {
        /// <summary>
        /// How long to wait before each refresh pass.
        /// </summary>
        /// <remarks>
        /// More than one pass because nothing can detect whether a pass worked: the window is drawn
        /// correctly either way, and the stale coordinates only show up when the user clicks. A pass that
        /// lands before the window has settled after the borderless style change is simply lost, so the
        /// later ones are there to cover a slow window - each costs a brief flicker of the left panel,
        /// which is why they are spread out rather than repeated tightly.
        /// </remarks>
        public static readonly IReadOnlyList<int> DelaysBeforeEachRefreshMilliseconds = new[] { 50, 250, 750 };

        /// <summary>
        /// Waits and refreshes once per entry in <paramref name="delaysBeforeEachRefresh"/>.
        /// </summary>
        /// <param name="refreshAsync">One refresh pass.</param>
        /// <param name="delayAsync">How to wait. Injected so tests don't sleep.</param>
        public static async Task SettleAsync(
            Func<Task> refreshAsync,
            Func<int, Task> delayAsync = null,
            IReadOnlyList<int> delaysBeforeEachRefresh = null)
        {
            if (refreshAsync == null)
            {
                throw new ArgumentNullException(nameof(refreshAsync));
            }

            delayAsync = delayAsync ?? (milliseconds => Task.Delay(milliseconds));
            delaysBeforeEachRefresh = delaysBeforeEachRefresh ?? DelaysBeforeEachRefreshMilliseconds;

            foreach (var delay in delaysBeforeEachRefresh)
            {
                await delayAsync(delay);
                await refreshAsync();
            }
        }
    }
}
