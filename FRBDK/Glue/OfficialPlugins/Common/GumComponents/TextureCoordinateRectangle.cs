using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Gum.Converters;
using Gum.Wireframe;
using OfficialPlugins.SpritePlugin.Managers;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment;
using VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment;

namespace OfficialPlugins.Common.GumComponents;

public class TextureCoordinateRectangle : ContainerRuntime
{
    #region Fields/Properties

    public RoundedRectangleRuntime[] Handles { get; private set; } = new RoundedRectangleRuntime[8];

    #endregion

    public TextureCoordinateRectangle() : base() { Initialize(); }

    public RoundedRectangleRuntime TopLeftHandle => Handles.First(item => item.Name == "TopLeft");
    public RoundedRectangleRuntime TopCenterHandle => Handles.First(item => item.Name == "TopCenter");
    public RoundedRectangleRuntime TopRightHandle => Handles.First(item => item.Name == "TopRight");
    public RoundedRectangleRuntime RightHandle => Handles.First(item => item.Name == "Right");
    public RoundedRectangleRuntime BottomRightHandle => Handles.First(item => item.Name == "BottomRight");
    public RoundedRectangleRuntime BottomCenterHandle => Handles.First(item => item.Name == "BottomCenter");
    public RoundedRectangleRuntime BottomLeftHandle => Handles.First(item => item.Name == "BottomLeft");
    public RoundedRectangleRuntime LeftHandle => Handles.First(item => item.Name == "Left");


    private void Initialize()
    {
        RoundedRectangleRuntime mainRectangle = CreateLineRectangle();
        Children.Add(mainRectangle);

        mainRectangle.Width = 100;
        mainRectangle.Height = 100;
        mainRectangle.CornerRadius = 0;
        mainRectangle.StrokeWidth = 1;
        mainRectangle.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        mainRectangle.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;

        // Note - the names here tell you which handle we're dealing with. Keep in mind
        // that the names are correct, and the alignments may seem like they are the opposite.
        // For example, handles on the left side are going to have a HorizontalAlignment of Right
        // because their right side touches the outer edge of the rectangle.
        CreateHandle(
            GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromSmall,
            HorizontalAlignment.Right, VerticalAlignment.Bottom, "TopLeft");

        CreateHandle(
            GeneralUnitType.PixelsFromMiddle, GeneralUnitType.PixelsFromSmall,
            HorizontalAlignment.Center, VerticalAlignment.Bottom, "TopCenter");

        CreateHandle(GeneralUnitType.PixelsFromLarge, GeneralUnitType.PixelsFromSmall,
            HorizontalAlignment.Left, VerticalAlignment.Bottom, "TopRight");

        CreateHandle(GeneralUnitType.PixelsFromLarge, GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment.Left, VerticalAlignment.Center, "Right");

        CreateHandle(GeneralUnitType.PixelsFromLarge, GeneralUnitType.PixelsFromLarge,
            HorizontalAlignment.Left, VerticalAlignment.Top, "BottomRight");

        CreateHandle(GeneralUnitType.PixelsFromMiddle, GeneralUnitType.PixelsFromLarge,
            HorizontalAlignment.Center, VerticalAlignment.Top, "BottomCenter");

        CreateHandle(GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromLarge,
            HorizontalAlignment.Right, VerticalAlignment.Top, "BottomLeft");

        CreateHandle(GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromMiddle,
            HorizontalAlignment.Right, VerticalAlignment.Center, "Left");
    }

    int nextHandleIndex = 0;
    private RoundedRectangleRuntime CreateHandle(GeneralUnitType xUnits, GeneralUnitType yUnits, 
        HorizontalAlignment xOrigin, VerticalAlignment yOrigin, string name)
    {
        var handle = CreateLineRectangle();
        handle.Name = name;
        const int handleSize = 12;
        handle.Width = handleSize;
        handle.WidthUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;
        handle.Height = handleSize;
        handle.HeightUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;

        handle.StrokeWidth = 1;
        handle.CornerRadius = 2;
        handle.CornerRadiusUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;

        handle.XUnits = xUnits;
        handle.YUnits = yUnits;
        handle.XOrigin = xOrigin;
        handle.YOrigin = yOrigin;
        Children.Add(handle);

        Handles[nextHandleIndex] = handle;
        nextHandleIndex++;

        return handle;
    }

    internal void MakeNormal(RoundedRectangleRuntime handle)
    {
        const int handleSize = 12;
        handle.Width = handleSize;
        handle.Height = handleSize;
        handle.IsFilled = false;
    }

    bool hasMadeRight = false;
    internal void MakeHighlighted(RoundedRectangleRuntime handle)
    {
        const int handleSize = 14;
        // need to figure out why we are getting highlights
        // not working when crossing over on the X. To do this, output
        // which handle is being highlighted:


        if(!handle.IsFilled)
        {
            if(handle.Name == "Left" && hasMadeRight)
            {
                int m = 3;
            }
            System.Diagnostics.Debug.WriteLine($"Making {handle} highlighted");
            handle.Width = handleSize;
            handle.Height = handleSize;
            handle.IsFilled = true;
        }

        if(handle.Name == "Right")
        {
            hasMadeRight = true;
        }
    }

    private static RoundedRectangleRuntime CreateLineRectangle()
    {
        var rectangle = new RoundedRectangleRuntime();
        rectangle.Color = SKColors.White;
        rectangle.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;
        rectangle.IsFilled = false;
        rectangle.StrokeWidth = 1;
        return rectangle;
    }

    internal RoundedRectangleRuntime GetHandleAt(double x, double y)
    {
        foreach (var handle in Handles)
        {
            var left = handle.AbsoluteLeft;
            var right = handle.AbsoluteRight;
            var top = handle.AbsoluteTop;
            var bottom = handle.AbsoluteBottom;

            if (x >= left && x <= right &&
                y >= top && y <= bottom)
            {
                return handle;
            }
        }
        return null;
    }
}
