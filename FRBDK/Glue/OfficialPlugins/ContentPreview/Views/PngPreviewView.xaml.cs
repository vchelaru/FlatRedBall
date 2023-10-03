using FlatRedBall.IO;
using OfficialPlugins.ContentPreview.ViewModels;
using OfficialPlugins.SpritePlugin.Managers;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OfficialPlugins.ContentPreview.Views
{
    /// <summary>
    /// Interaction logic for PngPreviewView.xaml
    /// </summary>
    public partial class PngPreviewView : UserControl
    {
        PngViewModel ViewModel => DataContext as PngViewModel;

        private FilePath _textureFilePath;
        public FilePath TextureFilePath
        {
            get => _textureFilePath;
            set
            {
                if (value != _textureFilePath)
                {
                    ForceRefreshMainSpriteTexture(value);
                }
            }
        }

        public SKBitmap Texture => _mainSprite?.Texture;

        private SpriteRuntime _mainSprite;
        private SolidRectangleRuntime GumBackground { get; set; }

        private CameraLogic _cameraLogic;

        public PngPreviewView()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;
        }

        public void ForceRefreshMainSpriteTexture(FilePath value)
        {
            if (value == null || value.Exists() == false)
            {
                _mainSprite.Texture = null;
                GumCanvas.InvalidateVisual();
            }
            else
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(value.FullPath))
                    {
                        // cache?
                        _mainSprite.Texture = SKBitmap.Decode(stream);
                        GumCanvas.InvalidateVisual();

                    }
                }
                catch
                {
                    // do we do anything?
                }
            }

            _textureFilePath = value;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            FillSpriteToView();
        }

        public void Initialize(CameraLogic cameraLogic)
        {
            this._cameraLogic = cameraLogic;

            CreateBackground();
            CreateMainSprite();

            // do this after creating the background so that it can be passed here:
            _cameraLogic.Initialize(this, null, this.GumCanvas, this.GumBackground);

        }


        private void CreateMainSprite()
        {
            _mainSprite = new SpriteRuntime();
            _mainSprite.Width = 100;
            _mainSprite.Height = 100;
            _mainSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            _mainSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            this.GumCanvas.Children.Add(_mainSprite);
        }

        private void GumCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _cameraLogic.HandleMousePush(e);
            //MouseEditingLogic.HandleMousePush(e);

            // This allows the canvas to receive focus:
            // Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/ed6caee6-2cae-4db8-a2df-eafad44dbe37/mouse-focus-versus-keyboard-focus?forum=wpf#:~:text=In%20WPF%2C%20some%20elements%20will%20get%20keyboard%20focus,trick%3A%20userControl.MouseLeftButtonDown%20%2B%3D%20delegate%20%7B%20userControl.Focusable%20%3D%20true%3B
            GumCanvas.Focusable = true;
            IInputElement element = Keyboard.Focus(GumCanvas);
        }

        private void GumCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            _cameraLogic.HandleMouseMove(e);
            //MouseEditingLogic.HandleMouseMove(e);
        }

        private void GumCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _cameraLogic.HandleMouseWheel(e);
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
            if (_mainSprite.Texture == null || GumCanvas.ActualWidth == 0 || GumCanvas.ActualHeight == 0)
            {
                ViewModel.CurrentZoomPercent = 100;
            }
            else
            {
                var zoomToFitWidth = GumCanvas.ActualWidth / _mainSprite.Texture.Width;
                var zoomToFitHeight = GumCanvas.ActualHeight / _mainSprite.Texture.Height;

                var minZoom = Math.Min(zoomToFitWidth, zoomToFitHeight);

                ViewModel.CurrentZoomPercent = (float)minZoom * 100;
            }



            _cameraLogic.RefreshCameraZoomToViewModel();
        }
    }
}
