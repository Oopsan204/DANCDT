# Responsive Ring Buffer Monitor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Giữ toàn bộ ứng dụng phản hồi khi stream chương trình CAD lớn qua ring buffer.

**Architecture:** Khởi tạo và refill ring buffer trên thread pool, nhưng chỉ bật RUN sau khi 599 điểm đầu đã được nạp thành công. Dashboard và Monitor dùng `DispatcherTimer` 100 ms để gom các yêu cầu cuộn, chỉ cuộn tới dòng mới nhất.

**Tech Stack:** C# 7.3, .NET Framework 4.8, WPF, Mitsubishi PLC communication, bộ kiểm thử console `DACDT_2026.Tests`.

## Global Constraints

- Không thay đổi nội dung hoặc thứ tự dữ liệu gửi PLC.
- Giữ ring buffer 600 điểm, hai vùng 300/299 điểm và JUMP tại điểm 600.
- Giữ chu kỳ đọc Md.44 là 50 ms.
- Giữ bảng Monitor, đánh dấu dòng chạy, tiến độ và sự kiện hiện có.

---

### Task 1: Chuyển ring buffer khỏi UI thread

**Files:**
- Modify: `tests/DACDT_2026.Tests/Program.cs`
- Modify: `src/DACDT_2026.App/QD75RingBufferRunner.cs`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs`

**Interfaces:**
- Consumes: `QD75RingBufferRunner`, `LoadInitialBuffer()`, `MonitorMd44AndRefillAsync(CancellationToken)`.
- Produces: `Task<bool> StartAsync()` hoàn tất sau khi initial load sẵn sàng; monitor tiếp tục chạy nền.

- [ ] **Step 1: Viết kiểm thử thất bại**

Thêm `LargeRingBufferRunsPlcIoOutsideUiThread();` vào `Main`, rồi thêm kiểm thử
đọc source:

```csharp
private static void LargeRingBufferRunsPlcIoOutsideUiThread()
{
    string runner = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "QD75RingBufferRunner.cs"));
    string handler = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
    string start = ExtractMethodBody(runner, "public async Task<bool> StartAsync");
    string monitor = ExtractMethodBody(
        runner, "private async Task MonitorMd44AndRefillAsync");

    AssertTrue(start.Contains(
            "await Task.Run(() => LoadInitialBuffer(), cts.Token).ConfigureAwait(false);"),
        "Ring initial PLC writes must run outside the UI thread.");
    AssertTrue(start.Contains("_ = Task.Run(() => MonitorAndFinalizeAsync());"),
        "Ring monitoring and refill must be launched on the thread pool.");
    AssertTrue(monitor.Contains(
            "await Task.Delay(PollIntervalMs, ct).ConfigureAwait(false);"),
        "Ring polling continuations must not capture the WPF UI context.");
    AssertTrue(handler.Contains(
            "bool ringReady = await ringRunner.StartAsync();"),
        "The send workflow must await ring initialisation.");
    AssertTrue(handler.Contains("if (!ringReady)")
        && handler.Contains("return false;"),
        "RUN must remain disabled when ring initialisation fails.");
}
```

- [ ] **Step 2: Xác nhận RED**

```powershell
dotnet msbuild tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj -t:Build -p:Configuration=Debug -v:minimal
tests/DACDT_2026.Tests/bin/Debug/DACDT_2026.Tests.exe
```

Expected: FAIL tại `Ring initial PLC writes must run outside the UI thread.`

- [ ] **Step 3: Triển khai tối thiểu**

Đổi `StartAsync` thành `Task<bool>`. Dùng:

```csharp
await Task.Run(() => LoadInitialBuffer(), cts.Token).ConfigureAwait(false);
_ = Task.Run(() => MonitorAndFinalizeAsync());
return true;
```

Thêm `MonitorAndFinalizeAsync()` để bắt lỗi, phát sự kiện và đặt
`IsRunning = false`. Thêm `.ConfigureAwait(false)` vào `Task.Delay` của vòng
monitor. Tại `HandleSendCadXAsync`, dùng:

```csharp
bool ringReady = await ringRunner.StartAsync();
if (!ringReady)
    return false;
