# Remove Geometry Data Table Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove the coordinate-only `Geometry Data` table and its data-building work while preserving CAD Preview, G-code Editor, and the PLC `Process Table`.

**Architecture:** Keep the existing WPF view and `WpfUiState` model structure. Remove only the Geometry Data visual surface, its lazy-scroll event branch, and the `BuildGeometryRows`/`SetGeometryRows` work from the CAD state publication pipeline. Do not alter CAD projection, CAD source data, process rows, or PLC execution behavior.

**Tech Stack:** .NET Framework WPF, XAML, C#, console-style regression tests, MSBuild x86 Release.

## Global Constraints

- Keep `CAD Preview`.
- Keep `G-code Editor`.
- Keep `Process Table` because it supports PLC execution.
- Do not change the current preview projection/fit behavior.
- Do not change full CAD data used for compilation or PLC transfer.

---

### Task 1: Add the regression contract

**Files:**
- Modify: `tests/DACDT_2026.Tests/Program.cs` near the existing WPF/XAML contract tests.

**Interfaces:**
- Consumes: `GetRepositoryPath`, `AssertTrue`, and the current `DxfRunView.xaml`/`Form1.StatePublisher.cs` source files.
- Produces: `GeometryDataTableIsRemovedWithoutRemovingCadAndGcodeViews()`.

- [ ] **Step 1: Write the failing test**

Add the method call in `Main()` after `WpfXamlUsesValidResourceAndGridSyntax();`, then add:

```csharp
private static void GeometryDataTableIsRemovedWithoutRemovingCadAndGcodeViews()
{
    string dxfRun = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
    string viewCode = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml.cs"));
    string publisher = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.StatePublisher.cs"));

    AssertTrue(!dxfRun.Contains("Geometry Data"), "DxfRunView must not show the Geometry Data panel.");
    AssertTrue(!dxfRun.Contains("GeometryDataGrid"), "DxfRunView must not declare the Geometry Data grid.");
    AssertTrue(!viewCode.Contains("LoadMoreGeometryRows"), "DxfRunView must not lazy-load coordinate rows.");
    AssertTrue(!publisher.Contains("BuildGeometryRows("), "CAD state publication must not build coordinate rows.");
    AssertTrue(!publisher.Contains("SetGeometryRows("), "CAD state publication must not publish coordinate rows.");
    AssertTrue(dxfRun.Contains("CAD Preview"), "DxfRunView must keep CAD Preview.");
    AssertTrue(dxfRun.Contains("G-code Editor"), "DxfRunView must keep G-code Editor.");
    AssertTrue(dxfRun.Contains("Process Table"), "DxfRunView must keep Process Table.");
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
dotnet msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug /v:minimal
.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected: FAIL because the current XAML contains `Geometry Data`/`GeometryDataGrid` and the publisher still calls `BuildGeometryRows`/`SetGeometryRows`.

### Task 2: Remove the Geometry Data UI and publication work

**Files:**
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml` by removing the `Geometry Data` panel only.
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml.cs` by removing the `GeometryDataGrid` lazy-scroll branch while retaining `ProcessTableGrid` loading.
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs` by removing geometry-row construction, anonymous-model `geometryRows`, and the `ui.SetGeometryRows` call.

**Interfaces:**
- Consumes: existing CAD preview collections, G-code editor binding, process rows, and PLC state publication.
- Produces: a DXF view with CAD Preview, G-code Editor, and Process Table only; CAD state publication no longer allocates coordinate-table rows.

- [ ] **Step 1: Remove the XAML panel**

Delete the `Border Grid.Row="1" Grid.Column="1"` block whose title is `Geometry Data`, including the `DataGrid x:Name="GeometryDataGrid"` and all coordinate columns. Leave the `Process Table` border at `Grid.Row="1" Grid.Column="0"` unchanged.

- [ ] **Step 2: Remove only the Geometry Data scroll branch**

In `LazyTable_ScrollChanged`, retain:

```csharp
if (ReferenceEquals(sender, ProcessTableGrid))
    state.LoadMoreProcessRows();
```

Remove the `else if (ReferenceEquals(sender, GeometryDataGrid))` branch. Keep the shared near-end scroll test and method because Process Table still uses it.

- [ ] **Step 3: Stop building coordinate rows**

In `PushDxfStateAsync`, remove:

```csharp
var geometryRows = BuildGeometryRows(snapDoc);
```

Remove `geometryRows` from the anonymous return object and remove:

```csharp
ui.SetGeometryRows(model.geometryRows);
```

Do not remove `CadPoints`, `CadPrimitives`, `CadPreviewImage`, preview geometries, process rows, or PLC-related state.

- [ ] **Step 4: Delete dead coordinate-table builders only after the caller is gone**

Remove `BuildGeometryRows`, `CreateGeometryRow`, `GetGeometryLineType`, `MakeGeometryPointKey`, and `FormatGeometryNumber` from `Form1.StatePublisher.cs` if no remaining caller exists. Do not remove `WpfUiState.GeometryRows` or its methods unless compilation proves they are unused and their removal is still isolated to this request.

- [ ] **Step 5: Run the regression test to verify it passes**

Run:

```powershell
dotnet msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug /v:minimal
.\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected: `All tests passed.`

- [ ] **Step 6: Commit the focused implementation**

```powershell
git add tests/DACDT_2026.Tests/Program.cs src/DACDT_2026.App/Views/DxfRunView.xaml src/DACDT_2026.App/Views/DxfRunView.xaml.cs src/DACDT_2026.App/Form1.StatePublisher.cs
git commit -m "refactor: remove geometry data table"
```

### Task 3: Verify the WPF build and scope

**Files:**
- No source changes expected.

- [ ] **Step 1: Build the application**

Run:

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild src\DACDT_2026.App\DACDT_2026.csproj /t:Rebuild /p:Configuration=Release /p:Platform=x86 /v:minimal
```

Expected: exit code `0` and `src\DACDT_2026.App\bin\x86\Release\DACDT_2026.exe` is produced.

- [ ] **Step 2: Run the final checks**

Run:

```powershell
git diff --check
git status --short
```

Expected: no whitespace errors; after the implementation commit, the working tree is clean. Confirm the final diff contains only the test and the four scoped UI/publication files.
