# English UI and Settings Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Simplify System Settings and make all operator-facing UI English while preserving Vietnamese Help content and all machine behavior.

**Architecture:** Keep existing bindings and configuration keys. Limit behavior changes to consolidating G-code Apply handling; make the rest presentation-only and verify the PLC boundary remains untouched.

**Tech Stack:** .NET Framework 4.8, WPF XAML, C#, existing console test project, MSBuild x86.

## Global Constraints

- Help content remains Vietnamese.
- Existing binding property names and persisted configuration keys remain unchanged.
- PLC coordinate transfer, M-code, laser-power switching, QD75, and WebRTC behavior must not change.
- No new dependency or localization framework.

---

### Task 1: Define the UI text contract

**Files:**
- Modify: `tests/DACDT_2026.Tests/Program.cs`
- Test: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`

- [x] Add a test that reads `SettingsView.xaml`, requires the new English section/field labels, and rejects `Single DXF Speed M04` and the separate `Apply Speed` button.
- [x] Add a test that scans non-Help XAML files for the known Vietnamese operator labels being replaced.
- [x] Run the test and confirm it fails on the current XAML.

### Task 2: Simplify System Settings

**Files:**
- Modify: `src/DACDT_2026.App/Views/SettingsView.xaml`
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/WpfUiState.cs`

- [x] Rename the DXF section and fields using the approved English labels.
- [x] Remove the obsolete `GlobalSpeedInput` field from DXF Settings without deleting the backing property or configuration key.
- [x] Move `RapidSpeedInput` into G-code Motion and remove the separate Apply Speed control.
- [x] Update `ApplyGcodeSettingsAsync` to save both M03 and G00 values through the existing state fields.
- [x] Remove the now-unused `SetG0SpeedCommand` UI command property and registration.
- [x] Run the UI text contract and existing tests.

### Task 3: Normalize operator-facing English

**Files:**
- Modify: `src/DACDT_2026.App/Views/*.xaml` except `HelpView.xaml`
- Modify: user-facing notification/log strings in `src/DACDT_2026.App/*.cs`
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs`

- [x] Change process display values from `Khac`/`Cat` to `Engrave`/`Cut` without changing internal process kinds.
- [x] Translate remaining operator-facing Vietnamese labels, dialogs, notifications, and log text to English.
- [x] Leave comments, internal identifiers, protocol values, and Help content unchanged.
- [x] Run the full test executable.

### Task 4: Build and visual verification

**Files:**
- Verify: `src/DACDT_2026.App/DACDT_2026.csproj`

- [x] Build tests with MSBuild and run `DACDT_2026.Tests.exe`; expect `All tests passed.`
- [x] Build the application with `/p:Platform=x86`; expect both DACDT_2026 and WebRtcCameraService outputs.
- [x] Open System Settings and verify labels, grouping, spacing, and no overlap.
- [x] Confirm no diff in `Form1.PlcControl.cs`, `PLCCommunication.cs`, `QD75BufferWriter.cs`, or `QD75RingBufferRunner.cs`.
- [x] Commit the verified implementation.
