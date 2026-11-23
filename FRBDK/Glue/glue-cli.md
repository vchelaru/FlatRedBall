# GlueCLI direction

GlueCLI is a command-line tool that makes the same edits Glue's editor makes. It is meant for AI
agents. Agents can already edit `.gluj`/`.glej`/`.glsj` by hand, but they often stop and ask a person
to do it, because they don't know Glue's editing rules or can't tell whether an edit broke anything. A
CLI that applies an edit through Glue's own code, with plugins reacting, and then reports errors lets
an agent edit with confidence. Gum has the same tool (GumCLI), built on a shared editing library.

This file is the plan across many sessions. Update the status table as work lands.

## Shape

GlueCLI hosts the real Glue headless. It is a `net8.0-windows` console app that references Glue.dll
and the plugins, starts Glue the way the editor does with no window, and runs each command through the
same `GluxCommands`/`ElementCommands`/`GlueCommands` calls the UI makes. Plugins react to edits exactly
as they do in the editor, and code generation runs.

This is possible because headless Glue already exists for the tests: `GlueTestBootstrap`
(`Tests/GlueUnitTests/TestSupport`) runs the production startup without a window, and the gold-project
tests load real projects with real plugins registered and `GlueGui.ShowGui = false`. WPF in plugins is
not the obstacle as long as no view is shown. The obstacles are modal dialogs and UI-thread marshalling,
and the bootstrap already handles marshalling (`TaskManager.UiThreadMarshaller`, `SynchronousMode`, no
WinForms sync context).

Moving edit logic into `GlueCommon` (plain `net8.0`, see the `glue-common-extraction` skill) continues
in parallel. It is a cleanup that makes logic testable and portable, not something the CLI waits on.

We are deliberately not refactoring all of Glue first. Every step below ships something an agent can
use, and the order can change once we see what agents actually reach for.

Every step is its own small PR merged into `NetStandard`, never a long-lived feature branch. Glue is in
daily use while this happens, so each merge must leave the editor working and gets tested in real use
before the next step builds on it.

## Command rules

- One invocation loads the project, runs one command, waits for queued tasks, and exits. `batch` runs
  several commands, one per line of a file or stdin, against one load. Each line goes through the same
  per-command step as a single run (`GlueCliRunner.RunStep`), so a command behaves the same either way.
- Loading reads the project and nothing else: `ProjectLoader.LoadProject` with `ProjectLoadScope.ModelOnly`.
  What the editor's open does on top (plugin project code, generate all code, save what the load fix-ups
  changed, `.csproj` fix-ups, the unreferenced-file list) is `ProjectLoader.Operations`
  (`ProjectLoadOperations`), and a command calls the steps it needs: `regen` the generate-all steps,
  `delete` the unreferenced-file list, edit commands nothing (the runner regenerates and saves what the
  edit changed). Plugin `ReactToLoadedGlux` handlers set up the plugin's model and UI only; a plugin's
  project-level output (Gum runtimes, Tiled classes, camera setup, file aliases, compiler settings) is its
  `GenerateProjectCode` handler, raised by `GeneratePluginProjectCode`. So `check` writes nothing, even on
  a fresh clone. Edits do not raise it: project-level code comes from the Gum project, Tiled's embedded
  classes and the project's settings, which no edit command changes. What an edit does change reaches
  plugins through their own reactions (Tiled rewrites its object types when an entity changes) and through
  the element's regenerated code (a Gum object's code is part of its screen's). A fresh clone has no `*.Generated.cs`, and data changed outside Glue
  leaves generated code stale, so the help tells agents: run `regen` once after cloning or pulling, or
  whenever generated code is out of date; edit commands regenerate only what they change.
- Every command evaluates the `.csproj` with MSBuild, because the model load and `check`'s errors read its
  items. As in Glue.exe, the host starts that evaluation and the `.gluj` read (`ProjectLoader.StartEarlyReads`)
  before the plugin scan, so they cost almost nothing on top of startup. The Gum plugin reads the `.gumx`
  and its element files the same way from its `StartUp` (`GumProjectPrefetch`). The Gum project itself
  always loads: every command reports the project's errors, and Gum's error reporter reads all of it.
- Exit code 0 means the edit applied and left no new errors. 1 means the edit was not applied (bad
  input, a Glue rule it breaks) or it added errors. 2 means bad arguments or a project that would not
  load. Errors the project had before the edit are reported as `preexistingErrors` and do not fail the
  run, so an agent can edit a project that already has problems. When an edit is not applied, the
  project's changes are not saved.
- Output is plain text by default and JSON with `--json`, so agents can parse it.
- GlueCli works with the editor open on the same project: the editor's file watcher reloads what the CLI
  saved.
- `--dry-run` works on every command with no per-command code. The runner copies the .csproj folder to a
  temp folder (`CliDryRunCopy`), runs the command there in full, and reports the files it would change,
  with a diff of each JSON file. Art, audio and binaries are hard-linked, and each link is held open
  without write sharing for the run, so a write through one fails instead of reaching the project.
