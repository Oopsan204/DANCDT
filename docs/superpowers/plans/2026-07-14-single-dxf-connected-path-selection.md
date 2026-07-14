# Single DXF Connected-Path Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the two-file DXF Engrave/Cut import workflow with one DXF import whose connected contours can be toggled between Engrave and Cut by clicking them in CAD Preview.

**Architecture:** Keep one master `CadLoadResult` containing every DXF primitive. Assign a stable `PathId` after the existing endpoint-based normalization, and share the existing 0.001 mm path grouping with the compiler and preview. Build temporary Engrave and Cut document views from the master, then reuse the current mixed `processRows` and PLC run path unchanged. Render each connected path as a selectable WPF polyline with a transparent hit stroke over its colored visible stroke.

**Tech Stack:** .NET Framework 4.8, WPF, C# 7.3, existing `netDxf` parser, existing console test executable, MSBuild.

## Global Constraints

- Import exactly one DXF for the mixed Engrave/Cut workflow.
- Every connected contour starts as Khac; clicking it toggles the whole contour to Cat; clicking again restores Khac.
- Connected endpoints use the existing 0.001 mm grouping precision.
- Keep one `processRows` collection with Engrave rows before Cut rows.
- Keep one final End row and remove the intermediate Engrave home/end row when Cut rows follow.
- Power changes only at the existing Engrave-to-Cut boundary.
- Do not change PLC coordinate layout, destination addresses, M-code generation, coordinate formatting, ring-buffer transfer, or the Set Power path.
- Do not change G-code, camera, or WebRTC behavior.
- Use existing dependencies; do not add a geometry or UI package.
- Follow TDD: write each behavior test, run it red, then add the minimum implementation and run green.

## File Map

- Create: `src/DACDT_2026.App/CadPathSelection.cs`
- Modify: `src/DACDT_2026.App/CadDocumentService.cs`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs`
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs`
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml`
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml.cs`
- Modify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

---

### Task 1: Add Failing Path Selection Tests

**Files:**
- Modify: `tests/DACDT_2026.Tests/Program.cs`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`

**Interfaces:**
- Consumes: the planned `CadPathSelection.GroupConnectedPaths`, `AssignPathIds`, and `ToggleProcessKind` APIs.
- Produces: a red executable test contract for connected contours and whole-contour toggling.

- [ ] **Step 1: Add the existing CAD model references to the test project.**

Add `System.Drawing` and the existing `netDxf` package reference:

```xml
<Reference Include="System.Drawing" />
<Reference Include="netDxf, Version=2023.11.10.0, Culture=neutral, PublicKeyToken=618c63290969e781, processorArchitecture=MSIL">
  <HintPath>..\\..\\packages\\netDxf.2023.11.10\\lib\\net48\\netDxf.dll</HintPath>
</Reference>
```

Link the existing CAD model and fallback parser:

```xml
<Compile Include="..\\..\\src\\DACDT_2026.App\\CadDocumentService.cs">
  <Link>CadDocumentService.cs</Link>
</Compile>
<Compile Include="..\\..\\src\\DACDT_2026.App\\SimpleDxfParser.cs">
  <Link>SimpleDxfParser.cs</Link>
</Compile>
```

- [ ] **Step 2: Register three red tests in `Program.Main`.**

```csharp
CadPathSelectionGroupsConnectedLineSegments();
CadPathSelectionTogglesEveryPrimitiveInSelectedPath();
CadPathSelectionToggleTwiceRestoresEngrave();
```

- [ ] **Step 3: Add the test bodies and a small line factory.**

```csharp
private static void CadPathSelectionGroupsConnectedLineSegments()
{
    var first = NewCadLine(0, 0, 10, 0);
    var second = NewCadLine(10, 0, 10, 10);
    var separate = NewCadLine(100, 100, 110, 100);

    var paths = CadPathSelection.GroupConnectedPaths(
        new List<CadDocumentService.CadPrimitiveData> { first, second, separate });

    AssertEqual("2", paths.Count.ToString(), "Connected segments should form one path and disconnected geometry another.");
    AssertEqual("2", CadPathSelection.AssignPathIds(paths).ToString(), "Two groups should receive two path ids.");
    AssertEqual("0", first.PathId.ToString(), "The first chain should receive path id zero.");
    AssertEqual("0", second.PathId.ToString(), "Connected segments should share a path id.");
    AssertEqual("1", separate.PathId.ToString(), "Disconnected geometry should receive a different path id.");
}

