using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.IO;
using OfficialPlugins.Common.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal class AchxViewModel : ViewModel
    {
        public ZoomViewModel TopWindowZoom { get; set; }
        public ZoomViewModel BottomWindowZoom { get; set; }

        public FilePath AchxFilePath { get; set; }

        public int ResolutionWidth
        {
            get => Get<int>();
            set => Set(value);
        }

        public int ResolutionHeight
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(ResolutionWidth))]
        [DependsOn(nameof(ResolutionHeight))]
        public string ResolutionDisplay => $"{ResolutionWidth}x{ResolutionHeight}";

        public ViewModel SelectedItem
        {
            get => Get<ViewModel>();
            set => Set(value);
        }

        public ObservableCollection<AnimationChainViewModel> VisibleRoot { get; private set; }
            = new ObservableCollection<AnimationChainViewModel>();

        public bool IsShowGuidesChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedItem))]
        public AnimationFrameViewModel SelectedAnimationFrame => 
            SelectedItem as AnimationFrameViewModel;

        [DependsOn(nameof(SelectedItem))]
        public ShapeViewModel SelectedShape =>
            SelectedItem as ShapeViewModel;

        /// <summary>
        /// The effective current AnimationChain, which could be directly selected or indirectly selected
        /// by having one of its children selected
        /// </summary>
        [DependsOn(nameof(SelectedShape))]
        [DependsOn(nameof(SelectedAnimationFrame))]
        public AnimationFrameViewModel CurrentAnimationFrame
        {
            get
            {
                if(SelectedShape != null)
                {
                    return SelectedShape.Parent;
                }
                else
                {
                    return SelectedAnimationFrame;
                }
            }
        }

        [DependsOn(nameof(SelectedItem))]
        public AnimationChainViewModel CurrentAnimationChain
        {
            get
            {
                var selectedItem = SelectedItem;
                if(selectedItem is AnimationChainViewModel asAnimationChainViewModel)
                {
                    return asAnimationChainViewModel;
                }
                else if(selectedItem is AnimationFrameViewModel asAnimationFrameViewModel)
                {
                    return asAnimationFrameViewModel.Parent;
                }
                else if(selectedItem is ShapeViewModel asShapeViewModel)
                {
                    return asShapeViewModel.Parent?.Parent;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                SelectedItem = value;
            }
        }

        public AnimationChainListSave BackgingData { get; internal set; }

        public event Action<AnimationFrameViewModel, string> FrameUpdatedByUi;

        // Event raised when an animation chain is updated by the UI,
        // such as a new frame being added or a frame being removed.
        // This is used rather than relying on the VisibleRoot because
        // VisibleRoot updates when animations are loaded. We don't want 
        // to react to properties changing when animations are loaded, only
        // when the user interacts with the UI.
        public event Action<AnimationChainViewModel, string> animationChainUpdatedByUi;

        public AchxViewModel()
        {
            TopWindowZoom = new ZoomViewModel();
            BottomWindowZoom = new ZoomViewModel();

            TopWindowZoom.CurrentZoomPercent = 100;
            BottomWindowZoom.CurrentZoomPercent = 100;

            VisibleRoot.CollectionChanged += HandleAnimationChainViewModelCollectionChanged;
        }

        private void HandleAnimationChainViewModelCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach(AnimationChainViewModel item in e.NewItems)
                {
                    item.FrameUpdatedByUi += (frame, property) => FrameUpdatedByUi?.Invoke(frame, property);
                }
            }
        }

        public void SetFrom(AnimationChainListSave animationChainListSave, FilePath achxFilePath, int resolutionWidth, int resolutionHeight)
        {
            VisibleRoot.Clear();

            if (animationChainListSave == null) return;


            AchxFilePath = achxFilePath;
            ResolutionWidth = resolutionWidth;
            ResolutionHeight = resolutionHeight;


            foreach (var animationChain in animationChainListSave.AnimationChains)
            {
                AddAnimationChain(animationChain);
            }
        }

        public void AddAnimationChain(AnimationChainSave animationChainSave)
        {
            var newViewModel = new AnimationChainViewModel();
            newViewModel.SetFrom(animationChainSave, AchxFilePath, ResolutionWidth, ResolutionHeight);
            newViewModel.FrameUpdatedByUi += (frame, property) => FrameUpdatedByUi?.Invoke(frame, property);
            newViewModel.PropertyChanged += (sender, args) => animationChainUpdatedByUi?.Invoke(newViewModel, args.PropertyName);
            VisibleRoot.Add(newViewModel);
        }
    }
}
