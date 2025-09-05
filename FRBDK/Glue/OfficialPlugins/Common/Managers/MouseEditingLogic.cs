using OfficialPlugins.Common.GumComponents;
using OfficialPlugins.Common.ViewModels;
using OfficialPlugins.SpritePlugin.Managers;
using OfficialPlugins.SpritePlugin.Views;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using SkiaGum.GueDeriving;
using SkiaGum.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;

namespace OfficialPlugins.Common.Managers;

#region Enums

enum XSide
{
    Left,
    Right
}

enum YSide
{
    Top,
    Bottom
}

#endregion

class MouseEditingLogic
{
    #region Fields/Properties

    XSide? xSideGrabbed;
    YSide? ySideGrabbed;
    decimal xAnchor;
    decimal yAnchor;
    decimal grabbedDifferenceX;
    decimal grabbedDifferenceY;

    TextureCoordinateSelectionView View;
    private System.Windows.Point LastGrabbedMousePoint;
    //static ColoredCircleRuntime circle;

    private RoundedRectangleRuntime HandleOver;
    private RoundedRectangleRuntime HandleGrabbed;
    private bool IsBodyGrabbed;

    private int? StartDragSelectX = null;
    private int? StartDragSelectY = null;
    private Stopwatch LeftClickTimer = new Stopwatch();
    private GumSKElement _canvas;
    private TextureCoordinateSelectionViewModel _viewModel;
    private TextureCoordinateRectangle _textureCoordinateRectangle;
    private CameraLogic CameraLogic;

    #endregion

    public void Initialize(
        TextureCoordinateSelectionView view,
        GumSKElement canvas,
        TextureCoordinateSelectionViewModel viewModel,
        TextureCoordinateRectangle textureCoordinateRectangle,
        CameraLogic cameraLogic)
    {
        if(viewModel == null)
        {
            throw new ArgumentException(nameof(_viewModel));
        }
        _canvas = canvas;
        _viewModel = viewModel;
        _textureCoordinateRectangle = textureCoordinateRectangle;
        CameraLogic = cameraLogic;
        View = view;
        LeftClickTimer.Start();

        //circle = new ColoredCircleRuntime();
        //circle.Width = 16;
        //circle.Height = 16;
        //circle.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
        //circle.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;

        //_canvas.Children.Add(circle);
    }

    public void HandleMousePush(MouseButtonEventArgs args)
    {
        UpdateHandleGrabbed(args);

        //double click
        if(IsBodyGrabbed && LeftClickTimer.ElapsedMilliseconds < System.Windows.Forms.SystemInformation.DoubleClickTime) {
            HandleGrabbed = null;
            IsBodyGrabbed = false;
        }
        if(args.ChangedButton == MouseButton.Left)
            LeftClickTimer.Restart();

        //Not interacting with TextureCoordinateRectangle, move TextureCoordinateRectangle to this cell & init start drag select
        if(HandleGrabbed == null && !IsBodyGrabbed && args.ChangedButton == MouseButton.Left) {
            View.SelectCell(args.GetPosition(_canvas), out int columnX, out int columnY);
            StartDragSelectX = columnX;
            StartDragSelectY = columnY;
        }
    }

    public void HandleMouseMove(MouseEventArgs args)
    {
        //start drag select / not interacting with TextureCoordinateRectangle
        if(StartDragSelectX != null && !IsBodyGrabbed && args.LeftButton == MouseButtonState.Pressed) {
            View.SelectDragCell(args.GetPosition(_canvas), (int)StartDragSelectX, (int)StartDragSelectY);
            return;
        }

        var canvasPosition = args.GetPosition(_canvas);

        if (HandleGrabbed == null)
        {
            UpdateHandleOver(canvasPosition.X, canvasPosition.Y);
        }
        //var point = args.GetPosition(_canvas); 
        //View.GetWorldPosition(point, out double x, out double y);
        //circle.X = (float)x;
        //circle.Y = (float)y;
        //_canvas.InvalidateVisual();
        //System.Diagnostics.Debug.WriteLine($"Skia:{x}, {y} Window:({point})");

        UpdateGrabbed(args);

        UpdateHandleHighlight();
    }

