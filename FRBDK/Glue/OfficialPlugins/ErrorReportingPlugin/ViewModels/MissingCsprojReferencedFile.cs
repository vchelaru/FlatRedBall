using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;

namespace OfficialPlugins.ErrorReportingPlugin.ViewModels;
public class MissingCsprojReferencedFile : ErrorViewModel
{
    public VisualStudioProject Project { get; init; }
    public FilePath FilePath { get; init; }


    public override string UniqueId => Details;

    public MissingCsprojReferencedFile(VisualStudioProject project, FilePath filePath)
    {
        Project = project;
        FilePath = filePath;
        Details = $"The file {FilePath} is referenced in the .csproj {project.Name}, but it does not exist on disk.";

        // "Go To Object" does nothing for this error since there is no Glue object to select.
        MenuItemList.Clear();
        MenuItemList.Add(new MenuItemViewModel
        {
            Header = "Remove Reference From .csproj",
            Command = new Command(RemoveReference)
        });
    }

    internal void RemoveReference()
    {
        if (Project.RemoveMissingFileReference(FilePath))
        {
            GlueCommands.Self.ProjectCommands.SaveProjects();
        }
    }

    public override bool GetIfIsFixed()
    {
        // This error is fixed when the file is added to the project
        if(FilePath.Exists())
        {
            return true;
        }
        if(GlueState.Self.CurrentMainProject != Project && GlueState.Self.SyncedProjects.Contains(Project) == false)
        {
            return true;
        }

        // fixed if the reference was removed from the project
        return !Project.IsFileReferenced(FilePath);
    }
}
