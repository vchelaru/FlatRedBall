using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    public class SettingsViewModel : ViewModel
    {
        public bool ShowGuides
        {
            get => Get<bool>();
            set => Set(value);
        }

        public Color BackgroundColor
        {
            get => Get<Color>();
            set => Set(value);
        }
    }
}
