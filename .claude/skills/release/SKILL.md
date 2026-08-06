---
name: release
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

Both workflows are `workflow_dispatch`-only, run against whatever is currently on the default branch (not a tag/push trigger) — confirm local `NetStandard` matches `origin/NetStandard` before dispatching. **The default branch is `NetStandard`, not `main`.**

`glue.yml` runs the Glue unit tests *and* the `Category=BuildSmoke` new-project builds before the FTP upload, so a red test fails the release rather than shipping — expect it to take longer than a pure build, and read a failure as a real gate.

## Release sequence

1. `git status`, `git fetch`, confirm local `NetStandard` == `origin/NetStandard`. If this release needs a new Gum tool version, publish it first via Gum's own release process (`gum-release`/`gum-monthly-release` skills in the Gum repo) — see Gum dependency below.
   Then run `scripts/Test-DownstreamBuilds.ps1`, which compiles the games and in-repo projects the checklist names against the working tree, and dry-run the template steps (see below). The GitBook puts this smoke test *before* any workflow dispatch, which is the safer order — nuget.org publishes can't be revoked.
   Also run the slow gate the fast unit run skips, since it covers what a user's first five minutes actually exercise: `dotnet test "FRBDK/Glue/Glue with All.sln" -c Debug --filter "Category=BuildSmoke"` (new-project creation + build, and the Gum runtime contract sweep).
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
9. *(Manual, human-only, per GitBook)* Download latest FRBDK into a **fresh** folder, run Glue, confirm version. Then create a new project **with Gum and Forms enabled** — that combination is what exercises Gum codegen against the shipped runtime, and it is the path that has broken most often. Check its `.csproj` picked up the new engine version, and build it. Then *run* the smoke-test games — `Test-DownstreamBuilds.ps1` only proves they compile, never that they play.
   The wizard's friendly names don't match the internal template folder names — pick "Desktop GL .NET 9 (Windows, Mac, Linux) - MonoGame", which is `FlatRedBallDesktopGlMonoGameTemplate`. The `Net6` spelling surviving in the engine package ids is deliberate: renaming a published package id strands every existing project on the old one. The wizard's list (`FRBDK/Glue/NpcWpfLib/Data/EmptyTemplates.cs`) and the engine list in `AllData.cs` are maintained separately by hand and drift both ways; `NewProjectTemplateListTests` fails if a template is offered with no engine, or shipped with no wizard entry.
10. Draft release notes — invoke [[release-notes]].
11. **CHECKPOINT — before making the release public.**
    `gh release create <tag> --draft --notes-file <path> --title "<Month DD, YYYY>"`, review, then `gh release edit <tag> --draft=false`.
12. *(Manual, human-only)* Post to Discord and share on Twitter/X, per GitBook.

## Which workflow ships which fix (landmine)

The two workflows ship disjoint artifacts, so a hotfix usually needs only one of them:

| Fix touches | Re-run | Leaves alone |
|---|---|---|
| `Engines/**` (runtime) | `Engine.yml` | FRBDK |
| `FRBDK/Glue/**`, incl. all Gum codegen | `glue.yml` | engine NuGet packages |

A codegen bug reaches users through `FRBDK.zip`, not through the engine packages — re-running `Engine.yml` for one is pure churn, and it mints a *new* version string that supersedes the one you just announced. Relatedly, a failed `IsBeta=false` run that got as far as the NuGet push leaves those packages published; the re-run publishes a second, higher version rather than replacing them. The stranded set is harmless (identical content, and NuGet resolves to newest), so unlisting it is optional tidiness, not a correctness fix.

## Version scheme (landmine)

Engine/FRBDK versions are **date+time based, not semver**: `yyyy.M.d` + `.` + minutes-since-midnight, e.g. a run at 2:03am → `<year>.<month>.<day>.123`. This is what makes same-day re-runs not collide.

- Beta (`IsBeta=true`) appends `-beta` to that string, and **skips `AssemblyInfo.cs` entirely** — only the `.csproj` `<Version>` and template NuGet `PackageReference` versions are touched, since beta is NuGet-only.
- `changefrbdkversion` (glue.yml) always calls `UpdateAssemblyVersions` with `isBeta:false` hardcoded — **Glue/FRBDK itself never gets a beta version**, only the engine can be beta.

## Build matrix (landmine)

Engine.yml builds each enabled platform Debug *and* Release — but **only Debug is published to NuGet today**; Release is built and uploaded as a workflow artifact only (the YAML comment literally says "we don't (yet?) publish any release nuget packages"). Don't expect a Release NuGet to show up.

| Platform | Framework |
|---|---|
| Web (Kni) | net8.0 |
| FNA | net7.0 |
| DesktopGL | net6.0 |
| iOS | net8.0 — **can't build**, see below |
| Android | net8.0 — **can't build**, see below |

The mobile targets are `net8.0-ios`/`net8.0-android`, whose workloads are past end of life and are **no longer on the GitHub runner image**. `setup-dotnet` installs SDKs, not workloads, so the build fails with `NETSDK1140` — *"1.0 is not a valid TargetPlatformVersion for ios. Valid versions include: None."* Suppressing the EOL check (`CheckEolWorkloads=false`) only surfaces that underlying error; there is no workload to build against either way. Retargeting the mobile projects off net8 is the only real fix. Read Engine.yml itself for which platforms are currently wired up — the disabled steps are commented in place with restore instructions rather than deleted.

All five NuGet pushes happen in a single step after every platform has compiled, so the matrix is all-or-nothing — a build failure on any platform means nothing reaches nuget.org. Don't reintroduce per-platform publishing; `dotnet nuget push` can't be undone, and interleaving it is what makes a mid-matrix failure leave a half-shipped release.

