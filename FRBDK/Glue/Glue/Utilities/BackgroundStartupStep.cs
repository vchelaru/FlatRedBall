using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Utilities;

/// <summary>
/// A startup step split in two: slow work that touches no UI runs on a background thread as soon as
/// <see cref="Start"/> is called, and the part that applies its result runs on the caller's thread, once,
/// the first time <see cref="EnsureCompleted"/> is called. Lets Glue overlap the slow part with the rest
/// of startup while still finishing it before anything that depends on it.
/// </summary>
public class BackgroundStartupStep<T>
{
    readonly Func<T> produce;
    readonly Action<T> consume;
    Task<T>? pending;

    public BackgroundStartupStep(Func<T> produce, Action<T> consume)
    {
        this.produce = produce;
        this.consume = consume;
    }

    public void Start()
    {
        pending = Task.Run(produce);
    }

    /// <summary>
    /// Waits for the background work and applies its result. Does nothing if the step was never started
    /// or has already completed. An exception from the background work is rethrown here.
    /// </summary>
    public void EnsureCompleted()
    {
        var task = Interlocked.Exchange(ref pending, null);
        if (task == null)
        {
            return;
        }
        consume(task.GetAwaiter().GetResult());
    }
}
