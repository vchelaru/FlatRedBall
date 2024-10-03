using OfficialPlugins.Wizard.ViewModels;
using System.Windows;

namespace OfficialPlugins.Wizard.Views
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
