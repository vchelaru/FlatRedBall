using FlatRedBall;
using GlueView.Facades;
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

namespace GlueView.EmbeddedPlugins.CameraControlsPlugin.Controls
{
    /// <summary>
    /// Interaction logic for GuidesControl.xaml
    /// </summary>
    public partial class GuidesControl : UserControl
    {
        public GuidesControl()
        {
            InitializeComponent();
        }

        private void ResetCameraButton_Click(object sender, RoutedEventArgs e)
        {
            // todo - apply settings from Glue
            var glueProject = GlueViewState.Self.CurrentGlueProject;

            if(glueProject.DisplaySettings != null)
            {
                var settings = glueProject.DisplaySettings;

                if(settings.Is2D)
                {
                    Camera.Main.UsePixelCoordinates();

                }
                else
                {
                    Camera.Main.Orthogonal = false;
                    Camera.Main.UsePixelCoordinates3D(0);
                }
            }
            else
            {
                Camera.Main.UsePixelCoordinates();
            }
        }
    }
}
