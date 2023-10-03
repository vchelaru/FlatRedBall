using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Common.ViewModels;
using System.Collections.Generic;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal class ZoomViewModel : ViewModel, ICameraZoomViewModel
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
    }
}
