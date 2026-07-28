#Requires -Version 5.1
<#
Builds Glue and uploads it as a rolling "glue-daily" GitHub release,
so artists can download a working build without cloning/building anything.
#>
param(
    # Release is broken: several plugin post-build steps hardcode bin\Debug\Plugins\... regardless of
    # $(Configuration) (why CI's Release matrix entry is disabled). Debug is what actually builds/tests.
    [string]$Configuration = "Debug",
    [switch]$SkipPull
)

$ErrorActionPreference = "Stop"

$glueDir = Split-Path -Parent $MyInvocation.MyCommand.Path      # FRBDK\Glue
$frbdkDir = Split-Path -Parent $glueDir                        # FRBDK
$repoRoot = Split-Path -Parent $frbdkDir                        # FlatRedBall
$gumRoot = Join-Path (Split-Path -Parent $repoRoot) "Gum"       # sibling checkout

function Assert-Clean($repoPath, $name) {
    Push-Location $repoPath
    try {
        $dirty = git status --porcelain --untracked-files=no
        if ($dirty) {
            throw "$name has uncommitted changes ($repoPath). Commit/stash before running this script."
        }
    } finally {
        Pop-Location
    }
}

if (-not (Test-Path $gumRoot)) {
    throw "Expected a sibling Gum checkout at $gumRoot but it doesn't exist."
}

if (-not $SkipPull) {
    Assert-Clean $repoRoot "FlatRedBall"
    Assert-Clean $gumRoot "Gum"

    Write-Host "Pulling latest FlatRedBall..."
    Push-Location $repoRoot
    try { git pull --ff-only } finally { Pop-Location }

    Write-Host "Pulling latest Gum..."
    Push-Location $gumRoot
    try { git pull --ff-only } finally { Pop-Location }
}

$version = Get-Date -Format "yyyy.M.d"
$glueCsproj = Join-Path $glueDir "Glue\Glue.csproj"
$csprojText = Get-Content $glueCsproj -Raw
$csprojText = $csprojText -replace "<Version>[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?</Version>", "<Version>$version</Version>"
Set-Content $glueCsproj $csprojText -NoNewline

try {
    $sln = Join-Path $glueDir "Glue with All.sln"
    Write-Host "Building $sln ($Configuration), version $version..."
    dotnet build $sln -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
} finally {
    # don't leave the version bump as an uncommitted change
    git -C $repoRoot checkout -- "FRBDK/Glue/Glue/Glue.csproj"
}

$binDir = Join-Path $glueDir "Glue\bin\$Configuration"
if (-not (Test-Path (Join-Path $binDir "GlueFormsCore.exe"))) {
    throw "Expected build output at $binDir but GlueFormsCore.exe is missing."
}

$zipPath = Join-Path $glueDir "Glue\bin\GlueDailyBuild.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Write-Host "Zipping $binDir..."
Compress-Archive -Path (Join-Path $binDir "*") -DestinationPath $zipPath

$commit = git -C $repoRoot rev-parse --short HEAD
$date = Get-Date -Format "yyyy-MM-dd HH:mm"
$notes = "Automated daily build. Not for production - grab whatever's newest.`n`nFlatRedBall commit: $commit`nBuilt: $date"

Push-Location $repoRoot
try {
    $existing = gh release view glue-daily 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Updating existing glue-daily release..."
        gh release upload glue-daily $zipPath --clobber
        gh release edit glue-daily --title "Glue Daily Build ($date)" --notes $notes
    } else {
        Write-Host "Creating glue-daily release..."
        gh release create glue-daily $zipPath --title "Glue Daily Build ($date)" --notes $notes --prerelease
    }
} finally {
    Pop-Location
}

Write-Host "Done. Download: https://github.com/vchelaru/FlatRedBall/releases/download/glue-daily/GlueDailyBuild.zip"
