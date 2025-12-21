using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using OfficialPlugins.SpritePlugin.Managers;
using SkiaGum.GueDeriving;
using SkiaGum.Wpf;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using RenderingLibrary;
using FlatRedBall.Content.AnimationChain;
using System.ComponentModel;
using SkiaSharp.Views.Desktop;

namespace OfficialPlugins.AnimationChainPlugin.Managers;

internal class BottomWindowManager
{
    #region Fields/Properties

    SpriteRuntime MainAnimationSprite;

    PolygonRuntime BottomWindowHorizontalGuide;
    PolygonRuntime BottomWindowVerticalGuide;

    private int _currentDisplayedAnimationFrameIndex = 0;
    private DateTime _lastFrameTime = DateTime.MinValue;
    private float _currentFrameTime = 0;

    List<SkiaShapeRuntime> AnimationShapes = new List<SkiaShapeRuntime>();

    private System.Timers.Timer _animationTimer = new System.Timers.Timer();

    GumSKElement BottomGumCanvas;

    private bool _isFirstTime = false;

    private object _animationLock = new object();

    private readonly CameraLogic _cameraLogic;
    private readonly CachedTextureService _cachedTextureService;
    private readonly AchxViewModel _viewModel;
    FilePath _currentParentDirectory;

    UserControl UserControl;

    ZoomViewModel zoomViewModel;

    SettingsViewModel settingsViewModel;


    SolidRectangleRuntime GumBackground { get; set; }

    #endregion

    public BottomWindowManager(GumSKElement bottomGumCanvas,
        UserControl userControl,
        CameraLogic cameraLogic,
        ZoomViewModel bottomWindowZoom,
        SettingsViewModel settingsViewModel,
        CachedTextureService cachedTextureService,
        AchxViewModel viewModel)
    {
        zoomViewModel = bottomWindowZoom;
        this.settingsViewModel = settingsViewModel;
        this.settingsViewModel.PropertyChanged += HandleSettingsViewModelPropertyChanged;
        UserControl = userControl;
        BottomGumCanvas = bottomGumCanvas;
        _cameraLogic = cameraLogic;
        _cachedTextureService = cachedTextureService;
        _viewModel = viewModel;



        // background first, so it's behind the other sprites
        CreateBackground();
        CreateAnimatedSprite();
        CreateBottomGuideLines();

        _cameraLogic.Initialize(userControl, bottomWindowZoom, BottomGumCanvas, this.GumBackground);
        bottomGumCanvas.SystemManagers.Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

        StartAnimating();

        // Refresh after creating the sprite
        RefreshBackgroundColor();
    }

