using System.Windows.Controls;

namespace OfficialPlugins.ErrorPlugin.Views
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : UserControl
    {
        public ErrorWindow()
        {
            InitializeComponent();
        }

        public void ForceRefreshErrors()
        {
            ListBox.Items.Refresh();
        }
    }
}
