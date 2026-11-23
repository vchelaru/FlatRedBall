using System.IO;
using System.Diagnostics;
using System.Text.Json;
using EditorObjects.IoC;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Headless;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace GlueCli;

/// <summary>
/// Parses a GlueCli command line and runs it in-process: load the project the way the editor's File >
/// Load Project does, run the command, wait for queued tasks, save, and report every error Glue knows of.
/// Tests call <see cref="Run"/> directly with their own <see cref="IGlueCliHost"/>; Program.Main passes
/// the real one.
///
/// A command is the part between load and report (<see cref="Commands"/>). Edit commands slot in there:
/// apply the edit through the same GluxCommands/ElementCommands call the UI makes, and the shared tail
/// waits, saves and reports.
/// </summary>
public static class GlueCliRunner
{
    public const int ExitSuccess = 0;
    public const int ExitErrors = 1;
    public const int ExitUsageOrLoadFailure = 2;

    /// <summary>
    /// What a command does once the project is loaded. Null for <c>check</c>, which only loads.
    /// </summary>
    static readonly Dictionary<string, Func<Task>?> Commands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["check"] = null,
        ["regen"] = RegenerateAllCodeAsync,
    };

    public const string Usage =
@"GlueCli - runs FlatRedBall Glue headless against a game project.

Usage:
  GlueCli check <path> [--json]
  GlueCli regen <path> [--json]

  <path>   A .gluj, a .csproj, or a folder containing exactly one .gluj (searched recursively,
           skipping bin and obj).

Commands:
  check    Load the project as the editor's File > Load Project does and report every error Glue's
           error reporters, its error output, and any would-be dialog produce. Loading writes what
           opening the project in the editor writes (generated code, the .csproj), and nothing else.
  regen    Same load, then regenerate all code, wait for queued tasks, save the .csproj, and report.

Options:
  --json   Print one JSON object to stdout instead of text.

Plugins load from the Plugins folder next to GlueCli.exe, plus the project's own Plugins folder, as in
the editor. The editor's settings are read but never saved. Messages Glue would have shown in a dialog go
to stderr.

