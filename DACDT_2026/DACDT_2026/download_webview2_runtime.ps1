# Script to download and setup embedded WebView2 Runtime (Fixed Version)
# This script provides two options:
#   1. Download Fixed Version for embedding (manual download + extract)
#   2. Install Evergreen Runtime system-wide (automatic)

$ErrorActionPreference = "Stop"

$appDir = $PSScriptRoot
$runtimeDir = Join-Path $appDir "WebView2Runtime"

Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     DACDT_2026 - WebView2 Runtime Setup             ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "Select setup option:" -ForegroundColor Yellow
Write-Host "  [1] Install Evergreen Runtime system-wide (recommended, automatic)" -ForegroundColor White
Write-Host "  [2] Download Fixed Version for embedding (manual, ~150MB)" -ForegroundColor White
Write-Host "  [3] Cancel" -ForegroundColor White
Write-Host ""
$choice = Read-Host "Enter 1, 2, or 3"

switch ($choice) {
    "1" {
        Write-Host "`nInstalling WebView2 Runtime system-wide..." -ForegroundColor Yellow
        $setupDir = Join-Path $env:TEMP "WebView2Setup"
        New-Item -ItemType Directory -Path $setupDir -Force | Out-Null
        $installerPath = Join-Path $setupDir "MicrosoftEdgeWebView2Setup.exe"

        Write-Host "Downloading installer (Evergreen Standalone)..." -ForegroundColor Green
        Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/p/?LinkId=2124703" -OutFile $installerPath

        Write-Host "Running installer. Please follow the prompts..." -ForegroundColor Green
        Start-Process -FilePath $installerPath -Wait

        Write-Host "`n✓ Installation complete!" -ForegroundColor Green
        Write-Host "You can now run DACDT_2026." -ForegroundColor Cyan
    }
    
    "2" {
        Write-Host "`nTo embed WebView2 Runtime into the application:" -ForegroundColor Yellow
        Write-Host "Step 1: Download the Fixed Version from Microsoft:" -ForegroundColor White
        Write-Host "  https://developer.microsoft.com/en-us/microsoft-edge/webview2/" -ForegroundColor Cyan
        Write-Host "  Click 'Download Fixed Version' -> download the .cab or .exe"
        Write-Host ""
        Write-Host "Step 2: Run the downloadred file or extract the CAB to:" -ForegroundColor White
        Write-Host "  $runtimeDir" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Step 3: After extraction, your folder should contain:" -ForegroundColor White
        Write-Host "  - msedgewebview2.exe"
        Write-Host "  - WebView2Loader.dll"
        Write-Host "  - various .dll files"
        Write-Host ""
        Write-Host "Step 4: Rebuild and run DACDT_2026" -ForegroundColor White
        Write-Host "  The embedded Runtime will be used automatically." -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Note: Fixed Version download requires a Microsoft account login." -ForegroundColor Yellow
        Write-Host "For most users, Option 1 (system-wide install) is recommended instead." -ForegroundColor Yellow
    }

    "3" {
        Write-Host "Setup cancelled." -ForegroundColor Red
    }

    default {
        Write-Host "Invalid choice. Setup cancelled." -ForegroundColor Red
    }
}

Write-Host "`nPress any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")