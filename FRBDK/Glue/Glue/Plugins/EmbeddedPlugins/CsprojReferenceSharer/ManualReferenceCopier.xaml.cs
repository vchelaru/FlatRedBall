using Microsoft.Win32;
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

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CsprojReferenceSharer
{
    /// <summary>
    /// Interaction logic for ManualReferenceCopier.xaml
    /// </summary>
    public partial class ManualReferenceCopier : UserControl
    {
        ReferenceCopierViewModel ViewModel
        {
            get
            {
                return this.DataContext as ReferenceCopierViewModel;
            }
        }

        public ManualReferenceCopier()
        {
            InitializeComponent();
        }


        private void FromButtonClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".csproj";
            fileDialog.Filter = "C# Project Files|*.csproj";

            var result = fileDialog.ShowDialog();

            if (result == true)
            {
                ViewModel.FromFile = fileDialog.FileName;
            }
        }

        private void ToButtonClicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();

            fileDialog.DefaultExt = ".csproj";
            fileDialog.Filter = "C# Project Files|*.csproj";


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
