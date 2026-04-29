# dotnet run --project GantrySCADA.csproj -c Debug

# GantrySCADA - Tài liệu Dự án Chi tiết

## 📋 Mục lục

1. [Tổng quan dự án](#tổng-quan-dự-án)
2. [Cấu trúc file và liên kết](#cấu-trúc-file-và-liên-kết)
3. [Các tính năng chính](#các-tính-năng-chính)
4. [MainViewModel - Core MVVM](#mainviewmodel---core-mvvm)
5. [Dashboard - Giao diện điều khiển](#dashboard---giao-diện-điều-khiển)
6. [Telemetry - Giám sát dữ liệu](#telemetry---giám-sát-dữ-liệu)
7. [LogMonitor - Theo dõi nhật ký](#logmonitor---theo-dõi-nhật-ký)
8. [MainLayout - Navigation &amp; Highlighting](#mainlayout---navigation--highlighting)
9. [Luồng giao tiếp dữ liệu](#luồng-giao-tiếp-dữ-liệu)
10. [Thông tin PLC](#thông-tin-plc)
11. [Tính năng DXF Trajectory (CAM)](#tính-năng-dxf-trajectory-cam)

---

## 🎯 Tổng quan dự án

**GantrySCADA** là ứng dụng **WPF/Blazor** cho phép điều khiển và giám sát hệ thống **Gantry Robot** thông qua **PLC (Programmable Logic Controller)**.

### Thành phần chính

- **.NET 8.0 WPF**: Cửa sổ chính của ứng dụng
- **Blazor WebView**: Nhúng giao diện Blazor Razor vào WPF
- **MVVM Pattern**: Tách biệt logic (ViewModel) với giao diện (View)
- **PLC Communication**: Kết nối TCP/IP với PLC để đọc/ghi dữ liệu

### Công nghệ sử dụng

```
.NET 8.0 WPF
├── Blazor WebView (Microsoft.AspNetCore.Components.WebView.Wpf 8.0.82)
├── MVVM Toolkit (CommunityToolkit.Mvvm 8.4.2)
├── Custom PLC Library (NVKProject.PLC.dll)
└── Custom Logger (NVKProject.Logger.dll)
```

---

## 🗂️ Cấu trúc file và liên kết

```
GantrySCADA/
│
├── 🔵 MainWindow.xaml / MainWindow.xaml.cs
│   └─ Cửa sổ WPF chính - Host Blazor WebView
│      └─ Gọi: BlazorApp.razor
│
├── 🟢 App.xaml / App.xaml.cs
│   └─ Khởi động ứng dụng - Dependency Injection
│      └─ Đăng ký: MainViewModel (Singleton)
│      └─ Xây dựng ServiceProvider
│
├── 🔶 MainViewModel.cs
│   └─ **CORE**: Logic điều khiển PLC
│      ├─ Quản lý kết nối PLC
│      ├─ Đọc/ghi dữ liệu memory (D, M, X, Y)
│      └─ Background Monitor Thread (100Hz)
│
├── 🔷 BlazorApp.razor
│   └─ Root component của Blazor
│      └─ Nhúng: MainLayout.razor
│
├── 🟦 MainLayout.razor
│   └─ **Shared layout với Dynamic Navigation**
│      ├─ Inject: NavigationManager
│      ├─ IsActive() method - Kiểm tra current route
│      ├─ NavLink: Dashboard (/) - Tự động highlight
│      ├─ NavLink: Logs (logmonitor) - Tự động highlight
│      ├─ NavLink: Telemetry (telemetry) - Tự động highlight
│      └─ Subscribe LocationChanged → StateHasChanged (highlight update)
│
├── Pages/
│   │
│   ├── 🔴 Dashboard.razor
│   │   └─ **Giao diện chính** - Điều khiển và giám sát
│   │      ├─ @inject MainViewModel
│   │      ├─ Kết nối PLC (IP, Port)
│   │      ├─ Jog() & StopJog() - **FULLY IMPLEMENTED**
│   │      │  ├─ X±: m3000/m3001 | Y±: m3002/m3003 | Z±: m3004/m3005
│   │      │  └─ Log: ViewModel.AddLog() cho mỗi jog action
│   │      ├─ Position Cards (X, Y, Z từ arr_R32)
│   │      ├─ Velocity Slider (0.0 - 5.0 m/s)
│   │      ├─ System State indicator
│   │      └─ Memory Stream debugger (arr_R_V)
│   │
│   ├── 🔵 Telemetry.razor
│   │   └─ **Giám sát thời gian thực**
│   │      ├─ Real-Time Monitor (D/M/X/Y addresses)
│   │      ├─ Write Control (ghi D-words)
│   │      ├─ Action History Log (max 200 entries)
│   │      └─ Auto-Refresh Timer (250ms)
│   │
│   └── 🟢 LogMonitor.razor (NEW)
│       └─ **Theo dõi nhật ký hệ thống toàn bộ**
│          ├─ @inject MainViewModel
│          ├─ Subscribe: ViewModel.LogAdded event
│          ├─ Shared: ViewModel.AllLogs (centralized)
│          ├─ Three-panel: UI / PC / PLC streams
│          ├─ Search & Filter: status, source, query
│          ├─ Terminal Input: Gửi lệnh PLC
│          ├─ Tab Strip: ALL / UI / PC / PLC
│          ├─ Controls: Pause/Resume, Export, Clear
│          └─ Expandable detail modal: Tag, Delete, Timestamp
│
├── wwwroot/
│   └─ index.html
│      └─ Host page cho Blazor
│
├── _Imports.razor
│   └─ Shared using statements cho Razor pages
│
└── GantrySCADA.csproj
    └─ Project config
```

### 🔗 Liên kết giữa các file

```
MainWindow.xaml.cs
    ↓ (Host)
BlazorWebView
    ↓ (Selector: #app)
BlazorApp.razor
    ↓ (Layout)
MainLayout.razor
    ├─ (Subscribe NavigationManager.LocationChanged)
    ├─ (Dynamic NavLink highlighting via IsActive())
    │
    └─ (Body)
        ├─ Dashboard.razor
        ├─ Telemetry.razor
        └─ LogMonitor.razor
            ↓ (@inject)
        MainViewModel
            ├─ AllLogs (shared centralized log collection)
            ├─ LogAdded event (notify on new log)
            ├─ AddLog() method (add to AllLogs)
            ├─ JogStart/JogStop methods
            └─ (Tương tác PLC)
                ↓
            ePLCControl (NVKProject.PLC.dll)
```

---

## ✨ Các tính năng chính

### 1. 🔌 Kết nối và Quản lý Kết nối

**File**: `MainViewModel.cs`

```csharp
// Thuộc tính kết nối
IpAddress       // "192.168.3.39" - IP của PLC
Port            // 3000 - Cổng kết nối
NetworkNo       // 0 - Số mạng
StationNo       // 0 - Số station
StationPLCNo    // 255 - Số PLC
Status          // true/false - Trạng thái kết nối
```

**Method**:

- `ConnectPLC()` - Kết nối đến PLC
- `DisconnectPLC()` - Đóng kết nối
- `Monitor()` - Background thread theo dõi và đồng bộ dữ liệu (100Hz = 10ms)

### 2. 🎮 Điều khiển chuyển động Jog (Dashboard) - **FULLY IMPLEMENTED**

**File**: `Dashboard.razor` + `MainViewModel.cs`

**Jog Controls UI**: 4 nút điều khiển XY + 2 nút Z

```
        ↑ Y-
    X- XY X+
        ↓ Y+

        ↑ Z+
        ↓ Z-
```

**Mark Address Mapping** (Quy ước từ PLC):

| Hướng    | Mark Address | Trong ViewModel |
| ---------- | ------------ | --------------- |
| X+ (Right) | m3000        | 3000            |
| X- (Left)  | m3001        | 3001            |
| Y+ (Down)  | m3002        | 3002            |
| Y- (Up)    | m3003        | 3003            |
| Z+ (Up)    | m3004        | 3004            |
| Z- (Down)  | m3005        | 3005            |

**Methods trong Dashboard.razor**:

```csharp
private void Jog(char axis, bool dir)
{
    // Map axis + direction → mark address
    int markAddress = axis switch
    {
        'X' => dir ? 3001 : 3000,  // X-=3001, X+=3000
        'Y' => dir ? 3003 : 3002,  // Y-=3003, Y+=3002
        'Z' => dir ? 3005 : 3004,  // Z-=3005, Z+=3004
        _ => -1
    };
  
    if (markAddress >= 0)
    {
        // Log user action
        string dirName = (axis, dir) switch { ... };
        ViewModel.AddLog("UI", "info", $"Jog {dirName} pressed");
      
        // Ghi mark = 1 vào PLC
        ViewModel.JogStart(markAddress);
    }
}

private void StopJog(char axis)
{
    // Log release
    ViewModel.AddLog("UI", "info", $"Jog {axis} released");
  
    // Dừng tất cả marks cho axis này
    switch (axis)
    {
        case 'X':
            ViewModel.JogStop(3000);
            ViewModel.JogStop(3001);
            break;
        case 'Y':
            ViewModel.JogStop(3002);
            ViewModel.JogStop(3003);
            break;
        case 'Z':
            ViewModel.JogStop(3004);
            ViewModel.JogStop(3005);
            break;
    }
}
```

**Methods trong MainViewModel.cs**:

```csharp
public void JogStart(int markAddress)
{
    try
    {
        if (ePLC != null && ePLC.IsConnected)
        {
            int[] bitValue = { 1 };
            // Ghi mark bit = 1 vào PLC
            ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Bit, 
                                  ePLCControl.DeviceName.M, 
                                  $"{markAddress}", 
                                  bitValue);
        }
    }
    catch { }
}

public void JogStop(int markAddress)
{
    try
    {
        if (ePLC != null && ePLC.IsConnected)
        {
            int[] bitValue = { 0 };
            // Ghi mark bit = 0 vào PLC
            ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Bit, 
                                  ePLCControl.DeviceName.M, 
                                  $"{markAddress}", 
                                  bitValue);
        }
    }
    catch { }
}
```

**Luồng hoạt động**:

1. Người dùng nhấn nút Jog → `Jog(axis, dir)`
2. Xác định mark address
3. Ghi log vào ViewModel.AllLogs
4. Gọi `ViewModel.JogStart(markAddress)` → Ghi bit=1 vào PLC
5. PLC nhận lệnh → Axis chuyển động
6. Người dùng thả nút → `StopJog(axis)`
7. Ghi log "released"
8. Gọi `ViewModel.JogStop()` → Ghi bit=0 vào PLC
9. PLC nhận → Axis dừng

**Velocity Control**: Slider điều chỉnh vận tốc (0.0 - 5.0 m/s)

### 5. 🎯 Custom Memory Stream (REAL-TIME MONITOR) - **NEW FEATURE**

**File**: `Dashboard.razor` + `MainViewModel.cs`

**Tính năng**:

- User có thể thêm bất kỳ địa chỉ nào (D, M, X, Y) để đọc thời gian thực
- Refresh 100Hz từ Monitor thread (RefreshCustomMemory)
- Hiển thị currentValue và LastUpdate timestamp
- Add/Remove entry dynamically

**Custom Memory Entry Model**:

```csharp
public class CustomMemoryEntry
{
    public string AddrType { get; set; } = "D";      // D, M, X, Y
    public int AddrIndex { get; set; }
    public int CurrentValue { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.Now;
}
```

**Methods**:

- `AddCustomMemoryEntry(addrType, addrIndex)` - Thêm address (log action)
- `RemoveCustomMemoryEntry(entry)` - Xoá address (log action)
- `RefreshCustomMemory()` - Cập nhật values (gọi trong Monitor loop 100Hz)

**UI Components (Dashboard)**:

- Input fields để nhập Address Type (D/M/X/Y) và Address Index
- List hiển thị entries với CurrentValue, LastUpdate, Delete button
- Real-time update từ Monitor thread

### 6. 📋 Centralized Logging System - **GLOBAL FEATURE**

**File**: `MainViewModel.cs` + `LogMonitor.razor`

**Tính năng**:

- **Global AllLogs**: Tất cả logs từ mọi page (Dashboard, Telemetry, LogMonitor) lưu trong ViewModel.AllLogs
- **LogAdded event**: Notify subscribers khi có log mới (real-time UI update)
- **Max 500 entries**: Auto remove oldest logs để tránh memory leak
- **Multi-source**: UI actions, PC operations, PLC events
- **Multi-status**: info, success, warning, error
- **Throttling**: Read/Write logs throttled 1 second để tránh spam (chạy 100Hz)

**LogItem Model**:

```csharp
public class LogItem
{
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = "UI";        // UI / PC / PLC
    public string Message { get; set; }
    public string Status { get; set; } = "info";      // info/success/warning/error
    public string Detail { get; set; }                // Optional: exception type, etc
    public bool Tagged { get; set; }
    public bool IsNewest { get; set; }
}
```

**Logging Points**:

```csharp
// Connection lifecycle
AddLog("PLC", "info", $"Connection attempt → {IpAddress}:{Port}");
AddLog("PLC", "success", "PLC connection established");
AddLog("PLC", "error", "PLC connection failed");
AddLog("PLC", "warning", "Connection closed by user");

// Custom Memory operations
AddLog("UI", "info", $"Added custom memory: D4000");
AddLog("UI", "info", $"Removed custom memory: D4000");

// Monitor cycle (throttled 1 second to avoid flooding)
AddLog("PC", "success", $"Read D{D_R_V}({Length} words) + D32-blocks → OK", "Monitor cycle");
AddLog("PC", "success", $"Write D{D_W_V}(99 words) + D{D_W_P}(99 words) → OK", "Monitor cycle");
AddLog("PC", "error", $"Read failed: {exception.Message}", "AccessViolationException");
```

### 7. 📊 Hiển thị dữ liệu vị trí (Dashboard)

**File**: `Dashboard.razor`

**Position Cards** - Hiển thị 3 giá trị 32-bit từ `arr_R32`:

```
┌────────────┐  ┌────────────┐  ┌────────────┐
│ Position X │  │ Position Y │  │ Position Z │
│ arr_R32[0] │  │ arr_R32[1] │  │ arr_R32[2] │
└────────────┘  └────────────┘  └────────────┘
```

**Memory Stream** - Hiển thị 10 giá trị đầu tiên từ `arr_R_V`:

```
D4000 → arr_R_V[0]
D4001 → arr_R_V[1]
...
D4009 → arr_R_V[9]
```

### 4. 📡 Giám sát thời gian thực (Telemetry)

**File**: `Telemetry.razor`

**Real-Time Monitor Section**:

- Danh sách địa chỉ PLC (D, M, X, Y)
- Hiển thị giá trị thời gian thực
- Timestamp cập nhật (ms ago)
- Auto-refresh ON/OFF

**Write Control Section**:

- Danh sách lệnh ghi vào PLC
- Validation (0-65535 cho Word)
- Status indicator (OK/ERR)

**Action History Log**:

- Ghi lại tất cả hoạt động READ/WRITE
- Timestamp, địa chỉ, giá trị, trạng thái
- Max 200 entries, có thể xoá

---

## 🏗️ MainViewModel - Core MVVM

---

## 🔍 Detected Features (Auto-generated - scanned 2026-04-18)

This section was generated by scanning the repository source files and summarizes the app's current capabilities and where they are implemented.

- **PLC connection & monitor loop**: connect/disconnect to PLC, start/stop a background `Monitor()` loop (100Hz) — [MainViewModel.cs](MainViewModel.cs)
- **Read cycle (device blocks & coordinate mapping)**: reads D-words, optional legacy 32-bit D32 blocks, bit registers (M/X/Y), and maps coordinate sources — [MainViewModel.ReadFeature.cs](MainViewModel.ReadFeature.cs)
- **Write cycle & pending write queue**: collects pending writes, attempts bit/word/block writes with fallbacks, clears queue on success — [MainViewModel.WriteFeature.cs](MainViewModel.WriteFeature.cs)
- **Jog / Motion controls**: queued M-register writes for JogStart/JogStop with safety checks, position helpers and bit manipulation utilities — [MainViewModel.MotionAndLoggingFeature.cs](MainViewModel.MotionAndLoggingFeature.cs) and [Pages/Dashboard.razor](Pages/Dashboard.razor)
- **Custom memory monitoring**: user-addable address list (D/M/X/Y) with real-time refresh and per-entry timestamps — [MainViewModel.State.cs](MainViewModel.State.cs) and [Pages/Dashboard.razor](Pages/Dashboard.razor)
- **Centralized logging system**: `AllLogs`, `LogItem`, `AddLog()` and `LogAdded` event; UI pages subscribe for real-time display — [MainViewModel.MotionAndLoggingFeature.cs](MainViewModel.MotionAndLoggingFeature.cs) and [Pages/LogMonitor.razor](Pages/LogMonitor.razor)
- **Dashboard UI**: WPF-hosted Blazor dashboard with connection card, jog controls, position cards, velocity slider, and memory stream debugger — [Pages/Dashboard.razor](Pages/Dashboard.razor)
- **Telemetry UI**: real-time monitor and write control with action history log and auto-refresh — [Pages/Telemetry.razor](Pages/Telemetry.razor)
- **LogMonitor UI**: three-panel log viewer (UI / PC / PLC), filters, terminal input, export/clear controls — [Pages/LogMonitor.razor](Pages/LogMonitor.razor)
- **Position formatting & scaling**: position scaling, offsets, decimal formatting helpers and pos unit settings — [MainViewModel.State.cs](MainViewModel.State.cs)
- **Address/Device helpers**: utilities for normalizing address types, reading coordinate source values and bit-word conversions — [MainViewModel.State.cs](MainViewModel.State.cs) and [PlcBitHelper.cs](PlcBitHelper.cs)
- **WPF + Blazor integration**: `MainWindow.xaml` hosts `BlazorWebView` → `BlazorApp.razor` → `MainLayout.razor` → pages — [MainWindow.xaml](MainWindow.xaml) and [BlazorApp.razor](BlazorApp.razor)
- **NuGet dependencies**: `netDxf` package was added to the project recently (see project file) — [GantrySCADA.csproj](GantrySCADA.csproj)

---

If you want, I can:

- expand any bullet into a full subsection with code snippets and line references,
- add a short "How to run" or "Developer setup" section (build/run commands), or
- create a small sample demonstrating reading a DXF file with the installed `netDxf` package.

### 📌 Fields (Biến Private)

```csharp
private ePLCControl ePLC              // Instance PLC
private int _ValuePLC = 1
private int _Length = 99              // Số từ để đọc
private int _StartAddress = 700
private string ipAddress = "192.168.3.39"
private int port = 3000
private int networkNo = 0
private int stationNo = 0
private int stationPLCNo = 255
private bool status                   // Trạng thái kết nối

// Địa chỉ register
public int D_R_V = 4000              // Read Velocity base
public int D_W_V = 5000              // Write Velocity base
public int D_W_P = 3000              // Write Position base

// Mảng dữ liệu
private int[] _arr_R32 = new int[6]  // 6 giá trị 32-bit đọc từ PLC
public int[] arr_W_Position          // 99 từ ghi position
private int[] _arr_R_V               // 99 từ đọc velocity
public int[] arr_W_V                 // 99 từ ghi velocity

// Config 32-bit read (CONFIGURABLE)
private int _d32Base1 = 1000         // Base địa chỉ khối 1 (configurable)
private int _d32Base2 = 2000         // Base địa chỉ khối 2 (configurable)
private int _dReadEnable = 3000      // Enable flag address (configurable)

// Throttle excessive logs (Read/Write 100x/sec)
private DateTime _lastWriteLogTime = DateTime.MinValue;
private DateTime _lastReadLogTime = DateTime.MinValue;

// Global log storage (CENTRALIZED)
public class LogItem
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Source { get; set; } = "UI";        // "UI" / "PC" / "PLC"
    public string Message { get; set; } = "";
    public string Status { get; set; } = "info";      // "info"/"success"/"warning"/"error"
    public string Detail { get; set; } = "";
    public bool Tagged { get; set; }
    public bool IsNewest { get; set; }
}
private List<LogItem> _allLogs = new();
public List<LogItem> AllLogs => _allLogs;

// Event for new log notification
public event EventHandler<LogItem>? LogAdded;

// Custom Memory Entry: for user-defined address reading
public class CustomMemoryEntry
{
    public string AddrType { get; set; } = "D";      // D, M, X, Y
    public int AddrIndex { get; set; }
    public int CurrentValue { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.Now;
}

private List<CustomMemoryEntry> _customMemoryEntries = new();
public List<CustomMemoryEntry> CustomMemoryEntries => _customMemoryEntries;
```

### 🔄 Properties (Thuộc tính Public)

```csharp
// Cấu hình kết nối
public string IpAddress         // RW - IP PLC, notify UI
public int Port                 // RW - Port PLC, notify UI
public int NetworkNo            // RW - Số mạng
public int StationNo            // RW - Số station, notify UI
public int StationPLCNo         // RW - Số PLC, notify UI
public bool Status              // RW - Trạng thái, notify UI

// Cấu hình dữ liệu
public int Length               // RW - Số từ đọc, notify UI
public int StartAddress         // RW - Địa chỉ bắt đầu, notify UI
public int D32Base1             // RW - Base1 32-bit, notify UI
public int D32Base2             // RW - Base2 32-bit, notify UI
public int DReadEnable          // RW - Enable flag addr, notify UI

// Dữ liệu
public int[] arr_R_V            // RO - Mảng velocity đọc
                                // Private setter, SetProperty() để notify
public int[] arr_R32            // RW - Mảng 32-bit, SetProperty() để notify
```

### ⚙️ Commands

```csharp
public ICommand ConnectCommand      // RelayCommand → ConnectPLC()
public ICommand DisconnectCommand   // RelayCommand → DisconnectPLC()
public ICommand TestReadCommand     // Placeholder
public ICommand TestWriteCommand    // Placeholder
public ICommand TestBit             // Placeholder
```

### 🔧 Methods

#### 1. **Constructor**

```csharp
public MainViewModel()
{
    ePLC = new ePLCControl();
    ConnectCommand = new RelayCommand(ConnectPLC);
    DisconnectCommand = new RelayCommand(DisconnectPLC);
    TestReadCommand = new RelayCommand(new Action(() => { }));
    TestWriteCommand = new RelayCommand(new Action(() => { }));
    TestBit = new RelayCommand(new Action(() => { }));
  
    // Seed initial logs
    AddLog("UI",  "info",    "Application started");
    AddLog("PC",  "info",    "MainViewModel initialized");
    AddLog("PC",  "info",    "Monitor thread started @ 100Hz");
}
```

Khởi tạo các command và seed initial logs.

#### 2. **ConnectPLC()**

```csharp
private void ConnectPLC()
{
    ePLC = new ePLCControl();
    ePLC.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
    ePLC.Open();
    Status = ePLC.IsConnected;
  
    AddLog("PLC", "info", $"Connection attempt → {IpAddress}:{Port}");
    AddLog("PLC", Status ? "success" : "error", 
           Status ? "PLC connection established" : "PLC connection failed");

    Thread t1 = new Thread(Monitor);
    t1.IsBackground = true;
    t1.Start();
}
```

- Tạo ePLCControl instance
- Cấu hình kết nối
- Log connection attempt
- Bắt đầu background thread

#### 2.1 **Disconn

    {
        Thread.Sleep(10);  // 100Hz (10ms interval)
        Status = ePLC.IsConnected;
        if (Status)
        {
            Read();                     // Đọc từ PLC
            RefreshCustomMemory();      // Cập nhật custom memory entries
            // Write();                 // (commented out)
        }
    }
}

```
- **Chạy ở background**: Không chặn UI
- **Tần số**: 100Hz (cứ 10ms chạy 1 lần)
- **Hoạt động**: 
  - Đọc dữ liệu từ PLC
  - Refresh custom memory entries
  - Write method hiện tại commented out

#### 3. **Monitor()** ⭐ Core Loop
```csharp
private void Monitor()
{
    while (Status)  // Lặp khi kết nối
    {
        Thread.Sleep(10);   // 100Hz (10ms interval)
        Status = ePLC.IsConnected;  // Kiểm tra kết nối
        if (Status)
        {
            Read();         // Đọc từ PLC
            Write();        // Ghi vào PLC
        }
    }
}
```

- **Chạy ở background**: Không chặn UI
- **Tần số**: 100Hz (cứ 10ms chạy 1 lần)
- **Vòng lặp**: Liên tục đọc/ghi dữ liệu

#### 4. **Read()** - Đọc dữ liệu từ PLC

```csharp
private void Read()configurable address (DReadEnable)
        int[] flag = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, 
                                          ePLCControl.DeviceName.D, 
                                          $"{DReadEnable}", 1);
        if (flag == null || flag.Length == 0)
        {
            arr_R_V = ePLC.ReadDeviceBlock(...);
            return;
        }

        if (flag[0] != 0)  // Flag enabled
        {
            // Đọc velocity array (D4000-D4099)
            arr_R_V = ePLC.ReadDeviceBlock(...);
          
            // Đọc 32-bit values từ configurable bases
            int[] b1 = ePLC.ReadDeviceBlock(..., D32Base1, 6);
            int[] b2 = ePLC.ReadDeviceBlock(..., D32Base2, 6);
          
            // Combine pairs: (low | high << 16)
            int[] newR32 = new int[6];
            if (b1 != null && b1.Length >= 6)
            {
                for (int i = 0; i < 3; i++)
                    newR32[i] = b1[i*2] | (b1[i*2+1] << 16);
            }
            if (b2 != null && b2.Length >= 6)
            {
                for (int i = 0; i < 3; i++)
                    newR32[3+i] = b2[i*2] | (b2[i*2+1] << 16);
            }
          
            arr_R32 = newR32;  // Trigger PropertyChanged
          
            // Log every 1 second to avoid flooding
            if ((DateTime.Now - _lastReadLogTime).TotalSeconds >= 1.0)
            {
                AddLog("PC", "success", $"Read D{D_R_V}({Length} words) + D32-blocks → OK", "Monitor cycle");
                _lastReadLogTime = DateTime.Now;
            }
        }
    }
    catch (Exception ex)
    {
        AddLog("PC", "error", $"Read failed: {ex.Message}", ex.GetType().Name);
        _lastReadLogTime = DateTime.Now;
    }
}
```

**Logic đọc**:

1. Đọc enable flag ở DReadEnable (configurable, default D3000)
2. Nếu flag != 0, đọc các khối:
   - arr_R_V: D4000-D4099 (velocity)
   - 32-bit: D32Base1, D32Base2 (configurable bases)
3. Combine 2 words thành 32-bit position
4. Log hoạt động (throttled 1 second)
5. Nếu flag != 0, đọc các khối:
   - arr_R_V: D4000-D4099 (velocity)
   - 32-bit: D1000-D1005, D2000-D2005
6. Combine 2 words thành 32-bit position
7. Update UI via PropertyChanged

#### 5. **Write()** - Ghi dữ liệu vào PLC

```csharp
private void Write()
{
    try
    {
        ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Word, 
                              ePLCControl.DeviceName.D, 
                              $"{D_W_V}", arr_W_V);
        ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Word, 
                              ePLCControl.DeviceName.D, 
                              $"{D_W_P}", arr_W_Position);
      
        // Log every 1 second to avoid flooding log window
        if ((DateTime.Now - _lastWriteLogTime).TotalSeconds >= 1.0)
        {
            AddLog("PC", "success", $"Write D{D_W_V}(99 words) + D{D_W_P}(99 words) → OK", "Monitor cycle");
            _lastWriteLogTime = DateTime.Now;
        }
    }
    catch (Exception ex)
    {
        AddLog("PC", "error", $"Write failed: {ex.Message}", ex.GetType().Name);
        _lastWriteLogTime = DateTime.Now;
    }
}
```

**Ghi vào**:

- D5000+: velocity commands (99 words)
- D3000+: position commands (99 words)
- **Lưu ý**: Method này hiện tại commented out trong Monitor() loop

**Throttling**: Log hoạt động mỗi 1 second để tránh spam (chạy 100Hz)

#### 6. **ReadDevice(int iAddress)** - Đọc 1 bit

```csharp
private bool ReadDevice(int iAddress)
{
    if ((iAddress - D_R_V) >= 0 && (iAddress - D_R_V) < arr_R_V.Length)
        return arr_R_V[iAddress - D_R_V] != 0;
    return false;
}
```

#### 7. **WriteDevice(int iAddress, bool value)** - Ghi 1 bit

```csharp
private void WriteDevice(int iAddress, bool value)
{
    if ((iAddress - D_W_V) >= 0 && (iAddress - D_W_V) < arr_W_V.Length)
        arr_W_V[iAddress - D_W_V] = value ? 1 : 0;
}
```

#### 8. **AddCustomMemoryEntry()** - Thêm custom memory address

```csharp
public void AddCustomMemoryEntry(string addrType, int addrIndex)
{
    try
    {
        var entry = new CustomMemoryEntry { AddrType = addrType, AddrIndex = addrIndex };
        CustomMemoryEntries.Add(entry);
        AddLog("UI", "info", $"Added custom memory: {addrType}{addrIndex}");
        OnPropertyChanged(nameof(CustomMemoryEntries));
    }
    catch (Exception ex)
    {
        AddLog("PC", "error", $"Failed to add entry: {ex.Message}");
    }
}
```

**Mục đích**: Cho phép user thêm địa chỉ tùy ý (D, M, X, Y) để đọc giá trị thời gian thực

#### 8.1 **RemoveCustomMemoryEntry()** - Xoá custom memory address

```csharp
public void RemoveCustomMemoryEntry(CustomMemoryEntry entry)
{
    try
    {
        CustomMemoryEntries.Remove(entry);
        AddLog("UI", "info", $"Removed custom memory: {entry.AddrType}{entry.AddrIndex}");
        OnPropertyChanged(nameof(CustomMemoryEntries));
    }
    catch (Exception ex)
    {
        AddLog("PC", "error", $"Failed to remove entry: {ex.Message}");
    }
}
```

#### 8.2 **RefreshCustomMemory()** - Cập nhật custom memory values

```csharp
public void RefreshCustomMemory()
{
    try
    {
        if (!Status || ePLC == null || CustomMemoryEntries.Count == 0)
            return;

        foreach (var entry in CustomMemoryEntries)
        {
            try
            {
                ePLCControl.DeviceName devName = entry.AddrType switch
                {
                    "M" => ePLCControl.DeviceName.M,
                    "X" => ePLCControl.DeviceName.X,
                    "Y" => ePLCControl.DeviceName.Y,
                    _ => ePLCControl.DeviceName.D
                };

                int[] result = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, 
                                                    devName, 
                                                    $"{entry.AddrIndex}", 1);
                if (result != null && result.Length > 0)
                {
                    entry.CurrentValue = result[0];
                    entry.LastUpdate = DateTime.Now;
                }
            }
            catch { }
        }

        // Trigger UI refresh
        OnPropertyChanged(nameof(CustomMemoryEntries));
    }
    catch (Exception ex)
    {
        AddLog("PC", "error", $"RefreshCustomMemory error: {ex.Message}");
    }
}
```

**Hoạt động**:

- Được gọi trong Monitor() loop (100Hz)
- Đọc giá trị của mỗi custom memory entry
- Update CurrentValue và LastUpdate timestamp
- Trigger UI refresh via PropertyChanged

```csharp
public void SetPosition(int iAddress, double value)
{
    int index = iAddress - D_W_P;
    SetCurrentPosition(arr_W_Position, index, value);
}
```

#### 9. **GetCurrentPosition(int[] arr, int index)** - Đọc position 32-bit

```csharp
public double GetCurrentPosition(int[] arr, int index)
{
    return arr[index] + (arr[index + 1] << 16);
}
```

Combine 2 words: `low + (high << 16)`

#### 10. **SetCurrentPosition(int[] arr, int index, double value)** - Ghi position 32-bit

```csharp
public void SetCurrentPosition(int[] arr, int index, double value)
{
    int v = (int)value;
    arr[index] = v & 0xFFFF;           // Low word
    arr[index + 1] = (v >> 16) & 0xFFFF;  // High word
}
```

#### 11. **Bit Operation Methods**

```csharp
public bool GetBit(int word, int bit)
{
    return ((word >> bit) & 1) == 1;
}

public int SetBit(int word, int bit, bool value)
{
    if (value) return word | (1 << bit);
    else return word & ~(1 << bit);
}
```

#### 12. **GetDataValue_() & SetDataValue_()** - Array bit helpers

```csharp
private int[] GetDataValue_(int[] arr, int index)
{
    // Lấy 16 bits từ word
    int word = arr[index];
    int[] bits = new int[16];
    for (int i = 0; i < 16; i++)
        bits[i] = (word >> i) & 1;
    return bits;
}

private void SetDataValue_(int[] arr, int index, int[] bits)
{
    // Ghi 16 bits vào word
    int word = 0;
    for (int i = 0; i < 16; i++)
        if (bits[i] == 1) word |= (1 << i);
    arr[index] = word;
}
```

### 🧮 Static Helper Class: PlcBitHelper

Cung cấp các utility cho bit/word conversion:

```csharp
BoolArrayToIntArray(bool[])           // bool[] → int[]
IntArrayToBoolArray(int[])            // int[] → bool[]
WordToBits(int word)                  // word → bool[16]
BitsToWord(bool[])                    // bool[16] → word
WordToBitString(int)                  // word → "1010101..."
BitStringToWord(string)               // "1010..." → word
GetBit(int word, int bitIndex)        // Lấy 1 bit
SetBit(int word, int bitIndex, bool)  // Ghi 1 bit
GetCurrentPosition(int[], int)        // 2 words → 32-bit
SetCurrentPosition(int[], int, int)   // 32-bit → 2 words
```

---

## 🎨 Dashboard - Giao diện điều khiển

**File**: `Pages/Dashboard.razor`
**Inject**: `MainViewModel`

### 🔗 Cấu trúc Layout

```
Grid 12 cột
├── Cột 4 (lg): Connection & Control
│   ├── System Connectivity (IP, Port, Connect button)
│   └── Manual Kinematic Jog (XY pad, Z buttons)
│
└── Cột 8 (lg): Data Display
    ├── Position Cards (X, Y, Z)
    ├── Velocity Slider
    ├── System State (Running/Stopped)
    └── Real-Time Memory Stream (arr_R_V[0-9])
```

### 📌 UI Components

#### 1. **System Connectivity Panel**

```html
Input: IpAddress (bind="ViewModel.IpAddress")
Input: Port      (bind="ViewModel.Port")
Button: Connect  (@onclick="Connect")
Button: Disconnect
Status Indicator: ViewModel.Status (green=connected, red=disconnected)
```

**Method gọi**:

```csharp
private void Connect()
{
    if (!ViewModel.Status)
    {
        ViewModel.ConnectCommand.Execute(null);  // Gọi ConnectPLC()
    }
}
```

#### 2. **XY Jog Control**

```
        ↑ Y-
    X- XY X+
        ↓ Y+
```

**Buttons**:

```csharp
@onmousedown="() => Jog('X', false)"   // X- (Left)
@onmousedown="() => Jog('X', true)"    // X+ (Right)
@onmousedown="() => Jog('Y', false)"   // Y- (Up)
@onmousedown="() => Jog('Y', true)"    // Y+ (Down)

@onmouseup="() => StopJog('X')"
@onmouseup="() => StopJog('Y')"
```

**Methods** (placeholder hiện tại):

```csharp
private void Jog(char axis, bool dir)
{
    // TODO: Write jog bit to PLC
    // Example: WriteDevice(5000 + (axis=='X' ? 0 : 1), true)
}

private void StopJog(char axis)
{
    // TODO: Clear jog bit
    // Example: WriteDevice(5000 + (axis=='X' ? 0 : 1), false)
}
```

#### 3. **Z Control** (Up/Down)

```csharp
@onmousedown="() => Jog('Z', false)"   // Z UP
@onmousedown="() => Jog('Z', true)"    // Z DOWN
@onmouseup="() => StopJog('Z')"
```

#### 4. **Position Cards**

```csharp
// Card X
@(ViewModel.arr_R32.Length > 0 ? ViewModel.arr_R32[0] : 0) mm

// Card Y
@(ViewModel.arr_R32.Length > 1 ? ViewModel.arr_R32[1] : 0) mm

// Card Z
@(ViewModel.arr_R32.Length > 2 ? ViewModel.arr_R32[2] : 0) mm
```

Hiển thị 3 giá trị 32-bit từ `arr_R32` (đọc từ D1000-D1005, D2000-D2005)

#### 5. **Velocity Slider**

```csharp
@code {
    private double VelocityVal = 1.5;  // Default 1.5 m/s
}

<input type="range" min="0" max="5.0" step="0.1" @bind="VelocityVal" />
```

Ghi `VelocityVal` vào PLC (D5000+ hoặc bit flag)

#### 6. **Memory Stream Debugger**

```csharp
@for (int i = 0; i < Math.Min(10, ViewModel.arr_R_V.Length); i++)
{
    <div>
        D@(ViewModel.D_R_V + i) → @ViewModel.arr_R_V[i]
    </div>
}
```

Hiển thị 10 giá trị đầu tiên từ `arr_R_V` (D4000-D4009)

---

## 📊 Telemetry - Giám sát dữ liệu

**File**: `Pages/Telemetry.razor`
**Inject**: `MainViewModel`

### 🔗 Layout

```
Grid 12 cột
├── Cột 5: Real-Time Monitor
│   ├── Header (Auto ON/OFF)
│   ├── Read Entries List
│   └── Add Address / Refresh
│
└── Cột 7: Write Control
    ├── Write Entries List
    └── Add Write Entry

Bottom: Action History Log
├── Log Header (Clear)
└── Log Table (Timestamp, Type, Address, Value, Status)
```

### 📌 Models

```csharp
public class ReadEntry
{
    public string AddrType { get; set; } = "D";  // D/M/X/Y
    public int AddrIndex { get; set; }
    public int Value { get; set; }
    public long UpdatedMs { get; set; }
    public bool JustUpdated { get; set; }
    public int PrevValue { get; set; }
}

public enum WriteStatus { None, OK, ERR }
public class PlcWriteItem
{
    public string AddrType { get; set; } = "D";
    public int AddrIndex { get; set; }
    public int WriteValue { get; set; }
    public WriteStatus Status { get; set; }
    public string ErrorMessage { get; set; }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; }  // "READ" or "WRITE"
    public string Address { get; set; }  // "D4000"
    public int Value { get; set; }
    public bool Success { get; set; }
}
```

### ⚙️ State (Local Component State)

```csharp
List<ReadEntry> readEntries = new()
{
    new() { AddrType = "D", AddrIndex = 4000 },
    new() { AddrType = "D", AddrIndex = 4001 },
    new() { AddrType = "D", AddrIndex = 4002 },
    new() { AddrType = "M", AddrIndex = 0 },
};

List<PlcWriteItem> writeEntries = new()
{
    new() { AddrType = "D", AddrIndex = 5000 },
    new() { AddrType = "D", AddrIndex = 5001 },
    new() { AddrType = "M", AddrIndex = 0 },
};

List<LogEntry> actionLog = new();
bool autoRefresh = true;
bool showAddRead = false;
string newReadType = "D";
int newReadIndex = 0;
System.Timers.Timer? _timer;  // 250ms update
```

### 🔧 Methods

#### 1. **Lifecycle**

```csharp
protected override void OnInitialized()
{
    ViewModel.PropertyChanged += OnViewModelChanged;
    _timer = new System.Timers.Timer(250);
    _timer.Elapsed += OnTimerTick;
    _timer.AutoReset = true;
    _timer.Start();
}
```

Hook vào `PropertyChanged` của ViewModel. Timer chạy mỗi 250ms.

#### 2. **OnViewModelChanged()** - React to ViewModel changes

```csharp
void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
{
    if (autoRefresh)
        InvokeAsync(() => { UpdateReadValues(); StateHasChanged(); });
}
```

Khi ViewModel property thay đổi, cập nhật read entries.

#### 3. **OnTimerTick()** - Timer callback

```csharp
async void OnTimerTick(object? sender, ElapsedEventArgs e)
{
    if (autoRefresh)
        await InvokeAsync(() => { UpdateReadValues(); StateHasChanged(); });
}
```

Mỗi 250ms cập nhật UI từ ViewModel.

#### 4. **UpdateReadValues()** - Cập nhật dữ liệu đọc

```csharp
void UpdateReadValues()
{
    if (!ViewModel.Status || ViewModel.arr_R_V == null) return;
    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    foreach (var entry in readEntries)
    {
        if (entry.AddrType != "D") continue;  // Chỉ xử lý D
      
        int offset = entry.AddrIndex - ViewModel.D_R_V;
        if (offset < 0 || offset >= ViewModel.arr_R_V.Length) continue;

        int newVal = ViewModel.arr_R_V[offset];
        if (newVal != entry.PrevValue)
        {
            entry.PrevValue = newVal;
            entry.Value = newVal;
            entry.JustUpdated = true;
            entry.UpdatedMs = 0;
            AddLog("READ", $"{entry.AddrType}{entry.AddrIndex}", newVal, true);
        }
        else
        {
            entry.JustUpdated = false;
        }
    }
}
```

**Logic**:

1. Kiểm tra kết nối
2. Duyệt từng read entry
3. Lấy giá trị từ `arr_R_V` dựa trên offset
4. Nếu giá trị thay đổi, cập nhật và log

#### 5. **ManualRefresh()** - Cập nhật thủ công

```csharp
void ManualRefresh()
{
    UpdateReadValues();
    StateHasChanged();
}
```

#### 6. **ToggleAutoRefresh()** - Bật/tắt auto update

```csharp
void ToggleAutoRefresh()
{
    autoRefresh = !autoRefresh;
}
```

#### 7. **AddReadEntry()** - Thêm địa chỉ để đọc

```csharp
void AddReadEntry()
{
    readEntries.Add(new ReadEntry { AddrType = newReadType, AddrIndex = newReadIndex });
    showAddRead = false;
    newReadIndex = 0;
}
```

#### 8. **RemoveReadEntry()** - Xoá địa chỉ

```csharp
void RemoveReadEntry(ReadEntry e) => readEntries.Remove(e);
```

#### 9. **ExecuteWrite()** - Thực thi ghi dữ liệu ⭐

```csharp
void ExecuteWrite(PlcWriteItem entry)
{
    entry.ErrorMessage = "";

    // Validate
    if (entry.WriteValue < 0 || entry.WriteValue > 65535)
    {
        entry.Status = WriteStatus.ERR;
        entry.ErrorMessage = "Value must be 0 – 65535";
        AddLog("WRITE", $"{entry.AddrType}{entry.AddrIndex}", entry.WriteValue, false);
        return;
    }

    if (!ViewModel.Status)
    {
        entry.Status = WriteStatus.ERR;
        entry.ErrorMessage = "PLC not connected.";
        AddLog("WRITE", ..., false);
        return;
    }

    try
    {
        if (entry.AddrType == "D")
        {
            int offset = entry.AddrIndex - ViewModel.D_W_V;
            if (offset >= 0 && offset < ViewModel.arr_W_V.Length)
            {
                ViewModel.arr_W_V[offset] = entry.WriteValue;  // Ghi vào array
                entry.Status = WriteStatus.OK;
                AddLog("WRITE", ..., true);
            }
            else
            {
                entry.Status = WriteStatus.ERR;
                entry.ErrorMessage = $"Address outside writable range...";
                AddLog("WRITE", ..., false);
            }
        }
        else
        {
            // M/X/Y not implemented
            entry.Status = WriteStatus.ERR;
            entry.ErrorMessage = $"Write to {entry.AddrType} not implemented.";
            AddLog("WRITE", ..., false);
        }
    }
    catch (Exception ex)
    {
        entry.Status = WriteStatus.ERR;
        entry.ErrorMessage = ex.Message;
        AddLog("WRITE", ..., false);
    }
}
```

**Logic ghi**:

1. Validate giá trị (0-65535)
2. Kiểm tra kết nối PLC
3. Nếu D register, tính offset và ghi vào `arr_W_V`
4. Monitor thread sẽ ghi vào PLC (Write() method)
5. Log hoạt động

#### 10. **AddWriteEntry()** - Thêm lệnh ghi

```csharp
void AddWriteEntry() => writeEntries.Add(new PlcWriteItem());
```

#### 11. **RemoveWriteEntry()** - Xoá lệnh ghi

```csharp
void RemoveWriteEntry(PlcWriteItem e) => writeEntries.Remove(e);
```

#### 11. **AddLog()** - Ghi log hoạt động (CENTRALIZED LOGGING)

```csharp
public void AddLog(string source, string status, string message, string detail = "")
{
    if (_allLogs.Count > 0) 
        _allLogs[^1].IsNewest = false;
  
    var log = new LogItem 
    { 
        Source = source,        // "UI", "PC", "PLC"
        Status = status,        // "info", "success", "warning", "error"
        Message = message,
        Detail = detail, 
        IsNewest = true 
    };
  
    _allLogs.Add(log);
    if (_allLogs.Count > 500)   // Max 500 entries
        _allLogs.RemoveAt(0);
  
    LogAdded?.Invoke(this, log);  // Notify subscribers (e.g., LogMonitor)
}
```

**Tính năng**:

- **Centralized storage**: Tất cả logs từ mọi trang (Dashboard, Telemetry, LogMonitor) được lưu trong _allLogs
- **Event notification**: LogAdded event cho phép UI subscribe và cập nhật real-time
- **Max limit**: Giới hạn 500 entries để tránh memory leak
- **IsNewest flag**: Tự động mark log mới nhất
- **Sources**: "UI" (user actions), "PC" (local operations), "PLC" (connection)
- **Statuses**: "info", "success", "warning", "error"
- **Detail field**: Optional thông tin chi tiết (e.g., exception type)

**Logging điểm gọi**:

```csharp
// Application startup
AddLog("UI", "info", "Application started");
AddLog("PC", "info", "MainViewModel initialized");

// Connection
AddLog("PLC", "info", $"Connection attempt → {IpAddress}:{Port}");
AddLog("PLC", "success", "PLC connection established");
AddLog("PLC", "error", "PLC connection failed");
AddLog("PLC", "warning", "Connection closed by user");

// Custom Memory
AddLog("UI", "info", $"Added custom memory: D4000");
AddLog("UI", "info", $"Removed custom memory: D4000");

// Read/Write operations (throttled, 1 sec)
AddLog("PC", "success", $"Read D{D_R_V}(...) + D32-blocks → OK", "Monitor cycle");
AddLog("PC", "success", $"Write D{D_W_V}(...) + D{D_W_P}(...) → OK", "Monitor cycle");
AddLog("PC", "error", $"Read failed: {exception}", "AccessViolationException");
```

#### 13. **ClearLog()** - Xoá log

```csharp
void ClearLog() => actionLog.Clear();
```

#### 14. **GetTypeColor()** - Màu cho loại địa chỉ

```csharp
string GetTypeColor(string type) => type switch
{
    "D" => "#0050cb",    // Xanh
    "M" => "#006c49",    // Xanh lá
    "X" => "#b45309",    // Cam
    "Y" => "#7c3aed",    // Tím
    _   => "#727687"
};
```

#### 15. **Dispose()** - Cleanup

```csharp
public void Dispose()
{
    _timer?.Stop();
    _timer?.Dispose();
    ViewModel.PropertyChanged -= OnViewModelChanged;
}
```

---

## � LogMonitor - Theo dõi nhật ký (NEW)

**File**: `Pages/LogMonitor.razor`
**Inject**: `MainViewModel`

### 📌 Purpose

LogMonitor là giao diện tập trung để theo dõi **tất cả log hệ thống** từ khi ứng dụng khởi động. Khác với Telemetry chỉ ghi log các lệnh READ/WRITE, LogMonitor hiển thị:

- ✅ Tất cả user actions (Connect, Disconnect, Jog, Page navigation)
- ✅ Tất cả PLC operations (Read, Write, Connection status)
- ✅ System events (Application startup, Monitor thread status)

### 🔗 Liên kết với MainViewModel

LogMonitor sử dụng **centralized log storage** từ MainViewModel:

```csharp
// Trong MainViewModel.cs
public class LogItem
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Source { get; set; } = "UI";      // "UI" / "PC" / "PLC"
    public string Message { get; set; } = "";
    public string Status { get; set; } = "info";    // "info"/"success"/"warning"/"error"
    public string Detail { get; set; } = "";
    public bool Tagged { get; set; }
    public bool IsNewest { get; set; }
}

private List<LogItem> _allLogs = new();
public List<LogItem> AllLogs => _allLogs;
public event EventHandler<LogItem>? LogAdded;

public void AddLog(string source, string status, string message, string detail = "")
{
    if (_allLogs.Count > 0) _allLogs[^1].IsNewest = false;
    var log = new LogItem 
    { 
        Source = source, 
        Status = status, 
        Message = message, 
        Detail = detail, 
        IsNewest = true 
    };
    _allLogs.Add(log);
    if (_allLogs.Count > 500) _allLogs.RemoveAt(0);  // Max 500 logs
    LogAdded?.Invoke(this, log);  // Notify subscribers
}
```

### ⚙️ Component Architecture

```csharp
@code {
    List<MainViewModel.LogItem> allLogs => ViewModel.AllLogs;
  
    // Display filters
    string selectedSource = "ALL";      // ALL / UI / PC / PLC
    string selectedStatus = "ALL";      // ALL / info / success / warning / error
    string searchQuery = "";
  
    bool isPaused = false;
  
    protected override void OnInitialized()
    {
        // Subscribe to new logs
        ViewModel.LogAdded += (s, log) => InvokeAsync(StateHasChanged);
      
        // Subscribe to ViewModel changes
        ViewModel.PropertyChanged += OnVmChanged;
    }
  
    // Methods for filtering, searching, exporting, etc.
}
```

### 🔧 Methods

#### 1. **OnInitialized()** - Component lifecycle

```csharp
protected override void OnInitialized()
{
    ViewModel.LogAdded += (s, log) => 
    {
        if (!isPaused)
        {
            InvokeAsync(StateHasChanged);  // Real-time update
        }
    };
  
    ViewModel.PropertyChanged += OnVmChanged;
}
```

- Subscribe tới `LogAdded` event từ MainViewModel
- Mỗi log mới được thêm → tự động refresh UI
- Hỗ trợ pause để không bị spam updates

#### 2. **Filter Logs** - Lọc theo source/status/query

```csharp
List<MainViewModel.LogItem> GetFilteredLogs()
{
    return allLogs
        .Where(log => selectedSource == "ALL" || log.Source == selectedSource)
        .Where(log => selectedStatus == "ALL" || log.Status == selectedStatus)
        .Where(log => string.IsNullOrEmpty(searchQuery) || 
                      log.Message.Contains(searchQuery))
        .ToList();
}
```

#### 3. **Pause/Resume** - Tạm dừng updates

```csharp
void TogglePause()
{
    isPaused = !isPaused;
}
```

#### 4. **Clear Logs** - Xoá tất cả logs

```csharp
void ClearLogs()
{
    ViewModel.AllLogs.Clear();
    StateHasChanged();
}
```

#### 5. **Export Logs** - Xuất CSV

```csharp
async Task ExportLogs()
{
    var csv = string.Join("\n", allLogs.Select(l =>
        $"{l.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{l.Source},{l.Status},{l.Message},{l.Detail}"));
    // Download file...
}
```

#### 6. **Tag/Untag** - Đánh dấu logs quan trọng

```csharp
void ToggleTag(MainViewModel.LogItem log)
{
    log.Tagged = !log.Tagged;
    StateHasChanged();
}
```

### 📊 UI Components

**Tab Strip**:

- `ALL` - Hiển thị tất cả logs
- `UI` - Chỉ logs từ UI actions
- `PC` - Logs từ PC operations
- `PLC` - Logs từ PLC interactions

**Search & Filter**:

- Status dropdown: ALL / info / success / warning / error
- Search textbox: Tìm kiếm message
- Live update checkbox: Bật/tắt pause

**Log Entry Row**:

```
[Tag] [Timestamp] [Source] [Status] [Message] [→ Detail] [Delete]
```

**Controls**:

- ▶ Resume / ⏸ Pause
- 📥 Export to CSV
- 🗑 Clear All
- ↻ Auto-scroll to newest

### 📝 Logging Points

Tất cả user actions được log tại các điểm sau:

**Dashboard.razor**:

```csharp
// Connection
ViewModel.AddLog("UI", "info", "Connect button pressed");
ViewModel.AddLog("UI", "success", "PLC connected");  // or error

// Jog actions
ViewModel.AddLog("UI", "info", "Jog X+ pressed");
ViewModel.AddLog("UI", "info", "Jog X released");
```

**MainViewModel.cs**:

```csharp
// Constructor
AddLog("UI", "info", "Application started");

// Connection lifecycle
AddLog("PC", "info", "Connection attempt to " + IpAddress);
AddLog("PC", "success", "Connected successfully");
AddLog("PC", "error", "Connection failed: " + ex.Message);
```

**Telemetry.razor**:

```csharp
// When page loads
ViewModel.AddLog("UI", "info", "Telemetry page loaded");

// Read operations
AddLog("READ", address, value, success);

// Write operations
AddLog("WRITE", address, value, success);
```

---

## 🟦 MainLayout - Navigation & Highlighting (UPDATED)

**File**: `MainLayout.razor`

### 🔗 Dynamic Navigation System

MainLayout sekarang mengelola **dynamic tab highlighting** berdasarkan current route.

```csharp
@inject NavigationManager NavManager

@code {
    private bool IsActive(string path)
    {
        var currentPath = NavManager.ToBaseRelativePath(NavManager.Uri);
        if (path == "/") return currentPath == "";
        return currentPath.StartsWith(path);
    }
  
    protected override void OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            NavManager.LocationChanged += (sender, e) => 
            {
                InvokeAsync(StateHasChanged);  // Refresh highlighting
            };
        }
    }
}
```

### 📌 Conditional Styling

**Sidebar Navigation**:

```html
<!-- Control link -->
<a href="/" class="@(IsActive("/") ? 
    "bg-surface-container-lowest text-primary border-l-4 border-primary shadow-sm" : 
    "text-on-surface-variant hover:bg-surface-container-low")">
    Control
