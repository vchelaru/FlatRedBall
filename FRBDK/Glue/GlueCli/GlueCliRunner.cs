using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using EditorObjects.IoC;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Headless;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers;
using GlueCli.Commands;
using GluxCommands = FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces.GluxCommands;

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
            "the project once, runs every command, then saves and regenerates once, or writes nothing if a command",
            "failed. See GlueCli batch --help.",
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
        var projectRoot = Path.GetDirectoryName(csproj)!;
        Dictionary<string, FileState>? before = null;
        try
        {
            HeadlessOutputRecorder.Clear();
            // A batch keeps the text files' content, to put back what a failed batch wrote outside the held writes.
            before = dryRun == null || batch != null ? SnapshotFiles(projectRoot, keepContent: batch != null) : null;
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

            if (batch == null)
            {
                var single = steps[0];
                RunStep(single.Command, single.Context, single.Result, Collected.Empty, dialogMessages, inBatch: false);
                if (dryRun == null)
                {
                    single.Result.FilesWritten = ChangedFiles(projectRoot, before!);
                }
                else
                {
                    single.Result.Changes = dryRun.ComputeChanges();
                }
                single.Result.ExitCode = single.Result.ProblemCount == 0 ? ExitSuccess : ExitErrors;
                single.Result.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                Write(single.Result, single.Command, json, stdout);
                return single.Result.ExitCode;
            }

            // A batch runs every command against the model with writes held, then generates and saves what the
            // whole batch changed once, or writes nothing when a command failed.
            var elementsBefore = SerializeElements();
            var projectLevelBefore = SerializeProjectLevel();
            // What each edit's errors are counted against: the project as the previous command left it.
            var previous = Collect(dialogMessages);
            // The first command reports what the load printed; a later read-only command only its own output.
            var earlierOutput = Collected.Empty;
            ProjectWriteHold.Hold();
            for (var i = 0; i < steps.Count; i++)
            {
                current = steps[i];
                var stepStart = stopwatch.Elapsed.TotalSeconds;
                var result = current.Result;
                var baseline = current.Command.IsEdit ? previous : earlierOutput;
                previous = RunStep(current.Command, current.Context, result, baseline, dialogMessages, inBatch: true);
                earlierOutput = previous with { Errors = new() };
                result.ExitCode = result.ProblemCount == 0 ? ExitSuccess : ExitErrors;

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
            }

            var codeRequested = ProjectWriteHold.Release();
            if (batch.ExitCode == ExitSuccess)
            {
                var writeStart = stopwatch.Elapsed.TotalSeconds;
                GenerateAndSave(ChangedElements(elementsBefore), projectLevelBefore, failed: false,
                    savesWhenFailed: new HashSet<string>(), codeRequested);
                var after = Collect(dialogMessages);
                var previousKeys = previous.Errors.Select(ErrorKey).ToHashSet();
                batch.WriteErrors = after.Errors.Where(error => !previousKeys.Contains(ErrorKey(error)))
                    .Concat(after.OutputErrors.Skip(previous.OutputErrors.Count).Select(line => new CliError("ErrorOutput", line)))
                    .Concat(after.DialogMessages.Skip(previous.DialogMessages.Count).Select(line => new CliError("DialogMessage", line)))
                    .ToList();
                if (batch.WriteErrors.Count > 0)
                {
                    batch.ExitCode = ExitErrors;
                }
                if (dryRun == null)
                {
                    batch.FilesWritten = ChangedFiles(projectRoot, before!);
                }
                batch.WriteSeconds = stopwatch.Elapsed.TotalSeconds - writeStart;
            }
            else
            {
                RestoreAfterFailedBatch(batch, projectRoot, before!);
            }

            batch.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            if (dryRun != null)
            {
                batch.Changes = dryRun.ComputeChanges();
            }
            WriteBatch(batch, json, stdout);
            return batch.ExitCode;
        }
        catch (Exception e)
        {
            if (batch != null && before != null)
            {
                ProjectWriteHold.Release();
                RestoreAfterFailedBatch(batch, projectRoot, before);
            }
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
            ProjectWriteHold.Release();
            SynchronizationContext.SetSynchronizationContext(previousContext);
            (DialogService.ShowMessageImpl, DialogService.ShowConfirmImpl,
                DialogService.ShowChoiceImpl, DialogService.ShowDeleteImpl, GlueGui.SuppressedMessageHandler) = previousDialogs;
        }
    }

    /// <summary>
    /// One command against the loaded project, shared by a single run and each line of a batch, reporting only
    /// the errors it added to <paramref name="baseline"/>. On its own, an edit's baseline is collected here (pass
    /// <see cref="Collected.Empty"/>), and the edit then regenerates and saves what it changed. In a batch the
    /// baseline is what the previous command left, writes are held (<see cref="ProjectWriteHold"/>), and a
    /// read-only command's baseline is only the output and dialogs earlier commands produced.
    /// </summary>
    /// <returns>What Glue reports after the command, the next batch command's baseline.</returns>
    static Collected RunStep(CliCommand command, CliContext context, CliResult result, Collected baseline,
        List<string> dialogMessages, bool inBatch)
    {
        var savesHere = command.IsEdit && !inBatch;
        // For an edit, what the project already had is reported but does not fail the run.
        if (savesHere)
        {
            baseline = Collect(dialogMessages);
        }
        var elementsBefore = savesHere ? SerializeElements() : null;
        var projectLevelBefore = savesHere ? SerializeProjectLevel() : null;

        // Edits that can't name every element they change (rename, delete) save the whole project
        // (SaveProjectAndElements), which rewrites every element file. During an edit, that save's element files
        // are held back and their names recorded; afterwards only the elements whose model changed are written.
        // A save of one element (SaveElementAsync) still runs.
        var elementSavesHeld = new HashSet<string>();
        if (savesHere)
        {
            GluxCommands.ElementSaveFilter =name => { elementSavesHeld.Add(name); return false; };
        }
        try
        {
            if (command.Run != null)
            {
                command.Run(context).GetAwaiter().GetResult();
                TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();
            }

            if (savesHere)
            {
                GenerateAndSave(ChangedElements(elementsBefore!), projectLevelBefore!, context.HasFailed, elementSavesHeld,
                    codeRequested: null);
            }
        }
        finally
        {
            GluxCommands.ElementSaveFilter = null;
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
        return after;
    }

    static void RestoreAfterFailedBatch(CliBatchResult batch, string projectRoot, Dictionary<string, FileState> before)
    {
        batch.WriteErrors = RestoreFiles(projectRoot, before)
            .Select(file => new CliError("NotRestored", $"{file} was changed by a command and could not be put back."))
            .ToList();
    }

    static List<GlueElement> ChangedElements(Dictionary<GlueElement, string> elementsBefore) =>
        SerializeElements()
            .Where(pair => !elementsBefore.TryGetValue(pair.Key, out var jsonBefore) || jsonBefore != pair.Value)
            .Select(pair => pair.Key)
            .ToList();

    /// <summary>
    /// After an edit, or after a whole batch: regenerates the code of the changed elements and the code generation
    /// the batch held (<paramref name="codeRequested"/>), then writes what was left unsaved and the .csproj.
    /// <paramref name="failed"/> skips the codegen and every save except the changed elements in
    /// <paramref name="savesWhenFailed"/>.
    /// </summary>
    static void GenerateAndSave(List<GlueElement> changedElements, string projectLevelBefore, bool failed,
        HashSet<string> savesWhenFailed, ProjectWriteHold.Requests? codeRequested)
    {
        if (!failed)
        {
            // Headless, TaskManager.SynchronousMode runs the AddOrMoveToEnd codegen an edit queues inline,
            // before the rest of the edit's task, where the editor's queue runs it after. Plugin reactions
            // later in that task (NewNamedObjectPlugin making a new list ExposedInDerived) would then be
            // missing from the generated code, so regenerate every element the edit changed.
            var project = GlueState.Self.CurrentGlueProject;
            var toGenerate = changedElements
                .SelectMany(element => ObjectFinder.Self.GetAllElementsThatInheritFrom(element).Prepend(element))
                .Concat((codeRequested?.Elements ?? Array.Empty<GlueElement>())
                    .Where(element => project.Screens.Contains(element) || project.Entities.Contains(element)))
                .Distinct()
                .ToList();
            if (codeRequested?.AllCode == true)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
            }
            foreach (var element in toGenerate)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element, generateDerivedElements: false);
            }
            if (codeRequested?.GlobalContent == true)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCode();
            }
            TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();
        }

        // Edits save what they change themselves; this writes whatever one left unsaved, so no command can
        // forget a save: each changed element whose file differs from the model, and the .gluj when its own
        // data changed. A failed edit writes only the changed elements a held full save had asked for.
        if (SavesWhatTheEditLeftUnsaved)
        {
            GluxCommands.ElementSaveFilter = null;
            foreach (var element in changedElements.Where(item => !failed || savesWhenFailed.Contains(item.Name)))
            {
                _ = GluxCommands.Self.SaveElementAsync(element, TaskExecutionPreference.Fifo, onlyIfChanged: true);
            }
            if (!failed && SerializeProjectLevel() != projectLevelBefore)
            {
                GlueCommands.Self.GluxCommands.SaveGlujFile();
            }
            TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();
        }
        if (!failed)
        {
            GlueCommands.Self.ProjectCommands.SaveProjects();
            TaskManager.Self.WaitForAllTasksFinished().GetAwaiter().GetResult();
        }
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
    /// False skips RunStep's save of what an edit left unsaved, so a test sees only what the editor's own
    /// code wrote.
    /// </summary>
    internal static bool SavesWhatTheEditLeftUnsaved { get; set; } = true;

    /// <summary>What the .gluj itself holds: the project without its elements, plus the element names.</summary>
    internal static string SerializeProjectLevel()
    {
        var project = GlueState.Self.CurrentGlueProject;
        var settings = new Newtonsoft.Json.JsonSerializerSettings { ContractResolver = ProjectLevelContractResolver.Instance };
        return Newtonsoft.Json.JsonConvert.SerializeObject(project, settings) + "\n" +
            string.Join("\n", project.Screens.Select(item => item.Name).Concat(project.Entities.Select(item => item.Name)));
    }

    sealed class ProjectLevelContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        public static readonly ProjectLevelContractResolver Instance = new();

        protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(System.Reflection.MemberInfo member,
            Newtonsoft.Json.MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (member.DeclaringType == typeof(GlueProjectSave) &&
                member.Name is nameof(GlueProjectSave.Entities) or nameof(GlueProjectSave.Screens))
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
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
        ".gluj", ".glej", ".glsj", ".glux", ".csproj", ".cs", ".json", ".xml",
    };

    /// <param name="Content">The bytes of a hashed file, kept by a batch so <see cref="RestoreFiles"/> can put it back.</param>
    sealed record FileState(DateTime WriteTime, string? Hash, byte[]? Content);

    static Dictionary<string, FileState> SnapshotFiles(string root, bool keepContent) =>
        EnumerateProjectFiles(root).ToDictionary(file => file, file =>
        {
            var content = ReadIfText(file);
            return new FileState(File.GetLastWriteTimeUtc(file), content == null ? null : Convert.ToHexString(SHA256.HashData(content)),
                keepContent ? content : null);
        }, StringComparer.OrdinalIgnoreCase);

    static bool HasChanged(string file, FileState state) =>
        state.WriteTime != File.GetLastWriteTimeUtc(file) && (state.Hash == null || state.Hash != HashIfText(file));

    static List<string> ChangedFiles(string root, Dictionary<string, FileState> before) =>
        EnumerateProjectFiles(root)
            .Where(file => !before.TryGetValue(file, out var state) || HasChanged(file, state))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Puts the project folder back as <paramref name="before"/> found it, for a failed batch: some edits write files
    /// as part of the edit itself (a new entity's custom code file, a rename moving files), outside the saves and code
    /// generation <see cref="ProjectWriteHold"/> holds. Deletes files the batch created and rewrites the ones it changed
    /// or deleted, with their old write times.
    /// </summary>
    /// <returns>Changed or deleted files whose old content was not kept (not text), relative to <paramref name="root"/>.</returns>
    static List<string> RestoreFiles(string root, Dictionary<string, FileState> before)
    {
        foreach (var file in EnumerateProjectFiles(root).Where(file => !before.ContainsKey(file)).ToList())
        {
            File.Delete(file);
        }
        var notRestored = new List<string>();
        foreach (var (file, state) in before)
        {
            if (File.Exists(file) && !HasChanged(file, state))
            {
                continue;
            }
            if (state.Content == null)
            {
                notRestored.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllBytes(file, state.Content);
            File.SetLastWriteTimeUtc(file, state.WriteTime);
        }
        return notRestored;
    }

    static string? HashIfText(string file)
    {
        var content = ReadIfText(file);
        return content == null ? null : Convert.ToHexString(SHA256.HashData(content));
    }

    static byte[]? ReadIfText(string file)
    {
        if (!HashedExtensions.Contains(Path.GetExtension(file)))
        {
            return null;
        }
        try
        {
            return File.ReadAllBytes(file);
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
        WriteSection(stdout, "Write errors", batch.WriteErrors.Select(error => $"{error.Type}: {error.Details}"));
        WriteSection(stdout, "Files written", batch.FilesWritten);
        WriteChanges(stdout, batch.Changes);
        var ran = batch.Results.Count;
        var failedLines = batch.Results.Select((result, index) => (result, index))
            .Where(item => item.result.ExitCode != ExitSuccess)
            .Select(item => (item.index + 1).ToString())
            .ToList();
        stdout.WriteLine(failedLines.Count == 0
            ? $"Ran all {ran} command(s), exit code {batch.ExitCode}."
            : ran == batch.CommandCount
                ? $"Command(s) {string.Join(", ", failedLines)} of {ran} failed, so nothing was written. Exit code {batch.ExitCode}."
                : $"Stopped after command {ran} of {batch.CommandCount} failed, so nothing was written. Exit code {batch.ExitCode}. " +
                  "--keep-going runs the rest to report every failure.");
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
    /// <summary>
    /// Each command's result, in order. Its errors are the ones it added; its filesWritten is empty, since the
    /// batch writes once at the end (<see cref="FilesWritten"/>).
    /// </summary>
    public List<CliResult> Results { get; set; } = new();
    /// <summary>
    /// Files the batch's one save and code generation wrote, relative to the .csproj folder. Empty when a command
    /// failed: then the batch writes nothing.
    /// </summary>
    public List<string> FilesWritten { get; set; } = new();
    /// <summary>
    /// Errors, error output (type ErrorOutput) and dialogs (type DialogMessage) the final save and code generation
    /// added. After a failed batch, files a command changed that could not be put back (type NotRestored).
    /// </summary>
    public List<CliError> WriteErrors { get; set; } = new();
    /// <summary>How long the final code generation and save took, included in <see cref="ElapsedSeconds"/>.</summary>
    public double WriteSeconds { get; set; }
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
