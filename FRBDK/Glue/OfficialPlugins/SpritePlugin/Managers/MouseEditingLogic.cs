using OfficialPlugins.SpritePlugin.ViewModels;
using OfficialPlugins.SpritePlugin.Views;
using RenderingLibrary;
using SkiaGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace OfficialPlugins.SpritePlugin.Managers
{
    static class MouseEditingLogic
    {
        #region Fields/Properties

        static TextureCoordinateSelectionView View;
        private static System.Windows.Point LastGrabbedMousePoint;
        static ColoredCircleRuntime circle;

        private static SkiaGum.GueDeriving.RoundedRectangleRuntime HandleOver;
        private static SkiaGum.GueDeriving.RoundedRectangleRuntime HandleGrabbed;
        private static bool IsBodyGrabbed;

        static TextureCoordinateSelectionViewModel ViewModel => View.ViewModel;

        #endregion

        public static void Initialize(TextureCoordinateSelectionView view)
        {
            View = view;

            //circle = new ColoredCircleRuntime();
            //circle.Width = 16;
            //circle.Height = 16;
            //circle.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            //circle.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;

            //View.Canvas.Children.Add(circle);
        }

        public static void HandleMousePush(MouseButtonEventArgs args)
        {
            UpdateHandleGrabbed(args);
        }

        public static void HandleMouseMove(MouseEventArgs args)
        {
            if(HandleGrabbed == null)
            {
                UpdateHandleOver(args);
            }
            //var point = args.GetPosition(View.Canvas); 
            //View.GetWorldPosition(point, out double x, out double y);
            //circle.X = (float)x;
            //circle.Y = (float)y;
            //View.Canvas.InvalidateVisual();
            //System.Diagnostics.Debug.WriteLine($"Skia:{x}, {y} Window:({point})");

            UpdateGrabbed(args);

            UpdateHandleHighlight();
        }

        internal static void HandleMouseUp(MouseButtonEventArgs e)
        {
            if(HandleGrabbed != null)
            {
                View.TextureCoordinateRectangle.MakeNormal(HandleGrabbed);


                HandleGrabbed = null;

                UpdateHandleOver(e);
                RefreshHandleVisuals();
                View.Canvas.InvalidateVisual();
            }

            // Copy int to decimal values to prevent "flickering" due to half pixels when moving on subsequent grabs:
            View.ViewModel.TopTexturePixel = View.ViewModel.TopTexturePixelInt;
            View.ViewModel.LeftTexturePixel = View.ViewModel.LeftTexturePixelInt;
            View.ViewModel.SelectedWidthPixels = View.ViewModel.SelectedWidthPixelsInt;
            View.ViewModel.SelectedHeightPixels = View.ViewModel.SelectedHeightPixelsInt;
        }

        private static void UpdateHandleHighlight()
        {
            if(HandleOver != null)
            {
                View.TextureCoordinateRectangle.MakeHighlighted(HandleOver);
            }
            if (HandleOver != null)
            {
                View.TextureCoordinateRectangle.MakeHighlighted(HandleOver);
            }

        }

        private static void UpdateGrabbed(MouseEventArgs args)
        {
            var newPosition = args.GetPosition(View.Canvas);

            if (args.LeftButton == MouseButtonState.Pressed && newPosition != LastGrabbedMousePoint)
            {
                var xDifference = (float)(newPosition.X - LastGrabbedMousePoint.X) / ViewModel.CurrentZoomScale;
                var yDifference = (float)(newPosition.Y - LastGrabbedMousePoint.Y) / ViewModel.CurrentZoomScale;

                if (HandleGrabbed != null)
                {
                    var viewModel = View.ViewModel;

                    if (xDifference != 0)
                    {
                        if (HandleGrabbed.XOrigin == RenderingLibrary.Graphics.HorizontalAlignment.Right)
                        {
                            viewModel.LeftTexturePixel += (decimal)xDifference;
                            viewModel.SelectedWidthPixels -= (decimal)xDifference;
                        }
                        else if (HandleGrabbed.XOrigin == RenderingLibrary.Graphics.HorizontalAlignment.Left)
                        {
                            viewModel.SelectedWidthPixels += (decimal)xDifference;
                        }
                    }
                    if (yDifference != 0)
                    {
                        if (HandleGrabbed.YOrigin == RenderingLibrary.Graphics.VerticalAlignment.Bottom)
                        {
                            viewModel.TopTexturePixel += (decimal)yDifference;
                            viewModel.SelectedHeightPixels -= (decimal)yDifference;
                        }
                        else if (HandleGrabbed.YOrigin == RenderingLibrary.Graphics.VerticalAlignment.Top)
                        {
                            viewModel.SelectedHeightPixels += (decimal)yDifference;
                        }
                    }
                }
                else if(IsBodyGrabbed)
                {
                    var viewModel = View.ViewModel;
                    viewModel.LeftTexturePixel += (decimal)xDifference;
                    viewModel.TopTexturePixel += (decimal)yDifference;
                }

                LastGrabbedMousePoint = newPosition;
            }
        }

        private static void UpdateHandleOver(MouseEventArgs args)
        {
            var oldHandleOver = HandleOver;

            var newHandleOver = View.GetHandleAt(args.GetPosition(View.Canvas));

            if(oldHandleOver != newHandleOver)
            {
                HandleOver = newHandleOver;
                RefreshHandleVisuals();

                View.Canvas.InvalidateVisual();
            }

        }

        private static void RefreshHandleVisuals()
        {
            foreach(var handle in View.TextureCoordinateRectangle.Handles)
            {
                if(handle == HandleOver || handle == HandleGrabbed)
                {
                    View.TextureCoordinateRectangle.MakeHighlighted(handle);
                }
                else
                {
                    View.TextureCoordinateRectangle.MakeNormal(handle);
                }
            }
        }

        private static void UpdateHandleGrabbed(MouseButtonEventArgs args)
        {
            if (args.LeftButton == MouseButtonState.Pressed)
            {
                LastGrabbedMousePoint = args.GetPosition(View.Canvas);
                var handleOver = View.GetHandleAt(LastGrabbedMousePoint);

                HandleGrabbed = handleOver;
                RefreshHandleVisuals();

                var textureCoordinateRectangle = View.TextureCoordinateRectangle;
                View.GetWorldPosition(LastGrabbedMousePoint, out double worldX, out double worldY);
                IsBodyGrabbed = HandleGrabbed == null &&
                    worldX >= textureCoordinateRectangle.GetAbsoluteLeft() &&
                    worldX <= textureCoordinateRectangle.GetAbsoluteRight() &&
                    worldY >= textureCoordinateRectangle.GetAbsoluteTop() &&
                    worldY <= textureCoordinateRectangle.GetAbsoluteBottom();

                View.Canvas.InvalidateVisual();

            }
        }


    }
}
