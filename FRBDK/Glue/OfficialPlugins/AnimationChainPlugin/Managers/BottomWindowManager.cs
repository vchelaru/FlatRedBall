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

namespace OfficialPlugins.AnimationChainPlugin.Managers
{
    internal class BottomWindowManager
    {
        #region Fields/Properties

        SpriteRuntime MainAnimationSprite;

        PolygonRuntime BottomWindowHorizontalGuide;
        PolygonRuntime BottomWindowVerticalGuide;

        private int _currentAnimationFrame = 0;
        private AnimationChainViewModel _currentAnimationChain = null;
        private DateTime _lastFrameTime = DateTime.MinValue;
        private float _currentFrameTime = 0;

        List<SkiaShapeRuntime> AnimationShapes = new List<SkiaShapeRuntime>();

        private System.Timers.Timer _animationTimer = new System.Timers.Timer();

        GumSKElement BottomGumCanvas;

        private bool _isFirstTime = false;

        private object _animationLock = new object();

        CameraLogic CameraLogic { get; set; }

        UserControl UserControl;

        ZoomViewModel zoomViewModel;

        SettingsViewModel settingsViewModel;


        SolidRectangleRuntime GumBackground { get; set; }

        #endregion

        public BottomWindowManager(GumSKElement bottomGumCanvas, UserControl userControl, 
            CameraLogic cameraLogic, ZoomViewModel bottomWindowZoom, SettingsViewModel settingsViewModel)
        {
            zoomViewModel = bottomWindowZoom;
            this.settingsViewModel = settingsViewModel;
            this.settingsViewModel.PropertyChanged += HandleSettingsViewModelPropertyChanged;
            UserControl = userControl;
            BottomGumCanvas = bottomGumCanvas;
            CameraLogic = cameraLogic;

            

            // background first, so it's behind the other sprites
            CreateBackground();
            CreateAnimatedSprite();
            CreateBottomGuideLines();

            CameraLogic.Initialize(userControl, bottomWindowZoom, BottomGumCanvas, this.GumBackground);
            bottomGumCanvas.SystemManagers.Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

            StartAnimating();

            // Refresh after creating the sprite
            RefreshBackgroundColor();
        }

        private void HandleSettingsViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(SettingsViewModel.BackgroundColor):
                    RefreshBackgroundColor();
                    break;
            }
        }

        private void RefreshBackgroundColor()
        {
            GumBackground.Color = settingsViewModel.BackgroundColor.ToSKColor();
            BottomGumCanvas.InvalidateSurface();
        }

        public void RefreshAnimationPreview(AchxViewModel ViewModel)
        {
            var texture = MainAnimationSprite.Texture;

            _currentAnimationChain = null;
            _currentAnimationFrame = -1;
            _lastFrameTime = DateTime.MinValue;
            _currentFrameTime = 0;

            foreach (var shape in AnimationShapes)
            {
                BottomGumCanvas.Children.Remove(shape);
            }
            AnimationShapes.Clear();

            if (texture != null && ViewModel != null)
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

                    RenderFrame(frame, shapes);
                }
                else if (ViewModel.CurrentAnimationChain != null)
                {
                    if (ViewModel.CurrentAnimationChain.VisibleChildren.Count > 0)
                    {
                        _currentAnimationChain = ViewModel.CurrentAnimationChain;

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
            var isShowGuidesChecked = viewModel.Settings.ShowGuides;
            BottomWindowVerticalGuide.Visible = isShowGuidesChecked;
            BottomWindowHorizontalGuide.Visible = isShowGuidesChecked;
            BottomGumCanvas.InvalidateVisual();
        }

        private void RenderShapes(List<ShapeViewModel> shapes, AnimationFrameViewModel owner)
        {
            foreach (var shape in AnimationShapes)
            {
                BottomGumCanvas.Children.Remove(shape);
            }
            AnimationShapes.Clear();

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

        private void RunAnimation()
        {
            AnimationFrameViewModel frame;
            lock (_animationLock)
            {
                if (_currentAnimationChain == null)
                    return;

                if ((DateTime.Now - _lastFrameTime).TotalMilliseconds < _currentFrameTime)
                    return;

                _isFirstTime = _currentAnimationFrame < 0;

                _currentAnimationFrame++;

                if (_currentAnimationFrame >= _currentAnimationChain.VisibleChildren.Count())
                    _currentAnimationFrame = 0;

                frame = _currentAnimationChain.VisibleChildren[_currentAnimationFrame];

                _lastFrameTime = DateTime.Now;
                _currentFrameTime = frame.LengthInSeconds * 1000;
            }

            try
            {
                UserControl.Dispatcher.Invoke(() =>
                {
                    RenderFrame(frame, frame.VisibleChildren.ToList());

                    CameraLogic.RefreshCameraZoomToViewModel();
                });
            }
            catch(TaskCanceledException)
            {

            }
        }

        private void RenderFrame(AnimationFrameViewModel frame, List<ShapeViewModel> shapes)
        {
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
            MainAnimationSprite.Y = -frame.RelativeY * (frame.FlipVertical?-1:1);
            MainAnimationSprite.X = frame.RelativeX * (frame.FlipHorizontal?-1:1);

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


            RenderShapes(shapes, frame);


        }

        public void ForceRefreshMainAnimationSpriteTexture(FilePath value)
        {
            if (value == null || value.Exists() == false)
            {
                MainAnimationSprite.Texture = null;
                BottomGumCanvas.InvalidateVisual();
            }
            else
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(value.FullPath))
                    {
                        // cache?
                        MainAnimationSprite.Texture = SKBitmap.Decode(stream);
                        BottomGumCanvas.InvalidateVisual();

                    }
                }
                catch
                {
                    // do we do anything?
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

        //private void FillAnimationSpriteToView(AchxViewModel viewModel)
        //{
        //    if (MainAnimationSprite.Texture == null || BottomGumCanvas.ActualWidth == 0 || BottomGumCanvas.ActualHeight == 0)
        //    {
        //        viewModel.WholeZoom.CurrentAnimationZoomPercent = 100;
        //    }
        //    else
        //    {
        //        var zoomToFitWidth = BottomGumCanvas.ActualWidth / MainAnimationSprite.Texture.Width;
        //        var zoomToFitHeight = BottomGumCanvas.ActualHeight / MainAnimationSprite.Texture.Height;

        //        var minZoom = Math.Min(zoomToFitWidth, zoomToFitHeight);

        //        viewModel.WholeZoom.CurrentAnimationZoomPercent = (float)minZoom * 100;
        //    }



        //    CameraLogic.RefreshCameraZoomToViewModel();
        //}

        private void CreateBackground()
        {

            GumBackground = new SolidRectangleRuntime();
            GumBackground.Color = new SKColor(68, 34, 136);
            GumBackground.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Width = 100;
            GumBackground.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Height = 100;
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

            CameraLogic.RefreshCameraZoomToViewModel();
        }


    }
}
