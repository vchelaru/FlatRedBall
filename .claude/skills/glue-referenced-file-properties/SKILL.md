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

## Two property grids, one shared hide list

Two separate UIs show `ReferencedFileSave` properties. Both hide members from the same set,
`GetHiddenMemberNames()` in `GlueCommon/SaveClasses/ReferencedFileSaveElementExtensions.cs`, which
holds every container-type, extension and state rule:

| Grid | File | Exclusion mechanism |
| --- | --- | --- |
| Legacy WinForms | `Glue/FormHelpers/PropertyGrids/ReferencedFileSavePropertyGridDisplayer.cs` (`UpdateIncludedAndExcluded`) | loops `GetHiddenMemberNames()` calling `ExcludeMember(...)`, then adds its own extras |
| WPF "Settings (Preview)" | `OfficialPlugins/PropertyGrid/ReferencedFileSaveSettingsLogic.cs` (`GetMembersToIgnore`, used by `MainPropertyGridPlugin.ShowPropertiesForReferencedFileSave`) | the shared set plus the WPF-only ignores |

The WPF grid ignores `[Browsable(false)]`, so a member that must stay hidden there has to be in the shared set.
It also doesn't re-evaluate on its own: `MainPropertyGridPlugin.HandlePropertyChanged` rebuilds it after the
properties that change the rules (e.g. `TreatAsCsv`, `LoadedAtRuntime`), so add a new rule's input property there.

Both funnel edits into the same place - `SetPropertyManager.ReactToPropertyChanged` →
`ReferencedFileSaveSetPropertyManager.ReactToChangedReferencedFile`. A property excluded from only one
grid is still reachable (and still crashes if its handler assumes context, e.g. a non-null
`GlueState.Self.CurrentElement`) through the other.

**Add a new hide rule to `GetHiddenMemberNames()`** (it calls `GetIsSharedStaticEditable` for the container-type
case), never as a condition in one grid.

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
