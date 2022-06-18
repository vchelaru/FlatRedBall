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
        public static Point? LastMiddleMouseButtonPoint { get; private set; }
        static TextureCoordinateSelectionView View;

        static Camera Camera => View.Canvas.SystemManagers.Renderer.Camera;

        public static void Initialize(TextureCoordinateSelectionView view)
        {
            View = view;

            //Camera.X = -20;
            //Camera.Y = -20;
            View.Background.X = Camera.X;
            View.Background.Y = Camera.Y;

            View.Canvas.InvalidateVisual();
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

                camera.X -= (float)(newPosition.X - LastMiddleMouseButtonPoint.Value.X);
                camera.Y -= (float)(newPosition.Y - LastMiddleMouseButtonPoint.Value.Y);
                View.Background.X = Camera.X;
                View.Background.Y = Camera.Y;
                View.Canvas.InvalidateVisual();

                LastMiddleMouseButtonPoint = newPosition;
            }
        }
    }
}
