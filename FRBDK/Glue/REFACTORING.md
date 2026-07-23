# Glue Refactoring Guide

This document tracks the refactoring philosophy, process checklist, and progress for the FlatRedBall Editor (Glue).

## Philosophy

- **Incremental over big-bang**: Each PR should leave the code measurably better without rewriting everything at once.
- **Test before and after**: Write or identify unit tests that cover the area being touched before refactoring, then verify they still pass after.
- **Preserve behavior**: Refactoring should not change observable behavior. New features come after the refactor, not during.
- **Follow the seam**: When you find code that's hard to refactor, look for a natural seam to split it rather than forcing a restructure.
- **Retyping a static `Xyz.Self` from a concrete class to an interface can silently change overload resolution** — a delegate-typed call site (e.g. `Self.Invoke((MethodInvoker)x)`) may have been resolving to a base-class overload only reachable through the concrete type; once `Self` is interface-typed, that overload disappears from resolution with no compiler warning pointing at the real cause. After this kind of retype, grep for delegate casts (`MethodInvoker`, `Action`, `Func`) at call sites of the retyped member and rebuild — don't assume a clean compile plus green tests caught every semantic shift.

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

- [x] **Delete dead `AddRemoveFromProjectItems()`** ✅ 2026-03-04 — unreachable private static method removed.

- [ ] **Migrate pre-created items to `ICommand`** — the pre-created `GeneralToolStripMenuItem` fields
  from `Initialize()` should become `ICommand` properties on a future `RightClickViewModel`. This removes
  the WinForms pre-allocation pattern entirely.

- [ ] **Inject `IObjectFinder`** — `GetItemDescriptors` still calls `ObjectFinder.Self.GetEntitySave` in the
  `IsNamedObjectNode` branch. The `GetAllDerivedElementsRecursive` call in that branch was migrated
  to `ReferenceService.Self` (2026-03-04). Full injection requires wrapping `GetEntitySave` behind an interface.

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

- [x] **Inject `IReferenceService` into consumers** ✅ 2026-03-04 — Created `IReferenceService` interface,
  converted `InheritanceManager` to an instance class with `IReferenceService` constructor injection,
  injected into `ElementReferenceListWindow` via constructor. See completed refactors below.

## Known Areas Needing Improvement

- **`ProjectCommands.UpdateFileMembershipInProject`** is a ~170-line method with no seams — it touches
  a concrete `VisualStudioProject`, `FileReferenceManager`, and `PluginManager` all inline. The
  `GlueState.Self` dependency was removed (see below), but `VisualStudioProject` remains the blocker to
  fully unit-testing this specific method (it's ~170 lines with several branches, more than the one call
  site `GlueUnitTests/TestSupport/TestVisualStudioProjectFactory` was built to unblock — see the
  "Unblock WizardProjectLogic AddGameScreen testing" entry below). Note that `VisualStudioProject`'s
  constructor requiring a live MSBuild `Project` is no longer an unconditional blocker: a real (not
  fake) `VisualStudioProject` can now be constructed in a unit test via a minimal, non-SDK-style `.csproj`
  written to a temp directory — see `TestVisualStudioProjectFactory`. Widening that factory's coverage to
  `UpdateFileMembershipInProject`'s branches (content vs. code items, synced projects, etc.) — rather than
  extracting an `IVisualStudioProject` seam — is the path forward when a feature next needs to touch this
  method.

- **`UpdateReactor.ReloadGlux`'s apply step** (swap the replaced Screen/Entity/GlobalFile into
  `ProjectManager.GlueProjectSave`, then call `GlueCommands.Self.RefreshCommands`/`UpdateCommands`/
  `GenerateCodeCommands`, or `ProjectLoader.Self.LoadProject` on full reload) still calls
  `GlueState.Self`/`GlueCommands.Self`/`ObjectFinder.Self`/`ProjectLoader.Self` directly. Unlike
  `ProjectCommands`, this isn't flagged as a DI-injection target: everything left in that method is
  side-effecting plumbing (refresh a tree node, trigger codegen, reload from disk), not decision logic.
  Injecting interfaces here would make the side effects mockable but wouldn't add any test coverage
  that matters — the actual decision (partial swap vs. full reload) was already extracted into the
  pure, dependency-free `BuildProjectDiffPlan` (see below), which is where the real seam was. Revisit
  only if a future feature needs to assert *which* Glue commands fire for a given file change, not just
  what gets computed.

## Completed Refactors

### 2026-07-23 — Real `FixNamedObjectCollisionType` coverage, Collision Plugin only (issue #1894 reopened)

