# Settings Save Design

## Goal

Persist the values entered in the System Settings tab so they are restored when the application starts.

## Scope

- Keep using the existing `app_settings.txt` file and key names.
- Add a `Save Settings` command to the System Settings view.
- Save automatically after each existing Settings apply command.
- Load the saved values during application startup, as the app already does.
- Do not add a separate import/export workflow.
- Do not change PLC coordinate transfer, M-code generation, laser-power switching, QD75 writing, or WebRTC behavior.

## Behavior

`Save Settings` writes the current values for DXF processing, G-code motion, workspace, WCS offsets, PLC connection, laser power, theme, and other existing persisted settings to `app_settings.txt`.

The existing `Apply DXF Settings`, `Apply G-code Settings`, `Apply Workspace`, and `Apply WCS Table` commands continue applying their current behavior and also persist the resulting values. Saving settings does not send coordinates or start machine motion.

## Verification

- Add a test that verifies the Settings view exposes the save command and does not introduce a second settings format.
- Build and run the existing test executable.
- Build the x86 WPF application.
- Confirm the PLC-boundary files remain unchanged.
