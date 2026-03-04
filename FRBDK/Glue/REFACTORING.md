# Glue Refactoring Guide

This document tracks the refactoring philosophy, process checklist, and progress for the FlatRedBall Editor (Glue).

## Philosophy

- **Incremental over big-bang**: Each PR should leave the code measurably better without rewriting everything at once.
- **Test before and after**: Write or identify unit tests that cover the area being touched before refactoring, then verify they still pass after.
- **Preserve behavior**: Refactoring should not change observable behavior. New features come after the refactor, not during.
- **Follow the seam**: When you find code that's hard to refactor, look for a natural seam to split it rather than forcing a restructure.

## Checklist for New Feature Work

1. [ ] Identify the area of code the feature will touch
2. [ ] Read and understand the existing code before modifying it
3. [ ] Identify any existing unit tests covering that area
4. [ ] Do an incremental refactor pass if the code is disorganized (separate commit/PR if possible)
5. [ ] Write unit tests for the new behavior before or alongside implementation
6. [ ] Implement the feature
7. [ ] Verify all existing tests still pass

## Plugin Architecture

Glue uses MEF (Managed Extensibility Framework) to separate functionality into plugins. There are two kinds:

- **External plugins** (e.g., Gum) — compiled as their own `.dll` and loaded at runtime.
- **Official plugins** — located in `OfficialPlugins/` (`OfficialPlugins.csproj`). Each plugin is its own subfolder within that project.

### Refactoring scope

Refactors should generally stay within a single plugin. Avoid reorganizing code across plugin boundaries unless there is a clear architectural reason.

### Cross-plugin method invocation (important)

Some plugins call methods on other plugins **by method name** (string-based invocation). This is not common, but it happens. As a result:

- Renaming public methods on a plugin can silently break other plugins that invoke them by name.
- Be especially careful around the **main plugin class** of any plugin — its public interface is the most likely target for cross-plugin calls.
- Before renaming or removing a public method, search the codebase for string references to that method name, not just symbol references.

## TreeViewPlugin — Refactoring Goals and Status

The `TreeViewPlugin` is a self-contained plugin that owns the explorer panel (Screens / Entities / Global Content). It is the most active UI area of the editor and has a number of architectural issues.

### Identified problems

