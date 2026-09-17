---
name: glue-common-extraction
description: Moving Glue.csproj logic into GlueCommon (net8.0) through the XxxCore.Self seams. Triggers: IObjectFinderCore, IAvailableAssetTypesCore, ITypeResolutionCore, IGlueStateCore, IPluginManagerCore, GlueCommonUnitTests, Fake*Core, ObjectFinderCoreCollection.
---

# GlueCommon Extraction

`GlueCommon` is plain net8.0 and can't reference `Glue.csproj`, so logic moved there reaches Glue's singletons only through narrow seam interfaces: `GlueCommon/SaveClasses/IObjectFinderCore.cs`, `SaveClasses/IGlueStateCore.cs`, `Elements/IAvailableAssetTypesCore.cs`, `Parsing/ITypeResolutionCore.cs`, `Build/IBuildToolAssociationCore.cs`, `Controls/IErrorReportingCore.cs`, `SaveClasses/IPluginManagerCore.cs` (under `SaveClasses/`, not `Plugins/`, because `GlueCommon.csproj` compile-excludes `Plugins\**`). Each has a static `XxxCore.Self` that the Glue-side class (`ObjectFinder`, `GlueState`, `AvailableAssetTypes`, `TypeManager`, `PluginManager`, ...) assigns from its own static constructor. Tests live in `Tests/GlueCommonUnitTests`, one hand-rolled `Fake*Core` per seam, and every test class that swaps a `Self` sits in `[Collection(nameof(ObjectFinderCoreCollection))]`.

## Pattern

- Widen a seam only with members the Glue class already implements publicly; the seam carries no logic of its own.
- Split a class across the two projects, not a file. Extension-method callers resolve unchanged; non-extension static calls need the new class name.
- `GlueState.Self.CurrentGlueProject` is `ObjectFinder.Self.GlueProject`, so `IObjectFinderCore` already covers that reach.

## Landmines

- `XxxCore.Self` stays null until the Glue class's static constructor runs, and only a direct touch of that class triggers it. Code that runs during project load and reaches a singleton only through its seam needs an explicit startup touch; `TypeManager.EnsureTypeResolutionSeamWired()` in `MainGlueWindow`/`GlueTestBootstrap` is the existing example.
- `GlueCommonUnitTests.csproj` builds and tests standalone; `GlueUnitTests.csproj` needs `-p:SolutionDir` (see [glue-unit-test-bootstrap](../glue-unit-test-bootstrap/SKILL.md)).