</a>

<!-- Telemetry link -->
<a href="/telemetry" class="@(IsActive("telemetry") ? 
    "bg-surface-container-lowest text-primary border-l-4 border-primary shadow-sm" : 
    "text-on-surface-variant hover:bg-surface-container-low")">
    Telemetry
</a>

<!-- Logs link -->
<a href="/logmonitor" class="@(IsActive("logmonitor") ? 
    "bg-surface-container-lowest text-primary border-l-4 border-primary shadow-sm" : 
    "text-on-surface-variant hover:bg-surface-container-low")">
    Logs
</a>
```

**Top Navigation Bar**:

```html
<!-- Dashboard link -->
<a href="/" class="@(IsActive("/") ? 
    "text-primary border-b-2 border-primary" : 
    "text-on-surface-variant hover:text-primary")">
    Dashboard
</a>

<!-- Similar for Telemetry and Logs -->
```

### 🔄 Features

1. **Automatic Highlighting**: Tab tự động highlight dựa trên URL
2. **LocationChanged Subscription**: Khi user navigate → UI update
3. **Visual Feedback**:
   - Active: Border + background color + primary text
   - Inactive: Neutral color + hover effect

---

## 🔄 Luồng giao tiếp dữ liệu

### 📤 **Luồng Ghi (Write Flow)**

```
Telemetry.razor (User)
    ↓