    internal void HandleMouseUp(MouseButtonEventArgs e)
    {
        StartDragSelectX = null;
        StartDragSelectY = null;

        if (HandleGrabbed != null)
        {
            _textureCoordinateRectangle.MakeNormal(HandleGrabbed);


            HandleGrabbed = null;

            var canvasPosition = e.GetPosition(_canvas);


            UpdateHandleOver(canvasPosition.X, canvasPosition.Y);
            RefreshHandleVisuals();
            _canvas.InvalidateVisual();
        }

        // Copy int to decimal values to prevent "flickering" due to half pixels when moving on subsequent grabs:
        _viewModel.TopTexturePixel = _viewModel.TopTexturePixelInt;
        _viewModel.LeftTexturePixel = _viewModel.LeftTexturePixelInt;
        _viewModel.SelectedWidthPixels = _viewModel.SelectedWidthPixelsInt;
        _viewModel.SelectedHeightPixels = _viewModel.SelectedHeightPixelsInt;
    }

    private void UpdateHandleHighlight()
    {
        if (HandleOver != null)
        {
            _textureCoordinateRectangle.MakeHighlighted(HandleOver);
        }
        if (HandleOver != null)
        {
            _textureCoordinateRectangle.MakeHighlighted(HandleOver);
        }

    }

    private void UpdateGrabbed(MouseEventArgs args)
    {
        var newPosition = args.GetPosition(_canvas);

        /////////////////////Early Out//////////////////////
        if (args.LeftButton != MouseButtonState.Pressed || newPosition == LastGrabbedMousePoint)
        {
            return;
        }
        ///////////////////End Early Out////////////////////

        var xDifference = (decimal)(
            (newPosition.X - LastGrabbedMousePoint.X) * CameraLogic.WindowsScaleFactor / _viewModel.CurrentZoomScale);
        var yDifference = (decimal)(
            (newPosition.Y - LastGrabbedMousePoint.Y) * CameraLogic.WindowsScaleFactor / _viewModel.CurrentZoomScale);

        decimal SnappedX(decimal value) => MathFunctions.RoundDecimal(value, _viewModel.SnapChecked ? _viewModel.CellWidth : 1);
        decimal SnappedY(decimal value) => MathFunctions.RoundDecimal(value, _viewModel.SnapChecked ? _viewModel.CellHeight : 1);
        if (HandleGrabbed != null)
        {
            var viewModel = _viewModel;

            if (xDifference != 0)
            {
                if(xSideGrabbed == XSide.Left)
                {
                    grabbedDifferenceX -= xDifference;

                    viewModel.SelectedWidthPixels = SnappedX(grabbedDifferenceX);
                    viewModel.LeftTexturePixel = xAnchor - SnappedX(grabbedDifferenceX);

                    if(viewModel.SnapChecked) {
                        //Make sure it's on a snap line
                        var off = viewModel.LeftTexturePixel % _viewModel.CellWidth;
                        viewModel.LeftTexturePixel -= off;
                        viewModel.SelectedWidthPixels += off;
                    }
                }
                else if(xSideGrabbed == XSide.Right)
                {
                    grabbedDifferenceX += xDifference;
                    viewModel.SelectedWidthPixels = SnappedX(grabbedDifferenceX);

                    if(viewModel.SnapChecked) {
                        //Make sure it's on a snap line
                        var off = viewModel.SelectedWidthPixels % _viewModel.CellWidth;
                        viewModel.SelectedWidthPixels -= off;
                    }
                }

            }
            if (yDifference != 0)
            {
                if(ySideGrabbed == YSide.Top)
                {
                    grabbedDifferenceY -= yDifference;
                    viewModel.SelectedHeightPixels = SnappedY(grabbedDifferenceY);
                    viewModel.TopTexturePixel = yAnchor - SnappedY(grabbedDifferenceY);

                    if(viewModel.SnapChecked) {
                        //Make sure it's on a snap line
                        var off = viewModel.TopTexturePixel % _viewModel.CellHeight;
                        viewModel.TopTexturePixel -= off;
                        viewModel.SelectedHeightPixels += off;
                    }
                }
                else if(ySideGrabbed == YSide.Bottom)
                {
                    grabbedDifferenceY += yDifference;
                    viewModel.SelectedHeightPixels = SnappedY(grabbedDifferenceY);

                    if(viewModel.SnapChecked) {
                        //Make sure it's on a snap line
                        var off = viewModel.SelectedHeightPixels % _viewModel.CellHeight;
                        viewModel.SelectedHeightPixels -= off;
                    }
                }
            }
        }
        else if (IsBodyGrabbed)
        {
            var viewModel = _viewModel;
            grabbedDifferenceX += xDifference;
            grabbedDifferenceY += yDifference;

            viewModel.LeftTexturePixel = xAnchor + SnappedX(grabbedDifferenceX);
            viewModel.TopTexturePixel = yAnchor + SnappedY(grabbedDifferenceY);

            //Make sure it's on all the snap lines
            viewModel.LeftTexturePixel -= viewModel.LeftTexturePixel % _viewModel.CellWidth;
            viewModel.SelectedWidthPixels -= viewModel.SelectedWidthPixels % _viewModel.CellWidth;
            viewModel.TopTexturePixel -= viewModel.TopTexturePixel % _viewModel.CellHeight;
            viewModel.SelectedHeightPixels -= viewModel.SelectedHeightPixels % _viewModel.CellHeight;
        }

        LastGrabbedMousePoint = newPosition;
    }

