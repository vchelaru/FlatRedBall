using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.SyncedProjects;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
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
        if(project != null)
        {
            ProjectListEntry.OpenInVisualStudio(project);
        }
    }
}