ExecuteWrite(PlcWriteItem)
    ↓
Validate & Check PLC Connection
    ↓
ViewModel.AddLog("WRITE", address, value, success)
    ↓
ViewModel.arr_W_V[offset] = value
    ↓
(Monitor thread chạy mỗi 10ms)
Monitor() → Write()
    ↓
ePLC.WriteDeviceBlock(..., D_W_V, arr_W_V)
    ↓
TCP/IP → PLC
    ↓
Dashboard & LogMonitor get notification via PropertyChanged/LogAdded
    ↓
UI updates to reflect latest state
```

### 📥 **Luồng Đọc (Read Flow)**

```
PLC
    ↓
(Monitor thread chạy mỗi 10ms)
Monitor() → Read()
    ↓
ePLC.ReadDeviceBlock(..., D_R_V, 99)
ePLC.ReadDeviceBlock(..., D32Base1, 6)
ePLC.ReadDeviceBlock(..., D32Base2, 6)
    ↓
arr_R_V = [ giá trị đọc ]
arr_R32 = [ combine 2 words thành 32-bit ]
    ↓
PropertyChanged event (triggers binding updates)
    ↓
Dashboard.razor: Hiển thị Position Cards (arr_R32)
Telemetry.razor: UpdateReadValues() → Add log entries
LogMonitor.razor: Receive PropertyChanged notification
    ↓
