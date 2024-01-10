using System;
using System.ComponentModel.Composition;
using System.Linq;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ProjectExclusionPlugin
{
    [Export(typeof (PluginBase))]
    public class MainPlugin : EmbeddedPlugin
    {
        ExclusionControl control;
        PluginTab pluginTab;

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelected;
            this.ReactToLoadedGlux += HandleLoadedGlux;
            this.ReactToReferencedFileChangedValueHandler += HandleFilePropertyChange;
            }

        private void HandleFilePropertyChange(string variableName, object oldValue)
        {
            if(variableName == nameof(ReferencedFileSave.ProjectsToExcludeFrom))
            {
                var ideProjects = GlueState.Self.GetProjects();
                var glueProject = GlueState.Self.CurrentGlueProject;
                var wasAnythingRemoved = false;

                var rfs = GlueState.Self.CurrentReferencedFileSave;

                if(rfs != null)
                {
                    if(GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(rfs))
                    {
                        wasAnythingRemoved = true;
                    }
                }

                foreach (var ideProject in ideProjects)
                {
                    if(ProjectMembershipManager.Self.RemoveAllExcludedFiles(ideProject, glueProject))
                    {
                        wasAnythingRemoved = true;
                    }
                }

                if (wasAnythingRemoved)
                {
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }
            }
        }

        private void HandleLoadedGlux()
        {
            var glueProject = GlueState.Self.CurrentGlueProject;

            var ideProjects = GlueState.Self.GetProjects();

            bool wasAnythingRemoved = false;
            foreach (var ideProject in ideProjects)
            {
                wasAnythingRemoved |= ProjectMembershipManager.Self.RemoveAllExcludedFiles(ideProject, glueProject);
            }

            if(wasAnythingRemoved)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }

        private void HandleItemSelected(ITreeNode selectedTreeNode)
        {
            var file = GlueState.Self.CurrentReferencedFileSave;

            if (file == null)
            {
                pluginTab?.Hide();
            }
            else if(GlueState.Self.SyncedProjects.Count() != 0)
            {
                if (control == null)
                {
                    control = new ExclusionControl();
                    pluginTab = base.CreateTab(control, L.Texts.PlatformInclusions);
                }
                pluginTab.Show();

                FileExclusionViewModel viewModel = new FileExclusionViewModel();
                viewModel.PropertyChanged += HandlePropertyChanged;
                viewModel.SetFrom(file);

                UpdateTabTitle(file);

                control.DataContext = viewModel;

            }
        }

        private void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var file = GlueState.Self.CurrentReferencedFileSave;
            if (file != null)
            {
                UpdateTabTitle(file);
            }
        }

        private void UpdateTabTitle(SaveClasses.ReferencedFileSave file)
        {
            int count = file.ProjectsToExcludeFrom.Count;

            pluginTab.Title = count == 0 ? L.Texts.PlatformInclusions : String.Format(L.Texts.PlatformsExcluded, count);
        }
    }
}
