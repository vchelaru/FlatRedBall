---
name: glue-add-object-flow
description: Glue's "New Object" add pipeline and the tree's inline-rename mechanism. Triggers: AddObjectViewModel, ShowAddNewObjectDialog, NewObjectTypeSelectionControlWpf, IsTypePredetermined, NodeViewModel.IsEditing, RenameNamedObjectSave.
---

# Glue Add-Object Flow

Adding a `NamedObjectSave` and renaming one in the tree are two separate code paths that share
validation logic. See [[glue-task-manager]] for how each path's save/codegen work gets queued.

## File Map

| File | Role |
|---|---|
| `Glue/Controls/NewObjectTypeSelectionControlWpf.xaml(.cs)` | "New Object" modal — type radios, filtered type list, name box |
| `Glue/ViewModels/AddObjectViewModel.cs` | Backing VM; `SetDefaultObjectName()` auto-generates a unique name; `IsTypePredetermined`/`IsSelectionEnabled`/`IsObjectTypeGroupBoxEnabled` gate which controls are enabled |
| `Glue/Plugins/ExportedImplementations/CommandInterfaces/DialogCommands.cs` | `ShowAddNewObjectDialog` / `CreateAndShowAddNamedObjectWindow` — shows the modal, validates the name after it closes |
| `Glue/Plugins/ExportedImplementations/CommandInterfaces/GluxCommands.cs` | `AddNewNamedObjectToSelectedElementAsync` → `AddNewNamedObjectToAsync` → `AddNamedObjectToAsync` (actual add); `RenameNamedObjectSave` (separate rename path) |
| `OfficialPlugins/TreeViewPlugin/ViewModels/NodeViewModel.cs` | `IsEditing` toggle drives in-place tree rename; `HandleRenameThroughEdit` dispatches by `Tag` type (`GlueElement`/`ReferencedFileSave`/`NamedObjectSave`/folder) |
| `OfficialPlugins/TreeViewPlugin/Logic/RightClickHelper.cs` | `RenameItem()` — `SelectionLogic.Current.CurrentNode.IsEditing = true` is the whole mechanism for entering inline rename |

## Gotchas

- `IsTypePredetermined` (set when adding to a typed list, or explicitly by the caller, e.g.
  `HandleAddLayerClick`) drives `IsSelectionEnabled => !IsTypePredetermined`, which disables *both* the
  type radio group and the type list, not just one. `CreateAndShowAddNamedObjectWindow` checks the
  *final* value of this flag (it can be narrowed later, e.g. `AvailableEntities.Count < 2` for a typed
  list whose generic type has derived entities to choose from) and skips constructing the WPF window
  entirely when true — adds immediately with the `SetDefaultObjectName()` default, no modal shown at
  all. `RightClickHelper.AddObjectAndBeginRename()` then drops the new tree node straight into inline
  rename.
- `IsOkButtonEnabled` only checks `SelectedItem != null` — it was never gated on name validity. The
  name field itself no longer exists in `NewObjectTypeSelectionControlWpf.xaml`; naming happens via
  inline tree rename after add, not before.
- `DialogCommands.ShowAddNewObjectDialog` no longer calls `NameVerifier.IsNamedObjectNameValid` before
  adding — the name is Glue's own computed default at that point, not free-typed input. It still checks
  `RecursionManager.Self.CanContainInstanceOf` (unrelated to naming).
- Double-clicking a type in the list (`StrongSelect`) immediately confirms the dialog with whatever's
  in the name box — no Enter/OK click needed.
- Tree inline rename validates via `NameVerifier` and reverts on invalid input inside
  `NodeViewModel.HandleRenameThroughEdit` → `GluxCommands.RenameNamedObjectSave` — the same validation
  the Add path uses, so chaining add → immediate rename stays safe.
- Chaining Add then Rename queues two independent `SaveProjectAndElements(AddOrMoveToEnd)` calls with
  different `DisplayInfo`, so per `glue-task-manager`'s coalescing rule they run as two separate
  save/codegen passes, not one.
- `AddNewNamedObjectToAsync` and `AddNamedObjectToAsync`'s own `TaskManager.Self.AddAsync` calls both use
  `TaskExecutionPreference.Asap`, not the `AddAsync` default of `Fifo` — the add is a direct
  user-triggered action `AddObjectAndBeginRename` depends on completing immediately, not something that
  should sit behind unrelated Fifo-tier work. Bumping the inner (nested) call is defensive rather than
  strictly required — see `glue-task-manager`'s note on why a nested `AddAsync` call runs inline
  regardless of its own preference.
- `RightClickHelper.AddObjectAndBeginRename()` is the single shared entry point for the tree's own "Add
  Object"/"Add Layer" menu items and the Ctrl+N shortcut (`MainTreeViewControl.xaml.cs`) — it calls
  `ShowAddNewObjectDialog`, then `SelectionLogic.Current.SelectByTag`/`.IsEditing = true`.
  `DragDropManager.cs`'s drag-and-drop add and `QuickActionPlugin`'s add call `ShowAddNewObjectDialog`
  directly instead (they get the modal-skip but not the auto-inline-rename trigger, since
  `SelectionLogic` is internal to the TreeViewPlugin assembly and unreachable from those projects).