Issue #1894 was reopened: closing it after the `IMainGlueWindow` PR was premature because everything routed
through `PluginManager.CallPluginMethod`/`CallPluginMethodAsync` (Gum, Tiled, Entity Input Movement,
Collision Plugin's `FixNamedObjectCollisionType`) is untested and silently no-ops in a test host instead of
running the real plugin behavior - `CallPluginMethod` iterates loaded plugins by friendly name and returns
null if none match, it does not throw. This entry covers the Collision-Plugin slice only, per the reopened
issue's scoping (Gum/Tiled/Entity-Input-Movement are separate follow-ups).

**No production code changes were needed.** `MainCollisionPlugin.FixNamedObjectCollisionType` (the method
`WizardProjectLogic.cs`'s two call sites, ~line 996/1033, reach via
`PluginManager.CallPluginMethod("Collision Plugin", "FixNamedObjectCollisionType", nos)`) was already a
one-line forwarder to the public static `CollisionRelationshipViewModelController.TryFixSourceClassType` -
the same "thin forwarder to a directly-callable static method" shape as the earlier
`MainCollisionPlugin.RegisterAssetTypes` extraction. Of the two seam shapes the issue proposed - (a)
register a real plugin instance with `PluginManager` so `CallPluginMethod`'s reflection lookup finds it, or
(b) call the already-extracted static method directly, bypassing `CallPluginMethod` for tests - (b) needed
zero extraction work, so it's what's used: `GlueUnitTests.CollisionPlugin.FixNamedObjectCollisionTypeTests`
calls `CollisionRelationshipViewModelController.TryFixSourceClassType` directly.

(a) was investigated and rejected: `PluginManager.CallMethodOnPlugin` always routes through
`PluginCommand(doOnUiThread: true)`, which reads the private static `PluginManager.mMenuStrip` - only ever
set by `MainGlueWindow`'s real startup (`PluginManager.ShareMenuStripReference`) - so a lightweight
test-registered plugin would still NRE there, and reaching a workaround (e.g. a headless `MenuStrip` whose
`Control.Invoke` executes without a live message loop) rests on unverified WinForms internals not worth the
risk for this scope.

The test drives the real `WizardProjectLogic.HandleAddGameScreen` path (`AddSolidCollision`/
`AddCloudCollision = true`) to get real `SolidCollision`/`CloudCollision` `TileShapeCollection`
`NamedObjectSave`s, then builds a collision-relationship `NamedObjectSave` referencing them via
`FirstCollisionName`/`SecondCollisionName` (mirroring what
`CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects` would produce - see the blocker
below for why that method isn't driven directly), and calls `TryFixSourceClassType` twice: once with
`CollisionType = BounceCollision` (asserts `SourceClassType` becomes
`CollidableVsTileShapeCollectionRelationship<...>`), then again after changing the property to
`PlatformerSolidCollision` (asserts it becomes `DelegateCollisionRelationship<...>` and differs from the
first result) - proving the recomputation is driven by the real `CollisionType` property read through
`AssetTypeInfoManager.GetCollisionRelationshipSourceClassType`, not a stub.

**Still not covered, and why (the real, deeper blocker found here):** `FixNamedObjectCollisionType`'s two
production call sites live inside `WizardProjectLogic.HandleAddPlayerInstance`, which is unreachable
end-to-end in a test host for reasons that have nothing to do with `FixNamedObjectCollisionType` itself:

1. The relationship `NamedObjectSave` (`"PlayerVsSolidCollision"`/`"PlayerVsCloudCollision"`) is created by
   `PluginManager.ReactToCreateCollisionRelationshipsBetween(playerList, collisionNos)`, a static event with
   no subscriber unless a real `MainCollisionPlugin` instance has run `StartUp`/`AssignEvents` - the same
   "would need real plugin loading" problem this entry's seam choice avoided.
2. Even with a subscriber, the handler
   (`CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects`) ends by calling
   `GlueCommands.Self.DialogCommands.FocusTab("Collision")`, which reads `PluginManager.TabControlViewModel`
   - `private set`, only ever assigned by a live WinForms `MainGlueWindow`. This NREs in a test host.

Point 2 is not new - it's the same blocker the "AvailableAssetTypes.Self.Initialize / Collision Plugin
asset-type test seam" entry below already flagged as "deliberately not attempted" for
`CreateCollisionRelationshipBetweenObjects`. It's also not Collision-Plugin-specific: `TabControlViewModel`
is a `PluginManager`-wide static, the same shape of problem `IMainGlueWindow` solved for
`MainGlueWindow.Self` - fixing it properly means an `ITabControlViewModel`-style seam across all of
`PluginManager`, a materially bigger refactor than "Collision Plugin only" scope. Left open; a future pass
extending the `IMainGlueWindow` pattern to `PluginManager.TabControlViewModel` would unblock this along with
every other `FocusTab`/tab-selection call site, not just this one.

### 2026-07-23 — Extract `IMainGlueWindow`, unblock `AddSolidCollision`/`AddCloudCollision` end-to-end (issue #1894 follow-up)

Follow-up to "AvailableAssetTypes.Self.Initialize / Collision Plugin asset-type test seam" directly below:
that entry's remaining blocker was `MainAddScreenPlugin.AddCollision` → `GluxCommands.AddNewNamedObjectToAsync`
always running with `updateUi: true` (no way to opt out) and unconditionally calling
`MainGlueWindow.Self.PropertyGrid.Refresh()` - `MainGlueWindow.Self` is only ever set by a live WinForms
window. This was the third time a `MainGlueWindow.Self.X` call site NRE'd in a test host and got patched
one member at a time (`DoOnUiThread`→`Invoke` in #1896, `SaveProjectAndElementsImmediately`→`HasErrorOccurred`
in #1898) - systemic fix this time: put `MainGlueWindow.Self` itself behind an interface, so every current
and future call site resolves through one seam.

- **`IMainGlueWindow`** (`Glue/Managers/IMainGlueWindow.cs`) covers every member any call site outside
  `MainGlueWindow.cs` touches through `MainGlueWindow.Self` - `Invoke`/`BeginInvoke`, `PropertyGrid`,
  `HasErrorOccurred`, `Close`, `IsDisposed`, `Text`, `Components`, `NumberOfStoredRecentFiles`,
  `SyncMenuStripWithTheme`, `TryGenerateImplicitWindowStylesFor`, plus `Width`/`Height` and
  `IWin32Window.Handle` (found only by also grepping for bare `MainGlueWindow.Self` passed *by value* -
  e.g. `var window = MainGlueWindow.Self; window.Width`, or `ShowDialog(MainGlueWindow.Self)` - a
  member-only grep for `MainGlueWindow.Self.<name>` misses these). `IMainGlueWindow : IWin32Window` alone
  fixed every `ShowDialog`/`Show`/`MessageBox.Show(MainGlueWindow.Self, ...)` call site (about a dozen)
  since `Control` already implements `IWin32Window` via `Handle`.
- **`MainGlueWindow : Form, IMainGlueWindow`** - two members were plain fields, which can't satisfy an
  interface property, so `PropertyGrid` and `HasErrorOccurred` became auto-properties (zero behavior
  change - same get/set semantics, just compiler-backed instead of directly declared).
- **`MainGlueWindow.Self`** changed from `public static MainGlueWindow Self { get; private set; }` to
  `public static IMainGlueWindow Self { get; internal set; }` - `internal` (not `public`) so only
  `GlueTestBootstrap` can swap it, and deliberately still starts `null` and is only ever assigned inside
  `MainGlueWindow`'s own constructor (`Self = this;`), unlike `GlueCommands.Self`/
  `TaskManager.UiThreadMarshaller`, which eagerly construct their default in the field initializer.
  `MainGlueWindow`'s constructor has real WinForms/WPF side effects (spins up a `System.Windows.Application`,
  calls `SetMsBuildEnvironmentVariable`), so eagerly constructing one at static-field-init time would be a
  real production behavior change, not the zero-behavior-change this pattern is supposed to guarantee -
  `Self` staying lazily-null until `Program.cs` constructs the real window preserves exactly today's
  lifecycle.
- **Two call sites broke from the interface-vs-concrete-class overload resolution difference, not scope
  gaps**: `UpdateReactor.cs` and `MainGumPlugin.cs` each called `MainGlueWindow.Self.Invoke((MethodInvoker)x)`.
  This compiled against the concrete `MainGlueWindow` type only because C# overload resolution for
  differently-*signed* methods sharing a name merges the derived type's overloads with the base type's
  (unlike same-signature hiding, which fully replaces) - so `(MethodInvoker)x` was silently resolving to
  the *inherited, untyped* `Control.Invoke(Delegate)`, not any of `MainGlueWindow`'s own four `Invoke`
  overloads. `IMainGlueWindow` isn't a `Control` and declares no `Invoke(Delegate)` overload, so that
  fallback path doesn't exist through the interface. Fixed by changing the cast from `(MethodInvoker)` to
  `(Action)` at both call sites - same delegate, now resolves to `IMainGlueWindow.Invoke(Action)`, the
  overload this was always meant to hit.
- **Three call sites needed the concrete `Form`/`ISynchronizeInvoke`, not `IMainGlueWindow`**: a WinForms
  `Timer.SynchronizingObject` (`ISynchronizeInvoke`) in `MainCompilerPlugin.cs` (×2, one live, one
  already-commented-out) and `ModalReportingService`'s constructor param, plus `Form.Owner` in
  `DialogCommands.SetFormOwner` and a `Form`-typed extension-method parameter in
  `MapTextureButtonContainer.xaml.cs`. Widening `IMainGlueWindow` to cover `ISynchronizeInvoke` (4 more
  members, colliding in name with the `Invoke`/`BeginInvoke` already declared) for two framework-interop
  call sites wasn't worth it - left as explicit casts (`(ISynchronizeInvoke)MainGlueWindow.Self`,
  `MainGlueWindow.Self is Form owner`) at the call site instead. In production `Self` is always the real
  `MainGlueWindow`, so these casts never fail; they're a narrower exception than the `IWin32Window`
  widening above because each has exactly one or two consumers instead of a dozen.
- **`FakeMainGlueWindow`** (`GlueUnitTests/TestSupport/`) - no-op/harmless implementation, wired into
  `GlueTestBootstrap.EnsureInitialized` (`MainGlueWindow.Self ??= new FakeMainGlueWindow()`).
  `Invoke`/`BeginInvoke` run the delegate synchronously (tests are single-threaded). `PropertyGrid` is a
  real (not mocked) `System.Windows.Forms.PropertyGrid` instance - constructing one needs no running
  message loop, and a real control means `.Refresh()`/`.SelectedObject` behave exactly like production.

Unblocking `PropertyGrid.Refresh()` immediately surfaced one more, unrelated NRE one level further in:
`GluxCommands.AddNamedObjectToAsync`'s `updateUi:true` path also sets `GlueState.Self.CurrentNamedObjectSave`,
whose setter calls `GlueState.Self.Find.TreeNodeByTag(...)` - `GlueState.Self.Find` (`IFindManager`) is only
ever set by `MainTreeViewPlugin.StartUp` (which also constructs a WPF `MainTreeViewControl`), never run in a
test host. Same fix shape: added **`FakeFindManager`** (`GlueUnitTests/TestSupport/`, all five `IFindManager`
members return null/empty/false) and wired it into `GlueTestBootstrap` (`GlueState.Self.Find ??= new
FakeFindManager()`). `GlueState.CurrentTreeNode`'s setter already tolerates a null tree node (just clears
the selection), so this needed no other production change.

