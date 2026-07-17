# Tablet CAD Gesture and Selection Performance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make tablet CAD path selection immediate and make pinch zoom stable without changing PLC run or coordinate-write behavior.

**Architecture:** A pure touch-session object owns two-finger identity and produces one stable pinch frame from the latest positions. The DXF view consumes that state at render cadence, while the DXF handler applies selection color immediately and coalesces expensive process-table rebuilding and MQTT publication by selection version.

**Tech Stack:** C# 7.3, .NET Framework 4.8 WPF, existing manual C# test executable, MQTTnet.

## Global Constraints

- Preserve mouse click, wheel zoom, and mouse double-click reset.
- Preserve existing one-finger pan and connected-path Cut/Engrave toggle semantics.
- Do not change PLC coordinate, power, pause, continue, or RUN logic.
- Keep all operator-facing UI text in English.
- Build and verify both `DACDT_2026.Tests.exe` and x86 Release.

---

### Task 1: Add deterministic touch-session state

**Files:**
- Create: `src/DACDT_2026.App/CadTouchGestureSession.cs`
- Modify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- Produces `CadTouchGestureSession.BeginTouch`, `UpdateTouch`, `EndTouch`, `TryTakePinchFrame`, and `Reset`.
- Consumes `System.Windows.Point` and keeps the first two active touch IDs fixed for one pinch session.
- The DXF view will use `CadPinchFrame` from this task in Task 2.

- [ ] **Step 1: Write the failing tests**

```csharp
private static void CadTouchSessionKeepsFixedPinchPairAndResetsOnFingerRelease()
{
    var session = new CadTouchGestureSession();
    session.BeginTouch(11, new Point(10, 10));
    session.BeginTouch(22, new Point(30, 10));
    session.UpdateTouch(22, new Point(50, 10));

    AssertTrue(session.TryTakePinchFrame(out CadPinchFrame frame), "A two-finger move must produce one pinch frame.");
    AssertEqual("11", frame.PrimaryTouchId.ToString(), "The first touch must remain the primary pinch touch.");
    AssertEqual("22", frame.SecondaryTouchId.ToString(), "The second touch must remain the secondary pinch touch.");
    AssertTrue(!session.TryTakePinchFrame(out frame), "A frame must be consumed once rather than applied repeatedly.");

    session.EndTouch(11);
    AssertTrue(!session.IsPinching, "Releasing either pinch finger must end the pinch session.");
}
```

- [ ] **Step 2: Run the test executable and verify RED**

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj' /t:Build /p:Configuration=Debug /v:minimal
```

Expected: `CS0246` for the missing `CadTouchGestureSession` type.

- [ ] **Step 3: Add the minimal touch-session implementation**

```csharp
public bool TryTakePinchFrame(out CadPinchFrame frame)
{
    if (!pinchFramePending || primaryTouchId < 0 || secondaryTouchId < 0)
    {
        frame = default(CadPinchFrame);
        return false;
    }

    pinchFramePending = false;
    frame = new CadPinchFrame(primaryTouchId, secondaryTouchId, previousPrimary, previousSecondary, primary, secondary);
    previousPrimary = primary;
    previousSecondary = secondary;
    return true;
}
```

`EndTouch` must call `Reset()` whenever the released ID is either pinch ID. Add the new source file to both project files.

- [ ] **Step 4: Run the test executable and verify GREEN**

Run:

```powershell
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

Expected: `All tests passed.`

### Task 2: Stabilize tablet pinch and enlarge the touch target

**Files:**
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml`
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml.cs`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- Consumes `CadTouchGestureSession` and `CadPinchFrame` from Task 1.
- Uses existing `ApplyCadPinchTransform`, `CadZoomTransform`, and `CadPanTransform`.
- Produces stable one-finger pan and two-finger pinch behavior for the CAD viewport.

- [ ] **Step 1: Write source-contract tests**

```csharp
AssertTrue(dxfXaml.Contains("<Binding Source=\"24\"/>"), "Tablet CAD hit targets must use a 24 DIP stroke.");
AssertTrue(viewCode.Contains("CadTouchGestureSession"), "CAD touch handling must use one deterministic touch session.");
AssertTrue(viewCode.Contains("CompositionTarget.Rendering"), "Pinch transforms must be coalesced to render cadence.");
AssertTrue(viewCode.Contains("e.StylusDevice != null"), "Promoted touch mouse events must not trigger CAD mouse commands.");
```

