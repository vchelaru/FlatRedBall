using System.Windows;
using System.Windows.Controls;
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
            fileDialog.Filter = Localization.Texts.MobilePackage + " (.apk, .ipa)|*.apk;*.ipa";

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
