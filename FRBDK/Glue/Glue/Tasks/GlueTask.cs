using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Tasks;

public abstract class GlueTaskBase
{
    public DateTime? TimeStarted { get; set;}
    public DateTime TimeEnded { get; set; }

    public List<TimedLabel> TimedLabels { get; init; } = new ();

    public string DisplayInfo
    {
        get;
        set;
    }

    /// <summary>
    /// Custom ID used to uniquely identify a task. If this is null or empty, the DisplayInfo will be used.
    /// <seealso cref="EffectiveId"/>
    /// </summary>
    public string CustomId
    {
        get; set;
    }

    public string EffectiveId => CustomId ?? DisplayInfo;

    public string TaskType
    {
        get;
        set;
    }

    public TaskExecutionPreference TaskExecutionPreference { get; set; }

    public bool DoOnUiThread { get; set; }

    public override string ToString()
    {
        if (!IsCancelled)
        {
            return DisplayInfo;
        }
        else
        {
            return $"Cancelled {DisplayInfo}";
        }
    }

    public object Result { get; set; }

    bool isCancelled;
    public bool IsCancelled
    {
        get => isCancelled;
        set
        {
            isCancelled = value;
            if (value)
            {
                MarkFinished();
            }
        }
    }

    readonly TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completes once the task has run or been cancelled, so a waiter needn't poll for it.
    /// </summary>
    internal Task Finished => finished.Task;

    internal void MarkFinished() => finished.TrySetResult();

    // Named in a very particular way so that it can be checked
    // in the TaskManager and not have any name collision.
    internal abstract Task Do_Action_Internal();
}

public class GlueTask : GlueTaskBase
{
    public Action Action { get; set; }

    internal override Task Do_Action_Internal()
    {
        if (DoOnUiThread && !TaskManager.Self.IsOnUiThread)
        {
            TaskManager.UiThreadMarshaller.Invoke(Action);
        }
        else
        {
            Action();
        }

        return Task.CompletedTask;
    }
}

public class GlueTask<T> : GlueTaskBase
{
    public Func<T> Func { get; set; }

    internal override Task Do_Action_Internal()
    {
        if (DoOnUiThread && !TaskManager.Self.IsOnUiThread)
        {
            Result =  TaskManager.UiThreadMarshaller.Invoke(() => Result = Func());
        }
        else
        {
            Result = Func();

        }

        return Task.CompletedTask;
    }
}

public class GlueAsyncTask : GlueTaskBase
{
    public Func<Task> Func { get; set; }

    internal override async Task Do_Action_Internal()
    {
        if (DoOnUiThread && !TaskManager.Self.IsOnUiThread)
        {
            await TaskManager.UiThreadMarshaller.Invoke(() => Func());
        }
        else
        {
            await Func();

        }
    }
}

public class GlueAsyncTask<T> : GlueTaskBase
{
    public Func<Task<T>> Func { get; set; }

    internal override async Task Do_Action_Internal()
    {
        if (DoOnUiThread && !TaskManager.Self.IsOnUiThread)
        {
            Result = await TaskManager.UiThreadMarshaller.Invoke(() => Func());
        }
        else
        {
            Result = await Func();

        }
    }
}

public class TimedLabel
{
    public string Label { get; set; }
    public DateTime Time { get; set; }
}