- [ ] **Step 2: Run the test executable and verify RED**

Run:

```powershell
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

Expected: a tablet CAD touch assertion failure.

- [ ] **Step 3: Implement the view changes**

```csharp
private void ApplyPendingCadPinchFrame(object sender, EventArgs e)
{
    if (!touchSession.TryTakePinchFrame(out CadPinchFrame frame))
        return;

    ApplyCadPinchTransform(
        Distance(frame.PreviousPrimary, frame.PreviousSecondary),
        Distance(frame.Primary, frame.Secondary),
        Midpoint(frame.PreviousPrimary, frame.PreviousSecondary),
        Midpoint(frame.Primary, frame.Secondary));
}
```

Subscribe only while a pinch session is active and unsubscribe when either finger is released or touch capture is lost. In all CAD mouse event handlers, return when `e.StylusDevice != null` or a touch session is active. Replace the transparent selection stroke source from `10` to `24` in XAML.

- [ ] **Step 4: Run the test executable and verify GREEN**

Run:

```powershell
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

Expected: `All tests passed.`

### Task 3: Make path color immediate and defer heavy refresh work

**Files:**
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- `WpfUiState.UpdateCadPathStroke(int pathId, bool isCut)` changes only existing path view models.
- `Form1.ScheduleCadPathSelectionRefreshAsync(int selectionVersion, CadLoadResult selectedDocument)` rebuilds rows only for the newest touch selection.
- MQTT publication is scheduled after the deferred rebuild and is never awaited by the tap command.

- [ ] **Step 1: Write the failing source-contract tests**

```csharp
AssertTrue(stateSource.Contains("public void UpdateCadPathStroke(int pathId, bool isCut)"), "The UI state must update one selected CAD path without rebuilding the canvas.");
AssertTrue(handler.Contains("Interlocked.Increment(ref cadPathSelectionVersion)"), "Each CAD tap must invalidate an older deferred refresh.");
AssertTrue(handler.Contains("await Task.Delay(CadPathSelectionRefreshDelayMs)"), "Repeated taps must be coalesced before rebuilding process rows.");
AssertTrue(!handler.Contains("await PublishAllMqttAsync();"), "A path tap must not wait for MQTT publication.");
```

- [ ] **Step 2: Run the test executable and verify RED**

Run:

```powershell
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

Expected: a CAD selection performance assertion failure.

- [ ] **Step 3: Implement the minimal deferred refresh**

```csharp
bool isCut = selectedDocument.Primitives
    .Any(primitive => primitive.PathId == pathId && string.Equals(primitive.ProcessKind, EngraveCutProcessComposer.CutKind, StringComparison.OrdinalIgnoreCase));
ui.UpdateCadPathStroke(pathId, isCut);
int selectionVersion = Interlocked.Increment(ref cadPathSelectionVersion);
_ = ScheduleCadPathSelectionRefreshAsync(selectionVersion, selectedDocument);
```

`ScheduleCadPathSelectionRefreshAsync` waits 120 ms, checks both the document reference and newest version, then performs `RebuildMixedEngraveCutProgramAsync` and `PushDxfStateAsync`. It starts MQTT publication without awaiting it. `CadPrimitiveViewModel` must implement property-change notification for `Stroke` so the selected outline changes color immediately.

- [ ] **Step 4: Run the test executable and verify GREEN**

Run:

```powershell
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

Expected: `All tests passed.`

### Task 4: Full verification

**Files:**
- Verify only.

- [ ] **Step 1: Run full tests**

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj' /t:Rebuild /p:Configuration=Debug /v:minimal
& '.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

Expected: `All tests passed.`

- [ ] **Step 2: Build the x86 Release application**

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'src\DACDT_2026.App\DACDT_2026.csproj' /t:Rebuild /p:Configuration=Release /p:Platform=x86 /v:minimal
```

Expected: `DACDT_2026 -> ...\\bin\\x86\\Release\\DACDT_2026.exe` with no errors or warnings.

- [ ] **Step 3: Check the final diff**

```powershell
git -c core.safecrlf=false diff --check
```

Expected: no whitespace errors.
