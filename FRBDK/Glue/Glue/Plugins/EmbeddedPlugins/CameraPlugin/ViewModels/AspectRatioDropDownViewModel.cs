using System;
using System.Collections.Generic;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.CameraPlugin.ViewModels
{
    public class AspectRatioDropDownViewModel
    {
        public decimal AspectWide { get; set; }
        public decimal AspectTall { get; set; }

        public string AlternativeText { get; set; }

        public AspectRatioDropDownViewModel(decimal width, decimal height, string text = null)
        {
            AspectWide = width;
            AspectTall = height;
            AlternativeText = text;
        }

        public override string ToString()
        {
            if(!string.IsNullOrEmpty( AlternativeText ))
            {
                return AlternativeText;
            }
            else
            {
                return $"{AspectWide}:{AspectTall}";
            }
        }
    }
}
