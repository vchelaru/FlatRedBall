using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Diagnostics;
using System.Threading;
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
    public static Task<bool> FontsAsync(FilePath gumProjectFilePath) =>
        RunAsync($@"fonts ""{gumProjectFilePath.FullPath}""", "gumcli fonts");

    // A wedged gumcli would otherwise hang project load forever, because reading its stdout waits for it to exit.
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    static Task<bool> RunAsync(string arguments, string description) =>
        RunAsync(ExecutablePath, arguments, description, DefaultTimeout);

    internal static async Task<bool> RunAsync(string executablePath, string arguments, string description, TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        // Read both streams at once: a full stderr pipe would otherwise block the child while we wait on stdout.
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        using var timeoutSource = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            GlueCommands.Self.PrintError($"{description} did not finish within {timeout.TotalSeconds:0} seconds and was stopped ({startInfo.FileName} {startInfo.Arguments})");
            return false;
        }

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            GlueCommands.Self.PrintError($"{description} failed ({startInfo.FileName} {startInfo.Arguments}):\n{error}");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            GlueCommands.Self.PrintOutput(output.Trim());
        }

        return true;
    }
}
