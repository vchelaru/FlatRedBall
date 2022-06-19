using FlatRedBall.Glue.MVVM;
using FlatRedBall.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.SpritePlugin.ViewModels
{
    public class TextureCoordinateSelectionViewModel : ViewModel
    {

        int snapping = 1;


        public decimal LeftTexturePixel
        {
            get => Get<decimal>();
            set => Set(value);
        }

        int Rounded(decimal value) => MathFunctions.RoundToInt( MathFunctions.RoundDouble( (double)value, snapping) );

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

        public TextureCoordinateSelectionViewModel()
        {
            CurrentZoomLevelIndex = 8; 
        }
    }
}
