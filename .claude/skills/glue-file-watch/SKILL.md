---
name: glue-file-watch
description: Glue's editor-side file watcher — detects external edits to project/content files and reacts (reload glux, regen code, notify plugins). Triggers: FileWatchManager, ChangedFileGroup, UpdateReactor, IgnoreChangeOnFileUntil, IgnoreNextChangeOnFile, FileSystemWatcher, self-save suppression.
---

# Glue File Watch

Not to be confused with `OfficialPlugins\RuntimeFileWatcherPlugin` — that generates code so the *running game* watches its own content files at runtime. This skill is about Glue the editor watching disk for external changes (a user editing a `.achx` in a text editor, git checkout, another tool writing a file).

## Architecture / flow

`FRBDK\Glue\Glue\IO\`:

1. **`ChangedFileGroup.cs`** wraps one `FileSystemWatcher` (recursive) rooted at the project directory. Its `Changed/Created/Deleted/Renamed` handlers filter cheaply (`IsSkippedBasedOnTypeOrLocation`: `.vs/`, `bin/Debug/`, `*.generated.cs`, temp shader files) then queue survivors into a `ChangeInformation` bucket (`mChangedFiles` or `mDeletedFiles`).
2. **`FileWatchManager.cs`** is the static facade. `Flush()` drains the queue once `CanFlush` is true (debounced: 500ms of quiet since the last queued change), re-filters each file (`IsFileIgnoredBasedOnFileType`: outside project/content dir, `obj/`, `bin/`, `*.generated.cs`, tiled-session files), and spawns one `TaskManager` task per surviving file.
3. **`UpdateReactor.UpdateFile`** is what each task calls. It decides, in order: is this the `.glux`/`.gluj`/a Screen/Entity file (→ reload project), does it reference tracked content (→ refresh `ReferencedFileSave` + project membership), is it a `.cs` file (→ `PluginManager.ReactToChangedCodeFile`), otherwise is it project content (→ `PluginManager.ReactToChangedFile`).

Watched root: normally the folder containing the `.csproj`, but `FileWatchManager.UpdateToProjectDirectory`/`ShouldMoveUpForRoot` walks one level up when the content project shares a parent with the code project (old XNA-style split).

## Diffing & partial reload (glux/gluj/glsj/glej changes)

A changed `.glux`/`.gluj`, or an individual per-element `.glsj`/`.glej` file (`GetIfIsGlueProjectOrElementFile`), all funnel into the same `UpdateReactor.ReloadGlux()` — there's no separate "just this screen" path.

`ReloadGlux` loads the changed file into a *second*, throwaway `GlueProjectSave` and deep-diffs it against the in-memory one (`GlueProjectSaveExtensionMethods.ReloadUsingComparison`, using KellermanSoftware's `CompareLogic` with a handful of derived/volatile members excluded, e.g. `TypedMembers`, `RuntimeType`, `ImageWidth/Height`). It is **not** a text/property patch — it's a full object-graph comparison.

This classification is extracted into `UpdateReactor.BuildProjectDiffPlan` (pure/static, unit-tested in `Tests/GlueUnitTests/IO/UpdateReactorTests.cs`). For each reported difference, `GetElementFromObjectString`/`GetFileFromObjectString` regex-match the diff's property path (e.g. `Screens[3].SomeProperty`) down to its containing top-level `Screens[i]` / `Entities[i]` / `GlobalFiles[i]`. Any difference inside that element's subtree collapses to "this element changed": the whole `ScreenSave`/`EntitySave`/`ReferencedFileSave` object is swapped by reference, then only that element's code is regenerated. A top-level project property collapses the same way if it's listed in `DiffableTopLevelProperties` (currently just `StartUpScreen`) — see the matching `case` in `ReloadGlux`'s apply step to add another.

**Escape hatch to full reload:** any difference that doesn't collapse to an element, a global file, or a whitelisted property — e.g. a list `Count` change from an element being added/removed — aborts the loop and falls back to `ProjectLoader.Self.LoadProject()`, a full reload. Diffs resolved before the aborting one are still applied (redundant once the reload lands, but it's the pinned existing behavior).

## Gotchas

- **Self-save suppression: two APIs, prefer the time-based one.** `IgnoreChangeOnFileUntil(FilePath, TimeSpan?)` / `IgnoreNextChangeOnFile` (default 5s window) silently absorbs *any number* of OS events during the window — use this for every new call site where Glue is about to write a file itself. The older `IgnoreNextChangeOn` is counter-based (drops exactly N events) and only correct if you know the exact event count a save will produce, which varies by writer (in-place write = 1 `Modified`; atomic save = `Created`+`Renamed`+`Deleted`). Full rationale is in `ChangedFileGroup`'s class doc comment.
- **Deletes on `.glux`/`.gluj`/Screen/Entity extensions are dropped outright** (`extensionsToIgnoreDeletes`) — a project file vanishing is treated as destructive and must go through a deliberate flow, not the auto-reload queue. `Created`/`Renamed` on those same extensions *are* processed, since atomic-save editors (VS Code, Claude Code) end a save with a rename to the final name.
- **Filtering happens twice**: once cheaply at the watcher (`IsSkippedBasedOnTypeOrLocation`, before queuing) and again at flush (`IsFileIgnoredBasedOnFileType`, which also produces the `IgnoreReason` used to route `BuiltFile` changes to `PluginManager.ReactToChangedBuiltFile` instead of the normal `UpdateReactor` path).
- **Plugin authors hook `PluginManager.ReactToChangedFile` / `ReactToChangedCodeFile`**, not the `FileSystemWatcher` directly — implement the corresponding `PluginBase` method; `UpdateReactor` dispatches to it after Glue's own routing (project/glux reload, RFS refresh) has had first refusal.
- Reactions run as queued `TaskManager` tasks, not inline on the watcher callback thread — a burst of file events (e.g. git checkout) serializes through the task queue rather than reacting concurrently.
