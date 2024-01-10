using OfficialPlugins.Common.ViewModels;
using OfficialPlugins.SpritePlugin.ViewModels;
using OfficialPlugins.SpritePlugin.Views;
using RenderingLibrary;
using SkiaGum.GueDeriving;
using SkiaGum.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace OfficialPlugins.SpritePlugin.Managers
{
    public class CameraLogic
    {
        #region Fields/Properties

        public Point? LastMiddleMouseButtonPoint { get; private set; }
        System.Windows.Controls.UserControl View;

        Camera Camera => Canvas.SystemManagers.Renderer.Camera;

        ICameraZoomViewModel ViewModel => View.DataContext as ICameraZoomViewModel;

        GumSKElement Canvas;

        BindableGraphicalUiElement Background;

        static double? windowsScaleFactor = null;
        public static double WindowsScaleFactor
        {
            get
            {
                if (windowsScaleFactor == null)
                {
                    // todo - fix on a computer that has scaling using:
                    // https://stackoverflow.com/questions/68832226/get-windows-10-text-scaling-value-in-wpf/68846399#comment128365225_68846399

                    // This doesn't seem to work on Windows11:
                    //var userKey = Microsoft.Win32.Registry.CurrentUser;
                    //var softKey = userKey.OpenSubKey("Software");
                    //var micKey = softKey.OpenSubKey("Microsoft");
                    //var accKey = micKey.OpenSubKey("Accessibility");

                    //var factor = accKey.GetValue("TextScaleFactor");
                    // this returns text scale, not window scale
                    //var uiSettings = new Windows.UI.ViewManagement.UISettings();
                    //windowsScaleFactor = uiSettings.
                    windowsScaleFactor =
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth;
                }
                return windowsScaleFactor.Value;
            }
        }

        #endregion

        public void Initialize(System.Windows.Controls.UserControl view, GumSKElement canvas, BindableGraphicalUiElement background)
        {
            Canvas = canvas;
            View = view;
            Background = background;
            Camera.X = -20;
            Camera.Y = -20;
            UpdateBackgroundPosition();

            Canvas.InvalidateVisual();
        }

        private void UpdateBackgroundPosition()
        {
            Background.X = Camera.X;
            Background.Y = Camera.Y;
        }

        public void HandleMousePush(MouseButtonEventArgs args)
        {
            if(args.MiddleButton == MouseButtonState.Pressed)
            {
                LastMiddleMouseButtonPoint = args.GetPosition(View);
            }
        }

        public void HandleMouseMove(MouseEventArgs args)
        {
            var newPosition = args.GetPosition(View);

            if(args.MiddleButton == MouseButtonState.Pressed
               && LastMiddleMouseButtonPoint != null
               && newPosition != LastMiddleMouseButtonPoint)
            {
                var camera = Camera;

                camera.X -= (float)(newPosition.X - LastMiddleMouseButtonPoint.Value.X) / ViewModel.CurrentZoomScale;
                camera.Y -= (float)(newPosition.Y - LastMiddleMouseButtonPoint.Value.Y) / ViewModel.CurrentZoomScale;
                UpdateBackgroundPosition();

                Canvas.InvalidateVisual();

                LastMiddleMouseButtonPoint = newPosition;
            }
        }

        public void HandleMouseWheel(MouseWheelEventArgs args)
        {
            if(args.Delta != 0)
            {
                var zoomDirection = args.Delta;
                var screenPosition = args.GetPosition(Canvas);

                HandleZoomInDirection(zoomDirection, screenPosition);
            }
        }

        private void HandleZoomInDirection(int zoomDirection, System.Windows.Point cursorPosition)
        {

            GetWorldPosition(cursorPosition, out double worldBeforeX, out double worldBeforeY);
            var newValue = ViewModel.CurrentZoomPercent;
            if (zoomDirection > 0)
            {
                var zooms = ViewModel.ZoomPercentages.Where(x => x > newValue);
                if (zooms.Count() == 0) return;
                newValue = zooms.Last();
            }
            else if (zoomDirection < 0)
            {
                var zooms = ViewModel.ZoomPercentages.Where(x => x < newValue);
                if (zooms.Count() == 0) return;
                newValue = zooms.First();
            }

            ViewModel.CurrentZoomPercent = newValue;

            Camera.X = (float)(worldBeforeX - cursorPosition.X / ViewModel.CurrentZoomScale);
            Camera.Y = (float)(worldBeforeY - cursorPosition.Y / ViewModel.CurrentZoomScale);

            RefreshCameraZoomToViewModel();
        }

        public void ResetCamera() {
            Camera.X = -20;
            Camera.Y = -20;
            UpdateBackgroundPosition();
            RefreshCameraZoomToViewModel();
        }

        public void RefreshCameraZoomToViewModel()
        {
            Camera.Zoom = ViewModel.CurrentZoomScale;
            UpdateBackgroundPosition();
            Canvas.InvalidateVisual();
        }

        internal void HandleKey(KeyEventArgs e)
        {
            var moveAmount = 16 / ViewModel.CurrentZoomScale;
            var refresh = false;
            if (e.Key == Key.Left)
            {
                Camera.X -= moveAmount;
                refresh = true;
            }
            else if(e.Key == Key.Right)
            {
                Camera.X += moveAmount;
                refresh = true;
            }
            else if(e.Key == Key.Up)
            {
                Camera.Y -= moveAmount;
                refresh = true;
            }
            else if(e.Key == Key.Down)
            {
                Camera.Y += moveAmount;
                refresh = true;
            }

            if (e.Key == Key.OemPlus && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // zoom in
                HandleZoomInDirection(1, new Point());

            }
            else if(e.Key == Key.OemPlus && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // zoom out
                HandleZoomInDirection(-1, new Point());

            }

            e.Handled = refresh;
            if (refresh)
            {
                UpdateBackgroundPosition();
                Canvas.InvalidateVisual();
            }
        }

        public void GetWorldPosition(Point lastMousePoint, out double x, out double y)
        {
            var camera = Canvas.SystemManagers.Renderer.Camera;

            x = lastMousePoint.X * WindowsScaleFactor;
            y = lastMousePoint.Y * WindowsScaleFactor;
            x /= camera.Zoom;
            y /= camera.Zoom;
            // vic says - did I get the zoom right here?
            x += camera.X;
            y += camera.Y;
        }
    }
}
