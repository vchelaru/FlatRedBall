using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GumPlugin.Managers;

// Spawns the published "gumcli" tool bundled under Tools/gumcli (same pattern as
// MainGumPlugin.HandleBuildMissingFonts spawning GumProjectFontGenerator.exe) instead of vendoring
// a hand-maintained copy of Gum's project templates in this repo. See issue #2335.
public static class GumCliRunner
{
    const string ExecutablePath = "Plugins/GumPlugin/Tools/gumcli/gumcli.exe";

    public static Task<bool> NewProjectAsync(FilePath gumxFilePath, string template) =>
        RunAsync($@"new ""{gumxFilePath.FullPath}"" --template {template}", "gumcli new");

    public static Task<bool> AddFormsAsync(FilePath gumxFilePath) =>
        RunAsync($@"add-forms ""{gumxFilePath.FullPath}""", "gumcli add-forms");

    // Goes through gumcli rather than the vendored GumProjectFontGenerator.exe, which is a frozen build of
    // Gum's loader and so can't read a .gumj project.
    // The returned task only runs the process and calls no Glue code, so a queued task can block on it
    // without deadlocking (see ExternalFileWrites). Report prints the result afterwards.
    public static Task<GumCliResult> RunFontsProcessAsync(FilePath gumProjectFilePath) =>
        RunProcessAsync($@"fonts ""{gumProjectFilePath.FullPath}""");

    public const string FontsDescription = "gumcli fonts";

    static async Task<bool> RunAsync(string arguments, string description) =>
        Report(await RunProcessAsync(arguments), description);

    static Task<GumCliResult> RunProcessAsync(string arguments) => RunProcessAsync(ExecutablePath, arguments);

    internal static async Task<GumCliResult> RunProcessAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        // Both streams are drained at once. Reading one to the end first leaves gumcli blocked writing the
        // other once it fills its pipe buffer, and both processes then wait forever.
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        var output = await outputTask;
        var error = await errorTask;
        await process.WaitForExitAsync();

        return new GumCliResult(process.ExitCode, output, error, $"{startInfo.FileName} {startInfo.Arguments}");
    }

    /// <summary>
    /// Prints a finished gumcli run to Glue's output and returns whether it succeeded.
    /// </summary>
    public static bool Report(GumCliResult result, string description)
    {
        if (result.ExitCode != 0)
        {
            GlueCommands.Self.PrintError($"{description} failed ({result.CommandLine}):\n{result.Error}");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            GlueCommands.Self.PrintOutput(result.Output.Trim());
        }

        return true;
    }
}

public record GumCliResult(int ExitCode, string Output, string Error, string CommandLine);
