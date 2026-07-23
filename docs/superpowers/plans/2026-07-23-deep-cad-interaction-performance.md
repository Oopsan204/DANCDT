# Deep CAD Interaction Performance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the remaining CAD interaction lag by replacing per-path WPF hit shapes with a spatial index and by publishing only the newest PLC compilation.

**Architecture:** Combined vector geometries remain the only permanent CAD drawing elements. A pure uniform-grid index resolves the nearest path from one pointer coordinate, and a single overlay provides immediate selection feedback. A versioned compile coordinator debounces selection changes, cancels stale work, and requires current rows at PLC execution/export boundaries.

**Tech Stack:** C# 7.3, .NET Framework 4.8 WPF, existing manual C# test executable, Visual Studio MSBuild, x86.

## Global Constraints

- Preserve the complete CAD source and complete internal PLC command list.
- Preserve CAD Preview, G-code Editor, work-area frame, mouse/touch gestures, and Cut/Engrave semantics.
- Do not add the Geometry Data or Process Table UI back.
- Do not change PLC coordinates, offsets, laser power, ring-buffer protocol, pause, continue, or stop behavior.
- Do not add MQTT or web code.

---

### Task 1: Add a deterministic spatial path index

**Files:**
- Create: `src/DACDT_2026.App/CadPathHitIndex.cs`
- Modify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- `CadHitPath` owns one path ID and projected points.
- `CadPathHitIndex.Build(paths, cellSize)` creates a uniform-grid segment index.
- `TryFindNearest(point, radius, out pathId)` returns the nearest eligible path.
- `TryGetPathPoints(pathId, out points)` supports the one-path selection overlay.

- [ ] **Step 1: Write failing tests**

Add tests for a nearest horizontal segment, a miss outside the radius, a deterministic tie, and a large set where only the correct nearby path is returned.

- [ ] **Step 2: Build tests and verify RED**

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj' /t:Build /p:Configuration=Debug /v:minimal
```

Expected: `CS0246` for the missing spatial-index types.

- [ ] **Step 3: Implement the minimum uniform-grid index**

Index segment bounding boxes by integer grid coordinates. Deduplicate candidate segment IDs during a query, calculate exact point-to-segment squared distance, and use path ID for equal-distance tie breaking.

- [ ] **Step 4: Run tests and verify GREEN**

```powershell
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

Expected: `All tests passed.`

### Task 2: Remove per-path WPF shapes and wire direct hit testing

**Files:**
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml`
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml.cs`
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- `WpfUiState.CadPathHitIndex` exposes the current immutable index.
- `WpfUiState.CadSelectionOverlayGeometry` and its stroke expose immediate single-path feedback.
- `DxfRunView` queries the index from mouse/touch content coordinates and invokes the existing toggle command with a path ID.

- [ ] **Step 1: Write failing source-contract and behavior tests**

Verify the XAML contains no `CadSelectionLayer`, no selectable `Polyline`, and contains one selection overlay `Path`. Verify the view calls `TryFindNearest`, scales the 12-DIP hit radius, and does not walk `OriginalSource`.

- [ ] **Step 2: Run tests and verify RED**

Expected: assertions fail while the transparent selection layer still exists.

- [ ] **Step 3: Build the hit index with preview state**

Replace `BuildCadPrimitiveLines` output with projected `CadHitPath` data used to create the index. Publish that index alongside combined preview geometry without creating `CadPrimitiveViewModel` objects.

- [ ] **Step 4: Replace view selection handling**

Remove the selection `ItemsControl`. Handle mouse and one-finger tap directly at `CadContent`, convert the fixed screen hit radius to content units, query the index, show one overlay, and execute the existing toggle command.

- [ ] **Step 5: Run tests and verify GREEN**

Expected: `All tests passed.`

### Task 3: Cache CAD rendering only while interacting

