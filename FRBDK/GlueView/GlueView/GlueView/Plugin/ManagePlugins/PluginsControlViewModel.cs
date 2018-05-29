using FlatRedBall.Glue.Plugins;
using GlueView.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView.Plugin.ManagePlugins
{
    class PluginsControlViewModel : ViewModel
    {
        public ObservableCollection<PluginContainer> PluginListItems
        {
            get; private set;
        }

        public PluginContainer SelectedPlugin
        {
            get { return Get<PluginContainer>(); }
            set { Set(value); }
        }

        public string CurrentPluginText
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public PluginsControlViewModel()
        {
            PluginListItems = new ObservableCollection<PluginContainer>();
        }

    }
}
