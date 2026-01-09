# Build Installer for PostmanClone
# This script builds the Windows installer using WiX Toolset

param(
    [string]$Configuration = "Release",
    [string]$Platform = "win-x64",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "PostmanClone Installer Builder" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$RootDir = Split-Path -Parent $PSScriptRoot
$SrcDir = Join-Path $RootDir "src"
$AppProject = Join-Path $SrcDir "PostmanClone.App\PostmanClone.App.csproj"
$InstallerDir = Join-Path $RootDir "installer"
$PublishDir = Join-Path $RootDir "publish\$Platform"
$OutputDir = Join-Path $InstallerDir "bin\$Configuration\$Platform"

Write-Host "Root Directory: $RootDir" -ForegroundColor Gray
Write-Host "Publish Directory: $PublishDir" -ForegroundColor Gray
Write-Host "Output Directory: $OutputDir" -ForegroundColor Gray
Write-Host ""

# Step 1: Clean previous build
Write-Host "[1/5] Cleaning previous build..." -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force
}
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}

# Step 2: Restore dependencies
Write-Host "[2/5] Restoring dependencies..." -ForegroundColor Yellow
Push-Location $RootDir
dotnet restore
Pop-Location

# Step 3: Publish the application
Write-Host "[3/5] Publishing application for $Platform..." -ForegroundColor Yellow
dotnet publish $AppProject `
    -c $Configuration `
    -r $Platform `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to publish application" -ForegroundColor Red
    exit 1
}

Write-Host "Application published successfully to: $PublishDir" -ForegroundColor Green
Write-Host ""

# Step 4: Generate WiX Files.wxs using Heat
Write-Host "[4/5] Generating WiX file manifest..." -ForegroundColor Yellow

# Check if WiX is installed
$HeatExe = Get-Command "heat.exe" -ErrorAction SilentlyContinue
if (-not $HeatExe) {
    Write-Host "Warning: WiX Toolset not found. Skipping installer creation." -ForegroundColor Yellow
    Write-Host "To build the installer, install WiX Toolset from: https://wixtoolset.org/releases/" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Published application is available at: $PublishDir" -ForegroundColor Green
    exit 0
}

$FilesWxs = Join-Path $InstallerDir "Files.wxs"
$HeatArgs = @(
    "dir", $PublishDir,
    "-cg", "ProductComponents",
    "-dr", "APPFOLDER",
    "-gg", "-sfrag", "-srd", "-sreg",
    "-var", "var.PublishDir",
    "-out", $FilesWxs
)

& heat.exe $HeatArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to generate file manifest" -ForegroundColor Red
    exit 1
}

Write-Host "File manifest generated successfully" -ForegroundColor Green
Write-Host ""

# Step 5: Build the installer
Write-Host "[5/5] Building Windows installer..." -ForegroundColor Yellow

# Set environment variable for WiX
$env:PublishDir = $PublishDir

# Build with MSBuild or WiX directly
$WixProj = Join-Path $InstallerDir "PostmanClone.Installer.wixproj"

if (Test-Path $WixProj) {
    msbuild $WixProj /p:Configuration=$Configuration /p:Platform=$Platform /p:DefineConstants="ProductVersion=$Version;PublishDir=$PublishDir"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to build installer" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Warning: Installer project not found at: $WixProj" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Green
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""
Write-Host "Published application: $PublishDir" -ForegroundColor Cyan
if (Test-Path $OutputDir) {
    Write-Host "Installer output: $OutputDir" -ForegroundColor Cyan
}
Write-Host ""
