using GameCommunicationPlugin.GlueControl.Views;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// How many times the embedded game window gets nudged after being reparented into the Game tab, and
/// how long it is given to settle first. Getting this wrong is not a crash: the game embeds and draws
/// correctly, but every click and drag-rectangle lands offset from the cursor, because MonoGame/SDL is
/// still translating screen coordinates with the position it cached before the reparent.
/// </summary>
public class EmbedSettlePolicyTests
{
    static (List<int> delays, int refreshes) Record(IReadOnlyList<int> schedule = null)
    {
        var delays = new List<int>();
        var refreshes = 0;

        EmbedSettlePolicy.SettleAsync(
            refreshAsync: () => { refreshes++; return Task.CompletedTask; },
            delayAsync: milliseconds => { delays.Add(milliseconds); return Task.CompletedTask; },
            delaysBeforeEachRefresh: schedule).Wait();

        return (delays, refreshes);
    }

    [Fact]
    public async Task SettleAsync_WaitsBeforeEachRefresh()
    {
        var order = new List<string>();

        await EmbedSettlePolicy.SettleAsync(
            refreshAsync: () => { order.Add("refresh"); return Task.CompletedTask; },
            delayAsync: _ => { order.Add("delay"); return Task.CompletedTask; },
            delaysBeforeEachRefresh: new[] { 10, 20 });

        order.ShouldBe(new[] { "delay", "refresh", "delay", "refresh" });
    }

    [Fact]
    public async Task SettleAsync_RefreshesOncePerScheduledDelay()
    {
        var refreshes = 0;

        await EmbedSettlePolicy.SettleAsync(
            refreshAsync: () => { refreshes++; return Task.CompletedTask; },
            delayAsync: _ => Task.CompletedTask,
            delaysBeforeEachRefresh: new[] { 1, 2, 3 });

        refreshes.ShouldBe(3);
    }

    /// <summary>
    /// A single pass is a race: it can land before the window has finished settling after the borderless
    /// style change, and a nudge SDL never latches leaves the stale position in place for the rest of the
    /// session. Nothing detects that, so the only fix available is to nudge again later.
    /// </summary>
    [Fact]
    public void TheRealSchedule_NudgesMoreThanOnce()
    {
        var (_, refreshes) = Record();

        refreshes.ShouldBeGreaterThan(1, "one pass that lands too early is never retried");
    }

    /// <summary>
    /// Each pass costs a brief visible flicker of the left panel, so the extra ones have to be spread far
    /// enough apart to actually cover a slow window rather than all landing inside the same bad moment.
    /// </summary>
    [Fact]
    public void TheRealSchedule_KeepsNudgingWellPastTheFirstPass()
    {
        var (delays, _) = Record();

        delays.First().ShouldBeLessThanOrEqualTo(50, "the first pass should stay as prompt as it is today");
        delays.Sum().ShouldBeGreaterThanOrEqualTo(1000, "the last pass has to outlast a slow window setup");
    }
}
