#Example pack only (no push, override version):
#.\tools\publish-nuget.ps1 -PackageVersion 4.3.0

param(
    [string]$Configuration = "ReleaseMulti",
    [string]$PackageVersion = "",
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

function Write-Section {
    param([string]$Message)
    Write-Host "`n=== $Message ==="
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Section "Restore"
dotnet restore

Write-Section "Build"
dotnet build BigFloat.sln --configuration $Configuration --no-restore

if (-not $SkipTests) {
    Write-Section "Test"
    dotnet test BigFloat.sln --configuration $Configuration --no-build --verbosity normal
}

Write-Section "Pack"
$packArgs = @(
    "pack",
    "BigFloatLibrary/BigFloatLibrary.csproj",
    "--configuration", $Configuration,
    "--no-build",
    "--output", "published-packages"
)

if (-not [string]::IsNullOrWhiteSpace($PackageVersion)) {
    $packArgs += "/p:PackageVersion=$PackageVersion"
}

dotnet @packArgs


Write-Section "Package pushed to \published-packages\ for upload to https://www.nuget.org/packages/manage/upload."