UI cập nhật
```

### 🔄 **Jog Flow (Điều khiển chuyển động)**

```
User presses Jog Button
    ↓
Dashboard.Jog(axis, dir)
    ↓
ViewModel.AddLog("UI", "info", "Jog X+ pressed")
    ↓
ViewModel.JogStart(markAddress)
    ├─ ePLC.WriteDeviceBlock(..., M, markAddress, {1})
    └─ Ghi bit=1 vào PLC mark
    ↓
PLC receives jog mark
    ↓
Axis starts moving
    ↓

User releases Jog Button
    ↓
Dashboard.StopJog(axis)
    ↓
ViewModel.AddLog("UI", "info", "Jog X released")
    ↓
ViewModel.JogStop(markAddress)
    ├─ ePLC.WriteDeviceBlock(..., M, markAddress, {0})
    └─ Ghi bit=0 vào PLC mark
    ↓
PLC receives stop signal
    ↓
Axis stops moving
    ↓
LogMonitor displays: [UI] [info] [Jog X+ pressed] & [Jog X released]
```

### 🔄 **Chu trình Monitor Thread (100Hz)**

```
while (Status)
{
    Sleep(10ms)              // 10ms = 100Hz
    Status = IsConnected     // Check connection
    if (Status)
    {
        Read()               // Đọc từ PLC + log (throttled 1sec)
        RefreshCustomMemory()// Cập nhật custom memory entries
        // Write();          // (currently commented out)
    }
}
```

**Hoạt động**:

- **Read()**: Đọc enable flag, velocity array, 32-bit positions (configurable)
- **RefreshCustomMemory()**: Cập nhật giá trị của user-defined custom entries
- **Write()**: Hiện tại commented out trong Monitor loop

---

## 🖥️ Thông tin PLC

### 📍 Địa chỉ Memory

| Địa chỉ      | Mục đích       | Kiểu         | Kích thước   | Ghi chú                        |
| --------------- | ----------------- | ------------- | --------------- | ------------------------------- |
| **M3000** | **X+ Jog**  | **Bit** | **1 bit** | **Jog hướng X dương** |
| **M3001** | **X- Jog**  | **Bit** | **1 bit** | **Jog hướng X âm**     |
| **M3002** | **Y+ Jog**  | **Bit** | **1 bit** | **Jog hướng Y dương** |
| **M3003** | **Y- Jog**  | **Bit** | **1 bit** | **Jog hướng Y âm**     |
| **M3004** | **Z+ Jog**  | **Bit** | **1 bit** | **Jog hướng Z dương** |
| **M3005** | **Z- Jog**  | **Bit** | **1 bit** | **Jog hướng Z âm**     |
| D1000-D1005     | 32-bit Position 1 | Word          | 6 words         | 3 giá trị 32-bit (X, Y, Z)    |
| D2000-D2005     | 32-bit Position 2 | Word          | 6 words         | 3 giá trị 32-bit thêm        |
| D3000           | Enable Flag       | Word          | 1 word          | 0=disabled, !=0=enabled         |
| D4000-D4099     | Read Velocity     | Word          | 99 words        | Đọc từ PLC                   |
| D5000-D5099     | Write Velocity    | Word          | 99 words        | Ghi vào PLC                    |
| D3000-D3099     | Write Position    | Word          | 99 words        | Ghi position commands           |

**Jog Mark Details**:

- Loại: Bit memory (M)
- Ghi từ Dashboard: `JogStart()` → ghi bit=1, `JogStop()` → ghi bit=0
- PLC nhận → Axis chuyển động theo hướng tương ứng
- Log entry: Tự động add vào ViewModel.AllLogs

### 🔧 Cấu hình kết nối

```csharp
IpAddress: "192.168.3.39"
Port: 3000
NetworkNo: 0
StationNo: 0
StationPLCNo: 255
```

### 📊 Dữ liệu mẫu

```
arr_R32[0] = D1000 + (D1001 << 16)   // Position X (32-bit)
arr_R32[1] = D1002 + (D1003 << 16)   // Position Y (32-bit)
arr_R32[2] = D1004 + (D1005 << 16)   // Position Z (32-bit)
arr_R32[3] = D2000 + (D2001 << 16)   // Extra 1 (32-bit)
arr_R32[4] = D2002 + (D2003 << 16)   // Extra 2 (32-bit)
arr_R32[5] = D2004 + (D2005 << 16)   // Extra 3 (32-bit)

