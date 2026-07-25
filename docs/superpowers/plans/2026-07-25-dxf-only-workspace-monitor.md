# DXF-Only Workspace Monitor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chuyển ứng dụng WPF sang luồng DXF-only, áp dụng Workspace động cho toàn bộ kiểm tra giới hạn và thay G-code Editor bằng DXF Point Monitor mà không thay đổi giao thức PLC, QD75 hoặc Ring Buffer.

**Architecture:** Luồng nhập file chỉ còn một cửa vào `HandleImportDxfAsync`, tạo `activeCadDocument` và `processRows` DXF như hiện tại. Workspace được kiểm tra bằng một policy thuần, sau đó trở thành nguồn duy nhất cho preview, Scan Limits và Test Area. DXF Point Monitor dùng trực tiếp cửa sổ `ProgramRows` đã có và một bộ hẹn giờ 100 ms để gộp yêu cầu cuộn.

**Tech Stack:** C# 7.x, .NET Framework 4.8, WPF/XAML, `dotnet msbuild`, bộ test console `DACDT_2026.Tests`.

## Global Constraints

- Chỉ nhận file `.dxf`; không nhận G-code, NC, NGC, CNC, TAP hoặc TXT.
- Chỉ chấp nhận Workspace Width/Height hữu hạn và lớn hơn `0`.
- Giá trị Workspace không hợp lệ phải giữ nguyên giá trị hợp lệ trước đó và không được lưu.
- Giữ nguyên `processRows`, QD75, Ring Buffer và các lệnh PLC RUN/PAUSE/CONTINUE/STOP/HOME/RESET.
- Giữ nguyên Test Area, Clear Buffer, Export QD75, Camera, Monitor và Logs.
- Giữ nguyên `MotionType` và mã lệnh nội bộ dùng cho QD75.
- DXF Point Monitor phải dùng `ProgramRows`, không sao chép toàn bộ danh sách lệnh.
- Bật row/column virtualization và giới hạn cuộn tự động ở `100 ms` một lần.
- Không thay đổi giao thức PLC, cấu trúc QD75, tần suất đọc Md.44 hoặc cơ chế nạp Ring Buffer.
- Không tạo installer trong thay đổi này.

---

## File Map

**Create**

- `src/DACDT_2026.App/WorkspaceLimitPolicy.cs`: kiểm tra kích thước Workspace và khoảng tọa độ bằng logic thuần có thể unit test.

**Modify**

- `src/DACDT_2026.App/Form1.cs`: command DXF-only, áp dụng Workspace, loại bỏ state/settings G-code và WCS.
- `src/DACDT_2026.App/Form1.DxfHandler.cs`: giữ một luồng import DXF, dùng Workspace động, loại bỏ parser/preview/save/compiler G-code.
- `src/DACDT_2026.App/Form1.StatePublisher.cs`: publish preview/process rows chỉ theo offset DXF.
- `src/DACDT_2026.App/Form1.PlcControl.cs`: bỏ nội dung mô tả G-code cũ, không đổi logic PLC.
- `src/DACDT_2026.App/Form1.Models.cs`: loại bỏ `WcsIndex` không còn dùng.
- `src/DACDT_2026.App/WpfUiState.cs`: bỏ command/state G-code và WCS; giữ state bảng chương trình.
- `src/DACDT_2026.App/CadDisplayDocumentBuilder.cs`: chỉ áp dụng offset DXF.
- `src/DACDT_2026.App/CadDocumentService.cs`: bỏ metadata WCS khỏi primitive DXF.
- `src/DACDT_2026.App/CadPreviewBuilder.cs`: không sao chép metadata WCS.
- `src/DACDT_2026.App/CadPathSelection.cs`: nhóm đường DXF theo X/Y và bỏ tham số `isGcode`.
- `src/DACDT_2026.App/Views/DxfRunView.xaml`: thay editor bằng DXF Point Monitor.
- `src/DACDT_2026.App/Views/DxfRunView.xaml.cs`: theo dõi dòng active và cuộn latest-only mỗi 100 ms.
- `src/DACDT_2026.App/Views/SettingsView.xaml`: bỏ phần G-code Motion và WCS.
- `src/DACDT_2026.App/Views/SettingsView.xaml.cs`: bỏ event chọn WCS.
- `src/DACDT_2026.App/Views/Panels/SidebarControl.xaml`: đổi nhãn thành `DXF Run`.
- `src/DACDT_2026.App/Views/DashboardView.xaml`: đổi tiêu đề cột thành `DXF Point`.
- `src/DACDT_2026.App/Views/MonitorView.xaml`: đổi tiêu đề cột thành `DXF Point`.
- `src/DACDT_2026.App/Views/HelpView.xaml`: hướng dẫn vận hành chỉ còn DXF.
- `src/DACDT_2026.App/DACDT_2026.csproj`: thêm policy và bỏ module/dependency G-code.
- `src/DACDT_2026.App/packages.config`: bỏ `Gcode.Utils`.
- `tests/DACDT_2026.Tests/Program.cs`: thêm test Workspace/DXF-only/monitor và bỏ test parser G-code.
- `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`: link policy mới và bỏ helper G-code.

**Delete**

- `src/DACDT_2026.App/GcodeCoordinateService.cs`
- `src/DACDT_2026.App/GcodeDocumentService.cs`
- `src/DACDT_2026.App/GcodeLineSanitizer.cs`
- `src/DACDT_2026.App/NcGcodeCleaner.cs`

---

### Task 1: Make Workspace the Dynamic Limit Source

**Files:**

- Create: `src/DACDT_2026.App/WorkspaceLimitPolicy.cs`
- Modify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Modify: `src/DACDT_2026.App/Form1.cs:282-325,386-417`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs:1678-1718,2751-2832`
- Test: `tests/DACDT_2026.Tests/Program.cs`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`

