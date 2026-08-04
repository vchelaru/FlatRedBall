---
name: glue-unit-test-bootstrap
description: Bootstrapping GlueUnitTests that touch GlueState.Self/GlueCommands.Self/ProjectManager. Triggers: NullReferenceException in tests from ProjectManager.CodeProjectHelper, FileWatchManager, EditorObjects.IoC.Container, or MainGlueWindow.Self.Invoke.
version: 1.2.0
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

- **`ObjectFinder.Self.GlueProject`** — assign a `GlueProjectSave` for anything reaching `ObjectFinder`
  lookups (`GetEntitySaveUnqualified`, `GetAllReferencedFiles`).

Every one of these is process-wide, which is why the whole assembly runs non-parallel — see
`GlueUnitTests/AssemblyInfo.cs`. So cross-class interleaving isn't a hazard, but leakage still is: a test
which assigns one of these must restore it (`IDisposable`/`try-finally`), or it silently changes the
setup of whatever runs next. The failure mode is a test that passes under `--filter` and fails in the
full run.

## Landmine — calling a real plugin method for the first time can pop a real dialog on the developer's desktop

`IMainGlueWindow`/`IUiThreadMarshaller` only cover calls that were already routed through `MainGlueWindow.Self`/`TaskManager`. A plugin method you're calling directly for the first time (bypassing `PluginManager.CallPluginMethod`'s silent no-op to get real coverage — see `REFACTORING.md`'s Collision/Gum entries) may still contain its own unseamed `MessageBox.Show(...)` on some branch (e.g. an "already exists, overwrite?" check). That call blocks the test thread on a real, visible modal dialog on the *developer's actual desktop* — not something `GlueTestBootstrap` catches, and not obvious from reading the test in isolation.

Before exercising a plugin method's real logic for the first time: skim it (and what it calls) for `MessageBox.Show`/similar dialog calls, and design the test to never take that branch (fresh unique temp directory per test, `askToOverwrite`/equivalent flags set to avoid the prompt, call the method at most once per test run rather than twice to probe an idempotency branch). If you ever see an unexpected pause or the user reports a popup, stop immediately, confirm no process is still blocked waiting on it, and fix the test to avoid the branch rather than building a dialog seam just to unblock one test.

## Landmine — `dotnet test` on the bare csproj fails with `MSB3073`/`*Undefined*` paths

Running `dotnet test FRBDK/Glue/Tests/GlueUnitTests/GlueUnitTests.csproj` directly fails building `OfficialPlugins.csproj`'s `PostBuild` target: it copies its output using `$(SolutionDir)Glue\bin\Debug\Plugins\...`, and `SolutionDir` is a solution-scoped MSBuild property that's simply undefined when you build/test a project file directly (no `.sln` in the invocation) — same failure would hit any of the plugin projects with a similar post-build copy step, not just OfficialPlugins.

Fix: pass `SolutionDir` explicitly, pointed at `FRBDK/Glue/` (the directory containing `Glue with All.sln`, since the copy path is `$(SolutionDir)Glue\...`), with a trailing backslash:

```
dotnet test FRBDK/Glue/Tests/GlueUnitTests/GlueUnitTests.csproj -p:SolutionDir="<repo>\FRBDK\Glue\\"
```

## Landmine — an orphaned `testhost` locks the output DLLs, and the next run looks like a hang

A cancelled or timed-out `dotnet test` can leave `testhost.exe` alive holding `GlueUnitTests/bin/.../*.dll` open. The next run then can't copy dependencies into `bin`, and fails with `MSB3021`/`MSB3027` naming the holder (`The file is locked by: "testhost (PID)"`). The trap is that the *build* stalls through its 10 retries × 1s per file while producing no test output — piped through a `grep`, that buffers to nothing and reads as a hung test suite rather than a locked file.

Before diagnosing a slow or silent run as a test problem, clear the holders:

```powershell
Get-Process testhost,vstest.console,MSBuild -ErrorAction SilentlyContinue | Stop-Process -Force
```

Then re-run with `--no-build` when nothing was recompiled since the last successful build — it skips the copy step the lock blocks, and the full non-BuildSmoke suite finishes in seconds.

## Landmine — a broad `FullyQualifiedName~` filter silently sweeps in `Category=BuildSmoke` tests and thrashes the machine

`GumRuntimeMemberContractTests`, `GumGeneratedCodeCompilesTests`, and the `*CreationSmokeTests` classes are
tagged `[Trait("Category", "BuildSmoke")]` because they shell out to real `dotnet build`/`dotnet test`
child processes (building the whole engine or a scaffolded project) instead of just exercising codegen
in-memory. `pr-tests.yml`/`glue.yml` deliberately run them as a separate `Category=BuildSmoke` step, and
run everything else with `--filter "Category!=BuildSmoke"`.

A filter like `--filter "FullyQualifiedName~GumPlugin"` does not know about that split — it matches
BuildSmoke tests in the same namespace right along with the fast ones. Run several of those together and
each spawns its own nested `dotnet`/`testhost`/`VBCSCompiler` tree; a handful of them running serially is
enough to make the machine crawl and a run that should take seconds look hung for many minutes.

Always AND in `Category!=BuildSmoke` unless you specifically intend to run the slow build-smoke suite:

```
dotnet test FRBDK/Glue/Tests/GlueUnitTests/GlueUnitTests.csproj -p:SolutionDir="<repo>\FRBDK\Glue\\" --filter "FullyQualifiedName~GumPlugin&Category!=BuildSmoke"
```

## Deep dive

For the full story of why each seam was needed and what's still NRE-prone (e.g. `AvailableAssetTypes.CommonAtis`
requiring real `PluginManager` plugin loading), see `REFACTORING.md`'s "Unblock WizardProjectLogic
AddGameScreen testing" and "Wizard apply-engine test seams" entries.
