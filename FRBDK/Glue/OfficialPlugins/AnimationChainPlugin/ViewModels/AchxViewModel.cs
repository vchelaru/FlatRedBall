using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Common.ViewModels;
using System.Collections.ObjectModel;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal class AchxViewModel : ViewModel
    {
        public ZoomViewModel WholeZoom { get; set; }
        public ZoomViewModel SingleZoom { get; set; }

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

        [DependsOn(nameof(SelectedItem))]
        public AnimationFrameViewModel SelectedAnimationFrame => 
            SelectedItem as AnimationFrameViewModel;

        [DependsOn(nameof(SelectedItem))]
        public ShapeViewModel SelectedShape =>
            SelectedItem as ShapeViewModel;

        public AchxViewModel()
        {
            WholeZoom = new ZoomViewModel();
            SingleZoom = new ZoomViewModel();

            WholeZoom.CurrentZoomPercent = 100;
            SingleZoom.CurrentZoomPercent = 100;
        }
    }
}
