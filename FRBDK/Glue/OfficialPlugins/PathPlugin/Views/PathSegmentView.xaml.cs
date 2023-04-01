using OfficialPlugins.PathPlugin.ViewModels;
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
            if(sender == XTextBox)
            {
                ViewModel.X += 5 * sign;
            }
            else if(sender == YTextBox)
            {
                ViewModel.Y += 5 * sign;

            }
            else if(sender == AngleTextBox)
            {
                ViewModel.Angle += 5 * sign;

            }

        }
    }
}
