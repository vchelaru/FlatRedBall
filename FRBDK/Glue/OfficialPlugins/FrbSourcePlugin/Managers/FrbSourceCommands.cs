using System;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;

namespace OfficialPlugins.FrbSourcePlugin.Managers;

/// <summary>The FRB Source plugin's <see cref="IFrbSourceCommands"/>.</summary>
public class FrbSourceCommands : IFrbSourceCommands
{
    readonly AddSourceManager addSourceManager;
    readonly Frb2AddSourceManager frb2AddSourceManager;

    public FrbSourceCommands() : this(new AddSourceManager(), new Frb2AddSourceManager()) { }

    internal FrbSourceCommands(AddSourceManager addSourceManager, Frb2AddSourceManager frb2AddSourceManager)
    {
        this.addSourceManager = addSourceManager;
        this.frb2AddSourceManager = frb2AddSourceManager;
    }

    public bool HasFrbAndGumReposInDefaultLocation() =>
        (Directory.Exists(addSourceManager.DefaultFrbFilePath) &&
         Directory.Exists(addSourceManager.DefaultGumFilePath)) ||
        frb2AddSourceManager.HasFrb2RepoInDefaultLocation();

    public Task AddFrbSourceToDefaultLocationAsync(VisualStudioProject project) =>
        TaskManager.Self.AddAsync(async () =>
        {
            try
            {
                await AddFrbSourceToDefaultLocationInternal(project);
            }
            catch (Exception e)
            {
                GlueCommands.Self.PrintError($"FRB Source failed linking {project?.Name} to source\n{e}");
            }
        }, "FRB Source: link project to source");

    async Task AddFrbSourceToDefaultLocationInternal(VisualStudioProject project)
    {
        // FRB2 projects use a different solution/reference shape entirely (no .shproj, a .slnx
        // rather than a classic .sln) - AddSourceManager's FRB1 logic doesn't apply and previously
        // failed here with "Unable to parse solution file" for any FRB2 project.
        if (project is Frb2Project frb2Project)
        {
            await frb2AddSourceManager.LinkToSourceUsingDefaults(frb2Project);
        }
        else
        {
            await addSourceManager.LinkToSourceUsingDefaults(project);
        }
    }
}
