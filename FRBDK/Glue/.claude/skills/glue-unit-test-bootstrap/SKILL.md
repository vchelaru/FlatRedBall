---
name: glue-unit-test-bootstrap
description: Bootstrapping GlueUnitTests that touch GlueState.Self/GlueCommands.Self/ProjectManager. Triggers: NullReferenceException in tests from ProjectManager.CodeProjectHelper, FileWatchManager, EditorObjects.IoC.Container, or MainGlueWindow.Self.Invoke.
version: 1.0.0
---

# Glue Unit Test Bootstrap

`GlueCommands.Self`/`GlueState.Self` aren't usable out of the box in a plain xunit host — Glue.exe's real
startup (`Program.cs`/`MainGlueWindow.cs`) does several one-time registrations first. Any test that drives
production code through those statics (not just calling a pure static method directly) needs:

```csharp
GlueUnitTests.TestSupport.GlueTestBootstrap.EnsureInitialized();
```

Call it in the test's constructor. It's idempotent (safe to call every test, cheap after the first call).
Skipping it is what produces the NRE chain: DI container not built, legacy `EditorObjects.IoC.Container`
locator not registered, `ProjectManager.CodeProjectHelper` null, `FileWatchManager` not initialized.

None of this is faked — `GlueTestBootstrap` runs the same production init calls Glue.exe makes, just
outside a live WinForms app. See its doc comment at
`FRBDK/Glue/Tests/GlueUnitTests/TestSupport/GlueTestBootstrap.cs` for exactly what it registers.

## Sibling seams

Also process-wide static state a test may need to control alongside the bootstrap:

- **`TaskManager.SynchronousMode`** — set `true` to run `TaskManager.Self.Add`/`AddAsync` inline instead of
  on the background thread. See `GlueUnitTests/Tasks/TaskManagerSynchronousModeTests.cs`.
- **`TaskManager.UiThreadMarshaller`** — swap in an inline `IUiThreadMarshaller` to avoid needing a real
  WinForms message loop for `DoOnUiThread`/`OnUiThread` calls.
- **`TestVisualStudioProjectFactory`** (`GlueUnitTests/TestSupport/TestVisualStudioProjectFactory.cs`) —
  builds a real (not fake) `VisualStudioProject` from a minimal non-SDK `.csproj`, for tests that need
  `GlueState.CurrentMainProject` to be non-null.

Both `TaskManager` statics mutate process-wide state, so test classes that touch them share a
non-parallel xunit collection — see `TaskManagerSequentialCollection` in the file above and reuse it
rather than inventing a new one.

## Deep dive

For the full story of why each seam was needed and what's still NRE-prone (e.g. `AvailableAssetTypes.CommonAtis`
requiring real `PluginManager` plugin loading), see `REFACTORING.md`'s "Unblock WizardProjectLogic
AddGameScreen testing" and "Wizard apply-engine test seams" entries.