**Interfaces:**

- Produces: `WorkspaceLimitPolicy.IsValid(double width, double height) : bool`
- Produces: `WorkspaceLimitPolicy.IsRangeWithin(double minimum, double maximum, double limit) : bool`
- Produces: `Form1.TryApplyWorkspaceInputs(out string errorMessage) : bool`
- Produces: `Form1.ApplyWorkspaceSettingsAsync() : Task`

- [ ] **Step 1: Add failing Workspace behavior and source-contract tests**

Add these calls to `Main()` before the view contract tests:

```csharp
WorkspaceLimitPolicyUsesConfiguredDimensions();
WorkspaceSettingsDriveScanAndTestAreaLimits();
```

Add these methods:

```csharp
private static void WorkspaceLimitPolicyUsesConfiguredDimensions()
{
    AssertTrue(WorkspaceLimitPolicy.IsValid(175.0, 175.0),
        "175 x 175 must be a valid Workspace.");
    AssertTrue(!WorkspaceLimitPolicy.IsValid(0.0, 175.0),
        "Zero width must be rejected.");
    AssertTrue(!WorkspaceLimitPolicy.IsValid(double.NaN, 175.0),
        "NaN width must be rejected.");
    AssertTrue(!WorkspaceLimitPolicy.IsValid(175.0, double.PositiveInfinity),
        "Infinite height must be rejected.");
    AssertTrue(WorkspaceLimitPolicy.IsRangeWithin(0.0, 171.0, 175.0),
        "Coordinate 171 must fit a configured 175 mm Workspace.");
    AssertTrue(!WorkspaceLimitPolicy.IsRangeWithin(0.0, 171.0, 170.0),
        "Coordinate 171 must exceed a configured 170 mm Workspace.");
}

private static void WorkspaceSettingsDriveScanAndTestAreaLimits()
{
    string form = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "Form1.cs"));
    string handler = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "Form1.DxfHandler.cs"));

    AssertTrue(form.Contains("WorkspaceLimitPolicy.IsValid(requestedWidth, requestedHeight)"),
        "Workspace Apply must validate both configured dimensions.");
    AssertTrue(form.Contains("workspaceWidth = requestedWidth;")
        && form.Contains("workspaceHeight = requestedHeight;"),
        "Workspace Apply must update runtime state before scan and preview.");
    AssertTrue(handler.Contains("double snapLimitX = workspaceWidth;")
        && handler.Contains("double snapLimitY = workspaceHeight;"),
        "Scan Limits must snapshot the configured Workspace.");
    AssertTrue(!handler.Contains("const double LimitX = 170.0")
        && !handler.Contains("const double LimitY = 170.0")
        && !handler.Contains("170x170"),
        "DXF limit checks must not retain fixed 170 mm constants.");
}
```

Add this linked compile item to the test project:

```xml
<Compile Include="..\..\src\DACDT_2026.App\WorkspaceLimitPolicy.cs">
  <Link>WorkspaceLimitPolicy.cs</Link>
</Compile>
```

- [ ] **Step 2: Run the tests and verify the new contract fails**

Run:

```powershell
dotnet msbuild "tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj" /t:Rebuild /p:Configuration=Debug /v:minimal
```

Expected: FAIL because `WorkspaceLimitPolicy.cs` does not exist yet.

- [ ] **Step 3: Implement the pure Workspace policy**

Create `WorkspaceLimitPolicy.cs`:

```csharp
using System;

namespace DACDT_2026
{
    internal static class WorkspaceLimitPolicy
    {
        public static bool IsValid(double width, double height)
            => IsFinitePositive(width) && IsFinitePositive(height);

        public static bool IsRangeWithin(double minimum, double maximum, double limit)
            => IsFinitePositive(limit)
               && !double.IsNaN(minimum)
               && !double.IsInfinity(minimum)
               && !double.IsNaN(maximum)
               && !double.IsInfinity(maximum)
               && minimum >= 0.0
               && maximum <= limit;

        private static bool IsFinitePositive(double value)
            => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
    }
}
```

Add it to the app project:

```xml
<Compile Include="WorkspaceLimitPolicy.cs" />
```

- [ ] **Step 4: Apply validated Workspace values from both Settings actions**

Replace the inline `SetWorkspaceCommand` body with:

```csharp
ui.SetWorkspaceCommand = new RelayCommand(ApplyWorkspaceSettingsAsync);
```

Add:

```csharp
private bool TryApplyWorkspaceInputs(out string errorMessage)
{
    double requestedWidth = ui.WorkspaceWidthInput;
    double requestedHeight = ui.WorkspaceHeightInput;
    if (!WorkspaceLimitPolicy.IsValid(requestedWidth, requestedHeight))
    {
        errorMessage = "Workspace Width and Height must be finite values greater than 0.";
        ui.WorkspaceWidthInput = workspaceWidth;
        ui.WorkspaceHeightInput = workspaceHeight;
        return false;
    }

    workspaceWidth = requestedWidth;
    workspaceHeight = requestedHeight;
    errorMessage = string.Empty;
    return true;
}

private async Task ApplyWorkspaceSettingsAsync()
{
    if (!TryApplyWorkspaceInputs(out string errorMessage))
    {
        await NotifyAsync("error", "Workspace", errorMessage);
        return;
    }

    SaveSettingsToFile();
    await HandleScanLimitsAsync();
    await PushDxfStateAsync();
    await NotifyAsync("success", "Settings", "Updated workspace size.");
}
```

Call `TryApplyWorkspaceInputs` at the start of `ApplyDxfSettingsAsync`, before changing settings or rebuilding:

```csharp
if (!TryApplyWorkspaceInputs(out string workspaceError))
{
    await NotifyAsync("error", "Workspace", workspaceError);
    return;
}
```

