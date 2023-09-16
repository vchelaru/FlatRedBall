using GameCommunicationPlugin.GlueControl.Managers;
using System.Windows;
using System.Windows.Controls;

namespace GameCommunicationPlugin.GlueControl.Views
{
    /// <summary>
    /// Interaction logic for ProfilingControl.xaml
    /// </summary>
    public partial class ProfilingControl : UserControl
    {
        public ProfilingControl()
        {
            InitializeComponent();
        }

        private void InvokeSnapShot(object sender, RoutedEventArgs e)
        {
            _ = ProfilingManager.Self.RefreshProfilingData();
        }
    }
}
