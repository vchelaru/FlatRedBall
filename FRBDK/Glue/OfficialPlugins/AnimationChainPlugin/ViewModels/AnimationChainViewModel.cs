using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal class AnimationChainViewModel : ViewModel
    {
        [DependsOn(nameof(Name))]
        public string Text => Name;

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public float LengthInSeconds
        {
            get => Get<float>();
            set => Set(value);
        }

        public AnimationChainSave BackingModel { get; private set; }


        public ObservableCollection<AnimationFrameViewModel> VisibleChildren { get; set; } = 
            new ObservableCollection<AnimationFrameViewModel>();

        public override string ToString() => Name;

        public void SetFrom(AnimationChainSave animationChain, int resolutionWidth, int resolutionHeight)
        {
            BackingModel = animationChain;
            Name = animationChain.Name;
            LengthInSeconds = animationChain.Frames.Sum(item => item.FrameLength);

            foreach(var frame in animationChain.Frames)
            {
                var frameVm = new AnimationFrameViewModel();
                frameVm.SetFrom(this, frame, resolutionWidth, resolutionHeight);
                VisibleChildren.Add(frameVm);
            }
        }
    }
}
