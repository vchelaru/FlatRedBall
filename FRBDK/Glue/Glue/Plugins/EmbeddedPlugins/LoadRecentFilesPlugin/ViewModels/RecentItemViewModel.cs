using FlatRedBall.Glue.MVVM;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.ViewModels
{
    internal class RecentItemViewModel : ViewModel
    {
        public string FullPath
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(FullPath))]
        public string StrippedName => !string.IsNullOrEmpty(FullPath)
            ? FileManager.RemovePath(FullPath)
            : "";
    }
}