    private void HandleSettingsViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsViewModel.BackgroundColor):
                RefreshBackgroundColor();
                break;
        }
    }

    private void RefreshBackgroundColor()
    {
        GumBackground.Color = new SKColor(settingsViewModel.BackgroundColor.R, settingsViewModel.BackgroundColor.G, settingsViewModel.BackgroundColor.B);
        BottomGumCanvas.InvalidateSurface();
    }

    public void RefreshAnimationPreview(AchxViewModel ViewModel)
    {
        _currentDisplayedAnimationFrameIndex = -1;
        _lastFrameTime = DateTime.MinValue;
        _currentFrameTime = 0;

        foreach (var shape in AnimationShapes)
        {
            BottomGumCanvas.Children.Remove(shape);
        }
        AnimationShapes.Clear();

        if (ViewModel != null)
        {
            if (ViewModel.SelectedAnimationFrame != null || ViewModel.SelectedShape != null)
            {
                var frame = ViewModel.SelectedAnimationFrame ?? ViewModel.SelectedShape.Parent;

                List<ShapeViewModel> shapes;
                if (ViewModel.SelectedShape != null)
                {
                    shapes = new List<ShapeViewModel>()
                    {
                        ViewModel.SelectedShape
                    };
                }
                else
                {
                    shapes = ViewModel.SelectedAnimationFrame.VisibleChildren.ToList();
                }


                // force render because we're displaying a frame or a shape explicitly
                RenderFrame(frame, shapes, forceRenderShapes:true);
            }
            else if (ViewModel.CurrentAnimationChain != null)
            {
                if (ViewModel.CurrentAnimationChain.VisibleChildren.Count > 0)
                {
                    RunAnimation();
                }
            }
            else //if(ViewModel.CurrentAnimationChain == null)
            {
                MainAnimationSprite.Visible = false;
            }
        }
    }

    public void RefreshBottomGuideVisibility(AchxViewModel viewModel)
    {
        var isShowGuidesChecked = viewModel.Settings.IsShowingGuides;
        BottomWindowVerticalGuide.Visible = isShowGuidesChecked;
        BottomWindowHorizontalGuide.Visible = isShowGuidesChecked;
        BottomGumCanvas.InvalidateVisual();
    }

    private void RenderShapes(List<ShapeViewModel> shapes, AnimationFrameViewModel? owner, bool forceRenderShapes)
    {
        foreach (var shape in AnimationShapes)
        {
            BottomGumCanvas.Children.Remove(shape);
        }
        AnimationShapes.Clear();

        if (shapes != null && (settingsViewModel.IsShowingFrameShapes || forceRenderShapes))
        {
            foreach (var loopShape in shapes)
            {
                if (loopShape is RectangleViewModel)
                {
                    var shape = (RectangleViewModel)loopShape;

                    var outline = new PolygonRuntime();
                    outline.Color = SKColors.White;

                    var verticalCenter = shape.Height / 2.0f;

                    var shapeLeft = shape.X + owner.RelativeX;
                    var shapeTop = verticalCenter - shape.Y - owner.RelativeY;

                    var left = shapeLeft;
                    var top = verticalCenter + (shapeTop) + shape.Height / 2.0f;
                    var right = shapeLeft + shape.Width;
                    var bottom = verticalCenter + (shapeTop) - shape.Height / 2.0f;

                    outline.IsFilled = false;
                    outline.Points = new List<SKPoint>
                        {
                            new SKPoint(left, top),
                            new SKPoint(right, top),
                            new SKPoint(right, bottom),
                            new SKPoint(left, bottom),
                            new SKPoint(left, top),
                        };

                    AnimationShapes.Add(outline);
                    BottomGumCanvas.Children.Add(outline);
                }

                if (loopShape is CircleViewModel)
                {
                    var shape = (CircleViewModel)loopShape;

                    var outline = new ColoredCircleRuntime();
                    outline.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
                    outline.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;

                    outline.Color = SKColors.White;

                    outline.X = shape.X;
                    outline.Y = -shape.Y;
                    outline.Width = shape.Radius * 2;
                    outline.Height = shape.Radius * 2;

                    outline.IsFilled = false;

                    AnimationShapes.Add(outline);
                    BottomGumCanvas.Children.Add(outline);
                }
            }

        }

    }

    private void RunAnimation()
    {
        AnimationFrameViewModel? frame = null;
        lock (_animationLock)
        {
            if (
                // There's no animation to show...
                _viewModel.CurrentAnimationChain == null ||
                // ...there is an animation to show, but we are showing a frame too, so we don't want to animate over it.
                _viewModel.CurrentAnimationFrame != null)
                return;

            if ((DateTime.Now - _lastFrameTime).TotalMilliseconds < _currentFrameTime)
                return;

            _isFirstTime = _currentDisplayedAnimationFrameIndex < 0;

            _currentDisplayedAnimationFrameIndex++;

            if (_currentDisplayedAnimationFrameIndex >= _viewModel.CurrentAnimationChain.VisibleChildren.Count())
                _currentDisplayedAnimationFrameIndex = 0;

            if(_viewModel.CurrentAnimationChain.VisibleChildren.Count > _currentDisplayedAnimationFrameIndex)
            {
                frame = _viewModel.CurrentAnimationChain.VisibleChildren[_currentDisplayedAnimationFrameIndex];

                _lastFrameTime = DateTime.Now;
                _currentFrameTime = frame.LengthInSeconds * 1000;
            }
        }

        try
        {
            UserControl.Dispatcher.Invoke(() =>
            {
                RenderFrame(frame, frame?.VisibleChildren.ToList(), 
                    // do not force render shapes, we're viewing an animation so only if the animation is 
                    forceRenderShapes:false);

                // Why is this happening constantly. Seems expensive...
                //_cameraLogic.RefreshCameraZoomToViewModel();
                BottomGumCanvas.InvalidateVisual();
            });
        }
        catch (TaskCanceledException)
        {

        }
    }

    private void RenderFrame(AnimationFrameViewModel? frame, List<ShapeViewModel>? shapes, bool forceRenderShapes)
    {
        if(frame == null)
        {
            MainAnimationSprite.Visible = false;
        }
        else
        {
            MainAnimationSprite.Visible = true;
            // don't use percentage, because that will result in a flipped sprite having negative width and height
            //MainAnimationSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            //MainAnimationSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            MainAnimationSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            MainAnimationSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            MainAnimationSprite.TextureAddress = Gum.Managers.TextureAddress.Custom;
            MainAnimationSprite.TextureLeft = (int)frame.LeftCoordinate;
            MainAnimationSprite.TextureTop = (int)frame.TopCoordinate;
            MainAnimationSprite.TextureWidth = FlatRedBall.Math.MathFunctions.RoundToInt(frame.RightCoordinate - frame.LeftCoordinate);
            MainAnimationSprite.TextureHeight = FlatRedBall.Math.MathFunctions.RoundToInt(frame.BottomCoordinate - frame.TopCoordinate);
            MainAnimationSprite.Visible = true;
            MainAnimationSprite.Y = -frame.RelativeY * (frame.FlipVertical ? -1 : 1);
            MainAnimationSprite.X = frame.RelativeX * (frame.FlipHorizontal ? -1 : 1);

            if (frame.FlipHorizontal)
            {
                MainAnimationSprite.TextureLeft += MainAnimationSprite.TextureWidth;
                MainAnimationSprite.TextureWidth = -MainAnimationSprite.TextureWidth;
            }
            if (frame.FlipVertical)
            {
                MainAnimationSprite.TextureTop += MainAnimationSprite.TextureHeight;
                MainAnimationSprite.TextureHeight = -MainAnimationSprite.TextureHeight;
                MainAnimationSprite.Y -= MainAnimationSprite.TextureHeight;
            }
            MainAnimationSprite.Width = System.Math.Abs(MainAnimationSprite.TextureWidth);
            MainAnimationSprite.Height = System.Math.Abs(MainAnimationSprite.TextureHeight);

            MainAnimationSprite.Texture = _cachedTextureService.TryGetTexture(_currentParentDirectory + frame.RelativeTextureName);

        }
        RenderShapes(shapes, frame, forceRenderShapes);
    }

    public void UpdateTextureCache()
    {
        var parentFilePath = _viewModel.AchxFilePath;
        var animationChain = _viewModel.CurrentAnimationChain?.BackingModel;

        if(parentFilePath != null && animationChain != null)
        {
            _currentParentDirectory = parentFilePath.GetDirectoryContainingThis();

            foreach (var frame in animationChain.Frames)
            {
                FilePath filePath = _currentParentDirectory + frame.TextureName;

                _cachedTextureService.RefreshCacheFor(filePath);
            }
        }
    }

    void StartAnimating()
    {
        _animationTimer.Elapsed += (sender, args) => RunAnimation();
        _animationTimer.Interval = 1;
        _animationTimer.Start();
    }

    private void CreateBottomGuideLines()
    {
        this.BottomWindowHorizontalGuide = new PolygonRuntime();
        BottomWindowHorizontalGuide.IsFilled = false;
        BottomWindowHorizontalGuide.Color = SKColors.White;
        BottomWindowHorizontalGuide.Points = new List<SKPoint>
        {
            new SKPoint(-100_000, 0),
            new SKPoint(100_000, 0),
        };

        this.BottomGumCanvas.Children.Add(BottomWindowHorizontalGuide);

        this.BottomWindowVerticalGuide = new PolygonRuntime();
        BottomWindowVerticalGuide.IsFilled = false;
        BottomWindowVerticalGuide.Color = SKColors.White;
        BottomWindowVerticalGuide.Points = new List<SKPoint>
        {
            new SKPoint(0, -100_000),
            new SKPoint(0, 100_000),
        };
        this.BottomGumCanvas.Children.Add(BottomWindowVerticalGuide);
    }

    public void CreateAnimatedSprite()
    {
        MainAnimationSprite = new SpriteRuntime();
        MainAnimationSprite.Visible = false;
        MainAnimationSprite.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
        MainAnimationSprite.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
        MainAnimationSprite.Width = 100;
        MainAnimationSprite.Height = 100;
        MainAnimationSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        MainAnimationSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        this.BottomGumCanvas.Children.Add(MainAnimationSprite);
    }

    private void CreateBackground()
    {

        GumBackground = new SolidRectangleRuntime();
        GumBackground.Color = new SKColor(68, 34, 136);
        GumBackground.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        GumBackground.Width = 2000;
        GumBackground.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        GumBackground.Height = 2000;
        this.BottomGumCanvas.Children.Add(GumBackground);
    }


    public void FocusSingleToSprite()
    {
        var centerX = (MainAnimationSprite.GetAbsoluteLeft() + MainAnimationSprite.GetAbsoluteRight()) / 2.0f;
        var centerY = (MainAnimationSprite.GetAbsoluteTop() + MainAnimationSprite.GetAbsoluteBottom()) / 2.0f;

        var camera = BottomGumCanvas.SystemManagers.Renderer.Camera;

        //// If already zoomed in, stay zoomed in...
        if (zoomViewModel.CurrentZoomPercent < 100)
        {
            zoomViewModel.CurrentZoomPercent = 100;
        }
        camera.X = centerX - (BottomGumCanvas.CanvasSize.Width / 2f) / zoomViewModel.CurrentZoomScale;
        camera.Y = centerY - (BottomGumCanvas.CanvasSize.Height / 2f) / zoomViewModel.CurrentZoomScale;

        _cameraLogic.RefreshCameraZoomToViewModel();
    }


}
