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

- `IsTypePredetermined` (set when adding to a typed list, or explicitly by the caller) drives
  `IsSelectionEnabled => !IsTypePredetermined`, which disables *both* the type radio group and the
  type list, not just one. When true, the modal does zero type-selection work — it's pure naming
  friction.
- `IsOkButtonEnabled` only checks `SelectedItem != null`; name validity is checked *after* the modal
  closes, in `ShowAddNewObjectDialog`. An invalid name discards the whole dialog and shows a message
  box — no re-prompt.
- Double-clicking a type in the list (`StrongSelect`) immediately confirms the dialog with whatever's
  in the name box — no Enter/OK click needed.
- Tree inline rename validates via `NameVerifier` and reverts on invalid input inside
  `NodeViewModel.HandleRenameThroughEdit` → `GluxCommands.RenameNamedObjectSave` — the same validation
  the Add path uses, so chaining add → immediate rename stays safe.
- Chaining Add then Rename queues two independent `SaveProjectAndElements(AddOrMoveToEnd)` calls with
  different `DisplayInfo`, so per `glue-task-manager`'s coalescing rule they run as two separate
  save/codegen passes, not one.
