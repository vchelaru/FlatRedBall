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

        int Rounded(decimal value) => MathFunctions.RoundToInt( (double)value );

        [DependsOn(nameof(LeftTexturePixel))]
        public int LeftTexturePixelInt => Rounded(LeftTexturePixel);

        public decimal TopTexturePixel
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(TopTexturePixel))]
        public int TopTexturePixelInt => Rounded(TopTexturePixel);

        public decimal SelectedWidthPixels
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedWidthPixels))]
        public int SelectedWidthPixelsInt => Rounded(SelectedWidthPixels);

        public decimal SelectedHeightPixels 
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedHeightPixels))]
        public int SelectedHeightPixelsInt => Rounded(SelectedHeightPixels);



        public List<int> ZoomPercentages { get; set; } =
            new List<int>
            {
                4000,// 0
                2000,// 1
                1500,// 2
                1000,
                750,
                500,
                350,
                200,
                100,
                75,
                50,
                25,
                10,
                5
            };

        [DependsOn(nameof(CurrentZoomLevelIndex))]
        public float CurrentZoomScale =>
            ZoomPercentages[CurrentZoomLevelIndex] / 100.0f;

        public int CurrentZoomLevelIndex 
        {
            get => Get<int>();
            set
            {
                Set(value);
            }
        } 

        public decimal Snapping
        {
            get => Get<decimal>();
            set => Set(value);
        }

        [DependsOn(nameof(LeftTexturePixel))]
        [DependsOn(nameof(TopTexturePixel))]
        [DependsOn(nameof(SelectedWidthPixels))]
        [DependsOn(nameof(SelectedHeightPixels))]
        public string CoordinateDisplay => $"X:{LeftTexturePixel} Y:{TopTexturePixel} Width:{SelectedWidthPixels} Height:{SelectedHeightPixels}";

        public double WindowX { get; set; }
        public double WindowY { get; set; }

        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }

        public TextureCoordinateSelectionViewModel()
        {
            CurrentZoomLevelIndex = 8;
            Snapping = 4;
        }
    }
}
