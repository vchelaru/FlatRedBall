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

        public string CurrentTaskText => TaskManager.Self.CurrentTask;

        public bool LogTaskDetailsToOutput
        {
            get => Get<bool>();
            set => Set(value);
        }

        public TaskDisplayerViewModel()
        {
            TaskManager.Self.TaskAddedOrRemoved += HandleSyncTaskAddedOrRemoved;
        }

        private void HandleSyncTaskAddedOrRemoved(AddedOrRemoved addedOrRemoved, GlueTask glueTask)
        {
            this.NotifyPropertyChanged("StatusText");
            this.NotifyPropertyChanged("CurrentTaskText");

            if(LogTaskDetailsToOutput)
            {
                PluginManager.ReceiveOutput($"{addedOrRemoved} {glueTask.DisplayInfo}");
            }

        }
    }
}
