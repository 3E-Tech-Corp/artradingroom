# ARTrading Frontend Deployment Script
# This script deploys the built React frontend to the aitrade.synthia.bot website root

param(
    [string]$SourcePath = "",
    [string]$DestinationPath = "F:\New_WWW\aitrade.synthia.bot\WWW",
    [switch]$RestartIIS
)

# If no source path provided, try to find the dist directory
if (-not $SourcePath) {
    $ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $SourcePath = Join-Path $ScriptRoot "..\Frontend\dist"
    if (-not (Test-Path $SourcePath)) {
        Write-Error "Could not auto-detect frontend dist directory. Please provide -SourcePath parameter."
        exit 1
    }
    Write-Host "Using detected dist path: $SourcePath"
}

# Verify source directory
if (-not (Test-Path $SourcePath)) {
    Write-Error "Source directory not found: $SourcePath"
    exit 1
}

# Verify destination directory
if (-not (Test-Path $DestinationPath)) {
    Write-Error "Destination directory not found: $DestinationPath"
    exit 1
}

Write-Host "Deploying ARTrading Frontend..."
Write-Host "Source: $SourcePath"
Write-Host "Destination: $DestinationPath"
Write-Host ""

# Backup existing files (optional)
$BackupPath = "$DestinationPath.backup-$(Get-Date -Format 'yyyy-MM-dd-HHmmss')"
Write-Host "Creating backup at: $BackupPath"
Copy-Item -Path $DestinationPath -Destination $BackupPath -Recurse -Force

Write-Host "Clearing existing files..."
Get-ChildItem -Path $DestinationPath -Recurse | Remove-Item -Force -Recurse

Write-Host "Copying new frontend files..."
Copy-Item -Path "$SourcePath\*" -Destination $DestinationPath -Recurse -Force

Write-Host "Verifying deployment..."
$fileCount = (Get-ChildItem -Path $DestinationPath -Recurse -File).Count
Write-Host "Deployed $fileCount files"

# Restart IIS if requested
if ($RestartIIS) {
    Write-Host "Restarting IIS..."
    & iisreset
    Write-Host "IIS restarted"
}

Write-Host ""
Write-Host "Frontend deployment complete!"
Write-Host "Frontend is now available at: https://aitrade.synthia.bot"