arr_R_V[0..98] = D4000..D4098         // Velocity values (99 words)
arr_W_V[0..98] = D5000..D5098         // Write to PLC
```

---

## 📋 Bảng tóm tắt liên kết File

| File                       | Chức năng                                     | Gọi/Nhận từ                                               |
| -------------------------- | ----------------------------------------------- | ------------------------------------------------------------ |
| **MainViewModel.cs** | MVVM ViewModel, quản lý PLC, centralized logs | Dashboard, Telemetry, LogMonitor                             |
| **Dashboard.razor**  | UI điều khiển chính + Jog (X/Y/Z ±)        | MainViewModel (Connect, Jog, StopJog, AddLog)                |
| **Telemetry.razor**  | UI giám sát + debug, Read/Write operations    | MainViewModel (Read, Write, AddLog)                          |
| **LogMonitor.razor** | UI theo dõi nhật ký hệ thống               | MainViewModel (AllLogs, LogAdded event)                      |
| **MainLayout.razor** | Shared layout + Dynamic Navigation              | NavigationManager (IsActive), Dashboard/Telemetry/LogMonitor |
| **MainWindow.xaml**  | WPF host window                                 | BlazorApp.razor                                              |
| **BlazorApp.razor**  | Blazor root component                           | MainLayout.razor                                             |

### 🔗 Key Relationships

**MainViewModel → All Pages**:

- Dashboard: `Jog()`, `StopJog()`, `ConnectCommand`
- Telemetry: `arr_R_V`, `arr_W_V`, `AddLog()`
- LogMonitor: `AllLogs`, `LogAdded` event

**MainLayout → All Pages**:

- NavigationManager: Track current route
- IsActive(path): Determine highlight state
- Dynamic class binding: Apply visual feedback

---

## 🚀 Quy trình Startup

```
1. App.xaml.cs: OnStartup
   ├─ Khởi tạo ServiceCollection
   ├─ AddWpfBlazorWebView()
   ├─ AddSingleton<MainViewModel>()
   │  └─ Constructor: Seed initial logs
   └─ BuildServiceProvider()

