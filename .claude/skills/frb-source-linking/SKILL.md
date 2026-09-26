---
name: frb-source-linking
description: What "Link Game to FRB Source" actually swaps in a game project's references, vs. the plain checker. Triggers: IsFrbSourceLinked, AddSourceManager, FrbSourcePlugin, GumCore.DesktopGlNet6.dll, stale prebuilt Gum/FRB binaries, Libraries\DesktopGl.
---

# Linking a Game Project to FRB Source

## Two different things — don't conflate them
- `VisualStudioProject.IsFrbSourceLinked()` (`Glue/VSHelpers/Projects/VisualStudioProject.cs`) is only a **checker**: true iff the project has a `<ProjectReference>` to `FlatRedBallDesktopGLNet6.csproj`. Reading only this tells you nothing about what else linking touches — it's easy to wrongly conclude linking only affects the core FRB engine reference.
- The actual **link action** is `AddSourceManager` (`FRBDK/Glue/OfficialPlugins/FrbSourcePlugin/Managers/AddSourceManager.cs`), and it's much broader.

## What linking actually does
For the target platform (`DesktopGlNet6`/`DesktopFNA`/`Web`/`AndroidNet8`/`IosNet8` — see the matching lists in `AddSourceManager.cs`), it removes the platform's prebuilt DLL/NuGet references (`RemoveDllReference`/`RemoveNugetReference` — `GumCore.DesktopGlNet6`, `SkiaInGum`, `FlatRedBall.GumCore.DesktopGlNet6`, etc.) and replaces them with real `<ProjectReference>`s into **both** FRB's engine projects (`FrbOrGum.Frb`) **and Gum's own** (`FrbOrGum.Gum`, e.g. `GumCore\GumCoreXnaPc\GumCore.DesktopGlNet6\GumCore.DesktopGlNet6.csproj`).

So linking source does fix "project's `GumCore.<platform>.dll` is stale relative to a Gum source change" — afterward the project builds `RenderingLibrary` (and everything else GumCore pulls in) live from the sibling `Gum` checkout instead of a copied binary.

**Landmine:** don't answer "does linking source fix a stale-Gum-binary compile error" by reading `IsFrbSourceLinked()` alone — it's the symptom check, not the fix mechanism. Read `AddSourceManager.cs`'s reference-swap lists instead.

## Unlinked (default) projects still ship prebuilt binaries
A project that's never been linked to source — the common case, every Glue-generated sample/new project — references Gum via a **committed, prebuilt** `Libraries\<Platform>\Debug\GumCore.<Platform>.dll`, copied in at project-creation time. Nothing in Glue refreshes it afterward (no `CopyLibraries`-equivalent exists). A Gum-side runtime addition (new type/member) is invisible to an unlinked project until either (a) it's linked to source, or (b) its `Libraries\...\GumCore.*.dll` is manually replaced with a fresh build. This includes the repo's own bundled sample/test projects (`Samples/Beefball`, `Tests/TestProjectDesktopNet6`, etc.) — their `Libraries\DesktopGl\...` DLLs are just as stale as a user's unlinked project unless explicitly refreshed.
