using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace GlueCli.Commands;

/// <summary><c>regen</c>: load, run the editor's generate-all steps, save the .csproj, report.</summary>
public static class RegenCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "regen",
        Summary = "Load the project, regenerate all code, and report errors.",
        Usage = "regen <project> [--json]",
        Description =
@"Same load as check, then does to the code what opening the project in the editor does: regenerates the code for
every screen, entity and global content, removes .csproj entries for generated files no element owns, waits for
queued tasks, saves the .csproj, and reports.",
        Examples = new[]
        {
            ("regen MyGame --json", "Regenerate everything and list the files written."),
        },
        Run = RegenerateAllCodeAsync,
    };

    static async Task RegenerateAllCodeAsync(CliContext context)
    {
        var operations = ProjectLoader.Self.Operations;
        operations.GeneratePerformanceTypes();
        operations.GenerateAllCode();
        await TaskManager.Self.WaitForAllTasksFinished();
        operations.RemoveOrphanedGeneratedCsprojEntries();
        GlueCommands.Self.ProjectCommands.SaveProjects();
    }
}
