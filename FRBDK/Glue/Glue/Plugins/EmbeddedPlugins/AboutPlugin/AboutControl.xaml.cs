using GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin;
using System.Windows;
using System.Windows.Controls;

namespace GlueFormsCore.Controls
{
    /// <summary>
    /// Interaction logic for AboutControl.xaml
    /// </summary>
    public partial class AboutControl : UserControl
    {
        AboutViewModel ViewModel => DataContext as AboutViewModel;
        public AboutControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DoInstallUpdate();
        }
    }
}
