<#
.SYNOPSIS
    Builds the downstream projects that gate an FRB release against the current working tree.

.DESCRIPTION
    Step 2 of the release checklist (docs.flatredball.com/flatredball/contributing/builds) is
    "run tests to verify FRB Editor functionality" against a set of real games. Those games
    reference FlatRedBall and Gum by ProjectReference into sibling checkouts, not by NuGet, so
    they compile directly against whatever is in the working tree right now -- no package
    publish required. That makes the compile half of step 2 scriptable, which is what this does.

    Compiling is not the whole of step 2: it catches broken APIs, not broken pixels. Actually
    running each game and looking at it is still a human job. But since pr-tests.yml covers only
    the Glue editor, compiling these is currently the only automated check that an engine change
    hasn't broken consumer code.

    Targets outside this repo are skipped (not failed) when absent, so a contributor without the
    game repos still gets a useful run over the in-repo targets.

.PARAMETER Only
    Build just the named targets. Accepts the Name column from the summary table.

.PARAMETER Configuration
    Build configuration. Defaults to Debug, which is what the release workflows publish.

.PARAMETER Clean
    Pass --no-incremental, forcing a full rebuild. Slower, but catches stale-artifact false
    passes -- worth it for an actual release gate.

.PARAMETER LogDirectory
    Where per-target build logs are written. Defaults to a timestamped folder under the repo's
    ignored artifacts directory.

.EXAMPLE
    ./scripts/Test-DownstreamBuilds.ps1
    Full release gate: every available target, incremental.

.EXAMPLE
    ./scripts/Test-DownstreamBuilds.ps1 -Only EngineUnitTests,TestProject -Clean
    Just the in-repo targets, from scratch.
