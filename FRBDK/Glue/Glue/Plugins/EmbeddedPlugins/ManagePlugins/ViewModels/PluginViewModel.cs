using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins.ViewModels
{
    public class PluginViewModel : ViewModel
    {
        public PluginContainer BackingData
        {
            get;
            set;
        }

        public bool LoadOnStartup
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string LastUpdatedText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string GithubRepoAddress
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool RequiredByProject
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
