# Deep CAD Interaction Performance Design

**Date:** 2026-07-23

## Goal

Make pan, zoom, and Cut/Engrave path selection remain responsive on large DXF files while preserving the complete internal command list used by the PLC.

## Scope

- Remove the WPF visual element created for every selectable CAD path.
- Select the nearest path through a spatial index over the preview coordinates.
- Keep the existing combined vector preview, work-area frame, G-code editor, and all PLC command data.
- Update selection color immediately.
- Compile the Cut/Engrave PLC program only after selection activity becomes quiet, and only publish the newest completed build.
- Force a current PLC program before commands that consume `processRows`.
- Do not reintroduce MQTT or web features.

## Measured Cause

The supplied DXF is about 5.47 MB and contains 2,366 paths with 151,369 normalized points.

- Building the PLC rows takes about 276 ms, creates 151,370 rows, and allocates about 30 MB.
- Creating the old selection data takes about 26 ms and allocates about 9.6 MB.
- Constructing 2,366 transparent WPF `Polyline` elements takes about 307 ms; layout takes another 168 ms.
- Software rendering measured about 387 ms per frame with the transparent selection layer, compared with about 277 ms for the combined geometry alone.

Disabling `IsHitTestVisible` during pan does not remove those shapes from WPF rendering and tessellation. Rebuilding all PLC rows after each selection also competes with the UI thread through CPU and allocation pressure.

## Design

### 1. One rendered CAD layer

The view retains the three combined `Path` geometries used for normal, Engrave, and Cut lines. The transparent `ItemsControl` and its per-path `Polyline` children are removed completely.

A single lightweight selection overlay displays immediate feedback for the most recently toggled path. When the rebuilt Engrave/Cut geometry is published, the overlay is cleared.

### 2. Spatial nearest-path selection

`CadPathHitIndex` is a UI-independent uniform-grid index built from the same projected preview points used to draw the CAD.

- Each segment is registered only in grid cells intersecting its bounding box.
- A click or tap queries only nearby cells.
- Exact point-to-segment squared distance chooses the nearest segment inside the requested radius.
- Equal distances are resolved by the lowest path ID, so tests and operator behavior are deterministic.
- The index returns the selected path ID and its point sequence; it creates no WPF visual elements.

The hit radius remains approximately 12 screen DIPs. The view converts that radius to CAD-content units using the current Viewbox scale and zoom, so selection feels consistent across screen sizes.

### 3. Interaction rendering cache

While mouse/touch pan or pinch zoom is active, the CAD content uses a WPF `BitmapCache`. This lets WPF transform one cached image instead of tessellating all vector paths every frame.

When interaction ends, the cache is removed and the normal vector geometry is rendered again. The stopped image therefore remains sharp. Mouse wheel zoom uses a short idle timer before restoring vector rendering.

### 4. Latest-wins PLC compilation

Every path toggle immediately updates the path's process kind and selection overlay, then marks the CAD program dirty and increments a version number.

After a quiet interval, one background compilation starts for the captured document and version. If another selection arrives:

- the pending delay is cancelled;
- a running build observes cancellation checks in its path/point loops;
- a completed stale build is discarded and never replaces `processRows`.

Only a build whose document reference and version still match the active CAD document may publish Engrave/Cut documents and PLC rows.

`EnsureCadProgramCurrentAsync` is called before operations that consume the current DXF command list, including mixed RUN/send and QD75 export. Test Area may temporarily replace `processRows`; the CAD program remains marked dirty so the next CAD RUN/export recompiles the selected paths first.

### 5. Data ownership

- `activeCadDocument` remains the complete source of CAD primitives.
- `processRows` remains the complete internal PLC command list.
- Preview geometry and the hit index are presentation data only.
- No coordinate or process table is added back to the interface.

### 6. Large-data publication hardening

- Preview documents contain drawable primitives only; they do not duplicate hidden coordinate rows.
- The one-million-point preview budget is distributed across the whole drawing, while each retained primitive keeps at least two points.
- Initial and selection-refresh geometry share one offset-aware display-document builder.
- The temporary selection overlay is capped at 10,000 sampled points.
- Dashboard and Monitor materialize process rows in 100-row windows; the full `processRows` list remains owned by the PLC pipeline.
- DXF UI publication uses a latest-version guard so an older background preview cannot overwrite newer geometry or row windows.

## Failure and Concurrency Behavior

- A cancelled or stale compile is silent and cannot overwrite newer rows.
- A real compile error is logged and leaves the program dirty.
- RUN/export waits for the newest compile instead of using stale selection data.
- Changing or unloading the active document invalidates pending compilation and selection index data.
- Path selection remains disabled while a PLC program is running, preserving the current safety rule.

## Verification

- Unit tests cover nearest-segment selection, miss behavior, deterministic ties, and large-index queries.
- Unit tests cover dirty/current version transitions and stale-result rejection.
- Source-contract tests verify there is no per-path `ItemsControl`/`Polyline`, interaction caching is wired, selection schedules latest-wins compilation, and PLC consumers ensure current rows.
- The supplied large DXF is used for a repeatable hit-index benchmark.
- Build the tests and WPF application with Visual Studio MSBuild for .NET Framework 4.8, C# 7.3, x86.