- [ ] **Step 5: Replace hard-coded Scan Limits and Test Area limits**

In `HandleScanLimitsAsync`, snapshot:

```csharp
double snapLimitX = workspaceWidth;
double snapLimitY = workspaceHeight;
```

Use:

```csharp
bool xWithin = WorkspaceLimitPolicy.IsRangeWithin(adjMinX, adjMaxX, snapLimitX);
bool yWithin = WorkspaceLimitPolicy.IsRangeWithin(adjMinY, adjMaxY, snapLimitY);
bool anyExceed = !xWithin || !yWithin;
```

Format the result with `snapLimitX` and `snapLimitY`, and change the empty-state text to:

```csharp
"No DXF file is loaded."
```

In Test Area, snapshot the same values and reject with:

```csharp
if (!WorkspaceLimitPolicy.IsRangeWithin(adjMinX, adjMaxX, snapLimitX)
    || !WorkspaceLimitPolicy.IsRangeWithin(adjMinY, adjMaxY, snapLimitY))
{
    await NotifyAsync(
        "error",
        "Test Area",
        $"Engrave area exceeds Workspace ({snapLimitX:0.###} x {snapLimitY:0.###} mm).");
    return;
}
```

Use `globalSpeedM3` for the first Test Area travel row so removing the G-code-only rapid setting later does not alter configurability.

- [ ] **Step 6: Build and run tests**

Run:

```powershell
dotnet msbuild "tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj" /t:Rebuild /p:Configuration=Debug /v:minimal
& "tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe"
```

Expected: build succeeds and prints `All tests passed.`

- [ ] **Step 7: Commit**

```powershell
git add src/DACDT_2026.App/WorkspaceLimitPolicy.cs src/DACDT_2026.App/DACDT_2026.csproj src/DACDT_2026.App/Form1.cs src/DACDT_2026.App/Form1.DxfHandler.cs tests/DACDT_2026.Tests/Program.cs tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj
git commit -m "fix: apply configured DXF workspace limits"
```

---

### Task 2: Replace the Editor with DXF Point Monitor

**Files:**

- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml:8-14,299-323`
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml.cs:1-42`
- Test: `tests/DACDT_2026.Tests/Program.cs:1420-1445,1684-1697`

**Interfaces:**

- Consumes: `WpfUiState.ProgramRows`
- Consumes: `WpfUiState.ActiveProgramIndex`
- Consumes: `WpfUiState.EnsureProcessRowVisible(int rowIndex) : bool`
- Consumes: `WpfUiState.LoadMoreProgramRows() : bool`
- Produces: `DxfRunView.QueueActiveProgramScroll() : void`
- Produces: `DxfRunView.ProgramGrid_ScrollChanged(object, ScrollChangedEventArgs) : void`

- [ ] **Step 1: Add failing XAML and throttling contract tests**

Add this call:

```csharp
DxfRunViewShowsVirtualizedPointMonitor();
```

Replace the old geometry/editor assertion method with:

```csharp
private static void DxfRunViewShowsVirtualizedPointMonitor()
{
    string xaml = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
    string code = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "Views", "DxfRunView.xaml.cs"));

    AssertTrue(xaml.Contains("Text=\"DXF Point Monitor\""),
        "The DXF tab must show the DXF Point Monitor title.");
    AssertTrue(xaml.Contains("ItemsSource=\"{Binding ProgramRows}\""),
        "The point monitor must reuse the existing ProgramRows window.");
    AssertTrue(xaml.Contains("Header=\"DXF Point\"")
        && xaml.Contains("Binding=\"{Binding MotionType}\""),
        "The table must expose the DXF Point column.");
    AssertTrue(xaml.Contains("Header=\"End X;Y\"")
        && xaml.Contains("Binding=\"{Binding EndCoordinate}\""),
        "The table must expose the endpoint column.");
    AssertTrue(xaml.Contains("EnableRowVirtualization=\"True\"")
        && xaml.Contains("EnableColumnVirtualization=\"True\"")
        && xaml.Contains("ScrollViewer.CanContentScroll=\"True\""),
        "The DXF point table must virtualize rows and columns.");
    AssertTrue(xaml.Contains("ScrollViewer.ScrollChanged=\"ProgramGrid_ScrollChanged\""),
        "The point table must lazy-load its existing row window.");
    AssertTrue(!xaml.Contains("G-code Editor")
        && !xaml.Contains("PreviewGcodeCommand")
        && !xaml.Contains("SaveGcodeCommand"),
        "The old editor and editor actions must be removed.");
    AssertTrue(code.Contains("DispatcherTimer activeProgramScrollTimer")
        && code.Contains("TimeSpan.FromMilliseconds(100)"),
        "The DXF tab must coalesce auto-scroll requests at 10 Hz.");
}
```

Extend `ProgramMonitorAutoScrollIsLatestOnlyAndThrottled`:

```csharp
string[] files =
{
    "DashboardView.xaml.cs",
    "MonitorView.xaml.cs",
    "DxfRunView.xaml.cs"
};
```

- [ ] **Step 2: Run tests and verify the UI contract fails**

Run the Debug test rebuild and executable from Task 1.

Expected: FAIL at `DxfRunViewShowsVirtualizedPointMonitor`.

- [ ] **Step 3: Replace the right panel XAML**

Declare:

```xml
<local:BoolToStatusBrushConverter x:Key="BoolToStatusBrushConverter"/>
```

