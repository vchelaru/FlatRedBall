using System;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Tasks;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.TaskDisplayer
{
    public class TaskDisplayerViewModel : ViewModel
    {
        public string StatusText => $"{L.Texts.TasksRemaining} {TaskManager.Self.TaskCount}";

        public string CurrentTaskText => TaskManager.Self.NextTasksDescription;

        public bool LogTaskDetailsToOutput
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

        private void HandleSyncTaskAddedOrRemoved(TaskEvent addedOrRemoved, GlueTaskBase glueTask)
        {
            this.NotifyPropertyChanged(nameof(StatusText));
            this.NotifyPropertyChanged(nameof(CurrentTaskText));

            if(LogTaskDetailsToOutput)
            {
                var shouldlog = LogQueueChanges || addedOrRemoved is TaskEvent.Started or TaskEvent.Removed or TaskEvent.StartedImmediate;

                if(shouldlog)
                {
                    var taskEvent = addedOrRemoved.ToString();
                    if(addedOrRemoved == TaskEvent.StartedImmediate)
                    {
                        // indent it a little so we know we're inside a task already
                        taskEvent = "  " + taskEvent;
                    }
                    var text = $"{taskEvent} {glueTask.DisplayInfo}";
                    if(addedOrRemoved == TaskEvent.Removed)
                    {
                        var time = glueTask.TimeEnded - glueTask.TimeStarted;

                        if(time.Minutes > 0)
                        {
                            text += $" {time.Minutes}:{time.Seconds}.{time.Milliseconds.ToString("000")}";
                        }
                        else
                        {
                            text += $" {time.Seconds}.{time.Milliseconds.ToString("000")}";
                        }
                    }
                    PluginManager.ReceiveOutput(text);
                }
            }

        }
    }
}
