<#
.SYNOPSIS
    Builds the Android app as an installable APK: artifacts/android/activity-tracker-v<version>.apk

.DESCRIPTION
    Same script locally, in CI and in the release pipeline.

    Signing: when ANDROID_KEYSTORE_BASE64 (CI secret) or ANDROID_KEYSTORE_PATH (local file) is set,
    together with ANDROID_KEYSTORE_PASSWORD, the APK is signed with the release key. Every published
    version must use that same key, otherwise Android refuses to update the installed app.
    Without them the APK is signed with the local debug key (fine for testing, not for releases).

.EXAMPLE
    ./scripts/build-android.ps1                                   # debug-signed APK
    ./scripts/build-android.ps1 -RequireReleaseKey                 # fails if the release key is missing
    ./scripts/build-android.ps1 -InstallDependencies -VersionSuffix ci.42   # CI
#>
[CmdletBinding()]
param(
    [string] $VersionSuffix,
    # Installs the maui-android workload and the Android SDK packages the project needs (CI runners).
    [switch] $InstallDependencies,
    # Fails instead of falling back to the debug key (release pipeline).
    [switch] $RequireReleaseKey
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src/ActivityTracker.Mobile/ActivityTracker.Mobile.csproj"
$output = Join-Path $root "artifacts/android"
$framework = "net10.0-android"

function Step([string] $name) { Write-Host "`n==> $name" -ForegroundColor Cyan }

function Invoke-Native {
    $exe = $args[0]
    $rest = @($args | Select-Object -Skip 1)
    & $exe @rest
    if ($LASTEXITCODE -ne 0) { throw "$exe $($rest -join ' ') failed with exit code $LASTEXITCODE" }
}

$version = ([xml](Get-Content (Join-Path $root "Directory.Build.props"))).Project.PropertyGroup.VersionPrefix
if ($VersionSuffix) { $version = "$version-$VersionSuffix" }

$sdkArgs = @()
if ($env:ANDROID_HOME) { $sdkArgs += "-p:AndroidSdkDirectory=$env:ANDROID_HOME" }

if ($InstallDependencies) {
    Step "Installing the maui-android workload and Android SDK packages"
    Invoke-Native dotnet workload install maui-android
    Invoke-Native dotnet build $project -t:InstallAndroidDependencies -f $framework -p:AcceptAndroidSDKLicenses=True @sdkArgs
}

$signArgs = @()
$tempKeystore = $null
$keystore = $env:ANDROID_KEYSTORE_PATH
if ($env:ANDROID_KEYSTORE_BASE64) {
    $tempKeystore = Join-Path ([IO.Path]::GetTempPath()) "activity-tracker-release.jks"
    [IO.File]::WriteAllBytes($tempKeystore, [Convert]::FromBase64String($env:ANDROID_KEYSTORE_BASE64))
    $keystore = $tempKeystore
}

if ($keystore) {
    if (-not $env:ANDROID_KEYSTORE_PASSWORD) { throw "ANDROID_KEYSTORE_PASSWORD is required with the release keystore." }
    $alias = if ($env:ANDROID_KEY_ALIAS) { $env:ANDROID_KEY_ALIAS } else { "activity-tracker" }
    # "env:" makes MSBuild read the password from the environment, so it never appears in logs.
    $signArgs = @(
        "-p:AndroidKeyStore=true",
        "-p:AndroidSigningKeyStore=$keystore",
        "-p:AndroidSigningKeyAlias=$alias",
        "-p:AndroidSigningStorePass=env:ANDROID_KEYSTORE_PASSWORD",
        "-p:AndroidSigningKeyPass=env:ANDROID_KEYSTORE_PASSWORD"
    )
    $signedWith = "release key ($alias)"
}
elseif ($RequireReleaseKey) {
    throw "Release key missing: set ANDROID_KEYSTORE_BASE64 (or ANDROID_KEYSTORE_PATH) and ANDROID_KEYSTORE_PASSWORD."
}
else {
    $signedWith = "debug key (testing only)"
}

try {
    Step "Publishing the Android app $version (signed with the $signedWith)"
    if (Test-Path $output) { Remove-Item $output -Recurse -Force }
    $versionArgs = if ($VersionSuffix) { @("-p:VersionSuffix=$VersionSuffix") } else { @() }
    Invoke-Native dotnet publish $project -c Release -f $framework -o $output @versionArgs @sdkArgs @signArgs
}
finally {
    if ($tempKeystore -and (Test-Path $tempKeystore)) { Remove-Item $tempKeystore -Force }
}

$signed = Get-ChildItem $output -Filter "*-Signed.apk" | Select-Object -First 1
if (-not $signed) { throw "No signed APK found in $output." }
$apk = Join-Path $output "activity-tracker-v$version.apk"
Move-Item $signed.FullName $apk -Force
Get-ChildItem $output -Exclude (Split-Path $apk -Leaf) | Remove-Item -Recurse -Force

Write-Host "`nAPK: $apk ($([math]::Round((Get-Item $apk).Length / 1MB, 1)) MB, $signedWith)" -ForegroundColor Green
if ($env:GITHUB_OUTPUT) { "apk=$apk" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8 }