glue.yml's build matrix only runs `Debug` (Release is commented out).

`Program.cs` also has a `zipanduploadgum` command wired into the manual/debug code path, but **no workflow in this repo calls it** — it uploads Gum to FRB's FTP, a path that's no longer used (see Gum dependency below).

## Building Glue for local testing (landmine)

`Glue.csproj` is just the core editor lib/exe (`GlueFormsCore.exe`) — it does **not** reference plugin projects like `GumPlugin.csproj`. Plugins are separate projects built independently and copied into `Glue/Glue/bin/<Config>/Plugins/<PluginName>/<PluginName>.dll`, which is where Glue actually loads them from at runtime. **`dotnet build FRBDK/Glue/Glue/Glue.csproj` silently leaves that Plugins folder untouched** — no error, no warning, just a stale plugin DLL sitting next to a freshly-built exe. If you're testing a plugin change (e.g. anything in `GumPlugin`), you must build either the whole solution (`dotnet build "FRBDK/Glue/Glue with All.sln"`) or that plugin's `.csproj` explicitly — building `Glue.csproj` alone is not enough and will make it look like your fix "didn't work."

## What can be CI-gated (landmine)

`*.Generated.cs` is gitignored repo-wide, with `Samples/BeefballKni` the only `!`-exception. Any project that depends on Glue codegen therefore **cannot build from a clean checkout** — including `Tests/TestProjectDesktopNet6` (the checklist's "Automated Test Project") and every sample but BeefballKni. Locally they build fine off untracked generated files already on disk, so adding one to a workflow produces a green local run and a red CI run.

The split this forces: `pr-tests.yml` gates what a clean checkout can build (Glue, `Tests/EngineUnitTests`, Forms under `DebugAutoBuild`); `scripts/Test-DownstreamBuilds.ps1` covers what needs a developer's machine (codegen-dependent projects plus the sibling game checkouts).

`DebugAutoBuild`/`ReleaseAutoBuild` are worth knowing separately: they're the configurations Glue uses to rebuild the engine during live edit, and the **only** ones where `FlatRedBall.Forms` references SkiaGum. Plain Debug/Release — all `Engine.yml` builds — never evaluate that reference, so breakage there is invisible to the release pipeline.

## Dry-running the template steps (landmine)

`copytotemplates` and `zipanduploadtemplates` run **only when `IsBeta=false`**, and they run *after* the NuGet push — so a beta run cannot exercise them and a failure there lands with packages already public. Dry-run them locally instead. From the directory *containing* the `FlatRedBall` and `Gum` checkouts (the file paths in `AllData.cs` are relative to that parent, not to the repo root):

```
dotnet build -c Debug   Engines/Forms/FlatRedBall.Forms/<Web|FNA|DesktopGLNet6>.sln   # and -c Release
dotnet build -c Debug   FRBDK/BuildServerUploader/BuildServerUploader.sln
./FlatRedBall/FRBDK/.../BuildServerUploaderConsole.exe copytotemplates
./FlatRedBall/FRBDK/.../BuildServerUploaderConsole.exe zipanduploadtemplates BAD_USER BAD_PASSWORD
```

Dummy credentials are the point: everything local (copy-to-release-folder, zip) executes for real and only the final SFTP fails with `SshAuthenticationException`, which is the pass condition. Afterwards `git checkout -- Templates/ && rm -f Templates/*.zip` — the template DLLs are tracked, so a dry run dirties ~90 files.

`AllData.cs`'s per-engine file lists are hand-maintained literal paths with no build-time validation, which is what makes this dry run worth doing: a project that moves on disk breaks the release and nothing else notices. SkiaInGum is the standing example — FRB forked it into `Engines/SkiaGum/`, and it is pulled from that project's own `bin` rather than the Forms output folder because Forms only references it under the AutoBuild configurations.

## Gum dependency (landmine)

`glue.yml` bundles Gum into FRBDK by downloading whatever GitHub currently reports as Gum's **latest** release (`https://github.com/vchelaru/Gum/releases/latest/download/Gum.zip`) — Gum is released on GitHub only, not FTP. This means **running glue.yml before a needed Gum change has been released on GitHub silently bundles the previous Gum build**, with no error. If this release depends on new Gum functionality, publish Gum's release first (Gum repo's `Build and Release Gum Tool` workflow) and confirm it's visible at `gh release view --repo vchelaru/Gum` before running glue.yml here.

## Secrets & what Claude can't verify

`NUGET_APIKEY`, `FTPUSERNAME`, `FTPPASSWORD` are GitHub Actions repo secrets — never passed via `gh workflow run`, never inspectable. "Did it actually publish" is inferred from **workflow run success** (the `dotnet nuget push`/FTP steps fail the job on error) — there's no separate nuget.org query scripted here; check nuget.org's listing page by hand if you want to confirm directly.

## Release notes

Drafting the notes is its own skill — see [[release-notes]] (fan-out over commits since the last tag, hybrid curated + full changelog format). This skill's step 10 just invokes it and takes the resulting markdown file as `gh release create`'s `--notes-file`.

Tags follow `Release_<Month>_<Day>_<Year>` (e.g. `Release_September_23_2025`); some older tags drop the `Release_` prefix. This naming is unrelated to the NuGet version string above — don't conflate the two.

## GitBook doc

[docs.flatredball.com/flatredball/contributing/builds](https://docs.flatredball.com/flatredball/contributing/builds) is the narrative release doc (source at `contributing/builds/README.md` and `contributing/builds/gum.md` in the separate `FlatRedBallDocs` GitBook repo, not this one) — it's the place to update the step-by-step checklist itself; this skill should stay a pointer + landmine list, not a second copy of it.
