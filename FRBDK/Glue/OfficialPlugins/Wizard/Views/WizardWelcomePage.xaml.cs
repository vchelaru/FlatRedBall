using System;
using System.Windows;
using System.Windows.Controls;

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
