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

- [x] **Extract `ITreeViewDisplay` interface from `MainTreeViewControl`** ✅ 2026-03-04
- [x] **Decouple `NodeViewModel` from `SelectionLogic`**: Replace direct `SelectionLogic.Current.HandleSelected/HandleDeselection` calls in `NodeViewModel` with a delegate or event. The `SelectionLogic` instance (or plugin) would subscribe. This removes the `Logic` → `ViewModel` → `Logic` circular coupling. ✅ 2026-03-04
- [x] **Inject `GlueState`/`GlueCommands` into `MainTreeViewViewModel`**: Introduce constructor parameters or a context object so the ViewModel does not call static singletons directly. Enables unit testing of search and refresh logic. ✅ 2026-03-04
- [x] **Unit tests for tree-navigation methods in `Search.cs`**: `TreeNodeByDirectory`, `TreeNodeForDirectoryOrEntityNode`, `TreeNodeForDirectoryOrScreenNode`, `GetTreeNodeByQualifiedPath`, `GetTreeNodeByTag`, and `IsInTreeView` are all pure tree-structure traversal with no static dependencies — 30 `[WpfFact]` tests added in `SearchNavigationTests`. ✅ 2026-03-04
- [ ] **Extract search logic into a `TreeSearchService`**: `MainTreeViewViewModel.Search.cs` still calls `ObjectFinder.Self.GetAllReferencedFiles()`, `FileManager.*`, and `ProjectManager.*` in `RefreshFlattenedList`. Extracting this method into a service class and wrapping those statics behind an interface would unlock unit tests for the filtered search list logic.

## RightClickHelper — Architecture and Refactoring Plan

### Current state (2026-03-04)

`RightClickHelper` is a `public static` class (~1,500 lines) in the `FlatRedBall.Glue.FormHelpers` namespace,
but physically lives in `OfficialPlugins/TreeViewPlugin/Logic/`. This namespace/location mismatch reflects
organic growth — it was written as a shared helper but is tightly coupled to the TreeViewPlugin.

#### Identified problems

1. **Static God Object** — all `static`, no injection points. Impossible to unit-test as-is.

2. **`GlueState.Self` / `GlueCommands.Self` called directly** inside `PopulateRightClickMenuItemsShared`
   — prevents mocking for tests.

3. **Pre-created items in `Initialize()`** — items like `mMoveToTop`, `mDuplicate`,
   `addObjectToolStripMenuItem` are created once and reused across all menu invocations (WinForms artifact).
   A WPF ViewModel design would use `ICommand` bindings instead.

4. **Decision logic and UI construction are entangled** — `PopulateRightClickMenuItemsShared` decides
   which items to show AND creates WinForms objects in the same pass. No way to test "which items appear
   for this node type" without standing up the full editor.

5. **Wrong namespace** — `FlatRedBall.Glue.FormHelpers` but lives inside the TreeViewPlugin. Should
   eventually move to `OfficialPlugins.TreeViewPlugin.Logic`.

### Suggested refactoring path (in order)

- [x] **Extract `GetItemDescriptors`** ✅ 2026-03-04 — See completed refactors below.

- [ ] **Delete dead `AddRemoveFromProjectItems()`** — now unreachable private static method left after
  the extraction. Safe to delete; remove it in a small cleanup PR.

- [ ] **Migrate pre-created items to `ICommand`** — the pre-created `GeneralToolStripMenuItem` fields
  from `Initialize()` should become `ICommand` properties on a future `RightClickViewModel`. This removes
  the WinForms pre-allocation pattern entirely.

- [ ] **Inject `IObjectFinder`** — `GetItemDescriptors` still calls `ObjectFinder.Self` in the
  `IsNamedObjectNode` branch (abstract-entity list check). Wrap behind an interface to make that branch
  testable.

- [ ] **Move into the plugin and rename** — change namespace to `OfficialPlugins.TreeViewPlugin.Logic`,
  rename to `RightClickLogic` or `RightClickService`, make it an instance class injected into
  `MainTreeViewPlugin`.

