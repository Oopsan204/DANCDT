# Remove Telemetry Design

## Goal

Remove the Telemetry feature completely because it is no longer needed. The application must keep Dashboard, DXF/G-code Run, Monitor, Logs, Settings, and Help working as before.

## Scope

- Remove the `Telemetry` navigation button and the `telemetry` view route.
- Remove `TelemetryView.xaml` and its code-behind from the project.
- Remove Telemetry-only state, commands, collections, models, and PLC read/write handlers.
- Remove Telemetry state publishing and polling branches.
- Remove the project-file page entry for the Telemetry view.
- Preserve unrelated PLC write operations and existing user changes in the worktree.

## Design

The navigation surface will expose only the remaining views. `WpfUiState` will no longer publish an `IsTelemetryView` property or Telemetry collections/commands. `Form1` will no longer initialize Telemetry commands or hold Telemetry register/buffer configuration. State publishing will stop scheduling Telemetry reads, and PLC polling will no longer refresh a removed view. The root XAML will remove the Telemetry view element, leaving the remaining views unchanged.

The cleanup is intentionally limited to feature-specific code. Existing messages that use the word “Telemetry” as an incidental notification category will be reviewed individually and retained unless they are part of the removed feature, so operational error handling is not accidentally changed.

## Verification

- Add or update a lightweight regression check proving the sidebar and root view markup no longer contain the Telemetry route/view.
- Run the focused test project and the application build.
- Search the application source and project file for feature-specific identifiers; no executable Telemetry feature references should remain.
- Review `git diff` and ensure unrelated pre-existing modifications are preserved.
