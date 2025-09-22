# PowerShell script to run Cake build
param(
    [string]$Target = "Default",
    [string]$Configuration = "Release",
    [string]$Verbosity = "Verbose",
    [switch]$Experimental,
    [switch]$WhatIf
)

# Make sure Cake is installed
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI is not installed. Please install .NET SDK."
    exit 1
}

# Install Cake if not already installed
$cakeInstalled = dotnet tool list --global | Select-String "Cake.Tool"
if (-not $cakeInstalled) {
    Write-Host "Installing Cake.Tool..." -ForegroundColor Green
    dotnet tool install --global Cake.Tool --version 3.0.0
}

# Build arguments
$cakeArgs = @(
    "cake"
    "build.cake"
    "--target=$Target"
    "--configuration=$Configuration"
    "--verbosity=$Verbosity"
)

if ($Experimental) {
    $cakeArgs += "--experimental"
}

if ($WhatIf) {
    $cakeArgs += "--dryrun"
}

# Run Cake
Write-Host "Running Cake with arguments: $($cakeArgs -join ' ')" -ForegroundColor Green
& dotnet $cakeArgs
