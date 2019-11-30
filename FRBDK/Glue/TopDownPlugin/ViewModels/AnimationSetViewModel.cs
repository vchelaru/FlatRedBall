using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.Data;

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

        public AnimationSetModel BackingData
        {
            get; set;
        }

        internal void SetValuesOnBackingData()
        {
            BackingData.UpLeftAnimation = this.UpLeftAnimation;
            BackingData.UpAnimation = this.UpAnimation;
            BackingData.UpRightAnimation = this.UpRightAnimation;

            BackingData.LeftAnimation = this.LeftAnimation;
            BackingData.RightAnimation = this.RightAnimation;

            BackingData.DownLeftAnimation = this.DownLeftAnimation;
            BackingData.DownAnimation = this.DownAnimation;
            BackingData.DownRightAnimation = this.DownRightAnimation;
        }
    }
}