`GlueUnitTests/Wizard/WizardProjectLogicAddGameScreenTests.HandleAddGameScreen_ShouldAddSolidAndCloudCollision_WhenRequested`
drives `WizardProjectLogic.HandleAddGameScreen` with `AddSolidCollision`/`AddCloudCollision` both true (the
one Wizard add-on path #1894 was tracking as still-blocked) and pins: both `NamedObjectSave`s land in the
screen with the right `InstanceName`/`SourceClassType`, the fake `PropertyGrid` ran instead of NRE-ing, and
code generation completed (`GameScreen.Generated.cs` written to disk).

**`IUiThreadMarshaller` (added in #1895) is now overlapping, not redundant, with `IMainGlueWindow.Invoke`/
`BeginInvoke`** - deliberately left both rather than folding one into the other. `IUiThreadMarshaller` is
reached through `TaskManager.UiThreadMarshaller`/`TaskManager.OnUiThread`/`GlueTask.DoOnUiThread`, a
narrower, purpose-built seam for "marshal onto the UI thread" that has nothing WinForms-specific in its
signature (`Action`/`Func<T>`/`Task` only). `IMainGlueWindow` is the *window*, and `Invoke`/`BeginInvoke`
are two of eleven-plus members alongside `PropertyGrid`/`Text`/`Close`/etc. that have nothing to do with UI
marshalling. Production's `WinFormsUiThreadMarshaller` already forwards to `MainGlueWindow.Self.Invoke`/
`BeginInvoke` - that forwarding is the correct relationship between the two seams and doesn't change here.
Collapsing them would mean either giving `IMainGlueWindow` to every `TaskManager` caller (dragging in
`PropertyGrid`/`Close`/`Text` for callers that only want "run this on the UI thread") or giving
`IUiThreadMarshaller` to every window-property caller (which doesn't have `PropertyGrid` etc. to give) -
neither direction simplifies anything, so both stay, each doing its one job.

### 2026-07-23 — AvailableAssetTypes.Self.Initialize / Collision Plugin asset-type test seam (issue #1894 follow-up)

Follow-up to "Cover `WizardProjectLogic.Apply()` end-to-end" directly below: that entry left the
`AvailableAssetTypes.CommonAtis`/plugin-loading blocker open. Rather than replicate `PluginManager`'s
reflection/directory-scan plugin loading in tests (rejected - too big a shift, and would blur the
third-party extensibility boundary that machinery exists for), this splits the blocker into its two real
parts and unblocks each directly:

- **`AvailableAssetTypes.CommonAtis`** (Camera/Text/Sprite/Polygon/etc.) is populated by
  `AvailableAssetTypes.Self.Initialize(startupPath)` from `Content/ContentTypes.csv` - a plain CSV read,
  nothing plugin-related. `GlueTestBootstrap.EnsureInitialized` now calls it (same call
  `MainGlueWindow.cs:336` makes), with `startupPath` resolved by walking up from the test assembly's
  `AppContext.BaseDirectory` to find `Glue/Content/ContentTypes.csv` (mirrors the existing
  `FindTemplatesRoot` pattern in `NewProjectCreationSmokeTests`) rather than hardcoding a path that only
  resolves from one machine/output layout.
- **Per-plugin ATIs** (e.g. `AssetTypeInfoManager.Self.CollisionRelationshipAti`, registered today only
  from inside `MainCollisionPlugin.StartUp`) are a separate concern: official plugins are already compiled
  directly into `Glue with All.sln` as a real project reference (not a sideloaded DLL), so their
  registration can be called directly without touching `PluginManager.LoadPlugins` at all. Split
  `MainCollisionPlugin.StartUp`'s asset-type registration into `MainCollisionPlugin.RegisterAssetTypes()`
  (`StartUp` still calls it, unchanged - zero behavior change) and added
  `GlueTestBootstrap.EnsureCollisionPluginAssetTypesRegistered()`, a separate opt-in call (not part of the
  always-on `EnsureInitialized`) that invokes it directly. Third-party plugin loading through
  `PluginManager.LoadPlugins`'s reflection/directory scan is completely untouched.

`GlueUnitTests/CollisionPlugin/CollisionAssetTypeRegistrationTests` pins this: without the registration,
`NamedObjectSave.GetAssetTypeInfo()` (which resolves by `SourceClassType` string via
`AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType`, not object identity) can never find
`CollisionRelationshipAti`, so every production consumer that compares against it - the
`NamedObjectSaveCodeGenerator.ConstructorFunc` dispatch, `HandleAddEventsForObject`,
`GetEventSignatureAndArgs`, `CollidableNamedObjectController`, etc. - silently treats a
collision-relationship `NamedObjectSave` as an unrecognized type. The test adds one via the real
`GluxCommands.AddNamedObjectToAsync` path, drives real code generation for its containing screen, and
asserts the lookup now resolves.

**Deliberately not attempted**: driving `CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects`
(the real handler `MainCollisionPlugin` wires to `PluginManager.ReactToCreateCollisionRelationshipsBetween`)
end-to-end. It also calls `GlueCommands.Self.DialogCommands.FocusTab`, which reads
`PluginManager.TabControlViewModel` - only ever set by a live WinForms `MainGlueWindow` - so it NREs outside
one. That's a separate, UI-rooted blocker, not an asset-type one.

**Still open after this entry**: the Wizard's `AddSolidCollision`/`AddCloudCollision` steps
(`MainAddScreenPlugin.AddCollision`) no longer NRE on `AvailableAssetTypes.CommonAtis` (fixed above), but
hit a different, unrelated NRE one level down: they go through `GluxCommands.AddNewNamedObjectToAsync`,
which always runs with `updateUi:true` (no way to opt out from that entry point), and that path calls
`MainGlueWindow.Self.PropertyGrid.Refresh()` - a generic `AddNewNamedObjectToAsync` UI coupling, unrelated
to collision types specifically. Not fixed here - out of scope for this entry, left for whichever future
work needs `AddNewNamedObjectToAsync` testable.

### 2026-07-23 — Unblock WizardProjectLogic AddGameScreen testing (issue #1894 follow-up)

Follow-up to the "Wizard apply-engine test seams" entry directly below: that PR left `HandleAddGameScreen`
untested because `ProjectCommands.CreateAndAddCodeFile` throws `NullReferenceException("Main Project")`
unless `GlueState.CurrentMainProject` is a real, MSBuild-backed `VisualStudioProject`, and building a
fakeable `IVisualStudioProject` seam looked like a separate, much larger refactor (that type's constructor
takes a live `Microsoft.Build.Evaluation.Project` and dereferences it immediately, and `CurrentMainProject`
is read as the concrete type in dozens of places, e.g. `is MonoGameDesktopGlBaseProject` checks - widening
the property to an interface would ripple everywhere).

Turns out no interface extraction was needed. `VisualStudioProject`'s constructor only needs *a* real
`Project` - it doesn't need one loaded through Glue's SDK-style-project machinery (which requires
`MSBuildLocator.RegisterDefaults()`, only ever called from `MainGlueWindow`). A bare, non-SDK-style
`.csproj` (no `Sdk="..."` attribute, no imports) evaluates with zero SDK/toolset resolution, so
`new Project(path)` and wrapping it in the existing concrete `ClassLibraryProject` works cleanly in a
plain xunit host. `GlueUnitTests/TestSupport/TestVisualStudioProjectFactory` does exactly this, writing a
minimal `.csproj` to a fresh temp directory per test.

Getting `GluxCommands.ScreenCommands.AddScreen` to run cleanly from there needed:
- **One real production fix**: `GlueCommands.DoOnUiThread` (all three overloads) called
  `MainGlueWindow.Self.Invoke`/`Invoke<T>` directly - a coupling the previous entry's
  `TaskManager.UiThreadMarshaller` seam didn't reach because it's not on `TaskManager`. Routed through
  `TaskManager.UiThreadMarshaller` instead, same as the other four call sites. Zero behavior change in
  production (still resolves to `WinFormsUiThreadMarshaller` by default).
- **Test-only bootstrap**, in `GlueUnitTests/TestSupport/GlueTestBootstrap`: a handful of one-time calls
  that mirror what `Glue.exe`'s own startup (`Program.cs`, `MainGlueWindow.cs`) does before
  `GlueCommands.Self`/`GlueState.Self` are usable - building the app's lightweight DI container
  (`Services.Builder`), registering `IGlueCommands`/`IGlueState` on the legacy `EditorObjects.IoC.Container`
  service locator (a second, older locator several command classes still read from directly, e.g.
  `GenerateCodeCommands.GlueCommands`), and `ProjectManager.Initialize()`/`FileWatchManager.Initialize()`.
  None of this is faked - it's real production initialization, just never previously run outside a live
  WinForms app.
- Per-test setup of `GlueState.CurrentMainProject`, `ObjectFinder.Self.GlueProject`, and
  `FileManager.RelativeDirectory` (the last one because `ElementCommands.AddScreen` builds its code-file
  path from that static rather than from the project's own directory - both need to agree for
  `IsFilePartOfProject` to correctly recognize a freshly-added file).

`HandleAddGameScreen` was changed from `private static` to `internal static` (zero behavior change) so
`GlueUnitTests/Wizard/WizardProjectLogicAddGameScreenTests.cs` can call it directly with a `WizardViewModel`
that only sets `AddGameScreen = true`, pinning: the returned `ScreenSave`'s name/tags, that it lands in
`GlueProjectSave.Screens` and becomes `StartUpScreen`, and that both the custom and generated code files
get added to the project.

**Still not covered:**
- `WizardProjectLogic.Apply()` end-to-end was covered by a follow-up - see the entry two above this one in
  the file (most recent first).
- `AddSolidCollision`/`AddCloudCollision` (and by extension most of the Wizard's other add-on steps): the
  `AvailableAssetTypes.CommonAtis` NRE this note used to describe is fixed - see the "AvailableAssetTypes.
  Self.Initialize / Collision Plugin asset-type test seam" entry directly above this one - but a different,
  unrelated NRE blocks them one level down (`AddNewNamedObjectToAsync`'s `MainGlueWindow.Self.PropertyGrid`
  UI coupling). Still open (issue #1894); see that entry for details.

### 2026-07-23 — Cover `WizardProjectLogic.Apply()` end-to-end (issue #1894 follow-up)

Follow-up to the "Unblock WizardProjectLogic AddGameScreen testing" entry directly below: that entry left
`Apply()` itself untested because, beyond `HandleAddGameScreen`, it unconditionally also runs "Generate all
code" (`GenerateAllCode`), a "Flush Files" step with a hard-coded 2.5s+ real delay, and "Saving Project"
(`SaveProjectAndElements`).

Driving `Apply()` with a `WizardViewModel { AddGameScreen = true }` (the same minimal config the
`HandleAddGameScreen` test uses) against the existing `GlueTestBootstrap`/`TestVisualStudioProjectFactory`
setup got through `GenerateAllCode` and the Flush Files delay with no changes needed, and hit exactly one
new blocker: `GluxCommands.SaveProjectAndElementsImmediately` reads `MainGlueWindow.Self.HasErrorOccurred`
directly - `MainGlueWindow.Self` is only ever set by constructing a real WinForms `MainGlueWindow`, which
`GlueTestBootstrap` deliberately does not do, so it NREs in a plain xunit host.

**Fix**: made the three `MainGlueWindow.Self.HasErrorOccurred` reads/writes in
`SaveProjectAndElementsImmediately` null-conditional (`MainGlueWindow.Self?.HasErrorOccurred`). Zero
behavior change in production - `MainGlueWindow.Self` is always set there; this only changes behavior when
`Self` is null, which previously just crashed.

No other blockers found: `GenerateAllCode` walking every element worked cleanly off the project state left
by `HandleAddGameScreen`, and the Flush Files delay is exactly what it says - a real 2.5s+ `Task.Delay`,
not a wait on a file-watcher event, so it slows the test but doesn't hang it.

Added `Apply_ShouldAddGameScreenGenerateCodeAndSaveProject_EndToEnd` to
`GlueUnitTests/Wizard/WizardProjectLogicAddGameScreenTests.cs`, reusing that class's existing
bootstrap/teardown. It drives the real `WizardProjectLogic.Apply()` (not just `HandleAddGameScreen`) and
pins: the GameScreen lands in the project and becomes `StartUpScreen`, both code files are part of the
project, `GenerateAllCode` writes `GameScreen.Generated.cs` to disk, and `SaveProjectAndElements` writes
the project file to disk (`.glux`, since a fresh `GlueProjectSave()` defaults to `FileVersion` 0 - `.gluj`
requires `GluxVersions.GlueSavedToJson` or later).

This closes the `Apply()`-end-to-end gap issue #1894 was tracking. The `AvailableAssetTypes.CommonAtis`/
plugin-loading blocker on `AddSolidCollision`/`AddCloudCollision` and most other Wizard add-on steps this
note used to flag as the one remaining open item was split and partly fixed by a follow-up - see the
"AvailableAssetTypes.Self.Initialize / Collision Plugin asset-type test seam" entry (most recent, near the
top of this section) for the current state and what's still open.

### 2026-07-23 — Wizard apply-engine test seams (issue #1894)

Follow-up to #1892: applied the same seam pattern to the Wizard's apply-engine (`WizardProjectLogic.Apply`),
which was untestable because it runs through `TaskManager`'s real background thread and
`MainGlueWindow`'s real WinForms `Invoke`/`BeginInvoke`.

- **`TaskManager.SynchronousMode`** (static bool, default `false`) - when set, `Add`/`Add<T>`/
  `Add(Func<Task>)` run the task inline on the calling thread (via a new `RunSynchronously` helper)
  instead of enqueueing to the background STA thread; the constructor also skips spinning that thread
  when the flag is already set. `AddAsync`/`AddOrRunIfTasked` needed no changes - they already delegate
  to `Add` in the non-reentrant case. Zero behavior change when unset.
- **`IUiThreadMarshaller`** (`Glue/Managers/IUiThreadMarshaller.cs`) - abstraction over
  Invoke/BeginInvoke, injected via `TaskManager.UiThreadMarshaller` (static, defaults to
  `WinFormsUiThreadMarshaller`, which forwards to `MainGlueWindow.Self.Invoke`/`BeginInvoke` exactly as
  before). `TaskManager.OnUiThread`/`BeginOnUiThread`, and all four `GlueTask*.Do_Action_Internal()`
  variants (`GlueTask`, `GlueTask<T>`, `GlueAsyncTask`, `GlueAsyncTask<T>`), now go through this instead of
  referencing `MainGlueWindow.Self` directly - the latter four were a coupling #1894 hadn't
  originally identified (only `TaskManager.OnUiThread`/`BeginOnUiThread` were flagged), found because any
  `GlueTask` with `DoOnUiThread = true` would otherwise still NRE in a test host with no live window.

Both seams are pinned directly in `GlueUnitTests/Tasks/TaskManagerSynchronousModeTests.cs`.

**`WizardProjectLogic.Apply` itself is not covered end-to-end.** The issue's fallback plan was to start
with a plugin-free step (bare `AddGameScreen`) if full coverage wasn't practical. That step turned out to
be blocked by something deeper than plugins: `HandleAddGameScreen` -> `GluxCommands.ScreenCommands.AddScreen`
-> `ProjectCommands.CreateAndAddCodeFile`, which throws `NullReferenceException("Main Project")` unless
`GlueState.CurrentMainProject` is a real, MSBuild-backed `VisualStudioProject` - the same
`VisualStudioProject` construction blocker already documented above under "Known Areas Needing
Improvement". Building an `IVisualStudioProject` seam to unblock that is a separate, larger refactor.
Instead, extracted and tested the one piece of `Apply` that's pure decision logic with no
GlueState/TaskManager/plugin coupling: `WizardProjectLogic.GetDisplaySettingsFor` (the `CameraResolution`
-> width/height/aspect-ratio mapping used by `ApplyMainCameraSettings`), pinned in
`GlueUnitTests/Wizard/WizardProjectLogicTests.cs`. The scoping-out is documented directly above
`WizardProjectLogic.Apply` in a code comment pointing back here.

### 2026-07-23 — Make new-project creation (NPC) testable (issue #1892)

`ProjectCreationHelper` (the New Project Creator in `NpcWpfLib`) was untested despite being one of the
most common things to break. Its core rename/namespace/guid logic was already UI-free static; the only
UI couplings were `ShowMessageBox` (7 call sites) and `DownloadFileSync` (an Updater window). Extracted
those into `IProjectCreationNotifier` and `IDownloadService` (in `NpcWpfLib/Services/`), default-injected
via settable statics `ProjectCreationHelper.Notifier`/`.Downloader` (WPF impls in production, fakes in
tests) — zero behavior change, all existing call sites unchanged.

Tests added in `GlueUnitTests/Projects/`:
- `ProjectCreationHelperTests` — pins name validation, file/dir rename + namespace rewrite, guid replace.
- `NewProjectCreationSmokeTests` — drives the real `MakeNewProject` against a checked-in template (via
  `PlatformProjectInfo.LocalSourceFile`, so creation is network-free) then runs `dotnet build` on the
  result. Tagged `[Trait("Category", "BuildSmoke")]`; currently covers the Desktop GL MonoGame template
  (Android/iOS/Web/FNA need extra toolchains — see the test's comment).

Also wired `dotnet test` into `.github/workflows/glue.yml` (it previously ran no tests): a fast gate
(`Category!=BuildSmoke`) plus a separate build-smoke step, both run via the `.sln` so `$(SolutionDir)` is
defined for OfficialPlugins' post-build step. Fixed 5 pre-existing `RightClickDescriptorTests` failures
along the way — the mock `ITreeNode` never set up `Text`, so `IsRootLayerNode()`'s `Text.Equals(...)`
threw; real nodes always have `Text`, so the fix was to configure the mock, not production.

### 2026-07-17 — Make `StartUpScreen` diffable so changing it doesn't force a full reload

Setting the startup screen is a common Glue action, but `GluxCommands.SaveGlujFile()` never registers
a self-save suppression (`FileWatchManager.IgnoreChangeOnFileUntil`) for the `.gluj` path, so its own
write is picked up by the file watcher like an external edit. That external-edit path (`ReloadGlux` →
`BuildProjectDiffPlan`) only recognized diffs inside `Screens[i]`/`Entities[i]`/`GlobalFiles[i]` -
`StartUpScreen` is a top-level `GlueProjectSave` field, so every startup-screen change forced a full
project reload, which is disruptive mid-workflow.

Added `DiffableTopLevelProperties` (a `HashSet<string>` on `UpdateReactor`, currently just
`nameof(GlueProjectSave.StartUpScreen)`) and a new `ProjectDiffPlan.TopLevelPropertiesChanged` list.
`BuildProjectDiffPlan` now collapses a whitelisted top-level property diff into that list instead of
forcing `FullReloadRequired`. `ReloadGlux`'s apply step handles `StartUpScreen` by copying the value
onto the live project, calling `GenerateCodeCommands.GenerateStartupScreenCode()`, refreshing the
old/new startup screen tree nodes, and calling `PluginManager.ReactToChangedStartupScreen()` - mirroring
what `GluxCommands.StartUpScreenName`'s setter already does for the in-Glue-UI path, minus the
redundant re-save (the value on disk is already correct; that's what triggered this in the first place).

The whitelist is deliberately narrow: other top-level properties (`CustomGameClass`,
`SuppressBaseTypeGeneration`, etc.) haven't been individually verified safe to apply without a full
reload, so they still fall back to `FullReloadRequired`. Adding another diffable property later means
adding its name to the whitelist and a matching `case` in `ReloadGlux`'s switch - no structural changes.

Added 2 tests to `UpdateReactorTests.cs` (`ShouldApplyStartUpScreenChange_WithoutRequiringFullReload`,
`ShouldApplyStartUpScreenChangeAlongsideAScreenSwap_WhenBothDiffer`) and repointed the two existing
"unrecognized property forces full reload" tests from `StartUpScreen` (now recognized) to
`CustomGameClass` (still not whitelisted).

### 2026-07-17 — Extract `UpdateReactor.BuildProjectDiffPlan`, pin partial-reload behavior

`UpdateReactor.ReloadGlux` handles external edits to the `.glux`/`.gluj`/per-element `.glsj`/`.glej`
files: it diffs the in-memory project against a freshly-reloaded copy (`CompareNetObjects`) and, when
every difference collapses to a single `Screens[i]`/`Entities[i]`/`GlobalFiles[i]` entry, swaps just
that element and regenerates only its code instead of doing a full project reload. This decision logic
was inline in a ~70-line loop with no test coverage.

Extracted the classification into `internal static ProjectDiffPlan BuildProjectDiffPlan(IEnumerable<string>
differencePropertyNames, GlueProjectSave oldProjectSave, GlueProjectSave newProjectSave)` — pure and
static, no Glue singletons touched, so it's constructible and callable directly in a unit test with
plain `GlueProjectSave`/`ScreenSave`/`EntitySave`/`ReferencedFileSave` instances. `ReloadGlux` now calls
it and applies `plan.ElementsToReplace`/`plan.GlobalFilesToReplace` before checking
`plan.Outcome`, preserving an existing quirk exactly: when a difference list contains an unresolvable
entry (a project-level property, or a list `Count` change from an add/remove/reorder), the replacements
resolved *before* that entry in iteration order are still applied even though the overall result is
`FullReloadRequired` — matching the original inline loop's break-after-partial-work behavior byte for
byte. Zero behavior change; see the new "Known Areas Needing Improvement" bullet above for why this
stopped at a pure-function seam rather than an `IGlueState`-style DI injection.

Added `Tests/GlueUnitTests/IO/UpdateReactorTests.cs` (8 tests) pinning: no differences, single
Screen/Entity/GlobalFile replacement, dedup of multiple diffs against the same element, full-reload
fallback for a project-level property and for a list-Count change, and the partial-work-before-abort
ordering quirk above.

### 2026-07-12 — Inject `IGlueState` into `ProjectCommands`

Follow-up to the directory-path fix below. `ProjectCommands` had ~40 direct `GlueState.Self` reads
across the class (not just the bug's method) — the largest concentration of static-singleton coupling
of any class touched by that fix. Converted it to the same transitional pattern already used for
`SelectionLogic.Current`/`ReferenceService.Self` elsewhere in this doc:

- Added `internal IGlueState _glueState = GlueState.Self;` to `ProjectCommands`, defaulting to the real
  singleton (zero behavior change in production) with an internal setter tests can override.
- Replaced all reads of `GlueState.Self.*` in `ProjectCommands.cs` with `_glueState.*`, **except** one:
  `AddSyncedProject`'s `GlueState.Self.SyncedProjects.Add(syncedProject)` mutates the list, and
  `IGlueState.SyncedProjects` is intentionally read-only (`IEnumerable<ProjectBase>`) — that one call
  stays on the concrete singleton rather than widening the interface's mutation surface for one caller.
- `IGlueState` was missing `CurrentCodeProjectFileName` (used by `ProjectCommands` but never previously
  exposed on the interface, only the concrete `GlueState` class) — added it to `IGlueState`, to
  `GlueStateSnapshot`'s implementation, and to `GlueStateSnapshot.SetFrom` (per its own "STOP! if adding
  more properties" comment).
- Two `private static` helper methods (`ShouldFileBeInContentProject`, and an unused 2-arg
  `CopyToBuildFolder(FilePath, string)` overload — dead code, no call sites found anywhere in the repo)
  read `_glueState` internally, so both had `static` removed. Safe: both are `private`.
- Added `InternalsVisibleTo("GlueUnitTests")` / `("DynamicProxyGenAssembly2")` to `Glue.csproj` (it only
  existed on `OfficialPlugins.csproj` before), since `ProjectCommands` is `internal` and tests need to
  reach it and its new `_glueState` field.

This unlocks unit-testing any `ProjectCommands` logic that only depends on `IGlueState` — the
`VisualStudioProject` construction problem noted above is the next, harder blocker.

### 2026-07-12 — Fix directory paths leaking into csproj Include items, pin with a unit test

Bug: Glue was occasionally adding directory paths (e.g. `Content\Entities\Bosses\ResonatorCoil\`) as
`<Content Include="...">` items in the target project, which MSBuild rejects with "A file item cannot
end with a path separator." Root cause: `FileSystemWatcher` in `ChangedFileGroup.cs` watches with
`NotifyFilters.DirectoryName`, so folder `Created`/`Renamed` events flow through
`UpdateReactor.UpdateFile` → plugins → `ProjectCommands.UpdateFileMembershipInProject` the same as file
events, and that method never checked whether the incoming `FilePath` was a directory before handing it
to `VisualStudioProject.AddContentBuildItem` → `mProject.AddItem`.

`UpdateFileMembershipInProject` itself can't be unit-tested directly: it depends on `GlueState.Self` and
a concrete `VisualStudioProject`, and `VisualStudioProject`'s constructor requires a live MSBuild
`Project` (dereferenced immediately in `GetDotNetVersion()`), so it can't be instantiated or mocked in a
test without loading a real `.csproj` from disk. Rather than take on that larger extraction to add one
test, the directory check itself — the actual fix — was pulled out into a standalone, pure, testable
method: `internal static bool ShouldSkipBecauseDirectory(FilePath fileName)`. `UpdateFileMembershipInProject`
calls it as an early-out; `ProjectCommandsTests.cs` (new, `Tests/GlueUnitTests/CommandInterfaces/`) pins
it directly against the exact path shape from the bug report (trailing separator, both `\` and `/`) plus
ordinary file paths that must NOT be skipped.

See "Known Areas Needing Improvement" above for the larger `VisualStudioProject` testability gap this
left unaddressed.

### 2026-03-04 — Reference-finding centralization

Audited all reference-finding code across the codebase and consolidated it into a new `ReferenceService`:

- **`Glue/Managers/FindManager.cs` → deleted** — contained only dead commented-out WinForms code plus the `IFindManager` interface.
- **`Glue/Managers/IFindManager.cs` → created** — clean interface-only file. Also restored the missing `GlobalContentFilesPath` property (was accidentally commented out, causing a latent compile error in `RuntimeDebuggingPlugin`).
- **`OfficialPlugins/TreeViewPlugin/Logic/FindManager.cs`** — added `GlobalContentFilesPath` implementation.
- **`Glue/Elements/ReferenceService.cs` → created** — 24 public methods + 2 private helpers + 4 composite `GetReferencesTo` query methods extracted from `ObjectFinder` and `ElementReferenceListWindow`.
- **`Glue/Elements/ObjectFinder.cs`** — all 24 moved methods replaced with one-line forwarding stubs. Zero behavior change for existing callers.
- **`Glue/Controls/ElementReferenceListWindow.xaml.cs`** — reduced to a pure display class. Each `Populate*` method is now a 3-line foreach over `ReferenceService.Self`.

**Follow-up done (2026-03-04):** `IReferenceService` extracted and injected — see completed refactors below.

### 2026-03-04 — Extract `IReferenceService`, inject into `InheritanceManager` and `ElementReferenceListWindow`

- **`Glue/Elements/IReferenceService.cs` → created** — interface with all 28 public methods from `ReferenceService` (composite queries, file/RFS finding, named-object/entity finding, variable/type finding, inheritance traversal).
- **`Glue/Elements/ReferenceService.cs`** — added `: IReferenceService`. `Self` property type changed from `ReferenceService` to `IReferenceService` with `internal set` (same transitional pattern as `SelectionLogic.Current`; tests can swap the singleton for a mock).
- **`Glue/Managers/InheritanceManager.cs`** — converted from `static class` to instance class. Constructor takes `IReferenceService`. Added `public static InheritanceManager Self { get; internal set; }` accessor. All 13 call sites across 10 files updated from `InheritanceManager.Method()` to `InheritanceManager.Self.Method()`. `_referenceService` used in place of `ReferenceService.Self` for all 8 reference-finding calls.
- **`Glue/Services/Builder.cs`** — added `InheritanceManager.Self = new InheritanceManager(ReferenceService.Self)` in the composition-root `Build()` method alongside the other singleton initializations.
- **`Glue/Controls/ElementReferenceListWindow.xaml.cs`** — constructor now takes `IReferenceService referenceService`; stored as `_referenceService` field. All 4 `Populate*` methods use `_referenceService` instead of `ReferenceService.Self`.
- **`OfficialPlugins/TreeViewPlugin/Logic/RightClickHelper.cs`** — updated the single `new ElementReferenceListWindow()` instantiation to pass `ReferenceService.Self`.

Zero behavior change. `ObjectFinder.Self` forwarding stubs unchanged.

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
