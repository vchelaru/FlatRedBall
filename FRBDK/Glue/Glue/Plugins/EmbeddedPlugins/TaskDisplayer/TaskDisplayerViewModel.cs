using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.TaskDisplayer
{
    public class TaskDisplayerViewModel : ViewModel
    {
        public string StatusText
        {
            get
            {
                return "Tasks remaining: " + TaskManager.Self.TaskCount;
            }
        }

        public string CurrentTaskText => TaskManager.Self.NextTasksDescription;

        public bool LogTaskDetailsToOutput
        {
            get => Get<bool>();
            set => Set(value);
        }

        public TaskDisplayerViewModel()
        {
            TaskManager.Self.TaskAddedOrRemoved += HandleSyncTaskAddedOrRemoved;
        }

        private void HandleSyncTaskAddedOrRemoved(TaskEvent addedOrRemoved, GlueTaskBase glueTask)
        {
            this.NotifyPropertyChanged(nameof(StatusText));
            this.NotifyPropertyChanged(nameof(CurrentTaskText));

            if(LogTaskDetailsToOutput)
            {
                var text = $"{addedOrRemoved} {glueTask.DisplayInfo}";
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
