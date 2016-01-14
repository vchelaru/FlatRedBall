using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    [Export(typeof(PluginBase))]
    class CameraMainPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            base.AddMenuItemTo(
                "Camera Settings", HandleCameraSettings, "Settings");

            base.AddToToolBar(new CameraToolbar(), "Standard");
        }

        private void HandleCameraSettings(object sender, EventArgs e)
        {
            ShowCameraUi();
        }

        public static void ShowCameraUi()
        {
            if (ProjectManager.GlueProjectSave != null)
            {
                CameraSettingsWindow cameraSettingsWindow = new CameraSettingsWindow();
                cameraSettingsWindow.ShowDialog();
            }
            else
            {
                GlueGui.ShowMessageBox("You must load or create a project first to set the camera settings.");
            }
        }
    }
}
