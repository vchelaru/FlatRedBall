using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Controls.ProjectSync
{
    internal class SyncedProjectsViewModel : ViewModel
    {
        public IEnumerable<SyncedProjectViewModel> AllProjects
        {
            get
            {
                if(GlueState.Self.CurrentMainProject != null)
                {
                    yield return new SyncedProjectViewModel
                    {
                        ProjectBase = GlueState.Self.CurrentMainProject
                    };
                }
                foreach(var item in ProjectManager.SyncedProjects)
                {
                    yield return new SyncedProjectViewModel
                    {
                        ProjectBase = item
                    };
                }
            }
        }

        public SyncedProjectViewModel SelectedItem
        {
            get;
            set;
        }


        internal void Refresh()
        {
            this.NotifyPropertyChanged(nameof(AllProjects));
        }
    }
}
