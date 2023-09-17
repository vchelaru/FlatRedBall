using OfficialPlugins.FilesPlugin.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPlugins.FilesPlugin.Controls
{
    /// <summary>
    /// Interaction logic for FileReferenceControl_.xaml
    /// </summary>
    public partial class FileReferenceControl : UserControl
    {
        public FileReferenceControl()
        {
            InitializeComponent();
        }

        private void RefreshButtonClicked(object sender, RoutedEventArgs e)
        {
            (DataContext as FileReferenceViewModel).Refresh();
        }
    }
}
