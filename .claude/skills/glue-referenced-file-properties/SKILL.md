---
name: glue-referenced-file-properties
description: ReferencedFileSave properties whose meaning depends on container type (Screen/Entity/Global), and the two independent property grids that must each hide the ones that don't apply. Triggers: IsSharedStatic, IsDatabaseForLocalizing, GetContainerType, ContainerType.None, ReferencedFileSavePropertyGridDisplayer, MainPropertyGridPlugin, DataUiGrid.MembersToIgnore, crash toggling a checkbox on a file/global content.
---

# ReferencedFileSave Properties: Container-Dependent Meaning, Two Grids to Update

A `ReferencedFileSave` can live in three places - `GetContainerType()`
(`FRBDK/Glue/Glue/SaveClasses/ReferencedFileSaveExtensionMethods.cs`) returns `Screen`, `Entity`, or
`None` (global content, in `GlueProjectSave.GlobalFiles`, no owning `GlueElement`). Several of its
properties only make sense for a subset of those - e.g. `IsSharedStatic` only varies per-instance for
Screen-owned files; Entity-owned files are forced static and global content is *always* static
(`GlobalContentCodeGenerator` never reads the flag - see the constructor comment in
`GlueCommon/SaveClasses/ReferencedFileSave.cs`). Before assuming a property is universally meaningful,
trace whether its consuming code generator (`ReferencedFileSaveCodeGenerator.cs` for element-scoped vs.
`GlobalContentCodeGenerator.cs` for global) actually reads it for every container type.

## Landmine: two independent property grids, both must exclude it

Two separate UIs show `ReferencedFileSave` properties, and neither knows about the other's exclusion list:

| Grid | File | Exclusion mechanism |
| --- | --- | --- |
| Legacy WinForms | `Glue/FormHelpers/PropertyGrids/ReferencedFileSavePropertyGridDisplayer.cs` (`UpdateIncludedAndExcluded`) | `ExcludeMember(...)`, driven by `GetContainerType()`/extension checks |
| WPF "Settings (Preview)" | `OfficialPlugins/PropertyGrid/MainPropertyGridPlugin.cs` (`ShowPropertiesForReferencedFileSave`) | `settingsGrid.MembersToIgnore.Add(...)` - a flat list, doesn't consult container type at all unless you add the check yourself |

Both funnel edits into the same place - `SetPropertyManager.ReactToPropertyChanged` →
`ReferencedFileSaveSetPropertyManager.ReactToChangedReferencedFile`. A property excluded from only one
grid is still reachable (and still crashes if its handler assumes context, e.g. a non-null
`GlueState.Self.CurrentElement`) through the other.

**When a property's meaning/effect depends on container type, put the decision in one shared extension
method (e.g. `ReferencedFileSaveExtensionMethods.GetIsSharedStaticEditable`) and call it from both grids**,
not a condition duplicated in each.

## Two real crashes from this exact gap

- **#2016/#2017** - `IsDatabaseForLocalizing` is conditionally excluded in the WinForms grid (non-CSV
  extensions) but the WPF grid showed it unconditionally; its handler cast `oldValue` without a null
  check. `ReactToChangedReferencedFile` is `internal` with callers that hand over no old value, so a null
  `oldValue` is a legitimate input to any handler added there, not a caller bug to chase upstream.
- **#2018** - `IsSharedStatic` wasn't excluded for global content in *either* grid (only Entity was
  handled). Its handler dereferenced `GlueState.Self.CurrentElement.NamedObjects`, null for a global file.
  The first fix attempt null-guarded the handler instead of hiding the now-provably-meaningless checkbox -
  wrong layer. **If a property is inert for a container type (confirmed by tracing its codegen consumer),
  hide it there; don't just prevent the crash.** A null check treats "shouldn't be interactable" as "should
  render safely," which fixes the exception but leaves a dead control the user can still toggle.

## Related
- `glue-project-codegen` - the code generator pipeline these properties feed into.
