using FlatRedBall.Content.AnimationChain;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using OfficialPlugins.SpritePlugin.Managers;
using RenderingLibrary;
using SkiaGum.GueDeriving;
using SkiaGum.Wpf;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OfficialPlugins.AnimationChainPlugin.Managers
{
    internal class TopWindowManager
    {
        #region Fields/Properties

        public SpriteRuntime MainSprite { get; private set; }

        GumSKElement TopGumCanvas;

        FilePath textureFilePath;

        public FilePath TextureFilePath => textureFilePath;

        List<PolygonRuntime> Outlines = new List<PolygonRuntime>();

        public SKBitmap Texture => MainSprite?.Texture;

        SolidRectangleRuntime GumBackground { get; set; }

        CameraLogic CameraLogic;

        SettingsViewModel settingsViewModel;

        #endregion


        public TopWindowManager(GumSKElement topGumCanvas, UserControl userControl, 
            CameraLogic cameraLogic, ZoomViewModel wholeZoom, SettingsViewModel settingsViewModel)
        {

            this.settingsViewModel = settingsViewModel;
            this.settingsViewModel.PropertyChanged += HandleSettingsViewModelPropertyChanged;

            TopGumCanvas = topGumCanvas;
            this.CameraLogic = cameraLogic;

            // Create the abckground first...
            CreateBackground();

            //... then the main sprite so the main sprite sits on top
            CreateMainSprite();

            cameraLogic.Initialize(userControl, wholeZoom, this.TopGumCanvas, this.GumBackground);
            TopGumCanvas.SystemManagers.Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

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
            GumBackground.Color = settingsViewModel.BackgroundColor.ToSKColor();
            TopGumCanvas.InvalidateSurface();
        }

        private void CreateMainSprite()
        {
            MainSprite = new SpriteRuntime();
            MainSprite.Width = 100;
            MainSprite.Height = 100;
            MainSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            MainSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            this.TopGumCanvas.Children.Add(MainSprite);
        }

        void CreateBackground()
        {
            GumBackground = new SolidRectangleRuntime();
            GumBackground.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Width = 100;
            GumBackground.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Height = 100;
            this.TopGumCanvas.Children.Add(GumBackground);
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

        public void RefreshTopCanvasOutlines(AchxViewModel ViewModel)
        {
            foreach (var outline in Outlines)
            {
                TopGumCanvas.Children.Remove(outline);
            }
            Outlines.Clear();

            var texture = MainSprite.Texture;
            if (texture != null && ViewModel != null)
            {
                if (ViewModel.CurrentAnimationFrame != null)
                {
                    CreatePolygonFor(ViewModel.CurrentAnimationFrame.BackingModel);
                }
                else if (ViewModel.CurrentAnimationChain != null)
                {
                    CreatePolygonsFor(ViewModel.CurrentAnimationChain.BackingModel);
                }
                else if (ViewModel.SelectedShape != null)
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


            TopGumCanvas.InvalidateVisual();
        }

        public void RefreshTexture(FilePath achxFilePath, AnimationChainViewModel animationChainViewModel)
        {
            var animationChain = animationChainViewModel?.BackingModel;

            if (animationChain == null)
            {
                ForceRefreshMainSpriteTexture(null);
            }
            else
            {

                var firstFrame = animationChain.Frames.FirstOrDefault();

                var textureName = firstFrame.TextureName;

                var textureAbsolute = achxFilePath.GetDirectoryContainingThis() + textureName;

                ForceRefreshMainSpriteTexture(textureAbsolute);
            }
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

        public void FillSpriteToView(AchxViewModel ViewModel)
        {
            if (MainSprite.Texture == null || TopGumCanvas.ActualWidth == 0 || TopGumCanvas.ActualHeight == 0)
            {
                ViewModel.TopWindowZoom.CurrentZoomPercent = 100;
            }
            else
            {
                var zoomToFitWidth = TopGumCanvas.ActualWidth / MainSprite.Texture.Width;
                var zoomToFitHeight = TopGumCanvas.ActualHeight / MainSprite.Texture.Height;

                var minZoom = Math.Min(zoomToFitWidth, zoomToFitHeight);

                ViewModel.TopWindowZoom.CurrentZoomPercent = (float)minZoom * 100;
            }



            CameraLogic.RefreshCameraZoomToViewModel();
        }

        internal void ResetCamera(AchxViewModel viewModel)
        {
            TopGumCanvas.SystemManagers.Renderer.Camera.X = 0;
            TopGumCanvas.SystemManagers.Renderer.Camera.Y = 0;
            GumBackground.X = 0;
            GumBackground.Y = 0;

            FillSpriteToView(viewModel);

        }

        public void FocusWholeToFrame(AnimationFrameSave animationFrame, AchxViewModel ViewModel)
        {
            var centerX = (animationFrame.LeftCoordinate + animationFrame.RightCoordinate) / 2.0f;
            var centerY = (animationFrame.TopCoordinate + animationFrame.BottomCoordinate) / 2.0f;

            var camera = TopGumCanvas.SystemManagers.Renderer.Camera;

            // If already zoomed in, stay zoomed in...
            if (ViewModel.TopWindowZoom.CurrentZoomPercent < 100)
            {
                ViewModel.TopWindowZoom.CurrentZoomPercent = 100;
            }
            camera.X = centerX - (TopGumCanvas.CanvasSize.Width / 2f) / ViewModel.TopWindowZoom.CurrentZoomScale;
            camera.Y = centerY - (TopGumCanvas.CanvasSize.Height / 2f) / ViewModel.TopWindowZoom.CurrentZoomScale;

            CameraLogic.RefreshCameraZoomToViewModel();
        }

        public void MoveBackgroundToCamera()
        {
            CameraLogic.UpdateBackgroundPositionToCamera();
        }
    }
}
