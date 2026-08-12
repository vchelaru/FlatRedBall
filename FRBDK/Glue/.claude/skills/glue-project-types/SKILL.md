---
name: glue-project-types
description: How Glue decides what kind of project it opened, and the seams that stop it writing code/csproj for FRB2 projects. Triggers: ProjectCreator, Frb2Project, IsMaintainedByGlue, CodeWritePolicy, GenerateCode, Frb2CodeGenerator, "could not determine project type", LocateSolution.
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

`Frb2Project.ContentDirectory` and `GlueProjectSubdirectory` are both `"Content/FrbEditor/"` — unlike
FRB1, the `.gluj` does *not* sit beside the `.csproj`. Everything Glue authors for an FRB2 project lives
under that one folder so deleting it removes every trace of the editor, and every relative path in the
JSON resolves against it.

## FRB2 code generation is opt-in, and not via `IsMaintainedByGlue`

`GlueProjectSave.GenerateCode` (default `false`) turns on typed accessors for an FRB2 project's
Screens/Entities, written by `Glue/CodeGeneration/Frb2CodeGenerator.cs` via `Frb2GenerateCodeCommands`.

**Landmine:** the opt-in is `CodeWritePolicy.GeneratesFrb2Code`, deliberately *not*
`ProjectBase.IsMaintainedByGlue`, which stays permanently `false` for `Frb2Project`. That flag gates all
four seams in the table above at once, so routing the setting through it hands an FRB2 project the entire
FRB1 pipeline: Glue rewrites the `.csproj`, and every generator in the landmine above writes its
FRB1-only output into a project that cannot compile it. `GenerateCodeCommands` therefore checks
`GeneratesFrb2Code` *before* the `IsMaintainedByGlue` early-out.

**Landmine:** `Frb2CodeGenerator` writes both halves of the partial class itself rather than through
`FileCommands.SaveIfDiffers` / `CodeProjectHelper.CreateAndAddPartialGeneratedCodeFile`. Those add a
nested `.csproj` item and honour the FRB1 write-gate, neither of which applies — FRB2's SDK-style project
already globs `**/*.cs`. Both halves resolve through `IFileCommands.GetGeneratedCodeFilePath` /
`GetCustomCodeFilePath`, which are rooted at `CurrentGlueProjectDirectory` (the `.gluj`'s folder).
Composing either from `FileManager.RelativeDirectory` instead is identical for FRB1 and splits the pair
across directories for FRB2.

The tree view's Code/Events nodes still key off `WritesCodeForCurrentProject`, so an opted-in project's
generated files show up on disk but not in Glue's Explorer tree.

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
