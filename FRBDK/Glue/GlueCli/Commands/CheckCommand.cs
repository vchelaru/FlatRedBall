namespace GlueCli.Commands;

/// <summary><c>check</c>: load and report. No <see cref="CliCommand.Run"/>, since loading is the whole command.</summary>
public static class CheckCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "check",
        Summary = "Load the project and report every error Glue finds.",
        Usage = "check <project> [--json]",
        Description =
@"Loads the project as the editor's File > Load Project does and reports every error Glue's error reporters,
its error output, and any would-be dialog produce. Loading writes what opening the project in the editor
writes (generated code, the .csproj), and nothing else. Use it after hand-editing .gluj/.glsj/.glej files.",
        Examples = new[]
        {
            ("check MyGame", "Report errors in the only .gluj under MyGame."),
            ("check MyGame/MyGame.gluj --json", "Same, as JSON."),
        },
    };
}
