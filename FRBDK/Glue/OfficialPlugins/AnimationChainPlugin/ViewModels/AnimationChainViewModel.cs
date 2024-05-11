using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal class AnimationChainViewModel : ViewModel
    {
        [DependsOn(nameof(Name))]
        public string Text => Name;

        public FilePath FilePath { get; set; }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Duration
        {
            get => Get<float>();
            private set => Set(value);
        }

        public AnimationChainSave BackingModel { get; private set; }


        public ObservableCollection<AnimationFrameViewModel> VisibleChildren { get; set; } = 
            new ObservableCollection<AnimationFrameViewModel>();

        public override string ToString() => Name;

        public void SetFrom(AnimationChainSave animationChain, FilePath achxFilePath, int resolutionWidth, int resolutionHeight)
        {
            FilePath = achxFilePath;
            BackingModel = animationChain;
            Name = animationChain.Name;

            foreach(var frame in animationChain.Frames)
            {
                AddAnimationFrame(frame);
            }
        }

        public AnimationFrameViewModel AddAnimationFrame(AnimationFrameSave frame)
        {
            var frameVm = new AnimationFrameViewModel();
            frameVm.SetFrom(this, frame);
            frameVm.PropertyChanged += HandleFrameViewModelPropertyChanged;
            VisibleChildren.Add(frameVm);

            Duration = VisibleChildren.Sum(item => item.LengthInSeconds);
            return frameVm;
        }

        public Action<AnimationFrameViewModel, string> FrameUpdatedByUi;

        private void HandleFrameViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vm = (AnimationFrameViewModel)sender;
            var frame = vm.BackingModel;

            var changed = vm.ApplyToFrame(frame);

            if(changed)
            {
                FrameUpdatedByUi?.Invoke(vm, e.PropertyName);
            }
        }
    }
}