- [ ] **Inject `IGlueCommands`** — `GetItemDescriptors` currently takes a `Func<ReferencedFileSave, bool>
  fileExists` delegate as a workaround for the file-existence check. Once `IGlueCommands` is injected,
  replace the delegate with a direct call.

### `RightClickItemDescriptor` design

The descriptor type (`OfficialPlugins.TreeViewPlugin.Logic.RightClickItemDescriptor`) handles three cases:

| Source | Fields used |
|--------|-------------|
| Fresh item (`Add(text, action)`) | `Text`, `Handler`, optionally `Image` |
| Pre-created item (`AddItem(mMoveToTop)`) | `Text` (may update the item's text), `PreCreatedItem` |
| Separator (`AddSeparator()`) | `IsSeparator = true` |
| Sub-menu (`Copy Name...`) | `Text`, `Handler`, `SubItems` |

Tests use `descriptor.Text` to assert which items would appear. Tests do NOT call
`RightClickHelper.Initialize()`, so `PreCreatedItem` will be `null` in test scenarios — but `Text` is
always set explicitly via `item?.Text ?? fallbackText` so assertions still work.

### What is testable after the extraction (2026-03-04)

- Which items appear for each node type (EntityRootNode, ScreenRootNode, EntityNode, ScreenNode, DirectoryNode, etc.)
- Conditional items (e.g., FileVersion-gated items, GameScreen-specific items, PooledByFactory items)
- The new right-click command added after this refactor

### What is NOT yet testable

- Handler correctness (lambdas still call `GlueCommands.Self`, `GluxCommands.Self`, `SelectionLogic.Current`, etc.)
- The `IsNamedObjectNode` abstract-entity list check (calls `ObjectFinder.Self`)
- File-existence check in `IsReferencedFile` (workaround: pass `fileExists` delegate in production, `null` in tests → item never shown in tests)

---

## Reference-Finding — Architecture and Refactoring Plan

### Current state (2026-03-04)

Reference-finding logic ("who uses X?") is scattered across at least seven locations with no single
authoritative service. The closest thing to a hub is `ObjectFinder`, but it is a large catch-all class
that also handles element lookup by name/type, making it hard to test or inject.

#### Locations with reference-finding logic

| Location | What it finds |
|----------|--------------|
| `Glue/Elements/ObjectFinder.cs` | Inheritance chains, named-object usages, file references, variable type usages — the primary hub |
| `Glue/Controls/ElementReferenceListWindow.xaml.cs` | Inline CSV/variable reference logic duplicated outside of ObjectFinder |
| `Glue/Managers/InheritanceManager.cs` | Calls reference-finding methods to propagate base-element changes to all derived elements |
| `Glue/SaveClasses/IElementExtensionMethods.cs` | File-reference extension methods (`GetReferencedFileSaveRecursively`, `GetAllReferencedFileSavesRecursively`, etc.) |
| `Glue/Managers/FindManager.cs` | Interface/base: `IfReferencedFileSaveIsReferenced()` |
| `OfficialPlugins/TreeViewPlugin/Logic/FindManager.cs` | Implementation of the above — relationship unclear |
| `Glue/Managers/FileReferenceManager.cs` | Manager-level file→element reference tracking |
| `GumPlugin/GumPlugin/Managers/FileReferenceTracker.cs` | Plugin-specific file reference tracking, may overlap with core |

#### Identified problems

1. **No single `IReferenceService`** — callers reach into `ObjectFinder.Self` (static singleton) directly.
   Impossible to inject or mock.

2. **`ElementReferenceListWindow` contains business logic** — the CSV/variable reference lookup in
   `PopulateWithReferencesTo(ReferencedFileSave)` is inline in the UI class rather than delegated to a
   service. The window should only display results.

3. **Two `FindManager` classes** — one in `Glue/Managers/` and one in `OfficialPlugins/TreeViewPlugin/Logic/`.
   The relationship (interface vs. implementation? duplication?) is unclear and should be resolved.

4. **`ObjectFinder` has too many responsibilities** — element lookup, file lookup, inheritance traversal,
   named-object search, and variable-type search all live in one 1,000+ line class. The reference-finding
   group should be separated from the element-lookup group.

5. **`FileReferenceManager` and `ObjectFinder` file methods may overlap** — it's unclear whether
   `FileReferenceManager` is a cache/index on top of `ObjectFinder` or an independent parallel
   implementation.

### Suggested refactoring path (in order)

- [x] **Audit and document `ObjectFinder` method groups** ✅ 2026-03-04 — See inventory below.

#### `ObjectFinder` method inventory (2026-03-04)

62 methods total. Grouped by responsibility:

**Element Lookup** (15) — find a screen/entity/named object/variable/state by name or type
`GetAllElements`, `GetEntitySave` ×2, `GetElementUnqualified`, `GetElementsUnqualified`,
`GetEntitySaveUnqualified`, `GetScreenSave`, `GetScreenSaveUnqualified`, `GetIElement` (obsolete alias),
`GetElement` ×2, `DoesEntityExist`, `GetStateSaveCategory` ×2, `GetElementDefiningStateCategory`

**Reference Finding** (10) — "who uses X?"
`GetReferencedFileSavesFromSource`, `GetAllReferencedFiles`, `GetMatchingReferencedFiles`,
`GetAllElementsReferencingFile`, `GetAllNamedObjects`, `GetAllNamedObjectsThatUseElement`,
`GetAllNamedObjectsThatUseEntity` ×2, `GetAllNamedObjectsThatUseEntityAsVariableType`,
`GetVariablesReferencingElementType`

**Inheritance Traversal** (14) — walk up/down the inheritance chain
`GetAllEntitiesThatInheritFrom` ×2, `GetAllScreensThatInheritFrom` ×2,
`GetAllElementsThatInheritFrom` ×2, `GetIfInherits`, `GetRootBaseElement`, `GetBaseElement`,
`GetBaseElementRecursively`, `GetAllBaseElementsRecursively`, `GetAllDerivedElementsRecursive`,
`GetHierarchyDepth`, `GetInheritanceChain`

**Container / List Resolution** (14) — find what owns an object, or what list should hold it
`GetElementContaining` ×6 (overloads for NOS, RFS, EventResponse, StateSave, CustomVariable,
StateSaveCategory), `GetNamedObjectContainer`, `GetDefaultListToContain` ×3,
`GetPossibleListsToContain` ×3

**Variable / Property Resolution** (9) — get variable values and definitions
`GetNamedObjectFor`, `GetVariableContainer`, `GetBaseCustomVariable`, `GetRootCustomVariable` ×2,
`GetVariableDefinition`, `GetPropertyValueRecursively`, `GetValueRecursively` ×2

**File Resolution** (4) — path conversion and CSV class lookup
`MakeAbsoluteContent`, `GetFirstCsvUsingClass` ×2, `GetCustomClassFor`

**NamedObject Hierarchy** (1)
`GetRootDefiningObject`

**Private helpers** (3): `AddAllDerivedElementsRecursive`, `GetAllNamedObjectsThatUseElement` (overload),
`GetVariableOnInstance`

---

**Extraction target for `ReferenceService`**: the **Reference Finding** (10) and **Inheritance
Traversal** (14) groups — 24 methods. Everything else stays on `ObjectFinder`.

- [x] **Clarify the two `FindManager` classes** ✅ 2026-03-04 — `Glue/Managers/FindManager.cs`
  contained `IFindManager` (the interface) plus ~230 lines of entirely commented-out WinForms dead code.
  `OfficialPlugins/TreeViewPlugin/Logic/FindManager.cs` was the sole live implementation.
  Actions taken: deleted the dead class body, renamed the file to `IFindManager.cs`, restored the
  accidentally-omitted `GlobalContentFilesPath` property (used by `RuntimeDebuggingPlugin` but missing
  from the interface), and added the corresponding implementation to `FindManager`.
  `IFindManager` mixes two concerns (tree-node lookup and `IfReferencedFileSaveIsReferenced`) — the
  latter will eventually move to `IReferenceService`.

- [x] **Clarify `FileReferenceManager` vs. `ObjectFinder` file methods** ✅ 2026-03-04 — No overlap.
  They operate at different levels: `ObjectFinder` file methods query the **Glue project model** (which
  `ReferencedFileSave` objects exist, which elements reference a given RFS name); `FileReferenceManager`
  operates at the **disk/content level** (which content files does an asset file import on disk, cached by
  write time via `ContentParser` + plugin system). `TileGraphicsPlugin/Managers/FileReferenceManager` and
  `GumPlugin/Managers/FileReferenceTracker` are plugin providers that feed format-specific file
  dependencies into the core `FileReferenceManager` — not duplicates. The name collision between the
  Glue-core and TileGraphics managers is unfortunate but harmless (different namespaces, different
  responsibilities). No code changes required.

- [x] **Extract `ReferenceService`** ✅ 2026-03-04 — Created `Glue/Elements/ReferenceService.cs`.
  Moved 24 public methods (+ 2 private helpers) out of `ObjectFinder` into `ReferenceService`:
  - **File/RFS reference finding (4)**: `GetReferencedFileSavesFromSource`, `GetAllReferencedFiles`,
    `GetMatchingReferencedFiles`, `GetAllElementsReferencingFile`
  - **Named-object/entity reference finding (5)**: `GetAllNamedObjects`,
    `GetAllNamedObjectsThatUseElement`, `GetAllNamedObjectsThatUseEntity` ×2,
    `GetAllNamedObjectsThatUseEntityAsVariableType`
  - **Variable/type reference finding (1)**: `GetVariablesReferencingElementType`
  - **Inheritance traversal (14)**: `GetAllEntitiesThatInheritFrom` ×2,
    `GetAllScreensThatInheritFrom` ×2, `GetAllElementsThatInheritFrom` ×2, `GetIfInherits`,
    `GetRootBaseElement`, `GetBaseElement`, `GetBaseElementRecursively`,
    `GetAllBaseElementsRecursively`, `GetAllDerivedElementsRecursive`, `GetHierarchyDepth`,
    `GetInheritanceChain`

  `ReferenceService` reads `GlueProject` via `ObjectFinder.Self.GlueProject` and delegates
  `GetElement()` lookups back to `ObjectFinder.Self`. `ObjectFinder` retains thin one-line forwarding
  stubs for all 24 methods so all existing call sites continue to work unchanged.
  `ReferenceService.Self` is a static singleton accessor (same transitional pattern as `SelectionLogic`).
  Zero behavior change. Verified: `Glue.csproj` and `OfficialPlugins.csproj` compile with no C# errors.

- [x] **Move inline reference logic out of `ElementReferenceListWindow`** ✅ 2026-03-04 — Added four
  `GetReferencesTo` / `GetReferencesToElement` overloads to `ReferenceService` returning
  `IReadOnlyList<object>`, each bundling all reference types for a given subject:
  - `GetReferencesTo(ReferencedFileSave)` — owning-element RFS + file-sourced NOS + CSV custom variables
  - `GetReferencesToElement(IElement)` — NOS usages + inheritance + NextScreen links
  - `GetReferencesTo(NamedObjectSave, IElement)` — tunneling CustomVariables + derived-element NOS
  - `GetReferencesTo(CustomVariable, IElement)` — states that set it + derived variables + event responses

  `ElementReferenceListWindow` is now a pure display class: each `Populate*` method is a 3-line foreach
  over the service result. The window no longer imports `ObjectFinder` or contains any query logic.
  Zero behavior change. `Glue.csproj` builds with 0 errors.

- [ ] **Inject `IReferenceService` into consumers** — `InheritanceManager` and the tree-view/right-click
  code are the primary consumers. Inject via constructor so they can be unit-tested without a live
  `ObjectFinder`.

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

### 2026-03-04 — Extract `ITreeViewDisplay` from `MainTreeViewControl`

`SelectionLogic` previously took `MainTreeViewControl` (a concrete WPF `UserControl`) as a constructor parameter and called three things on it directly: `mainView.RefreshRightClickMenu()`, `mainView.MainTreeView.UpdateLayout()`, and `mainView.MainTreeView.ScrollIntoView()`. Additionally, `NodeViewModel` had a `Focus(MainTreeViewControl)` method that accessed `MainTreeView.ItemContainerGenerator` directly.

Extracted `ITreeViewDisplay` interface (`Views/ITreeViewDisplay.cs`) with four members:
- `RefreshRightClickMenu()` — already existed on `MainTreeViewControl`
- `ScrollNodeIntoView(NodeViewModel)` — wraps `MainTreeView.ScrollIntoView`
- `UpdateTreeViewLayout()` — wraps `MainTreeView.UpdateLayout`
- `FocusNode(NodeViewModel)` — focus logic moved here from `NodeViewModel.Focus(MainTreeViewControl)`

`MainTreeViewControl` implements `ITreeViewDisplay`. `SelectionLogic` now depends only on `ITreeViewDisplay`. `NodeViewModel.Focus(MainTreeViewControl)` deleted — its logic lives in `MainTreeViewControl.FocusNode`.

Added `SelectionLogicTests` with constructor/default-state/mock-wiring tests. `HandleDeselection` on an empty selection is the first test that validates `ITreeViewDisplay` is properly called (`RefreshRightClickMenu` on the mock).

**Remaining blocker for deeper `SelectionLogic` tests**: `RefreshGlueState` calls `GlueState.Self.CurrentTreeNodes` when selection changes (`forcePushToGlue = true`). Tests that invoke `HandleSelected` need either a real or mock `GlueState.Self` — this is addressed by the "Inject GlueState/GlueCommands" next step.

### 2026-03-04 — Extract `SearchMatcher` from `MainTreeViewViewModel`

`GetMatchWeight` and `CamelCaseMatchUpper` were local functions buried inside `RefreshFlattenedList`, capturing `searchTermCaseSensitive` and `searchToLower` from the outer scope. Extracted into `OfficialPlugins.TreeViewPlugin.Logic.SearchMatcher` as a standalone `internal static` class with explicit parameters.

Call sites in `RefreshFlattenedList` updated to `SearchMatcher.GetMatchWeight(name, SearchText)`.

Added `InternalsVisibleTo("GlueUnitTests")` to `OfficialPlugins.csproj` and wrote `SearchMatcherTests` covering all weight tiers (exact, case-sensitive prefix, case-insensitive exact, camel case, case-insensitive prefix, contains, no match) plus ordering invariants and the `isDefinedByBase` penalty.

### 2026-03-04 — Decouple `NodeViewModel` from `SelectionLogic`

`NodeViewModel.IsSelected` setter previously called `SelectionLogic.Current.HandleSelected` and `SelectionLogic.Current.HandleDeselection` directly, giving `NodeViewModel` a compile-time dependency on `SelectionLogic` (a `Logic` class depending on a `ViewModel`, and vice versa — circular coupling).

Replaced both calls with two `internal static` delegates on `NodeViewModel`:
- `NodeViewModel.NodeSelected` — `Action<NodeViewModel, bool, bool>` (node, focus, replaceSelection)
- `NodeViewModel.NodeDeselected` — `Action<NodeViewModel>`

`SelectionLogic` constructor now wires these delegates to `HandleSelected` and `HandleDeselection`. The `using OfficialPlugins.TreeViewPlugin.Logic;` import was removed from `NodeViewModel.cs`.

Added `InternalsVisibleTo("DynamicProxyGenAssembly2")` to `OfficialPlugins.csproj` (required for Moq to proxy the `internal ITreeViewDisplay` interface). Added two delegate-wiring tests to `SelectionLogicTests`: `IsSelected_True_InvokesNodeSelectedDelegate` and `IsSelected_False_InvokesNodeDeselectedDelegate`, which use spy lambdas to confirm the correct delegate fires without depending on `GlueState.Self`.

### 2026-03-04 — Extract `GetItemDescriptors` from `RightClickHelper`

`PopulateRightClickMenuItemsShared` was a ~500-line `private static` method that both decided which menu
items to show AND built the WinForms `GeneralToolStripMenuItem` objects. No seam existed between the
two concerns.

Extracted `internal static IReadOnlyList<RightClickItemDescriptor> GetItemDescriptors(ITreeNode,
IGlueState, MenuShowingAction, ITreeNode?, bool shiftHeld, Func<ReferencedFileSave, bool>? fileExists)`
into a new method. It contains the complete `if/else if` node-type decision chain and returns a list of
`RightClickItemDescriptor` values (see descriptor design in the plan section above).

`PopulateRightClickMenuItemsShared` is now a thin materialization loop that calls `GetItemDescriptors`
(passing `GlueState.Self`, `(Control.ModifierKeys & Keys.Shift) != 0`, and `rfs =>
GlueCommands.Self.GetAbsoluteFilePath(rfs).Exists()`) and converts each descriptor to a
`GeneralToolStripMenuItem`.

`GlueState.Self.CurrentGlueProject` calls inside the decision logic are replaced with the injected
`IGlueState` parameter. `GlueState.Self.CurrentElement` (collision relationships branch) likewise.

`AddRemoveFromProjectItems()` is now dead code — its logic was inlined into `GetItemDescriptors` as a
local `RemoveItems()` function.

Zero behavior change. Tests can now call `GetItemDescriptors` with a mock `IGlueState` and a real or
stub `ITreeNode` to assert which items appear for a given node type.

### 2026-03-04 — Unit tests for tree-navigation methods in `Search.cs`

Added `SearchNavigationTests` (30 `[WpfFact]` tests) covering the pure tree-navigation methods in `MainTreeViewViewModel.Search.cs`:

- `TreeNodeByDirectory` — direct child, nested path, missing intermediate, case-insensitive matching, trailing slash
- `TreeNodeForDirectoryOrEntityNode` / `TreeNodeForDirectoryOrScreenNode` — empty path returns root, non-empty delegates to directory search
- `GetTreeNodeByQualifiedPath` — entity/screen/global content roots, child lookup, deep nesting, not-found returns null, unknown root throws `InvalidOperationException`
- `GetTreeNodeByTag` — tag in entity/screen/global content tree, nested tag, tag not found
- `IsInTreeView` — nodes under each of the three roots, deep nesting, orphan node returns false

These methods have no static singleton dependencies — all they touch is `_glueState`/`_glueCommands` (mocked) and `NodeViewModel` tree structure built directly in each test.

**Still blocked** (requires `ObjectFinder.Self` and `FileManager.*` wrappers): `RefreshFlattenedList` — covered by the next step below.

### 2026-03-04 — Inject `IGlueState`/`IGlueCommands` into `MainTreeViewViewModel` and `SelectionLogic`

`MainTreeViewPlugin` is the composition root and the only class now allowed to call static singletons. All lower-level classes receive their dependencies via constructor injection.

**`MainTreeViewViewModel`**: Added `IGlueState glueState` and `IGlueCommands glueCommands` constructor parameters (stored as `readonly` fields `_glueState` / `_glueCommands`). All `GlueState.Self.*` and `GlueCommands.Self.*` calls in `Refresh.cs` and `Search.cs` replaced with field access. `MainTreeViewPlugin` field initializer moved to `StartUp()` so `GlueState.Self` is available at construction time.

**`SelectionLogic`**: `GlueState.Self.CurrentTreeNodes = ...` (the one singleton call remaining) replaced with an `Action<IReadOnlyList<ITreeNode>> reportSelection` callback injected via constructor. `MainTreeViewPlugin` passes `nodes => GlueState.Self.CurrentTreeNodes = nodes`. Tests pass a no-op spy lambda — `SelectionLogicTests` no longer needs any reference to `GlueState`.

**Still static** (left for future steps): `ProjectManager.*`, `ObjectFinder.Self`, `FileManager.*` — these have no direct `IGlueState`/`IGlueCommands` equivalent and require more effort to wrap.
