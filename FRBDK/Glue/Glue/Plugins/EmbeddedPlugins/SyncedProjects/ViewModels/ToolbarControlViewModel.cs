using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.SyncedProjects.ViewModels
{
    class ToolbarControlViewModel : ViewModel
    {
        public bool IsOpenVisualStudioAutomaticallyChecked
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
