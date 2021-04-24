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
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            (this.DataContext as ViewModels.ElementImportItemViewModel).Url = null;
        }
    }
}
