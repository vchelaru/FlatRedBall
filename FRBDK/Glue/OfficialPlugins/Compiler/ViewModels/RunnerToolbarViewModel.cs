using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OfficialPluginsCore.Compiler.ViewModels
{
    public class RunnerToolbarViewModel : ViewModel
    {
        public string StartupScreenName
        {
            get => Get<string>();
            set => Set(value);
        }

        public ObservableCollection<string> AvailableScreens
        {
            get; set;
        } = new ObservableCollection<string>();


    }
}
