using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            CameraMainPlugin.ShowCameraUi();
        }
    }
}
