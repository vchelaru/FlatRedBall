using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Controls.ProjectSync;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        SyncedProjectsControl control;
        PluginTab tab;
        ToolbarControl toolbarControl;

        public override void StartUp()
        {
            this.AddMenuItemTo("View Projects", HandleSyncedProjectsClick, "Project", preferredIndex:0);

            AddToolbarUi();
        }

        private void AddToolbarUi()
        {
            toolbarControl = new ToolbarControl();
            base.AddToToolBar(toolbarControl, "Standard");
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

            var defaultTab = PluginManager.LeftTab;

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
