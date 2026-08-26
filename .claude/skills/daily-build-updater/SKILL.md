---
name: daily-build-updater
description: Glue daily-build self-update and its Windows replacement helper. Triggers: DoInstallDailyBuild, DailyBuildUpdateLauncher, GlueDailyBuildUpdate.log, GlueDailyBuild.zip.
---

## Lifecycle

| File | Responsibility |
|---|---|
| `FRBDK/Glue/Glue/Plugins/EmbeddedPlugins/AboutPlugin/AboutViewModel.cs` | Confirmation, download, staging, and helper hand-off. |
| `FRBDK/Glue/Glue/Plugins/EmbeddedPlugins/AboutPlugin/DailyBuildUpdateLauncher.cs` | Generates the detached PowerShell replacement helper. |
| `FRBDK/Glue/Glue/Plugins/EmbeddedPlugins/AboutPlugin/DailyBuildUpdateDiagnostics.cs` | Bounded lifecycle log at `%LOCALAPPDATA%\FlatRedBall\GlueDailyBuildUpdate.log`. |
| `FRBDK/Glue/Tests/GlueUnitTests/AboutPlugin/DailyBuildUpdateLauncherTests.cs` | Script parsing, path normalization, lifecycle, and diagnostic tests. |

The running editor stages `GlueDailyBuild.zip` beside its installation, starts the helper, exits, then the helper deletes the old directory, moves staging into place, and restarts Glue.

## Landmines

- `FileManager.GetDirectory` supplies a trailing separator. Apply `Path.TrimEndingDirectorySeparator` before deriving the helper working directory; otherwise PowerShell runs inside and locks the directory it must delete.
- Generated PowerShell must be parsed by a real `powershell.exe` test. In interpolated strings, delimit a variable immediately followed by `:` as `${variable}:`.
- Keep the lifecycle log. Its last successful phase separates host hand-off, helper startup/parsing, Glue exit, file replacement, and restart failures. On a real replacement failure, Restart Manager lock owners belong in that log.
