using System.Collections.Generic;

namespace OfficialPlugins.Common.ViewModels
{
    public interface ICameraZoomViewModel
    {
        float CurrentZoomScale { get; }

        public float CurrentZoomPercent
        {
            get;
            set;
        }

        List<int> ZoomPercentages { get; set; } 
    }
}
