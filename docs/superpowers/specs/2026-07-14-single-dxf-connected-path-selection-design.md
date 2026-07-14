# Single DXF Connected-Path Selection Design

## Goal

Replace the two-file Engrave/Cut import workflow with one DXF import. The user clicks a contour in CAD Preview to toggle the entire connected contour between Engrave and Cut.

## Confirmed Behavior

- The toolbar has one `Import DXF` button instead of separate `Import Khac` and `Import Cat` buttons.
- Every connected contour starts as `Khac` after import.
- Clicking any visible segment toggles its entire connected contour to `Cat`.
- Clicking a cut contour again toggles the entire contour back to `Khac`.
- Engrave contours are DeepSkyBlue and cut contours are OrangeRed.
- Clicking empty preview space continues to pan. Mouse-wheel zoom and double-click reset keep their current behavior.
- Importing another file resets all contours to `Khac`.
- A selection is session state only. The application does not modify or write classification data back into the DXF file.

## Connected Contour Rule

Use the same endpoint grouping already used by DXF process compilation. Primitive endpoints that match at the existing 0.001 mm precision belong to the same connected contour. This gives preview selection and PLC row compilation the same interpretation of a path.

The complete connected chain is selected as one unit. This includes:

- a square made from four connected LINE entities;
- a connected open chain;
- one ARC, CIRCLE, or POLYLINE entity and any entities connected to its endpoints.

Contours that do not share matching endpoints remain independently selectable.

## UI Design

CAD Preview renders selectable contours individually instead of using only the current merged, non-interactive geometry. Each selectable contour carries a stable path index and its current process kind.

Each contour uses two overlaid strokes:

- a thin visible stroke showing Engrave or Cut color;
- a wider transparent hit-test stroke so thin geometry remains easy to click.

The contour mouse event handles the click before it reaches the preview pan handler. Background clicks still start panning. Selection is ignored while `IsProgramRunning` is true so a running PLC program cannot be changed from the preview.

No instructional panel, modal, or additional selection mode is added.

## Data Flow

1. `Import DXF` loads and normalizes one `CadLoadResult`.
2. All primitives receive `ProcessKind = Khac`.
3. The existing connected-path grouping produces selectable contour groups.
4. CAD Preview receives one selectable view model per contour.
5. Clicking a contour toggles `ProcessKind` on every primitive in that group.
6. The application rebuilds the process table and republishes DXF UI state.

The imported document remains the single source document. Engrave and Cut process documents are temporary filtered views created from it during compilation. Primitive geometry is cloned for compilation so path normalization cannot mutate the master selection document.

## Process Compilation

Rebuilding the program preserves the established mixed-process behavior:

- build all Engrave rows first with Engrave speed and power;
- build all Cut rows second with Cut speed and power;
- keep one `processRows` collection;
- remove the intermediate Engrave home/end row when Cut rows follow;
- retain exactly one final End row after the last Cut row;
- use the non-cut M03 speed for travel rows;
- send all coordinate rows through the existing PLC transfer path.

If every contour is Engrave, no Cut power transition is scheduled. If every contour is Cut, Cut power is set before the run starts. For a mixed program, power changes only at the existing Engrave-to-Cut boundary.

## PLC Safety Boundary

This feature does not change:

- PLC coordinate row layout or destination addresses;
- M-code generation;
- coordinate formatting;
- ring-buffer transfer or run commands;
- laser power write implementation;
- pause, Set Power, and continue sequence used at the Engrave-to-Cut transition.

Only the source classification of DXF primitives and the resulting ordering of existing process rows change.

## Error And Empty States

- Canceling the file dialog leaves the current document unchanged.
- A DXF with no supported geometry shows the existing import error/empty behavior and cannot run.
- An invalid path index or stale click is ignored.
- Clearing or replacing the loaded file also clears selectable contour groups.

## Testing

Automated tests will cover:

- connected LINE entities are assigned to one selectable contour;
- disconnected contours remain separate;
- toggling one path changes every primitive in that path and no other path;
- toggling twice restores Engrave;
- compilation orders Engrave rows before Cut rows with one final End;
- all-Engrave and all-Cut programs select the correct initial power behavior;
- the existing mixed speed and power-transition regression tests remain green.

Manual verification will cover direct clicking, thin-line click tolerance, path colors, background panning, zoom/reset, and the selection lock while a program is running.

## Out Of Scope

- Saving path classifications into the DXF file or a sidecar file.
- Selecting by DXF layer, source color, rectangle, or multi-select modifier keys.
- Changing G-code import or execution behavior.
- Changing PLC transfer, power-write, or camera behavior.
