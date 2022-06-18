using Gum.Converters;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.SpritePlugin.GumComponents
{
    public class TextureCoordinateRectangle : ContainerRuntime
    {
        public TextureCoordinateRectangle() : base() { Initialize(); }

        public RoundedRectangleRuntime[] Handles { get; private set; } = new RoundedRectangleRuntime[8];


        private void Initialize()
        {
            RoundedRectangleRuntime mainRectangle = CreateLineRectangle();
            this.Children.Add(mainRectangle);

            mainRectangle.Width = 100;
            mainRectangle.Height = 100;
            mainRectangle.CornerRadius = 0;
            mainRectangle.StrokeWidth = 1;
            mainRectangle.WidthUnits = Gum.DataTypes.DimensionUnitType.Percentage;
            mainRectangle.HeightUnits = Gum.DataTypes.DimensionUnitType.Percentage;

            CreateHandle(
                GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromSmall,
                HorizontalAlignment.Right, VerticalAlignment.Bottom);

            CreateHandle(
                GeneralUnitType.PixelsFromMiddle, GeneralUnitType.PixelsFromSmall,
                HorizontalAlignment.Center, VerticalAlignment.Bottom);

            CreateHandle(GeneralUnitType.PixelsFromLarge, GeneralUnitType.PixelsFromSmall,
                HorizontalAlignment.Left, VerticalAlignment.Bottom);

            CreateHandle(GeneralUnitType.PixelsFromLarge, GeneralUnitType.PixelsFromMiddle,
                HorizontalAlignment.Left, VerticalAlignment.Center);

            CreateHandle(GeneralUnitType.PixelsFromLarge, GeneralUnitType.PixelsFromLarge,
                HorizontalAlignment.Left, VerticalAlignment.Top);

            CreateHandle(GeneralUnitType.PixelsFromMiddle, GeneralUnitType.PixelsFromLarge,
                HorizontalAlignment.Center, VerticalAlignment.Top);

            CreateHandle(GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromLarge,
                HorizontalAlignment.Right, VerticalAlignment.Top);

            CreateHandle(GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromMiddle,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        int nextHandleIndex = 0;
        private RoundedRectangleRuntime CreateHandle(GeneralUnitType xUnits, GeneralUnitType yUnits, 
            HorizontalAlignment xOrigin, VerticalAlignment yOrigin)
        {
            var handle = CreateLineRectangle();
            const int handleSize = 12;
            handle.Width = handleSize;
            handle.Height = handleSize;
            handle.StrokeWidth = 1;
            handle.CornerRadius = 2;
            handle.XUnits = xUnits;
            handle.YUnits = yUnits;
            handle.XOrigin = xOrigin;
            handle.YOrigin = yOrigin;
            this.Children.Add(handle);

            Handles[nextHandleIndex] = handle;
            nextHandleIndex++;

            return handle;
        }

        private static RoundedRectangleRuntime CreateLineRectangle()
        {
            var rectangle = new RoundedRectangleRuntime();
            rectangle.Color = SKColors.White;
            rectangle.IsFilled = false;
            rectangle.OutlineThickness = 1;
            return rectangle;
        }
    }
}
