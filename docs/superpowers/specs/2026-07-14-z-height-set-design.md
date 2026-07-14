# Z Height Set Design

## Goal

Add a Manual Jog control for setting Z height in millimetres. A decimal input is converted to PLC units by multiplying by 10000, written to `D110`, then `M212` is pulsed ON and OFF.

## Boundaries

- The feature is a manual machine setting, not a motion-program row.
- It must not modify `processRows`, QD75 buffers, CAD paths, G-code, laser power, or coordinate transfer.
- It reuses the existing PLC connection guard and device-write path.
- The UI label and command stay in English.

## Data Flow

```text
Z Height (mm) input
        |
        v
DecimalInputParser -> millimetres * 10000 -> integer PLC value
        |
        v
Write D110 -> Write M212=1 -> Write M212=0
```

The command rejects invalid, negative, non-finite, and out-of-range values before any PLC write. The first supported range is `0` through `214748.3647 mm`, which fits the signed 32-bit value used by the existing device writer.

## UI

Add `Z height (mm)` input and `SET` button below the existing jog-speed control in `SidebarControl.xaml`. Bind them to a new `ZHeightInput` property and `SetZHeightCommand`.

## Error Handling

- A disconnected PLC shows the existing connection error and performs no writes.
- Invalid input shows a validation error and performs no writes.
- A write exception is reported through the existing notification and log mechanisms.

## Verification

- Unit tests cover decimal point/comma parsing, conversion to PLC units, invalid input rejection, and the required write order through a small pure helper.
- Build and the existing test executable must pass.
- Antigravity may edit only the Sidebar UI and must not touch PLC or test files.