private static void CadPathSelectionTogglesEveryPrimitiveInSelectedPath()
{
    var first = NewCadLine(0, 0, 10, 0);
    var second = NewCadLine(10, 0, 10, 10);
    var separate = NewCadLine(100, 100, 110, 100);
    first.PathId = 4;
    second.PathId = 4;
    separate.PathId = 5;

    bool changed = CadPathSelection.ToggleProcessKind(
        new[] { first, second, separate },
        4,
        EngraveCutProcessComposer.EngraveKind,
        EngraveCutProcessComposer.CutKind);

    AssertTrue(changed, "A valid path id should toggle.");
    AssertEqual(EngraveCutProcessComposer.CutKind, first.ProcessKind, "The first segment should become Cut.");
    AssertEqual(EngraveCutProcessComposer.CutKind, second.ProcessKind, "Every selected segment should become Cut.");
    AssertEqual(EngraveCutProcessComposer.EngraveKind, separate.ProcessKind, "An unrelated contour must not change.");
}

private static void CadPathSelectionToggleTwiceRestoresEngrave()
{
    var first = NewCadLine(0, 0, 10, 0);
    first.PathId = 7;
    first.ProcessKind = EngraveCutProcessComposer.EngraveKind;

    CadPathSelection.ToggleProcessKind(
        new[] { first }, 7,
        EngraveCutProcessComposer.EngraveKind,
        EngraveCutProcessComposer.CutKind);
    CadPathSelection.ToggleProcessKind(
        new[] { first }, 7,
        EngraveCutProcessComposer.EngraveKind,
        EngraveCutProcessComposer.CutKind);

    AssertEqual(EngraveCutProcessComposer.EngraveKind, first.ProcessKind, "Two toggles should restore Engrave.");
}

private static CadDocumentService.CadPrimitiveData NewCadLine(double x1, double y1, double x2, double y2)
{
    return new CadDocumentService.CadPrimitiveData
    {
        SourceType = "Line",
        Points = new List<CadDocumentService.CadCoordinate>
        {
            new CadDocumentService.CadCoordinate(x1, y1),
            new CadDocumentService.CadCoordinate(x2, y2)
        },
        ProcessKind = EngraveCutProcessComposer.EngraveKind,
        PathId = -1
    };
}
```

- [ ] **Step 4: Run the test build and verify the intentional red result.**

Run:

```powershell
& 'C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe' tests\\DACDT_2026.Tests\\DACDT_2026.Tests.csproj /p:Configuration=Debug /v:minimal
```

Expected: compilation fails because `CadPathSelection` and `PathId` do not exist yet. This is the expected missing-feature failure.

- [ ] **Step 5: Commit the red test contract.**

```powershell
git add tests\\DACDT_2026.Tests\\Program.cs tests\\DACDT_2026.Tests\\DACDT_2026.Tests.csproj
git commit -m "test: define single DXF path selection contract"
```

### Task 2: Implement Shared Grouping, Path IDs, And Toggle

**Files:**
- Create: `src/DACDT_2026.App/CadPathSelection.cs`
- Modify: `src/DACDT_2026.App/CadDocumentService.cs:278-290`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs:2233-2366`
- Modify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`

**Interfaces:**
- Consumes: `CadPrimitiveData` and the current endpoint grouping algorithm.
- Produces: `CadPathSelection.GroupConnectedPaths`, `AssignPathIds`, and `ToggleProcessKind`.

- [ ] **Step 1: Add the stable id to `CadPrimitiveData`.**

```csharp
public int PathId { get; set; } = -1;
```

- [ ] **Step 2: Extract the existing grouping method into `CadPathSelection.GroupConnectedPaths`.**

Move the body of `GetConnectedPathsFromCad` at `Form1.DxfHandler.cs:2233-2366` to the new static class without changing behavior. Preserve the existing endpoint key format, 0.001 mm precision, start/end maps, seed order, tail/head extension, point reversal, and arc clockwise flip. The public declaration is:

```csharp
public static List<List<CadDocumentService.CadPrimitiveData>> GroupConnectedPaths(
    List<CadDocumentService.CadPrimitiveData> primitives,
    bool isGcode = false)
