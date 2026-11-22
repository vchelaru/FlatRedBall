using System.Collections.Generic;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Tasks;

public class ThrottledActionTests
{
    // Each delay the action awaits is completed by hand, so the tests decide when a run happens.
    readonly List<TaskCompletionSource> delays = new();
    int runCount;

    ThrottledAction Create(System.Action extra = null) => new ThrottledAction(
        () => { extra?.Invoke(); runCount++; },
        () =>
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            delays.Add(tcs);
            return tcs.Task;
        });

    // The run happens on a thread pool thread after the delay completes, so wait for it to land.
    async Task Release(TaskCompletionSource tcs, int expectedRunCount)
    {
        tcs.SetResult();
        for (int i = 0; i < 200 && runCount < expectedRunCount; i++) await Task.Delay(10);
    }

    [Fact]
    public async Task Request_ManyTimesBeforeTheDelayEnds_RunsOnce()
    {
        var throttled = Create();

        for (int i = 0; i < 50; i++) throttled.Request();

        delays.Count.ShouldBe(1);
        runCount.ShouldBe(0);
        await Release(delays[0], 1);
        await Task.Delay(50);
        runCount.ShouldBe(1);
        delays.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Request_AfterARun_RunsAgainSoTheLastChangeIsShown()
    {
        var throttled = Create();

        throttled.Request();
        await Release(delays[0], 1);
        throttled.Request();
        delays.Count.ShouldBe(2);
        await Release(delays[1], 2);

        runCount.ShouldBe(2);
    }

    [Fact]
    public async Task Request_WhileTheActionRuns_SchedulesAnotherRun()
    {
        ThrottledAction throttled = null;
        throttled = Create(() =>
        {
            if (runCount == 0) throttled.Request();
        });

        throttled.Request();
        await Release(delays[0], 1);

        delays.Count.ShouldBe(2);
        await Release(delays[1], 2);
        runCount.ShouldBe(2);
    }
}