    private void UpdateHandleOver(double mouseX, double mouseY)
    {
        var oldHandleOver = HandleOver;

        double x, y;

        CameraLogic.GetWorldPosition(new System.Windows.Point(mouseX, mouseY), out x, out y);

        var newHandleOver = _textureCoordinateRectangle.GetHandleAt(x, y);

        if (oldHandleOver != newHandleOver)
        {
            HandleOver = newHandleOver;
            RefreshHandleVisuals();

            _canvas.InvalidateVisual();
        }

    }

    private void RefreshHandleVisuals()
    {
        foreach (var handle in _textureCoordinateRectangle.Handles)
        {
            if (handle == HandleOver || handle == HandleGrabbed)
            {
                _textureCoordinateRectangle.MakeHighlighted(handle);
            }
            else
            {
                _textureCoordinateRectangle.MakeNormal(handle);
            }
        }
    }

    private void UpdateHandleGrabbed(MouseButtonEventArgs args)
    {
        if (args.LeftButton == MouseButtonState.Pressed)
        {
            LastGrabbedMousePoint = args.GetPosition(_canvas);
            var handleOver = _textureCoordinateRectangle.GetHandleAt(
                LastGrabbedMousePoint.X,
                LastGrabbedMousePoint.Y);

            HandleGrabbed = handleOver;
            RefreshHandleVisuals();

            var textureCoordinateRectangle = _textureCoordinateRectangle;
            CameraLogic.GetWorldPosition(LastGrabbedMousePoint, out double worldX, out double worldY);
            IsBodyGrabbed = HandleGrabbed == null &&
                worldX >= textureCoordinateRectangle.GetAbsoluteLeft() &&
                worldX <= textureCoordinateRectangle.GetAbsoluteRight() &&
                worldY >= textureCoordinateRectangle.GetAbsoluteTop() &&
                worldY <= textureCoordinateRectangle.GetAbsoluteBottom();

            _canvas.InvalidateVisual();


            if(IsBodyGrabbed)
            {
                xAnchor = _viewModel.LeftTexturePixel;
                yAnchor = _viewModel.TopTexturePixel;
                grabbedDifferenceX = 0;
                grabbedDifferenceY = 0;
            }

            if (HandleGrabbed?.XOrigin == HorizontalAlignment.Right)
            {
                xSideGrabbed = XSide.Left;
                xAnchor = _viewModel.LeftTexturePixel + _viewModel.SelectedWidthPixels;
                grabbedDifferenceX = _viewModel.SelectedWidthPixels;
            }
            else if (HandleGrabbed?.XOrigin == HorizontalAlignment.Left)
            {
                xSideGrabbed = XSide.Right;
                xAnchor = _viewModel.LeftTexturePixel;
                grabbedDifferenceX = _viewModel.SelectedWidthPixels;

            }
            else
            {
                xSideGrabbed = null;
            }

            if (HandleGrabbed?.YOrigin == VerticalAlignment.Bottom)
            {
                ySideGrabbed = YSide.Top;
                yAnchor = _viewModel.TopTexturePixel + _viewModel.SelectedHeightPixels;
                grabbedDifferenceY = _viewModel.SelectedHeightPixels;
            }
            else if (HandleGrabbed?.YOrigin == VerticalAlignment.Top)
            {
                ySideGrabbed = YSide.Bottom;
                yAnchor = _viewModel.TopTexturePixel;
                grabbedDifferenceY = _viewModel.SelectedHeightPixels;
            }
            else
            {
                ySideGrabbed = null;
            }
        }
    }


}
