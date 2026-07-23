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

## Landmine — calling a real plugin method for the first time can pop a real dialog on the developer's desktop

`IMainGlueWindow`/`IUiThreadMarshaller` only cover calls that were already routed through `MainGlueWindow.Self`/`TaskManager`. A plugin method you're calling directly for the first time (bypassing `PluginManager.CallPluginMethod`'s silent no-op to get real coverage — see `REFACTORING.md`'s Collision/Gum entries) may still contain its own unseamed `MessageBox.Show(...)` on some branch (e.g. an "already exists, overwrite?" check). That call blocks the test thread on a real, visible modal dialog on the *developer's actual desktop* — not something `GlueTestBootstrap` catches, and not obvious from reading the test in isolation.

Before exercising a plugin method's real logic for the first time: skim it (and what it calls) for `MessageBox.Show`/similar dialog calls, and design the test to never take that branch (fresh unique temp directory per test, `askToOverwrite`/equivalent flags set to avoid the prompt, call the method at most once per test run rather than twice to probe an idempotency branch). If you ever see an unexpected pause or the user reports a popup, stop immediately, confirm no process is still blocked waiting on it, and fix the test to avoid the branch rather than building a dialog seam just to unblock one test.

## Deep dive

For the full story of why each seam was needed and what's still NRE-prone (e.g. `AvailableAssetTypes.CommonAtis`
requiring real `PluginManager` plugin loading), see `REFACTORING.md`'s "Unblock WizardProjectLogic
AddGameScreen testing" and "Wizard apply-engine test seams" entries.
