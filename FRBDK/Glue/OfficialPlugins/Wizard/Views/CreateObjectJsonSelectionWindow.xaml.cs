using OfficialPluginsCore.Wizard.ViewModels;
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
using System.Windows.Shapes;

namespace OfficialPluginsCore.Wizard.Views
{
    /// <summary>
    /// Interaction logic for CreateObjectJsonSelectionWindow.xaml
    /// </summary>
    public partial class CreateObjectJsonSelectionWindow : Window
    {
        CreateObjectJsonViewModel ViewModel => DataContext as CreateObjectJsonViewModel;
        public CreateObjectJsonSelectionWindow()
        {
            InitializeComponent();
        }

        private void CopyJsonToClipboardClicked(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ViewModel.GeneratedJson);
        }
    }
}
