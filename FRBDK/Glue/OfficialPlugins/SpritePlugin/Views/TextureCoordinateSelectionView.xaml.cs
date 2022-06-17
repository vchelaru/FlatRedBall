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

namespace OfficialPlugins.SpritePlugin.Views
{
    /// <summary>
    /// Interaction logic for TextureCoordinateSelectionView.xaml
    /// </summary>
    public partial class TextureCoordinateSelectionView : UserControl
    {
        public TextureCoordinateSelectionView()
        {
            InitializeComponent();

            var rectangle = new RoundedRectangleRuntime();
            rectangle.Width = 100;
            rectangle.Height = 150;
            rectangle.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            //rectangle.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            rectangle.Color = SKColors.Purple;
            rectangle.CornerRadius = 10;
            this.Canvas.Children.Add(rectangle);
        }
    }
}
