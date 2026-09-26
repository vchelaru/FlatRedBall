---
name: glue-manual-repro
description: Manually launching a built GlueFormsCore.exe to repro/verify an editor UI bug (layout, startup state, first-launch behavior). Triggers: GlueFormsCore.exe, %AppData%\GlueFormsCore, settings.xml, GlueLayoutSettings.xml, simulating first launch.
---

For investigating a UI/layout bug that a unit test can't pin alone (timing, first-launch state, window
rendering) — driving the real `GlueFormsCore.exe` yourself is fine; the process doc's "don't drive the
tool yourself" rule is about the final user-facing manual-test step, not internal repro.

## Landmines

- **Build `Glue with All.sln`, not `Glue.csproj` alone.** A `Glue.csproj`-only build boots with no
  plugins loaded (Explorer tree, property grid, etc. all silently missing) — the window looks badly
  broken in a way that has nothing to do with the bug you're chasing. Costs a full rebuild cycle to
  notice.
- **`settings.xml`'s `LastProjectFile` must be the `.csproj`, not the `.gluj`.** Glue opens it via
  `Microsoft.Build.Evaluation.Project`; a `.gluj` path throws a real "Could not load the project" popup.
- **User profile lives at `%AppData%\GlueFormsCore\settings.xml` and `GlueLayoutSettings.xml`.** Back
  both up before simulating a fresh/first-launch profile (delete or rewrite them), restore after —
  these are the developer's real Glue preferences, not scratch files.
- **A backgrounded/unfocused window doesn't repaint.** `Program.cs` forces
  `RenderOptions.ProcessRenderMode = SoftwareOnly`; a screenshot of an occluded or non-foreground window
  shows stale/blank content. Bring it to the foreground and maximize (`ShowWindow`/`SetForegroundWindow`
  via P/Invoke in PowerShell) immediately before capturing.
- **A project auto-load on startup can take minutes**, not seconds (MSBuild restore, codegen) — a window
  title still reading generic "FlatRedBall Editor" (no project path) means it hasn't finished; don't
  read the UI state as final until the title shows the loaded `.csproj` path.
- **`exitwhenquiet` may never fire.** The automation hook
  (`MainGlueWindow.StartExitWhenQuietWatcherIfRequested`) exits only once
  `TaskManager.Self.AreAllAsyncTasksDone` has held for N seconds, and on some projects that never
  happens — Glue loads fine and then just stays open (issue #2053). Don't build a headless
  verify loop on a clean exit; launch, wait for the title to show the `.csproj`, kill, and assert on
  files written.

## Reusable pattern

Self-serve-log via a temporary `File.AppendAllText` probe in the suspect code path (see the
`self-serve-logging` skill), launch the exe from Bash/PowerShell, drive it headlessly where possible
(writing `settings.xml` directly beats simulating clicks), then read the log — don't ask the user to
transcribe anything.
