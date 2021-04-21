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
    /// Interaction logic for WizardWelcomePage.xaml
    /// </summary>
    public partial class WizardWelcomePage : UserControl
    {
        public event Action StartWithConfiguration;
        public event Action StartFromScratch;
        public WizardWelcomePage()
        {
            InitializeComponent();
        }

        private void StartWithConfigurationClicked(object sender, RoutedEventArgs e)
        {
            StartWithConfiguration();
        }

        private void StartFromScratchClicked(object sender, RoutedEventArgs e)
        {
            StartFromScratch();
        }
    }
}
