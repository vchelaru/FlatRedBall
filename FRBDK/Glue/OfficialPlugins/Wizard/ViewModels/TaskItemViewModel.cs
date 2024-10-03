using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace OfficialPlugins.Wizard.ViewModels
{
    public class TaskItemViewModel : ViewModel
    {
        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsComplete
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsInProgress
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsComplete))]
        public Visibility CheckVisibility => IsComplete.ToVisibility();

        [DependsOn(nameof(IsComplete))]
        [DependsOn(nameof(IsInProgress))]
        public Visibility SpinnerVisibility => 
            (!IsComplete && IsInProgress).ToVisibility();

        public double? ProgressPercentage
        {
            get => Get<double?>();
            set => Set(value);
        }

        [DependsOn(nameof(IsInProgress))]
        [DependsOn(nameof(ProgressPercentage))]
        public Visibility ProgressBarVisibility =>
            (IsInProgress && ProgressPercentage != null).ToVisibility();




        public Func<Task> Task { get; set; }
        public Action Action { get; set; }
    }
}
