using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Math;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for TilesetTileSelectorFull.xaml
    /// </summary>
    public partial class TilesetTileSelectorFull : UserControl
    {
        #region Fields/Properties

        TilesetTileSelectorFullViewModel ViewModel => DataContext as TilesetTileSelectorFullViewModel;

        #endregion

        #region Events

        public event Action OkClicked;
        public event Action CancelClicked;

        #endregion

        public TilesetTileSelectorFull()
        {
            InitializeComponent();

            HighlightRectangle.Visibility = Visibility.Collapsed;

            DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var id = ViewModel.TileId;

            var multipliedId = id * 16;

            var width = (int)PopupImage.ActualWidth;
            if(width == 0)
            {
                width = (int)(GlueState.Self.TiledCache.StandardTilesetImage?.Width ?? 0);
            }
            if(width > 0)
            {
                var x = (multipliedId) % width;
                var y = 16 * (multipliedId) / width;

                SelectionRectangle.Margin = new Thickness(x, y, 0, 0);
            }
        }

        private void PopupImage_MouseMove(object sender, MouseEventArgs e)
        {
            Point relativeSnappedPoint = GetRelativeSnappedPoint(e);
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

        private Point GetRelativeSnappedPoint(MouseEventArgs e)
        {
            var position = e.GetPosition(PopupImage);
            var relativeSnappedPoint = new Point(
                16 * (((int)position.X) / 16),
                16 * (((int)position.Y) / 16));
            return relativeSnappedPoint;
        }

        private void PopupImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point relativeSnappedPoint = GetRelativeSnappedPoint(e);
            if (relativeSnappedPoint.X < PopupImage.ActualWidth && relativeSnappedPoint.Y < PopupImage.ActualHeight)
            {
                SelectionRectangle.Margin = new Thickness(relativeSnappedPoint.X, relativeSnappedPoint.Y, 0, 0);

                var tilesWide = PopupImage.ActualHeight / 16;

                ViewModel.TileId = MathFunctions.RoundToInt( relativeSnappedPoint.X / 16 + tilesWide * relativeSnappedPoint.Y/16);
            }
            if(e.ClickCount == 2)
            {
                OkClicked();
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
