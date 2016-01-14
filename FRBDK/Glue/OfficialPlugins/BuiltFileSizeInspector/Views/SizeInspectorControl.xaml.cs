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
using Microsoft.Win32;
using OfficialPlugins.BuiltFileSizeInspector.ViewModels;

namespace OfficialPlugins.BuiltFileSizeInspector.Views
{
    /// <summary>
    /// Interaction logic for SizeInspectorControl.xaml
    /// </summary>
    public partial class SizeInspectorControl : UserControl
    {
        public SizeInspectorControl()
        {
            InitializeComponent();
        }

        private void HandleLoadFileButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Mobile Package (.apk, .ipa)|*.apk;*.ipa";

            var result = fileDialog.ShowDialog();

            if(result == true)
            {
                var file = fileDialog.FileName;

                BuiltFileSizeViewModel viewModel = new BuiltFileSizeViewModel();
                viewModel.SetFromFile(file);

                this.DataContext = viewModel;
            }
        }
    }
}
