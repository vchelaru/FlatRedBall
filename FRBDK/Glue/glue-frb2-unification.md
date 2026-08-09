# Glue + FRB2 unification — Glue-side work log

The counterpart to `FlatRedBall2/design/glue-frb2-unification.md`, which records the FRB2 side. This
file records what has been done **in the Glue repo**, on branch `2021-frb2-project-mode` / PR #2023,
tracking issue #2021.

An FRB2 game reads Glue's `.gluj`/`.glsj`/`.glej` at runtime and owns all of its own C#, so Glue's
job for one of these projects is to author that JSON and nothing else: no generated code, no
`.csproj` writes.

## The two things everything hangs off

**`Frb2Project`** (`Glue/VSHelpers/Projects/Frb2Project.cs`) — the project type, chosen by
`ProjectCreator` *before* its `DefineConstants` cascade, because an FRB2 `.csproj` sets none of the
constants that cascade keys off (measured: `TRACE;DEBUG`) and the cascade fails a load outright
rather than degrading. Detection is a rule list (`Frb2ProjectDetector`) with one rule today: a
`ProjectReference` named `FlatRedBall2.csproj`. A nuget rule gets added when FRB2 ships a package.

**`ProjectBase.IsMaintainedByGlue`** — false for `Frb2Project`, and the single question every
suppression asks. The `.csproj` stays fully loaded and read; only writes go away.

## Where writes are suppressed

| Seam | Stops |
| --- | --- |
| `VisualStudioProject.Save`, and `Load`'s duplicate-resolution save | every `.csproj` write |
| `GlueCommands.GenerateCodeCommands` → `NoCodeGenerationCommands` | everything reached through `IGenerateCodeCommands` |
| `CodeWritePolicy`, consulted by `FileCommands.SaveIfDiffers` and `ProjectCommands.CreateAndAddCodeFile`/`CreateAndAddPartialFile` | any `.cs` written, whoever wrote it |
| `CodeBuildItemAdder.PerformAddInternal` | embedded resource drops (Performance/*, Tiled's) |
| `ProjectCommands.AddFileToContentProject` | content items added to the `.csproj` |
| `CodeProjectHelper.CreateAndAddPartialGeneratedCodeFile` | every `.Generated.cs` |
| `ProjectLoader.CheckForMissingCustomFile` | the load-time "re-create this missing `.cs`?" prompt |
| `BuildLogic.TryRemoveXnbReferences` / `TryAddXnbReferencesAndBuild` | content-pipeline work with no platform to build for |

The last four are not redundant with the no-op `IGenerateCodeCommands`. A real amount of Glue's
generation never goes through that interface — `CodeBuildItemAdder`, `CameraSetupCodeGenerator` and
`AliasCodeGenerator` all write on glux load — which is why suppressing only the interface left files
appearing in a user's project.

## Layout: one folder, `Content/FrbEditor/`

`ProjectBase.GlueProjectSubdirectory` (empty by default, so projects Glue maintains are unchanged)
puts an FRB2 project's `.gluj`, `Screens/*.glsj`, `Entities/*.glej`, `GlueSettings/` **and referenced
content** under `Content/FrbEditor/`. `Frb2Project.ContentDirectory` is that same folder, so content
and project share one root.

Two properties this buys, both load-bearing:

- A user can delete that one folder and no trace of the editor is left. `GlueSettings/` being copied
  to output along with it is the accepted cost of that.
- The game copies it with one rule and no exclusions:
  `<Content Include="Content\FrbEditor\**\*.*" CopyToOutputDirectory="PreserveNewest" />`. A glob
  rooted at the project directory cannot do this — it also matches `bin`/`obj`, so each build copies
  the previous build's copies in again. That reached eleven levels deep in a real project.

FRB2's half of the contract: every referenced file resolves relative to the `.gluj`, with no fixed
`Content` segment in between.

## Landmines

- **`GlueState.CurrentGlueProjectDirectory` was derived from the `.csproj`**, with a doc comment
  asserting the two are the same directory. ~90 call sites inherit it. Symptoms are quiet: "View in
  Explorer" opened the desktop, because Explorer falls back there when handed a nonexistent path.
- **`ProjectSyncer.LocateSolution` throws** when it finds no solution, and that surfaces during load
  — so an unrecognized solution format is a project that will not open. `.slnx` support was added for
  exactly this.
- **`ProjectCommands.CreateAndAddCodeFile` returns null for two different reasons** now: the file is
  already in the project, or the project takes no code files. Callers that read null as only the
  first told users a nonexistent `NewScreen.cs` would be reused, and `MakeBuildItemNested`
  dereferenced it.

## Not covered by tests

- `BuildLogic.TryAddXnbReferencesAndBuild`'s guard. It early-outs when the MonoGame builder is
  absent, which it always is in the test host, so any test passes with the guard disabled.
- Plugin generators that call `FileManager.SaveText` directly (Platformer/TopDown/Racing CSV and enum
  generators, Gum, GameCommunication) bypass `CodeWritePolicy` entirely. They only fire for projects
  that opt into those behaviors.
- `ReactToFileRemoved` and friends never reach a plugin in a headless host, so plugin-event-surface
  tests are vacuous — verify any such test by disabling the fix and watching it fail.

## Deliberately out of scope

Gum; custom code, ViewCode and AddEvent; hiding FRB1-only UI (present but inert); creating FRB2
projects or templates; nuget-based detection. `Content/TiledObjects.Generated.xml` is still written
on load — content, not code.
