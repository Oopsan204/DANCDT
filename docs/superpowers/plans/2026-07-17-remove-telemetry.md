# Remove Telemetry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the Telemetry feature and only its feature-specific code, view, navigation, and build references.

**Architecture:** Keep the existing WPF navigation and state-publishing structure intact while removing the Telemetry branch from each layer. The remaining views and PLC control paths continue using their existing commands and state publishers; only the standalone register/buffer inspection path is removed.

**Tech Stack:** C#/.NET Framework 4.8, WPF/XAML, legacy MSBuild project files, custom console test executable.

## Global Constraints

- Only remove Telemetry; preserve unrelated user changes already present in the worktree.
- Keep Dashboard, DXF/G-code Run, Monitor, Logs, Settings, and Help unchanged in behavior.
- Do not remove shared `WriteResult`, QD75 bulk-write, clear-buffer, or PLC communication code used by non-Telemetry flows.
- Run the focused test executable and application build before claiming completion.

---

### Task 1: Add the regression check first

**Files:**
- Modify: `D:/DACDT_2026/DANCDT/tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- Consumes: repository files through the existing `GetRepositoryPath` and `AssertTrue` helpers.
- Produces: a `TelemetryFeatureIsRemoved` check that later implementation steps make pass.

- [x] **Step 1: Write the failing test**

Add `TelemetryFeatureIsRemoved();` to `Main()` immediately before the final `Console.WriteLine("All tests passed.");`, then add this method near the existing XAML contract checks:

```csharp
private static void TelemetryFeatureIsRemoved()
{
    string appRoot = GetRepositoryPath("src", "DACDT_2026.App");
    string sidebar = File.ReadAllText(Path.Combine(appRoot, "Views", "Panels", "SidebarControl.xaml"));
    string rootView = File.ReadAllText(Path.Combine(appRoot, "Form1.xaml"));
    string project = File.ReadAllText(Path.Combine(appRoot, "DACDT_2026.csproj"));

    AssertTrue(!sidebar.Contains("Content=\"Telemetry\""), "Sidebar must not expose the Telemetry navigation button.");
    AssertTrue(!sidebar.Contains("CommandParameter=\"telemetry\""), "Sidebar must not expose the telemetry route.");
    AssertTrue(!rootView.Contains("TelemetryView"), "Root view must not instantiate TelemetryView.");
    AssertTrue(!project.Contains("Views\\TelemetryView.xaml"), "The application project must not compile TelemetryView.xaml.");
    AssertTrue(!File.Exists(Path.Combine(appRoot, "Views", "TelemetryView.xaml")), "TelemetryView.xaml must be removed.");
    AssertTrue(!File.Exists(Path.Combine(appRoot, "Views", "TelemetryView.xaml.cs")), "TelemetryView.xaml.cs must be removed.");
}
```

- [x] **Step 2: Run it to verify it fails**

Run from `D:/DACDT_2026/DANCDT`:

```powershell
msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug
& .\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected: the test executable fails with `Sidebar must not expose the Telemetry navigation button.` because the current sidebar and Telemetry files still exist.

### Task 2: Remove Telemetry UI and project registration

**Files:**
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Views/Panels/SidebarControl.xaml`
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Form1.xaml`
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/DACDT_2026.csproj`
- Delete: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Views/TelemetryView.xaml`
- Delete: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Views/TelemetryView.xaml.cs`

**Interfaces:**
- Consumes: existing navigation command and visibility bindings for the remaining views.
- Produces: a sidebar and root layout with no Telemetry route or view.

- [x] **Step 1: Remove only the Telemetry button and root view line**

Delete the single sidebar button with `Content="Telemetry"` and `CommandParameter="telemetry"`, and delete the single root XAML line containing `<views:TelemetryView .../>`. Leave surrounding view order and bindings unchanged.

- [x] **Step 2: Remove the project page entry and view files**

Delete only the `Page Include="Views\\TelemetryView.xaml"` item (including its `Generator` and `SubType` children) from `DACDT_2026.csproj`, then delete both Telemetry view files.

- [x] **Step 3: Run the regression check**

Run:

```powershell
msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug
& .\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected: the new UI/file assertions pass, while the executable may still fail later checks if code references to the removed view remain; continue to Task 3 before treating the test suite as green.

### Task 3: Remove Telemetry navigation and state wiring

**Files:**
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Form1.cs`
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Form1.StatePublisher.cs`
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Form1.PlcControl.cs`
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/WpfUiState.cs`

**Interfaces:**
- Consumes: remaining navigation names and state publishers.
- Produces: navigation and polling code with no Telemetry route, property, collections, or publisher.

- [x] **Step 1: Remove the Telemetry navigation branch**

In `Form1.cs`, remove only the `if` branch in `HandleSwitchViewAsync` that compares `viewName` to `"telemetry"` and calls `PushTelemetryStateAsync`. Keep the `logs` branch and navigation refresh behavior unchanged.

