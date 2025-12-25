using System;
using System.Windows;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Tasks;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.TaskDisplayer;

public class TaskDisplayerViewModel : ViewModel
{
    public string StatusText => $"{L.Texts.TasksRemaining} {TaskManager.Self.TaskCount}";

    public string CurrentTaskText => TaskManager.Self.NextTasksDescription;

    public bool LogTaskDetailsToOutput
    {
        get => Get<bool>();
        set => Set(value);
    }

    [DependsOn(nameof(LogTaskDetailsToOutput))]
    public Visibility IsInclude0LengthVisibility => LogTaskDetailsToOutput.ToVisibility();

    public bool Include0LengthTasks
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool LogQueueChanges
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool LogPluginCalls
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool LogFileWatch
    {
        get => Get<bool>();
        set
        {
            if(Set(value))
            {
                FileWatchManager.IsPrintingDiagnosticOutput = value;
            }
        }
    }

    public bool LogGameCommunication
    {
        get => Get<bool>();
        set
        {
            if (Set(value))
            {
                // send this to the game communication plugin:
                PluginManager.CallPluginMethod("Glue Compiler", "SetIsLoggingSentCommands", value);
            }
        }
    }

    public bool LogGameToFrbCommands
    {
        get => Get<bool>();
        set
        {
            if (Set(value))
            {
                // send this to the game communication plugin:
                PluginManager.CallPluginMethod("Glue Compiler", "SetIsLoggingReceivedCommands", value);
            }
        }
    }

    public TaskDisplayerViewModel()
    {
        TaskManager.Self.TaskAddedOrRemoved += HandleSyncTaskAddedOrRemoved;
        PluginManager.PluginMethodCalled += HandlePluginMethodCalled;
    }

    private void HandlePluginMethodCalled(string plugin, TimeSpan time)
    {
        const double MillisecondThreshold = 3;
        if(LogPluginCalls && time.TotalMilliseconds > MillisecondThreshold)
        {

            var text = plugin;
            if (time.Minutes > 0)
            {
                text += $" {time.Minutes}:{time.Seconds}.{time.Milliseconds:000}";
            }
            else
            {
                text += $" {time.Seconds}.{time.Milliseconds:000}";
            }

            PluginManager.ReceiveOutput(text);

        }
    }

    private void HandleSyncTaskAddedOrRemoved(TaskEvent taskEvent, GlueTaskBase glueTask)
    {
        this.NotifyPropertyChanged(nameof(StatusText));
        this.NotifyPropertyChanged(nameof(CurrentTaskText));

        if(LogTaskDetailsToOutput)
        {
            var shouldlog = LogQueueChanges ||
                // We can't log start events if we aren't logging 0-length because
                // we don't yet know the length:
                (taskEvent == TaskEvent.Started && Include0LengthTasks) ||
                (taskEvent == TaskEvent.StartedImmediate && Include0LengthTasks) ||
                taskEvent == TaskEvent.Removed;

            if(shouldlog)
            {
                var taskEventName = taskEvent.ToString();
                if(taskEvent == TaskEvent.StartedImmediate)
                {
                    // indent it a little so we know we're inside a task already
                    taskEventName = "  " + taskEventName;
                }
                string text;

                if(Include0LengthTasks)
                {
                    text = $"{taskEventName} {glueTask.DisplayInfo}";
                }
                else
                {
                    text = $"{glueTask.DisplayInfo}";
                }



                bool passesMinTimeThreshold = true;

                if(taskEvent == TaskEvent.Removed )
                {
                    // time started better not be null here:
                    var time = glueTask.TimeEnded - glueTask.TimeStarted!.Value;

                    if(time.Minutes > 0)
                    {
                        text += $" {time.Minutes}:{time.Seconds}.{time.Milliseconds.ToString("000")}";
                    }
                    else
                    {
                        if(time.TotalMilliseconds >= 1 || Include0LengthTasks)
                        {
                            text += $" {time.Seconds}.{time.Milliseconds.ToString("000")}";
                        }
                        else
                        {
                            passesMinTimeThreshold = false;
                        }
                    }
                }
                if(passesMinTimeThreshold)
                {
                    PluginManager.ReceiveOutput(text);
                }
            }
        }

    }
}
