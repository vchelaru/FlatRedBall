using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;

namespace FlatRedBall.Glue.IO;

/// <summary>
/// External tools that write content files outside the TaskManager queue, such as gumcli building Gum
/// fonts. When such a tool ran as a queued task, every task after it saw its files only once they were
/// fully written. Queued tasks that read files a tool may be writing call <see cref="WaitIfInTask"/> to
/// keep that guarantee. Track only a task that calls no Glue code (just the process), so a queued
/// task blocking on it cannot deadlock.
/// </summary>
public static class ExternalFileWrites
{
    static readonly object lockObject = new object();
    static Task running = Task.CompletedTask;

    public static void Track(Task writing)
    {
        lock (lockObject)
        {
            running = running.IsCompleted ? writing : Task.WhenAll(running, writing);
        }
    }

    /// <summary>
    /// Blocks until tracked writes finish, but only on the task thread. Other threads never waited for
    /// queued tasks either.
    /// </summary>
    public static void WaitIfInTask() => WaitIf(isOnTaskThread: TaskManager.Self.IsInTask());

    internal static void WaitIf(bool isOnTaskThread)
    {
        Task toWaitFor;
        lock (lockObject)
        {
            toWaitFor = running;
        }

        if (toWaitFor.IsCompleted || !isOnTaskThread)
        {
            return;
        }

        try
        {
            toWaitFor.Wait();
        }
        catch
        {
            // Whoever started the write reports its failure; a reader only needs it to be over.
        }
    }
}
