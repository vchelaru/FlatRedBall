using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Security.RightsManagement;

namespace FlatRedBall.Glue.Controls.ProjectSync
{
    internal class SyncedProjectsViewModel : ViewModel
    {
        // This ViewModel used to pull the current project
        // from GlueState. The problem with that is projects
        // are not unloaded when the ReactToUnloadedGlux event
        // is raised. Therefore, the plugin needs to manage the
        // project reference rather than always pulling from GlueState

        public ProjectBase CurrentProject { get; set; }
        public IEnumerable<ProjectBase> SyncedProjects { get; set; }

        public IEnumerable<SyncedProjectViewModel> AllProjects
        {
            get
            {
                if(CurrentProject != null)
                {
                    yield return new SyncedProjectViewModel
                    {
                        ProjectBase = GlueState.Self.CurrentMainProject
                    };
                }

                if(SyncedProjects != null)
                {
                    foreach(var item in SyncedProjects)
                    {
                        yield return new SyncedProjectViewModel
                        {
                            ProjectBase = item
                        };
                    }
                }
            }
        }


        public SyncedProjectViewModel SelectedItem
        {
            get => Get<SyncedProjectViewModel>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedItem))]
        public bool IsProjectSelected => SelectedItem != null;

        internal void Refresh()
        {
            this.NotifyPropertyChanged(nameof(AllProjects));
        }
    }
}
