using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using Shouldly;

namespace GlueUnitTests.Tasks;

// Runs a real TaskManager with its own task thread (not SynchronousMode, which runs tasks inline and so
// never reaches the task loop). An idle loop used to poll the queue every 50 ms, so a caller that queues a
// task, awaits it and then queues the next waited up to 50 ms per task with the queue empty. A project load
// does that hundreds of times (#2422).
[Collection(nameof(TaskManagerSequentialCollection))]
public class TaskManagerLoopWakeTests
{
    [Fact]
    public async Task Add_ShouldRunATaskQueuedOnAnIdleLoop_WithoutWaitingForThePoll()
    {
        const int taskCount = 20;
        var originalSynchronousMode = TaskManager.SynchronousMode;
        TaskManager.SynchronousMode = false;
        try
        {
            var taskManager = new TaskManager();
            // Let the loop find the queue empty and go idle.
            await Task.Delay(200);

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < taskCount; i++)
            {
                var ran = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                taskManager.Add(() => ran.SetResult(), "serialized task " + i);
                (await Task.WhenAny(ran.Task, Task.Delay(5000))).ShouldBe(ran.Task);
            }
            stopwatch.Stop();

            // Polling took about 50 ms per task, so 20 tasks about a second.
            stopwatch.ElapsedMilliseconds.ShouldBeLessThan(taskCount * 50 / 4);
        }
        finally
        {
            TaskManager.SynchronousMode = originalSynchronousMode;
        }
    }
}
