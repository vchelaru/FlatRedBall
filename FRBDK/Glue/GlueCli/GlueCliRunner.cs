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
using FlatRedBall.Glue.VSHelpers;
using GlueCli.Commands;

namespace GlueCli;

/// <summary>
/// Parses a GlueCli command line and runs it in-process: load the project's model
/// (<see cref="ProjectLoadScope.ModelOnly"/>), run the command, wait for queued tasks, save, and report every
/// error Glue knows of.
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
        DescribeCommand.Definition,
        AddObjectCommand.Definition,
        SetVariableCommand.Definition,
        AddFileCommand.Definition,
        AddEntityCommand.Definition,
        AddScreenCommand.Definition,
        RenameCommand.Definition,
        DeleteCommand.Definition,
        SetPropertyCommand.Definition,
        SetFilePropertyCommand.Definition,
        AddStateCategoryCommand.Definition,
        AddStateCommand.Definition,
        SetStateVariableCommand.Definition,
        RenameStateCommand.Definition,
        RenameStateCategoryCommand.Definition,
        DeleteStateCommand.Definition,
        AddVariableCommand.Definition,
        SetVariablePropertyCommand.Definition,
        RemoveVariableCommand.Definition,
        AddEventCommand.Definition,
        RemoveEventCommand.Definition,
        AddCollisionRelationshipCommand.Definition,
        SetCollisionPropertyCommand.Definition,
        BatchCommand.Definition,
    };

    public static readonly IReadOnlyList<CliOption> GlobalOptions = new CliOption[]
    {
        new("json", null, "Print one JSON object to stdout instead of text. Everything else goes to stderr."),
        new("dry-run", null, "Run the command in full against a temp copy of the project and report what it would\n" +
            "change (with a diff of each JSON file), leaving the project on disk exactly as it was."),
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
        lines.AddRange(GlobalOptions.Select(option =>
            $"  {option.Signature,-9}  {option.Description.Replace("\n", Environment.NewLine + new string(' ', 13))}"));
        lines.AddRange(new[]
        {
            "",
            "Every run loads the project, runs one command, waits for Glue's queued work, and reports what Glue's",
            "error reporters, its error output, and any would-be dialog produced. Loading reads the project and",
            "writes nothing, so check and describe change no file. regen writes all generated code and the files",
            "plugins generate for the project (Gum runtimes, Tiled classes, camera setup).",
            "Edit commands save the .gluj and element files and regenerate the code of what they changed, as the",
            "editor does after an edit, and report errors the project already had separately from the ones the edit",
            "added. Run regen once after cloning or pulling, or whenever generated code is out of date; edit",
            "commands regenerate only what they change. Plugins load from the Plugins folder next to",
            "GlueCli.exe, plus the project's own Plugins folder, as in the editor. The editor's settings are read but",
            "never saved. The project can stay open in the Glue editor: its file watcher reloads what an edit saved.",
            "",
            "To make several edits, put them in a file, one command per line, and run batch: Glue starts and loads",
            "the project once, and each command runs as it would alone. See GlueCli batch --help.",
            "",
            "To preview an edit, add --dry-run: the command runs exactly as it would (same checks, plugin reactions",
            "and codegen) against a temp copy, and the run reports the files it would change instead of changing",
            "them. The exit code is the one the real edit would get.",
            "",
            "When an edit is not applied, the project's changes are not saved.",
            "",
            "--json prints one object: command, project, exitCode, failure (why the run stopped, exit 2),",
            "editFailures (why an edit was not applied), edit (what it did; shape per command), editSummary (the",
            "text output's one-line version of edit), errors (Glue's error list, each {type, details}),",
            "outputErrors, dialogMessages, preexistingErrors (edits only: what the project already had, not",
            "counted), filesWritten (relative to the .csproj folder), startupSeconds, loadSeconds and",
            "elapsedSeconds. With --dry-run it also has dryRun: true and changes, each {path, kind (modified,",
            "added or deleted), diff (a unified diff for .gluj, .glsj, .glej and .json files, else null)}, and",
            "filesWritten is empty.",
            "",
            ExitCodeHelp,
        });
        return string.Join(Environment.NewLine, lines);
    }

    /// <param name="dryRunCopier">Makes the temp copy for --dry-run. Null uses the default temp location.</param>
    /// <param name="stdin">What <c>batch --stdin</c> reads. Null reads <see cref="Console.In"/>.</param>
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr, IGlueCliHost host,
        CliDryRunCopier? dryRunCopier = null, TextReader? stdin = null)
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

        var isBatch = command == BatchCommand.Definition;
        ICliOutcome outcome = isBatch ? new CliBatchResult() : result;
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
            return Fail(outcome, parseError, wantsJson, stdout, stderr, command.GetHelp());
        }
        var json = options.ContainsKey("json");

        var csproj = ResolveCsproj(positional[0], out var resolveError);
        if (csproj == null)
        {
            return Fail(outcome, resolveError!, json, stdout, stderr);
        }
        outcome.Project = csproj;

        List<CliStep> steps;
        if (isBatch)
        {
            var batchSteps = BatchCommand.ReadSteps(context, stdin ?? Console.In, csproj, out var batchError);
            if (batchSteps == null)
            {
                return Fail(outcome, batchError!, json, stdout, stderr);
            }
            steps = batchSteps;
            ((CliBatchResult)outcome).CommandCount = steps.Count;
        }
        else
        {
            steps = new List<CliStep> { new(command, context, result) };
        }

        var stopwatch = Stopwatch.StartNew();
        CliDryRunCopy? dryRun = null;
        if (options.ContainsKey("dry-run"))
        {
            try
            {
                dryRun = (dryRunCopier ?? new CliDryRunCopier()).Copy(csproj);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return Fail(outcome, "Could not make the temp copy for --dry-run: " + e.Message, json, stdout, stderr);
            }
            outcome.DryRun = true;
            outcome.DryRunCopy = dryRun;
            foreach (var step in steps)
            {
                step.Result.DryRunCopy = dryRun;
            }
        }

        try
        {
            return RunLoaded(steps, outcome, dryRun?.Csproj ?? csproj, dryRun, stopwatch, json,
                keepGoing: options.ContainsKey("keep-going"), stdout, stderr, host);
        }
        finally
        {
            dryRun?.Dispose();
        }
    }

    /// <summary>
    /// Starts Glue, loads the project once, and runs <paramref name="steps"/> in order through
    /// <see cref="RunStep"/>. A single command is a list of one, reported as <paramref name="outcome"/> itself;
    /// a batch (<see cref="CliBatchResult"/>) stops at the first step that does not exit 0 unless
    /// <paramref name="keepGoing"/>.
    /// </summary>
    static int RunLoaded(IReadOnlyList<CliStep> steps, ICliOutcome outcome, string csproj, CliDryRunCopy? dryRun,
        Stopwatch stopwatch, bool json, bool keepGoing, TextWriter stdout, TextWriter stderr, IGlueCliHost host)
    {
        var batch = outcome as CliBatchResult;
        var dialogMessages = new List<string>();
        void ReportMessage(string message)
        {
            lock (dialogMessages) { dialogMessages.Add(message); }
            stderr.WriteLine("[dialog] " + message);
        }

        IReadOnlyList<string> pluginFailures;
        try
        {
            pluginFailures = host.Prepare(csproj, ReportMessage);
        }
        catch (Exception e)
        {
            return Fail(outcome, "Glue failed to start: " + e, json, stdout, stderr);
        }
        if (pluginFailures.Count > 0)
        {
            return Fail(outcome, "These plugins failed to start, so results would be incomplete:" + Environment.NewLine +
                string.Join(Environment.NewLine, pluginFailures), json, stdout, stderr);
        }
        outcome.StartupSeconds = stopwatch.Elapsed.TotalSeconds;

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

        var current = steps[0];
        try
        {
            var projectRoot = Path.GetDirectoryName(csproj)!;
            HeadlessOutputRecorder.Clear();
            var before = dryRun == null ? SnapshotFiles(projectRoot) : null;
            var projectBefore = GlueState.Self.CurrentGlueProject;

            // Only the model: generating code, saving, and .csproj fix-ups are ProjectLoader.Operations steps a
            // command calls when it needs them, so check and describe write nothing.
            ProjectLoader.Self.LoadProject(csproj, ProjectLoadScope.ModelOnly).GetAwaiter().GetResult();
            TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();

            // LoadProject reports a failure (a missing file, an unreadable .csproj) by not loading anything.
            if (GlueState.Self.CurrentGlueProject == null || GlueState.Self.CurrentGlueProject == projectBefore)
            {
                return Fail(outcome, $"Glue could not load {csproj}." + DescribeCollected(dialogMessages), json, stdout, stderr);
            }
            outcome.LoadSeconds = stopwatch.Elapsed.TotalSeconds - outcome.StartupSeconds;

            // The first command reports what the load printed; a later read-only command only its own output.
            var earlierOutput = Collected.Empty;
            for (var i = 0; i < steps.Count; i++)
            {
                current = steps[i];
                var stepStart = stopwatch.Elapsed.TotalSeconds;
                var result = current.Result;
                RunStep(current.Command, current.Context, result, earlierOutput, dialogMessages);

                if (dryRun == null)
                {
                    result.FilesWritten = ChangedFiles(projectRoot, before!);
                }
                else if (batch == null)
                {
                    result.Changes = dryRun.ComputeChanges();
                }
                result.ExitCode = result.ProblemCount == 0 ? ExitSuccess : ExitErrors;

                if (batch == null)
                {
                    result.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                    Write(result, current.Command, json, stdout);
                    return result.ExitCode;
                }

                result.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds - stepStart;
                batch.Results.Add(result);
                if (!json)
                {
                    Write(result, current.Command, json: false, stdout, inBatch: true);
                }
                if (batch.ExitCode == ExitSuccess)
                {
                    batch.ExitCode = result.ExitCode;
                }
                if (result.ExitCode != ExitSuccess && !keepGoing)
                {
                    break;
                }
                if (i + 1 < steps.Count)
                {
                    before = dryRun == null ? SnapshotFiles(projectRoot) : null;
                    earlierOutput = Collect(dialogMessages) with { Errors = new() };
                }
            }

            batch!.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            if (dryRun != null)
            {
                batch.Changes = dryRun.ComputeChanges();
            }
            WriteBatch(batch, json, stdout);
            return batch.ExitCode;
        }
        catch (Exception e)
        {
            // What Glue reported before throwing usually names the cause better than the exception does
            // (a .gluj that does not parse surfaces as an unanswered question, then an NRE).
            return Fail(outcome, $"'{current.Command.Name}' failed: {e}" + DescribeCollected(dialogMessages), json, stdout, stderr);
        }
        finally
        {
            // The copy is about to be deleted, so nothing in this process may keep pointing at it. Closed here,
            // before the dialog seams go, because closing a project can ask questions too.
            try
            {
                var loadedFile = GlueState.Self.GlueProjectFileName;
                if (dryRun != null && FlatRedBall.Glue.ProjectManager.GlueProjectSave != null && loadedFile != null &&
                    dryRun.Contains(loadedFile.FullPath))
                {
                    FlatRedBall.Glue.ProjectManager.UnloadProject(isExiting: false);
                }
            }
            catch (Exception e)
            {
                stderr.WriteLine("Closing the --dry-run copy failed: " + e.Message);
            }
            SynchronizationContext.SetSynchronizationContext(previousContext);
            (DialogService.ShowMessageImpl, DialogService.ShowConfirmImpl,
                DialogService.ShowChoiceImpl, DialogService.ShowDeleteImpl, GlueGui.SuppressedMessageHandler) = previousDialogs;
        }
    }

    /// <summary>
    /// One command against the loaded project, shared by a single run and each line of a batch: run it, and for
    /// an edit regenerate and save what it changed and report only the errors it added to what the project had
    /// right before it. <paramref name="earlierOutput"/> is the error output and dialog messages a read-only
    /// command leaves out because an earlier command in the batch produced them.
    /// </summary>
    static void RunStep(CliCommand command, CliContext context, CliResult result, Collected earlierOutput,
        List<string> dialogMessages)
    {
        // For an edit, what the project already had is reported but does not fail the run.
        var baseline = command.IsEdit ? Collect(dialogMessages) : earlierOutput;
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
        // A read-only command's baseline is only output an earlier batch command printed, not the project's.
        result.PreexistingErrors = !command.IsEdit ? new() : after.Errors.Where(error => baselineErrorKeys.Contains(ErrorKey(error)))
            .Concat(baseline.OutputErrors.Select(line => new CliError("ErrorOutput", line)))
            .Concat(baseline.DialogMessages.Select(line => new CliError("DialogMessage", line)))
            .ToList();
    }

    internal static CliCommand? FindCommand(string name) =>
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
            return ProjectFileResolver.WithFileNameCasingOnDisk(fullPath);
        }
        if (extension != ".gluj")
        {
            error = $"{fullPath} is not a .gluj, a .csproj, or a folder.";
            return null;
        }

        var sameName = Path.ChangeExtension(fullPath, ".csproj");
        if (File.Exists(sameName))
        {
            return ProjectFileResolver.WithFileNameCasingOnDisk(sameName);
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

    static int Fail(ICliOutcome outcome, string message, bool json, TextWriter stdout, TextWriter stderr, string? help = null)
    {
        outcome.ExitCode = ExitUsageOrLoadFailure;
        outcome.Failure = outcome.DryRunCopy?.MapPaths(message) ?? message;
        if (json)
        {
            WriteJson(outcome, stdout);
        }
        stderr.WriteLine(outcome.Failure);
        if (help != null)
        {
            stderr.WriteLine();
            stderr.WriteLine(help);
        }
        return outcome.ExitCode;
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // The output goes to a console or a parser, never into HTML, so List<T> can stay readable.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Prints the --json object. A dry run loaded the temp copy; the report names the real project, so every
    /// path is mapped back, including the command-specific <c>edit</c> objects in the serialized text.
    /// </summary>
    static void WriteJson(ICliOutcome outcome, TextWriter stdout)
    {
        var results = outcome is CliBatchResult batch ? batch.Results : new List<CliResult> { (CliResult)outcome };
        foreach (var result in results)
        {
            outcome.DryRunCopy?.MapResultPaths(result);
        }
        var serialized = JsonSerializer.Serialize(outcome, outcome.GetType(), JsonOptions);
        stdout.WriteLine(outcome.DryRunCopy?.MapPaths(serialized) ?? serialized);
    }

    /// <param name="inBatch">One block of a batch's text output: the timing line is the command's own, and the
    /// batch's summary says the rest.</param>
    static void Write(CliResult result, CliCommand? command, bool json, TextWriter stdout, bool inBatch = false)
    {
        if (json)
        {
            WriteJson(result, stdout);
            return;
        }
        result.DryRunCopy?.MapResultPaths(result);

        stdout.WriteLine($"{result.Command} {result.Project}");
        stdout.WriteLine(inBatch
            ? $"Ran in {result.ElapsedSeconds:0.0}s."
            : $"Glue started in {result.StartupSeconds:0.0}s, project loaded in {result.LoadSeconds:0.0}s, " +
              $"{result.ElapsedSeconds:0.0}s total.");
        if (result.EditSummary != null)
        {
            stdout.WriteLine(result.EditSummary);
        }
        WriteSection(stdout, command?.IsEdit == false ? "Failed" : "Edit not applied", result.EditFailures);
        WriteSection(stdout, "Errors", result.Errors.Select(error => $"{error.Type}: {error.Details}"));
        WriteSection(stdout, "Error output", result.OutputErrors);
        WriteSection(stdout, "Dialog messages", result.DialogMessages);
        WriteSection(stdout, "Errors the project already had (not counted)", result.PreexistingErrors.Select(error => $"{error.Type}: {error.Details}"));
        WriteSection(stdout, "Files written", result.FilesWritten);
        WriteChanges(stdout, result.Changes);
        var problemCount = result.ProblemCount;
        stdout.WriteLine(problemCount > 0 ? $"{problemCount} problem(s)."
            : command?.IsEdit == true ? "No new errors." : "No errors.");
        if (result.DryRun)
        {
            stdout.WriteLine("Dry run: the project on disk was not changed.");
        }
        if (inBatch)
        {
            stdout.WriteLine();
        }
    }

    /// <summary>The batch's text summary, after each command's block.</summary>
    static void WriteBatch(CliBatchResult batch, bool json, TextWriter stdout)
    {
        if (json)
        {
            WriteJson(batch, stdout);
            return;
        }
        stdout.WriteLine($"batch {batch.Project}");
        stdout.WriteLine($"Glue started in {batch.StartupSeconds:0.0}s, project loaded in {batch.LoadSeconds:0.0}s, " +
            $"{batch.ElapsedSeconds:0.0}s total.");
        WriteChanges(stdout, batch.Changes);
        var ran = batch.Results.Count;
        stdout.WriteLine(ran == batch.CommandCount
            ? $"Ran all {ran} command(s), exit code {batch.ExitCode}."
            : $"Stopped after command {ran} of {batch.CommandCount} failed, exit code {batch.ExitCode}. " +
              "The commands before it stay applied; --keep-going runs the rest.");
        if (batch.DryRun)
        {
            stdout.WriteLine("Dry run: the project on disk was not changed.");
        }
    }

    static void WriteChanges(TextWriter stdout, List<CliFileChange>? changes)
    {
        if (changes != null)
        {
            WriteSection(stdout, "Would change", changes.Select(change =>
                $"{change.Kind} {change.Path}" + (change.Diff == null ? "" : "\n" + change.Diff)));
        }
    }

    static void WriteSection(TextWriter stdout, string title, IEnumerable<string> lines)
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

/// <summary>One command a run executes, with the options parsed for it and the result it fills.</summary>
internal sealed record CliStep(CliCommand Command, CliContext Context, CliResult Result);

/// <summary>What every run reports, a single command (<see cref="CliResult"/>) or a batch (<see cref="CliBatchResult"/>).</summary>
internal interface ICliOutcome
{
    string? Project { get; set; }
    int ExitCode { get; set; }
    string? Failure { get; set; }
    bool DryRun { get; set; }
    CliDryRunCopy? DryRunCopy { get; set; }
    double StartupSeconds { get; set; }
    double LoadSeconds { get; set; }
    double ElapsedSeconds { get; set; }
}

/// <summary>What <c>batch</c> reports. Serialized as-is for --json.</summary>
public sealed class CliBatchResult : ICliOutcome
{
    public string Command => "batch";
    public string? Project { get; set; }
    /// <summary>The first non-zero exit code of the commands that ran, or 2 when the batch itself failed.</summary>
    public int ExitCode { get; set; }
    /// <summary>Set when the batch could not run (bad syntax, a failed load), or a command threw (exit code 2).</summary>
    public string? Failure { get; set; }
    /// <summary>How many commands the batch has; <see cref="Results"/> is shorter when it stopped early.</summary>
    public int CommandCount { get; set; }
    /// <summary>Each command's result, in order, as the command reports it on its own.</summary>
    public List<CliResult> Results { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool DryRun { get; set; }
    /// <summary>--dry-run only: what the whole batch would have changed, relative to the .csproj folder.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<CliFileChange>? Changes { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CliDryRunCopy? DryRunCopy { get; set; }
    public double StartupSeconds { get; set; }
    public double LoadSeconds { get; set; }
    public double ElapsedSeconds { get; set; }
}

public sealed record CliError(string Type, string Details);

/// <summary>
/// What a run reports. Serialized as-is for --json.
/// </summary>
public sealed class CliResult : ICliOutcome
{
    public string Command { get; set; } = "";
    public string? Project { get; set; }
    public int ExitCode { get; set; }
    /// <summary>Set when the run could not get as far as reporting errors (exit code 2).</summary>
    public string? Failure { get; set; }
    /// <summary>
    /// Why an edit command did not apply its edit. The project's changes were not saved.
    /// </summary>
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
    /// <summary>Set by --dry-run: the run used a temp copy and nothing in the project was written.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool DryRun { get; set; }
    /// <summary>--dry-run only: what the run would have changed, relative to the .csproj folder.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<CliFileChange>? Changes { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CliDryRunCopy? DryRunCopy { get; set; }
    public double StartupSeconds { get; set; }
    public double LoadSeconds { get; set; }
    public double ElapsedSeconds { get; set; }

    internal int ProblemCount => EditFailures.Count + Errors.Count + OutputErrors.Count + DialogMessages.Count;
}
