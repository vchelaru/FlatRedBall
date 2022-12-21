using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Common.ViewModels
{
    internal interface ICameraZoomViewModel
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
