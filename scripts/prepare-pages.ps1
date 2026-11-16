<#
.SYNOPSIS
    Adapts a published wwwroot folder for GitHub Pages (project site served under /<repo>/).

.DESCRIPTION
    - sets <base href> to the sub-path,
    - updates the index.html hash in the PWA service-worker manifest (otherwise offline caching fails its integrity check),
    - adds 404.html (SPA deep-link fallback) and .nojekyll (serve _framework as-is),
    - removes precompressed index.html variants that would be stale.

.EXAMPLE
    ./scripts/prepare-pages.ps1 -Path artifacts/publish/wwwroot -BasePath /activity-tracker/
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Path,
    [Parameter(Mandatory)] [string] $BasePath
)

$ErrorActionPreference = "Stop"
if (-not $BasePath.StartsWith("/")) { $BasePath = "/$BasePath" }
if (-not $BasePath.EndsWith("/")) { $BasePath = "$BasePath/" }

$index = Join-Path $Path "index.html"
$html = [IO.File]::ReadAllText($index)
if ($html -notmatch '<base href="/" />') { throw "index.html does not contain '<base href=""/"" />'" }
$html = $html.Replace('<base href="/" />', "<base href=""$BasePath"" />")
$utf8 = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText($index, $html, $utf8)

$sha = [System.Security.Cryptography.SHA256]::Create()
$hash = "sha256-" + [Convert]::ToBase64String($sha.ComputeHash([IO.File]::ReadAllBytes($index)))
$swManifest = Join-Path $Path "service-worker-assets.js"
if (Test-Path $swManifest) {
    $sw = [IO.File]::ReadAllText($swManifest)
    $pattern = '("hash":\s*")[^"]+(",\s*"url":\s*"index\.html")'
    if ($sw -notmatch $pattern) { throw "index.html entry not found in service-worker-assets.js" }
    $sw = [regex]::Replace($sw, $pattern, "`${1}$hash`${2}")
    [IO.File]::WriteAllText($swManifest, $sw, $utf8)
}

Remove-Item (Join-Path $Path "index.html.br"), (Join-Path $Path "index.html.gz") -ErrorAction SilentlyContinue
Copy-Item $index (Join-Path $Path "404.html") -Force
New-Item -ItemType File -Force (Join-Path $Path ".nojekyll") | Out-Null
Write-Host "Prepared $Path for GitHub Pages under $BasePath"
