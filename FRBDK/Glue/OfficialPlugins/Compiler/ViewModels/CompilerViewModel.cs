using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.Compiler.ViewModels
{
    public class CompilerViewModel : ViewModel
    {

        public bool AutoBuildContent { get; set; }

        public bool IsRunning
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsRunning))]
        public Visibility WhileRunningViewVisibility => IsRunning ? 
            Visibility.Visible : Visibility.Collapsed;

        [DependsOn(nameof(IsRunning))]
        public Visibility WhileStoppedViewVisibility => IsRunning ?
            Visibility.Collapsed : Visibility.Visible;

        public bool IsPaused
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsPaused))]
        public Visibility PauseButtonVisibility => IsPaused ?
            Visibility.Collapsed : Visibility.Visible;

        [DependsOn(nameof(IsPaused))]
        public Visibility UnpauseButtonVisibility => IsPaused ?
            Visibility.Visible : Visibility.Collapsed;



        public string Configuration { get; set; }

        Visibility compileContentButtonVisibility;
        public Visibility CompileContentButtonVisibility
        {
            get { return compileContentButtonVisibility; }
            set { base.ChangeAndNotify(ref compileContentButtonVisibility, value); }
        }
    }
}
