# Portable Configuration File Design

## Goal

Use one portable text configuration file for the machine. The application must load
that file at startup and save current settings when the user selects Save Settings
or closes the application.

## Settings UI

The Configuration Profiles panel is removed. The Settings page contains:

- A `Configuration file` path field.
- A `Browse` action that selects a `.txt` file from a local drive or UNC path.
- One `Save Settings` action that writes all current settings to the selected file.

## File Selection And Startup

1. On first use, the default configuration path is
   `%USERPROFILE%\Documents\DACDT_2026\DACDT_2026_settings.txt`.
2. The selected configuration path is stored separately as local app metadata so
   the main configuration file remains portable.
3. At startup, the application reads the remembered configuration path and loads
   the `.txt` file before populating the Settings UI.
4. If the remembered file is missing or unavailable, including an unavailable UNC
   network path, the application shows a file-selection dialog. If the dialog is
   cancelled, the default Documents path remains selected and is created by the
   next Save Settings action or normal application close.

## Save Behavior

- Save Settings writes the complete settings snapshot to the selected `.txt` file.
- Closing the application also writes the complete snapshot to that same file.
- A successful file selection is remembered immediately for the next launch.
- Write failures are reported to the user and do not change the remembered path.

## Portability

The selected `.txt` file contains machine settings only. It can be copied to a
different machine, selected once through Browse, and will then load automatically
on each later application start.

## Verification

- Test the default path and persisted selected-path behavior.
- Test startup loading from the remembered file.
- Test missing/unreachable configuration files request a replacement file.
- Test Save Settings and the close path use the selected configuration file.