**Files:**
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml.cs`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- `BeginCadInteractionRendering()` installs one `BitmapCache`.
- `EndCadInteractionRendering()` removes the cache and restores sharp vector rendering.
- Mouse wheel uses a short dispatcher timer; pan/pinch use gesture start/end.

- [ ] **Step 1: Write failing source-contract tests**

Assert that interaction start assigns `CadContent.CacheMode`, interaction end clears it, and mouse/touch pan and pinch call the lifecycle methods.

- [ ] **Step 2: Run tests and verify RED**

Expected: interaction-cache assertions fail.

- [ ] **Step 3: Implement interaction cache lifecycle**

Use one reusable `BitmapCache` with `EnableClearType = false` and `RenderAtScale = 1`. Never leave it active after the wheel idle timer or pointer gesture completes.

- [ ] **Step 4: Run tests and verify GREEN**

Expected: `All tests passed.`

### Task 4: Add versioned latest-wins compilation

**Files:**
- Create: `src/DACDT_2026.App/CadProgramCompilationState.cs`
- Modify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- `MarkDirty()` increments and returns the requested version.
- `IsCurrent(version)` and `TryPublish(version)` prevent stale publication.
- `PublishedVersion` identifies the rows currently stored in `processRows`.

- [ ] **Step 1: Write failing tests**

Cover dirty state, successful current publication, stale-result rejection after a newer request, and initial/current behavior.

- [ ] **Step 2: Build tests and verify RED**

Expected: `CS0246` for `CadProgramCompilationState`.

- [ ] **Step 3: Implement the minimum thread-safe state object**

Use `Interlocked` and `Volatile`; do not put WPF or PLC dependencies in this class.

- [ ] **Step 4: Run tests and verify GREEN**

Expected: `All tests passed.`

### Task 5: Integrate cancellable compilation and execution gates

**Files:**
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs`
- Modify: `src/DACDT_2026.App/Form1.PlcControl.cs`
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- `ScheduleCadProgramCompilation(document, version)` debounces selection changes.
- `CompileCadProgramAsync(document, version, cancellationToken)` compiles in the background and publishes only a current result.
- `EnsureCadProgramCurrentAsync()` cancels the debounce and waits for current rows.
- `BuildDxfProcessRows` and connected-path loops accept and observe a cancellation token.

- [ ] **Step 1: Write failing integration-contract tests**

Verify selection marks the program dirty without awaiting a rebuild, stale versions are guarded before publishing rows, and mixed RUN, send, and QD75 export call `EnsureCadProgramCurrentAsync`.

- [ ] **Step 2: Run tests and verify RED**

Expected: latest-wins integration assertions fail.

- [ ] **Step 3: Implement debounce, cancellation, and stale-result rejection**

Keep one cancellation source and one current compile task under a small lock. Cancel older work, delay the normal background compile, check cancellation in large loops, and atomically publish only the active document/current version.

- [ ] **Step 4: Integrate safety gates**

Call `EnsureCadProgramCurrentAsync` before DXF `processRows` are consumed by RUN/send/export. Preserve the G-code path unchanged. Mark the CAD program dirty after Test Area replaces `processRows`.

- [ ] **Step 5: Run tests and verify GREEN**

Expected: `All tests passed.`

### Task 6: Full verification and performance regression

**Files:**
- Verify only.

- [ ] **Step 1: Build and run all tests**

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj' /t:Rebuild /p:Configuration=Debug /v:minimal
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

Expected: `All tests passed.`

- [ ] **Step 2: Build the x86 Release application**

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'src\DACDT_2026.App\DACDT_2026.csproj' /t:Rebuild /p:Configuration=Release /p:Platform=x86 /v:minimal
```

Expected: zero errors and zero warnings.

- [ ] **Step 3: Measure the supplied large DXF**

Confirm there are no per-path WPF elements, hit-index creation is bounded by preview data, and repeated nearest-path queries stay well below one frame budget.

- [ ] **Step 4: Check the final diff**

```powershell
git -c core.safecrlf=false diff --check
```

Expected: no whitespace errors.
