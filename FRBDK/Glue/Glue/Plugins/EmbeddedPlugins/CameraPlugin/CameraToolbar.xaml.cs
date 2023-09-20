using System.Windows;
using System.Windows.Controls;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    /// <summary>
    /// Interaction logic for CameraToolbar.xaml
    /// </summary>
    public partial class CameraToolbar : UserControl
    {
        public CameraToolbar()
        {
            InitializeComponent();
        }

        private void HandleButtonClick(object sender, RoutedEventArgs e)
        {
            CameraMainPlugin.Self.ShowCameraUi();
        }
    }
}
