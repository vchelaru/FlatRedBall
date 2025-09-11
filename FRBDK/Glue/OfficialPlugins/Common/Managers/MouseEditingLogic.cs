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
    /// <summary>
    /// The X position that remains static when moving the handles. If the user grabbed the left side
    /// of the selection, then the xAnchor is the right side. If the user grabbed the right side of the
    /// selection, then the anchor is the left side.
    /// </summary>
    decimal xAnchor;
    /// <summary>
    /// The Y position that remains static when moving the handles. If the user grabbed the top side
    /// of the selection, then the yAnchor is the bottom side. If the user grabbed the bottom side of the
    /// selection, then the anchor is the top side.
    /// </summary>
    decimal yAnchor;

    decimal grabbedDifferenceX;
    decimal grabbedDifferenceY;

    private System.Windows.Point LastGrabbedMousePoint;
    //static ColoredCircleRuntime circle;

    private RoundedRectangleRuntime? HandleOver;
    private RoundedRectangleRuntime? HandleGrabbed;
    private bool IsBodyGrabbed;

    private int? StartDragSelectX1Based = null;
    private int? StartDragSelectY1Based = null;
    private Stopwatch LeftClickTimer = new Stopwatch();
    private GumSKElement _canvas;
    private TextureCoordinateSelectionViewModel _viewModel;
    private TextureCoordinateRectangle _textureCoordinateRectangle;
    private CameraLogic CameraLogic;

    #endregion

    public void Initialize(
        GumSKElement canvas,
        TextureCoordinateSelectionViewModel viewModel,
        TextureCoordinateRectangle textureCoordinateRectangle,
        CameraLogic cameraLogic)
    {
        _canvas = canvas;
        _viewModel = viewModel;
        _textureCoordinateRectangle = textureCoordinateRectangle;
        CameraLogic = cameraLogic;
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
        if(HandleGrabbed == null && !IsBodyGrabbed && args.ChangedButton == MouseButton.Left && _viewModel.SnapChecked)
        {
            var canvasPosition = args.GetPosition(_canvas);
            CameraLogic.GetWorldPosition(canvasPosition, out double worldX, out double worldY);

            _viewModel.SelectCell((float)worldX, (float)worldY, out int columnX, out int columnY);
            StartDragSelectX1Based = columnX;
            StartDragSelectY1Based = columnY;
        }
    }

    public void HandleMouseMove(MouseEventArgs args)
    {
        //start drag select / not interacting with TextureCoordinateRectangle
        var isDragging = StartDragSelectX1Based != null && !IsBodyGrabbed && args.LeftButton == MouseButtonState.Pressed;
        if (isDragging)
        {
            var canvasPosition = args.GetPosition(_canvas);
            CameraLogic.GetWorldPosition(canvasPosition,out double worldX, out double worldY);
            _viewModel.DoDragSelectionLogic(worldX, worldY, (int)StartDragSelectX1Based!, (int)StartDragSelectY1Based!);
            _canvas.InvalidateVisual();
        }
        else
        {
            var canvasPosition = args.GetPosition(_canvas);

            if (HandleGrabbed == null)
            {
                UpdateHandleOver(canvasPosition.X, canvasPosition.Y);
            }

            UpdateGrabbed(args);

            UpdateHandleHighlight();
            _canvas.InvalidateVisual();
        }

    }

    internal void HandleMouseUp(MouseButtonEventArgs e)
    {
        StartDragSelectX1Based = null;
        StartDragSelectY1Based = null;


        if (HandleGrabbed != null)
        {
            _textureCoordinateRectangle.MakeNormal(HandleGrabbed);


            HandleGrabbed = null;

            var canvasPosition = e.GetPosition(_canvas);


            UpdateHandleOver(canvasPosition.X, canvasPosition.Y);
            RefreshHandleHighlightedVisuals();
            _canvas.InvalidateVisual();
        }

        // Copy int to decimal values to prevent "flickering" due to half pixels when moving on subsequent grabs:
        _viewModel.TopTexturePixel = _viewModel.TopTexturePixelInt;
        _viewModel.LeftTexturePixel = _viewModel.LeftTexturePixelInt;
        _viewModel.SelectedWidthPixels = _viewModel.SelectedWidthPixelsInt;
        _viewModel.SelectedHeightPixels = _viewModel.SelectedHeightPixelsInt;


        IsBodyGrabbed = false;
        HandleGrabbed = null;
    }

    private void UpdateHandleHighlight()
    {
        if (HandleGrabbed != null)
        {
            _textureCoordinateRectangle.MakeHighlighted(HandleGrabbed);
        }
        else if (HandleOver != null)
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
                    var oldRightSide = viewModel.RightTexturePixelInt;

                    grabbedDifferenceX -= xDifference;

                    viewModel.SelectedWidthPixels = SnappedX(grabbedDifferenceX);
                    viewModel.LeftTexturePixel = xAnchor - SnappedX(grabbedDifferenceX);

                    if(viewModel.SnapChecked)
                    {
                        //Make sure it's on a snap line
                        var off = viewModel.LeftTexturePixel % _viewModel.CellWidth;
                        viewModel.LeftTexturePixel -= off;
                        viewModel.SelectedWidthPixels += off;
                    }

                    // See if we've moved too far and are crossing over the sides.
                    if (viewModel.LeftTexturePixelInt > oldRightSide && xDifference > 0)
                    {
                        var newRightSide = viewModel.RightTexturePixel;
                        // We've flipped, so flip which side we're dragging
                        xSideGrabbed = XSide.Right;
                        viewModel.LeftTexturePixel = oldRightSide;
                        viewModel.SelectedWidthPixels = newRightSide - oldRightSide;

                        if (HandleGrabbed == _textureCoordinateRectangle.TopLeftHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.TopRightHandle;
                        }
                        if (HandleGrabbed == _textureCoordinateRectangle.LeftHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.RightHandle;
                        }
                        if (HandleGrabbed == _textureCoordinateRectangle.BottomLeftHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.BottomRightHandle;
                        }
                        RefreshHandleHighlightedVisuals();
                    }

                }
                else if(xSideGrabbed == XSide.Right)
                {
                    var oldLeftSide = viewModel.LeftTexturePixelInt;

                    grabbedDifferenceX += xDifference;
                    viewModel.SelectedWidthPixels = SnappedX(grabbedDifferenceX);

                    if(viewModel.SnapChecked)
                    {
                        //Make sure it's on a snap line
                        var off = viewModel.SelectedWidthPixels % _viewModel.CellWidth;
                        viewModel.SelectedWidthPixels -= off;
                    }

                    if(viewModel.RightTexturePixelInt < oldLeftSide && xDifference < 0)
                    {
                        var newLeftSide = viewModel.LeftTexturePixel;
                        // We've flipped, so flip which side we're dragging
                        xSideGrabbed = XSide.Left;
                        viewModel.SelectedWidthPixels = newLeftSide - oldLeftSide;
                        viewModel.LeftTexturePixel = newLeftSide;

                        if (HandleGrabbed == _textureCoordinateRectangle.TopRightHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.TopLeftHandle;
                        }
                        if (HandleGrabbed == _textureCoordinateRectangle.RightHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.LeftHandle;
                        }
                        if (HandleGrabbed == _textureCoordinateRectangle.BottomRightHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.BottomLeftHandle;
                        }
                        RefreshHandleHighlightedVisuals();
                    }
                }

            }
            if (yDifference != 0)
            {
                if(ySideGrabbed == YSide.Top)
                {
                    var oldBottomSide = viewModel.BottomTexturePixelInt;

                    grabbedDifferenceY -= yDifference;

                    viewModel.SelectedHeightPixels = SnappedY(grabbedDifferenceY);
                    viewModel.TopTexturePixel = yAnchor - SnappedY(grabbedDifferenceY);

                    if(viewModel.SnapChecked)
                    {
                        //Make sure it's on a snap line
                        var off = viewModel.TopTexturePixel % _viewModel.CellHeight;
                        viewModel.TopTexturePixel -= off;
                        viewModel.SelectedHeightPixels += off;
                    }

                    if(viewModel.TopTexturePixel > oldBottomSide && yDifference > 0)
                    {
                        var newBottomSide = viewModel.BottomTexturePixel;
                        // We've flipped, so flip which side we're dragging
                        ySideGrabbed = YSide.Bottom;
                        viewModel.TopTexturePixel = oldBottomSide;
                        viewModel.SelectedHeightPixels = newBottomSide - oldBottomSide;
                        if (HandleGrabbed == _textureCoordinateRectangle.TopLeftHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.BottomLeftHandle;
                        }
                        if (HandleGrabbed == _textureCoordinateRectangle.TopCenterHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.BottomCenterHandle;
                        }
                        if (HandleGrabbed == _textureCoordinateRectangle.TopRightHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.BottomRightHandle;
                        }
                        RefreshHandleHighlightedVisuals();
                    }
                }
                else if(ySideGrabbed == YSide.Bottom)
                {
                    var oldTopSide = viewModel.TopTexturePixelInt;

                    grabbedDifferenceY += yDifference;
                    viewModel.SelectedHeightPixels = SnappedY(grabbedDifferenceY);

                    if(viewModel.SnapChecked)
                    {
                        //Make sure it's on a snap line
                        var off = viewModel.SelectedHeightPixels % _viewModel.CellHeight;
                        viewModel.SelectedHeightPixels -= off;
                    }

                    if(viewModel.BottomTexturePixelInt < oldTopSide && yDifference < 0)
                    {
                        var newTopSide = viewModel.TopTexturePixel;
                        // We've flipped, so flip which side we're dragging
                        ySideGrabbed = YSide.Top;
                        viewModel.SelectedHeightPixels = newTopSide - oldTopSide;
                        viewModel.TopTexturePixel = newTopSide;
                        if (HandleGrabbed == _textureCoordinateRectangle.BottomLeftHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.TopLeftHandle;
                        }
                        if (HandleGrabbed == _textureCoordinateRectangle.BottomCenterHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.TopCenterHandle;
                        }
                        if (HandleGrabbed == _textureCoordinateRectangle.BottomRightHandle)
                        {
                            HandleGrabbed = _textureCoordinateRectangle.TopRightHandle;
                        }
                        RefreshHandleHighlightedVisuals();
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

            if(_viewModel.SnapChecked)
            {
                //Make sure it's on all the snap lines
                viewModel.LeftTexturePixel -= viewModel.LeftTexturePixel % _viewModel.CellWidth;
                viewModel.SelectedWidthPixels -= viewModel.SelectedWidthPixels % _viewModel.CellWidth;
                viewModel.TopTexturePixel -= viewModel.TopTexturePixel % _viewModel.CellHeight;
                viewModel.SelectedHeightPixels -= viewModel.SelectedHeightPixels % _viewModel.CellHeight;
            }
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
            RefreshHandleHighlightedVisuals();

            _canvas.InvalidateVisual();
        }

    }

    private void RefreshHandleHighlightedVisuals()
    {
        if(HandleGrabbed != null)
        {
            foreach (var handle in _textureCoordinateRectangle.Handles)
            {
                if (handle == HandleGrabbed)
                {
                    _textureCoordinateRectangle.MakeHighlighted(handle);
                }
                else
                {
                    _textureCoordinateRectangle.MakeNormal(handle);
                }
            }
        }
        else
        {
            foreach (var handle in _textureCoordinateRectangle.Handles)
            {
                if (handle == HandleOver)
                {
                    _textureCoordinateRectangle.MakeHighlighted(handle);
                }
                else
                {
                    _textureCoordinateRectangle.MakeNormal(handle);
                }
            }
        }
    }

    private void UpdateHandleGrabbed(MouseButtonEventArgs args)
    {
        if (args.LeftButton == MouseButtonState.Pressed)
        {
            LastGrabbedMousePoint = args.GetPosition(_canvas);
            CameraLogic.GetWorldPosition(LastGrabbedMousePoint, out double worldX, out double worldY);
            var handleOver = _textureCoordinateRectangle.GetHandleAt(
                worldX,
                worldY);

            HandleGrabbed = handleOver;
            RefreshHandleHighlightedVisuals();

            var textureCoordinateRectangle = _textureCoordinateRectangle;
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

            // Handles on the left side of the rectangle have their XOrigin on the right side.
            // We can "cheat" by looking at that value:
            if (HandleGrabbed?.XOrigin == HorizontalAlignment.Right)
            {
                // If the XOrigin is on the right side, that means the user grabbed one of the left-side handles
                xSideGrabbed = XSide.Left;
                xAnchor = _viewModel.LeftTexturePixel + _viewModel.SelectedWidthPixels;
                grabbedDifferenceX = _viewModel.SelectedWidthPixels;
            }
            else if (HandleGrabbed?.XOrigin == HorizontalAlignment.Left)
            {
                // If the XOrigin is on the left side, that means the user grabbed one of the right-side handles
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
