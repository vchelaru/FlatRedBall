using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.Managers;

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

        public Action<AnimationFrameViewModel, string> FrameUpdatedByUi;

        public AnimationChainViewModel()
        {
            VisibleChildren.CollectionChanged += HandleCollectionChanged;
        }

        private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var shouldSave = false;
            // add any new .achx's
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var newItems = e.NewItems;

                if (newItems != null)
                {
                    foreach (AnimationFrameViewModel newItem in newItems)
                    {
                        // Is this already contained?
                        if (this.BackingModel.Frames.Contains(newItem.BackingModel) == false)
                        {
                            var index = this.VisibleChildren.IndexOf(newItem);

                            this.BackingModel.Frames.Insert(index, newItem.BackingModel);
                        }
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var oldItems = e.OldItems;
                if (oldItems != null)
                {
                    foreach (AnimationFrameViewModel oldItem in oldItems)
                    {
                        this.BackingModel.Frames.Remove(oldItem.BackingModel);
                    }
                }
            }
        }

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

        public bool ApplyTo(AnimationChainSave animationChainSave)
        {
            var toReturn = false;

            if(animationChainSave.Name != this.Name)
            {
                animationChainSave.Name = this.Name;
                toReturn = true;
            }

            return toReturn;
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

        public override string ToString() => Name;
    }
}
