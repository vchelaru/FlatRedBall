using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.EditorLaunch;

/// <summary>
/// Launches the real built editor (GlueFormsCore.exe, with its window, plugin views and real plugin folder)
/// against a copy of a sample, and fails if anything goes wrong between startup and a clean close. Everything
/// else in this assembly runs Glue headless, where <c>PluginBase.StartUpUi</c> never runs.
///
/// The editor reports through its opt-in automation mode (see <see cref="AutomationLog"/>): output, errors and
/// dialogs go to a file and no dialog is shown, so nothing modal is left on the desktop. "exitwhenquiet" closes
/// it through the normal close path once its task queue has been idle.
/// </summary>
public class EditorLaunchTests
{
    static readonly TimeSpan LaunchTimeout = TimeSpan.FromMinutes(5);

    [Trait("Category", "LiveGame")]
    [Fact]
    public async Task Beefball_LoadsAndClosesWithNoPluginFailures()
    {
        var editorExe = Path.Combine(RepoPaths.FrbRoot, "FRBDK", "Glue", "Glue", "bin", "Debug", "GlueFormsCore.exe");
        File.Exists(editorExe).ShouldBeTrue($"Build 'Glue with All.sln' first; the editor was not found at {editorExe}.");

        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var gluj = Path.Combine(project.Root, "Beefball", "Beefball.gluj");
        var logPath = Path.Combine(project.Root, "automation.log");

        var log = await RunEditor(editorExe, gluj, logPath);

        var lines = log.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        var loadedProjects = lines
            .Where(line => line.StartsWith(AutomationLog.ReadyTag + " "))
            .Select(line => Path.GetFullPath(line.Substring(AutomationLog.ReadyTag.Length + 1)))
            .ToList();
        loadedProjects.ShouldContain(loaded => string.Equals(loaded, Path.GetFullPath(gluj), StringComparison.OrdinalIgnoreCase),
            "The editor never finished starting up with the project loaded:" + Environment.NewLine + log);
        log.ShouldContain(AutomationLog.ExitingTag, Case.Sensitive,
            "The editor never reached its normal close path (crash or forced exit):" + Environment.NewLine + log);

        var problems = lines
            .Where(line => line.StartsWith(AutomationLog.ErrorTag) ||
                           line.StartsWith(AutomationLog.DialogTag) ||
                           line.Contains("Failed in "))
            .ToList();
        problems.ShouldBeEmpty("The editor reported errors, dialogs or plugin failures:" + Environment.NewLine + log);
    }

    static async Task<string> RunEditor(string editorExe, string projectFile, string logPath)
    {
        var startInfo = new ProcessStartInfo(editorExe)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(editorExe),
            WindowStyle = ProcessWindowStyle.Minimized,
        };
        startInfo.ArgumentList.Add(projectFile);
        startInfo.ArgumentList.Add("exitwhenquiet=3");
        startInfo.ArgumentList.Add("automationlog=" + logPath);

        using var process = Process.Start(startInfo)!;
        using var timeout = new CancellationTokenSource(LaunchTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"The editor did not close itself within {LaunchTimeout}. Log so far:" +
                Environment.NewLine + ReadLog(logPath));
        }

        return ReadLog(logPath);
    }

    static string ReadLog(string logPath) => File.Exists(logPath) ? File.ReadAllText(logPath) : "(no automation log was written)";
}
