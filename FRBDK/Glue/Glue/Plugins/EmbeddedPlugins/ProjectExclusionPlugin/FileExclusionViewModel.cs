using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ProjectExclusionPlugin
{
    public class FileExclusionViewModel : ViewModel
    {
        ReferencedFileSave backingData;


        public List<IndividualProjectSetting> ProjectSettings
        {
            get;
            private set;
        }

        public FileExclusionViewModel()
        {
            ProjectSettings = new List<IndividualProjectSetting>();
        }


        public void SetFrom(ReferencedFileSave rfs)
        {
            backingData = rfs;

            foreach(var project in GlueState.Self.GetProjects())
            {
                var projectName = project.Name;

                var settings = new IndividualProjectSetting();
                settings.ProjectName = projectName;
                settings.IsEnabled = backingData.ProjectsToExcludeFrom.Contains(projectName) == false;
                settings.PropertyChanged += HandlePropertyChanged;

                ProjectSettings.Add(settings);
            }
        }

        private void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var asViewModel = sender as IndividualProjectSetting;

            switch (e.PropertyName)
            {
                case "IsEnabled":
                    bool isProjectEnabled = asViewModel.IsEnabled;

                    string projectName = asViewModel.ProjectName;
                    ReferencedFileSave rfs = this.backingData;

                    var currentElement = GlueState.Self.CurrentElement;

                    bool shouldSave = false;

                    if (isProjectEnabled && rfs.ProjectsToExcludeFrom.Contains(projectName))
                    {
                        ProjectMembershipManager.Self.IncludeFileInProject(projectName, rfs);
                        shouldSave = true;
                    }
                    else if (!isProjectEnabled && !rfs.ProjectsToExcludeFrom.Contains(projectName))
                    {
                        ProjectMembershipManager.Self.ExcludeFileFromProject(projectName, rfs);
                        shouldSave = true;
                    }

                    if (shouldSave)
                    {

                        TaskManager.Self.AddParallelTask(() =>
                        {
                            if (currentElement != null)
                            {
                                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(currentElement);
                            }
                            else
                            {
                                GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                            }

                            GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                            GlueCommands.Self.ProjectCommands.SaveProjects();
                        },
                        L.Texts.ProjectsSaveGenerateElements
                        );
                    }
                    break;
            }

            //if(this.PropertyChanged != null)
            //{
                NotifyPropertyChanged(e.PropertyName);
            //}

        }
    }
}
