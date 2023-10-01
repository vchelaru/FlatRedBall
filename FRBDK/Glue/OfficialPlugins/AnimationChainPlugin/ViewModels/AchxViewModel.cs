using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Common.ViewModels;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal class AchxViewModel : ViewModel, ICameraZoomViewModel
    {
        [DependsOn(nameof(CurrentZoomPercent))]
        public float CurrentZoomScale => CurrentZoomPercent / 100.0f;

        public float CurrentZoomPercent
        {
            get => Get<float>();
            set
            {
                Set(value);
            }
        }

        public float CurrentAnimationZoomPercent
        {
            get => Get<float>();
            set
            {
                Set(value);
            }
        }

        public List<int> ZoomPercentages { get; set; } =
            new List<int> { 4000, 2000, 1500, 1000, 750, 500, 350, 200, 100, 75, 50, 25, 10, 5 };

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
            CurrentZoomPercent = 100;
        }
    }
}
