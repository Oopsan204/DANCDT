# Z Height Set Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Manual Jog Z-height setting that writes millimetres converted to PLC units at `D110`, then pulses `M212`.

**Architecture:** Keep conversion and validation in a small pure helper covered by the existing console test harness. Add one `WpfUiState` input and command binding, then implement the PLC handler in `Form1.PlcControl.cs` using the existing connection guard and `WriteDeviceValueAsync`. Delegate only the XAML layout update to Antigravity and review its diff.

**Tech Stack:** C# .NET Framework 4.8, WPF/XAML, existing `DecimalInputParser`, MX Component PLC writer, custom console tests, Antigravity CLI.

## Global Constraints

- Do not modify PLC coordinate layout, QD75 buffers, process rows, motion commands, laser power, camera, MQTT, or WebRTC behavior.
- Use `D110` for the converted integer value.
- Pulse `M212` in the exact order `1`, then `0`, after the successful `D110` write.
- Antigravity may edit only `src/DACDT_2026.App/Views/**` for this task.

---

### Task 1: Add conversion and validation contract

**Files:**
- Create: `src/DACDT_2026.App/ZHeightSetting.cs`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- Produces `ZHeightSetting.TryConvertToPlcUnits(string text, out int plcValue)`.

- [ ] **Step 1: Write the failing test**

Add test calls for `1.25 -> 12500`, comma decimal input, invalid text, and negative input in `Main` and a method named `ZHeightConversionUsesTenThousandScale`.

- [ ] **Step 2: Run test to verify it fails**

Run: `msbuild tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj /t:Rebuild /p:Configuration=Debug /v:minimal; & tests/DACDT_2026.Tests/bin/Debug/DACDT_2026.Tests.exe`

Expected: compile failure because `ZHeightSetting` does not exist.

- [ ] **Step 3: Write minimal implementation**

Implement `TryConvertToPlcUnits` with `DecimalInputParser.TryParseFlexibleDouble`, finite/non-negative checks, `value * 10000`, and `int` range validation.

- [ ] **Step 4: Run test to verify it passes**

Run the same rebuild and test command. Expected: `All tests passed.`

- [ ] **Step 5: Commit**

```powershell
git add src/DACDT_2026.App/ZHeightSetting.cs tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj tests/DACDT_2026.Tests/Program.cs
git commit -m "feat: validate z height plc conversion"
```

### Task 2: Add PLC command and state binding

**Files:**
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/Form1.PlcControl.cs`

**Interfaces:**
- `WpfUiState.ZHeightInput` is the editable text binding.
- `WpfUiState.SetZHeightCommand` invokes `HandleSetZHeightAsync`.

- [ ] **Step 1: Write the failing test**

Extend the source-contract test to require `ZHeightInput`, `SetZHeightCommand`, `D110`, and both `M212` values in the implementation source.

- [ ] **Step 2: Run test to verify it fails**

Run the existing rebuild and test command. Expected: the source-contract assertion fails because the new members are absent.

- [ ] **Step 3: Write minimal implementation**

Add the input and command property, wire the command in the existing command setup, and implement `HandleSetZHeightAsync`:

```csharp
private async Task HandleSetZHeightAsync(string text)
{
    try
    {
        if (!await RequirePlcConnectedAsync("Z Height"))
            return;
        if (!ZHeightSetting.TryConvertToPlcUnits(text, out int plcValue))
        {
            await NotifyAsync("error", "Z Height", "Z height must be a non-negative decimal in millimetres.");
            return;
        }

        await WriteDeviceValueAsync("D110", plcValue);
        AddLogEntry("D110", plcValue.ToString(CultureInfo.InvariantCulture), "Write", "OK", "Set Z Height");
        await WriteDeviceValueAsync(StopRunRegister, 1);
        AddLogEntry(StopRunRegister, "1", "Write", "OK", "Set Z Height trigger");
        await WriteDeviceValueAsync(StopRunRegister, 0);
        AddLogEntry(StopRunRegister, "0", "Write", "OK", "Set Z Height trigger reset");
        await NotifyAsync("success", "Z Height", $"Z height set to {text} mm.");
    }
    catch (Exception ex)
    {
        await NotifyAsync("error", "Z Height", "Error setting Z height: " + ex.Message);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run the existing rebuild and test command. Expected: `All tests passed.`

- [ ] **Step 5: Commit**

```powershell
git add src/DACDT_2026.App/WpfUiState.cs src/DACDT_2026.App/Form1.cs src/DACDT_2026.App/Form1.PlcControl.cs tests/DACDT_2026.Tests/Program.cs
git commit -m "feat: add z height plc set command"
```

### Task 3: Delegate the Manual Jog UI to Antigravity

**Files:**
- Modify: `docs/ui-task.md`
- Antigravity may modify: `src/DACDT_2026.App/Views/Panels/SidebarControl.xaml`

**Interfaces:**
- Consume the existing `ZHeightInput` and `SetZHeightCommand` bindings.

- [ ] **Step 1: Write the UI task contract**

Describe the new English `Z height (mm)` field and `SET` button below jog speed, keeping the existing dense panel layout.

- [ ] **Step 2: Run Antigravity through the guarded runner**

Run: `powershell -ExecutionPolicy Bypass -File tools/run-antigravity-ui.ps1 -TaskPath docs/ui-task.md -Mode accept-edits`

Expected: the runner reports that changes stayed inside approved UI paths.

- [ ] **Step 3: Review the diff**

Run: `git diff -- src/DACDT_2026.App/Views/Panels/SidebarControl.xaml`

Expected: only the new field and button layout/bindings are changed; no PLC or motion logic is touched.

- [ ] **Step 4: Commit**

```powershell
git add docs/ui-task.md src/DACDT_2026.App/Views/Panels/SidebarControl.xaml
git commit -m "feat: add z height control to manual jog"
```

### Task 4: Verify the complete feature

**Files:**
- Verify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Verify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`

- [ ] **Step 1: Run tests**

Run: `msbuild tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj /t:Rebuild /p:Configuration=Debug /v:minimal; & tests/DACDT_2026.Tests/bin/Debug/DACDT_2026.Tests.exe`

Expected: exit code 0 and `All tests passed.`

- [ ] **Step 2: Build the application**

Run: `msbuild src/DACDT_2026.App/DACDT_2026.csproj /t:Build /p:Configuration=Debug /v:minimal`

Expected: exit code 0 with no compilation errors.

- [ ] **Step 3: Check the diff and repository state**

Run: `git diff --check; git status --short`

Expected: no whitespace errors and only the intended feature files are changed.