2. MainWindow.xaml
   └─ BlazorWebView (host Blazor)
      └─ BlazorApp.razor (root)
         └─ MainLayout.razor
            ├─ Inject NavigationManager
            ├─ Subscribe LocationChanged
            └─ @Body
                ├─ Dashboard page
                ├─ Telemetry page
                └─ LogMonitor page

3. Người dùng click "Connect" button (Dashboard)
   ├─ Dashboard.Connect()
   ├─ ViewModel.ConnectCommand.Execute()
   ├─ ConnectPLC()
   ├─ AddLog("PC", "info", "Connection attempt...")
   ├─ AddLog("PC", "success", "Connected!") / AddLog(..., "error", ...)
   └─ Monitor() thread started

4. Monitor thread (100Hz loop)
   ├─ Read(): Đọc từ PLC
   ├─ Write(): Ghi vào PLC
   └─ PropertyChanged events
      ├─ Dashboard: Refresh Position Cards
      ├─ Telemetry: UpdateReadValues()
      └─ LogMonitor: (subscribe to LogAdded)

5. Người dùng nhấn Jog button (Dashboard)
   ├─ Dashboard.Jog('X', true)
   ├─ ViewModel.AddLog("UI", "info", "Jog X+ pressed")
   ├─ ViewModel.JogStart(3000)
   │  └─ Write mark=1 to PLC
   └─ LogMonitor: Displays log entry in real-time

