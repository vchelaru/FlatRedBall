using FlatRedBall.IO;
using OfficialPlugins.SpritePlugin.Managers;
using OfficialPlugins.SpritePlugin.GumComponents;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OfficialPlugins.SpritePlugin.ViewModels;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.ComponentModel;

namespace OfficialPlugins.SpritePlugin.Views
{
    /// <summary>
    /// Interaction logic for TextureCoordinateSelectionView.xaml
    /// </summary>
    public partial class TextureCoordinateSelectionView : UserControl
    {
        #region Fields/Properties

        FilePath textureFilePath;
        public FilePath TextureFilePath 
        {
            get => textureFilePath;
            set 
            {
                if(value != textureFilePath)
                {
                    if(value == null || value.Exists() == false)
                    {
                        MainSprite.Texture = null;
                    }
                    else
                    {
                        try
                        {
                            using (var stream = System.IO.File.OpenRead(value.FullPath))
                            {
                                // cache?
                                MainSprite.Texture = SKBitmap.Decode(stream);
                            }
                        }
                        catch
                        {
                            // do we do anything?
                        }
                    }
                }
            }
        }
        public SKBitmap Texture => MainSprite.Texture;
        SpriteRuntime MainSprite;
        public SolidRectangleRuntime Background { get; private set; }


        public TextureCoordinateSelectionViewModel ViewModel => DataContext as TextureCoordinateSelectionViewModel;

        public TextureCoordinateRectangle TextureCoordinateRectangle { get; private set; }

        double? windowsScaleFactor = null;

        #endregion

        #region Constructor/Initialize
        public TextureCoordinateSelectionView()
        {
            InitializeComponent();

            CreateBackground();

            CreateMainSprite();

            CreateSpriteOutline();

            this.Canvas.Children.Add(MainSprite);

            // Initialize CameraLogic after initializing the background so the background
            // position can be set
            CameraLogic.Initialize(this);
            MouseEditingLogic.Initialize(this);

            TextureCoordinateRectangle = new TextureCoordinateRectangle();

            TextureCoordinateRectangle.SetBinding(
                nameof(TextureCoordinateRectangle.X),
                nameof(ViewModel.LeftTexturePixelInt));

            TextureCoordinateRectangle.SetBinding(
                nameof(TextureCoordinateRectangle.Y),
                nameof(ViewModel.TopTexturePixelInt));

            TextureCoordinateRectangle.SetBinding(
                nameof(TextureCoordinateRectangle.Width),
                nameof(ViewModel.SelectedWidthPixelsInt));

            TextureCoordinateRectangle.SetBinding(
                nameof(TextureCoordinateRectangle.Height),
                nameof(ViewModel.SelectedHeightPixelsInt));

            this.Canvas.Children.Add(TextureCoordinateRectangle);

            this.DataContextChanged += HandleDataContextChanged;
        }

        private void CreateSpriteOutline()
        {
            var outline = new RoundedRectangleRuntime();
            outline.StrokeWidth = 1;
            outline.StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;
            outline.CornerRadius = 0;
            outline.Color = new SKColor(255, 255, 255, 128);
            // Normally I'd want 1 more pixel outside of the Sprite.
            // No such layout exists currently and it's probably easier to 
            // just have it match the size for now:
            outline.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            outline.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            outline.Width = 0;
            outline.Height = 0;
            outline.IsFilled = false;
            MainSprite.Children.Add(outline);
        }

        private void CreateMainSprite()
        {
            MainSprite = new SpriteRuntime();
            MainSprite.Width = 100;
            MainSprite.Height = 100;
            MainSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            MainSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        }

        private void CreateBackground()
        {
            Background = new SolidRectangleRuntime();
            Background.Color = new SKColor(68, 34, 136);
            Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            Background.Width = 100;
            Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            Background.Height = 100;
            this.Canvas.Children.Add(Background);
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(ViewModel != null)
            {
                ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(ViewModel.CurrentZoomLevelIndex):
                    CameraLogic.RefreshCameraZoomToViewModel();

                    break;
            }
        }

        #endregion

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CameraLogic.HandleMousePush(e);
            MouseEditingLogic.HandleMousePush(e);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            CameraLogic.HandleMouseMove(e);
            MouseEditingLogic.HandleMouseMove(e);
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            CameraLogic.HandleMouseWheel(e);

        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseEditingLogic.HandleMouseUp(e);
        }

        internal RoundedRectangleRuntime GetHandleAt(Point lastMousePoint)
        {
            double x, y;
            GetWorldPosition(lastMousePoint, out x, out y);

            foreach (var handle in TextureCoordinateRectangle.Handles)
            {
                var left = handle.GetAbsoluteLeft();
                var right = handle.GetAbsoluteRight();
                var top = handle.GetAbsoluteTop();
                var bottom = handle.GetAbsoluteBottom();

                if (x >= left && x <= right &&
                    y >= top && y <= bottom)
                {
                    return handle;
                }
            }
            return null;
        }


        public void GetWorldPosition(Point lastMousePoint, out double x, out double y)
        {
            if(windowsScaleFactor == null)
            {
                // todo - fix on a computer that has scaling using:
                // https://stackoverflow.com/questions/68832226/get-windows-10-text-scaling-value-in-wpf/68846399#comment128365225_68846399

                // This doesn't seem to work on Windows11:
                //var userKey = Microsoft.Win32.Registry.CurrentUser;
                //var softKey = userKey.OpenSubKey("Software");
                //var micKey = softKey.OpenSubKey("Microsoft");
                //var accKey = micKey.OpenSubKey("Accessibility");

                //var factor = accKey.GetValue("TextScaleFactor");

            }

            var camera = this.Canvas.SystemManagers.Renderer.Camera;
          
            x = lastMousePoint.X;
            y = lastMousePoint.Y;
            x /= camera.Zoom;
            y /= camera.Zoom;
            // vic says - did I get the zoom right here?
            x += camera.X;
            y += camera.Y;
        }

    }
}
