using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
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
using System.Windows.Shapes;

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

            this.Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.DialogCommands.MoveToCursor(this);
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
