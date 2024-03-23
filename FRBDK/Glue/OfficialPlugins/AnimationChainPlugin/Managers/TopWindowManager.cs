using FlatRedBall.Content.AnimationChain;
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

namespace OfficialPlugins.AnimationChainPlugin.Managers
{
    internal class TopWindowManager
    {
        #region Fields/Properties

        SpriteRuntime MainSprite;

        GumSKElement TopGumCanvas;

        FilePath textureFilePath;

        List<PolygonRuntime> Outlines = new List<PolygonRuntime>();

        public SKBitmap Texture => MainSprite?.Texture;

        SolidRectangleRuntime GumBackground { get; set; }

        CameraLogic CameraLogic;
        #endregion


        public TopWindowManager(GumSKElement topGumCanvas, UserControl userControl, CameraLogic cameraLogic, ZoomViewModel wholeZoom)
        {

            TopGumCanvas = topGumCanvas;
            this.CameraLogic = cameraLogic;

            // Create the abckground first...
            CreateBackground();

            //... then the main sprite so the main sprite sits on top
            CreateMainSprite();

            cameraLogic.Initialize(userControl, wholeZoom, this.TopGumCanvas, this.GumBackground);
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
            GumBackground.Color = new SKColor(68, 34, 136);
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
                if (ViewModel.SelectedAnimationFrame != null)
                {
                    CreatePolygonFor(ViewModel.SelectedAnimationFrame.BackingModel);
                }
                else if (ViewModel.SelectedAnimationChain != null)
                {
                    CreatePolygonsFor(ViewModel.SelectedAnimationChain.BackingModel);
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

        internal void ResetCamera(AchxViewModel viewModel)
        {
            TopGumCanvas.SystemManagers.Renderer.Camera.X = 0;
            TopGumCanvas.SystemManagers.Renderer.Camera.Y = 0;
            GumBackground.X = 0;
            GumBackground.Y = 0;

            FillSpriteToView(viewModel);

        }
    }
}
