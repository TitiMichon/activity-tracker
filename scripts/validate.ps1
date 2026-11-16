<#
.SYNOPSIS
    Validates a version of Activity Tracker: version metadata, build, unit tests,
    component tests, publish and end-to-end browser tests.

.DESCRIPTION
    Same script runs locally and in CI, so "it passed on my machine" means the same thing
    as "it passed in the pipeline". Exits with a non-zero code on the first failure.

.EXAMPLE
    ./scripts/validate.ps1                       # full validation
    ./scripts/validate.ps1 -SkipE2E              # faster, no browser
    ./scripts/validate.ps1 -ExpectedVersion 1.2.0   # also check the version (used by the release pipeline)
#>
[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [switch] $SkipE2E,
    # Fails if Directory.Build.props does not declare exactly this version.
    [string] $ExpectedVersion,
    # Pre-release label appended to the version, e.g. "ci.42" -> 1.0.0-ci.42.
    [string] $VersionSuffix,
    # Installs the Linux system libraries Chromium needs (CI runners).
    [switch] $InstallBrowserDeps
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root "artifacts"
$results = Join-Path $artifacts "test-results"
$publishDir = Join-Path $artifacts "publish"
$started = Get-Date

function Step([string] $name) { Write-Host "`n==> $name" -ForegroundColor Cyan }

# Simple function (no param block) so "--flags" are passed through untouched in $args.
function Invoke-Native {
    $exe = $args[0]
    $rest = @($args | Select-Object -Skip 1)
    & $exe @rest
    if ($LASTEXITCODE -ne 0) {
        throw "'$exe $($rest -join ' ')' failed with exit code $LASTEXITCODE"
    }
}

Push-Location $root
try {
    # ---------------------------------------------------------------- version
    Step "Checking version metadata"
    [xml] $props = Get-Content (Join-Path $root "Directory.Build.props")
    $version = ($props.Project.PropertyGroup | Where-Object { $_.VersionPrefix } | Select-Object -First 1).VersionPrefix
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        throw "VersionPrefix '$version' in Directory.Build.props is not MAJOR.MINOR.PATCH."
    }

    if ($ExpectedVersion -and $ExpectedVersion.TrimStart('v') -ne $version) {
        throw "Expected version $ExpectedVersion but Directory.Build.props says $version. Run scripts/bump-version.ps1 first."
    }

    $changelog = Get-Content (Join-Path $root "CHANGELOG.md") -Raw
    if ($changelog -notmatch "(?m)^## \[$([regex]::Escape($version))\]") {
        throw "CHANGELOG.md has no '## [$version]' section. Document the changes of this version."
    }

    $fullVersion = if ($VersionSuffix) { "$version-$VersionSuffix" } else { $version }
    Write-Host "Version $fullVersion OK"

    # ---------------------------------------------------------------- build
    Step "Restoring and building ($Configuration)"
    $versionArgs = @()
    if ($VersionSuffix) { $versionArgs += "-p:VersionSuffix=$VersionSuffix" }
    Invoke-Native dotnet restore
    Invoke-Native dotnet build --configuration $Configuration --no-restore @versionArgs

    if (Test-Path $results) { Remove-Item $results -Recurse -Force }

    # ---------------------------------------------------------------- tests
    foreach ($project in @("ActivityTracker.Core.Tests", "ActivityTracker.Web.Tests")) {
        Step "Running $project"
        Invoke-Native dotnet test "tests/$project" --configuration $Configuration --no-build `
            --logger "trx;LogFileName=$project.trx" --results-directory $results `
            --collect "XPlat Code Coverage"
    }

    # ---------------------------------------------------------------- publish
    Step "Publishing the web app"
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    Invoke-Native dotnet publish src/ActivityTracker.Web --configuration $Configuration --output $publishDir @versionArgs
    $wwwroot = Join-Path $publishDir "wwwroot"
    if (-not (Test-Path (Join-Path $wwwroot "index.html"))) { throw "Publish output has no index.html" }

    # ---------------------------------------------------------------- e2e
    if ($SkipE2E) {
        Write-Host "`nSkipping end-to-end tests (-SkipE2E)." -ForegroundColor Yellow
    }
    else {
        Step "Running end-to-end browser tests"
        if ($InstallBrowserDeps) {
            $playwrightScript = Join-Path $root "tests/ActivityTracker.E2E/bin/$Configuration/net10.0/playwright.ps1"
            Invoke-Native pwsh $playwrightScript install --with-deps chromium
        }

        $env:E2E_APP_DIR = $wwwroot
        try {
            Invoke-Native dotnet test tests/ActivityTracker.E2E --configuration $Configuration --no-build `
                --logger "trx;LogFileName=ActivityTracker.E2E.trx" --results-directory $results
        }
        finally {
            Remove-Item Env:E2E_APP_DIR -ErrorAction SilentlyContinue
        }
    }

    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    Write-Host "`nVersion $fullVersion validated in ${elapsed}s. Published app: $wwwroot" -ForegroundColor Green
}
catch {
    Write-Host "`nVALIDATION FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
