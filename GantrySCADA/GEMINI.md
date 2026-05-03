# Trả lời bằng tiếng việt 

Thay đổi xong thì tự build lại

cho tôi xem thay đổi j trc khi thay đổi code

# GantrySCADA Project Instructions

This project is a **WPF/Blazor Hybrid** application designed for controlling and monitoring a **Gantry Robot** system via **PLC (Programmable Logic Controller)**.

## 🚀 Project Overview

- **Technologies:** .NET 8.0, WPF, Blazor WebView, C#.
- **Primary Purpose:** Industrial SCADA (Supervisory Control and Data Acquisition) for Gantry systems.
- **Key Capabilities:**
  - Real-time PLC communication (Mitsubishi MC Protocol & MX Component).
  - Motion control (Jogging, Trajectory execution).
  ### DXF/CAM processing (Advanced Trajectory Generation).
  - **Protocol:** Uses a 10-word buffer frame per trajectory point.
  - **Addressing:** Writes directly to Simple Motion module buffer memory:
      - Axis 1 (X): `U0\G2000`
      - Axis 2 (Y): `U0\G8000`
  - **Advanced Features:**
      - **Travel Moves:** Automatically inserts travel moves (G0) between contours with M-Code 0.
      - **Per-point Speed:** Configurable speed for every individual motion segment.
      - **Trajectory Editing:** UI support for reordering (Up/Down), deleting contours, and adding manual points.
      - **Buffer Monitoring:** Real-time visualization of raw buffer data (Binary/Decimal) in the LogMonitor.
      - **Entity Support:** Enhanced support for Polylines (auto-close), Lines, Circles, Arcs, and Points.
  - **Command Codes:**
      - Linear: `H100A` (END), `H500A` (Cont. Pos), `HD00A` (Cont. Path).
      - Circular CW: `H100F` (END), `H500F` (Cont. Pos), `HD00F` (Cont. Path).
      - Circular CCW: `H1010` (END), `H5010` (Cont. Pos), `HD010` (Cont. Path).
  - **Data Scaling:** Coordinates and center points are scaled by 1000 (mm to µm).

  - Centralized logging and telemetry.

## 🏗️ Architecture

The application follows the **MVVM (Model-View-ViewModel)** pattern with a Blazor frontend hosted within a WPF window.

- **Host:** `MainWindow.xaml` hosts a `BlazorWebView`.
- **Core Logic:** `MainViewModel` (Singleton) manages all state and PLC interaction.
- **Frontend:** Razor components in the `Pages/` directory.
- **PLC Interface:** Dual-path communication via `PlcMcShim` (MC Protocol) and `MxBufferClient` (MX Component).

## 📂 Key Files & Directories

- `MainViewModel.cs`: Core ViewModel, split into multiple partial files by feature:
  - `MainViewModel.State.cs`: Shared state, address mapping, and settings.
  - `MainViewModel.ReadFeature.cs`: Periodic read cycle logic.
  - `MainViewModel.WriteFeature.cs`: Pending write queue and execution.
  - `MainViewModel.MotionAndLoggingFeature.cs`: Jogging and centralized logging.
  - `MainViewModel.DxfFeature.cs`: DXF parsing and trajectory generation.
  - `MainViewModel.CustomMemoryFeature.cs`: Dynamic address monitoring.
- `PlcMcShim.cs`: Shim for `HslCommunication` Melsec MC Protocol.
- `MxBufferClient.cs`: Client for Mitsubishi MX Component (COM) for buffer memory access.
- `Pages/`: Blazor pages (`Dashboard`, `Telemetry`, `LogMonitor`, `DxfRun`, `SystemSettings`).
- `wwwroot/index.html`: Entry point for the Blazor UI.

## 🛠️ Building and Running

### Prerequisites

- .NET 8.0 SDK.
- **Target Architecture:** `x86` (Required for MX Component COM interop).
- Mitsubishi MX Component installed (for `U...\G...` buffer access).

### Commands

- **Build:** `dotnet build GantrySCADA.csproj -c Debug`
- **Run:** `dotnet run --project GantrySCADA.csproj -c Debug`
- **Clean:** `dotnet clean`

## 📏 Development Conventions

### ViewModel Organization

- **Partial Classes:** Always add new ViewModel features to a new partial class file following the `MainViewModel.[FeatureName]Feature.cs` naming convention.
- **Thread Safety:** Use `_plcSync` for synchronizing PLC access and `_pendingWriteLock` for the write queue.
- **UI Updates:** Ensure properties call `SetProperty` or `OnPropertyChanged` to notify the Blazor UI.

### PLC Communication

- **Monitor Loop:** The `Monitor()` thread runs at ~100Hz (10ms sleep).
- **Write Queue:** Prefer `MarkPendingWrite()` over direct PLC writes for UI actions to ensure they are processed sequentially in the monitor loop.
- **Addressing:** Use `BuildAddress()` and `PlcBitHelper` for consistent address formatting.

### Logging

- Use `ViewModel.AddLog(source, status, message, detail)` for all significant events.
- Sources: `"UI"`, `"PC"`, `"PLC"`.
- Statuses: `"info"`, `"success"`, `"warning"`, `"error"`.

### UI Styling

- Use Vanilla CSS and standard Razor components.
- Navigation highlighting is handled in `MainLayout.razor` via the `IsActive()` helper.

## ⚠️ Known Constraints

- The project **must** be built for `x86` to maintain compatibility with Mitsubishi COM libraries.
- PLC communication timeouts and errors are handled by an auto-reconnect mechanism in the monitor loop.
