using FlatRedBall.Glue.MVVM;
using FlatRedBall.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.SpritePlugin.ViewModels
{
    public class TextureCoordinateSelectionViewModel : ViewModel
    {
        public decimal LeftTexturePixel
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(LeftTexturePixel))]
        public int LeftTexturePixelInt => MathFunctions.RoundToInt(LeftTexturePixel);

        public decimal TopTexturePixel
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(TopTexturePixel))]
        public int TopTexturePixelInt => MathFunctions.RoundToInt(TopTexturePixel);

        public decimal SelectedWidthPixels
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedWidthPixels))]
        public int SelectedWidthPixelsInt => MathFunctions.RoundToInt(SelectedWidthPixels);

        public decimal SelectedHeightPixels 
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedHeightPixels))]
        public int SelectedHeightPixelsInt => MathFunctions.RoundToInt(SelectedHeightPixels);
    }
}
