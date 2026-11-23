using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace GlueCli.Commands;

/// <summary><c>regen</c>: load, regenerate all code, save the .csproj, report.</summary>
public static class RegenCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "regen",
        Summary = "Load the project, regenerate all code, and report errors.",
        Usage = "regen <project> [--json]",
        Description =
@"Same load as check, then regenerates the code for every screen, entity and global content, waits for queued
tasks, saves the .csproj, and reports.",
        Examples = new[]
        {
            ("regen MyGame --json", "Regenerate everything and list the files written."),
        },
        Run = RegenerateAllCodeAsync,
    };

    static async Task RegenerateAllCodeAsync(CliContext context)
    {
        GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
        await TaskManager.Self.WaitForAllTasksFinished();
        GlueCommands.Self.ProjectCommands.SaveProjects();
    }
}
