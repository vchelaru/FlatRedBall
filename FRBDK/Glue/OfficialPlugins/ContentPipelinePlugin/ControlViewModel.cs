using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.ContentPipelinePlugin
{
    public class ControlViewModel : ViewModel
    {
        bool useContentPipelineOnPngs;

        public bool UseContentPipelineOnPngs
        {
            get { return useContentPipelineOnPngs; }
            set { base.ChangeAndNotify(ref useContentPipelineOnPngs, value); }
        }

        private bool isProjectLoaded;
        public bool IsProjectLoaded
        {
            get { return isProjectLoaded; }
            set { base.ChangeAndNotify(ref isProjectLoaded, value); }
        }

        [DependsOn(nameof(IsProjectLoaded))]
        public Visibility ProjectControlVisibility => IsProjectLoaded ? Visibility.Visible : Visibility.Collapsed;

        [DependsOn(nameof(IsProjectLoaded))]
        public Visibility UnloadedProjectControlVisibility => IsProjectLoaded ? Visibility.Collapsed : Visibility.Visible;
    }
}
