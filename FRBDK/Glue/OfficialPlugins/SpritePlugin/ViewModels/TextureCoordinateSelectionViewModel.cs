using FlatRedBall.Glue.MVVM;
using FlatRedBall.Math;
using SkiaGum.Renderables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace OfficialPlugins.SpritePlugin.ViewModels
{
    public class TextureCoordinateSelectionViewModel : ViewModel
    {

        public decimal LeftTexturePixel
        {
            get => Get<decimal>();
            set => Set(value);
        }

        int Rounded(decimal value) => MathFunctions.RoundToInt((double)value);

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
            new List<int> { 4000, 2000, 1500, 1000, 750, 500, 350, 200, 100, 75, 50, 25, 10, 5 };

        [DependsOn(nameof(CurrentZoomPercent))]
        public float CurrentZoomScale => CurrentZoomPercent / 100.0f;

        public float CurrentZoomPercent {
            get => Get<float>();
            set {
                Set(value);
            }
        }

        public double WindowX { get; set; }
        public double WindowY { get; set; }

        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }

        public double TextureWidth { get; set; }
        public double TextureHeight { get; set; }

        public bool SnapChecked
        {
            get => Get<bool>();
            set
            {
                Set(value);
                isSnapHeightEnabled = value;
                isSnapHeightCheckEnabled = value;
            }
        }
        public bool SnapHeightChecked
        {
            get => Get<bool>();
            set
            {
                Set(value);
                if(!value)
                    CellHeight = CellWidth;
            }
        }
        public bool isSnapHeightEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool isSnapHeightCheckEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public Visibility SnapWarningVisibility { get => Get<Visibility>(); set => Set(value); }
        public System.Windows.Media.Brush SnapWidthColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }
        public System.Windows.Media.Brush SnapHeightColor { get => Get<System.Windows.Media.Brush>(); set => Set(value); }

        public ushort CellWidth
        {
            get => Get<ushort>();
            set {
                Set(value);
                if(!SnapHeightChecked)
                    CellHeight = value;
                else
                    CheckCellTextureDivision();
            }
        }
        public ushort CellHeight
        {
            get => Get<ushort>();
            set {
                Set(value);
                CheckCellTextureDivision();
            }
        }

        public void CheckCellTextureDivision()
        {
            double x = TextureWidth % CellWidth;
            double y = TextureHeight % CellHeight;
            SnapWidthColor = (x == 0) ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.OrangeRed;
            SnapHeightColor = (y == 0) ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.OrangeRed;
            SnapWarningVisibility = (y == 0) && (x == 0) ? Visibility.Collapsed : Visibility.Visible;
        }

        public TextureCoordinateSelectionViewModel()
        {
            CurrentZoomPercent = 100;
            CellWidth = 16;
            CellHeight = 16;
            SnapChecked = true;
            SnapHeightChecked = false;
        }
    }
}
