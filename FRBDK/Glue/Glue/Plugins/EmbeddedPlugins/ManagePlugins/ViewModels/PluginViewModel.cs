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

        string lastUpdatedText;
        public string LastUpdatedText
        {
            get
            {
                return lastUpdatedText;
            }
            set
            {
                base.ChangeAndNotify(ref lastUpdatedText, value);
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
                base.ChangeAndNotify(ref requiredByProject, value);
            }
        }


    }
}