```

Add the two small operations below to the same class:

```csharp
public static int AssignPathIds(
    IEnumerable<List<CadDocumentService.CadPrimitiveData>> paths)
{
    if (paths == null)
        return 0;

    int pathId = 0;
    foreach (var path in paths)
    {
        if (path != null)
        {
            foreach (var primitive in path)
            {
                if (primitive != null)
                    primitive.PathId = pathId;
            }
        }
        pathId++;
    }
    return pathId;
}

public static bool ToggleProcessKind(
    IEnumerable<CadDocumentService.CadPrimitiveData> primitives,
    int pathId,
    string engraveKind,
    string cutKind)
{
    if (primitives == null || pathId < 0)
        return false;

    var selected = primitives
        .Where(primitive => primitive != null && primitive.PathId == pathId)
        .ToList();
    if (selected.Count == 0)
        return false;

    bool switchToCut = selected.Any(primitive =>
        !string.Equals(primitive.ProcessKind, cutKind, StringComparison.OrdinalIgnoreCase));
    string nextKind = switchToCut ? cutKind : engraveKind;

    foreach (var primitive in selected)
        primitive.ProcessKind = nextKind;
    return true;
}
```

- [ ] **Step 3: Keep existing Form1 callers stable.**

Replace the old private method body with this delegate:

```csharp
private List<List<CadDocumentService.CadPrimitiveData>> GetConnectedPathsFromCad(
    List<CadDocumentService.CadPrimitiveData> primitives,
    bool isGcode = false)
    => CadPathSelection.GroupConnectedPaths(primitives, isGcode);
```

Update `NormalizeCadDocumentPaths` to assign ids while flattening:

```csharp
var paths = GetConnectedPathsFromCad(document.Primitives, isGcode);
CadPathSelection.AssignPathIds(paths);
document.Primitives.Clear();
foreach (var path in paths)
    document.Primitives.AddRange(path);
```

- [ ] **Step 4: Register the new source in both projects.**

```xml
<Compile Include="CadPathSelection.cs" />
```

For the test project:

```xml
<Compile Include="..\\..\\src\\DACDT_2026.App\\CadPathSelection.cs">
  <Link>CadPathSelection.cs</Link>
</Compile>
```

- [ ] **Step 5: Run focused tests and verify green.**

```powershell
& 'C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe' tests\\DACDT_2026.Tests\\DACDT_2026.Tests.csproj /p:Configuration=Debug /v:minimal
tests\\DACDT_2026.Tests\\bin\\Debug\\DACDT_2026.Tests.exe
```

Expected: `All tests passed.`

- [ ] **Step 6: Commit the shared path logic.**

```powershell
git add src\\DACDT_2026.App\\CadPathSelection.cs src\\DACDT_2026.App\\CadDocumentService.cs src\\DACDT_2026.App\\Form1.DxfHandler.cs src\\DACDT_2026.App\\DACDT_2026.csproj tests\\DACDT_2026.Tests\\DACDT_2026.Tests.csproj
git commit -m "feat: add connected DXF path selection model"
```

### Task 3: Convert Import And Compilation To One Master DXF

**Files:**
- Modify: `src/DACDT_2026.App/Form1.cs:80-84,230-245`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs:212-524`
- Modify: `src/DACDT_2026.App/WpfUiState.cs:190-200`

**Interfaces:**
- Consumes: `CadPathSelection`, the master `activeCadDocument`, and `BuildDxfRowsForProcessDocument`.
- Produces: one import command, one master document, temporary filtered process views, and the existing mixed row output.

- [ ] **Step 1: Replace the two import command properties.**

In `WpfUiState`:

```csharp
public ICommand ImportDxfCommand { get; set; }
public ICommand ToggleCadPathCommand { get; set; }
```

In `ConfigureCommands`:

```csharp
ui.ImportDxfCommand = new RelayCommand(HandleImportDxfAsync);
ui.ToggleCadPathCommand = new RelayCommand(p => HandleToggleCadPathAsync(ToInt(p, -1)));
```

Remove the old Engrave/Cut command registrations after the XAML is updated.

- [ ] **Step 2: Add a temporary filtered document builder.**

