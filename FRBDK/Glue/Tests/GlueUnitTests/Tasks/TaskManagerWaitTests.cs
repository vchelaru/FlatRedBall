using System;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using Shouldly;

namespace GlueUnitTests.Tasks;

// Runs a real TaskManager with its own task thread, as TaskManagerLoopWakeTests does. WaitForTaskToFinish
// (and so AddAsync from outside a task) used to poll every 30 ms, 150 ms for a task with a result, so each
// awaited task cost up to that long after it finished (#2422). The recheck delay is set to 30 s here, so a
// waiter that only polls can't finish within the 5 s allowed and the result doesn't depend on machine load.
[Collection(nameof(TaskManagerSequentialCollection))]
public class TaskManagerWaitTests
{
    static async Task WithRealTaskManager(Func<TaskManager, Task> test)
    {
        var originalSynchronousMode = TaskManager.SynchronousMode;
        TaskManager.SynchronousMode = false;
        var taskManager = new TaskManager(idleLoopDelayMs: 30_000) { WaitRecheckDelayMs = 30_000 };
        try
        {
            await test(taskManager);
        }
        finally
        {
            taskManager.StopLoop();
            TaskManager.SynchronousMode = originalSynchronousMode;
        }
    }

    [Fact]
    public Task WaitForTaskToFinish_ShouldReturnWhenTheTaskFinishes_WithoutWaitingForARecheck() =>
        WithRealTaskManager(async taskManager =>
        {
            for (int i = 0; i < 10; i++)
            {
                using var release = new ManualResetEventSlim();
                var task = taskManager.Add(() => release.Wait(5_000), "gated task " + i);
                var wait = taskManager.WaitForTaskToFinish(task);
                release.Set();

                (await Task.WhenAny(wait, Task.Delay(5_000))).ShouldBe(wait, $"wait {i} did not return");
            }
        });

    [Fact]
    public Task WaitForTaskToFinish_ShouldReturnTheResultWhenTheTaskFinishes_WithoutWaitingForARecheck() =>
        WithRealTaskManager(async taskManager =>
        {
            using var release = new ManualResetEventSlim();
            var task = taskManager.Add(() => { release.Wait(5_000); return 42; }, "gated task with a result");
            var wait = taskManager.WaitForTaskToFinish(task);
            release.Set();

            (await Task.WhenAny(wait, Task.Delay(5_000))).ShouldBe(wait);
            (await wait).ShouldBe(42);
        });

    [Fact]
    public Task WaitForTaskToFinish_ShouldReturn_WhenTheTaskIsCancelledBeforeItRuns() =>
        WithRealTaskManager(async taskManager =>
        {
            using var release = new ManualResetEventSlim();
            taskManager.Add(() => release.Wait(5_000), "blocking task");
            var queued = taskManager.Add(() => { }, "queued task");
            var wait = taskManager.WaitForTaskToFinish(queued);

            queued.IsCancelled = true;

            (await Task.WhenAny(wait, Task.Delay(5_000))).ShouldBe(wait);
            release.Set();
        });
}
