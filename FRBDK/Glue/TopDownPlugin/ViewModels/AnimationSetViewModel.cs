using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.ViewModels
{
    public class AnimationSetViewModel : ViewModel
    {
        public string AnimationSetName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string UpLeftAnimation
        {
            get => Get<string>();
            set => Set(value);
        }

        public string UpAnimation
        {
            get => Get<string>();
            set => Set(value);
        }

        public string UpRightAnimation
        {
            get => Get<string>();
            set => Set(value);
        }


        public string LeftAnimation
        {
            get => Get<string>();
            set => Set(value);
        }

        public string RightAnimation
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DownLeftAnimation
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DownAnimation
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DownRightAnimation
        {
            get => Get<string>();
            set => Set(value);
        }

    }
}
