using System.Windows;
using System.Windows.Controls;

namespace FlatRedBall.Glue.UnreferencedFiles
{
    /// <summary>
    /// Interaction logic for UnreferencedFilesView.xaml
    /// </summary>
    public partial class UnreferencedFilesView : UserControl
    {
        UnreferencedFilesViewModel ViewModel => this.DataContext as UnreferencedFilesViewModel;

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
