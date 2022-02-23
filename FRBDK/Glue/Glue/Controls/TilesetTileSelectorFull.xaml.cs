using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Math;
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

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for TilesetTileSelectorFull.xaml
    /// </summary>
    public partial class TilesetTileSelectorFull : UserControl
    {
        TilesetTileSelectorFullViewModel ViewModel => DataContext as TilesetTileSelectorFullViewModel;

        public event Action OkClicked;
        public event Action CancelClicked;

        public TilesetTileSelectorFull()
        {
            InitializeComponent();

            HighlightRectangle.Visibility = Visibility.Collapsed;
        }

        private void PopupImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point relativeSnappedPoint = GetRelativeSnapedPoint(e);
            if (relativeSnappedPoint.X < PopupImage.ActualWidth && relativeSnappedPoint.Y < PopupImage.ActualHeight)
            {
                HighlightRectangle.Margin = new Thickness(relativeSnappedPoint.X, relativeSnappedPoint.Y, 0, 0);
                HighlightRectangle.Visibility = Visibility.Visible;
            }
            else
            {
                HighlightRectangle.Visibility = Visibility.Collapsed;
            }
        }

        private Point GetRelativeSnapedPoint(MouseEventArgs e)
        {
            var position = e.GetPosition(PopupImage);
            var relativeSnappedPoint = new Point(
                16 * (((int)position.X) / 16),
                16 * (((int)position.Y) / 16));
            return relativeSnappedPoint;
        }

        private void PopupImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point relativeSnappedPoint = GetRelativeSnapedPoint(e);
            if (relativeSnappedPoint.X < PopupImage.ActualWidth && relativeSnappedPoint.Y < PopupImage.ActualHeight)
            {
                SelectionRectangle.Margin = new Thickness(relativeSnappedPoint.X, relativeSnappedPoint.Y, 0, 0);

                var tilesWide = PopupImage.ActualHeight / 16;

                ViewModel.TileId = MathFunctions.RoundToInt( relativeSnappedPoint.X / 16 + tilesWide * relativeSnappedPoint.Y/16);
            }
        }

        private void PopupImage_MouseLeave(object sender, MouseEventArgs e)
        {
            HighlightRectangle.Visibility = Visibility.Collapsed;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OkClicked();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelClicked();
        }
    }
}