- A command never shows a dialog. Every MessageBox or yes/no prompt on a command's path goes through a
  dialog seam. In CLI mode the seam either answers from an explicit flag (for example `--overwrite`) or
  fails with a message naming the question it would have asked. Fix these one command at a time, on the
  path the command actually takes. Messages sent to `GlueGui.ShowMessageBox` while `ShowGui` is off are
  reported too (`GlueGui.SuppressedMessageHandler`), and count as failures.
- Edits call the `GluxCommands`/`ElementCommands` method the UI's dialog ends in, not the dialog. What
  the dialog checks before that call (names through `NameVerifier`, recursion, list placement) the
  command checks itself, before changing anything, and fails with a message that lists close matches.
- Each command gets a test that runs it against a temp copy of a sample project and checks the saved
  JSON and generated code.

### Writing a command

`AddObjectCommand` is the pattern. A command is a `CliCommand` in `GlueCli/Commands/<Name>Command.cs`,
listed once in `GlueCliRunner.Commands`:

- `Options` declares every option; the runner parses `--name value`, `--name=value` and flags, rejects
  unknown ones, and builds `GlueCli <command> --help` from `Usage`, `Description`, `Options` and
  `Examples`. Agents in other repos learn the CLI only from this help, so give real examples and say
  what `--json` puts in `edit`.
- `ValidateArguments` checks option combinations before Glue starts (exit 2 with the help).
- `IsEdit = true` makes the runner record the errors present after load, regenerate code for every
  element the edit changed (headless `TaskManager.SynchronousMode` runs an edit's queued codegen before
  the plugin reactions that follow it in the same task), and save the .gluj, element files and .csproj
  through `GluxCommands.SaveProjectAndElements` after `Run`.
- `Run` resolves names with `CliLookup` (elements, close matches), sets `GlueState.CurrentElement` to
  the target, calls `context.Fail(message)` and returns on bad input, applies the edit with
  `selectNewNos: false` where the call takes it, and calls `context.Report(data, text)`.
- A question the edit asks is answered from a flag by wrapping the call in `CliDialogs` (see
  `RenameCommand`), which can also take informational messages the command reports itself. Dispose it
  after `TaskManager.Self.WaitForAllTasksFinished()`; whatever it does not claim still fails the run.
- Tests go in `Tests/GlueUnitTests/GlueCli/<Name>CommandTests.cs` through `GlueCliTestRun`.

## Steps

1. **Shipped headless host.** Move `GlueTestBootstrap`'s production init into a `GlueHeadlessHost` in
   Glue. The tests and the CLI both use it, so there is one headless startup path. Test-only fakes stay in
   the test project.
2. **`check` and `regen`.** `check` loads the project and reports errors. `regen` also regenerates all
   code. These pay off before any edit command exists: an agent can hand-edit the JSON and then get proof
   the edit is valid.
3. **Edit commands, one per PR.** Starting order: add-object, set-variable, add-entity/add-screen,
   add-file, rename, delete. Reorder based on what agents ask for.
4. **Agent guidance.** A skill that tells agents GlueCLI exists, how to call it, and when to prefer it
   over hand-editing. Ship it with the first edit command and extend it with each later one.
5. **Ongoing extraction.** When a command's core logic moves into `GlueCommon`, that command no longer
   needs Glue.dll. Do this where it buys faster tests or less coupling, not as a goal of its own.

## Cross-platform

The headless CLI stays Windows-only, because Glue.dll targets `net8.0-windows` with WinForms and WPF.
Extraction (step 5) is what moves toward other platforms: once every CLI command runs on `GlueCommon`
plus plugin logic without Glue.dll, the CLI itself can ship on Linux and Mac. The count of commands that
don't need Glue.dll is the progress measure. A non-Windows Glue editor would also need each plugin split
into logic and UI, plus a cross-platform UI toolkit. This plan does not cover either one.

## Open questions

- How it ships: alongside Glue.exe in the release, as a dotnet tool, or both. It already builds into
  Glue's output folder, so a release zip of that folder carries `GlueCli.exe`.

## Where the project lives

`FRBDK/Glue/GlueCli/GlueCli.csproj`, in `Glue with All.sln`. It builds into Glue's own output folder
(`Glue/bin/<Configuration>/`), so `Content/ContentTypes.csv`, the `Plugins` folder the plugin projects copy
themselves into, and every dependency resolve exactly as they do for Glue.exe. `GlueCliRunner` holds the
commands; tests call it in-process with their own `IGlueCliHost`.

## Status

| Step | State | PR |
| --- | --- | --- |
| 1. Headless host | Done | #2470 |
| 2. `check` / `regen` | Done | #2471 |
| 3. Edit commands | In progress | add-object: #2472, set-variable: #2474, add-file: #2473, add-entity/add-screen: #2475, rename: #2477, delete: #2478, set-property: #2483, add-state-category/add-state/set-state-variable/rename-state/delete-state: #2486, add-variable/set-variable-property/remove-variable: #2487, add-event/remove-event: #2488, rename-state-category: #2490, describe (read-only): #2484, set-file-property: #2493, add-collision-relationship/set-collision-property: #2494, batch: this PR |
| 4. Agent skill | Not started | |
| 5. Extraction | Ongoing, separate from the CLI | |
