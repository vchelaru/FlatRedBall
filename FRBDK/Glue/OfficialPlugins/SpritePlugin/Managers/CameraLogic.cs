using OfficialPlugins.SpritePlugin.ViewModels;
using OfficialPlugins.SpritePlugin.Views;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace OfficialPlugins.SpritePlugin.Managers
{
    internal static class CameraLogic
    {
        #region Fields/Properties

        public static Point? LastMiddleMouseButtonPoint { get; private set; }
        static TextureCoordinateSelectionView View;

        static Camera Camera => View.Canvas.SystemManagers.Renderer.Camera;

        static TextureCoordinateSelectionViewModel ViewModel => View.ViewModel;

        #endregion

        public static void Initialize(TextureCoordinateSelectionView view)
        {
            View = view;
            Camera.X = -20;
            Camera.Y = -20;
            UpdateBackgroundPosition();

            View.Canvas.InvalidateVisual();
        }

        private static void UpdateBackgroundPosition()
        {
            View.Background.X = Camera.X;
            View.Background.Y = Camera.Y;
        }

        public static void HandleMousePush(MouseButtonEventArgs args)
        {
            if(args.MiddleButton == MouseButtonState.Pressed)
            {
                LastMiddleMouseButtonPoint = args.GetPosition(View);
            }
        }

        public static void HandleMouseMove(MouseEventArgs args)
        {
            var newPosition = args.GetPosition(View);

            if(args.MiddleButton == MouseButtonState.Pressed && newPosition != LastMiddleMouseButtonPoint)
            {
                var camera = Camera;

                camera.X -= (float)(newPosition.X - LastMiddleMouseButtonPoint.Value.X) / ViewModel.CurrentZoomScale;
                camera.Y -= (float)(newPosition.Y - LastMiddleMouseButtonPoint.Value.Y) / ViewModel.CurrentZoomScale;
                UpdateBackgroundPosition();

                View.Canvas.InvalidateVisual();

                LastMiddleMouseButtonPoint = newPosition;
            }
        }

        public static void HandleMouseWheel(MouseWheelEventArgs args)
        {
            if(args.Delta != 0)
            {
                var screenPosition = args.GetPosition(View.Canvas);
                View.GetWorldPosition(screenPosition, out double worldBeforeX, out double worldBeforeY);
                var newValue = ViewModel.CurrentZoomPercent;
                if (args.Delta > 0)
                {
                    var zooms = ViewModel.ZoomPercentages.Where(x => x > newValue);
                    if(zooms.Count() == 0) return;
                    newValue = zooms.Last();
                } else if(args.Delta < 0)
                {
                    var zooms = ViewModel.ZoomPercentages.Where(x => x < newValue);
                    if(zooms.Count() == 0) return;
                    newValue = zooms.First();
                }

                ViewModel.CurrentZoomPercent = newValue;

                Camera.X = (float)(worldBeforeX - screenPosition.X / ViewModel.CurrentZoomScale);
                Camera.Y = (float)(worldBeforeY - screenPosition.Y / ViewModel.CurrentZoomScale);

                RefreshCameraZoomToViewModel();
            }
        }

        public static void ResetCamera() {
            Camera.X = -20;
            Camera.Y = -20;
            UpdateBackgroundPosition();
            RefreshCameraZoomToViewModel();
        }

        public static void RefreshCameraZoomToViewModel()
        {
            Camera.Zoom = ViewModel.CurrentZoomScale;
            UpdateBackgroundPosition();
            View.Canvas.InvalidateVisual();
        }
    }
}
