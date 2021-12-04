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

namespace OfficialPlugins.Wizard.Views
{
    /// <summary>
    /// Interaction logic for NewWizardWelcomePage.xaml
    /// </summary>
    public partial class NewWizardWelcomePage : UserControl
    {
        public event Action PlatformerClicked;
        public event Action TopDownClicked;
        public event Action CustomClicked;
        public event Action FormsClicked;
        public event Action JsonConfigurationClicked;

        public NewWizardWelcomePage()
        {
            InitializeComponent();
        }

        private void PlatformerButtonClicked(object sender, RoutedEventArgs e) => PlatformerClicked();

        private void FormsButtonClicked(object sender, RoutedEventArgs e) => FormsClicked();

        private void TopDownButtonClicked(object sender, RoutedEventArgs e) => TopDownClicked();

        private void CustomButtonClicked(object sender, RoutedEventArgs e) => CustomClicked();

        private void FromJsonButtonClicked(object sender, RoutedEventArgs e) => JsonConfigurationClicked();
    }
}
