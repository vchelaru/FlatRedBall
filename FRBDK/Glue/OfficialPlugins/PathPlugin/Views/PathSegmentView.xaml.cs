using OfficialPlugins.PathPlugin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WpfDataUi.Controls;

namespace OfficialPlugins.PathPlugin.Views
{
    /// <summary>
    /// Interaction logic for PathSegmentView.xaml
    /// </summary>
    public partial class PathSegmentView : UserControl
    {
        PathSegmentViewModel ViewModel => DataContext as PathSegmentViewModel;

        public PathSegmentView()
        {
            InitializeComponent();

            DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(ViewModel != null)
            {
                ViewModel.TextBoxFocusRequested += ViewModel_TextBoxFocusRequested;
            }
        }

        private void ViewModel_TextBoxFocusRequested(int obj)
        {
            XTextBox.Focus();
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.HandleCloseClicked();
        }

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }

        private void MoveUpClick(object sender, RoutedEventArgs e)
        {
            ViewModel.HandleMoveUpClicked();
        }

        private void MoveDownClick(object sender, RoutedEventArgs e)
        {
            ViewModel.HandleMoveDownClicked();
        }

        private void CopyClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.HandleCopyClicked();
        }

        private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sign = Math.Sign( e.Delta);
            if(Equals(sender, XTextBox))
            {
                ViewModel.X += 5 * sign;
                e.Handled = true;
            }
            else if ((Equals(sender, YTextBox)))
            {
                ViewModel.Y += 5 * sign;

                e.Handled = true;
            }
            else if(Equals(sender, AngleTextBox))
            {
                ViewModel.Angle += 5 * sign;

                e.Handled = true;
            }


        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ViewModel?.HandleTextBoxFocus();
        }

        #region Label Dragging

        double? dragStartX;
        double dragUnroundedValue;
        TextBlock draggingLabel;

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null) return;

            draggingLabel = (TextBlock)sender;
            dragStartX = e.GetPosition(this).X;
            dragUnroundedValue = GetDraggedValue(draggingLabel);

            Mouse.Capture(draggingLabel);
        }

        private void Label_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragStartX == null || draggingLabel == null) return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                if (Mouse.Captured == draggingLabel)
                {
                    Mouse.Capture(null);
                }
                return;
            }

            var newX = e.GetPosition(this).X;
            var pixelDifference = newX - dragStartX.Value;
            dragStartX = newX;

            if (pixelDifference == 0) return;

            dragUnroundedValue += pixelDifference;
            var newValue = SnapDraggedValue(dragUnroundedValue);
            SetDraggedValue(draggingLabel, newValue);
        }

        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {
            draggingLabel = null;
            dragStartX = null;
            if (Mouse.Captured == sender)
            {
                Mouse.Capture(null);
            }
        }

        double GetDraggedValue(TextBlock label)
        {
            if (Equals(label, XLabel)) return ViewModel.X;
            if (Equals(label, YLabel)) return ViewModel.Y;
            return ViewModel.Angle;
        }

        void SetDraggedValue(TextBlock label, float value)
        {
            if (Equals(label, XLabel)) ViewModel.X = value;
            else if (Equals(label, YLabel)) ViewModel.Y = value;
            else ViewModel.Angle = value;
        }

        // Reuses WpfDataUi's own label-drag snapping so this matches the drag feel of every other
        // numeric field in Glue (1px = 1 unit, snapped to whole numbers) rather than a hand-rolled
        // approximation of it. Internal (not private) so GlueUnitTests can pin the snapping behavior
        // without driving real WPF mouse events.
        internal static float SnapDraggedValue(double unroundedValue) =>
            (float)TextBoxDisplayLogic.SnapDraggedValue(unroundedValue, rounding: 1);

        #endregion
    }
}