```csharp
private CadDocumentService.CadLoadResult CreateProcessDocumentForKind(
    CadDocumentService.CadLoadResult source,
    string processKind)
{
    if (source?.Primitives == null)
        return null;

    var primitives = source.Primitives
        .Where(primitive => primitive != null
            && string.Equals(primitive.ProcessKind, processKind, StringComparison.OrdinalIgnoreCase))
        .Select(CloneCadPrimitiveForUi)
        .Where(primitive => primitive != null)
        .ToList();
    if (primitives.Count == 0)
        return null;

    var points = RebuildPointRowsForDisplay(primitives);
    return new CadDocumentService.CadLoadResult
    {
        FilePath = source.FilePath,
        DirectoryPath = source.DirectoryPath,
        FileName = source.FileName,
        Primitives = primitives,
        Points = points,
        Bounds = BuildDisplayBounds(primitives, points)
    };
}
```

- [ ] **Step 3: Rebuild from the master document without replacing it.**

At the start of `RebuildMixedEngraveCutProgramAsync`:

```csharp
activeEngraveCadDocument = CreateProcessDocumentForKind(
    activeCadDocument, EngraveCutProcessComposer.EngraveKind);
activeCutCadDocument = CreateProcessDocumentForKind(
    activeCadDocument, EngraveCutProcessComposer.CutKind);
```

Keep the current row-building, home-row removal, row ordering, and motion normalization. Delete only `activeCadDocument = MergeEngraveCutDocuments();`. Keep:

```csharp
activeDocumentKind = "DXF";
isMixedEngraveCutProgram = activeCadDocument != null;
```

Delete `MergeEngraveCutDocuments` after confirming it has no remaining callers.

- [ ] **Step 4: Replace the old two-file handler with `HandleImportDxfAsync`.**

Keep the current gate, dialog, progress, notification, scan-limit, state-publish, MQTT, error, and finally-release pattern. After loading, normalize, assign ids, default to Engrave, and keep the loaded document as the master:

```csharp
NormalizeCadDocumentPaths(loadedDoc, isGcode: false);
TagCadDocumentProcessKind(loadedDoc, EngraveCutProcessComposer.EngraveKind);
activeCadDocument = loadedDoc;
activeEngraveCadDocument = null;
activeCutCadDocument = null;
activeDocumentKind = "DXF";
isMixedEngraveCutProgram = true;
await RebuildMixedEngraveCutProgramAsync();
```

Remove `HandleImportEngraveDxfAsync`, `HandleImportCutDxfAsync`, and `HandleImportDxfForProcessKindAsync` after no references remain. Do not create a second DXF dialog or a second PLC transfer path.

- [ ] **Step 5: Ensure clearing a file clears selectable paths.**

Update `ClearLoadedFileState` to clear the master, temporary views, rows, and `ui.CadPrimitives`. Keep the existing G-code and generic open-file behavior otherwise unchanged.

Replace the duplicated endpoint-grouping block in `HandleOpenDxfAsync` with `NormalizeCadDocumentPaths(loadedDoc, isGcode)`. This preserves the existing G-code grouping while assigning stable ids to every normalized CAD document; it does not route G-code through mixed Engrave/Cut compilation.

- [ ] **Step 6: Run all current tests.**

```powershell
& 'C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe' tests\\DACDT_2026.Tests\\DACDT_2026.Tests.csproj /p:Configuration=Debug /v:minimal
tests\\DACDT_2026.Tests\\bin\\Debug\\DACDT_2026.Tests.exe
```

Expected: `All tests passed.`

- [ ] **Step 7: Commit the single-document compiler flow.**

```powershell
git add src\\DACDT_2026.App\\Form1.cs src\\DACDT_2026.App\\Form1.DxfHandler.cs src\\DACDT_2026.App\\WpfUiState.cs
git commit -m "feat: compile one DXF into engrave and cut views"
```

### Task 4: Publish And Render Selectable Colored Paths

