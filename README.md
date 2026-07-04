# DACDT 2026

Gantry SCADA/control workspace for the DACDT 2026 project.

## Folder layout

```text
src/
  DACDT_2026.App/        Main WPF control application
  WebRtcCameraService/   Background WebRTC camera bridge
assets/
  design/                UI/design spreadsheets and slides
  references/            PLC/mechanical reference documents
  samples/               DXF, G-code, NC, and DWG sample files
  machine-design/        Machine design files
libs/                    Vendor/COM interop libraries
tools/                   Simulator and installer scripts
docs/                    Notes, handoff, plans, and project documentation
```

Standalone DXF library experiments were moved outside this do-an repository to:

```text
D:\DACDT_2026\Archived_Outside_DoAn\DxfLibrary
```

## Build

Open `DACDT_2026.sln` from this repository root.

Command-line debug build:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
  ".\DACDT_2026.sln" `
  /t:Build `
  /p:Configuration=Debug `
  /p:Platform=x86 `
  /m
```

If NuGet packages are missing, restore packages from Visual Studio before building.
