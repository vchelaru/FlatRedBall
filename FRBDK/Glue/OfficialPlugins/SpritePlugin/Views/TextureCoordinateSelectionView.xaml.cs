using FlatRedBall.IO;
using OfficialPlugins.SpritePlugin.Managers;
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
        SpriteRuntime MainSprite;
        public TextureCoordinateSelectionView()
        {
            InitializeComponent();
            CameraLogic.Initialize(this);
            //var rectangle = new RoundedRectangleRuntime();
            //rectangle.Width = 100;
            //rectangle.Height = 150;
            //rectangle.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            //rectangle.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            //rectangle.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            //rectangle.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            //rectangle.Color = SKColors.Purple;
            //rectangle.CornerRadius = 10;
            //this.Canvas.Children.Add(rectangle);

            MainSprite = new SpriteRuntime();
            MainSprite.Width = 100;
            MainSprite.Height = 100;
            MainSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            MainSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;

            MainSprite.X = 20;
            MainSprite.Y = 20;

            this.Canvas.Children.Add(MainSprite);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CameraLogic.HandleMousePush(e);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            CameraLogic.HandleMouseMove(e);
        }
    }
}
