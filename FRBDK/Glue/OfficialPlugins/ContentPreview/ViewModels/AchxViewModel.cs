using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Common.ViewModels;
using OfficialPlugins.ContentPreview.ViewModels.AnimationChains;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ContentPreview.ViewModels
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

        public ObservableCollection<AnimationEditorNodeViewModel> VisibleRoot { get; private set; } = new ObservableCollection<AnimationEditorNodeViewModel>();


        public AchxViewModel()
        {
            CurrentZoomPercent = 100;
        }
    }
}
