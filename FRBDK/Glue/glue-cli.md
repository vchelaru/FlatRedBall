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

- One invocation loads the project, runs one command, waits for queued tasks, saves, regenerates code,
  and exits. Batch mode (several commands per load) comes only if load time makes single commands too
  slow.
- Exit code 0 means the edit applied and left no new errors. 1 means the edit was not applied (bad
  input, a Glue rule it breaks) or it added errors. 2 means bad arguments or a project that would not
  load. Errors the project had before the edit are reported as `preexistingErrors` and do not fail the
  run, so an agent can edit a project that already has problems.
- Output is plain text by default and JSON with `--json`, so agents can parse it.
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

- What happens when the CLI saves a project that Glue's editor has open? The editor's file watch should
  reload it, but that is unverified, and the editor may have unsaved changes. The CLI may need to detect
  an open editor and refuse or warn.
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
| 3. Edit commands | In progress | add-object: #2472, set-variable: #2474, add-file: #2473 |
| 4. Agent skill | Not started | |
| 5. Extraction | Ongoing, separate from the CLI | |