```

trước khi bật `ui.IsStartActionEnabled`.

- [ ] **Step 4: Xác nhận GREEN**

Chạy lại lệnh ở Step 2. Expected: `All tests passed.`

- [ ] **Step 5: Commit**

```powershell
git add tests/DACDT_2026.Tests/Program.cs src/DACDT_2026.App/QD75RingBufferRunner.cs src/DACDT_2026.App/Form1.DxfHandler.cs
git commit -m "fix: move PLC ring streaming off UI thread"
```

### Task 2: Gom yêu cầu tự cuộn bảng chương trình

**Files:**
- Modify: `tests/DACDT_2026.Tests/Program.cs`
- Modify: `src/DACDT_2026.App/Views/DashboardView.xaml.cs`
- Modify: `src/DACDT_2026.App/Views/MonitorView.xaml.cs`

**Interfaces:**
- Consumes: `WpfUiState.ActiveProgramIndex`, `ProgramRows`, `ProgramGrid`.
- Produces: `QueueActiveProgramScroll()` và `ActiveProgramScrollTimer_Tick(...)` trong mỗi view.

- [ ] **Step 1: Viết kiểm thử thất bại**

Thêm `ProgramMonitorAutoScrollIsLatestOnlyAndThrottled();` vào `Main`, rồi kiểm
tra cả hai source view:

```csharp
private static void ProgramMonitorAutoScrollIsLatestOnlyAndThrottled()
{
    string[] files = { "DashboardView.xaml.cs", "MonitorView.xaml.cs" };
    foreach (string file in files)
    {
        string source = File.ReadAllText(GetRepositoryPath(
            "src", "DACDT_2026.App", "Views", file));
        AssertTrue(source.Contains("DispatcherTimer activeProgramScrollTimer"),
            file + " must use one reusable auto-scroll timer.");
        AssertTrue(source.Contains("TimeSpan.FromMilliseconds(100)"),
            file + " must limit auto-scroll work to 10 updates per second.");
        AssertTrue(source.Contains("QueueActiveProgramScroll();"),
            file + " must coalesce active-row changes.");
        AssertTrue(source.Contains("activeProgramScrollTimer.Stop();")
            && source.Contains("activeProgramScrollPending = false;"),
            file + " must consume only the latest pending scroll.");
        AssertTrue(!source.Contains(
                "Dispatcher.BeginInvoke(new Action(() => ProgramGrid.ScrollIntoView(activeRow)))"),
            file + " must not queue one Dispatcher operation per active row.");
    }
}
```

- [ ] **Step 2: Xác nhận RED**

Chạy bộ test ở Task 1 Step 2. Expected: FAIL tại
`DashboardView.xaml.cs must use one reusable auto-scroll timer.`

- [ ] **Step 3: Triển khai tối thiểu**

Trong cả hai view, tạo một `DispatcherTimer` 100 ms và cờ
`activeProgramScrollPending`. Các sự kiện DataContext, PropertyChanged và
CollectionChanged gọi `QueueActiveProgramScroll()`. Tick dừng timer, xóa cờ và
gọi trực tiếp `ScrollActiveProgramRow()`.

- [ ] **Step 4: Xác nhận GREEN và build WPF**

Chạy bộ test ở Task 1 Step 2. Expected: `All tests passed.`

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'src\DACDT_2026.App\DACDT_2026.csproj' /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /v:minimal
```

Expected: exit code `0` và sinh `src/DACDT_2026.App/bin/Release/DACDT_2026.exe`.

- [ ] **Step 5: Rà soát và commit**

```powershell
git diff --check
git diff -- src/DACDT_2026.App/QD75RingBufferRunner.cs src/DACDT_2026.App/Form1.DxfHandler.cs src/DACDT_2026.App/Views/DashboardView.xaml.cs src/DACDT_2026.App/Views/MonitorView.xaml.cs tests/DACDT_2026.Tests/Program.cs
git add tests/DACDT_2026.Tests/Program.cs src/DACDT_2026.App/Views/DashboardView.xaml.cs src/DACDT_2026.App/Views/MonitorView.xaml.cs
git commit -m "perf: throttle active program auto-scroll"
```
