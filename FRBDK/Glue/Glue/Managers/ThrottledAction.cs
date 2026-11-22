using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Managers;

/// <summary>
/// Runs an action at most once per delay no matter how often it is requested. A request that arrives
/// while a run is already scheduled is folded into that run, and a request made after the action
/// started schedules one more run, so the action always runs once after the last request. Suited to
/// refreshing a display of state that changes often (such as the task count) where only the latest
/// value matters.
/// </summary>
public class ThrottledAction
{
    readonly Action action;
    readonly Func<Task> delay;
    int isScheduled;

    public ThrottledAction(Action action, TimeSpan interval)
        : this(action, () => Task.Delay(interval))
    {
    }

    /// <param name="delay">Awaited between the first request and the run. Tests pass a delay they complete by hand.</param>
    public ThrottledAction(Action action, Func<Task> delay)
    {
        this.action = action;
        this.delay = delay;
    }

    public void Request()
    {
        if (Interlocked.Exchange(ref isScheduled, 1) == 0)
        {
            _ = RunAfterDelay();
        }
    }

    async Task RunAfterDelay()
    {
        await delay().ConfigureAwait(false);
        // Cleared before running so a request made while the action runs schedules another run,
        // and the last state is always shown.
        Interlocked.Exchange(ref isScheduled, 0);
        try
        {
            action();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
        }
    }
}
