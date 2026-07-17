# Tablet CAD Gesture and Selection Performance Design

## Goal

Make DXF cut-path selection reliable and responsive on touch tablets while preserving
mouse operation, CAD pan, and two-finger zoom. PLC coordinate writing and run logic
are out of scope.

## Current Problems

- The transparent CAD hit target is too narrow for a finger at common tablet scale.
- Each path toggle rebuilds the whole process table and CAD view, then waits for MQTT
  publication before the command completes.
- Pinch transforms are applied for each raw touch event. The two touch streams are not
  synchronized, which produces visible zoom and pan jumps.
- Releasing one finger leaves the remaining finger in pinch state instead of returning
  cleanly to one-finger pan.

## Interaction Contract

- A one-finger tap near a DXF primitive toggles the complete connected path between
  Engrave and Cut.
- A second tap on the same path restores Engrave.
- A single-finger drag pans the CAD view after the existing movement threshold.
- Two fingers pinch to zoom around their midpoint and can translate the view together.
- Releasing either pinch finger ends the pinch session. The remaining finger starts a
  new one-finger gesture and cannot accidentally select a path until it is lifted and
  tapped again.
- Mouse click, wheel zoom, and mouse double-click reset keep their existing behavior.

## Selection Design

- Keep a tablet-sized transparent hit stroke of 24 device-independent pixels, adjusted
  inversely for CAD zoom so the on-screen target remains finger friendly.
- Resolve a touch target at touch-down and toggle only on touch-up when the gesture has
  not become a pan or pinch.
- Update the selected path color immediately using only the affected CAD primitives.
- Coalesce repeated touch selections into one deferred rebuild of process rows and one
  MQTT CAD-state publication after a short quiet interval. RUN will always rebuild the
  mixed program before it sends coordinates to the PLC, so deferred UI work cannot
  change the sent program.

## Pinch Design

- Store a fixed primary and secondary touch identifier for the pinch session.
- Keep the latest coordinates from both fingers and apply one combined scale/translation
  update at render cadence, rather than applying a transform for every raw touch event.
- Clamp zoom to the existing minimum and maximum values.
- Ignore promoted mouse events while a touch gesture is active so a second finger cannot
  trigger the mouse double-click reset path.
- Reset touch state as soon as either pinch finger is released or capture is lost.

## Synchronization and Error Handling

- Local color updates remain on the UI dispatcher.
- Rebuild and MQTT work use a monotonic selection version. Work for an older version is
  discarded when a newer tap occurs.
- MQTT publish failures are logged and do not revert or delay the local selection.
- If a DXF import replaces the active document, pending work for the old document is
  discarded.

## Verification

- Unit tests cover the selection coalescing version policy and touch-session state
  transitions.
- Source-contract tests verify the wider hit target, fixed two-touch pinch session,
  mouse-promotion guard, and non-blocking MQTT publication.
- Build the test executable and the x86 Release application after implementation.
