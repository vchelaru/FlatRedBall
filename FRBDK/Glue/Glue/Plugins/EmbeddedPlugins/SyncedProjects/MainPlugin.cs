using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using FlatRedBall.Glue.Controls.ProjectSync;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueFormsCore.Plugins.EmbeddedPlugins.SyncedProjects.ViewModels;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        SyncedProjectsControl control;
        PluginTab tab;
        ToolbarControl toolbarControl;
        ToolbarControlViewModel toolbarControlViewModel;

        public override void StartUp()
        {
            this.AddMenuItemTo(L.Texts.ProjectView, L.MenuIds.ProjectViewId, HandleSyncedProjectsClick, L.MenuIds.ProjectId, preferredIndex:0);

            this.ReactToLoadedGlux += HandleGluxLoad;
            this.ReactToUnloadedGlux += HandleGluxUnload;

            AddToolbarUi();
        }

        private void HandleGluxUnload()
        {
            toolbarControlViewModel.HasProjectLoaded = false;
        }

        private void HandleGluxLoad()
        {
            var glueProject = GlueState.Self.CurrentGlueProject;

            var openAutomaticallyProperty = glueProject.Properties
                        .FirstOrDefault(item => item.Name == nameof(toolbarControlViewModel.IsOpenVisualStudioAutomaticallyChecked));

            var value = openAutomaticallyProperty?.Value as bool? == true;
            toolbarControlViewModel.IsOpenVisualStudioAutomaticallyChecked = value;
            toolbarControlViewModel.HasProjectLoaded = true;

            if (value)
            {
                ProjectListEntry.OpenInVisualStudio(
                    GlueState.Self.CurrentMainProject);
            }
        }

        private void AddToolbarUi()
        {
            toolbarControlViewModel = new ToolbarControlViewModel();

            var glueProject = GlueState.Self.CurrentGlueProject;

            toolbarControlViewModel.PropertyChanged += HandleToolbarPropertyChanged;

            toolbarControl = new ToolbarControl();
            toolbarControl.DataContext = toolbarControlViewModel;

            base.AddToToolBar(toolbarControl, "Standard");
        }

        private void HandleToolbarPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(toolbarControlViewModel.IsOpenVisualStudioAutomaticallyChecked):
                    var glueProject = GlueState.Self.CurrentGlueProject;
                    var openAutomaticallyProperty = glueProject.Properties
                        .FirstOrDefault(item => item.Name == nameof(toolbarControlViewModel.IsOpenVisualStudioAutomaticallyChecked));
                    if(openAutomaticallyProperty == null)
                    {
                        openAutomaticallyProperty = new PropertySave();
                        openAutomaticallyProperty.Name = nameof(toolbarControlViewModel.IsOpenVisualStudioAutomaticallyChecked);
                        glueProject.Properties.Add(openAutomaticallyProperty);
                    }
                    openAutomaticallyProperty.Value = toolbarControlViewModel.IsOpenVisualStudioAutomaticallyChecked;
                    GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                    break;
            }
        }

        private void RemoveToolbarUi()
        {
            if(toolbarControl != null)
            {
                base.RemoveFromToolbar(toolbarControl, "Standard");
            }
        }

        private void AddControl()
        {
            SyncedProjectsViewModel viewModel = new SyncedProjectsViewModel();

            viewModel.CurrentProject = GlueState.Self.CurrentMainProject;
            viewModel.SyncedProjects = GlueState.Self.SyncedProjects;

            control = new SyncedProjectsControl();
            control.DataContext = viewModel;

            tab = CreateAndAddTab(control, "Projects", TabLocation.Left);
            this.ReactToLoadedGlux += delegate
            {
                viewModel.CurrentProject = GlueState.Self.CurrentMainProject;
                viewModel.SyncedProjects = GlueState.Self.SyncedProjects;

                viewModel.Refresh();
            };
            this.ReactToLoadedSyncedProject += delegate
            {
                viewModel.CurrentProject = GlueState.Self.CurrentMainProject;
                viewModel.SyncedProjects = GlueState.Self.SyncedProjects;


                viewModel.Refresh();
            };
            this.ReactToUnloadedGlux += delegate
            {
                viewModel.CurrentProject = null;
                viewModel.SyncedProjects = new List<ProjectBase>() ;


                viewModel.Refresh();
            };
        }

        private void HandleSyncedProjectsClick(object sender, EventArgs e)
        {
            if (control == null)
            {
                AddControl();
            }
            tab.Show();
            tab.Focus();
        }
    }
}