Replace the editor panel with a `Border` containing:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        <StackPanel>
            <TextBlock Text="DXF Point Monitor" Style="{StaticResource PanelTitleStyle}"/>
            <TextBlock Text="{Binding ProgramMonitorSubtitle}"
                       Style="{StaticResource PanelSubtitleStyle}"
                       TextTrimming="CharacterEllipsis"/>
        </StackPanel>
        <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
            <TextBlock Text="{Binding ActiveProgramText}"
                       Foreground="{DynamicResource MutedBrush}"
                       Margin="0,0,8,0"/>
            <Ellipse Width="12" Height="12"
                     Fill="{Binding IsConnected, Converter={StaticResource BoolToStatusBrushConverter}}"/>
        </StackPanel>
    </Grid>

    <StackPanel Grid.Row="1" Margin="0,8,0,0">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBlock Text="{Binding RunProgressText}"
                       Foreground="{DynamicResource MutedBrush}"
                       Visibility="{Binding RunProgressVisible, Converter={StaticResource BoolToVisibilityConverter}}"/>
            <TextBlock Grid.Column="1"
                       Text="{Binding ProgressText}"
                       Foreground="{DynamicResource MutedBrush}"
                       Visibility="{Binding ProgressVisible, Converter={StaticResource BoolToVisibilityConverter}}"/>
        </Grid>
        <ProgressBar Height="8" Maximum="100"
                     Value="{Binding RunProgressPercent, Mode=OneWay}"
                     Visibility="{Binding RunProgressVisible, Converter={StaticResource BoolToVisibilityConverter}}"
                     Margin="0,5,0,0"/>
        <ProgressBar Height="5" Maximum="100"
                     Value="{Binding ProgressPercent}"
                     Visibility="{Binding ProgressVisible, Converter={StaticResource BoolToVisibilityConverter}}"
                     Margin="0,4,0,0"/>
    </StackPanel>

    <DataGrid x:Name="ProgramGrid"
              Grid.Row="2"
              ItemsSource="{Binding ProgramRows}"
              AutoGenerateColumns="False"
              IsReadOnly="True"
              EnableRowVirtualization="True"
              EnableColumnVirtualization="True"
              ScrollViewer.CanContentScroll="True"
              ScrollViewer.ScrollChanged="ProgramGrid_ScrollChanged"
              HorizontalScrollBarVisibility="Auto"
              Margin="0,8,0,0"
              Background="{DynamicResource PanelAltBrush}">
        <DataGrid.RowStyle>
            <Style TargetType="{x:Type DataGridRow}">
                <Setter Property="MinHeight" Value="28"/>
                <Setter Property="Foreground" Value="{DynamicResource TextBrush}"/>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsActive}" Value="True">
                        <Setter Property="Background" Value="#1E4E3A"/>
                        <Setter Property="Foreground" Value="White"/>
                        <Setter Property="FontWeight" Value="Bold"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </DataGrid.RowStyle>
        <DataGrid.Columns>
            <DataGridTextColumn Header="Run" Binding="{Binding ActiveMarker}" Width="48"/>
            <DataGridTextColumn Header="No." Binding="{Binding Index}" Width="58"/>
            <DataGridTextColumn Header="DXF Point" Binding="{Binding MotionType}" Width="*"/>
            <DataGridTextColumn Header="M" Binding="{Binding MCodeValue}" Width="48"/>
            <DataGridTextColumn Header="Speed" Binding="{Binding Speed}" Width="72"/>
            <DataGridTextColumn Header="End X;Y" Binding="{Binding EndCoordinate}" Width="135"/>
        </DataGrid.Columns>
    </DataGrid>
</Grid>
```

- [ ] **Step 4: Add latest-only 100 ms scrolling to DxfRunView**

Add the required namespaces:

```csharp
using System.Collections.Specialized;
using System.ComponentModel;
```

Add fields and constructor wiring:

```csharp
private WpfUiState observedState;
private readonly DispatcherTimer activeProgramScrollTimer;
private bool activeProgramScrollPending;

activeProgramScrollTimer = new DispatcherTimer(DispatcherPriority.Background)
{
    Interval = TimeSpan.FromMilliseconds(100)
};
activeProgramScrollTimer.Tick += ActiveProgramScrollTimer_Tick;
DataContextChanged += DxfRunView_DataContextChanged;
```

Add the same lifecycle used by MonitorView, with DxfRunView-specific handler names:

```csharp
private void DxfRunView_DataContextChanged(
    object sender,
    DependencyPropertyChangedEventArgs e)
{
    activeProgramScrollTimer.Stop();
    activeProgramScrollPending = false;

    if (observedState != null)
    {
        observedState.PropertyChanged -= ObservedState_PropertyChanged;
        observedState.ProgramRows.CollectionChanged -= ProgramRows_CollectionChanged;
    }

    observedState = e.NewValue as WpfUiState;
    if (observedState != null)
    {
        observedState.PropertyChanged += ObservedState_PropertyChanged;
        observedState.ProgramRows.CollectionChanged += ProgramRows_CollectionChanged;
    }

    QueueActiveProgramScroll();
}
```

Implement `ObservedState_PropertyChanged`, `ProgramRows_CollectionChanged`, `QueueActiveProgramScroll`, `ActiveProgramScrollTimer_Tick`, `ScrollActiveProgramRow`, `ProgramGrid_ScrollChanged` and `IsNearScrollEnd` with these invariants:

```csharp
if (e.PropertyName == nameof(WpfUiState.ActiveProgramIndex))
    QueueActiveProgramScroll();
```

```csharp
activeProgramScrollPending = true;
if (!activeProgramScrollTimer.IsEnabled)
    activeProgramScrollTimer.Start();
```

```csharp
observedState.EnsureProcessRowVisible(observedState.ActiveProgramIndex);
ProgramGrid.ScrollIntoView(activeRow);
```

```csharp
if (observedState != null && IsNearScrollEnd(e))
    observedState.LoadMoreProgramRows();
