using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.ComponentModel;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using Microsoft.Xna.Framework.Graphics;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    [Export(typeof(PluginBase))]
    class CameraMainPlugin : EmbeddedPlugin
    {
        CameraSettingsControl control;
        DisplaySettingsViewModel viewModel = new DisplaySettingsViewModel();
        PluginTab tab;
            
        public static CameraMainPlugin Self { get; private set; }

        private bool respondToViewModelChanges = true;
            
        public override void StartUp()
        {
            Self = this;

            viewModel.PropertyChanged += HandleDisplaySettingsChanged;

            this.ReactToLoadedGlux += HandleLoadedGlux;

            base.AddMenuItemTo(L.Texts.CameraSettings, L.MenuIds.CameraSettingsId, HandleCameraSettings, L.MenuIds.SettingsId);

            base.AddToToolBar(new CameraToolbar(), "Standard");
        }

        private void HandleLoadedGlux()
        {
            // When the project loads, immediately set the ATI so 
            // that Glue behaves properly
            if(GlueState.Self.CurrentGlueProject?.DisplaySettings != null)
            {
                respondToViewModelChanges = false;
                {
                    viewModel.SetFrom(GlueState.Self.CurrentGlueProject.DisplaySettings);
                }
                respondToViewModelChanges = true;

                CameraAtiUpdateLogic.UpdateAtiTo(viewModel);

                CameraSetupCodeGenerator.UpdateOrAddCameraSetup();
            }
        }

        private void HandleDisplaySettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (respondToViewModelChanges)
            {

                var glueProject = GlueState.Self.CurrentGlueProject;

                if(glueProject != null)
                {
                    glueProject.DisplaySettings = viewModel.ToDisplaySettings();

                    // This should only modify gluj, this will be faster.

                    //GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                    GlueCommands.Self.GluxCommands.SaveGlujFile(Managers.TaskExecutionPreference.AddOrMoveToEnd);

                    if(CameraSetupCodeGenerator.ShouldGenerateCodeWhenPropertyChanged(e.PropertyName))
                    {
                        CameraSetupCodeGenerator.UpdateOrAddCameraSetup();
                    }

                    CameraSetupCodeGenerator.GenerateCallInGame1(ProjectManager.GameClassFileName, true);

                    CameraAtiUpdateLogic.UpdateAtiTo(viewModel);
                }

                var propertyName = e.PropertyName;
                if(propertyName == nameof(viewModel.ResolutionWidth) ||
                    propertyName == nameof(viewModel.ResolutionHeight))
                {
                    PluginManager.ReactToResolutionChanged();
                }
            }
        } 

        private void HandleCameraSettings(object sender, EventArgs e)
        {

            ShowCameraUi();
        }

        public static void CreateGlueProjectSettingsFor(GlueProjectSave project)
        {
            DisplaySettings settings = new DisplaySettings();

            settings.SetDefaults();

            settings.TextureFilter = (int)TextureFilter.Point;
            settings.AllowWindowResizing = false;
            settings.AspectRatioHeight = 9;
            settings.AspectRatioWidth = 16;

            settings.Is2D = project.In2D;

            settings.ResizeBehavior = ResizeBehavior.StretchVisibleArea;
            settings.ResizeBehaviorGum = ResizeBehavior.StretchVisibleArea;

            settings.ResolutionWidth = project.ResolutionWidth;
            settings.ResolutionHeight = project.ResolutionHeight;

            settings.RunInFullScreen = false;
            settings.Scale = 100;
            settings.ScaleGum = 100;
            settings.SupportLandscape = true;
            settings.SupportPortrait = false;

            project.DisplaySettings = settings;
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
                        tab = base.CreateTab(control, "Display Settings");
                    }
                    tab.Show();
                    tab.Focus();

                    respondToViewModelChanges = false;
                    {
                        viewModel.SetFrom(glueProject.DisplaySettings);
                        foreach(var setting in glueProject.AllDisplaySettings)
                        {
                            viewModel.AvailableOptions.Add(setting);
                        }

                        bool showSupportedOrientationLink =
                            GetIfSupportsOrientation(GlueState.Self.CurrentMainProject) ||
                            GlueState.Self.SyncedProjects.Any(item => GetIfSupportsOrientation(item));

                        if(showSupportedOrientationLink)
                        {
                            viewModel.SupportedOrientationsLinkVisibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            viewModel.SupportedOrientationsLinkVisibility = System.Windows.Visibility.Collapsed;
                        }

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

        bool GetIfSupportsOrientation(ProjectBase projectBase)
        {
            return projectBase is IosMonogameProject ||
                projectBase is AndroidProject ||
                projectBase is UwpProject;
        }
    }
}
