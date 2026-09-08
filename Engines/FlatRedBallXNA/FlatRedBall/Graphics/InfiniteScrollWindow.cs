namespace FlatRedBall.Graphics
{
    /// <summary>
    /// Pure math for recycling a row of evenly-spaced, seamlessly repeating copies (used to draw
    /// an infinitely-scrolling tile layer from a small, fixed number of baked copies). Each copy
    /// occupies an integer "period index" along one axis; as the required visible range moves past
    /// a copy, that copy is recycled (its period index reassigned) to become the next copy needed
    /// at the opposite edge of the window, keeping the occupied period indices a contiguous run.
    /// </summary>
    public static class InfiniteScrollWindow
    {
        /// <summary>
        /// How many copies are needed to always be able to fully cover a view of the given span,
        /// with at least one spare copy on each side so a copy can be recycled before it is needed.
        /// </summary>
        public static int GetRequiredCopyCount(float viewSpan, float periodLength)
        {
            if (periodLength <= 0)
            {
                return 1;
            }
            if (viewSpan <= 0)
            {
                return 3;
            }
            return System.Math.Max(3, (int)System.Math.Ceiling(viewSpan / periodLength) + 2);
        }

        /// <summary>
        /// Checks whether a single copy has scrolled entirely outside the required visible range
        /// and, if so, recycles it to the opposite edge of the window.
        /// </summary>
        /// <param name="currentPeriodIndex">The copy's current period index.</param>
        /// <param name="periodLength">The world-space length of one period. Must be positive.</param>
        /// <param name="requiredMinLocal">The minimum local coordinate that must remain covered.</param>
        /// <param name="requiredMaxLocal">The maximum local coordinate that must remain covered.</param>
        /// <param name="minPeriodIndex">The window's current lowest occupied period index. Updated in place.</param>
        /// <param name="maxPeriodIndex">The window's current highest occupied period index. Updated in place.</param>
        /// <param name="newPeriodIndex">The copy's period index after this call.</param>
        /// <returns>True if the copy was recycled (its period index changed).</returns>
        public static bool TryRecycleSlot(
            int currentPeriodIndex,
            float periodLength,
            float requiredMinLocal,
            float requiredMaxLocal,
            ref int minPeriodIndex,
            ref int maxPeriodIndex,
            out int newPeriodIndex)
        {
            newPeriodIndex = currentPeriodIndex;

            if (periodLength <= 0)
            {
                return false;
            }

            float slotMin = currentPeriodIndex * periodLength;
            float slotMax = slotMin + periodLength;

            if (slotMax < requiredMinLocal)
            {
                // Fully before the required range - recycle to become the new highest copy.
                // Normally that's just one period past the current highest copy, but if the
                // required range has jumped far away (e.g. the camera teleported), crawling
                // forward one period per recycle would take many frames to catch up, so jump
                // straight to where the copy is actually needed instead.
                int neededIndex = (int)System.Math.Floor(requiredMinLocal / periodLength);
                newPeriodIndex = System.Math.Max(maxPeriodIndex + 1, neededIndex);
                maxPeriodIndex = newPeriodIndex;
                if (currentPeriodIndex == minPeriodIndex)
                {
                    minPeriodIndex++;
                }
                return true;
            }

            if (slotMin > requiredMaxLocal)
            {
                // Fully after the required range - recycle to become the new lowest copy,
                // jumping straight there if the required range is far below the current window.
                int neededIndex = (int)System.Math.Floor(requiredMaxLocal / periodLength);
                newPeriodIndex = System.Math.Min(minPeriodIndex - 1, neededIndex);
                minPeriodIndex = newPeriodIndex;
                if (currentPeriodIndex == maxPeriodIndex)
                {
                    maxPeriodIndex--;
                }
                return true;
            }

            return false;
        }
    }
}
