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


        // todo - this should be frames:
        public ObservableCollection<AnimationChainViewModel> VisibleChildren { get; set; } = new ObservableCollection<AnimationChainViewModel>();

        public override string ToString() => Name;

        public void SetFrom(AnimationChainSave animationChain)
        {
            BackingModel = animationChain;
            Name = animationChain.Name;
            LengthInSeconds = animationChain.Frames.Sum(item => item.FrameLength);
        }
    }
}
