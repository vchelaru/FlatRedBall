using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.SaveClasses
{
    #region Enums

    public enum ResizeBehavior
    {
        StretchVisibleArea,
        IncreaseVisibleArea
    }

    public enum WidthOrHeight
    {
        Width,
        Height
    }

    public enum AspectRatioBehavior
    {
        NoAspectRatio,
        FixedAspectRatio,
        RangedAspectRatio
    }

    #endregion

    public class DisplaySettings
    {
        public string Name { get; set; }
        public bool GenerateDisplayCode { get; set; } 

        public bool Is2D { get; set; }


        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }

        [Obsolete("Use AspectRatioBehavior, where AspectRatioehavior == AspectRatioBehavior.FixedAspectRatio if this is true")]
        public bool FixedAspectRatio
        {
            get => AspectRatioBehavior == AspectRatioBehavior.FixedAspectRatio;
            set
            {
                if(value)
                {
                    AspectRatioBehavior = AspectRatioBehavior.FixedAspectRatio;
                }
            }
        }
        public AspectRatioBehavior AspectRatioBehavior { get; set; }

        public decimal AspectRatioWidth { get; set; }
        public decimal AspectRatioHeight { get; set; }

        public decimal AspectRatioWidth2 { get; set; }
        public decimal AspectRatioHeight2 { get; set; }

        public bool SupportLandscape { get; set; }
        public bool SupportPortrait { get; set; }

        public bool RunInFullScreen { get; set; }
        public bool AllowWindowResizing { get; set; }

        public int Scale { get; set; }
        public int ScaleGum { get; set; }
        public ResizeBehavior ResizeBehavior { get; set; }
        public ResizeBehavior ResizeBehaviorGum { get; set; }
        public WidthOrHeight DominantInternalCoordinates { get; set; }

        public int TextureFilter { get; set; }

        public void SetDefaults()
        {
            Name = "Custom";
            GenerateDisplayCode = true;
            Scale = 100;
            ScaleGum = 100;

            DominantInternalCoordinates = WidthOrHeight.Height;
            TextureFilter = (int)Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
        }

        public override string ToString()
        {
            return $"{Name} {ResolutionWidth}x{ResolutionHeight} at {Scale}%";
        }
    }
}
