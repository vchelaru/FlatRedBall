using FlatRedBall.Glue.MVVM;
using OfficialPlugins.Common.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    public class ZoomViewModel : ViewModel, ICameraZoomViewModel
    {
        [DependsOn(nameof(CurrentZoomPercent))]
        public float CurrentZoomScale => CurrentZoomPercent / 100.0f;

        /// <summary>
        /// Event raised after the zoom percent changes, allowing "late" logic such as refreshing views
        /// in response to Gum cameras.
        /// </summary>
        public event EventHandler? AfterZoomPercentSet;

        public float CurrentZoomPercent
        {
            get => Get<float>();
            set
            {
                if(Set(value))
                {
                    AfterZoomPercentSet?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [DependsOn(nameof(CurrentZoomPercent))]
        public string CurrentZoomDisplay => CurrentZoomPercent + "%";

        public List<int> ZoomPercentages { get; set; } =
            new List<int> { 4000, 2000, 1500, 1000, 750, 500, 350, 200, 100, 75, 50, 25, 10, 5 };



    }
}
