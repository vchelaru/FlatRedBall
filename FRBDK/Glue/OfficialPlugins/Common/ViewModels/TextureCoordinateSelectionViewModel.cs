using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Math;
using PropertyTools.DataAnnotations;
using SkiaGum.Renderables;

namespace OfficialPlugins.Common.ViewModels;

public class TextureCoordinateSelectionViewModel : ViewModel, ICameraZoomViewModel
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

    public float CurrentZoomPercent
    {
        get => Get<float>();
        set => Set(value);
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

    public void SetCoordinatesWithout(decimal left, decimal top, decimal width, decimal height)
    {
        SetWithoutNotifying(left, nameof(LeftTexturePixel));
        SetWithoutNotifying(top, nameof(TopTexturePixel));
        SetWithoutNotifying(width, nameof(SelectedWidthPixels));
        SetWithoutNotifying(height, nameof(SelectedHeightPixels));

        NotifyPropertyChanged(nameof(LeftTexturePixel));
        NotifyPropertyChanged(nameof(TopTexturePixel));
        NotifyPropertyChanged(nameof(SelectedWidthPixels));
        NotifyPropertyChanged(nameof(SelectedHeightPixels));
    }

    public void CheckCellTextureDivision()
    {
        double x = TextureWidth % CellWidth;
        double y = TextureHeight % CellHeight;
        SnapWidthColor = x == 0 ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.OrangeRed;
        SnapHeightColor = y == 0 ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.OrangeRed;
        SnapWarningVisibility = y == 0 && x == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    public void SelectCell(double worldX, double worldY, out int columnX, out int columnY)
    {
        columnX = (int)Math.Ceiling(worldX / CellWidth);
        columnY = (int)Math.Ceiling(worldY / CellHeight);

        var numcellsX = TextureWidth / CellWidth;
        var numcellsY = TextureHeight / CellHeight;

        var isOutOfBounds =
            (columnX < 0) || (columnX > numcellsX) ||
            (columnY < 0) || (columnY > numcellsY);

        if(!isOutOfBounds)
        {
            LeftTexturePixel = (columnX - 1) * CellWidth;
            TopTexturePixel = (columnY - 1) * CellHeight;
            SelectedWidthPixels = CellWidth;
            SelectedHeightPixels = CellHeight;
        }
    }

    public void DoDragSelectionLogic(double worldX, double worldY, int startDraggingXCell, int startDraggingYCell )
    {
        int currentXCell1Based = (int)Math.Ceiling(worldX / CellWidth);
        int currentYCell1Based = (int)Math.Ceiling(worldY / CellHeight);

        var numcellsX = TextureWidth / CellWidth;
        var numcellsY = TextureHeight / CellHeight;

        var isStartOutOfBounds =
            (startDraggingXCell < 0) || (startDraggingXCell > numcellsX) ||
            (startDraggingYCell < 0) || (startDraggingYCell > numcellsY);

        var isCurrentOutOfBounds =
            (currentXCell1Based < 0) || (currentXCell1Based > numcellsX) ||
            (currentYCell1Based < 0) || (currentYCell1Based > numcellsY);

        if (!isStartOutOfBounds && !isCurrentOutOfBounds)
        {
            var minCellX = Math.Min(startDraggingXCell, currentXCell1Based)-1;
            var maxCellX = Math.Max(startDraggingXCell, currentXCell1Based)-1;
            var minCellY = Math.Min(startDraggingYCell, currentYCell1Based)-1;
            var maxCellY = Math.Max(startDraggingYCell, currentYCell1Based)-1;

            LeftTexturePixel = (decimal)minCellX * CellWidth;
            TopTexturePixel = (decimal)minCellY * CellHeight;

            SelectedWidthPixels = (maxCellX - minCellX + 1) * CellWidth;
            SelectedHeightPixels = (maxCellY - minCellY + 1) * CellHeight;
        }
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
