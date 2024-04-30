using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
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

        [DependsOn(nameof(SelectedItem))]
        public AnimationChainViewModel SelectedAnimationChain
        {
            set
            {
                SelectedItem = value;
            }
            get
            {
                if(SelectedItem is AnimationChainViewModel asAnimationChainViewModel)
                {
                    return asAnimationChainViewModel;
                }
                else if(SelectedItem is AnimationFrameViewModel asAnimationFrameViewModel)
                {
                    return asAnimationFrameViewModel.Parent;
                }
                else
                {
                    return null;
                }

            }
        }

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

        public AnimationChainListSave BackgingData { get; internal set; }

        public event Action<AnimationFrameViewModel, string> FrameUpdatedByUi;

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
    }
}
