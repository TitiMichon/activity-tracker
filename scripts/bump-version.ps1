<#
.SYNOPSIS
    Bumps the semantic version and turns the "Unreleased" changelog section into a release.

.EXAMPLE
    ./scripts/bump-version.ps1 -Part minor            # 1.0.0 -> 1.1.0
    ./scripts/bump-version.ps1 -Part patch -Commit    # also commits and creates tag v1.0.1
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateSet("major", "minor", "patch")] [string] $Part,
    # Commit the change and create the git tag (push it yourself to trigger the release).
    [switch] $Commit
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$propsPath = Join-Path $root "Directory.Build.props"
$changelogPath = Join-Path $root "CHANGELOG.md"

$props = Get-Content $propsPath -Raw
if ($props -notmatch '<VersionPrefix>(\d+)\.(\d+)\.(\d+)</VersionPrefix>') { throw "VersionPrefix not found in $propsPath" }
$major, $minor, $patch = [int]$Matches[1], [int]$Matches[2], [int]$Matches[3]
$old = "$major.$minor.$patch"
switch ($Part) {
    "major" { $major++; $minor = 0; $patch = 0 }
    "minor" { $minor++; $patch = 0 }
    "patch" { $patch++ }
}
$new = "$major.$minor.$patch"

$changelog = Get-Content $changelogPath -Raw
if ($changelog -notmatch '(?ms)^## \[Unreleased\]\s*\r?\n(.*?)(?=^## \[|\z)') { throw "CHANGELOG.md needs a '## [Unreleased]' section." }
if (-not $Matches[1].Trim()) { throw "The [Unreleased] section of CHANGELOG.md is empty: describe the changes before releasing." }

$date = Get-Date -Format "yyyy-MM-dd"
$changelog = $changelog -replace '(?m)^## \[Unreleased\]', "## [Unreleased]`n`n## [$new] - $date"
$utf8 = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText($changelogPath, $changelog, $utf8)
[IO.File]::WriteAllText($propsPath, ($props -replace "<VersionPrefix>$([regex]::Escape($old))</VersionPrefix>", "<VersionPrefix>$new</VersionPrefix>"), $utf8)
Write-Host "Version bumped: $old -> $new" -ForegroundColor Green

if ($Commit) {
    git -C $root add $propsPath $changelogPath
    git -C $root commit -m "chore: release v$new"
    if ($LASTEXITCODE -ne 0) { throw "git commit failed" }
    git -C $root tag -a "v$new" -m "Release v$new"
    if ($LASTEXITCODE -ne 0) { throw "git tag failed" }
    Write-Host "Created tag v$new. Run: git push origin main --follow-tags" -ForegroundColor Green
}
else {
    Write-Host "Next: ./scripts/validate.ps1, commit, then: git tag v$new && git push origin main --follow-tags"
}
