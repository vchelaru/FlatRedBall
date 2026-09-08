using System.Linq;
using FlatRedBall.Graphics;
using Shouldly;

namespace EngineUnitTests.Graphics;

public class InfiniteScrollWindowTests
{
    [Fact]
    public void GetRequiredCopyCount_ViewSmallerThanPeriod_ReturnsMinimumOfThree()
    {
        InfiniteScrollWindow.GetRequiredCopyCount(viewSpan: 10, periodLength: 100).ShouldBe(3);
    }

    [Fact]
    public void GetRequiredCopyCount_ViewMuchLargerThanPeriod_ScalesWithView()
    {
        // view is 10 periods wide, so we need at least 10 copies plus margin on each side.
        InfiniteScrollWindow.GetRequiredCopyCount(viewSpan: 1000, periodLength: 100).ShouldBe(12);
    }

    [Fact]
    public void GetRequiredCopyCount_ZeroOrNegativePeriod_ReturnsOne()
    {
        InfiniteScrollWindow.GetRequiredCopyCount(viewSpan: 100, periodLength: 0).ShouldBe(1);
        InfiniteScrollWindow.GetRequiredCopyCount(viewSpan: 100, periodLength: -5).ShouldBe(1);
    }

    [Fact]
    public void TryRecycleSlot_SlotOverlapsRequiredRange_DoesNotRecycle()
    {
        int min = -1;
        int max = 1;

        var recycled = InfiniteScrollWindow.TryRecycleSlot(
            currentPeriodIndex: 0, periodLength: 100,
            requiredMinLocal: -20, requiredMaxLocal: 20,
            minPeriodIndex: ref min, maxPeriodIndex: ref max,
            newPeriodIndex: out var newIndex);

        recycled.ShouldBeFalse();
        newIndex.ShouldBe(0);
        min.ShouldBe(-1);
        max.ShouldBe(1);
    }

    [Fact]
    public void TryRecycleSlot_SlotFullyBeforeRequiredRange_RecyclesToNewMax()
    {
        // Window covers period indices -1, 0, 1 (each 100 wide: [-100,0), [0,100), [100,200)).
        // The camera has scrolled so the required range is now [250, 350) - index -1 is long gone.
        int min = -1;
        int max = 1;

        var recycled = InfiniteScrollWindow.TryRecycleSlot(
            currentPeriodIndex: -1, periodLength: 100,
            requiredMinLocal: 250, requiredMaxLocal: 350,
            minPeriodIndex: ref min, maxPeriodIndex: ref max,
            newPeriodIndex: out var newIndex);

        recycled.ShouldBeTrue();
        newIndex.ShouldBe(2);
        min.ShouldBe(0);
        max.ShouldBe(2);
    }

    [Fact]
    public void TryRecycleSlot_SlotFullyAfterRequiredRange_RecyclesToNewMin()
    {
        int min = -1;
        int max = 1;

        var recycled = InfiniteScrollWindow.TryRecycleSlot(
            currentPeriodIndex: 1, periodLength: 100,
            requiredMinLocal: -350, requiredMaxLocal: -250,
            minPeriodIndex: ref min, maxPeriodIndex: ref max,
            newPeriodIndex: out var newIndex);

        // The near edge of the gap (requiredMaxLocal = -250) falls in period index -3's span
        // ([-300,-200)), so the recycled copy jumps straight there rather than just one step
        // (-2) short of actually covering anything.
        recycled.ShouldBeTrue();
        newIndex.ShouldBe(-3);
        min.ShouldBe(-3);
        max.ShouldBe(0);
    }

    [Fact]
    public void TryRecycleSlot_ContinuousScrollRight_AlwaysKeepsRequiredRangeCovered()
    {
        // Simulates a window of 3 copies (period 100) recycling as the camera scrolls
        // continuously to the right, one period at a time, over several steps. The exact set of
        // occupied period indices depends on slot processing order (a slot that's still needed is
        // left alone, so the window is not always perfectly contiguous), but the one invariant
        // that actually matters for rendering is that every point the camera can currently see is
        // covered by at least one slot's span - i.e. nothing that's on screen is ever left unbaked.
        int min = -1;
        int max = 1;
        var indices = new[] { -1, 0, 1 };
        const float periodLength = 100;

        for (int step = 1; step <= 20; step++)
        {
            float requiredMin = step * 100 + 10;
            float requiredMax = step * 100 + 90;

            for (int slot = 0; slot < indices.Length; slot++)
            {
                if (InfiniteScrollWindow.TryRecycleSlot(
                        indices[slot], periodLength, requiredMin, requiredMax,
                        ref min, ref max, out var newIndex))
                {
                    indices[slot] = newIndex;
                }
            }

            bool isCovered = indices.Any(index =>
                index * periodLength <= requiredMin && (index * periodLength + periodLength) >= requiredMax);

            isCovered.ShouldBeTrue($"step {step}: required [{requiredMin},{requiredMax}] not covered by slots [{string.Join(",", indices)}]");
        }
    }

    [Fact]
    public void TryRecycleSlot_LargeTeleportBeyondWindow_SelfHealsWithinAFewFrames()
    {
        // A camera teleport far outside the currently-baked window can't be caught up in a single
        // recycle pass (each slot only moves one period per pass), but repeatedly running the same
        // recycle pass (as happens once per rendered frame) must converge on covering the new
        // required range within a small, bounded number of frames rather than getting stuck.
        int min = -1;
        int max = 1;
        var indices = new[] { -1, 0, 1 };
        const float periodLength = 100;

        // Teleport far away: required range is now around period index 500.
        float requiredMin = 500 * periodLength + 10;
        float requiredMax = 500 * periodLength + 90;

        bool isCovered = false;
        for (int frame = 0; frame < 10 && !isCovered; frame++)
        {
            for (int slot = 0; slot < indices.Length; slot++)
            {
                if (InfiniteScrollWindow.TryRecycleSlot(
                        indices[slot], periodLength, requiredMin, requiredMax,
                        ref min, ref max, out var newIndex))
                {
                    indices[slot] = newIndex;
                }
            }

            isCovered = indices.Any(index =>
                index * periodLength <= requiredMin && (index * periodLength + periodLength) >= requiredMax);
        }

        isCovered.ShouldBeTrue("a large teleport should self-heal to full coverage within a handful of frames");
    }

    [Fact]
    public void TryRecycleSlot_ZeroPeriodLength_DoesNotRecycle()
    {
        int min = 0;
        int max = 0;

        var recycled = InfiniteScrollWindow.TryRecycleSlot(
            currentPeriodIndex: 0, periodLength: 0,
            requiredMinLocal: 1000, requiredMaxLocal: 2000,
            minPeriodIndex: ref min, maxPeriodIndex: ref max,
            newPeriodIndex: out var newIndex);

        recycled.ShouldBeFalse();
        newIndex.ShouldBe(0);
    }
}
