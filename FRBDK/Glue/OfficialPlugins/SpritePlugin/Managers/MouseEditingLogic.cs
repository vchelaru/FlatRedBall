using OfficialPlugins.SpritePlugin.Views;
using SkiaGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace OfficialPlugins.SpritePlugin.Managers
{
    static class MouseEditingLogic
    {
        static TextureCoordinateSelectionView View;
        private static System.Windows.Point LastMousePoint;
        private static SkiaGum.GueDeriving.RoundedRectangleRuntime HandleGrabbed;
        static ColoredCircleRuntime circle;
        public static void Initialize(TextureCoordinateSelectionView view)
        {
            View = view;

            circle = new ColoredCircleRuntime();
            circle.Width = 16;
            circle.Height = 16;
            circle.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            circle.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;

            View.Canvas.Children.Add(circle);
        }

        public static void HandleMousePush(MouseButtonEventArgs args)
        {
            if (args.LeftButton == MouseButtonState.Pressed)
            {
                LastMousePoint = args.GetPosition(View.Canvas);
                var handleOver = View.GetHandleAt(LastMousePoint);

                HandleGrabbed = handleOver;
            }
        }
        public static void HandleMouseMove(MouseEventArgs args)
        {
            var point = args.GetPosition(View.Canvas); 
            View.GetWorldPosition(point, out double x, out double y);
            circle.X = (float)x;
            circle.Y = (float)y;
            View.Canvas.InvalidateVisual();
            System.Diagnostics.Debug.WriteLine($"Skia:{x}, {y} Window:({point})");

            var newPosition = args.GetPosition(View.Canvas);

            if (HandleGrabbed != null && args.LeftButton == MouseButtonState.Pressed && newPosition != LastMousePoint)
            {
                //var camera = Camera;

                var xDifference = (float)(newPosition.X - LastMousePoint.X);
                var yDifference = (float)(newPosition.Y - LastMousePoint.Y);

                View.ViewModel.LeftTexturePixel++;
                //View.Background.X = Camera.X;
                //View.Background.Y = Camera.Y;
                //View.Canvas.InvalidateVisual();

                //LastMiddleMouseButtonPoint = newPosition;
            }
        }
    }
}
