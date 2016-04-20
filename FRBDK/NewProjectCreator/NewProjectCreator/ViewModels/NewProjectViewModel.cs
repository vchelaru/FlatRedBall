using NewProjectCreator.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NewProjectCreator.ViewModels
{
    public class NewProjectViewModel : INotifyPropertyChanged
    {
        #region Fields

        char[] invalidNamespaceCharacters = new char[] 
            { 
                '~', '`', '!', '@', '#', '$', '%', '^', '&', '*', 
                '(', ')', '-', '=', '+', ';', '\'', ':', '"', '<', 
                ',', '>', '.', '/', '\\', '?', '[', '{', ']', '}', 
                '|', 
                // Spaces are handled separately
            //    ' ' 
            };

        TemplateCategoryViewModel starterCategory;
        TemplateCategoryViewModel emptyProjectsCategory;

        #endregion

        public bool OpenSlnFolderAfterCreation { get; set; }

        public string ProjectName { get; set; }

        public bool UseDifferentNamespace { get; set; }

        public string DifferentNamespace { get; set; }

        public bool CheckForNewVersions { get; set; }

        public PlatformProjectInfo ProjectType
        {
            get
            {
                if(SelectedTemplate == null)
                {
                    return null;
                }
                else
                {
                    return SelectedTemplate.BackingData;
                }
            }
        }

        public string ProjectLocation { get; set; }

        public bool CreateProjectDirectory { get; set; }

        ObservableCollection<TemplateCategoryViewModel> allCategories = new ObservableCollection<TemplateCategoryViewModel>();
        ObservableCollection<TemplateCategoryViewModel> filteredCategories = new ObservableCollection<TemplateCategoryViewModel>();

        public ObservableCollection<TemplateCategoryViewModel> Categories
        {
            get { return filteredCategories; }
        }

        TemplateCategoryViewModel selectedCategory;
        public TemplateCategoryViewModel SelectedCategory
        {
            get { return selectedCategory; }
            set
            {
                selectedCategory = value;

                NotifyPropertyChanged(nameof(SelectedCategory));

                RefreshAvailableTemplates();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public ObservableCollection<TemplateViewModel> AvailableTemplates
        {
            get;
            private set;
        } = new ObservableCollection<TemplateViewModel>();
        public TemplateViewModel SelectedTemplate
        {
            get;
            set;
        }

        bool emptyProjectsOnly = false;
        public bool EmptyProjectsOnly
        {
            get
            {
                return emptyProjectsOnly;
            }
            set
            {
                emptyProjectsOnly = value;

                filteredCategories.Clear();

                bool addStarter = emptyProjectsOnly == false;

                if(addStarter)
                {
                    filteredCategories.Add(starterCategory);
                }
                filteredCategories.Add(emptyProjectsCategory);

                SelectedCategory = filteredCategories[0];
            }
        }

        public NewProjectViewModel()
        {

            starterCategory = new TemplateCategoryViewModel
            {
                Name = "Starter Projects"
            };

            emptyProjectsCategory = new TemplateCategoryViewModel
            {
                Name = "Empty Projects"
            };
            allCategories.Add(starterCategory);
            allCategories.Add(emptyProjectsCategory);

            foreach(var category in allCategories)
            {
                filteredCategories.Add(category);
            }

            SelectedCategory = filteredCategories[0];

            RefreshAvailableTemplates();
        }

        void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string CombinedProjectDirectory
        {
            get
            {
                if (!ProjectLocation.EndsWith("\\") && !ProjectLocation.EndsWith("/"))
                {
                    return ProjectLocation+ "\\" + ProjectName;
                }
                else
                {
                    return ProjectLocation + ProjectName;

                }
            }
        }

        public string GetWhyIsntValid()
        {
            string whyIsntValid = null;
            if(UseDifferentNamespace)
            {
                if (string.IsNullOrEmpty(DifferentNamespace))
                {
                    whyIsntValid = "You must enter a non-empty namespace if using a different namespace";
                }
                else if (char.IsDigit(DifferentNamespace[0]))
                {
                    whyIsntValid = "Namespace can't start with a number.";
                }
                else if (DifferentNamespace.Contains(' '))
                {
                    whyIsntValid = "The namespace can't have any spaces.";
                }
                else if (DifferentNamespace.IndexOfAny(invalidNamespaceCharacters) != -1)
                {
                    whyIsntValid = "The namespace can't contain invalid character " + DifferentNamespace[DifferentNamespace.IndexOfAny(invalidNamespaceCharacters)];
                }
            }

                if(string.IsNullOrEmpty(whyIsntValid))
                {
                    whyIsntValid = ProjectCreationHelper.GetWhyProjectNameIsntValid(ProjectName);
                }


            return whyIsntValid;
        }

        void RefreshAvailableTemplates()
        {
            AvailableTemplates.Clear();

            if (SelectedCategory != null)
            {
                if (SelectedCategory.Name == "Starter Projects")
                {
                    foreach (var item in DataLoader.StarterProjects)
                    {
                        var viewModel = new TemplateViewModel
                        {
                            BackingData = item
                        };

                        AvailableTemplates.Add(viewModel);
                    }
                }
                else
                {
                    foreach (var item in DataLoader.EmptyProjects)
                    {
                        var viewModel = new TemplateViewModel
                        {
                            BackingData = item
                        };

                        AvailableTemplates.Add(viewModel);
                    }
                }
            }

            if(AvailableTemplates.Count != 0)
            {
                SelectedTemplate = AvailableTemplates[0];
            }
        }
    }
}
