---
name: frb1-release
description: FRB1 (engine + Glue) release runbook — gh CLI sequence for Engine.yml/glue.yml, version scheme, release notes. Triggers: cutting a release, IsBeta, BuildServerUploaderConsole, NuGet publish, changeengineversion.
---

# FRB1 Release

Releasing FRB1 (engine + Glue/FRBDK) is semi-automated: `gh` CLI triggers and watches the two release workflows, but every hard-to-reverse step (a workflow run that publishes to nuget.org or pushes to the prod FTP, and publishing the GitHub release) needs explicit user go-ahead first — treat each **CHECKPOINT** below as a stop, not a formality.

## Pipeline

| File | Role |
|---|---|
| `.github/workflows/Engine.yml` | "Build Engine DLLs" — bumps engine version, builds+publishes engine NuGets across 5 platforms |
| `.github/workflows/glue.yml` | "FlatRedBall Editor" — bumps FRBDK version, builds Glue, zips+FTPs FRBDK.zip |
| `FRBDK/BuildServerUploader/BuildServerUploaderConsole/Program.cs` | The actual tool both workflows shell out to (`changeengineversion`, `changefrbdkversion`, `zipanduploadtemplates`, `zipanduploadfrbdk`, ...) |
| `Processes/UpdateAssemblyVersions.cs` | Version-bumping logic — see Version scheme below |
| `Processes/CopyFrbdkAndPluginsToReleaseFolder.cs` (`DownloadGum`) | Pulls Gum's **latest GitHub release** `Gum.zip` asset and bundles it into FRBDK — see Gum dependency below |
| [docs.flatredball.com/flatredball/contributing/builds](https://docs.flatredball.com/flatredball/contributing/builds) | The narrative doc — kept current, cross-check before deviating from this skill |

Both workflows are `workflow_dispatch`-only, run against whatever is currently on the default branch (not a tag/push trigger) — confirm local `main` matches `origin/main` before dispatching.

## Release sequence

1. `git status`, `git fetch`, confirm local `main` == `origin/main`. If this release needs a new Gum tool version, publish it first via Gum's own release process (`gum-release`/`gum-monthly-release` skills in the Gum repo) — see Gum dependency below.
2. **CHECKPOINT — publishes `-beta` NuGet packages, not revocable.**
   `gh workflow run Engine.yml -f IsBeta=true`
3. `gh run list --workflow=Engine.yml --limit 1` → grab the run id → `gh run watch <id> --exit-status`.
4. *(Manual, human-only)* Point a test project at the new `-beta` NuGet version and sanity-check it. Claude can't drive this step.
5. **CHECKPOINT — real NuGet publish + prod FTP template upload.**
   `gh workflow run Engine.yml -f IsBeta=false`
6. `gh run list --workflow=Engine.yml --limit 1` → `gh run watch <id> --exit-status`.
7. **CHECKPOINT — FTP push of FRBDK.zip to prod download.**
   `gh workflow run glue.yml`
8. `gh run list --workflow="FlatRedBall Editor" --limit 1` → `gh run watch <id> --exit-status`.
9. *(Manual, human-only, per GitBook)* Download latest FRBDK, run Glue, confirm version; create a new platformer project and check its `.csproj` version. Also smoke-test against Kid Defense, Cranky Chibi Cthulhu, Battlecrypt Bombers, and the Automated Test Project.
10. Draft release notes (see below).
11. **CHECKPOINT — before making the release public.**
    `gh release create <tag> --draft --notes-file <path> --title "<Month DD, YYYY>"`, review, then `gh release edit <tag> --draft=false`.
12. *(Manual, human-only)* Post to Discord and share on Twitter/X, per GitBook.

## Version scheme (landmine)

Engine/FRBDK versions are **date+time based, not semver**: `yyyy.M.d` + `.` + minutes-since-midnight, e.g. a run at 2:03am → `<year>.<month>.<day>.123`. This is what makes same-day re-runs not collide.

- Beta (`IsBeta=true`) appends `-beta` to that string, and **skips `AssemblyInfo.cs` entirely** — only the `.csproj` `<Version>` and template NuGet `PackageReference` versions are touched, since beta is NuGet-only.
- `changefrbdkversion` (glue.yml) always calls `UpdateAssemblyVersions` with `isBeta:false` hardcoded — **Glue/FRBDK itself never gets a beta version**, only the engine can be beta.

## Build matrix (landmine)

Engine.yml builds 5 platforms, each Debug *and* Release — but **only Debug is published to NuGet today**; Release is built and uploaded as a workflow artifact only (the YAML comment literally says "we don't (yet?) publish any release nuget packages"). Don't expect a Release NuGet to show up.

| Platform | Framework |
|---|---|
| Web (Kni) | net8.0 |
| iOS | net8.0 |
| Android | net8.0 |
| FNA | net7.0 |
| DesktopGL | net6.0 |

glue.yml's build matrix only runs `Debug` (Release is commented out).

`Program.cs` also has a `zipanduploadgum` command wired into the manual/debug code path, but **no workflow in this repo calls it** — it uploads Gum to FRB's FTP, a path that's no longer used (see Gum dependency below).

## Building Glue for local testing (landmine)

`Glue.csproj` is just the core editor lib/exe (`GlueFormsCore.exe`) — it does **not** reference plugin projects like `GumPlugin.csproj`. Plugins are separate projects built independently and copied into `Glue/Glue/bin/<Config>/Plugins/<PluginName>/<PluginName>.dll`, which is where Glue actually loads them from at runtime. **`dotnet build FRBDK/Glue/Glue/Glue.csproj` silently leaves that Plugins folder untouched** — no error, no warning, just a stale plugin DLL sitting next to a freshly-built exe. If you're testing a plugin change (e.g. anything in `GumPlugin`), you must build either the whole solution (`dotnet build "FRBDK/Glue/Glue with All.sln"`) or that plugin's `.csproj` explicitly — building `Glue.csproj` alone is not enough and will make it look like your fix "didn't work."

## Gum dependency (landmine)

`glue.yml` bundles Gum into FRBDK by downloading whatever GitHub currently reports as Gum's **latest** release (`https://github.com/vchelaru/Gum/releases/latest/download/Gum.zip`) — Gum is released on GitHub only, not FTP. This means **running glue.yml before a needed Gum change has been released on GitHub silently bundles the previous Gum build**, with no error. If this release depends on new Gum functionality, publish Gum's release first (Gum repo's `Build and Release Gum Tool` workflow) and confirm it's visible at `gh release view --repo vchelaru/Gum` before running glue.yml here.

## Secrets & what Claude can't verify

`NUGET_APIKEY`, `FTPUSERNAME`, `FTPPASSWORD` are GitHub Actions repo secrets — never passed via `gh workflow run`, never inspectable. "Did it actually publish" is inferred from **workflow run success** (the `dotnet nuget push`/FTP steps fail the job on error) — there's no separate nuget.org query scripted here; check nuget.org's listing page by hand if you want to confirm directly.

## Release notes

Don't hardcode the notes template — pull the real one live: `gh release list --limit 3` to find the previous tag, then `gh release view <prev-tag> --json body -q .body`. Recent format: an intro line (no binaries in-release, link to docs site), `## Breaking Changes`, `## Biggest Changes` (often with a screenshot), then per-component `##` sections (`FRB Editor`, `Edit Mode`, `FRB Engine and Code-related`, ...) as bullet lists, crediting contributors inline (`thanks @user`).

Tags follow `Release_<Month>_<Day>_<Year>` (e.g. `Release_September_23_2025`); some older tags drop the `Release_` prefix. This naming is unrelated to the NuGet version string above — don't conflate the two.

## GitBook doc

[docs.flatredball.com/flatredball/contributing/builds](https://docs.flatredball.com/flatredball/contributing/builds) is the narrative release doc (source at `contributing/builds/README.md` and `contributing/builds/gum.md` in the separate `FlatRedBallDocs` GitBook repo, not this one) — it's the place to update the step-by-step checklist itself; this skill should stay a pointer + landmine list, not a second copy of it.
