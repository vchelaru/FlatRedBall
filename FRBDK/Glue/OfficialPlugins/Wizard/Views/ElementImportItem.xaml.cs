using System.Windows;
using System.Windows.Controls;

namespace OfficialPluginsCore.Wizard.Views
{
    /// <summary>
    /// Interaction logic for ElementImportItem.xaml
    /// </summary>
    public partial class ElementImportItem : UserControl
    {
        public ElementImportItem()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as ViewModels.ElementImportItemViewModel).UrlOrLocalFile = null;
        }
    }
}