Exit codes:
  0  no errors
  1  the project loaded and has errors
  2  bad arguments, the project could not be loaded, or a plugin failed to start";

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr, IGlueCliHost host)
    {
        var json = args.Any(arg => string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase));
        var positional = args.Where(arg => !arg.StartsWith("--")).ToList();
        var unknownOptions = args.Where(arg => arg.StartsWith("--") && !string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase)
            && arg != "--help").ToList();

        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h") || positional.FirstOrDefault() == "help")
        {
            stdout.WriteLine(Usage);
            return args.Length == 0 ? ExitUsageOrLoadFailure : ExitSuccess;
        }

        var result = new CliResult { Command = positional.FirstOrDefault() ?? "" };

        if (unknownOptions.Count > 0)
        {
            return Fail(result, $"Unknown option {unknownOptions[0]}.", json, stdout, stderr, showUsage: true);
        }
        if (!Commands.TryGetValue(result.Command, out var command))
        {
            return Fail(result, $"Unknown command '{result.Command}'.", json, stdout, stderr, showUsage: true);
        }
        if (positional.Count != 2)
        {
            return Fail(result, $"'{result.Command}' takes exactly one path.", json, stdout, stderr, showUsage: true);
        }

        var csproj = ResolveCsproj(positional[1], out var resolveError);
        if (csproj == null)
        {
            return Fail(result, resolveError!, json, stdout, stderr);
        }
        result.Project = csproj;

        var stopwatch = Stopwatch.StartNew();

        var dialogMessages = new List<string>();
        void ReportMessage(string message)
        {
            lock (dialogMessages) { dialogMessages.Add(message); }
            stderr.WriteLine("[dialog] " + message);
        }

        IReadOnlyList<string> pluginFailures;
        try
        {
            pluginFailures = host.Prepare(Path.GetDirectoryName(csproj)! + Path.DirectorySeparatorChar, ReportMessage);
        }
        catch (Exception e)
        {
            return Fail(result, "Glue failed to start: " + e, json, stdout, stderr);
        }
        if (pluginFailures.Count > 0)
        {
            return Fail(result, "These plugins failed to start, so results would be incomplete:" + Environment.NewLine +
                string.Join(Environment.NewLine, pluginFailures), json, stdout, stderr);
        }
        result.StartupSeconds = stopwatch.Elapsed.TotalSeconds;

        var previousDialogs = (DialogService.ShowMessageImpl, DialogService.ShowConfirmImpl,
            DialogService.ShowChoiceImpl, DialogService.ShowDeleteImpl);
        DialogService.ShowMessageImpl = ReportMessage;
        DialogService.ShowConfirmImpl = (text, _, _) => { ReportMessage("(unanswered question) " + text); return null; };
        DialogService.ShowChoiceImpl = (text, _) => { ReportMessage("(unanswered question) " + text); return null!; };
        DialogService.ShowDeleteImpl = _ => { ReportMessage("(unanswered delete confirmation)"); return false; };

        // Blocking on the load below must not wait on continuations posted to a context nothing pumps.
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        try
        {
            var projectRoot = Path.GetDirectoryName(csproj)!;
            HeadlessOutputRecorder.Clear();
            var before = SnapshotFiles(projectRoot);
            var projectBefore = GlueState.Self.CurrentGlueProject;

            ProjectLoader.Self.LoadProject(csproj).GetAwaiter().GetResult();
            TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();

            // LoadProject reports a failure (a missing file, an unreadable .csproj) by not loading anything.
            if (GlueState.Self.CurrentGlueProject == null || GlueState.Self.CurrentGlueProject == projectBefore)
            {
                return Fail(result, $"Glue could not load {csproj}." + DescribeCollected(dialogMessages), json, stdout, stderr);
            }
            result.LoadSeconds = stopwatch.Elapsed.TotalSeconds - result.StartupSeconds;

            if (command != null)
            {
                command().GetAwaiter().GetResult();
                TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();
            }

            FileErrorReporter.FlushPendingErrors();

            var errors = Container.Get<GlueErrorManager>().GetAllCurrentErrors(out var reporterFailures);
            result.Errors = errors
                .Select(error => new CliError(error.GetType().Name, error.Details ?? error.GetType().Name))
                .ToList();
            lock (HeadlessOutputRecorder.Errors)
            {
                result.OutputErrors = HeadlessOutputRecorder.Errors.Concat(reporterFailures).ToList();
            }
            lock (dialogMessages)
            {
                result.DialogMessages = dialogMessages.ToList();
            }
            result.FilesWritten = ChangedFiles(projectRoot, before);
            result.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            result.ExitCode = result.Errors.Count + result.OutputErrors.Count + result.DialogMessages.Count == 0
                ? ExitSuccess
                : ExitErrors;

            Write(result, json, stdout);
            return result.ExitCode;
        }
        catch (Exception e)
        {
            // What Glue reported before throwing usually names the cause better than the exception does
            // (a .gluj that does not parse surfaces as an unanswered question, then an NRE).
            return Fail(result, $"'{result.Command}' failed: {e}" + DescribeCollected(dialogMessages), json, stdout, stderr);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
            (DialogService.ShowMessageImpl, DialogService.ShowConfirmImpl,
                DialogService.ShowChoiceImpl, DialogService.ShowDeleteImpl) = previousDialogs;
        }
    }

    static async Task RegenerateAllCodeAsync()
    {
        GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
        await TaskManager.Self.WaitForAllTasksFinished();
        GlueCommands.Self.ProjectCommands.SaveProjects();
    }

    /// <summary>
    /// The .csproj Glue should open for <paramref name="path"/>. A .gluj maps to the .csproj of the same name
    /// next to it, the pairing Glue itself uses, or else the only .csproj in its folder.
    /// </summary>
    internal static string? ResolveCsproj(string path, out string? error)
    {
        error = null;
        var fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            var glujs = Directory.EnumerateFiles(fullPath, "*.gluj", SearchOption.AllDirectories)
                .Where(file => !Path.GetRelativePath(fullPath, file).Split(Path.DirectorySeparatorChar)
                    .Any(segment => segment is "bin" or "obj"))
                .ToList();
            if (glujs.Count != 1)
            {
                error = glujs.Count == 0
                    ? $"No .gluj found under {fullPath}."
                    : $"{glujs.Count} .gluj files found under {fullPath}; pass one of them:" + Environment.NewLine +
                      string.Join(Environment.NewLine, glujs);
                return null;
            }
            fullPath = glujs[0];
        }

        if (!File.Exists(fullPath))
        {
            error = $"{fullPath} does not exist.";
            return null;
        }

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        if (extension == ".csproj")
        {
            return fullPath;
        }
        if (extension != ".gluj")
        {
            error = $"{fullPath} is not a .gluj, a .csproj, or a folder.";
            return null;
        }

        var sameName = Path.ChangeExtension(fullPath, ".csproj");
        if (File.Exists(sameName))
        {
            return sameName;
        }
        var csprojs = Directory.GetFiles(Path.GetDirectoryName(fullPath)!, "*.csproj");
        if (csprojs.Length == 1)
        {
            return csprojs[0];
        }
        error = $"Could not tell which .csproj goes with {fullPath}; pass the .csproj instead.";
        return null;
    }

    static Dictionary<string, DateTime> SnapshotFiles(string root) =>
        EnumerateProjectFiles(root).ToDictionary(file => file, File.GetLastWriteTimeUtc, StringComparer.OrdinalIgnoreCase);

    static List<string> ChangedFiles(string root, Dictionary<string, DateTime> before) =>
        EnumerateProjectFiles(root)
            .Where(file => !before.TryGetValue(file, out var time) || time != File.GetLastWriteTimeUtc(file))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

    static IEnumerable<string> EnumerateProjectFiles(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar)
                .Any(segment => segment is "bin" or "obj" or ".vs"));

    static string DescribeCollected(List<string> dialogMessages)
    {
        var lines = new List<string>();
        lock (dialogMessages) { lines.AddRange(dialogMessages); }
        lock (HeadlessOutputRecorder.Errors) { lines.AddRange(HeadlessOutputRecorder.Errors); }
        return lines.Count == 0 ? "" : Environment.NewLine + string.Join(Environment.NewLine, lines);
    }

    static int Fail(CliResult result, string message, bool json, TextWriter stdout, TextWriter stderr, bool showUsage = false)
    {
        result.ExitCode = ExitUsageOrLoadFailure;
        result.Failure = message;
        if (json)
        {
            Write(result, json: true, stdout);
        }
        stderr.WriteLine(message);
        if (showUsage)
        {
            stderr.WriteLine();
            stderr.WriteLine(Usage);
        }
        return result.ExitCode;
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    static void Write(CliResult result, bool json, TextWriter stdout)
    {
        if (json)
        {
            stdout.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            return;
        }

        stdout.WriteLine($"{result.Command} {result.Project}");
        stdout.WriteLine($"Glue started in {result.StartupSeconds:0.0}s, project loaded in {result.LoadSeconds:0.0}s, " +
            $"{result.ElapsedSeconds:0.0}s total.");
        WriteSection("Errors", result.Errors.Select(error => $"{error.Type}: {error.Details}"));
        WriteSection("Error output", result.OutputErrors);
        WriteSection("Dialog messages", result.DialogMessages);
        WriteSection("Files written", result.FilesWritten);
        var problemCount = result.Errors.Count + result.OutputErrors.Count + result.DialogMessages.Count;
        stdout.WriteLine(problemCount == 0 ? "No errors." : $"{problemCount} problem(s).");

        void WriteSection(string title, IEnumerable<string> lines)
        {
            var list = lines.ToList();
            if (list.Count == 0)
            {
                return;
            }
            stdout.WriteLine($"{title} ({list.Count}):");
            foreach (var line in list)
            {
                stdout.WriteLine("  " + line.Replace("\n", "\n    "));
            }
        }
    }
}

public sealed record CliError(string Type, string Details);

/// <summary>
/// What a run reports. Serialized as-is for --json.
/// </summary>
public sealed class CliResult
{
    public string Command { get; set; } = "";
    public string? Project { get; set; }
    public int ExitCode { get; set; }
    /// <summary>Set when the run could not get as far as reporting errors (exit code 2).</summary>
    public string? Failure { get; set; }
    /// <summary>What Glue's error reporters report: the Errors tab's contents.</summary>
    public List<CliError> Errors { get; set; } = new();
    /// <summary>Everything Glue printed to its error output (the output window's red lines).</summary>
    public List<string> OutputErrors { get; set; } = new();
    /// <summary>Messages and questions Glue would have shown in a dialog.</summary>
    public List<string> DialogMessages { get; set; } = new();
    /// <summary>Files under the project folder created or modified by the run, relative to it.</summary>
    public List<string> FilesWritten { get; set; } = new();
    public double StartupSeconds { get; set; }
    public double LoadSeconds { get; set; }
    public double ElapsedSeconds { get; set; }
}
