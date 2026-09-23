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

    static async Task<bool> RunAsync(string arguments, string description)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ExecutablePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

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