1. **`SelectionLogic` was a static class with mutable static state** — initialized via `Initialize()` (poor-man's DI). Made testing impossible and hid the dependency graph. ✅ Fixed 2026-03-04.

2. **`MainTreeViewViewModel` accesses global singletons directly** — `GlueState.Self`, `GlueCommands.Self`, `ProjectManager.*`, `ObjectFinder.Self`, `FileManager` are all called directly throughout `Refresh.cs` and `Search.cs`. These should eventually be injected so the ViewModel can be unit-tested without standing up the full editor.

3. **`NodeViewModel` calls `SelectionLogic.Current` directly** — every node knows how to push its selection state out to Glue. A cleaner design would use an event/delegate: nodes raise `Selected`/`Deselected` events and `SelectionLogic` subscribes. This would decouple `NodeViewModel` from `SelectionLogic` entirely.

4. **`RightClickHelper` is a large static God Object in a different namespace** (`FlatRedBall.Glue.FormHelpers`) but is tightly coupled to the plugin. It's out of scope for plugin-internal DI but should eventually be extracted into the plugin as an instance class.

5. **`MainTreeViewViewModel` is still doing too much** — it holds tree root state, search/filtering logic, bookmark management, and directory refresh. The `Search.cs` partial and `Refresh.cs` partial should eventually become dedicated service classes injected into (or used alongside) the ViewModel.

### Suggested next steps (in order)

- [ ] **Extract `ITreeViewDisplay` interface from `MainTreeViewControl`**: `SelectionLogic` calls `mainView.RefreshRightClickMenu()`, `mainView.MainTreeView.UpdateLayout()`, and `mainView.MainTreeView.ScrollIntoView()`. Extracting these into an interface would allow mocking the view in tests and unlock testing of `HandleSelected`, `HandleDeselection`, and `SelectByTreeNode`.
- [ ] **Decouple `NodeViewModel` from `SelectionLogic`**: Replace direct `SelectionLogic.Current.HandleSelected/HandleDeselection` calls in `NodeViewModel` with a delegate or event. The `SelectionLogic` instance (or plugin) would subscribe. This removes the `Logic` → `ViewModel` → `Logic` circular coupling.
- [ ] **Inject `GlueState`/`GlueCommands` into `MainTreeViewViewModel`**: Introduce constructor parameters or a context object so the ViewModel does not call static singletons directly. Enables unit testing of search and refresh logic.
- [ ] **Extract search logic into a `TreeSearchService`**: `MainTreeViewViewModel.Search.cs` builds filtered lists from project data — pure logic with no UI concerns. Move to a separate class injected via constructor.

## Known Areas Needing Improvement

<!-- Add notes here as problem areas are identified -->

## Completed Refactors

### 2026-03-04 — Split `MainTreeViewViewModel` into partial class files

Split the 1,455-line class into 5 focused files:
- `MainTreeViewViewModel.cs` — core: search properties, fields/properties, bookmark properties, constructor
- `BookmarkViewModel.cs` — `BookmarkViewModel` class extracted from the same file it was nested in
- `MainTreeViewViewModel.Refresh.cs` — `RefreshTreeNodeFor`, `AddDirectoryNodes`, `AddEntityTreeNode`, `AddScreenTreeNode`, `Clear`, `DeselectResursively`, and related helpers
- `MainTreeViewViewModel.Collapse.cs` — `CollapseAll`, `CollapseToDefinitions`
- `MainTreeViewViewModel.Search.cs` — `RefreshFlattenedList`, all `GetTreeNodeBy*`/`TreeNodeFor*` lookup methods, `PushSearchToContainedObject`

Zero behavior change. Sets up future extraction of Search and Refresh into proper service classes.

### 2026-03-04 — Split `NamedObjectVariableShowingLogic` into partial class files

Split the 954-line static God Object into 5 focused partial class files:
- `NamedObjectVariableShowingLogic.cs` — public API: `UpdateShownVariables`, `UpdateConditionalVisibility`, `GetIfNeedsFullRefresh`
- `NamedObjectVariableShowingLogic.Definitions.cs` — `GetVariableDefinitions` (ATI vs Entity two-path resolver)
- `NamedObjectVariableShowingLogic.Categories.cs` — category creation, sorting, subtext, `GetOrCreateCategoryToAddTo`
- `NamedObjectVariableShowingLogic.Filtering.cs` — `GetIfShouldBeSkipped` (per-ATI variable hide rules)
- `NamedObjectVariableShowingLogic.Members.cs` — grid item creators: Name, IsLocked, SourceName, variable items, MakeDefault

Zero behavior change. Sets up future conversion from static to instance-based class (see `// todo - make this not static:` in Categories file).

### 2026-03-04 — Convert `SelectionLogic` from static class to instance class

`SelectionLogic` was a static class with static mutable state (`mainViewModel`, `mainView`, `currentNodes`, `IsPushingSelectionOutToGlue`, etc.) initialized via `SelectionLogic.Initialize(vm, view)`.

Converted to a proper instance class:
- Constructor takes `MainTreeViewViewModel` and `MainTreeViewControl` (replacing `Initialize()`)
- All static members become instance members
- A `public static SelectionLogic Current { get; private set; }` accessor is set in the constructor, allowing `NodeViewModel`, `MainTreeViewControl`, and `RightClickHelper` to reach the instance without requiring it to be passed everywhere
- `MainTreeViewPlugin` stores the instance as a field (`selectionLogic`) and uses it directly; other callers use `SelectionLogic.Current`

Zero behavior change. The `Current` accessor is a transitional pattern — the remaining work is to decouple `NodeViewModel` from `SelectionLogic` entirely (see next steps above).

### 2026-03-04 — Extract `SearchMatcher` from `MainTreeViewViewModel`

`GetMatchWeight` and `CamelCaseMatchUpper` were local functions buried inside `RefreshFlattenedList`, capturing `searchTermCaseSensitive` and `searchToLower` from the outer scope. Extracted into `OfficialPlugins.TreeViewPlugin.Logic.SearchMatcher` as a standalone `internal static` class with explicit parameters.

Call sites in `RefreshFlattenedList` updated to `SearchMatcher.GetMatchWeight(name, SearchText)`.

Added `InternalsVisibleTo("GlueUnitTests")` to `OfficialPlugins.csproj` and wrote `SearchMatcherTests` covering all weight tiers (exact, case-sensitive prefix, case-insensitive exact, camel case, case-insensitive prefix, contains, no match) plus ordering invariants and the `isDefinedByBase` penalty.
