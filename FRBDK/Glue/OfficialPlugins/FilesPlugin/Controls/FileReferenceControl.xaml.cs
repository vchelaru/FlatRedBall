using OfficialPlugins.FilesPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
