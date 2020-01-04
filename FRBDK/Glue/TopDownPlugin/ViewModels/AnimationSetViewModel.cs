using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            set => SetNullIfEmpty(value);
        }

        public string UpAnimation
        {
            get => Get<string>();
            set => SetNullIfEmpty(value);
        }

        public string UpRightAnimation
        {
            get => Get<string>();
            set => SetNullIfEmpty(value);
        }


        public string LeftAnimation
        {
            get => Get<string>();
            set => SetNullIfEmpty(value);
        }

        public string RightAnimation
        {
            get => Get<string>();
            set => SetNullIfEmpty(value);
        }

        public string DownLeftAnimation
        {
            get => Get<string>();
            set => SetNullIfEmpty(value);
        }

        public string DownAnimation
        {
            get => Get<string>();
            set => SetNullIfEmpty(value);
        }

        public string DownRightAnimation
        {
            get => Get<string>();
            set => SetNullIfEmpty(value);
        }

        // If the user hasn't entered any animations, we don't want to 
        // generate an AnimationSet. This is easiest to do if we just check
        // for null in the AnimationSet generation code. If a user deletes values
        // from a text box, it will get saved as "". We'll convert here to make the
        // rest of the code cleaner.
        private bool SetNullIfEmpty(string value, [CallerMemberName] string caller = null)
        {
            value = string.IsNullOrWhiteSpace(value) ? null : value;
            return base.Set(value, caller);
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
