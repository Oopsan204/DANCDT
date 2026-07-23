param(
    [string]$TaskPath = "docs/ui-task.md",
    [string]$ContractPath = "docs/ui-contract.md",
    [string]$Mode = "accept-edits",
    [string]$Model = ""
)

$ErrorActionPreference = "Stop"

if ($PSBoundParameters.Values -contains "--dangerously-skip-permissions") {
    throw "Refusing to pass --dangerously-skip-permissions to Antigravity."
}

$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

$agyCommand = Get-Command agy -ErrorAction SilentlyContinue
$agyPath = if ($agyCommand) { $agyCommand.Source } else { Join-Path $env:LOCALAPPDATA "agy\bin\agy.exe" }
if (-not (Test-Path -LiteralPath $agyPath)) {
    throw "Antigravity CLI 'agy' was not found in PATH or at the default Windows install path. Install it or add it to PATH, then rerun this script."
}

if (-not (Test-Path -LiteralPath $TaskPath)) {
    throw "Missing UI task file: $TaskPath"
}

if (-not (Test-Path -LiteralPath $ContractPath)) {
    throw "Missing UI contract file: $ContractPath"
}

$allowedUiPaths = @(
    "src/DACDT_2026.App/Views/**",
    "src/DACDT_2026.App/Assets/**",
    "src/DACDT_2026.App/app_icon.*",
    "assets/design/**"
)

$forbiddenLogicPaths = @(
    "src/DACDT_2026.App/Form1.cs",
    "src/DACDT_2026.App/Form1.PlcControl.cs",
    "src/DACDT_2026.App/Form1.DxfHandler.cs",
    "src/DACDT_2026.App/Form1.Camera.cs",
    "src/DACDT_2026.App/Form1.StatePublisher.cs",
    "src/DACDT_2026.App/PLCCommunication.cs",
    "src/DACDT_2026.App/QD75BufferWriter.cs",
    "src/DACDT_2026.App/QD75RingBufferRunner.cs",
    "src/DACDT_2026.App/EngraveCutProcessComposer.cs",
    "src/DACDT_2026.App/CadDocumentService.cs",
    "src/DACDT_2026.App/CadPathSelection.cs",
    "tests/**"
)

function Normalize-RepoPath([string]$Path) {
    return $Path.Replace("\", "/").Trim()
}

function Test-AllowedUiPath([string]$Path) {
    $normalized = Normalize-RepoPath $Path
    foreach ($pattern in $allowedUiPaths) {
        $likePattern = $pattern.Replace("**", "*")
        if ($normalized -like $likePattern) {
            return $true
        }
    }

    return $false
}

function Test-ForbiddenLogicPath([string]$Path) {
    $normalized = Normalize-RepoPath $Path
    foreach ($pattern in $forbiddenLogicPaths) {
        $likePattern = $pattern.Replace("**", "*")
        if ($normalized -like $likePattern) {
            return $true
        }
    }

    return $false
}

$contract = Get-Content -LiteralPath $ContractPath -Raw
$task = Get-Content -LiteralPath $TaskPath -Raw
$prompt = @"
You are the UI implementation agent for this repository.

Read and obey this UI contract:
$contract

Current task:
$task

You may only edit these paths:
$($allowedUiPaths -join "`n")

You must not edit these paths:
$($forbiddenLogicPaths -join "`n")

Do not write business logic. Do not change PLC, QD75, camera, MQTT, WebRTC, laser power, or motion-control behavior.
Use existing bindings and commands. If a required binding or API is missing, stop and report what Codex must add.
After editing, run the smallest relevant UI/build check available in this repository.
"@

$arguments = @("--mode", $Mode)
if ($Model.Trim().Length -gt 0) {
    $arguments += @("--model", $Model)
}

$arguments += @("-p", $prompt)
& $agyPath @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Antigravity CLI exited with code $LASTEXITCODE."
}

$changedPaths = @()
$changedPaths += git diff --name-only
$changedPaths += git ls-files --others --exclude-standard
$changedPaths = $changedPaths | Where-Object { $_ } | Sort-Object -Unique

foreach ($path in $changedPaths) {
    if (Test-ForbiddenLogicPath $path) {
        throw "Antigravity changed a forbidden logic path: $path"
    }

    if (-not (Test-AllowedUiPath $path)) {
        throw "Antigravity changed a path outside the UI allow-list: $path"
    }
}

Write-Host "Antigravity UI changes stayed inside the approved UI paths."
