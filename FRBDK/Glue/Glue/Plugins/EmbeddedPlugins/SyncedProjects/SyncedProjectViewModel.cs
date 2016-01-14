using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System.Windows;

namespace FlatRedBall.Glue.Controls.ProjectSync
{
    internal class SyncedProjectViewModel : ViewModel
    {
        ProjectBase projectBase;
        public ProjectBase ProjectBase
        {
            get
            {
                return projectBase;
            }
            set
            {
                projectBase = value;

                RefreshOrphans();
                RefreshGeneralErrors();
            }
        }

        public string Name
        {
            get
            {
                return ProjectBase.Name;
            }
        }

        public Visibility XamarinButtonVisibility
        {
            get
            {
                if(projectBase is AndroidProject)
                {
                    return Visibility.Visible;
                }
                else
                {
                    // so columns are the same size
                    return Visibility.Hidden;
                }
            }
        }

        public ObservableCollection<BuildItemViewModel> Orphans
        {
            get;
            private set;
        }

        public ObservableCollection<string> GeneralErrors
        {
            get;
            private set;
        }

        public Visibility ErrorVisibility
        {
            get
            {
                if(GeneralErrors.Count != 0)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Hidden;
                }
            }
        }

        public SyncedProjectViewModel()
        {
            Orphans = new ObservableCollection<BuildItemViewModel>();
            GeneralErrors = new ObservableCollection<string>();
        }

        public void RefreshOrphans()
        {
            Orphans.Clear();

            foreach(var item in ProjectBase)
            {
                if(BuildItemViewModel.IsOrphaned(item, ProjectBase))
                {
                    Orphans.Add(new BuildItemViewModel
                    {
                        BuildItem = item,
                        Owner = ProjectBase
                    });
                }
            }

            if(ProjectBase.ContentProject != ProjectBase && ProjectBase.ContentProject != null)
            {
                foreach (var item in ProjectBase.ContentProject)
                {
                    if (BuildItemViewModel.IsOrphaned(item, ProjectBase))
                    {
                        Orphans.Add(new BuildItemViewModel
                        {
                            BuildItem = item,
                            Owner = ProjectBase
                        });
                    }
                }
            }
        }

        public void RefreshGeneralErrors()
        {
            GeneralErrors.Clear();

            var errorList = ProjectBase.GetErrors();

            foreach (var error in errorList)
            {
                GeneralErrors.Add(error);
            }
        }

        public string Display
        {
            get
            {
                if(projectBase.SaveAsRelativeSyncedProject)
                {
                    return FileManager.MakeRelative(projectBase.FullFileName);
                }
                else
                {
                    return projectBase.FullFileName;
                }
            }
        }

        public void SaveProject()
        {
            projectBase.Save();
        }

        public override string ToString()
        {
            return Display;
        }

    }
}
