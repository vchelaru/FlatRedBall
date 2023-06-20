using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal class AnimationFrameViewModel : ViewModel
    {
        public AnimationFrameSave BackingModel { get; set; }
        public AnimationChainViewModel Parent { get; private set; }
        public float LengthInSeconds
        {
            get => Get<float>();
            set => Set(value);
        }

        [DependsOn(nameof(LengthInSeconds))]
        public string Text => LengthInSeconds.ToString("0.00");

        public void SetFrom(AnimationChainViewModel parent, AnimationFrameSave animationFrame)
        {
            BackingModel = animationFrame;
            Parent = parent;
            LengthInSeconds = animationFrame.FrameLength;
        }
    }
}
