using OfficialPlugins.SpritePlugin.Views;
using RenderingLibrary;
using System;
using System.Collections.Generic;
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

        static List<int> ZoomPercentages { get; set; } =
            new List<int>
            {
                4000,
                2000,
                1000,
                500,
                200,
                100,
                50,
                25,
                10,
                5
            };
        public static float CurrentZoomScale =>
            ZoomPercentages[CurrentZoomLevelIndex] / 100.0f;

        static int CurrentZoomLevelIndex = 5;

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

                camera.X -= (float)(newPosition.X - LastMiddleMouseButtonPoint.Value.X) / CurrentZoomScale;
                camera.Y -= (float)(newPosition.Y - LastMiddleMouseButtonPoint.Value.Y) / CurrentZoomScale;
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
                if(args.Delta > 0)
                {
                    CurrentZoomLevelIndex--;
                    CurrentZoomLevelIndex = Math.Max(0, CurrentZoomLevelIndex);
                }
                else if(args.Delta < 0)
                {
                    CurrentZoomLevelIndex++;
                    CurrentZoomLevelIndex = Math.Min(CurrentZoomLevelIndex, ZoomPercentages.Count-1);
                }

                Camera.Zoom = CurrentZoomScale;

                Camera.X = (float)(worldBeforeX - screenPosition.X / CurrentZoomScale);
                Camera.Y = (float)(worldBeforeY - screenPosition.Y / CurrentZoomScale);

                UpdateBackgroundPosition();

                View.Canvas.InvalidateVisual();
            }
        }
    }
}
