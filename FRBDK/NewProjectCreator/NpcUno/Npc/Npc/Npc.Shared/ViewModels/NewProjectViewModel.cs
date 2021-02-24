using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Npc.ViewModels
{
    public class NewProjectViewModel : ViewModel
    {
        public bool OpenSlnFolderAfterCreation
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ProjectLocation
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ProjectName
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsDifferentNamespaceChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string DifferentNamespace
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsCreateProjectDirectoryChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsDifferentNamespaceChecked))]
        public Visibility DifferentNamespaceTextBoxVisibility
        {
            get => IsDifferentNamespaceChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        [DependsOn(nameof(ProjectLocation))]
        [DependsOn(nameof(ProjectName))]
        string CombinedProjectDirectory
        {
            get
            {
                if (!ProjectLocation.EndsWith("\\") && !ProjectLocation.EndsWith("/"))
                {
                    return ProjectLocation + "\\" + ProjectName;
                }
                else
                {
                    return ProjectLocation + ProjectName;

                }
            }
        }

        [DependsOn(nameof(IsCreateProjectDirectoryChecked))]
        [DependsOn(nameof(CombinedProjectDirectory))]
        [DependsOn(nameof(ProjectLocation))]
        public string FinalDirectory
        {
            get
            {
                if(IsCreateProjectDirectoryChecked)
                {
                    return CombinedProjectDirectory;
                }
                else
                {
                    return ProjectLocation;
                }
            }
        }

        public NewProjectViewModel()
        {
            ProjectName = "MyProject";
        }
    }
}
