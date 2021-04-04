using System;
using System.Collections.Generic;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.CameraPlugin.ViewModels
{
    public class ResolutionDropDownViewModel
    {
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }

        public ResolutionDropDownViewModel(int width, int height)
        {
            ResolutionWidth = width;
            ResolutionHeight = height;
        }

        public override string ToString()
        {
            var divided = ResolutionWidth / (decimal)ResolutionHeight;
            string aspectRatio = null;
            if(divided == 16/9m)
            {
                aspectRatio = "(16:9)";
            }
            else if (divided == 8 / 7m)
            {
                aspectRatio = "(8:7)";
            }
            else if(divided == 4/3m)
            {
                aspectRatio = "(4:3)";
            }
            else if (divided == 3 / 2m)
            {
                aspectRatio = "(3:2)";
            }
            return $"{ResolutionWidth}x{ResolutionHeight} {aspectRatio}";
        }
    }
}
