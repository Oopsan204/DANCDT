# DACDT_2026 Codex Handoff

Last updated: 2026-06-02

Use this file when the chat loses context, token, or quota. Tell Codex:

```text
Read D:\DACDT_2026\DANCDT\DACDT_2026\CODEX_HANDOFF.md first, then continue the DACDT_2026 work from there.
```

## Project

- Main repo: `D:\DACDT_2026\DANCDT\DACDT_2026`
- Main WPF project: `D:\DACDT_2026\DANCDT\DACDT_2026\DACDT_2026`
- Solution: `D:\DACDT_2026\DANCDT\DACDT_2026\DACDT_2026.sln`
- Do not modify sample project: `D:\DADV\WpfYolo_Detect_RealTime`

## User Preferences

- UI must be pure WPF/XAML. No HTML/JS/WebView UI.
- Keep current logic and structure as much as possible; UI/layout changes should not rewrite PLC/G-code logic unless needed.
- The app text should be English.
- UI style: dark industrial dashboard, rounded corners, compact spacing, efficient layout.
- Dashboard keeps 3 axes visible.
- No Z view in DXF/G-code CAD/process UI, but internal Z logic may remain if needed for PLC behavior.

## Current App State

- WinForms/WebView UI was converted to WPF.
- Main window is `Form1.xaml` / `Form1.cs`.
- UI state is in `WpfUiState.cs`.
- Major views are in `Views\*.xaml`.
- Reusable panel controls are in `Views\Panels\*.xaml`.
- Styling is in `Views\Styles.xaml`.
- Commands use `RelayCommand.cs`.
- Converters are in `Converters.cs`.

## Important Recent Changes

- DXF/G-code open/preview is guarded by `cadLoadGate` so file loads do not overlap.
- Removed risky `Thread.Abort`, forced `GC.Collect`, WinForms dialogs, and `InvokeRequired` patterns from DXF/G-code handling.
- File parsing, process row import, scan limits, and CAD view model building are moved to background tasks where practical.
- CAD preview supports pan/zoom, non-scaling stroke thickness, workspace limit area, X/Y axes, and robot tracking point.
- Robot tracking point uses actual PLC position:
  - Axis 1 `D0` as X
  - Axis 2 `D10` as Y
  - Raw to mm: `raw / 10000`
- G-code `G54-G59` WCS offsets are read by parser and applied to:
  - Process Table display
  - CAD Preview display
  - Geometry Data display
  - PLC send data
- DXF offset X/Y is also applied to display preview/geometry while raw data is kept internally to avoid double offset on send.
- Process Table has `No.` index column.
- Axis labels `X` and `Y` are placed near the axis arrows close to the origin, matching the user reference image.

## PLC Buttons / Addresses

- HOME: `M502`
- RESET: `M400`
- START ACTION: `M2000`
- CONTINUE: `M401`
- PAUSE: `M402`
- Manual jog base: `M3000-M3005`
- Emergency stop: `M3100`
- Jog speed label should be `Jog speed D406 (mm/min)`.

## Build Command

Use a separate output directory to avoid Visual Studio locking `bin\Debug`:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
  "D:\DACDT_2026\DANCDT\DACDT_2026\DACDT_2026.sln" `
  /p:Configuration=Debug `
  /p:Platform="Any CPU" `
  /p:OutDir="D:\DACDT_2026\DANCDT\DACDT_2026\DACDT_2026\bin\CodexDebug\" `
  /v:m
