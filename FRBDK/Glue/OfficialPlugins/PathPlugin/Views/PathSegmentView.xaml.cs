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
    }
}