6. Người dùng điều hướng (MainLayout)
   ├─ Click: Dashboard / Telemetry / Logs
   ├─ LocationChanged event fires
   ├─ IsActive() recalculated
   └─ Nav links re-render with new highlighting
```

---

## 💡 Ví dụ Sử dụng

### Ví dụ 1: Kết nối PLC

```csharp
// User clicks "Connect" button in Dashboard
Connect()
    ↓
ViewModel.ConnectCommand.Execute(null)
    ↓
ConnectPLC()
    - ePLC.SetPLCProperties(...)
    - ePLC.Open()
    - Thread Monitor start
  
// Status property changed
// UI shows "Connected" badge
```

### Ví dụ 2: Đọc Position

```csharp
// Monitor thread every 10ms
Read()
    ↓
flag = ReadDeviceBlock(D3000)
if (flag[0] != 0)
    ↓
    b1 = ReadDeviceBlock(D1000, 6)
    b2 = ReadDeviceBlock(D2000, 6)
    ↓
    arr_R32[0] = b1[0] | (b1[1] << 16)  // X position
    arr_R32[1] = b1[2] | (b1[3] << 16)  // Y position
    arr_R32[2] = b1[4] | (b1[5] << 16)  // Z position
    ↓
PropertyChanged event
    ↓
