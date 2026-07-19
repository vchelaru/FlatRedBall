<#
.SYNOPSIS
Automates the "open FRB Editor and confirm these sample projects still load/build" release
smoke test: launches Glue against each project's .gluj with "exitwhenquiet=N" (auto-loads +
auto-generates code per FRBDK\Glue\Glue\IO\ProjectLoader.cs, then Glue closes itself once
TaskManager has been idle for N seconds - see CommandLineManager.ExitWhenQuietSeconds and
MainGlueWindow.StartExitWhenQuietWatcherIfRequested), then runs `dotnet build` on the result.

Does NOT cover creating a brand new project via the New Project wizard - that's a separate,
harder automation task (modal WPF dialog, no headless entry point).

.PARAMETER GlueExe
Path to the built Glue executable. Note the real assembly/output name is GlueFormsCore.exe,
not Glue.exe (see FRBDK\Glue\Glue\Glue.csproj's <AssemblyName>). Build via:
dotnet build -c Debug "FRBDK\Glue\Glue with All.sln"

.PARAMETER ProjectNames
Optional filter - only run projects whose Name matches one of these (useful for debugging
a single project instead of the full sweep).

.PARAMETER QuietSeconds
Passed to Glue as "exitwhenquiet=<QuietSeconds>" - how long TaskManager must be idle before
Glue closes itself.

.PARAMETER TimeoutSeconds
Safety net only. If Glue doesn't self-exit within this long (e.g. the flag didn't take
effect, or it's genuinely stuck), the process is force-killed and the project is reported
as TIMEOUT rather than hanging the whole sweep forever.
#>
param(
    [string]$GlueExe = (Join-Path $PSScriptRoot "..\Glue\Glue\bin\Debug\GlueFormsCore.exe"),
    [string[]]$ProjectNames,
    [int]$QuietSeconds = 5,
    [int]$TimeoutSeconds = 240
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$siblingRoot = Resolve-Path (Join-Path $repoRoot "..")

$allProjects = @(
    [pscustomobject]@{
        Name        = "Automated Test Project (TestProjectDesktopNet6)"
        GlueProject = Join-Path $repoRoot "Tests\TestProjectDesktopNet6\TestProjectDesktopNet6\TestProjectDesktopNet6.gluj"
        BuildTarget = Join-Path $repoRoot "Tests\TestProjectDesktopNet6\TestProjectDesktopNet6\TestProjectDesktopNet6.csproj"
    }
    [pscustomobject]@{
        Name        = "Kid Defense"
        GlueProject = Join-Path $siblingRoot "KidDefense\GameProject\KidDefense\KidDefense.gluj"
        BuildTarget = Join-Path $siblingRoot "KidDefense\GameProject\KidDefense\KidDefense.csproj"
    }
    [pscustomobject]@{
        Name        = "Cranky Chibi Cthulhu"
        GlueProject = Join-Path $siblingRoot "Kimuzukash-chibi-kuto-urufu\CrankyChibiCthulu\CrankyChibiCthulu.gluj"
        BuildTarget = Join-Path $siblingRoot "Kimuzukash-chibi-kuto-urufu\CrankyChibiCthulu\CrankyChibiCthulu.csproj"
    }
    [pscustomobject]@{
        Name        = "Battlecrypt Bombers"
        GlueProject = Join-Path $siblingRoot "BattleCryptBombers\BattleCryptBombers\BattleCryptBombers.gluj"
        BuildTarget = Join-Path $siblingRoot "BattleCryptBombers\BattleCryptBombers\BattleCryptBombers.csproj"
    }
)

$projects = if ($ProjectNames) {
    $allProjects | Where-Object { $ProjectNames -contains $_.Name }
} else {
    $allProjects
}

if (-not (Test-Path $GlueExe)) {
    Write-Error "Glue executable not found at $GlueExe. Build it first: dotnet build -c Debug `"FRBDK\Glue\Glue with All.sln`""
    exit 1
}

# Scoped to this project's own directory so that if Glue's "reopen the previously-opened
# project" fallback ever silently kicks in (e.g. a malformed command-line path), we still
# notice - Glue would idle out and close itself against the WRONG project, and without this
# check that would misreport as a verified pass. A run against the wrong project never
# touches this directory.
function Get-NewestActivityTime($projectDir) {
    $newest = Get-ChildItem -Path $projectDir -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|\.vs)\\' } |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($newest) { $newest.LastWriteTimeUtc } else { [datetime]::MinValue }
}

$results = @()

foreach ($project in $projects) {
    Write-Host "`n=== $($project.Name) ===" -ForegroundColor Cyan

    if (-not (Test-Path $project.GlueProject)) {
        Write-Host "SKIP - Glue project not found: $($project.GlueProject)" -ForegroundColor Yellow
        $results += [pscustomobject]@{ Name = $project.Name; Status = "SKIPPED"; Detail = "Glue project file not found" }
        continue
    }
    if (-not (Test-Path $project.BuildTarget)) {
        Write-Host "SKIP - Build target not found: $($project.BuildTarget)" -ForegroundColor Yellow
        $results += [pscustomobject]@{ Name = $project.Name; Status = "SKIPPED"; Detail = "Build target not found" }
        continue
    }

    $projectDir = Split-Path $project.BuildTarget -Parent
    $baselineTime = Get-NewestActivityTime $projectDir

    Write-Host "Launching Glue against $($project.GlueProject) (exitwhenquiet=$QuietSeconds)..."
    $proc = Start-Process -FilePath $GlueExe `
        -ArgumentList "`"$($project.GlueProject)`"", "exitwhenquiet=$QuietSeconds" `
        -PassThru -WindowStyle Minimized

    $selfExited = $proc.WaitForExit($TimeoutSeconds * 1000)

    if (-not $selfExited) {
        Write-Host "WARNING - Glue did not exit on its own within $TimeoutSeconds s, force-killing" -ForegroundColor Yellow
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }

    $activityConfirmed = (Get-NewestActivityTime $projectDir) -gt $baselineTime
    if (-not $activityConfirmed) {
        Write-Host "WARNING - no file activity detected in $($project.Name)'s own directory. This usually means Glue never actually loaded THIS project (e.g. it fell back to reopening the previously-opened project instead) - the build below may just be re-checking stale/unrelated code, not a real verification." -ForegroundColor Yellow
    }

    Write-Host "Building $($project.BuildTarget)..."
    $buildOutput = & dotnet build $project.BuildTarget -c Debug 2>&1
    $buildExitCode = $LASTEXITCODE

    if (-not $selfExited) {
        $results += [pscustomobject]@{ Name = $project.Name; Status = "TIMEOUT"; Detail = "Glue never exited on its own; force-killed after $TimeoutSeconds s" }
    } elseif ($buildExitCode -ne 0) {
        Write-Host "FAIL - build exit code $buildExitCode" -ForegroundColor Red
        $tail = ($buildOutput | Select-Object -Last 40) -join "`n"
        Write-Host $tail
        $results += [pscustomobject]@{ Name = $project.Name; Status = "FAIL"; Detail = $tail }
    } elseif (-not $activityConfirmed) {
        Write-Host "UNVERIFIED - build passed but Glue load was never confirmed" -ForegroundColor Yellow
        $results += [pscustomobject]@{ Name = $project.Name; Status = "UNVERIFIED"; Detail = "Build passed, but no file activity was detected in this project's own directory - Glue may not have actually loaded it." }
    } else {
        Write-Host "PASS" -ForegroundColor Green
        $results += [pscustomobject]@{ Name = $project.Name; Status = "PASS"; Detail = "" }
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
$results | Format-Table Name, Status -AutoSize

if ($results | Where-Object { $_.Status -in @("FAIL", "UNVERIFIED", "TIMEOUT") }) {
    exit 1
} else {
    exit 0
}
