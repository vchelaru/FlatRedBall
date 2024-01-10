using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.SyncedProjects.ViewModels
{
    class ToolbarControlViewModel : ViewModel
    {
        public bool IsOpenVisualStudioAutomaticallyChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HasProjectLoaded
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(HasProjectLoaded))]
        public Visibility VisualStudioButtonVisibility => HasProjectLoaded.ToVisibility();

        [DependsOn(nameof(HasProjectLoaded))]
        public Visibility FolderButtonVisibility => HasProjectLoaded.ToVisibility();

    }
}
