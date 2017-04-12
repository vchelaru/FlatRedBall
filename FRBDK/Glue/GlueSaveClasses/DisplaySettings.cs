using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.SaveClasses
{
    public enum ResizeBehavior
    {
        StretchVisibleArea,
        IncreaseVisibleArea
    }
    public class DisplaySettings
    {
        public bool Is2D { get; set; }

        public bool GenerateDisplayCode { get; set; } = true;

        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }

        public bool FixedAspectRatio { get; set; }
        public decimal AspectRatioWidth { get; set; }
        public decimal AspectRatioHeight { get; set; }

        public bool SupportLandscape { get; set; }
        public bool SupportPortrait { get; set; }

        public bool RunInFullScreen { get; set; }
        public bool AllowWindowResizing { get; set; }

        public int Scale { get; set; }

        public ResizeBehavior ResizeBehavior { get; set; }
    }
}
