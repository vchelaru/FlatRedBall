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

        public LineGridRuntime linegrid { get; private set; }

        public void SelectCell(Point position, out int columnX, out int columnY)
        {
            GetWorldPosition(position, out double worldX, out double worldY);
            linegrid.LineGridCell(worldX, worldY, out columnX, out columnY);
            if(linegrid.GetCellPosition(columnX, columnY, out float left, out float top, out float right, out float bottom)) {
                ViewModel.LeftTexturePixel = (decimal)left;
                ViewModel.TopTexturePixel = (decimal)top;
                ViewModel.SelectedWidthPixels = (decimal)right;
                ViewModel.SelectedHeightPixels = (decimal)bottom;
            }
        }

        public void SelectDragCell(Point MousePoint, int StartDragSelectX, int StartDragSelectY) {
            GetWorldPosition(MousePoint, out double worldX, out double worldY);
            linegrid.LineGridCell(worldX, worldY, out int ColX, out int ColY);
            if((ColX != StartDragSelectX) || (ColY != StartDragSelectY))
            {
                var startok = linegrid.GetCellPosition(StartDragSelectX, StartDragSelectY, out float startLeft, out float startTop, out float startRight, out float startBottom);
                var endok = linegrid.GetCellPosition(ColX, ColY, out float endLeft, out float endTop, out float endRight, out float endBottom);
                if(startok && endok) {
                    ViewModel.LeftTexturePixel = (decimal)Math.Min(startLeft, endLeft);
                    ViewModel.TopTexturePixel = (decimal)Math.Min(startTop, endTop);
                    if(startLeft + startRight < endLeft + endRight)
                        ViewModel.SelectedWidthPixels = (decimal)(endLeft - startLeft + endRight);
                    else
                        ViewModel.SelectedWidthPixels = (decimal)(startLeft - endLeft + startRight);
                    if(startTop + startBottom < endTop + endBottom)
                        ViewModel.SelectedHeightPixels = (decimal)(endTop - startTop + endBottom);
                    else
                        ViewModel.SelectedHeightPixels = (decimal)(startTop - endTop + startBottom);
                }
            }
        }

        public TextureCoordinateSelectionViewModel ViewModel => DataContext as TextureCoordinateSelectionViewModel;

        public TextureCoordinateRectangle TextureCoordinateRectangle { get; private set; }

        double? windowsScaleFactor = null;
        public double WindowsScaleFactor
        {
            get
            {
                if (windowsScaleFactor == null)
                {
                    // todo - fix on a computer that has scaling using:
                    // https://stackoverflow.com/questions/68832226/get-windows-10-text-scaling-value-in-wpf/68846399#comment128365225_68846399

                    // This doesn't seem to work on Windows11:
                    //var userKey = Microsoft.Win32.Registry.CurrentUser;
                    //var softKey = userKey.OpenSubKey("Software");
                    //var micKey = softKey.OpenSubKey("Microsoft");
                    //var accKey = micKey.OpenSubKey("Accessibility");

                    //var factor = accKey.GetValue("TextScaleFactor");
                    // this returns text scale, not window scale
                    //var uiSettings = new Windows.UI.ViewManagement.UISettings();
                    //windowsScaleFactor = uiSettings.
                    windowsScaleFactor =
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth;
                }
                return windowsScaleFactor.Value;
            }
        }

        #endregion

        #region Constructor/Initialize
        public TextureCoordinateSelectionView()
        {
            InitializeComponent();

            CreateBackground();
            CreateMainSprite();
            CreateSpriteOutline();

            // Initialize CameraLogic after initializing the background so the background
            // position can be set
            CameraLogic.Initialize(this);
            MouseEditingLogic.Initialize(this);

            this.Canvas.Children.Add(MainSprite);

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


            linegrid = new LineGridRuntime();
            linegrid.X = 0;
            linegrid.Y = 0;
            linegrid.SetBinding(nameof(linegrid.Width), nameof(ViewModel.TextureWidth));
            linegrid.SetBinding(nameof(linegrid.Height), nameof(ViewModel.TextureHeight));
            linegrid.SetBinding(nameof(linegrid.CellWidth), nameof(ViewModel.CellWidth));
            linegrid.SetBinding(nameof(linegrid.CellHeight), nameof(ViewModel.CellHeight));
            this.Canvas.Children.Add(linegrid);

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
                case nameof(ViewModel.CurrentZoomPercent):
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
            var camera = this.Canvas.SystemManagers.Renderer.Camera;
          
            x = lastMousePoint.X * WindowsScaleFactor;
            y = lastMousePoint.Y * WindowsScaleFactor;
            x /= camera.Zoom;
            y /= camera.Zoom;
            // vic says - did I get the zoom right here?
            x += camera.X;
            y += camera.Y;
        }

        public void ZoomToTexture()
        {
            //-40 constant is x2 -20 in cameralogic initialization, but it doesn't seem to perfectly center w/ 20 all around.  must be wrong.
            var zoomw = 100 * ((ActualWidth - 40) / Texture.Width);
            var zoomh = 100 * ((ActualHeight - 40) / Texture.Height);
            ViewModel.CurrentZoomPercent = (float)Math.Round(Math.Min(zoomw, zoomh), 2);

            CameraLogic.ResetCamera();

            Canvas.InvalidateVisual();
        }

        #region it's always such a fight with wpf to do anything besides simple splut ui's https://stackoverflow.com/a/16328482/5679683
        private void TextBox_Select(object sender, RoutedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if(tb != null)
                tb.SelectAll();
            }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if((tb != null) && (!tb.IsKeyboardFocusWithin)) {
                e.Handled = true;
                tb.Focus();
            }
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e) {
            ZoomToTexture();
        }
    }
}
