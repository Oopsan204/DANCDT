# Settings Save Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `Save Settings` action that persists the current System Settings values to the existing `app_settings.txt` file and restores them on the next application start.

**Architecture:** Reuse `SaveSettingsToFile()` and the existing key-value format. Add one WPF command and a small UI-to-model synchronization method so Save captures edited fields without applying PLC or motion changes. Existing Apply commands keep their current behavior and continue saving automatically.

**Tech Stack:** .NET Framework 4.8, WPF XAML, existing console test executable, MSBuild x86.

## Global Constraints

- Use only the existing `app_settings.txt` format and keys.
- Do not add import/export files, a database, dependencies, or a localization framework.
- Save must not send coordinates, write PLC motion data, change laser power, or start a run.
- Keep Help content and the PLC/WebRTC/QD75 behavior unchanged.

---

### Task 1: Add the failing Settings Save contract test

**Files:**
- Modify: `tests/DACDT_2026.Tests/Program.cs`

- [x] Add `SettingsViewExposesSaveSettingsCommand()` to the test sequence.
- [x] Read `src/DACDT_2026.App/Views/SettingsView.xaml` and assert it contains `Save Settings` and `{Binding SaveSettingsCommand}`.
- [x] Assert the test source references the existing `app_settings.txt` format and does not introduce an export/import command.
- [x] Build and run the test executable; confirm it fails because the new button and command are not present yet.

### Task 2: Add the Save Settings command and capture current UI values

**Files:**
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `src/DACDT_2026.App/Form1.cs`

- [x] Add `public ICommand SaveSettingsCommand { get; set; }` beside the existing Settings commands.
- [x] Register the command in `ConfigureCommands()` with this behavior:

```csharp
ui.SaveSettingsCommand = new RelayCommand(async () =>
{
    SyncSettingsFromUiForPersistence();
    SaveSettingsToFile();
    await NotifyAsync("success", "Settings", "Settings saved locally.");
});
```

- [x] Add `SyncSettingsFromUiForPersistence()` next to `SyncSettingsToUi()` and copy the existing UI-bound values into their existing fields: offsets, rapid/G-code/DXF speeds, engrave/cut speed and power, workspace dimensions, PLC connection values, laser power, theme, active WCS, WCS offsets, and all existing dwell/Z/profile-compatible values.
- [x] Reuse existing parsing and normalization rules for numeric values; do not call PLC write or motion methods from this synchronization method.
- [x] Build and run the tests; confirm the new contract passes and existing tests remain green.

### Task 3: Add the Settings tab button and verify persistence boundaries

**Files:**
- Modify: `src/DACDT_2026.App/Views/SettingsView.xaml`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

- [x] Add one `Save Settings` button to the Settings view, bound to `SaveSettingsCommand`, near the existing Workspace apply action.
- [x] Keep the button visually distinct from `Apply` actions but within the existing panel styles.
- [x] Add source-level assertions that the Settings view has one Save Settings binding and no import/export UI.
- [x] Verify the four existing Apply commands still call `SaveSettingsToFile()` and the four PLC-boundary files have no diff.

### Task 4: Final verification and commit

**Files:**
- Verify: `src/DACDT_2026.App/DACDT_2026.csproj`

- [x] Run the full console test executable and expect `All tests passed.`
- [x] Build the WPF application with `/p:Platform=x86` and expect both DACDT_2026 and WebRtcCameraService outputs.
- [x] Open System Settings, confirm the button is visible without overlap, and confirm the app still starts WebRTC.
- [x] Run `git diff --check` and confirm no diff in `Form1.PlcControl.cs`, `PLCCommunication.cs`, `QD75BufferWriter.cs`, or `QD75RingBufferRunner.cs`.
- [ ] Commit the implementation.
