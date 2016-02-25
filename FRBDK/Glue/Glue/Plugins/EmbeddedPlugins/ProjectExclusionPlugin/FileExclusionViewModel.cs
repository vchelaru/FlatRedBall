using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;

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
                        IncludeFileInProject(projectName, rfs);
                        shouldSave = true;
                    }
                    else if (!isProjectEnabled && !rfs.ProjectsToExcludeFrom.Contains(projectName))
                    {
                        ExcludeFileFromProject(projectName, rfs);
                        shouldSave = true;
                    }

                    if (shouldSave)
                    {

                        TaskManager.Self.AddAsyncTask(() =>
                        {
                            if (currentElement != null)
                            {
                                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(currentElement);
                            }
                            else
                            {
                                GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
                            }

                            GlueCommands.Self.GluxCommands.SaveGlux();
                            GlueCommands.Self.ProjectCommands.SaveProjects();
                        },
                        "Generating element and saving projects"
                        );
                    }
                    break;
            }

            //if(this.PropertyChanged != null)
            //{
                NotifyPropertyChanged(e.PropertyName);
            //}

        }

        private void IncludeFileInProject(string projectName, ReferencedFileSave rfs)
        {
            rfs.ProjectsToExcludeFrom.Remove(projectName);

            //It's a little less efficient but we'll just perform a full sync to reuse code:

            var syncedProject = GlueState.Self.GetProjects().FirstOrDefault(item => item.Name == projectName);

            if (syncedProject != null)
            {
                syncedProject.SyncTo(GlueState.Self.CurrentMainProject, false);
            }
        }

        private static void ExcludeFileFromProject(string projectName, ReferencedFileSave rfs)
        {
            rfs.ProjectsToExcludeFrom.Add(projectName);

            var syncedProject = GlueState.Self.GetProjects().FirstOrDefault(item => item.Name == projectName);

            if(syncedProject != null)
            {
                string absolute = GlueCommands.Self.GetAbsoluteFileName(rfs);
                syncedProject.RemoveItem(absolute);
                if (syncedProject.ContentProject != null)
                {
                    syncedProject.ContentProject.RemoveItem(absolute);
                }
            }
            
        }
    }
}
