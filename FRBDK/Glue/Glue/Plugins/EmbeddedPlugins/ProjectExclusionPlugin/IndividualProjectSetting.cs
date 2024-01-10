using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.MVVM;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ProjectExclusionPlugin
{
    public class IndividualProjectSetting : ViewModel
    {
        bool isEnabled;
        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
            set
            {
                isEnabled = value;

                base.NotifyPropertyChanged("IsEnabled");
            }
        }


        public string ProjectName
        {
            get;
            set;
        }

        public IndividualProjectSetting()
        {
            ProjectName = L.Texts.ReplaceProjectName;

            IsEnabled = true;
        }
    }
}
