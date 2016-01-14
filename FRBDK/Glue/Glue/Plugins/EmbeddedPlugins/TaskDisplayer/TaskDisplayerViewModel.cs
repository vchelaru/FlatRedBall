using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.TaskDisplayer
{
    public class TaskDisplayerViewModel : ViewModel
    {
        string statusText = "Tasks remaining: 0";
        public string StatusText
        {
            get
            {
                return "Tasks remaining: " + TaskManager.Self.TaskCount;
            }
        }

        public string CurrentTaskText
        {
            get
            {
                return TaskManager.Self.CurrentTask;
            }
        }

        public TaskDisplayerViewModel()
        {
            TaskManager.Self.TaskAddedOrRemoved += HandleSyncTaskAddedOrRemovd;
        }

        private void HandleSyncTaskAddedOrRemovd()
        {
            this.NotifyPropertyChanged("StatusText");
            this.NotifyPropertyChanged("CurrentTaskText");
        }
    }
}
