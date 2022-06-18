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
        }
    }
}
