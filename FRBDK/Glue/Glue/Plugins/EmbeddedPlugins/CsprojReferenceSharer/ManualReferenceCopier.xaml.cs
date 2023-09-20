using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CsprojReferenceSharer
{
    /// <summary>
    /// Interaction logic for ManualReferenceCopier.xaml
    /// </summary>
    public partial class ManualReferenceCopier : UserControl
    {
        ReferenceCopierViewModel ViewModel => this.DataContext as ReferenceCopierViewModel;

        public ManualReferenceCopier()
        {
            InitializeComponent();
        }


        private void FromButtonClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                DefaultExt = ".csproj",
                Filter = L.Texts.CSharpProjectFiles + "|*.csproj"
            };

            var result = fileDialog.ShowDialog();

            if (result == true)
            {
                ViewModel.FromFile = fileDialog.FileName;
            }
        }

        private void ToButtonClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog
            {
                DefaultExt = ".csproj",
                Filter = L.Texts.CSharpProjectFiles + "|*.csproj"
            };


            var result = fileDialog.ShowDialog();

            if (result == true)
            {
                ViewModel.ToFile = fileDialog.FileName;
            }
        }

        private void HandleDoItClick(object sender, RoutedEventArgs e)
        {
            ViewModel.PerformCopy();
        }
    }
}
