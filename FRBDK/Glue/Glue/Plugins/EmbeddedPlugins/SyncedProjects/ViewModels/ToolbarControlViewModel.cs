using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.SyncedProjects.ViewModels;

class ToolbarControlViewModel : ViewModel
{
    public bool IsOpenVisualStudioAutomaticallyChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool HasProjectLoaded
    {
        get => Get<bool>();
        set => Set(value);
    }

    public ObservableCollection<ProjectItemViewModel> ProjectItems
    {
        get; set;
    } = new ObservableCollection<ProjectItemViewModel>();

    [DependsOn(nameof(HasProjectLoaded))]
    public Visibility VisualStudioButtonVisibility => HasProjectLoaded.ToVisibility();

    [DependsOn(nameof(HasProjectLoaded))]
    public Visibility FolderButtonVisibility => HasProjectLoaded.ToVisibility();

    public ToolbarControlViewModel()
    {
    }


    public void HandleGluxUnload()
    {
        HasProjectLoaded = false;
        ProjectItems.Clear();
    }
    public void HandleGluxLoad()
    {
        var glueProject = GlueState.Self.CurrentGlueProject;

        var openAutomaticallyProperty = glueProject.Properties
                    .FirstOrDefault(item => item.Name == nameof(IsOpenVisualStudioAutomaticallyChecked));

        var value = openAutomaticallyProperty?.Value as bool? == true;
        IsOpenVisualStudioAutomaticallyChecked = value;
        HasProjectLoaded = true;

        if (value)
        {
            ProjectListEntry.OpenInVisualStudio(
                GlueState.Self.CurrentMainProject);
        }
        RefreshProjectItems();
    }

    public void HandleLoadedSyncedProject(ProjectBase project)
    {
        RefreshProjectItems();
    }

    private void RefreshProjectItems()
    {
        ProjectItems.Clear();
        foreach (var item in GlueState.Self.SyncedProjects)
        {
            ProjectItems.Add(new ProjectItemViewModel
            {
                Name = item.Name
            });
        }
    }

    public void HandleLoadedSyncedProject()
    {

    }

}

class ProjectItemViewModel
{
    public ICommand OpenSlnCommand { get; private set; }

    public string Name { get; set; }

    public string DisplayName => $"Open {Name}";

    public ProjectItemViewModel()
    {
        OpenSlnCommand = new Command(OpenSln);
    }

    void OpenSln()
    {
        var project = GlueState.Self.SyncedProjects.FirstOrDefault(
            item => item.Name == Name);
        if (project != null)
        {
            ProjectListEntry.OpenInVisualStudio(project);
        }
    }
}
