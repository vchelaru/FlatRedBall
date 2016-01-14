using System;
using System.Collections.Generic;
using System.Linq;
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

namespace FlatRedBall.Glue.UnreferencedFiles
{
    /// <summary>
    /// Interaction logic for UnreferencedFilesView.xaml
    /// </summary>
    public partial class UnreferencedFilesView : UserControl
    {
        UnreferencedFilesViewModel ViewModel
        {
            get
            {
                return this.DataContext as UnreferencedFilesViewModel;
            }
        }

        public UnreferencedFilesView()
        {
            InitializeComponent();
        }

        private void HandleRemoveSelectedClick(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveSelectedReference();
        }

        private void HandleRemoveAllClick(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveAllReferences();
        }

        private void HandleRefreshClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Refresh();
        }

        private void HandleRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            ViewModel.Refresh();
        }

        private void ViewInExplorerClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ViewInExplorer();
        }
    }
}
