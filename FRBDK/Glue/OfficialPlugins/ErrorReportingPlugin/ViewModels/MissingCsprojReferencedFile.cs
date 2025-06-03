using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Errors;
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

        // is it included in the project?
        // todo:

        return false;
    }
}
