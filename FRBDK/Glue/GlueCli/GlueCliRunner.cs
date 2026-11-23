using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using EditorObjects.IoC;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Headless;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueCli.Commands;

namespace GlueCli;

/// <summary>
/// Parses a GlueCli command line and runs it in-process: load the project the way the editor's File >
/// Load Project does, run the command, wait for queued tasks, save, and report every error Glue knows of.
/// Tests call <see cref="Run"/> directly with their own <see cref="IGlueCliHost"/>; Program.Main passes
/// the real one.
///
/// Adding a command: write a <see cref="CliCommand"/> in Commands/ (see AddObjectCommand) and list it in
/// <see cref="Commands"/>. The runner parses its options, prints its help, and for an edit command
/// records the errors the project had before the edit and saves afterwards.
/// </summary>
public static class GlueCliRunner
{
    public const int ExitSuccess = 0;
    public const int ExitErrors = 1;
    public const int ExitUsageOrLoadFailure = 2;

    public static readonly IReadOnlyList<CliCommand> Commands = new[]
    {
        CheckCommand.Definition,
        RegenCommand.Definition,
        AddObjectCommand.Definition,
        SetVariableCommand.Definition,
    };

    public static readonly IReadOnlyList<CliOption> GlobalOptions = new CliOption[]
    {
        new("json", null, "Print one JSON object to stdout instead of text. Everything else goes to stderr."),
        new("help", null, "Show this help."),
    };

    public const string ExitCodeHelp =
@"Exit codes:
  0  done, and no new errors
  1  an edit command could not apply the edit, or the edit left errors the project did not have before
     (check and regen: the project has errors)
  2  bad arguments, the project could not be loaded, or a plugin failed to start";

