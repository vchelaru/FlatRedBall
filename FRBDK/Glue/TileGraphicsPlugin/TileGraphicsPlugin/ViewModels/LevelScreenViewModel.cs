using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace TiledPluginCore.ViewModels
{
    public class LevelScreenViewModel : PropertyListContainerViewModel
    {
        [SyncedProperty]
        public bool AutoCreateTmxScreens
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        public bool ShowLevelScreensInTreeView
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(AutoCreateTmxScreens))]
        public Visibility ListBoxVisibility => AutoCreateTmxScreens.ToVisibility();

        public ObservableCollection<string> TmxFiles
        {
            get; set;
        } = new ObservableCollection<string>();
    }
}
