using OfficialPlugins.PathPlugin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
    }
}
