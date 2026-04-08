# ============================================================
#  Vedion Screen Share — Full Build Pipeline
#  Run from repo root:  .\installer\build.ps1
#  Requirements:
#    - .NET 8 SDK
#    - Inno Setup 6 (https://jrsoftware.org/isinfo.php)
# ============================================================

param(
    [string]$Version = "2.0.0",
    [switch]$SkipPublish,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$Root     = Split-Path $PSScriptRoot -Parent
$Publish  = "$Root\publish\win-x64"
$InnoExe  = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host ""
Write-Host "  VEDION SCREEN SHARE — BUILD v$Version" -ForegroundColor Green
Write-Host "  ─────────────────────────────────────" -ForegroundColor DarkGreen
Write-Host ""

# ── Step 1: dotnet publish ────────────────────────────────────────────
if (-not $SkipPublish) {
    Write-Host "  [1/2] Publishing self-contained .NET 8 binary..." -ForegroundColor Cyan

    if (Test-Path $Publish) {
        Remove-Item $Publish -Recurse -Force
    }

    Push-Location $Root
    dotnet publish `
        -c Release `
        -r win-x64 `
        -p:PublishSingleFile=true `
        -p:SelfContained=true `
        -p:PublishReadyToRun=true `
        -p:DebugType=none `
        -p:DebugSymbols=false `
        -p:EnableCompressionInSingleFile=true `
        -o $Publish
    Pop-Location

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [!] dotnet publish failed." -ForegroundColor Red
        exit 1
    }

    $ExeSize = [math]::Round((Get-Item "$Publish\VedionScreenShare.exe").Length / 1MB, 1)
    Write-Host "  [1/2] Done — EXE: $ExeSize MB" -ForegroundColor Green
} else {
    Write-Host "  [1/2] Skipped publish." -ForegroundColor Yellow
}

# ── Step 2: Inno Setup compile ────────────────────────────────────────
if (-not $SkipInstaller) {
    Write-Host "  [2/2] Compiling installer..." -ForegroundColor Cyan

    if (-not (Test-Path $InnoExe)) {
        Write-Host "  [!] Inno Setup 6 not found at: $InnoExe" -ForegroundColor Red
        Write-Host "      Download: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
        exit 1
    }

    $IssFile = "$PSScriptRoot\setup.iss"
    & $InnoExe /DAppVersion=$Version $IssFile

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [!] Inno Setup compile failed." -ForegroundColor Red
        exit 1
    }

    $InstallerPath = "$PSScriptRoot\output\VedionScreenShare-Setup-v$Version.exe"
    if (Test-Path $InstallerPath) {
        $InstallerSize = [math]::Round((Get-Item $InstallerPath).Length / 1MB, 1)
        Write-Host "  [2/2] Done — Installer: $InstallerSize MB" -ForegroundColor Green
        Write-Host ""
        Write-Host "  OUTPUT: $InstallerPath" -ForegroundColor Green
    }
} else {
    Write-Host "  [2/2] Skipped installer." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  Build complete." -ForegroundColor Green
Write-Host ""
