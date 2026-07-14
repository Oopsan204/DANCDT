# English UI and Settings Cleanup Design

## Goal

Make the operator interface consistently English, except for the Help page, and simplify System Settings without changing machine behavior.

## Scope

- Translate user-facing labels, buttons, statuses, dialogs, notifications, and log messages to English.
- Keep the Help view and its instructional content in Vietnamese.
- Remove `Single DXF Speed M04` from System Settings because the active DXF workflow uses separate Engrave Speed and Cut Speed values.
- Keep the underlying `globalSpeed` setting and configuration key for backward compatibility.
- Move G00 rapid speed into the G-code group and apply G-code speed settings with one button.
- Keep M03/M04 dwell values, test-area speed, offsets, WCS offsets, workspace size, and profiles because each has independent behavior.

## Settings Layout

### DXF Processing

- X Offset (mm)
- Y Offset (mm)
- Travel Speed (M03 / Home) (mm/min)
- Laser On Delay (M03) (ms)
- Laser Off Delay (M04) (ms)
- Test Area Speed (mm/min)
- Engrave Speed (mm/min)
- Engrave Power (%)
- Cut Speed (mm/min)
- Cut Power (%)
- One `Apply DXF Settings` command

### G-code Motion

- Laser-On Speed (M03) (mm/min)
- Rapid Travel Speed (G00) (mm/min)
- One `Apply G-code Settings` command

### Workspace

- Workspace Width (mm)
- Workspace Height (mm)
- One `Apply Workspace` command

WCS offsets and configuration profiles remain separate operational sections.

## Compatibility Boundaries

- Do not rename existing WPF binding properties or persisted configuration keys.
- Do not change PLC coordinate transfer, M-code generation, laser-power switching, QD75 writing, or WebRTC lifecycle code.
- Internal process values remain `engrave` and `cut`; only their user-facing display becomes `Engrave` and `Cut`.

## Verification

- Add a source-level UI text contract test that verifies obsolete labels are gone, required English labels exist, and the Help view is excluded from English-only checks.
- Build and run the existing test executable.
- Build the x86 WPF application.
- Open System Settings and verify the revised grouping and labels at the target desktop resolution.
- Confirm the PLC-boundary files have no diff.
