---
name: glue-project-types
description: How Glue decides what kind of project it opened, and the seams that stop it writing code/csproj for FRB2 projects. Triggers: ProjectCreator, ProjectBase, VisualStudioProject, Frb2Project, IsMaintainedByGlue, CodeWritePolicy, "could not determine project type", LocateSolution.
---

# Glue Project Types

## Where the decision happens

| File | Purpose |
| --- | --- |
| `Glue/VSHelpers/Projects/ProjectCreator.cs` | The only place a `.csproj` becomes a `ProjectBase`. FRB2 check first, then the DefineConstants cascade. |
| `Glue/VSHelpers/Projects/Frb2ProjectDetector.cs` | Rule list identifying an FRB2 game project from its project items. |
| `GlueCommon/VSHelper/Projects/ProjectBase.cs` | `ContentDirectory` and `IsMaintainedByGlue`, the two properties everything downstream keys off. |
| `Glue/Plugins/ExportedImplementations/CodeWritePolicy.cs` | The one rule deciding whether Glue may write C# into the loaded project. |

## Project type comes from DefineConstants, not from references

`ProjectCreator.TryGetProjectTypeFromDefineConstants` picks the `ProjectBase` subclass by matching
`DESKTOP_GL` / `ANDROID` / `IOS` / `FNA` / `BLAZORGL` against the csproj's `DefineConstants`, plus a
minimum `TargetFrameworkVersion`.

**Landmine:** a project that defines none of those does not degrade gracefully — Glue shows "Could not
determine project type", offers a manual-pick dialog, and returns null, and `ProjectLoader` then skips
loading entirely. A perfectly valid modern SDK-style csproj can hit this simply by not carrying an
FRB-specific `DefineConstants`. Anything identifying a project by its *references* has to run **before**
that cascade, in `CreatePlatformSpecificProject`.

## FRB2 projects: reads yes, writes no

An FRB2 game reads Glue's `.gluj`/`.glsj`/`.glej` JSON at runtime and owns all of its own C#, so
`Frb2Project` sets `IsMaintainedByGlue = false`. The csproj is still fully loaded and read — it is what
resolves the project directory and Content folder every relative path in the JSON hangs off. Only writes
are suppressed, at four seams:

| Seam | Suppresses |
| --- | --- |
| `VisualStudioProject.Save` + the duplicate-resolution save in `Load` | Every write of the `.csproj` to disk |
| `GlueCommands.GenerateCodeCommands` (resolves to `NoCodeGenerationCommands`) | Everything reached through `IGenerateCodeCommands` |
| `FileCommands.SaveIfDiffers` | Any `.cs` written as text, whoever wrote it |
| `ProjectCommands.CreateAndAddCodeFile` / `CreateAndAddPartialFile` | The empty placeholder created before anything fills it |

**Landmine:** the last two are not redundant with `NoCodeGenerationCommands`. A real chunk of Glue's code
generation never goes through `IGenerateCodeCommands` at all — `CodeBuildItemAdder` drops embedded
resource files (`Performance/IEntityFactory`, `Performance/PoolList`), `CameraSetupCodeGenerator` writes
`Setup/CameraSetup.Generated.cs`, and `ContentPipelinePlugin`'s `AliasCodeGenerator` writes
`FileAliases.Generated.cs` — all on glux load, before and outside any `GenerateAllCode` call. Suppressing
only the interface leaves those writing into the project.

Plugin generators that call `FileManager.SaveText` directly (Platformer/TopDown/Racing CSV and enum
generators, Gum, GameCommunication) still bypass all of this. They only fire for projects that opt into
those behaviors.

`Frb2Project.ContentDirectory` is `"Content/"` because FRB2's `GlueContentSource` resolves referenced
files as `<directory holding the .gluj>/Content/<name>` — the `.gluj` must stay beside the Content folder,
which is why it is written at project root like FRB1's.

## Solution lookup can fail a load

`ProjectSyncer.LocateSolution` **throws** when it finds no solution file, and that exception surfaces
through `GlueState.CurrentSlnFileName` during `ProjectLoader.LoadProject` — so an unrecognized solution
format is a project that will not open, not a cosmetic gap. It searches the project directory, one up,
and two up, for both `.sln` and `.slnx`, then verifies the file's text actually names the `.csproj`.

## Testing

`Frb2ProjectDetector.IsFrb2Project` takes a plain item-type/include list, so detection tests need no
MSBuild evaluation. For anything needing a real `ProjectBase`, see
[glue-unit-test-bootstrap](../glue-unit-test-bootstrap/SKILL.md) — `TestVisualStudioProjectFactory`
(bare non-SDK csproj, no SDK resolution) and `GoldProject.LoadInGlueAsync` (the real loader) are the two
rungs.

**Landmine:** a test asserting "loading this project generated nothing" depends on which plugins are
registered, and that is process-wide state another test may have set. Call
`GlueTestBootstrap.EnsureGameProjectPluginsRegistered()` so the assertion means the same thing in
isolation and mid-suite.