Dashboard: Position Cards updated
Telemetry: Read entries updated
```

### Ví dụ 3: Ghi Velocity

```csharp
// User clicks "Write" in Telemetry
ExecuteWrite(PlcWriteItem)
    ↓
Validate: 0 <= value <= 65535
    ↓
offset = AddrIndex - D_W_V  // D5000
    ↓
ViewModel.arr_W_V[offset] = value
    ↓
// Monitor thread next cycle
Write()
    ↓
WriteDeviceBlock(D_W_V, arr_W_V)
    ↓
Status = WriteStatus.OK
Log: "WRITE D5000 → 1500 OK"
```

### Ví dụ 4: Jog Axis Chuyển động - **NEW**

```csharp
// User presses "X+" button in Dashboard
@onmousedown="() => Jog('X', true)"
    ↓
Dashboard.Jog('X', true)
    - markAddress = 3000 (X+ = M3000)
    - dirName = "X+"
    ↓
ViewModel.AddLog("UI", "info", "Jog X+ pressed")
    - Thêm entry vào AllLogs
    - Trigger LogAdded event
    ↓
ViewModel.JogStart(3000)
    - ePLC.WriteDeviceBlock(Bit, M, "3000", {1})
    - Ghi M3000 = 1 vào PLC
    ↓
PLC receives M3000=1
    ↓
X-axis starts moving in positive direction
    ↓
LogMonitor.razor displays:
    [UI] [info] [Jog X+ pressed] [Timestamp] [→ Detail] [Tag] [Delete]
    ↓

// User releases button
@onmouseup="() => StopJog('X')"
    ↓
Dashboard.StopJog('X')
    ↓
ViewModel.AddLog("UI", "info", "Jog X released")
    ↓
ViewModel.JogStop(3000)  // Clear M3000
ViewModel.JogStop(3001)  // Clear M3001
    - ePLC.WriteDeviceBlock(Bit, M, "3000", {0})
    - ePLC.WriteDeviceBlock(Bit, M, "3001", {0})
    ↓
PLC receives M3000=0, M3001=0
    ↓
X-axis stops moving
    ↓
LogMonitor displays 2nd entry:
    [UI] [info] [Jog X released] [Timestamp] [→ Detail] [Tag] [Delete]
```

### Ví dụ 5: View Navigation Highlighting - **NEW**

```csharp
// User clicks "Logs" in MainLayout sidebar
<a href="/logmonitor" ...>Logs</a>
    ↓
NavigationManager navigates to "/logmonitor"
    ↓
LocationChanged event fires
    ↓
OnAfterRenderAsync callback:
    NavManager.LocationChanged += (s, e) => InvokeAsync(StateHasChanged)
    ↓
IsActive("/logmonitor") → true
    - currentPath = ToBaseRelativePath(Uri) = "logmonitor"
    - currentPath.StartsWith("logmonitor") = true
    ↓
Conditional classes applied:
    @(IsActive("logmonitor") ? 
        "bg-surface-container-lowest text-primary border-l-4 border-primary shadow-sm" : 
        "text-on-surface-variant hover:bg-surface-container-low")
    ↓
Logs tab renders with:
    - Background: surface-container-lowest
    - Text color: primary (blue)
    - Border: 4px left border in primary color
    - Shadow: sm
  
// Other tabs now render with inactive styling:
Dashboard, Telemetry tabs show:
    - Text color: on-surface-variant (neutral)
    - Hover: background changes to surface-container-low
    - No border/shadow
    ↓
Visual feedback: User sees which page is active
```

---

## 📝 Ghi chú Kỹ thuật

1. **Thread Safety**: Monitor thread ghi vào mảng, UI đọc. .NET MVVM Toolkit xử lý thread-safe thông qua PropertyChanged.
2. **32-bit Position**: Kết hợp 2 words 16-bit thành 32-bit:

   ```csharp
   pos32 = low_word | (high_word << 16)
   ```
3. **100Hz Loop**: Sleep(10ms) trong Monitor thread tạo tần số ~100Hz cập nhật.
4. **AutoRefresh Timer**: Telemetry dùng 250ms timer để cập nhật UI, không phải 10ms để tránh quá tải.
5. **Exception Handling**: Read() method có try-catch fallback.
6. **Stateful Writing**: arr_W_V/arr_W_Position dùng mảng để lưu state ghi, tránh ghi liên tục.
7. **Centralized Logging**: Tất cả logs (UI, PC, PLC events) được lưu trong MainViewModel.AllLogs

   - Max 500 entries, oldest removed khi vượt quá
   - LogAdded event notify subscribers on new log
   - Component không cần local log lists, dùng shared ViewModel.AllLogs
8. **Jog Mark Architecture**: Marks (M3000-M3005) là quy ước

   - Ghi bit=1 để bắt đầu jog
   - Ghi bit=0 để dừng jog
   - Tất cả actions logged via ViewModel.AddLog()
9. **Dynamic Navigation**: MainLayout.IsActive() dựa trên ToBaseRelativePath

   - "/" maps to root ("")
   - "/telemetry" matches "telemetry" prefix
   - LocationChanged subscription tự động refresh styling
10. **Custom Memory Entries**: User-defined address reading

    - Stored in CustomMemoryEntries list
    - RefreshCustomMemory() called in Monitor loop (100Hz)
    - Supports D, M, X, Y address types
    - Auto-log when entries added/removed
11. **Log Throttling**: Prevent spam from 100Hz read/write cycle

    - _lastReadLogTime, _lastWriteLogTime timestamps
    - Read/Write logs throttled to 1 second intervals
    - Prevents log window from being flooded
    - Still logs errors immediately
12. **Configurable Address Bases**:

    - D32Base1, D32Base2 - User can change 32-bit read addresses
    - DReadEnable - User can change enable flag address
    - Dynamic property updates trigger refresh

---

**Tài liệu cập nhật**: April 29, 2026
**Phiên bản dự án**: GantrySCADA v1.1 (with DXF Trajectory Support)

---

## 📐 Tính năng DXF Trajectory (CAM)

**File**: `MainViewModel.DxfFeature.cs` + `netDxf.dll`

### 📌 Mô tả tính năng
Module DXF đóng vai trò như một hệ thống CAM (Computer-Aided Manufacturing) thu nhỏ, cho phép nạp bản vẽ kỹ thuật (.dxf) và biên dịch thành chuỗi lệnh điều khiển chuyển động nội suy cho robot Gantry.

### 🔄 Các giai đoạn xử lý

#### 1. Nạp và Phân tích DXF (`LoadDxfAdvanced`)
- Sử dụng thư viện `netDxf` để trích xuất các thực thể: `Lines`, `Polylines2D`, `Circles`, `Arcs`.
- Phân tách thành danh sách các quỹ đạo (`_dxfContours`).
- Tính toán các thuộc tính: Độ dài, tọa độ tâm, góc xoay, điểm bắt đầu/kết thúc.
- Xác định biên (Bounds) để tự động căn chỉnh khung nhìn và hỗ trợ hiển thị.

#### 2. Xem trước quỹ đạo (Preview)
- Hàm `GenerateSvgPath` chuyển đổi tọa độ CAD sang chuỗi Path SVG cho Blazor UI.
- Tự động Scale và dịch chuyển tọa độ để hiển thị vừa vặn trong khung Preview (tỷ lệ chuẩn 200x200).
- Hỗ trợ hiển thị các thực thể phức tạp như `Circle` và `Arc` bằng lệnh SVG `A` (Arc) chính xác.

#### 3. Thiết lập tọa độ Robot
- **Scale/Offset**: Tùy chỉnh tỷ lệ (Scale X/Y) và độ lệch (Offset X/Y) để khớp với không gian làm việc của bàn máy thực tế.
- **Safety Validation**: Kiểm tra xem quỹ đạo (sau khi scale/offset) có vượt quá giới hạn an toàn của máy (ví dụ: 0-1000mm) trước khi truyền lệnh.

#### 4. Biên dịch và Truyền lệnh xuống PLC (`DownloadTrajectoryToPlc`) ⭐
Chuyển đổi các điểm hình học sang mã lệnh nội suy chuyên dụng của PLC (thường dùng cho các Module nội suy/Simple Motion):
- **Tọa độ**: Chuyển mm sang micron (nhân 1000) để đạt độ phân giải integer cao trên PLC.
- **Mã lệnh (Command Codes)**:
  - `0xD00A`: Nội suy đoạn thẳng liên tục (Continuous Linear).
  - `0xD00F`: Nội suy cung tròn CW (theo chiều kim đồng hồ).
  - `0xD010`: Nội suy cung tròn CCW (ngược chiều kim đồng hồ).
- **Kết thúc (END)**: Lệnh cuối cùng trong chuỗi được đánh dấu bằng bit `0x1000` (`Positioning complete`) để PLC tự động dừng hành trình.
- **Vùng nhớ**: Ghi trực tiếp vào vùng đệm `D2000` (Trục X) và `D8000` (Trục Y) - user có thể dùng lệnh BMOV để chuyển vào U/G memory của module chuyển động.

### 📍 Cấu trúc gói dữ liệu điểm (Point Data Structure)
Mỗi điểm quỹ đạo được nạp vào PLC dưới dạng một khối 10-word (20 bytes):
1. **Command Code** (0x...): Loại chuyển động
2. **M-Code**: Mã phụ trợ
3. **Dwell Time (Low/High)**: Thời gian chờ
4. **Speed (Low/High)**: Tốc độ chuyển động (mm/s * scale)
5. **Position (Low/High)**: Tọa độ đích (X hoặc Y)
6. **Center/Aux (Low/High)**: Tọa độ tâm (nếu là cung tròn/đường tròn)

---