```

Known build warnings:

- `PLCCommunication.cs(356,39)` warning CS0675
- `PLCCommunication.cs(398,39)` warning CS0675

These warnings existed during prior successful builds and are not related to UI/CAD changes.

## Smoke Test

```powershell
$exe = 'D:\DACDT_2026\DANCDT\DACDT_2026\DACDT_2026\bin\CodexDebug\DACDT_2026.exe'
$p = Start-Process -FilePath $exe -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 5
if ($p.HasExited) {
  "Exited early with code $($p.ExitCode)"
} else {
  Stop-Process -Id $p.Id
  "Started successfully for 5 seconds; stopped test process $($p.Id)."
}
```

## Parser Sequence Test

```powershell
$ErrorActionPreference = 'Stop'
$asm = 'D:\DACDT_2026\DANCDT\DACDT_2026\DACDT_2026\bin\CodexDebug\DACDT_2026.exe'
[Reflection.Assembly]::LoadFrom($asm) | Out-Null
$cad = New-Object DACDT_2026.CadDocumentService
$gcode = New-Object DACDT_2026.GcodeCoordinateService
$files = @(
  'D:\DACDT_2026\DANCDT\DACDT_2026\Samples\12.dxf',
  'D:\DACDT_2026\DANCDT\DACDT_2026\Samples\Test_G00.gcode',
  'D:\DACDT_2026\DANCDT\DACDT_2026\Samples\TOP_MASK_G.dxf',
  'D:\DACDT_2026\DANCDT\DACDT_2026\Samples\test.nc',
  'D:\DACDT_2026\DANCDT\DACDT_2026\Samples\logo.dxf',
  'D:\DACDT_2026\DANCDT\DACDT_2026\Samples\1mm.gcode',
  'D:\DACDT_2026\DANCDT\DACDT_2026\Samples\ytyt.dxf'
)
foreach ($f in $files) {
  $ext = [IO.Path]::GetExtension($f).ToLowerInvariant()
  if ($ext -in @('.gcode','.g','.gc','.nc','.ngc','.cnc','.tap')) {
    $doc = $gcode.LoadAsCad($f)
  } else {
    $doc = $cad.Load($f)
  }
  $prim = if ($doc.Primitives) { $doc.Primitives.Count } else { 0 }
  $pts = if ($doc.Points) { $doc.Points.Count } else { 0 }
  "OK $([IO.Path]::GetFileName($f)) primitives=$prim points=$pts"
}
```

## Key Files To Inspect First

- `Form1.cs`: WPF host, command wiring, settings, fields.
- `Form1.DxfHandler.cs`: open/preview DXF/G-code, process row import, scan limits, PLC send data.
- `Form1.StatePublisher.cs`: pushes UI state, builds CAD preview geometry, applies display offsets, robot tracking point.
- `WpfUiState.cs`: all WPF view models and observable collections.
- `Views\DxfRunView.xaml`: CAD preview, G-code editor, Process Table, Geometry Data.
- `Views\DashboardView.xaml`: dashboard layout.
- `Views\SettingsView.xaml`: system settings, WCS table, DXF offset/speed settings.
- `GcodeCoordinateService.cs`: G-code parser, WCS index parsing.
- `SimpleDxfParser.cs`: simple DXF parser for ENTITIES section.
- `QD75BufferWriter.cs`: PLC positioning buffer write logic.

## Current Known Behavior / Notes

- DXF parser reads the `ENTITIES` section and supports common entities:
  - `LINE`
  - `ARC`
  - `CIRCLE`
  - `LWPOLYLINE`
  - `SPLINE`
- DXF parser does not currently filter by layer.
- DXF parser does not currently expand all block/INSERT content.
- G-code WCS:
  - `G54` means WCS index 0.
  - `G55` means WCS index 1.
  - ...
  - `G59` means WCS index 5.
- `G54` alone will not visibly shift anything unless G54 offset values are non-zero in the WCS settings table and applied/saved.
- CAD display uses offset-adjusted document only for display. Keep raw document for process/send path to prevent double offset.

## Before Making Changes

1. Run `git status --short` from `D:\DACDT_2026\DANCDT\DACDT_2026`.
2. Do not revert unrelated user changes.
3. Prefer small scoped patches.
4. Use `apply_patch` for manual edits.
5. Build with the `CodexDebug` output command above.
6. Mention if only existing PLCCommunication warnings remain.

