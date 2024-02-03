using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.Managers;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using OfficialPlugins.SpritePlugin.Managers;
using PropertyTools.Wpf;
using RenderingLibrary;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OfficialPlugins.ContentPreview.Views
{
    /// <summary>
    /// Interaction logic for AchxPreviewView.xaml
    /// </summary>
    public partial class AchxPreviewView : UserControl
    {
        #region Fields/Properties

        AchxViewModel ViewModel => DataContext as AchxViewModel;

        FilePath textureFilePath;
        FilePath TextureFilePath
        {
            get => textureFilePath;
            set
            {
                if (value != textureFilePath)
                {
                    ForceRefreshMainSpriteTexture(value);
                    ForceRefreshMainAnimationSpriteTexture(value);
                }
            }
        }

        FilePath achxFilePath;
        public FilePath AchxFilePath
        {
            get => achxFilePath;
            set
            {
                if(value != achxFilePath)
                {
                    achxFilePath = value;
                    ForceRefreshAchx(value);
                }
            }
        }

        public SKBitmap Texture => MainSprite?.Texture;

        SpriteRuntime MainSprite;
        SpriteRuntime MainAnimationSprite;
        List<PolygonRuntime> Outlines = new List<PolygonRuntime>();
        List<SkiaShapeRuntime> AnimationShapes = new List<SkiaShapeRuntime>();

        // todo - Vic says - let's move all the bototm grid stuff into its own class:
        PolygonRuntime BottomWindowHorizontalGuide;
        PolygonRuntime BottomWindowVerticalGuide;

        SolidRectangleRuntime GumBackground { get; set; }
        SolidRectangleRuntime GumAnimationBackground { get; set; }

        CameraLogic CameraLogic;
        CameraLogic CameraLogicAnimation;

        #endregion

        public AchxPreviewView()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;

            this.DataContextChanged += HandleDataContextChanged;
            //MemberCategoryManager.SetMemberCategories(PropertyGrid);

            PropertyGridManager.Initialize(PropertyGrid);
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.SelectedAnimationChain):

                    RefreshTopCanvasOutlines();
                    RefreshAnimationPreview();

                    TopGumCanvas.InvalidateVisual();
                    BottomGumCanvas.InvalidateVisual();

                    break;
                case nameof(ViewModel.IsShowGuidesChecked):
                    RefreshBottomGuideVisibility();
                    break;
            }
        }

        private void RefreshBottomGuideVisibility()
        {
            BottomWindowVerticalGuide.Visible = ViewModel.IsShowGuidesChecked;
            BottomWindowHorizontalGuide.Visible = ViewModel.IsShowGuidesChecked;
            BottomGumCanvas.InvalidateVisual();
        }

        public void ForceRefreshAchx(FilePath achxFilePath = null, bool preserveSelection = false)
        {
            GlueCommands.Self.DoOnUiThread(() =>
            {
                var previouslySelected = ViewModel.SelectedAnimationChain;

                achxFilePath = achxFilePath ?? this.AchxFilePath;
                foreach (var outline in Outlines)
                {
                    TopGumCanvas.Children.Remove(outline);
                }

                AnimationChainListSave animationChain = null;
                if (achxFilePath?.Exists() == true)
                {
                    animationChain = AnimationChainListSave.FromFile(achxFilePath.FullPath);
                }
                ViewModel.BackgingData = animationChain;
                RefreshTexture(achxFilePath, animationChain);

                RefreshTopCanvasOutlines();

                RefreshTreeView(animationChain);

                if(preserveSelection && previouslySelected != null)
                {
                    ViewModel.SelectedAnimationChain = ViewModel.VisibleRoot
                        .FirstOrDefault(item => item.Name == previouslySelected.Name);
                }

                TopGumCanvas.InvalidateVisual();
            });
        }

        private void RefreshTreeView(AnimationChainListSave animationChain)
        {
            ViewModel.VisibleRoot.Clear();

            if (animationChain == null) return;

            foreach(var animation in animationChain.AnimationChains)
            {
                var animationViewModel = new AnimationChainViewModel();
                animationViewModel.SetFrom(animation, ViewModel.ResolutionWidth, ViewModel.ResolutionHeight);
                ViewModel.VisibleRoot.Add(animationViewModel);

                animationViewModel.FrameUpdatedByUi += HandleFrameUpdated;
            }
        }

        private void HandleFrameUpdated()
        {
            RefreshTopCanvasOutlines();
            TopGumCanvas.InvalidateVisual();
            RefreshAnimationPreview();
            BottomGumCanvas.InvalidateVisual();

            // now save it:
            var animationChain = ViewModel.BackgingData;
            var filePath = this.achxFilePath;

            GlueCommands.Self.FileCommands.IgnoreChangeOnFileUntil(
                filePath, DateTimeOffset.Now.AddSeconds(2));
            try
            {
                GlueCommands.Self.TryMultipleTimes(() =>
                {
                    animationChain.Save(filePath.FullPath);
                });
            }
            catch (Exception ex)
            {
                GlueCommands.Self.PrintError(ex.ToString());
            }
        }

        private void RefreshTexture(FilePath value, AnimationChainListSave animationChain)
        {
            if (animationChain == null)
            {
                ForceRefreshMainSpriteTexture(null);
            }
            else
            {

                var firstAnimation = animationChain.AnimationChains.FirstOrDefault(item => item.Frames.Count > 0);
                if (firstAnimation != null)
                {
                    var firstFrame = firstAnimation.Frames.FirstOrDefault();

                    var textureName = firstFrame.TextureName;

                    var textureAbsolute = value.GetDirectoryContainingThis() + textureName;

                    ForceRefreshMainSpriteTexture(textureAbsolute);
                    ForceRefreshMainAnimationSpriteTexture(textureAbsolute);
                }
            }
        }

        private void RefreshTopCanvasOutlines()
        {
            foreach(var outline in Outlines)
            {
                TopGumCanvas.Children.Remove(outline);
            }
            Outlines.Clear();
            var texture = MainSprite.Texture;
            if (texture != null && ViewModel != null)
            {
                if(ViewModel.SelectedAnimationFrame != null)
                {
                    CreatePolygonFor(ViewModel.SelectedAnimationFrame.BackingModel);
                }
                else if(ViewModel.SelectedAnimationChain != null)
                {
                    CreatePolygonsFor(ViewModel.SelectedAnimationChain.BackingModel);
                }
                else if(ViewModel.SelectedShape != null)
                {
                    //Do Nothing
                }
                else //if(ViewModel.SelectedAnimationChain == null)
                {
                    foreach (var animationVm in ViewModel.VisibleRoot)
                    {
                        CreatePolygonsFor(animationVm.BackingModel);
                    }
                }
            }

            void CreatePolygonsFor(AnimationChainSave animation)
            {
                foreach (var frame in animation.Frames)
                {
                    CreatePolygonFor(frame);
                }
            }
        }

        private void RefreshAnimationPreview()
        {
            var texture = MainAnimationSprite.Texture;
            
            _currentAnimationChain = null;
            _currentAnimationFrame = -1;
            _lastFrameTime = DateTime.MinValue;
            _currentFrameTime = 0;

            foreach(var shape in AnimationShapes)
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
                    if(ViewModel.SelectedShape != null)
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
                else if (ViewModel.SelectedAnimationChain != null)
                {
                    if (ViewModel.SelectedAnimationChain.VisibleChildren.Count > 0)
                    {
                        _currentAnimationChain = ViewModel.SelectedAnimationChain;

                        RunAnimation();
                    }
                }
                else //if(ViewModel.SelectedAnimationChain == null)
                {
                    MainAnimationSprite.Visible = false;
                }
            }
        }

        private void RenderShapes(List<ShapeViewModel> shapes)
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

                    var left = shape.X;
                    var top = verticalCenter + (verticalCenter - shape.Y) + shape.Height / 2.0f;
                    var right = shape.X + shape.Width;
                    var bottom = verticalCenter + (verticalCenter - shape.Y) - shape.Height / 2.0f;

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
                    outline.Color = SKColors.White;

                    outline.X = shape.X - shape.Radius;
                    outline.Y = -shape.Y - shape.Radius;
                    outline.Width = shape.Radius * 2;
                    outline.Height = shape.Radius * 2;

                    outline.IsFilled = false;

                    AnimationShapes.Add(outline);
                    BottomGumCanvas.Children.Add(outline);
                }
            }
        }

        private object _animationLock = new object();
        private int _currentAnimationFrame = 0;
        private AnimationChainViewModel _currentAnimationChain = null;
        private DateTime _lastFrameTime = DateTime.MinValue;
        private float _currentFrameTime = 0;
        private System.Timers.Timer _animationTimer = new System.Timers.Timer();
        private bool _isFirstTime = false;
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

            Dispatcher.Invoke(() =>
            {
                RenderFrame(frame, frame.VisibleChildren.ToList());
                
                CameraLogicAnimation.RefreshCameraZoomToViewModel();
            });
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
            MainAnimationSprite.Y = 0;

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


            RenderShapes(shapes);


        }

        private void CreatePolygonFor(AnimationFrameSave frame)
        {
            PolygonRuntime outline = CreateOutlinePolygon(frame);
            Outlines.Add(outline);
            TopGumCanvas.Children.Add(outline);
        }

        private static PolygonRuntime CreateOutlinePolygon(AnimationFrameSave frame)
        {
            var left = frame.LeftCoordinate;
            var right = frame.RightCoordinate;
            var top = frame.TopCoordinate;
            var bottom = frame.BottomCoordinate;

            var outline = new PolygonRuntime();
            outline.Color = SKColors.White;
            outline.Name = "Frame Outline";
            outline.IsFilled = false;
            outline.Points = new List<SKPoint>
                            {
                                new SKPoint(left, top),
                                new SKPoint(right, top),
                                new SKPoint(right, bottom),
                                new SKPoint(left, bottom),
                                new SKPoint(left, top),
                            };
            return outline;
        }

        public void ForceRefreshMainSpriteTexture(FilePath value)
        {
            if (value == null || value.Exists() == false)
            {
                MainSprite.Texture = null;
                TopGumCanvas.InvalidateVisual();
            }
            else
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(value.FullPath))
                    {
                        // cache?
                        MainSprite.Texture = SKBitmap.Decode(stream);
                        TopGumCanvas.InvalidateVisual();

                    }
                }
                catch
                {
                    // do we do anything?
                }
            }

            textureFilePath = value;
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

            textureFilePath = value;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            FillSpriteToView();
        }

        public void Initialize(CameraLogic cameraLogic, CameraLogic cameraLogicAnimation)
        {
            this.CameraLogic = cameraLogic;
            this.CameraLogicAnimation = cameraLogicAnimation;

            CreateBackground();
            CreateMainSprite();

            CreateBottomGuideLines();

            // do this after creating the background so that it can be passed here:
            CameraLogic.Initialize(this, (this.DataContext as AchxViewModel)?.WholeZoom, this.TopGumCanvas, this.GumBackground);
            CameraLogicAnimation.Initialize(this, (this.DataContext as AchxViewModel)?.SingleZoom, this.BottomGumCanvas, this.GumAnimationBackground);

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

        private void CreateMainSprite()
        {
            MainSprite = new SpriteRuntime();
            MainSprite.Width = 100;
            MainSprite.Height = 100;
            MainSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            MainSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            this.TopGumCanvas.Children.Add(MainSprite);

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

        private void GumCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CameraLogic.HandleMousePush(e);
            //MouseEditingLogic.HandleMousePush(e);

            // This allows the canvas to receive focus:
            // Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/ed6caee6-2cae-4db8-a2df-eafad44dbe37/mouse-focus-versus-keyboard-focus?forum=wpf#:~:text=In%20WPF%2C%20some%20elements%20will%20get%20keyboard%20focus,trick%3A%20userControl.MouseLeftButtonDown%20%2B%3D%20delegate%20%7B%20userControl.Focusable%20%3D%20true%3B
            TopGumCanvas.Focusable = true;
            IInputElement element = Keyboard.Focus(TopGumCanvas);
        }

        private void GumCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            CameraLogic.HandleMouseMove(e);
            //MouseEditingLogic.HandleMouseMove(e);
        }

        private void GumCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            CameraLogic.HandleMouseWheel(e);
        }

        private void GumAnimationCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CameraLogicAnimation.HandleMousePush(e);
            //MouseEditingLogic.HandleMousePush(e);

            // This allows the canvas to receive focus:
            // Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/ed6caee6-2cae-4db8-a2df-eafad44dbe37/mouse-focus-versus-keyboard-focus?forum=wpf#:~:text=In%20WPF%2C%20some%20elements%20will%20get%20keyboard%20focus,trick%3A%20userControl.MouseLeftButtonDown%20%2B%3D%20delegate%20%7B%20userControl.Focusable%20%3D%20true%3B
            BottomGumCanvas.Focusable = true;
            IInputElement element = Keyboard.Focus(BottomGumCanvas);
        }

        private void GumAnimationCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            CameraLogicAnimation.HandleMouseMove(e);
            //MouseEditingLogic.HandleMouseMove(e);
        }

        private void GumAnimationCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            CameraLogicAnimation.HandleMouseWheel(e);
        }

        private void CreateBackground()
        {
            GumBackground = new SolidRectangleRuntime();
            GumBackground.Color = new SKColor(68, 34, 136);
            GumBackground.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Width = 100;
            GumBackground.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Height = 100;
            this.TopGumCanvas.Children.Add(GumBackground);

            GumAnimationBackground = new SolidRectangleRuntime();
            GumAnimationBackground.Color = new SKColor(68, 34, 136);
            GumAnimationBackground.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumAnimationBackground.Width = 100;
            GumAnimationBackground.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumAnimationBackground.Height = 100;
            this.BottomGumCanvas.Children.Add(GumAnimationBackground);
        }

        internal void ResetCamera()
        {
            TopGumCanvas.SystemManagers.Renderer.Camera.X = 0;
            TopGumCanvas.SystemManagers.Renderer.Camera.Y = 0;
            GumBackground.X = 0;
            GumBackground.Y = 0;

            FillSpriteToView();

        }


        private void FillSpriteToView()
        {
            if (MainSprite.Texture == null || TopGumCanvas.ActualWidth == 0 || TopGumCanvas.ActualHeight == 0)
            {
                ViewModel.WholeZoom.CurrentZoomPercent = 100;
            }
            else
            {
                var zoomToFitWidth = TopGumCanvas.ActualWidth / MainSprite.Texture.Width;
                var zoomToFitHeight = TopGumCanvas.ActualHeight / MainSprite.Texture.Height;

                var minZoom = Math.Min(zoomToFitWidth, zoomToFitHeight);

                ViewModel.WholeZoom.CurrentZoomPercent = (float)minZoom * 100;
            }



            CameraLogic.RefreshCameraZoomToViewModel();
        }

        private void FillAnimationSpriteToView()
        {
            if (MainAnimationSprite.Texture == null || BottomGumCanvas.ActualWidth == 0 || BottomGumCanvas.ActualHeight == 0)
            {
                ViewModel.WholeZoom.CurrentAnimationZoomPercent = 100;
            }
            else
            {
                var zoomToFitWidth = BottomGumCanvas.ActualWidth / MainAnimationSprite.Texture.Width;
                var zoomToFitHeight = BottomGumCanvas.ActualHeight / MainAnimationSprite.Texture.Height;

                var minZoom = Math.Min(zoomToFitWidth, zoomToFitHeight);

                ViewModel.WholeZoom.CurrentAnimationZoomPercent = (float)minZoom * 100;
            }



            CameraLogicAnimation.RefreshCameraZoomToViewModel();
        }


        private void TreeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject originalSource = e.OriginalSource as DependencyObject;
            var treeViewItem = GetTreeViewItemFromOriginalSource(originalSource);

            if (treeViewItem != null)
            {
                if(treeViewItem.DataContext is AnimationChainViewModel animationChainVm)
                {
                    FocusSingleToSprite();
                    FocusWholeToAnimation(animationChainVm.BackingModel);
                }
                else if(treeViewItem.DataContext is AnimationFrameViewModel animationFrameVm)
                {
                    FocusSingleToSprite();
                    FocusWholeToFrame(animationFrameVm.BackingModel);
                }
            }
        }

        private void FocusWholeToAnimation(AnimationChainSave backingModel)
        {
            if (backingModel.Frames.Count > 0)
            {
                var firstFrame = backingModel.Frames[0];
                FocusWholeToFrame(firstFrame);
            }
        }

        private void FocusWholeToFrame(AnimationFrameSave animationFrame)
        {
            var centerX = (animationFrame.LeftCoordinate + animationFrame.RightCoordinate) / 2.0f;
            var centerY = (animationFrame.TopCoordinate + animationFrame.BottomCoordinate) / 2.0f;

            var camera = TopGumCanvas.SystemManagers.Renderer.Camera;

            // If already zoomed in, stay zoomed in...
            if (ViewModel.WholeZoom.CurrentZoomPercent < 100)
            {
                ViewModel.WholeZoom.CurrentZoomPercent = 100;
            }
            camera.X = centerX - (TopGumCanvas.CanvasSize.Width / 2f) / ViewModel.WholeZoom.CurrentZoomScale;
            camera.Y = centerY - (TopGumCanvas.CanvasSize.Height / 2f) / ViewModel.WholeZoom.CurrentZoomScale;

            CameraLogic.RefreshCameraZoomToViewModel();
        }

        private void FocusSingleToSprite()
        {
            var centerX = (MainAnimationSprite.GetAbsoluteLeft() + MainAnimationSprite.GetAbsoluteRight()) / 2.0f;
            var centerY = (MainAnimationSprite.GetAbsoluteTop() + MainAnimationSprite.GetAbsoluteBottom()) / 2.0f;

            var camera = BottomGumCanvas.SystemManagers.Renderer.Camera;

            //// If already zoomed in, stay zoomed in...
            if (ViewModel.SingleZoom.CurrentZoomPercent < 100)
            {
                ViewModel.SingleZoom.CurrentZoomPercent = 100;
            }
            camera.X = centerX - (BottomGumCanvas.CanvasSize.Width / 2f) / ViewModel.SingleZoom.CurrentZoomScale;
            camera.Y = centerY - (BottomGumCanvas.CanvasSize.Height / 2f) / ViewModel.SingleZoom.CurrentZoomScale;

            CameraLogicAnimation.RefreshCameraZoomToViewModel();
        }

        private TreeListBoxItem GetTreeViewItemFromOriginalSource(DependencyObject originalSource)
        {
            while (originalSource != null && !(originalSource is TreeListBoxItem) && !(originalSource is TreeView))
            {
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            return originalSource as TreeListBoxItem;
        }
    }
}