```

- [ ] **Step 5: Build and run tests**

Run the Debug test rebuild and executable.

Expected: `All tests passed.`

- [ ] **Step 6: Commit**

```powershell
git add src/DACDT_2026.App/Views/DxfRunView.xaml src/DACDT_2026.App/Views/DxfRunView.xaml.cs tests/DACDT_2026.Tests/Program.cs
git commit -m "feat: add DXF point monitor to run view"
```

---

### Task 3: Remove All Visible G-code and WCS Controls

**Files:**

- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml:50-58`
- Modify: `src/DACDT_2026.App/Views/SettingsView.xaml:65-154`
- Modify: `src/DACDT_2026.App/Views/SettingsView.xaml.cs`
- Modify: `src/DACDT_2026.App/Views/Panels/SidebarControl.xaml:27`
- Modify: `src/DACDT_2026.App/Views/DashboardView.xaml:261`
- Modify: `src/DACDT_2026.App/Views/MonitorView.xaml:184`
- Modify: `src/DACDT_2026.App/Views/HelpView.xaml`
- Modify: `src/DACDT_2026.App/WpfUiState.cs:780-796`
- Test: `tests/DACDT_2026.Tests/Program.cs:1487-1519,1684-1755`

**Interfaces:**

- Produces: `WpfUiState.ProgramMonitorTitle` fixed to `DXF Point Monitor`
- Produces: `WpfUiState.ProgramMonitorSubtitle` with DXF-only empty state

- [ ] **Step 1: Add the failing visible DXF-only contract**

Add:

```csharp
DxfOnlyViewsRemoveGcodeAndWcsControls();
```

Implement:

```csharp
private static void DxfOnlyViewsRemoveGcodeAndWcsControls()
{
    string appRoot = GetRepositoryPath("src", "DACDT_2026.App");
    string dxf = File.ReadAllText(Path.Combine(appRoot, "Views", "DxfRunView.xaml"));
    string settings = File.ReadAllText(Path.Combine(appRoot, "Views", "SettingsView.xaml"));
    string sidebar = File.ReadAllText(Path.Combine(appRoot, "Views", "Panels", "SidebarControl.xaml"));
    string dashboard = File.ReadAllText(Path.Combine(appRoot, "Views", "DashboardView.xaml"));
    string monitor = File.ReadAllText(Path.Combine(appRoot, "Views", "MonitorView.xaml"));
    string help = File.ReadAllText(Path.Combine(appRoot, "Views", "HelpView.xaml"));

    AssertTrue(!dxf.Contains("New Gcode"), "The DXF toolbar must not create G-code.");
    AssertTrue(sidebar.Contains("Content=\"DXF Run\"")
        && !sidebar.Contains("DXF / GCODE Run"),
        "Navigation must expose a DXF-only route label.");
    AssertTrue(!settings.Contains("G-code Motion")
        && !settings.Contains("G54-G59")
        && !settings.Contains("WcsGrid"),
        "Settings must not expose G-code or WCS controls.");
    AssertTrue(dashboard.Contains("Header=\"DXF Point\"")
        && monitor.Contains("Header=\"DXF Point\""),
        "All program tables must use the DXF-only column label.");
    AssertTrue(help.Contains("Mở và kiểm tra file DXF")
        && !help.Contains("G-code")
        && !help.Contains("GCODE")
        && !help.Contains("WCS"),
        "The Vietnamese guide must describe DXF-only operation.");
}
```

Update `SettingsViewUsesApprovedEnglishContract` so required labels are:

```csharp
string[] requiredLabels =
{
    "DXF Processing",
    "Travel Speed (M03 / Home) (mm/min)",
    "Laser On Delay (M03) (ms)",
    "Laser Off Delay (M04) (ms)",
    "Workspace Width (mm)",
    "Workspace Height (mm)"
};
```

Add `G-code Motion`, `Rapid Travel Speed (G00) (mm/min)` and `G54-G59 WCS Offsets` to its obsolete-label array.

Update the Help section contract from `Mở và kiểm tra file DXF/G-code` to `Mở và kiểm tra file DXF`, and forbid `G-code`, `GCODE` and `WCS`.

- [ ] **Step 2: Run tests and verify the visible contract fails**

Run the Debug test rebuild and executable.

Expected: FAIL at `DxfOnlyViewsRemoveGcodeAndWcsControls`.

- [ ] **Step 3: Remove visible controls and rename labels**

In `DxfRunView.xaml`, leave only:

```xml
<Button Content="Import DXF"
        Command="{Binding ImportDxfCommand}"
        Style="{StaticResource PrimaryButtonStyle}"
        Width="110"/>
```

Delete the entire G-code Motion and WCS panels from `SettingsView.xaml`. Keep DXF Processing, Workspace, PLC Connection, Camera/recording settings and Save Settings.

Reduce `SettingsView.xaml.cs` to:

```csharp
using System.Windows.Controls;

namespace DACDT_2026.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }
    }
}
```

Use these labels:

```xml
Content="DXF Run"
```

```xml
Header="DXF Point"
```

- [ ] **Step 4: Rewrite Help copy to DXF-only**

Use `4. Mở và kiểm tra file DXF` in both navigation and section title. Replace the operating paragraph with:

```text
Mở tab DXF Run và chọn file DXF cần chạy. Hệ thống chỉ nhận đường thẳng, cung tròn và hình tròn.
Kiểm tra bản xem trước, thứ tự đường chạy, điểm bắt đầu, chiều di chuyển, kích thước vùng làm việc,
tốc độ và offset X/Y. Nếu hình vẽ hoặc thông số không đúng, không gửi dữ liệu xuống PLC.
```

Replace remaining checklist/help references to WCS with `offset X/Y`, and remove every reference to editing, previewing, saving or running G-code.

- [ ] **Step 5: Make monitor copy DXF-only**

Replace the conditional title/subtitle with:

```csharp
public string ProgramMonitorTitle => "DXF Point Monitor";

public string ProgramMonitorSubtitle => string.IsNullOrWhiteSpace(FileName)
    ? "Open a DXF file to populate this list"
    : FileName + " - highlight follows Axis 1 current data no.";
```

