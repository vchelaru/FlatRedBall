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

`RegisterPluginForTesting` adds a plugin to both `mPluginContainers` *and* `ImportedPlugins`. Those back
different dispatch paths: `CallPluginMethod` walks the containers, but every `ReactTo*` event enumerates
`ImportedPlugins`. A plugin in only the first answers direct calls and is never notified of anything —
which reads as "the plugin ran and chose to do nothing," not as a wiring bug.

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

## Loading a whole real project (gold projects)

`GoldProject`/`GoldProjectCompileTests` (`GlueUnitTests/TestSupport`, `GlueUnitTests/Projects`) drive a
checked-in sample through the real `ProjectLoader.LoadProject`, regenerate, and build. That is the only way
to cover generators that ask *another* plugin something at generation time. Needs
`GlueTestBootstrap.EnsureGameProjectPluginsRegistered()` and `[StaFact]`. Two non-obvious requirements:

- **Delete `*.Generated.cs` first.** Glue doesn't rewrite a file whose content is unchanged, so otherwise
  "codegen produced this" and "codegen never ran" are indistinguishable.
- **`*.Generated.cs` is gitignored repo-wide**, and the samples are checked in without it. A clean
  `git diff` after regenerating proves nothing, and no sample builds until Glue regenerates it. Assert
  nothing about these files *existing beforehand* — your working copy has them from earlier runs and a
  fresh CI clone does not. Before pushing, `find Samples -name "*.Generated.cs" -delete` and re-run, or
  CI is the first thing to see the real starting state.

## Landmine — a WinForms sync context plus SynchronousMode deadlocks the run with no timeout

WinForms installs a `WindowsFormsSynchronizationContext` on a thread the moment the first control is created
there — `FakeMainGlueWindow`'s `PropertyGrid`, a `MenuStrip`, a plugin toolbar. Continuations then post back
to it and only run while a message loop pumps, which no test host does. Combined with
`TaskManager.SynchronousMode` (whose `RunSynchronously` blocks the caller on the awaited task), the first
plugin that awaits during a load hangs forever: one blocked thread, no child process, nothing executing, and
no timeout anywhere. `GlueTestBootstrap` sets `WindowsFormsSynchronizationContext.AutoInstall = false`; don't
undo it, and don't diagnose this shape as a slow build. A stack dump (`dotnet-stack report -p <pid>`, real
Windows PID) showing exactly one blocked thread and nothing else running is the signature.

## Landmine — do NOT give `PluginManager` a `MenuStrip`

`GlueGui.Initialize(menuStrip)` is needed (`PluginBase.AddMenuItemTo` reads `GlueGui.MenuStrip.Items`).
`PluginManager.ShareMenuStripReference` is the opposite: `PluginCommand` marshals every plugin call through
`mMenuStrip.Invoke` whenever `mMenuStrip` is non-null, so with no message loop the calls silently never run
and `CallPluginMethod` returns null. Leaving PluginManager's null keeps its "no live menu strip means run
inline" guard doing the right thing.

## Landmine — code generation swallows its own exceptions

`GenerateAllCodeSync` calls the `async Task CodeWriter.GenerateCode` **without awaiting it**, so an exception
during one element's generation is captured into a discarded Task and lost. The symptom is an empty
`.Generated.cs` (the placeholder from `CreateGeneratedFileIfNecessary`) for that one element, no error
anywhere, and every later step for it — its factory, its project entry — silently skipped. To see the real
exception, `await CodeWriter.GenerateCode(element)` directly. `ErrorRecordingPlugin` subscribes to Glue's
error *and* output channels so a partial regeneration fails a test rather than passing quietly.

## Landmine — `MSBUILD_EXE_PATH` must be set before the *first* MSBuild evaluation in the process

`Microsoft.Build` caches its toolset on first use, so setting the variable later has no effect. A test that
evaluates a bare non-SDK project first (`TestVisualStudioProjectFactory`) fixes the toolset for the whole
run, and a later real-project load then fails with `The SDK 'Microsoft.NET.SDK.WorkloadAutoImportPropsLocator'
specified could not be found` — while passing when run alone. Call
`GlueTestBootstrap.EnsureMsBuildEnvironmentVariable()` before anything touches MSBuild.

## Landmine — calling a real plugin method for the first time can pop a real dialog on the developer's desktop

