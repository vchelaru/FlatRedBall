using FlatRedBall.IO;
using SkiaSharp;
using System.Windows;

namespace OfficialPlugins.SpritePlugin.Views
{
    /// <summary>
    /// Interaction logic for TextureCoordinateSelectionWindow.xaml
    /// </summary>
    public partial class TextureCoordinateSelectionWindow : Window
    {
        public FilePath TextureFilePath
        {
            get => InnerView.TextureFilePath;
            set => InnerView.TextureFilePath = value;
        }
        public SKBitmap Texture => InnerView.Texture;

        public TextureCoordinateSelectionWindow()
        {
            InitializeComponent();

            InnerView.Initialize(new Managers.CameraLogic());
            // Intentionally do not move the window to the cursor. Users place this where they want it.
            //this.Loaded += HandleLoaded;
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InnerView.ZoomToTexture();
        }

    }
}