- [ ] **Step 6: Build and run tests**

Run the Debug test rebuild and executable.

Expected: `All tests passed.`

- [ ] **Step 7: Commit**

```powershell
git add src/DACDT_2026.App/Views src/DACDT_2026.App/WpfUiState.cs tests/DACDT_2026.Tests/Program.cs
git commit -m "refactor: make operator views DXF-only"
```

---

### Task 4: Remove Executable G-code Entry Points and Branches

**Files:**

- Modify: `src/DACDT_2026.App/Form1.cs:49-119,282-325,419-456`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs`
- Modify: `src/DACDT_2026.App/Form1.PlcControl.cs:484`
- Modify: `src/DACDT_2026.App/WpfUiState.cs:103-121,199-215,431-542`
- Test: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**

- Keeps: `Form1.HandleImportDxfAsync() : Task`
- Keeps: `Form1.BuildDxfProcessRows(CadLoadResult sourceDocument = null, CancellationToken cancellationToken = default(CancellationToken)) : List<ProcessRow>`
- Changes: `Form1.BuildConnectedPathsFromCad() : List<ProcessRow>` to a DXF-only dispatcher
- Changes: `Form1.GetConnectedPathsFromCad(List<CadPrimitiveData>, CancellationToken) : List<List<CadPrimitiveData>>`
- Changes: `Form1.IsClosedPath(List<CadPrimitiveData>) : bool`
- Changes: `Form1.AreClose(CadCoordinate, CadCoordinate) : bool`

- [ ] **Step 1: Add a failing runtime-entry-point contract**

Add:

```csharp
DxfRuntimeHasNoGcodeEntryPoints();
```

Implement:

```csharp
private static void DxfRuntimeHasNoGcodeEntryPoints()
{
    string form = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "Form1.cs"));
    string handler = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
    string state = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "WpfUiState.cs"));

    string[] removedMembers =
    {
        "NewGcodeCommand",
        "SaveGcodeCommand",
        "PreviewGcodeCommand",
        "ApplyGcodeSettingsCommand",
        "HandlePreviewGcodeAsync",
        "HandleNewGcodeAsync",
        "HandleSaveGcodeAsync",
        "ShowSaveGcodeDialog",
        "IsGcodeFile",
        "BuildGcodeProcessRows",
        "UpdateGcodeFromProcessTable",
        "HandleOpenDxfAsync",
        "ShowOpenFileDialog"
    };

    foreach (string member in removedMembers)
    {
        AssertTrue(!form.Contains(member)
            && !handler.Contains(member)
            && !state.Contains(member),
            "DXF-only runtime must remove member: " + member);
    }

    AssertTrue(handler.Contains("Filter = \"DXF files (*.dxf)|*.dxf\""),
        "The only open-file filter must accept DXF.");
    AssertTrue(!handler.Contains("*.nc")
        && !handler.Contains("*.ngc")
        && !handler.Contains("*.cnc")
        && !handler.Contains("*.tap"),
        "The runtime must not recognize CNC/G-code extensions.");
}
```

- [ ] **Step 2: Run tests and verify the entry-point contract fails**

Run the Debug test rebuild and executable.

Expected: FAIL at `DxfRuntimeHasNoGcodeEntryPoints`.

- [ ] **Step 3: Keep only the DXF import command and dialog**

Command wiring must contain:

```csharp
ui.ImportDxfCommand = new RelayCommand(HandleImportDxfAsync);
```

Remove `OpenDxfCommand`, all G-code commands and their state properties. Delete the general file dialog and `HandleOpenDxfAsync`. Keep `ShowOpenDxfFileDialog` with:

```csharp
Filter = "DXF files (*.dxf)|*.dxf",
DefaultExt = "dxf",
AddExtension = true,
CheckFileExists = true
```

At the start of the import load block, reject a non-DXF path defensively:

```csharp
if (!string.Equals(Path.GetExtension(selectedPath), ".dxf",
        StringComparison.OrdinalIgnoreCase))
{
    await NotifyAsync("error", "DXF", "Only DXF files are supported.");
    return;
}
```

- [ ] **Step 4: Delete G-code handlers and simplify process-row compilation**

Delete:

- `ReportNcCleanerResultAsync`
- `HandlePreviewGcodeAsync`
- `HandleNewGcodeAsync`
- `HandleSaveGcodeAsync`
- `ShowSaveGcodeDialog`
- `IsGcodeFile`
- `UpdateGcodeFromProcessTable`
- `BuildGcodeProcessRows`

Remove calls to `UpdateGcodeFromProcessTable`.

Make the process-row dispatcher return the DXF path directly:

```csharp
private List<ProcessRow> BuildConnectedPathsFromCad()
    => BuildDxfProcessRows();
```

Collapse connected-path helpers to XY-only signatures:

```csharp
private List<List<CadDocumentService.CadPrimitiveData>> GetConnectedPathsFromCad(
    List<CadDocumentService.CadPrimitiveData> primitives,
    CancellationToken cancellationToken = default(CancellationToken))
    => CadPathSelection.GroupConnectedPaths(primitives, cancellationToken);

private bool IsClosedPath(List<CadDocumentService.CadPrimitiveData> path)
{
    if (path == null || path.Count == 0)
        return false;
    var first = path[0];
    var last = path[path.Count - 1];
    return first?.Points?.Count > 0
        && last?.Points?.Count > 0
        && AreClose(first.Points[0], last.Points[last.Points.Count - 1]);
}

private bool AreClose(
    CadDocumentService.CadCoordinate a,
    CadDocumentService.CadCoordinate b)
    => a != null
       && b != null
       && Math.Abs(a.X - b.X) < 0.001
       && Math.Abs(a.Y - b.Y) < 0.001;
