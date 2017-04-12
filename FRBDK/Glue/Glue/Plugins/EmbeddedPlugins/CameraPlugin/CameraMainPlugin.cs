using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.CodeGeneration;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    [Export(typeof(PluginBase))]
    class CameraMainPlugin : EmbeddedPlugin
    {
        CameraSettingsControl control;
        DisplaySettingsViewModel viewModel = new DisplaySettingsViewModel();
            
        public static CameraMainPlugin Self { get; private set; }

        private bool respondToViewModelChanges = true;
            
        public override void StartUp()
        {
            Self = this;

            viewModel.PropertyChanged += HandleDisplaySettingsChanged;

            base.AddMenuItemTo(
                "Camera Settings", HandleCameraSettings, "Settings");

            base.AddToToolBar(new CameraToolbar(), "Standard");
        }

        private void HandleDisplaySettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (respondToViewModelChanges)
            {
                var glueProject = GlueState.Self.CurrentGlueProject;

                if(glueProject != null)
                {
                    glueProject.DisplaySettings = viewModel.ToDisplaySettings();

                    GlueCommands.Self.GluxCommands.SaveGlux();

                    CameraSetupCodeGenerator.UpdateOrAddCameraSetup();

                    CameraSetupCodeGenerator.CallSetupCamera(ProjectManager.GameClassFileName, true);
                }
            }
        }

        private void HandleCameraSettings(object sender, EventArgs e)
        {

            ShowCameraUi();
        }



        public void ShowCameraUi()
        {
            var glueProject = ProjectManager.GlueProjectSave;
            if (glueProject != null)
            {
                bool shouldShowNewUi = glueProject.DisplaySettings != null;

                if(shouldShowNewUi)
                {
                    if(control == null)
                    {
                        control = new CameraSettingsControl();
                        base.AddToTab(PluginManager.LeftTab, control, "Display Settings");
                    }
                    else
                    {
                        base.AddTab();
                    }

                    respondToViewModelChanges = false;
                    {
                        viewModel.SetFrom(glueProject.DisplaySettings);

                        control.DataContext = viewModel;
                    }
                    respondToViewModelChanges = true;
                }

                else
                {
                    CameraSettingsWindow cameraSettingsWindow = new CameraSettingsWindow();
                    cameraSettingsWindow.ShowDialog();
                }
            }
            else
            {
                GlueGui.ShowMessageBox("You must load or create a project first to set the camera settings.");
            }
        }
    }
}