**Files:**
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs:628-688,957-976,1219-1248`
- Modify: `src/DACDT_2026.App/WpfUiState.cs:162,1214-1219`
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml:181-212`
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml.cs:85-101`

**Interfaces:**
- Consumes: normalized primitives with `PathId` and `ProcessKind`.
- Produces: `CadPrimitives` entries representing complete colored connected paths.

- [ ] **Step 1: Preserve `PathId` in `CloneCadPrimitiveForUi`.**

Add:

```csharp
PathId = primitive.PathId
```

- [ ] **Step 2: Extend the existing view model.**

Add to `CadPrimitiveViewModel`:

```csharp
public int PathId { get; set; }
```

- [ ] **Step 3: Change `BuildCadPrimitiveLines` to group by path.**

Keep the existing 50000-item limit, rapid filtering, projection, and frozen `PointCollection`. Group by `PathId`, append every primitive's points in group order, and use OrangeRed when any member is Cut, otherwise DeepSkyBlue:

```csharp
var groups = doc.Primitives
    .Take(50000)
    .Where(primitive => primitive != null
        && primitive.Points != null
        && primitive.Points.Count >= 2
        && !IsRapidPrimitive(primitive))
    .GroupBy(primitive => primitive.PathId)
    .OrderBy(group => group.Key);

foreach (var group in groups)
{
    var points = new PointCollection();
    foreach (var primitive in group)
        foreach (var point in primitive.Points)
            points.Add(projection.Project(point.X, point.Y));
    points.Freeze();

    bool isCut = group.Any(primitive =>
        string.Equals(primitive.ProcessKind, EngraveCutProcessComposer.CutKind, StringComparison.OrdinalIgnoreCase));

    lines.Add(new CadPrimitiveViewModel
    {
        PathId = group.Key,
        Points = points,
        Stroke = isCut ? Brushes.OrangeRed : Brushes.DeepSkyBlue,
        StrokeThickness = 0.65
    });
}
```

For a legacy primitive with `PathId < 0`, use a unique fallback key so it remains visible. Normalized DXF imports always have real ids.

- [ ] **Step 4: Publish the collection from `PushDxfStateAsync`.**

Build `cadPrimitiveLines = BuildCadPrimitiveLines(snapDoc, projection)`, include it in the background model, and replace the current clear-only operation with:

```csharp
ReplaceCollection(ui.CadPrimitives, model.cadPrimitiveLines);
```

Do not change row serialization, MQTT fields, axis data, limits, tracking points, or the existing geometry used by non-WPF consumers.

- [ ] **Step 5: Replace the three static preview paths with an interactive `ItemsControl`.**

Use this template inside the existing transformed `CadContent` canvas:

```xml
<ItemsControl ItemsSource="{Binding CadPrimitives}">
  <ItemsControl.ItemsPanel>
    <ItemsPanelTemplate><Canvas /></ItemsPanelTemplate>
  </ItemsControl.ItemsPanel>
  <ItemsControl.ItemTemplate>
    <DataTemplate>
      <Grid>
        <Polyline Points="{Binding Points}"
                  Stroke="Transparent"
                  StrokeThickness="10"
                  StrokeLineJoin="Round"
                  StrokeStartLineCap="Round"
                  StrokeEndLineCap="Round"
                  Cursor="Hand"
                  MouseLeftButtonDown="SelectableCadPath_MouseLeftButtonDown" />
        <Polyline Points="{Binding Points}"
                  Stroke="{Binding Stroke}"
                  StrokeThickness="1"
                  StrokeLineJoin="Round"
                  StrokeStartLineCap="Round"
                  StrokeEndLineCap="Round"
                  IsHitTestVisible="False" />
      </Grid>
    </DataTemplate>
  </ItemsControl.ItemTemplate>
</ItemsControl>
```

Remove the old static `CadPreviewGeometry`, `CadEngravePreviewGeometry`, and `CadCutPreviewGeometry` Path elements from the WPF preview so lines are not drawn twice. Keep other overlays hit-test disabled.

- [ ] **Step 6: Route path clicks while preserving pan and reset.**

Add:

```csharp
private void SelectableCadPath_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (e.ClickCount >= 2)
    {
        ResetCadView();
        e.Handled = true;
        return;
    }

    var item = (sender as FrameworkElement)?.DataContext as CadPrimitiveViewModel;
    var state = DataContext as WpfUiState;
    if (item == null || state?.ToggleCadPathCommand == null)
    {
        e.Handled = true;
        return;
    }

    if (state.ToggleCadPathCommand.CanExecute(item.PathId))
        state.ToggleCadPathCommand.Execute(item.PathId);

    e.Handled = true;
}
```

An empty-background click must continue to reach the existing pan handler. A double-click on a path must reset directly.

- [ ] **Step 7: Commit the selectable preview.**

```powershell
git add src\\DACDT_2026.App\\Form1.StatePublisher.cs src\\DACDT_2026.App\\WpfUiState.cs src\\DACDT_2026.App\\Views\\DxfRunView.xaml src\\DACDT_2026.App\\Views\\DxfRunView.xaml.cs
git commit -m "feat: make DXF contours selectable in preview"
```

### Task 5: Guard The Toggle And Refresh The Full State

**Files:**
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs`

