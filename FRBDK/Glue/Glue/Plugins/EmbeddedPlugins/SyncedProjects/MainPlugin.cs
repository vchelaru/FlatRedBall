using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Controls.ProjectSync;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects.Controls;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        SyncedProjectsControl control;

        public override void StartUp()
        {
            this.AddMenuItemTo("View Projects", HandleSyncedProjectsClick, "Project", preferredIndex:0);

            AddToolbarUi();
        }

        private void AddToolbarUi()
        {
            var control = new ToolbarControl();
            base.AddToToolBar(control, "Standard");
        }

        private void AddControl()
        {
            SyncedProjectsViewModel viewModel = new SyncedProjectsViewModel();
            var control = new SyncedProjectsControl();
            control.DataContext = viewModel;

            var defaultTab = PluginManager.LeftTab;

            this.AddToTab(defaultTab, control, "Projects");
            this.ReactToLoadedGlux += delegate
            {
                viewModel.Refresh();
            };
            this.ReactToLoadedSyncedProject += delegate
            {
                viewModel.Refresh();
            };
        }

        private void HandleSyncedProjectsClick(object sender, EventArgs e)
        {
            if (control == null)
            {
                AddControl();
            }
            else
            {
                AddTab();
            }

            FocusTab();

        }
    }
}
