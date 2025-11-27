# Build Script for Wallpaper Rotator V1.0

$ErrorActionPreference = "Stop"
$version = "1.0.0"
$publishDir = "bin\Release\net7.0-windows\win-x64\publish"
$outputDir = "ReleaseOutput"
$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "Cleaning previous builds..." -ForegroundColor Cyan
if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }
if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
New-Item -ItemType Directory -Path $outputDir | Out-Null

# --- 1. Build Standard Version (for Installer) ---
Write-Host "Building Standard Version (for Installer)..." -ForegroundColor Cyan
$standardDir = Join-Path $outputDir "Standard"
dotnet publish -c Release -r win-x64 --self-contained false -o $standardDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Standard build failed!"
    exit 1
}

# --- 2. Build Portable Single-File Version (Full - Self Contained) ---
Write-Host "Cleaning before Portable Build..." -ForegroundColor Cyan
dotnet clean -c Release
if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }

Write-Host "Building Portable Full Version (Self-Contained)..." -ForegroundColor Cyan
$portableDir = Join-Path $outputDir "Portable"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:DebugType=None -o $portableDir

if ($LASTEXITCODE -eq 0) {
    $exePath = Join-Path $portableDir "WallpaperRotator.exe"
    $destPath = Join-Path $outputDir "WallpaperRotator-Portable.exe"
    
    # Move EXE to output directory
    Move-Item -Path $exePath -Destination $destPath -Force
    Write-Host "Portable Version created at: $destPath" -ForegroundColor Green
    
    # Clean up temp portable folder
    Remove-Item $portableDir -Recurse -Force
} else {
    Write-Error "Portable Full build failed!"
}

# --- 3. Build Portable Lite Version (Framework Dependent) ---
Write-Host "Building Portable Lite Version (Framework-Dependent)..." -ForegroundColor Cyan
# We don't need to clean again necessarily, but safe to do so or just overwrite
$portableLiteDir = Join-Path $outputDir "PortableLite"
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:DebugType=None -o $portableLiteDir

if ($LASTEXITCODE -eq 0) {
    $exePath = Join-Path $portableLiteDir "WallpaperRotator.exe"
    $destPath = Join-Path $outputDir "WallpaperRotator-Lite.exe"
    
    # Move EXE to output directory
    Move-Item -Path $exePath -Destination $destPath -Force
    Write-Host "Portable Lite Version created at: $destPath" -ForegroundColor Green
    
    # Clean up temp portable folder
    Remove-Item $portableLiteDir -Recurse -Force
} else {
    Write-Error "Portable Lite build failed!"
}

# --- 4. Compile Installer ---
Write-Host "`nChecking for Inno Setup Compiler..." -ForegroundColor Cyan

if (Test-Path $isccPath) {
    Write-Host "Compiling Installer..." -ForegroundColor Cyan
    & $isccPath "installer.iss"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Installer created successfully!" -ForegroundColor Green
        Write-Host "Installer Location: $outputDir\WallpaperRotator-Setup-v$version.exe" -ForegroundColor Green
        
        # Clean up Standard build directory
        if (Test-Path $standardDir) {
            Remove-Item $standardDir -Recurse -Force
            Write-Host "Cleaned up Standard build directory." -ForegroundColor Cyan
        }
    } else {
        Write-Error "Installer compilation failed."
    }
} else {
    Write-Host "Inno Setup Compiler not found. Please compile 'installer.iss' manually." -ForegroundColor Yellow
}

Write-Host "`nBuild Process Complete!" -ForegroundColor Green
Write-Host "All artifacts are in the '$outputDir' folder." -ForegroundColor Green