- [x] **Step 2: Remove Telemetry from all-state publishing**

In `Form1.StatePublisher.cs`, change:

```csharp
=> Task.WhenAll(PushControlStateAsync(), PushDxfStateAsync(), PushTelemetryStateAsync(), PushLogsStateAsync());
```

to:

```csharp
=> Task.WhenAll(PushControlStateAsync(), PushDxfStateAsync(), PushLogsStateAsync());
```

Then remove the entire `PushTelemetryStateAsync` method only.

- [x] **Step 3: Remove Telemetry from PLC polling**

In `Form1.PlcControl.cs`, remove only the block:

```csharp
if (currentView == "telemetry")
    await PushTelemetryStateAsync();
```

Leave the surrounding polling and `ScheduleFullControlStatePushFromPoll` call unchanged.

- [x] **Step 4: Remove Telemetry-only state members**

In `WpfUiState.cs`, remove only the Telemetry-specific fields/properties:

- `TelemetryRegisters` and `TelemetryBuffers` collections.
- `AddTelemetryRegisterCommand` and `AddTelemetryBufferCommand` properties.
- `IsTelemetryView` notifications and property.
- `telemetryAddressInput` and `telemetryLengthInput` backing fields.
- `TelemetryAddressInput` and `TelemetryLengthInput` properties.
- `TelemetryRegisterViewModel` and `TelemetryBufferViewModel` classes.

Do not alter `WriteAddressInput`, `WriteValueInput`, or shared log/control state.

### Task 4: Remove Telemetry-only PLC handlers and data models

**Files:**
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Form1.cs`
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Form1.DxfHandler.cs`
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Form1.Models.cs`
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/QD75BufferWriter.cs`

**Interfaces:**
- Consumes: remaining command configuration and QD75 writer methods used by DXF/G-code execution.
- Produces: no standalone Telemetry register/buffer read/write handlers or dead Telemetry-only writer entry point.

- [x] **Step 1: Remove Telemetry configuration and command setup**

In `Form1.cs`, remove the `telemetryRegisters` and `telemetryBuffers` fields. In `ConfigureCommands`, remove the two assignments for `AddTelemetryRegisterCommand` and `AddTelemetryBufferCommand`. Keep `WriteBufferCommand` until its sole handler is removed in the next step.

- [x] **Step 2: Remove Telemetry handlers and model**

In `Form1.DxfHandler.cs`, remove these complete private methods only:

`HandleAddTelemetryRegisterAsync`, `HandleRemoveTelemetryRegisterAsync`, `HandleAddTelemetryBufferAsync`, `HandleRemoveTelemetryBufferAsync`, and `HandleWriteBufferRequestAsync`.

In `Form1.Models.cs`, remove the `TelemetryBuffer` nested class only.

- [x] **Step 3: Remove the now-unused write command and writer method**

In `Form1.cs` and `WpfUiState.cs`, remove `WriteBufferCommand` setup and property. In `QD75BufferWriter.cs`, remove only `WriteBufferValue(PLCCommunication plcComm, string path, int value)` and its Telemetry/manual-buffer-write comment. Preserve `WriteResult`, `Write16`, `Write32`, `WritePoints`, `WriteMasterPoints`, `WriteSlavePoints`, `ClearAllBuffers`, and `StartAxis` because they are used by non-Telemetry PLC workflows.

### Task 5: Verify scope and build

**Files:**
- No source changes expected; inspect only.

- [x] **Step 1: Run the focused tests**

Run:

```powershell
msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug
& .\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected: process exit code `0` and output `All tests passed.`

- [ ] **Step 2: Build the application**

Run:

```powershell
msbuild src\DACDT_2026.App\DACDT_2026.csproj /t:Build /p:Configuration=Debug
```

Result: blocked by the pre-existing legacy WPF project/toolchain configuration; `dotnet msbuild` does not import `MarkupCompilePass1`, so generated members such as `InitializeComponent` are unavailable. The system has no full Visual Studio `msbuild` executable in PATH.

- [x] **Step 3: Search for removed feature identifiers**

Run:

```powershell
rg -n -i 'TelemetryView|IsTelemetryView|AddTelemetry|PushTelemetryStateAsync|HandleWriteBufferRequestAsync|WriteBufferCommand|TelemetryRegisters|TelemetryBuffers|TelemetryAddressInput|TelemetryLengthInput|TelemetryBuffer' src\DACDT_2026.App tests\DACDT_2026.Tests
```

Expected: no matches in executable application code or the project file. Incidental notification labels containing the word `Telemetry` must be reviewed and either retained only when they are unrelated operational labels or changed only if they are part of the removed feature.

- [x] **Step 4: Review the diff and pre-existing worktree changes**

Run:

```powershell
git status --short
git diff --check
git diff -- src/DACDT_2026.App tests/DACDT_2026.Tests
```

Expected: the diff contains only the Telemetry removal plus the regression check; all unrelated pre-existing modifications remain present and untouched.
