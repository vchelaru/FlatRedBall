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
