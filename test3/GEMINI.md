# Gantry SCADA Robot Control

## Project Overview
This is a .NET Framework 4.8 Windows Forms application designed for the control and monitoring of a Gantry SCADA Robot. It integrates Mitsubishi Q-series PLCs for motion control and uses DXF files for path planning and automation.

### Main Technologies
- **Backend:** C# (.NET Framework 4.8)
- **Frontend:** WebView2 (HTML5/JavaScript/CSS)
- **PLC Communication:** Mitsubishi MX Component (ActUtlType COM Interop)
- **CAD Support:** netDxf (for DXF file parsing)
- **Architecture:** Bridge pattern between WinForms/C# and WebView2/JS

## Core Components
- **`Form1.cs`**: The main application controller and bridge. It manages the WebView2 lifecycle, handles UI messages, and coordinates between PLC and CAD services.
- **`PLCCommunication.cs`**: High-level wrapper for Mitsubishi PLC communication. It handles connecting, reading/writing devices (D, M, X, Y), and direct Buffer Memory (U\G) access.
- **`CadDocumentService.cs`**: Service for loading DXF files, extracting geometric primitives (Lines, Arcs, Circles, Polylines), and identifying key points for robot positioning.
- **`test1/ui/`**: Contains the web-based dashboard assets (`index.html`, `app.js`, `styles.css`).

## Building and Running
### Prerequisites
- Visual Studio 2019 or later.
- .NET Framework 4.8 Developer Pack.
- **Mitsubishi MX Component v5.0** (Required for `ActUtlType` COM objects).
- Microsoft Edge WebView2 Runtime.

### Build
Open `test1.sln` in Visual Studio and build the solution (usually `Debug` or `Release` for `x86` or `AnyCPU`).

### Run
The application requires a network connection to a Mitsubishi Q-series PLC. The default configuration points to `192.168.3.39:3000`. This can be adjusted in the UI or `Form1.cs`.

## Development Conventions
- **UI Bridge**: Communication between C# and JS is handled via `webView.CoreWebView2.PostWebMessageAsJson` (C# to JS) and `window.chrome.webview.postMessage` (JS to C#).
- **PLC Polling**: Real-time monitoring is implemented via a `System.Windows.Forms.Timer` in `Form1.cs` with a 50ms interval.
- **Naming Conventions**: PLC devices should use uppercase (e.g., `D100`, `M2000`). Buffer memory is accessed using the `U{IO}\G{Addr}` format.
- **Asynchronous Patterns**: WebView2 initialization and complex PLC/CAD operations should be handled asynchronously to keep the UI responsive.
- **Logging**: A built-in logging system in `Form1.cs` tracks PLC I/O operations and system events, which are then displayed in the "Logs" tab of the UI.

## Key Files
- `test1/Form1.cs`: Central logic and UI bridge.
- `test1/PLCCommunication.cs`: Mitsubishi PLC communication layer.
- `test1/CadDocumentService.cs`: DXF parsing and geometry extraction.
- `test1/ui/app.js`: Frontend application logic.
- `test1/PLC_COMMUNICATION_GUIDE.md`: Detailed documentation for PLC device mapping and communication protocols.