**Interfaces:**
- Consumes: path ids from `ToggleCadPathCommand`.
- Produces: guarded classification changes followed by process table and preview refresh.

- [ ] **Step 1: Add the guarded toggle handler.**

```csharp
private async Task HandleToggleCadPathAsync(int pathId)
{
    if (IsProgramRunning()
        || !string.Equals(activeDocumentKind, "DXF", StringComparison.OrdinalIgnoreCase)
        || activeCadDocument?.Primitives == null)
        return;

    if (!await cadLoadGate.WaitAsync(0))
        return;

    try
    {
        bool changed = CadPathSelection.ToggleProcessKind(
            activeCadDocument.Primitives,
            pathId,
            EngraveCutProcessComposer.EngraveKind,
            EngraveCutProcessComposer.CutKind);
        if (!changed)
            return;

        await RebuildMixedEngraveCutProgramAsync();
        await PushDxfStateAsync();
        await PublishAllMqttAsync();
    }
    finally
    {
        cadLoadGate.Release();
    }
}
```

This handler must not write laser power, coordinates, M-codes, or PLC registers.

- [ ] **Step 2: Preserve selections during settings changes.**

Keep settings updates routed to `RebuildMixedEngraveCutProgramAsync` when `isMixedEngraveCutProgram` is true. Do not re-tag the master document during a settings rebuild.

- [ ] **Step 3: Run the complete test executable.**

```powershell
& 'C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe' tests\\DACDT_2026.Tests\\DACDT_2026.Tests.csproj /p:Configuration=Debug /v:minimal
tests\\DACDT_2026.Tests\\bin\\Debug\\DACDT_2026.Tests.exe
```

Expected: `All tests passed.`

- [ ] **Step 4: Commit the guarded toggle flow.**

```powershell
git add src\\DACDT_2026.App\\Form1.cs src\\DACDT_2026.App\\Form1.DxfHandler.cs
git commit -m "feat: toggle DXF process kind from preview"
```

### Task 6: Build And Manually Verify The User Workflow

**Files:**
- No new files. Verify the files changed by Tasks 1-5.

- [ ] **Step 1: Check the diff boundary.**

```powershell
git diff --check
git status --short
git diff -- src\\DACDT_2026.App\\Form1.PlcControl.cs src\\DACDT_2026.App\\PLCCommunication.cs src\\DACDT_2026.App\\QD75BufferWriter.cs src\\DACDT_2026.App\\QD75RingBufferRunner.cs
```

Expected: no whitespace errors and no changes in the PLC transfer files.

- [ ] **Step 2: Build the actual x86 WPF target.**

```powershell
& 'C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe' src\\DACDT_2026.App\\DACDT_2026.csproj /p:Configuration=Debug /p:Platform=x86 /v:minimal
```

Expected: exit code 0 and a fresh `src\\DACDT_2026.App\\bin\\x86\\Debug\\DACDT_2026.exe`.

- [ ] **Step 3: Manually verify the preview and process table.**

Verify all of the following with a DXF containing a multi-segment contour and a separate contour:

- one `Import DXF` button is visible;
- every contour starts blue;
- clicking one segment changes the entire connected contour to orange-red;
- clicking another segment of that contour does not create a partial selection;
- clicking the contour again returns every segment to blue;
- disconnected contours toggle independently;
- empty-space drag still pans and double-click still resets;
- processRows shows Engrave rows first and Cut rows second;
- no intermediate End/home row appears between Engrave and Cut;
- final End remains after the last Cut row;
- changing Engrave/Cut settings preserves selected colors and classifications;
- clicking a path while RUN is active does not alter the process table.

- [ ] **Step 4: Perform final verification before claiming completion.**

Run the test executable, the x86 app build, `git diff --check`, and the focused no-PLC diff check again after any correction. Only report completion when all commands exit successfully and the manual checklist is observed.