`IMainGlueWindow`/`IUiThreadMarshaller` only cover calls that were already routed through `MainGlueWindow.Self`/`TaskManager`. A plugin method you're calling directly for the first time (bypassing `PluginManager.CallPluginMethod`'s silent no-op to get real coverage — see `REFACTORING.md`'s Collision/Gum entries) may still contain its own unseamed `MessageBox.Show(...)` on some branch (e.g. an "already exists, overwrite?" check). That call blocks the test thread on a real, visible modal dialog on the *developer's actual desktop* — not something `GlueTestBootstrap` catches, and not obvious from reading the test in isolation.

Before exercising a plugin method's real logic for the first time: skim it (and what it calls) for `MessageBox.Show`/similar dialog calls, and design the test to never take that branch (fresh unique temp directory per test, `askToOverwrite`/equivalent flags set to avoid the prompt, call the method at most once per test run rather than twice to probe an idempotency branch). If you ever see an unexpected pause or the user reports a popup, stop immediately, confirm no process is still blocked waiting on it, and fix the test to avoid the branch rather than building a dialog seam just to unblock one test.

## Landmine — `dotnet test` on the bare csproj fails with `MSB3073`/`*Undefined*` paths

Running `dotnet test FRBDK/Glue/Tests/GlueUnitTests/GlueUnitTests.csproj` directly fails building `OfficialPlugins.csproj`'s `PostBuild` target: it copies its output using `$(SolutionDir)Glue\bin\Debug\Plugins\...`, and `SolutionDir` is a solution-scoped MSBuild property that's simply undefined when you build/test a project file directly (no `.sln` in the invocation) — same failure would hit any of the plugin projects with a similar post-build copy step, not just OfficialPlugins.

Fix: pass `SolutionDir` explicitly, pointed at `FRBDK/Glue/` (the directory containing `Glue with All.sln`, since the copy path is `$(SolutionDir)Glue\...`), with a trailing backslash:

```
dotnet test FRBDK/Glue/Tests/GlueUnitTests/GlueUnitTests.csproj -p:SolutionDir="<repo>\FRBDK\Glue\\"
```

## What a healthy run costs — anything far past this is a hang, not slowness

Rough orders of magnitude on a warm dev machine, so "is it stuck or just slow?" is answerable without
guessing. All with `--no-build` after a successful build:

| Run | Roughly |
| --- | --- |
| Warm incremental build of `GlueUnitTests.csproj` | ~40s (a real build is most of a full run's wall clock) |
| `--filter "Category!=BuildSmoke"` (~280 tests) | ~25s |
| `--filter "Category=BuildSmoke"` (~10 tests) | ~60s |

A BuildSmoke run sitting at 5+ minutes is not a slow build. Check whether the child `dotnet build` has
already exited while `testhost` is still alive — that is the signature of the pipe hang below, not of work
in progress.

## Landmine — every nested `dotnet` call must go through `NestedDotnetCli`

BuildSmoke tests shell out to real `dotnet build`/`dotnet run`. The obvious way to write that — redirect
both streams, `StandardOutput.ReadToEnd()`, `WaitForExit()` — hangs for 10–15 minutes instead of failing,
because `dotnet build` starts MSBuild worker nodes with `/nodeReuse:true` that outlive it while holding the
inherited stdout pipe open, so `ReadToEnd()` never sees EOF. It is intermittent by nature: only the run that
*starts* the nodes inherits the handles, so the same test hangs once and passes on an immediate retry.

`GlueUnitTests/TestSupport/NestedDotnetCli.cs` is the only sanctioned way to spawn one — it disables the
persistent build servers, pumps both streams concurrently, and kills the entire child tree on timeout.
`NestedDotnetCliTests` fails the build if a redirected `Process.Start` reappears anywhere in the assembly.
See GitHub issue #1969.

## Standing rule — clear stragglers after an *interrupted* run

A `dotnet test`/`dotnet build` that is cancelled or killed mid-flight leaves its tree (`dotnet`, MSBuild
nodes, `VBCSCompiler`, `testhost`) behind; a run that completes normally now cleans up after itself. So
this is a response to an interruption, not a ritual before every command:

```powershell
Get-Process testhost,vstest.console,MSBuild,VBCSCompiler -ErrorAction SilentlyContinue | Stop-Process -Force
```

Never end a turn (report status, stop) while a `dotnet test`/`dotnet build` is still running detached in
the background — wait for it synchronously, or explicitly confirm it finished/kill it first. A background
run left alive across a stop/resume cycle is how the count climbs into the dozens over a long session.

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
