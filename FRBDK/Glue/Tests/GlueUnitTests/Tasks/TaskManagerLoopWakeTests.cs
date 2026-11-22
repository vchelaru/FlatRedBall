using System;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using Shouldly;

namespace GlueUnitTests.Tasks;

// Runs a real TaskManager with its own task thread (not SynchronousMode, which runs tasks inline and so
// never reaches the task loop). An idle loop used to poll the queue every 50 ms, so a caller that queues a
// task, awaits it and then queues the next waited up to 50 ms per task with the queue empty. A project load
// did that hundreds of times (#2422).
[Collection(nameof(TaskManagerSequentialCollection))]
public class TaskManagerLoopWakeTests
{
    [Fact]
    public async Task Add_ShouldRunATaskQueuedOnAnIdleLoop_WithoutWaitingForTheIdleDelay()
    {
        // With a 30 s idle delay, a loop that only polls can't run any of these within the 5 s allowed for
        // each, so the result doesn't depend on how loaded the machine is.
        const int taskCount = 20;
        var originalSynchronousMode = TaskManager.SynchronousMode;
        TaskManager.SynchronousMode = false;
        var taskManager = new TaskManager(idleLoopDelayMs: 30_000);
        try
        {
            // Let the loop find the queue empty and go idle.
            await Task.Delay(200);

            for (int i = 0; i < taskCount; i++)
            {
                var ran = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                taskManager.Add(() => ran.SetResult(), "serialized task " + i);
                (await Task.WhenAny(ran.Task, Task.Delay(5_000))).ShouldBe(ran.Task, $"task {i} did not run");
            }
        }
        finally
        {
            taskManager.StopLoop();
            TaskManager.SynchronousMode = originalSynchronousMode;
        }
    }
}
