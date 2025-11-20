# PowerShell script to increment version in .csproj file
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$VersionBump = "patch"
)

$ErrorActionPreference = "Stop"

$csprojPath = "Buser.AsyncCompression/Buser.AsyncCompression.csproj"

if (-not (Test-Path $csprojPath)) {
    Write-Error "Project file not found: $csprojPath"
    exit 1
}

# Read current version
[xml]$csproj = Get-Content $csprojPath
$versionNode = $csproj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1

if (-not $versionNode -or -not $versionNode.Version) {
    Write-Error "Version not found in project file"
    exit 1
}

$currentVersion = [Version]$versionNode.Version
Write-Host "Current version: $currentVersion"

# Increment version
$newVersion = switch ($VersionBump) {
    "major" { [Version]::new($currentVersion.Major + 1, 0, 0) }
    "minor" { [Version]::new($currentVersion.Major, $currentVersion.Minor + 1, 0) }
    "patch" { [Version]::new($currentVersion.Major, $currentVersion.Minor, $currentVersion.Build + 1) }
}

$versionString = $newVersion.ToString()
Write-Host "New version: $versionString"

# Update version in .csproj
$versionNode.Version = $versionString
$csproj.Save((Resolve-Path $csprojPath))

Write-Host "Version updated successfully to $versionString"
Write-Output $versionString