```

Remove the `isGcode` parameter from `ApplyPrimitiveExtraData`; preserve M code, speed and dwell assignment.

- [ ] **Step 5: Collapse send/test/compiler branches to DXF behavior**

For every former `activeDocumentKind == "GCODE"` branch:

- use `offsetX` and `offsetY` for outgoing coordinates;
- use `globalSpeedM3` for M3/home travel and `globalSpeed` or row process speed exactly as the existing DXF branch does;
- use the selected DXF document and `BuildDxfProcessRows`;
- keep `EnsureCadProcessRowsCurrentAsync`, cancellation/version guards and Ring Buffer calls unchanged.

Remove `gcodeSpeedM3`, `rapidSpeed`, `rawGcodeText`, `isPreviewingGcode` and the G-code branch in `HandleProcessValueAsync`. Replace the remaining Test Area travel use of `rapidSpeed` with `globalSpeedM3`.

Change operator messages from `DXF/G-code file not loaded` to `DXF file not loaded`.

- [ ] **Step 6: Build and run tests**

Run the Debug test rebuild and executable.

Expected: `All tests passed.`

- [ ] **Step 7: Commit**

```powershell
git add src/DACDT_2026.App/Form1.cs src/DACDT_2026.App/Form1.DxfHandler.cs src/DACDT_2026.App/Form1.PlcControl.cs src/DACDT_2026.App/WpfUiState.cs tests/DACDT_2026.Tests/Program.cs
git commit -m "refactor: remove G-code runtime entry points"
```

---

### Task 5: Remove G-code State, WCS Metadata, Modules and Dependency

**Files:**

- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/Form1.Models.cs`
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs`
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `src/DACDT_2026.App/CadDisplayDocumentBuilder.cs`
- Modify: `src/DACDT_2026.App/CadDocumentService.cs`
- Modify: `src/DACDT_2026.App/CadPreviewBuilder.cs`
- Modify: `src/DACDT_2026.App/CadPathSelection.cs`
- Modify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Modify: `src/DACDT_2026.App/packages.config`
- Modify: `tests/DACDT_2026.Tests/Program.cs`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`
- Delete: four G-code source files listed in File Map

**Interfaces:**

- Changes: `CadDisplayDocumentBuilder.Build(CadLoadResult source, double offsetX, double offsetY, CancellationToken cancellationToken) : CadLoadResult`
- Changes: `CadPathSelection.GroupConnectedPaths(List<CadPrimitiveData> primitives, CancellationToken cancellationToken = default(CancellationToken)) : List<List<CadPrimitiveData>>`
- Changes: `PublishProcessRowWindowState(ProcessRow[] rows, bool hasEngraveCut, int activeIndex, double offsetX, double offsetY) : void`

- [ ] **Step 1: Add a failing whole-app DXF-only source contract**

Add:

```csharp
ApplicationProjectContainsNoGcodeImplementation();
```

Implement:

```csharp
private static void ApplicationProjectContainsNoGcodeImplementation()
{
    string appRoot = GetRepositoryPath("src", "DACDT_2026.App");
    string[] extensions = { ".cs", ".xaml", ".csproj", ".config" };
    string[] prohibited = { "Gcode", "GCODE", "G-code", "Gcode.Utils" };

    foreach (string file in Directory.GetFiles(appRoot, "*", SearchOption.AllDirectories))
    {
        if (file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
            || file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
            || !extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
        {
            continue;
        }

        string source = File.ReadAllText(file);
        foreach (string token in prohibited)
        {
            AssertTrue(!source.Contains(token),
                Path.GetFileName(file) + " must not contain removed G-code token " + token);
        }
    }

    string[] removedFiles =
    {
        "GcodeCoordinateService.cs",
        "GcodeDocumentService.cs",
        "GcodeLineSanitizer.cs",
        "NcGcodeCleaner.cs"
    };

    foreach (string file in removedFiles)
    {
        AssertTrue(!File.Exists(Path.Combine(appRoot, file)),
            file + " must be removed from the DXF-only app.");
    }
}
```

- [ ] **Step 2: Run tests and verify the whole-app contract fails**

Run the Debug test rebuild and executable.

Expected: FAIL because G-code state, services and project references still exist.

- [ ] **Step 3: Remove G-code/WCS settings and state**

Delete these fields and methods from `Form1.cs`:

- `activeWcs`
- `wcsOffsetX`
- `wcsOffsetY`
- `ApplyGcodeSettingsAsync`
- `ApplyWcsSettingsAsync`
- `SyncWcsOffsetsToUi`
- `GetWcsIndex`

Stop reading and writing these old keys:

- `rapidSpeed`
- `gcodeSpeedM3`
- `activeWcs`
- every `wcsG54X` through `wcsG59Y`

Unknown keys remain harmless because `LoadSettingsFromFile` already ignores keys without a matching case.

Delete from `WpfUiState`:

- `RawGcodeText`
- `GcodeSpeedM3Input`
- `RapidSpeedInput`
- `ActiveWcs`
- `WcsOffsetXInput`
- `WcsOffsetYInput`
- `WcsOffsets`
- `SelectWcsCommand`
- `SetWcsCommand`
- `WcsOffsetViewModel`

- [ ] **Step 4: Simplify display and process-row projection to DXF**

Change `CadDisplayDocumentBuilder.Build` to:

```csharp
public static CadDocumentService.CadLoadResult Build(
    CadDocumentService.CadLoadResult source,
    double offsetX,
    double offsetY,
    CancellationToken cancellationToken)
```

Use one offset for every primitive:

```csharp
bool anyOffset = Math.Abs(offsetX) > 1e-9 || Math.Abs(offsetY) > 1e-9;
```

