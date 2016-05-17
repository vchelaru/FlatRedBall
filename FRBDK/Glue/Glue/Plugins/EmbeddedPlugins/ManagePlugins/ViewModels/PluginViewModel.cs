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

        bool loadOnStartup;
        public bool LoadOnStartup
        {
            get
            {
                return loadOnStartup;
            }
            set
            {
                loadOnStartup = value;
                base.NotifyPropertyChanged(nameof(LoadOnStartup));
            }
        }

        bool requiredByProject;
        public bool RequiredByProject
        {
            get
            {
                return requiredByProject;
            }
            set
            {
                requiredByProject = value;
                base.NotifyPropertyChanged(nameof(RequiredByProject));
            }
        }


    }
}