#>
[CmdletBinding()]
param(
    [string[]]$Only,
    [string]$Configuration = 'Debug',
    [switch]$Clean,
    [string]$LogDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot   = Split-Path -Parent $PSScriptRoot
$githubRoot = Split-Path -Parent $repoRoot

# Paths are relative to $githubRoot so the sibling-checkout layout is stated in one place.
# Test=$true runs `dotnet test` instead of `dotnet build`.
#
# Targets are .sln where that works, but BattleCryptBombers is deliberately the game .csproj: its
# .sln also carries a NarfoxGameTools project that no longer exists in that repo, so the solution
# cannot restore at all. The game itself consumes NarfoxGameTools as a checked-in DLL and builds
# fine. We want to know whether the game still compiles against FRB, not whether an unrelated
# sibling repo has drifted.
#
# A target may pin its own Config when the default configuration wouldn't exercise it. The Forms
# projects only reference SkiaGum under DebugAutoBuild/ReleaseAutoBuild -- the configurations Glue
# uses to rebuild the engine during live edit -- so plain Debug silently skips that reference and
# a broken path there stays invisible.
#
# Samples/ is deliberately absent. *.Generated.cs is gitignored repo-wide with BeefballKni as the
# only exception, so every other sample needs Glue to regenerate before it will compile. They pass
# or fail based on whether stale generated files happen to be on the current disk, which is exactly
# the kind of machine-dependent green a release gate must not report.
#
# TestProjectDesktopNet6 has the same gitignored-codegen property (440 untracked *.Generated.cs),
# which is why it lives here and not in pr-tests.yml -- a clean CI checkout cannot build it. It is
# still worth running locally, since a developer's checkout does have the generated code and the
# release checklist names it explicitly.
$targets = @(
    @{ Name = 'EngineUnitTests'; Project = 'FlatRedBall/Tests/EngineUnitTests/EngineUnitTests.sln';                      Test = $true;  Note = 'Engine unit tests (no workflow runs these yet)' }
    @{ Name = 'TestProject';     Project = 'FlatRedBall/Tests/TestProjectDesktopNet6/TestProjectDesktopNet6.sln';        Test = $false; Note = 'The "Automated Test Project" from the checklist' }
    @{ Name = 'FormsAutoBuild';  Project = 'FlatRedBall/Engines/Forms/FlatRedBall.Forms/FlatRedBall.Forms.DesktopGLNet6.sln'; Test = $false; Config = 'DebugAutoBuild'; Note = 'The config Glue live-edit builds; only here does Forms reference SkiaGum' }
    @{ Name = 'KidDefense';      Project = 'KidDefense/GameProject/KidDefense.sln';                                      Test = $false; Note = 'net9.0 / MonoGame 3.8.5' }
    @{ Name = 'CrankyChibi';     Project = 'Kimuzukash-chibi-kuto-urufu/CrankyChibiCthulu.sln';                          Test = $false; Note = 'net8.0 / MonoGame 3.8.4.1' }
    @{ Name = 'BattleCrypt';     Project = 'BattleCryptBombers/BattleCryptBombers/BattleCryptBombers.csproj';            Test = $false; Note = 'net6.0 / MonoGame 3.8.1.303 -- oldest target, widest drift' }
)

if ($Only) {
    $unknown = $Only | Where-Object { $_ -notin $targets.Name }
    if ($unknown) {
        Write-Host "Unknown target(s): $($unknown -join ', ')" -ForegroundColor Red
        Write-Host "Available: $($targets.Name -join ', ')" -ForegroundColor Yellow
        exit 1
    }
    $targets = $targets | Where-Object { $_.Name -in $Only }
}

if (-not $LogDirectory) {
    $stamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
    $LogDirectory = Join-Path $repoRoot "artifacts/downstream-builds/$stamp"
}
New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null

# ---------------------------------------------------------------------------
# Pre-flight: report what we are actually building against.
#
# A green table means nothing if you can't say which Gum and which FRB produced it. Engine.yml
# and pr-tests.yml check out Gum's *default branch*, while glue.yml bundles Gum's *latest
# GitHub release* -- so "the Gum" is already ambiguous in CI. Local builds add a third
# possibility (whatever you happen to have checked out). Print it rather than assume it.
# ---------------------------------------------------------------------------

function Get-RepoState {
    param([string]$Path, [string]$Label)

    if (-not (Test-Path (Join-Path $Path '.git'))) { return $null }

    # Submodules are excluded: Gum carries Sokol.NET/FNA submodules that are routinely dirty or
    # uninitialised, and that says nothing about whether Gum's own source is clean.
    $dirty    = git -C $Path status --porcelain --ignore-submodules=all
    $describe = git -C $Path describe --tags --always 2>$null
    $branch   = git -C $Path rev-parse --abbrev-ref HEAD 2>$null

    [pscustomobject]@{
        Label    = $Label
        Branch   = $branch
        Describe = $describe
        Dirty    = [bool]$dirty
    }
}

Write-Host ''
Write-Host 'Building against' -ForegroundColor Cyan
Write-Host '----------------' -ForegroundColor Cyan

$gumPath = Join-Path $githubRoot 'Gum'
foreach ($state in @((Get-RepoState -Path $repoRoot -Label 'FlatRedBall'), (Get-RepoState -Path $gumPath -Label 'Gum'))) {
    if (-not $state) { continue }
    $flag = if ($state.Dirty) { ' (uncommitted changes)' } else { '' }
    Write-Host ("  {0,-12} {1} @ {2}{3}" -f $state.Label, $state.Branch, $state.Describe, $flag)
}

# Gum is FRB's one hard external dependency, and shipping FRB against an unreleased Gum is a
# silent failure mode: it compiles locally and then bundles a different Gum at release time.
if (Test-Path $gumPath) {
    $latestGumTag = git -C $gumPath tag --list 'Release_*' --sort=-creatordate | Select-Object -First 1
    if ($latestGumTag) {
        git -C $gumPath merge-base --is-ancestor $latestGumTag HEAD 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host ''
            Write-Host "  WARNING: Gum HEAD does not contain its latest release tag ($latestGumTag)." -ForegroundColor Yellow
            Write-Host "           These builds test FRB against unreleased Gum source, but glue.yml" -ForegroundColor Yellow
            Write-Host "           bundles Gum's latest GitHub release. A pass here does not prove the" -ForegroundColor Yellow
            Write-Host "           shipped combination works." -ForegroundColor Yellow
        }
    }
}
Write-Host ''

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------

$results = @()