Delete `HasAnyOffset`, `GetDisplayOffset` and all WCS arrays. Keep cancellation checks, preview sampling and bounds rebuilding.

In `Form1.StatePublisher.cs`:

- call `CadDisplayDocumentBuilder.Build(rawDoc, snapOx, snapOy, cancellationToken)`;
- remove `snapIsGcodeKind`, raw editor text, rapid/G-code speed snapshots and WCS snapshots;
- set `ui.FileKind` to the current DXF state;
- make process-row view models use `offsetX/offsetY`, except the existing home `0;0` row which remains unshifted;
- reduce `PublishProcessRowWindowState` and `BuildProcessRowViewModelWindow` parameters to DXF-only values;
- simplify or delete the duplicate display-offset helpers that no longer have callers.

- [ ] **Step 5: Remove WCS metadata from CAD/process models**

Delete `WcsIndex` from:

- `CadDocumentService.CadPrimitiveData`
- `Form1.ProcessRow`

Stop copying/assigning it in:

- `CadPreviewBuilder`
- `CadPathSelection.ReversePrimitiveForPath`
- `Form1.DxfHandler`
- `Form1.StatePublisher`

Change `CadPathSelection.GroupConnectedPaths` to:

```csharp
public static List<List<CadDocumentService.CadPrimitiveData>> GroupConnectedPaths(
    List<CadDocumentService.CadPrimitiveData> primitives,
    CancellationToken cancellationToken = default(CancellationToken))
```

Use the existing X/Y key:

```csharp
string KeyOf(CadDocumentService.CadCoordinate point)
    => string.Format(
        CultureInfo.InvariantCulture,
        "{0:0.000}|{1:0.000}",
        point.X,
        point.Y);
```

Update the cancellation reflection test to expect `(List<CadPrimitiveData>, CancellationToken)` and invoke it with two arguments.

- [ ] **Step 6: Delete modules and dependency**

Delete:

```text
src/DACDT_2026.App/GcodeCoordinateService.cs
src/DACDT_2026.App/GcodeDocumentService.cs
src/DACDT_2026.App/GcodeLineSanitizer.cs
src/DACDT_2026.App/NcGcodeCleaner.cs
```

Remove their `<Compile Include>` entries, remove the `Gcode.Utils` reference from `DACDT_2026.csproj`, and remove its package entry from `packages.config`.

Remove the linked G-code helper entries from the test project. Delete these obsolete test calls and methods from `Program.cs`:

- `CleansMastercamNcAndNormalizesLaserCommands`
- `SplitsMastercamModalCodesFromMotionLine`
- `GcodeLineSanitizerAcceptsTrailingDecimalPoint`
- `PreservesLeadingDecimalArcOffsets`
- `DropsZOnlyMovesFromMastercamNc`
- `MovesLaserOnFromRapidToFirstCutMove`
- `ConvertsCutterCompLeadInToRapidPositioning`
- `ConvertsCutterCompLeadOutToRapidPositioning`
- `PreservesSupportedArcAndMotionCommands`

- [ ] **Step 7: Build, fix only compiler-confirmed dangling references, and run tests**

Run:

```powershell
dotnet msbuild "tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj" /t:Rebuild /p:Configuration=Debug /v:minimal
dotnet msbuild "src\DACDT_2026.App\DACDT_2026.csproj" /t:Rebuild /p:Configuration=Debug /v:minimal
& "tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe"
```

Expected: both builds succeed and tests print `All tests passed.`

- [ ] **Step 8: Commit**

```powershell
git add -A src/DACDT_2026.App tests/DACDT_2026.Tests
git commit -m "chore: remove G-code modules and dependency"
```

---

### Task 6: Final Regression and Release Rebuild

**Files:**

- Verify: all files changed in Tasks 1-5
- Test: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**

- Verifies: DXF import → preview/processRows → QD75/Ring Buffer remains intact.
- Verifies: no production G-code/WCS entry point or dependency remains.

- [ ] **Step 1: Scan production source for removed functionality**

Run:

```powershell
rg -n -S "Gcode|GCODE|G-code|Gcode\.Utils|NewGcode|SaveGcode|PreviewGcode|WcsIndex|G54-G59" "src/DACDT_2026.App" -g "*.cs" -g "*.xaml" -g "*.csproj" -g "*.config"
```

Expected: no output.

- [ ] **Step 2: Confirm retained PLC/DXF mechanisms still exist**

Run:

```powershell
rg -n "processRows|QD75RingBufferRunner|HandleImportDxfAsync|BuildDxfProcessRows|Import DXF|Test Area|Clear Buffer|Export QD75" "src/DACDT_2026.App"
```

Expected: matches remain for DXF import, internal rows, Ring Buffer and operator actions.

- [ ] **Step 3: Run the full test suite from a clean rebuild**

Run:

```powershell
dotnet msbuild "tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj" /t:Rebuild /p:Configuration=Release /v:minimal
& "tests\DACDT_2026.Tests\bin\Release\DACDT_2026.Tests.exe"
```

Expected: `All tests passed.`

- [ ] **Step 4: Rebuild the WPF application in Release**

Run:

```powershell
dotnet msbuild "src\DACDT_2026.App\DACDT_2026.csproj" /t:Rebuild /p:Configuration=Release /v:minimal
```

Expected: build succeeds and creates `src\DACDT_2026.App\bin\Release\DACDT_2026.exe`.

- [ ] **Step 5: Check patch integrity and repository status**

Run:

```powershell
git diff --check
git status --short
```

Expected: `git diff --check` has no output; status contains only intentional implementation changes if a final cleanup has not yet been committed.

- [ ] **Step 6: Commit final compiler-driven cleanup if present**

If Step 5 reports intentional uncommitted cleanup:

```powershell
git add -A
git commit -m "test: verify DXF-only release build"
```

If the tree is already clean, do not create an empty commit.
