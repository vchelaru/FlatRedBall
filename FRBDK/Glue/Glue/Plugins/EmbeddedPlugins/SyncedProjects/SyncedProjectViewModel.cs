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

        public string DisplayName
        {
            get
            {
                if(ProjectBase != null)
                {
                    return $"{ProjectBase.Name} - {ProjectBase.ProjectId}";
                }
                else
                {
                    return "";
                }
            }
        }


        /// <summary>
        /// An ObservableCollection of orphaned files - files which are referenced by the 
        /// ProjectBase which do not exist on disk.
        /// </summary>
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

        /// <summary>
        /// Refreshes the Orphans list, which is a list of files referenced by the ProjectBase which do not exist on disk.
        /// </summary>
        public void RefreshOrphans()
        {
            Orphans.Clear();

            if(ProjectBase != null)
            {
                foreach(var item in ((VisualStudioProject)ProjectBase).EvaluatedItems)
                {
                    if(BuildItemViewModel.IsOrphaned(item, ProjectBase))
                    {
                        Orphans.Add(new BuildItemViewModel
                        {
                            BuildItem = item,
                            Owner = (VisualStudioProject) ProjectBase
                        });
                    }
                }

                if(ProjectBase.ContentProject != ProjectBase && ProjectBase.ContentProject != null)
                {
                    foreach (var item in ((VisualStudioProject)ProjectBase.ContentProject).EvaluatedItems)
                    {
                        if (BuildItemViewModel.IsOrphaned(item, ProjectBase))
                        {
                            Orphans.Add(new BuildItemViewModel
                            {
                                BuildItem = item,
                                Owner = (VisualStudioProject)ProjectBase
                            });
                        }
                    }
                }
            }
        }

        public void RefreshGeneralErrors()
        {
            GeneralErrors.Clear();

            if(ProjectBase != null)
            {
                var errorList = ProjectBase.GetErrors();

                foreach (var error in errorList)
                {
                    GeneralErrors.Add(error);
                }
            }
        }

        public string Display
        {
            get
            {
                if(projectBase.SaveAsRelativeSyncedProject)
                {
                    return FileManager.MakeRelative(projectBase.FullFileName.FullPath);
                }
                else
                {
                    return projectBase.FullFileName.FullPath;
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