foreach ($target in $targets) {
    $projectPath = Join-Path $githubRoot $target.Project

    if (-not (Test-Path $projectPath)) {
        Write-Host ("SKIP  {0} -- not found at {1}" -f $target.Name, $target.Project) -ForegroundColor DarkGray
        $results += [pscustomobject]@{
            Name = $target.Name; Status = 'SKIPPED'; Duration = $null; Errors = 0; Warnings = 0; Log = $null
        }
        continue
    }

    $verb = if ($target.Test) { 'Testing' } else { 'Building' }
    Write-Host ("{0} {1} ({2})..." -f $verb, $target.Name, $target.Note) -ForegroundColor Cyan

    $log = Join-Path $LogDirectory "$($target.Name).log"

    # WarningLevel=0 keeps third-party game code from burying real errors in noise. Errors are
    # unaffected.
    $targetConfig = if ($target.ContainsKey('Config')) { $target.Config } else { $Configuration }
    $common = @('-c', $targetConfig, '/p:WarningLevel=0')

    # Each entry is one `dotnet` invocation; they run in order and the first failure stops the
    # target. Only -Clean on a test target needs more than one, because `dotnet test` rejects
    # --no-incremental (it forwards to MSBuild's VSTest target, which doesn't accept the switch).
    # So force the rebuild in its own build pass, then test without rebuilding.
    $invocations = @()
    if ($target.Test -and $Clean) {
        $invocations += ,(@('build', $projectPath) + $common + '--no-incremental')
        $invocations += ,(@('test',  $projectPath) + $common + '--no-build')
    }
    elseif ($target.Test) {
        $invocations += ,(@('test', $projectPath) + $common)
    }
    else {
        $buildArgs = @('build', $projectPath) + $common
        if ($Clean) { $buildArgs += '--no-incremental' }
        $invocations += ,$buildArgs
    }

    Remove-Item $log -ErrorAction SilentlyContinue

    $started  = Get-Date
    $exitCode = 0
    foreach ($invocation in $invocations) {
        & dotnet @invocation *>&1 | Tee-Object -FilePath $log -Append | Out-Null
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) { break }
    }
    $duration = (Get-Date) - $started

    $logText = Get-Content $log -Raw -ErrorAction SilentlyContinue
    if (-not $logText) { $logText = '' }

    # Count distinct diagnostic codes, not raw lines: MSBuild repeats the same error once per
    # project that consumes the failing one, which otherwise inflates a single break into dozens.
    $errorCount = @([regex]::Matches($logText, '(?m)\berror\s+([A-Z]{2,}\d+)') |
        ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique).Count
    $warnCount  = @([regex]::Matches($logText, '(?m)\bwarning\s+([A-Z]{2,}\d+)') |
        ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique).Count

    $status = if ($exitCode -eq 0) { 'PASS' } else { 'FAIL' }
    $color  = if ($exitCode -eq 0) { 'Green' } else { 'Red' }
    Write-Host ("  {0} in {1:mm\:ss}" -f $status, $duration) -ForegroundColor $color

    $results += [pscustomobject]@{
        Name     = $target.Name
        Status   = $status
        Duration = $duration
        Errors   = $errorCount
        Warnings = $warnCount
        Log      = $log
    }
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Host ''
Write-Host 'Downstream build summary' -ForegroundColor Cyan
Write-Host '------------------------' -ForegroundColor Cyan
Write-Host ('{0,-18} {1,-8} {2,8} {3,8} {4,9}' -f 'Target', 'Status', 'Errors', 'Warnings', 'Time')

foreach ($result in $results) {
    $color = switch ($result.Status) {
        'PASS'    { 'Green' }
        'FAIL'    { 'Red' }
        default   { 'DarkGray' }
    }
    $time = if ($result.Duration) { '{0:mm\:ss}' -f $result.Duration } else { '-' }
    Write-Host ('{0,-18} {1,-8} {2,8} {3,8} {4,9}' -f $result.Name, $result.Status, $result.Errors, $result.Warnings, $time) -ForegroundColor $color
}

$failed  = @($results | Where-Object Status -eq 'FAIL')
$skipped = @($results | Where-Object Status -eq 'SKIPPED')

Write-Host ''
Write-Host "Logs: $LogDirectory" -ForegroundColor DarkGray

if ($skipped) {
    Write-Host "Skipped (repo not present): $($skipped.Name -join ', ')" -ForegroundColor Yellow
}

if ($failed) {
    Write-Host ''
    Write-Host "$($failed.Count) target(s) failed: $($failed.Name -join ', ')" -ForegroundColor Red
    Write-Host 'Release checklist step 2 is NOT satisfied.' -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host 'All available targets compiled.' -ForegroundColor Green
Write-Host 'Remaining manual work for step 2: actually run each game and confirm it plays correctly.' -ForegroundColor Yellow
exit 0