    public static string GetHelp()
    {
        var width = Commands.Max(command => command.Name.Length);
        var lines = new List<string>
        {
            "GlueCli - runs FlatRedBall Glue headless against a game project, making the same edits the editor makes.",
            "",
            "Usage:",
            "  GlueCli <command> <project> [options]",
            "  GlueCli <command> --help      Options and examples for one command.",
            "",
            "  <project>  A .gluj, a .csproj, or a folder containing exactly one .gluj (searched recursively,",
            "             skipping bin and obj).",
            "",
            "Commands:",
        };
        lines.AddRange(Commands.Select(command => $"  {command.Name.PadRight(width)}  {command.Summary}"));
        lines.AddRange(new[]
        {
            "",
            "Options for every command:",
        });
        lines.AddRange(GlobalOptions.Select(option => $"  {option.Signature,-8}  {option.Description}"));
        lines.AddRange(new[]
        {
            "",
            "Every run loads the project, runs one command, waits for Glue's queued work, and reports what Glue's",
            "error reporters, its error output, and any would-be dialog produced. Edit commands save the .gluj and",
            "element files and regenerate code, as the editor does after an edit, and report errors the project",
            "already had separately from the ones the edit added. Plugins load from the Plugins folder next to",
            "GlueCli.exe, plus the project's own Plugins folder, as in the editor. The editor's settings are read but",
            "never saved. Do not run an edit while the project is open in the Glue editor.",
            "",
            "--json prints one object: command, project, exitCode, failure (why the run stopped, exit 2),",
            "editFailures (why an edit was not applied), edit (what it did; shape per command), errors (Glue's",
            "error list, each {type, details}), outputErrors, dialogMessages, preexistingErrors (edits only: what the",
            "project already had, not counted), filesWritten (relative to the .csproj folder), and timings.",
            "",
            ExitCodeHelp,
        });
        return string.Join(Environment.NewLine, lines);
    }

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr, IGlueCliHost host)
    {
        var wantsJson = args.Any(arg => string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase));
        if (args.Length == 0)
        {
            stdout.WriteLine(GetHelp());
            return ExitUsageOrLoadFailure;
        }

        var first = args[0];
        if (first is "--help" or "-h" or "help")
        {
            var helpCommand = args.Length > 1 ? FindCommand(args[1]) : null;
            stdout.WriteLine(helpCommand?.GetHelp() ?? GetHelp());
            return ExitSuccess;
        }

        var result = new CliResult { Command = first };
        var command = FindCommand(first);
        if (command == null)
        {
            return Fail(result, $"Unknown command '{first}'.", wantsJson, stdout, stderr, GetHelp());
        }
        if (args.Skip(1).Any(arg => arg is "--help" or "-h"))
        {
            stdout.WriteLine(command.GetHelp());
            return ExitSuccess;
        }

        var options = ParseOptions(command, args.Skip(1).ToList(), out var positional, out var parseError);
        if (parseError == null && positional.Count != 1)
        {
            parseError = positional.Count == 0
                ? $"'{command.Name}' needs the project path."
                : $"'{command.Name}' takes exactly one path, got: {string.Join(" ", positional)}.";
        }
        var context = new CliContext(options, result);
        parseError ??= command.ValidateArguments?.Invoke(context);
        if (parseError != null)
        {
            return Fail(result, parseError, wantsJson, stdout, stderr, command.GetHelp());
        }
        var json = options.ContainsKey("json");

        var csproj = ResolveCsproj(positional[0], out var resolveError);
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
            DialogService.ShowChoiceImpl, DialogService.ShowDeleteImpl, GlueGui.SuppressedMessageHandler);
        DialogService.ShowMessageImpl = ReportMessage;
        DialogService.ShowConfirmImpl = (text, _, _) => { ReportMessage("(unanswered question) " + text); return null; };
        DialogService.ShowChoiceImpl = (text, _) => { ReportMessage("(unanswered question) " + text); return null!; };
        DialogService.ShowDeleteImpl = _ => { ReportMessage("(unanswered delete confirmation)"); return false; };
        GlueGui.SuppressedMessageHandler = ReportMessage;

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

            // For an edit, what the project already had is reported but does not fail the run.
            var baseline = command.IsEdit ? Collect(dialogMessages) : Collected.Empty;
            var elementsBefore = command.IsEdit ? SerializeElements() : null;

            if (command.Run != null)
            {
                command.Run(context).GetAwaiter().GetResult();
                TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();
            }

            if (command.IsEdit && !context.HasFailed)
            {
                // Headless, TaskManager.SynchronousMode runs the AddOrMoveToEnd codegen an edit queues inline,
                // before the rest of the edit's task, where the editor's queue runs it after. Plugin reactions
                // later in that task (NewNamedObjectPlugin making a new list ExposedInDerived) would then be
                // missing from the generated code, so regenerate every element the edit changed.
                var elementsAfter = SerializeElements();
                foreach (var (element, elementJson) in elementsAfter)
                {
                    if (!elementsBefore!.TryGetValue(element, out var jsonBefore) || jsonBefore != elementJson)
                    {
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                    }
                }
                TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();

                // The editor's save after an edit: the .gluj and every element file, then the .csproj. Most
                // GluxCommands edits queue this themselves; doing it here means no command can forget it.
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();
                GlueCommands.Self.ProjectCommands.SaveProjects();
                TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();
            }

            var after = Collect(dialogMessages);
            var baselineErrorKeys = baseline.Errors.Select(ErrorKey).ToHashSet();
            result.Errors = after.Errors.Where(error => !baselineErrorKeys.Contains(ErrorKey(error))).ToList();
            result.OutputErrors = after.OutputErrors.Skip(baseline.OutputErrors.Count).ToList();
            result.DialogMessages = after.DialogMessages.Skip(baseline.DialogMessages.Count).ToList();
            result.PreexistingErrors = after.Errors.Where(error => baselineErrorKeys.Contains(ErrorKey(error)))
                .Concat(baseline.OutputErrors.Select(line => new CliError("ErrorOutput", line)))
                .Concat(baseline.DialogMessages.Select(line => new CliError("DialogMessage", line)))
                .ToList();

            result.FilesWritten = ChangedFiles(projectRoot, before);
            result.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            result.ExitCode = result.ProblemCount == 0 ? ExitSuccess : ExitErrors;

            Write(result, command, json, stdout);
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
                DialogService.ShowChoiceImpl, DialogService.ShowDeleteImpl, GlueGui.SuppressedMessageHandler) = previousDialogs;
        }
    }

    static CliCommand? FindCommand(string name) =>
        Commands.FirstOrDefault(command => string.Equals(command.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// <c>--name value</c>, <c>--name=value</c> and <c>--flag</c> into a dictionary keyed by the option name
    /// without dashes; everything else is positional.
    /// </summary>
    internal static Dictionary<string, string?> ParseOptions(CliCommand command, List<string> args,
        out List<string> positional, out string? error)
    {
        var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        positional = new List<string>();
        error = null;
        var known = command.Options.Concat(GlobalOptions).ToList();

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--"))
            {
                positional.Add(arg);
                continue;
            }

            var equals = arg.IndexOf('=');
            var name = equals == -1 ? arg.Substring(2) : arg.Substring(2, equals - 2);
            var option = known.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (option == null)
            {
                error = $"Unknown option --{name} for '{command.Name}'.";
                return options;
            }
            if (options.ContainsKey(option.Name))
            {
                error = $"--{option.Name} is given twice.";
                return options;
            }

            string? value = null;
            if (option.TakesValue)
            {
                if (equals != -1)
                {
                    value = arg.Substring(equals + 1);
                }
                else if (i + 1 < args.Count && !args[i + 1].StartsWith("--"))
                {
                    value = args[++i];
                }
                if (string.IsNullOrEmpty(value))
                {
                    error = $"--{option.Name} needs a value: --{option.Name} <{option.ValueName}>.";
                    return options;
                }
            }
            else if (equals != -1)
            {
                error = $"--{option.Name} takes no value.";
                return options;
            }
            options[option.Name] = value;
        }
        return options;
    }

    sealed record Collected(List<CliError> Errors, List<string> OutputErrors, List<string> DialogMessages)
    {
        public static readonly Collected Empty = new(new(), new(), new());
    }

    /// <summary>Everything Glue reports right now: its error list, its error output, would-be dialogs.</summary>
    static Collected Collect(List<string> dialogMessages)
    {
        FileErrorReporter.FlushPendingErrors();
        var errors = Container.Get<GlueErrorManager>().GetAllCurrentErrors(out var reporterFailures)
            .Select(error => new CliError(error.GetType().Name, error.Details ?? error.GetType().Name))
            .ToList();
        List<string> outputErrors;
        lock (HeadlessOutputRecorder.Errors)
        {
            outputErrors = HeadlessOutputRecorder.Errors.Concat(reporterFailures).ToList();
        }
        List<string> dialogs;
        lock (dialogMessages)
        {
            dialogs = dialogMessages.ToList();
        }
        return new Collected(errors, outputErrors, dialogs);
    }

    static string ErrorKey(CliError error) => error.Type + "\n" + error.Details;

    static Dictionary<GlueElement, string> SerializeElements()
    {
        var project = GlueState.Self.CurrentGlueProject;
        return project.Screens.Cast<GlueElement>().Concat(project.Entities)
            .ToDictionary(element => element, element => Newtonsoft.Json.JsonConvert.SerializeObject(element));
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

    // Glue's save rewrites every element file and much generated code with the same content, so a write time
    // alone would list files nothing changed. These are hashed to tell; anything else counts by write time.
    static readonly HashSet<string> HashedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gluj", ".glej", ".glsj", ".glux", ".csproj", ".cs", ".json",
    };

    sealed record FileState(DateTime WriteTime, string? Hash);

    static Dictionary<string, FileState> SnapshotFiles(string root) =>
        EnumerateProjectFiles(root).ToDictionary(file => file,
            file => new FileState(File.GetLastWriteTimeUtc(file), HashIfText(file)), StringComparer.OrdinalIgnoreCase);

    static List<string> ChangedFiles(string root, Dictionary<string, FileState> before) =>
        EnumerateProjectFiles(root)
            .Where(file => !before.TryGetValue(file, out var state) ||
                (state.WriteTime != File.GetLastWriteTimeUtc(file) && (state.Hash == null || state.Hash != HashIfText(file))))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

    static string? HashIfText(string file)
    {
        if (!HashedExtensions.Contains(Path.GetExtension(file)))
        {
            return null;
        }
        try
        {
            return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        }
        catch (IOException)
        {
            return null;
        }
    }

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

    static int Fail(CliResult result, string message, bool json, TextWriter stdout, TextWriter stderr, string? help = null)
    {
        result.ExitCode = ExitUsageOrLoadFailure;
        result.Failure = message;
        if (json)
        {
            Write(result, command: null, json: true, stdout);
        }
        stderr.WriteLine(message);
        if (help != null)
        {
            stderr.WriteLine();
            stderr.WriteLine(help);
        }
        return result.ExitCode;
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // The output goes to a console or a parser, never into HTML, so List<T> can stay readable.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static void Write(CliResult result, CliCommand? command, bool json, TextWriter stdout)
    {
        if (json)
        {
            stdout.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            return;
        }

        stdout.WriteLine($"{result.Command} {result.Project}");
        stdout.WriteLine($"Glue started in {result.StartupSeconds:0.0}s, project loaded in {result.LoadSeconds:0.0}s, " +
            $"{result.ElapsedSeconds:0.0}s total.");
        if (result.EditSummary != null)
        {
            stdout.WriteLine(result.EditSummary);
        }
        WriteSection("Edit not applied", result.EditFailures);
        WriteSection("Errors", result.Errors.Select(error => $"{error.Type}: {error.Details}"));
        WriteSection("Error output", result.OutputErrors);
        WriteSection("Dialog messages", result.DialogMessages);
        WriteSection("Errors the project already had (not counted)", result.PreexistingErrors.Select(error => $"{error.Type}: {error.Details}"));
        WriteSection("Files written", result.FilesWritten);
        var problemCount = result.ProblemCount;
        stdout.WriteLine(problemCount > 0 ? $"{problemCount} problem(s)."
            : command?.IsEdit == true ? "No new errors." : "No errors.");

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
    /// <summary>Why an edit command did not apply its edit. Nothing was saved.</summary>
    public List<string> EditFailures { get; set; } = new();
    /// <summary>What the edit did, shaped by the command (for add-object, the added object).</summary>
    public object? Edit { get; set; }
    /// <summary>The text output's one-line version of <see cref="Edit"/>.</summary>
    public string? EditSummary { get; set; }
    /// <summary>What Glue's error reporters report (the Errors tab), minus what was there before an edit.</summary>
    public List<CliError> Errors { get; set; } = new();
    /// <summary>What Glue printed to its error output (the output window's red lines), minus load output for an edit.</summary>
    public List<string> OutputErrors { get; set; } = new();
    /// <summary>Messages and questions Glue would have shown in a dialog, minus load-time ones for an edit.</summary>
    public List<string> DialogMessages { get; set; } = new();
    /// <summary>
    /// Edit commands only: errors, error output (type ErrorOutput) and dialog messages (type DialogMessage)
    /// the project already had after loading. Reported, but they do not fail the run.
    /// </summary>
    public List<CliError> PreexistingErrors { get; set; } = new();
    /// <summary>Files under the project folder created, or changed in content, by the run, relative to it.</summary>
    public List<string> FilesWritten { get; set; } = new();
    public double StartupSeconds { get; set; }
    public double LoadSeconds { get; set; }
    public double ElapsedSeconds { get; set; }

    internal int ProblemCount => EditFailures.Count + Errors.Count + OutputErrors.Count + DialogMessages.Count;
}
