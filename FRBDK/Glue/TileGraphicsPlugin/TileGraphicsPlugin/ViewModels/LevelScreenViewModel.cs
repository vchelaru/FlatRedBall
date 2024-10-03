using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace TiledPlugin.ViewModels
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

        public ObservableCollection<string> OrphanedScreens
        {
            get; set;
        } = new ObservableCollection<string>();

        public FilePath[] TmxFilePaths => TmxFiles.Select(item => new FilePath(GlueState.Self.ContentDirectory + item)).ToArray();

        public string SelectedTmxFile
        {
            get => Get<string>();
            set => Set(value);
        }

        public FilePath SelectedTmxFilePath => 
            (GlueState.Self.CurrentGlueProject != null && SelectedTmxFile != null) ? new FilePath(GlueState.Self.ContentDirectory + SelectedTmxFile) : null;

    }
}
