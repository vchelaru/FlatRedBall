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
        #region Fields/Properties

        public RoundedRectangleRuntime[] Handles { get; private set; } = new RoundedRectangleRuntime[8];

        #endregion

        public TextureCoordinateRectangle() : base() { Initialize(); }



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
            handle.WidthUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;
            handle.Height = handleSize;
            handle.HeightUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;

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

        internal void MakeNormal(RoundedRectangleRuntime handle)
        {
            const int handleSize = 12;
            handle.Width = handleSize;
            handle.Height = handleSize;
            handle.IsFilled = false;
        }

        internal void MakeHighlighted(RoundedRectangleRuntime handle)
        {
            const int handleSize = 16;
            handle.Width = handleSize;
            handle.Height = handleSize;
            handle.IsFilled = true;
        }

        private static RoundedRectangleRuntime CreateLineRectangle()
        {
            var rectangle = new RoundedRectangleRuntime();
            rectangle.Color = SKColors.White;
            rectangle.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;
            rectangle.IsFilled = false;
            rectangle.OutlineThickness = 1;
            return rectangle;
        }
    }
}
