using OfficialPlugins.SpritePlugin.Views;
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

        public static void Initialize(TextureCoordinateSelectionView view)
        {
            View = view;
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
                var managers = View.Canvas.SystemManagers;
                var camera = managers.Renderer.Camera;

                camera.X -= (float)(newPosition.X - LastMiddleMouseButtonPoint.Value.X);
                camera.Y -= (float)(newPosition.Y - LastMiddleMouseButtonPoint.Value.Y);

                View.Canvas.InvalidateVisual();

                LastMiddleMouseButtonPoint = newPosition;
            }
        }
    }
}
