using FlatRedBall.IO;
using OfficialPlugins.ContentPreview.ViewModels;
using OfficialPlugins.SpritePlugin.Managers;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OfficialPlugins.ContentPreview.Views
{
    /// <summary>
    /// Interaction logic for PngPreviewView.xaml
    /// </summary>
    public partial class PngPreviewView : UserControl
    {
        PngViewModel ViewModel => DataContext as PngViewModel;

        FilePath textureFilePath;
        public FilePath TextureFilePath
        {
            get => textureFilePath;
            set
            {
                if (value != textureFilePath)
                {
                    ForceRefreshMainSpriteTexture(value);
                }
            }
        }

        public SKBitmap Texture => MainSprite?.Texture;

        SpriteRuntime MainSprite;
        SolidRectangleRuntime GumBackground { get; set; }

        CameraLogic CameraLogic;

        public PngPreviewView()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;
        }

        public void ForceRefreshMainSpriteTexture(FilePath value)
        {
            if (value == null || value.Exists() == false)
            {
                MainSprite.Texture = null;
                GumCanvas.InvalidateVisual();
            }
            else
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(value.FullPath))
                    {
                        // cache?
                        MainSprite.Texture = SKBitmap.Decode(stream);
                        GumCanvas.InvalidateVisual();

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

        public void Initialize(CameraLogic cameraLogic)
        {
            this.CameraLogic = cameraLogic;

            CreateBackground();
            CreateMainSprite();

            // do this after creating the background so that it can be passed here:
            CameraLogic.Initialize(this, this.GumCanvas, this.GumBackground);

        }


        private void CreateMainSprite()
        {
            MainSprite = new SpriteRuntime();
            MainSprite.Width = 100;
            MainSprite.Height = 100;
            MainSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            MainSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            this.GumCanvas.Children.Add(MainSprite);
        }

        private void GumCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CameraLogic.HandleMousePush(e);
            //MouseEditingLogic.HandleMousePush(e);

            // This allows the canvas to receive focus:
            // Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/ed6caee6-2cae-4db8-a2df-eafad44dbe37/mouse-focus-versus-keyboard-focus?forum=wpf#:~:text=In%20WPF%2C%20some%20elements%20will%20get%20keyboard%20focus,trick%3A%20userControl.MouseLeftButtonDown%20%2B%3D%20delegate%20%7B%20userControl.Focusable%20%3D%20true%3B
            GumCanvas.Focusable = true;
            IInputElement element = Keyboard.Focus(GumCanvas);
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

        private void CreateBackground()
        {
            GumBackground = new SolidRectangleRuntime();
            GumBackground.Color = new SKColor(68, 34, 136);
            GumBackground.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Width = 100;
            GumBackground.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Height = 100;
            this.GumCanvas.Children.Add(GumBackground);
        }

        internal void ResetCamera()
        {
            GumCanvas.SystemManagers.Renderer.Camera.X = 0;
            GumCanvas.SystemManagers.Renderer.Camera.Y = 0;
            GumBackground.X = 0;
            GumBackground.Y = 0;

            FillSpriteToView();

        }

        private void FillSpriteToView()
        {
            if (MainSprite.Texture == null || GumCanvas.ActualWidth == 0 || GumCanvas.ActualHeight == 0)
            {
                ViewModel.CurrentZoomPercent = 100;
            }
            else
            {
                var zoomToFitWidth = GumCanvas.ActualWidth / MainSprite.Texture.Width;
                var zoomToFitHeight = GumCanvas.ActualHeight / MainSprite.Texture.Height;

                var minZoom = Math.Min(zoomToFitWidth, zoomToFitHeight);

                ViewModel.CurrentZoomPercent = (float)minZoom * 100;
            }



            CameraLogic.RefreshCameraZoomToViewModel();
        }
    }
}
