# GantrySCADA - Tài liệu Dự án Chi tiết (Cập nhật)

## 📋 Mục lục
1. [Tổng quan dự án](#tổng-quan-dự-án)
2. [Các tính năng chính](#các-tính-năng-chính)
3. [Kiến trúc hệ thống](#kiến-trúc-hệ-thống)
4. [Cấu trúc file](#cấu-trúc-file)
5. [MainViewModel - Core MVVM](#mainviewmodel---core-mvvm)
6. [Các trang Razor](#các-trang-razor)
7. [Luồng giao tiếp dữ liệu](#luồng-giao-tiếp-dữ-liệu)
8. [Thông tin PLC](#thông-tin-plc)

---

## 🎯 Tổng quan dự án

**GantrySCADA** là ứng dụng **WPF/Blazor** cho phép điều khiển và giám sát hệ thống **Gantry Robot** thông qua **PLC (Programmable Logic Controller)**.

### Thành phần chính
- **.NET 8.0 WPF**: Cửa sổ chính của ứng dụng
- **Blazor WebView**: Nhúng giao diện Blazor Razor vào WPF
- **MVVM Pattern**: Tách biệt logic (ViewModel) với giao diện (View)
- **PLC Communication**: Kết nối TCP/IP với PLC để đọc/ghi dữ liệu
- **Centralized Logging**: Hệ thống log tập trung theo dõi mọi hoạt động
- **Configurable Registers**: Tất cả địa chỉ PLC đều có thể cấu hình

### Công nghệ sử dụng
```
.NET 8.0 WPF
├── Blazor WebView (Microsoft.AspNetCore.Components.WebView.Wpf 8.0.82)
├── MVVM Toolkit (CommunityToolkit.Mvvm 8.4.2)
├── Custom PLC Library (NVKProject.PLC.dll)
└── Custom Logger (NVKProject.Logger.dll)
```

---

## ✨ Các tính năng chính

### 1. 🔌 Kết nối và Quản lý Kết nối PLC
**File**: `MainViewModel.cs`

**Thuộc tính kết nối** (tất cả đều có thể cấu hình):
```csharp
IpAddress       // "192.168.3.39" - IP của PLC
Port            // 3000 - Cổng kết nối
NetworkNo       // 0 - Số mạng
StationNo       // 0 - Số station
StationPLCNo    // 255 - Số PLC
Status          // true/false - Trạng thái kết nối
```

**Methods**:
- `ConnectPLC()` - Kết nối đến PLC, khởi động Monitor thread
- `DisconnectPLC()` - Đóng kết nối và dừng Monitor thread
- `Monitor()` - Background thread chạy 100Hz (10ms), gọi Read/Write/RefreshCustomMemory

### 2. 🎮 Điều khiển chuyển động Jog (Dashboard)
**File**: `Dashboard.razor` + `MainViewModel.cs`

**Jog Controls UI**: XY pad (8 hướng) + Z buttons (2 hướng)
```
        ↑ Y-
    X- XY X+
        ↓ Y+
```

**Jog Mark Mapping** (configurable M_W_Base, default M3000):
| Hướng | Mark Address | Default |
|------|--------------|---------|
| X+ (Right) | M_W_Base | M3000 |
| X- (Left) | M_W_Base + 1 | M3001 |
| Y+ (Down) | M_W_Base + 2 | M3002 |
| Y- (Up) | M_W_Base + 3 | M3003 |
| Z+ (Up) | M_W_Base + 4 | M3004 |
| Z- (Down) | M_W_Base + 5 | M3005 |

**Luồng hoạt động**:
1. User nhấn nút Jog → `Jog(axis, dir)`
2. Xác định mark address dựa trên M_W_Base
3. Log: `AddLog("UI", "info", "Jog X+ pressed")`
4. `ViewModel.JogStart(markAddress)` → Ghi bit=1 vào PLC
5. PLC nhận → Axis chuyển động
6. User thả nút → `StopJog(axis)`
7. Log: `AddLog("UI", "info", "Jog X released")`
8. `ViewModel.JogStop(markAddress)` → Ghi bit=0 vào PLC
9. PLC nhận → Axis dừng

### 3. 📊 Đọc vị trí 32-bit (Configurable) - NEW
**File**: `MainViewModel.cs` + `Dashboard.razor`

**Tính năng**:
- Đọc 6 giá trị 32-bit từ 2 base addresses (configurable)
- Mỗi 32-bit = combine 2 words (low + high << 16)
- Áp dụng scale & offset cho display
- Hiển thị với unit và decimal configuration

**Default Configuration**:
```csharp
D32Base1 = 1000    // D1000-D1005: 3 positions
D32Base2 = 2000    // D2000-D2005: 3 positions
DReadEnable = 3000 // D3000: Enable flag
```

**Position Scaling** (configurable per-axis):
```csharp
PosScaleX = 1.5    // Scale factor
PosOffsetX = 10    // Offset value
PosUnit = "mm"     // Display unit
PosDecimals = 2    // Decimal places

// Display = (rawValue × scale) + offset
// Example: 100 raw → (100 × 1.5) + 10 = 160.00 mm
```

### 4. ⚙️ Hệ thống Monitor Thread (100Hz)
**File**: `MainViewModel.cs`

**Frequency**: 10ms sleep = 100Hz polling

**Chu trình**:
```csharp
while (Status)
{
    Thread.Sleep(10);          // 100Hz tick
    Status = ePLC.IsConnected;
    if (Status)
    {
        Read();                // Đọc tất cả registers
        Write();               // Gửi pending writes
        RefreshCustomMemory(); // Cập nhật custom entries
    }
}
```

**Read Operations** (throttled log mỗi 1 sec):
1. Check DReadEnable flag (D3000)
2. Đọc velocity block: D4000-D4099 (99 words)
3. Đọc 32-bit positions: D1000-D1005 + D2000-D2005 (configurable)
4. Đọc M/X/Y registers (100 bits each)
5. Combine 2 words thành 32-bit
6. Trigger PropertyChanged → UI refresh

**Write Operations** (nếu có pending writes):
1. Check _hasPendingWrites flag
2. WriteDeviceBlock() cho D_W_V, D_W_P
3. WriteDeviceBlock() cho M/X/Y registers
4. Throttled log mỗi 1 sec

### 5. 📝 Hệ thống Logging Tập trung (Centralized) - NEW
**File**: `MainViewModel.cs` + All Pages

**Tính năng**:
- **Global Storage**: `AllLogs` (max 500 entries)
- **Event Notification**: `LogAdded` event cho real-time updates
- **Multi-source**: UI, PC, PLC
- **Multi-status**: info, success, warning, error
- **Throttling**: Read/Write logs limited to 1/sec

**LogItem Model**:
```csharp
public class LogItem
{
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = "UI";      // UI/PC/PLC
    public string Message { get; set; } = "";
    public string Status { get; set; } = "info";    // info/success/warning/error
    public string Detail { get; set; } = "";        // Optional detail
    public bool Tagged { get; set; }                // User tag flag
    public bool IsNewest { get; set; }              // Latest log indicator
}
```

**Logging Points**:
- **Connection**: `AddLog("PLC", "success", "PLC connection established")`
- **Jog Actions**: `AddLog("UI", "info", "Jog X+ pressed")`
- **Read Cycle**: `AddLog("PC", "success", "Read D4000(99) + D32-blocks → OK", "Monitor cycle")`
- **Write Cycle**: `AddLog("PC", "success", "Write commands → OK", "Monitor cycle")`
- **Custom Memory**: `AddLog("UI", "info", "Added custom memory: D4000")`

### 6. 🔍 Custom Memory Entries - NEW
**File**: `Dashboard.razor` + `MainViewModel.cs`

**Tính năng**:
- User thêm bất kỳ địa chỉ nào (D/M/X/Y) để đọc thời gian thực
- Cập nhật mỗi cycle (100Hz) trong Monitor thread
- Hiển thị giá trị hiện tại + last update timestamp
- Có thể thêm/xoá động

**UI Components (Dashboard)**:
- Input fields: Địa chỉ type (D/M/X/Y) + index
- Grid hiển thị: Type badge, address, value, timestamp, delete button
- Real-time update từ RefreshCustomMemory()

### 7. ⚡ Pending Write Queue (Write Command Batching) - NEW
**File**: `MainViewModel.cs`

**Tính năng**:
- Queue pending write operations
- Batch send trong Monitor thread
- Hỗ trợ D/M/X/Y register writes

**Methods**:
```csharp
public void MarkPendingWrite(string addrType, int addrIndex, int value)
public bool HasPendingWrites()
public void ClearPendingWrites()
```

### 8. 🛠️ System Settings (Job Configuration) - NEW
**File**: `SystemSettings.razor`

**Pages/Sections**:

#### Job Configuration
- Edit D-register base addresses:
  - `D_R_V`: Read Velocity base (default 4000)
  - `D_W_V`: Write Velocity base (default 5000)
  - `D_W_P`: Write Position base (default 2000)
  - `D32Base1`: 32-bit read base 1 (default 1000)
  - `D32Base2`: 32-bit read base 2 (default 2000)
  - `DReadEnable`: Enable flag (default 3000)
- Reset to Defaults button

#### Coordinate Configuration
- Position Scaling:
  - `PosScaleX`, `PosScaleY`, `PosScaleZ` (default 1.0)
  - `PosOffsetX`, `PosOffsetY`, `PosOffsetZ` (default 0)
  - `PosUnit`: Unit display (default "mm")
  - `PosDecimals`: Decimal places (default 2)

#### I/O Configuration
- M-Register bases:
  - `M_R_Base`: Read base (default 0)
  - `M_W_Base`: Write base (default 3000, used for jog)
- X-Register bases:
  - `X_R_Base`: Read base (default 0)
  - `X_W_Base`: Write base (default 100)
- Y-Register bases:
  - `Y_R_Base`: Read base (default 0)
  - `Y_W_Base`: Write base (default 100)

---

## 🗂️ Cấu trúc file

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
│   └─ **CORE**: Logic điều khiển PLC + Centralized Logging
│      ├─ Quản lý kết nối PLC
│      ├─ Monitor thread (100Hz)
│      ├─ Read/Write D/M/X/Y registers
│      ├─ Position scaling & offset
│      ├─ Custom memory entries
│      ├─ Pending write queue
│      └─ Global log storage
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
│      ├─ NavLink: Telemetry (telemetry) - Tự động highlight
│      ├─ NavLink: Logs (logmonitor) - Tự động highlight
│      ├─ NavLink: Settings (settings) - Tự động highlight
│      └─ Subscribe LocationChanged → StateHasChanged (highlight update)
│
├── Pages/
│   │
│   ├── 🔴 Dashboard.razor
│   │   └─ **Giao diện chính** - Điều khiển + giám sát
│   │      ├─ Connection control (IP, Port, Connect button)
│   │      ├─ Jog controls (XY pad + Z buttons) - FULLY IMPLEMENTED
│   │      │  ├─ Jog() method - write mark=1 to M_W_Base + offset
│   │      │  ├─ StopJog() method - write mark=0
│   │      │  └─ Automatic logging via AddLog()
│   │      ├─ Position Cards (X/Y/Z từ arr_R32 + scaling/offset)
│   │      ├─ Custom Memory Grid
│   │      │  ├─ Add arbitrary addresses (D/M/X/Y)
│   │      │  ├─ Display real-time values
│   │      │  └─ Delete button for each entry
│   │      └─ Memory Stream debugger (arr_R_V[0-9])
│   │
│   ├── 🔵 Telemetry.razor
│   │   └─ **Giám sát thời gian thực**
│   │      ├─ Real-Time Monitor (D/M/X/Y addresses)
│   │      ├─ Write Control (ghi D-words)
│   │      ├─ Action History Log (max 200 entries)
│   │      └─ Auto-Refresh Timer (250ms)
│   │
│   ├── 🟢 LogMonitor.razor
│   │   └─ **Theo dõi nhật ký hệ thống - CENTRALIZED**
│   │      ├─ Subscribe: ViewModel.LogAdded event
│   │      ├─ Display: ViewModel.AllLogs (tập trung)
│   │      ├─ Filter: Source (UI/PC/PLC), Status (info/success/warning/error)
│   │      ├─ Search: Query search in messages
│   │      ├─ Controls: Pause/Resume, Export CSV, Clear, Tag
│   │      ├─ Tab Strip: ALL / UI / PC / PLC
│   │      └─ Expandable detail modal
│   │
│   └── 🟠 SystemSettings.razor
│       └─ **Cấu hình hệ thống - NEW**
│          ├─ Job Configuration
│          │  ├─ D-register base addresses
│          │  └─ 32-bit read bases
│          ├─ Coordinate Configuration
│          │  ├─ Position scaling (X/Y/Z)
│          │  ├─ Position offset (X/Y/Z)
│          │  └─ Unit & decimal configuration
│          ├─ I/O Configuration
│          │  ├─ M-register bases
│          │  ├─ X-register bases
│          │  └─ Y-register bases
│          └─ Reset to Defaults button
│
├── wwwroot/
│   └─ index.html - Host page cho Blazor
│
├── _Imports.razor
│   └─ Shared using statements
│
└── GantrySCADA.csproj
    └─ Project config
```

---

## 🏗️ MainViewModel - Core MVVM

### 📌 Public Properties (Configuration)

**Connection Settings**:
```csharp
public string IpAddress { get; set; }      // "192.168.3.39"
public int Port { get; set; }              // 3000
public int NetworkNo { get; set; }         // 0
public int StationNo { get; set; }         // 0
public int StationPLCNo { get; set; }      // 255
public bool Status { get; set; }           // Connected?
```

**D-Register Base Addresses**:
```csharp
public int D_R_V { get; set; }            // Read Velocity (default 4000)
public int D_W_V { get; set; }            // Write Velocity (default 5000)
public int D_W_P { get; set; }            // Write Position (default 2000)
public int D32Base1 { get; set; }         // 32-bit read base 1 (default 1000)
public int D32Base2 { get; set; }         // 32-bit read base 2 (default 2000)
public int DReadEnable { get; set; }      // Enable flag (default 3000)
```

**M/X/Y Register Base Addresses**:
```csharp
public int M_R_Base { get; set; }         // M read base (default 0)
public int M_W_Base { get; set; }         // M write base (default 3000)
public int X_R_Base { get; set; }         // X read base (default 0)
public int X_W_Base { get; set; }         // X write base (default 100)
public int Y_R_Base { get; set; }         // Y read base (default 0)
public int Y_W_Base { get; set; }         // Y write base (default 100)
```

**Position Scaling & Display**:
```csharp
public double PosScaleX { get; set; }     // X scale (default 1.0)
public double PosScaleY { get; set; }     // Y scale (default 1.0)
public double PosScaleZ { get; set; }     // Z scale (default 1.0)
public double PosOffsetX { get; set; }    // X offset (default 0)
public double PosOffsetY { get; set; }    // Y offset (default 0)
public double PosOffsetZ { get; set; }    // Z offset (default 0)
public string PosUnit { get; set; }       // Display unit (default "mm")
public int PosDecimals { get; set; }      // Decimal places (default 2)
```

**Data Arrays**:
```csharp
// D-Registers
public int[] arr_R_V { get; }             // Read Velocity (99 words)
public int[] arr_R32 { get; }             // Read Positions (6x 32-bit)
public int[] arr_W_V { get; }             // Write Velocity (99 words)
public int[] arr_W_P { get; }             // Write Position (6 words)

// M/X/Y Registers
public int[] arr_R_M { get; }             // Read M (100 bits)
public int[] arr_R_X { get; }             // Read X (100 bits)
public int[] arr_R_Y { get; }             // Read Y (100 bits)
public int[] arr_W_M { get; }             // Write M (100 words)
public int[] arr_W_X { get; }             // Write X (100 words)
public int[] arr_W_Y { get; }             // Write Y (100 words)
```

**Logging & Events**:
```csharp
public List<LogItem> AllLogs { get; }    // Centralized log storage (max 500)
public event EventHandler<LogItem>? LogAdded;  // New log event

public List<CustomMemoryEntry> CustomMemoryEntries { get; }  // User-defined entries
```

### ⚙️ Public Methods

**Connection**:
```csharp
public void ConnectPLC()        // Connect & start Monitor thread
public void DisconnectPLC()     // Disconnect & stop Monitor thread
```

**Monitor Operations**:
```csharp
private void Monitor()          // Background 100Hz loop
private void Read()             // Read from PLC
private void Write()            // Write to PLC
public void RefreshCustomMemory()  // Update custom entries (called in Monitor)
```

**Jog Control**:
```csharp
public void JogStart(int markAddress)   // Write mark=1 to M register
public void JogStop(int markAddress)    // Write mark=0 to M register
```

**Custom Memory**:
```csharp
public void AddCustomMemoryEntry(string addrType, int addrIndex)
public void RemoveCustomMemoryEntry(CustomMemoryEntry entry)
```

**Logging**:
```csharp
public void AddLog(string source, string status, string message, string detail = "")
```

**Pending Writes**:
```csharp
public void MarkPendingWrite(string addrType, int addrIndex, int value)
public bool HasPendingWrites()
public void ClearPendingWrites()
```

---

## 🎨 Các trang Razor

### 1. Dashboard.razor (Home Page)

**Layout**:
- **Left Column**: Connection control + Jog controls
- **Right Column**: Position displays + Custom memory grid

**UI Components**:
- Connection card: IP input, Port input, Connect/Disconnect button
- Jog controls: XY pad (8-directional) + Z buttons
- Position cards: X, Y, Z coordinates with scaling applied
- Custom memory grid: Add arbitrary addresses, real-time values
- Memory stream debugger: First 10 D-register values

### 2. Telemetry.razor (/telemetry)

**Purpose**: Real-time data monitoring & manual writes

**Sections**:
- Real-Time Monitor: List of addresses (D/M/X/Y), auto-refresh toggle
- Write Control: Input form to write values to addresses
- Action History: Log of recent READ/WRITE operations

### 3. LogMonitor.razor (/logmonitor)

**Purpose**: Centralized view of all system logs

**Features**:
- Tab strip: Filter by source (ALL/UI/PC/PLC)
- Status filter: ALL/info/success/warning/error
- Search query: Find logs by message content
- Controls: Pause/Resume updates, Export CSV, Clear all, Tag important logs
- Expandable rows: View full detail + timestamp

### 4. SystemSettings.razor (/settings)

**Purpose**: Configure all PLC register addresses & position parameters

**Sections**:
- **Job Configuration**: D-register bases, 32-bit bases
- **Coordinate Configuration**: Scaling, offset, unit, decimals
- **I/O Configuration**: M/X/Y register bases

---

## 🔄 Luồng giao tiếp dữ liệu

### Read Flow (100Hz Monitor Loop)
```
Monitor() → Read()
├─ Check DReadEnable flag
├─ Read D_R_V block (99 words)
├─ Read 32-bit from D32Base1/D32Base2
├─ Combine: arr_R32[i] = low | (high << 16)
├─ Read M/X/Y blocks
├─ PropertyChanged event
└─ UI updates (Dashboard, Telemetry, LogMonitor)
```

### Write Flow
```
User writes value in Telemetry
├─ ValidateValue(0-65535)
├─ Check PLC connection
├─ arr_W_V[offset] = value
├─ Monitor() → Write()
│  └─ WriteDeviceBlock(D_W_V, arr_W_V)
├─ Log: AddLog("WRITE", ...)
└─ UI updates status
```

### Jog Flow
```
User mouse-down Jog button
├─ Dashboard.Jog(axis, dir)
├─ Calculate markAddress from axis + M_W_Base
├─ AddLog("UI", "info", "Jog X+ pressed")
├─ ViewModel.JogStart(markAddress)
│  └─ WriteDeviceBlock(Bit, M, markAddress, {1})
└─ PLC axis starts moving

User mouse-up
├─ Dashboard.StopJog(axis)
├─ AddLog("UI", "info", "Jog X released")
├─ ViewModel.JogStop(markAddress) × 2
│  └─ WriteDeviceBlock(Bit, M, markAddress, {0})
└─ PLC axis stops
```

### Monitor Loop Cycle
```
while (Status)
{
    Sleep(10ms)        // 100Hz
    Status = IsConnected
    if (Status)
    {
        Read()                    // ← Với throttle 1 log/sec
        Write()                   // ← Với throttle 1 log/sec
        RefreshCustomMemory()    // ← Cập nhật custom entries
    }
}
```

---

## 🖥️ Thông tin PLC

### 📍 Address Mapping

| Loại | Mục đích | Base | Phạm vi | Mô tả |
|------|---------|------|--------|-------|
| **M** | **Jog Marks** | `M_W_Base` (3000) | M3000-M3005 | Điều khiển chuyển động |
| **D** | **Velocity Read** | `D_R_V` (4000) | D4000-D4098 | Đọc vận tốc (99 words) |
| **D** | **Velocity Write** | `D_W_V` (5000) | D5000-D5098 | Ghi vận tốc (99 words) |
| **D** | **Position Write** | `D_W_P` (2000) | D2000-D2005 | Ghi position (6 words) |
| **D** | **Position Read 1** | `D32Base1` (1000) | D1000-D1005 | 3 × 32-bit positions |
| **D** | **Position Read 2** | `D32Base2` (2000) | D2000-D2005 | 3 × 32-bit positions |
| **D** | **Enable Flag** | `DReadEnable` (3000) | D3000 | Read enable flag |
| **M** | **M Read** | `M_R_Base` (0) | M0-M99 | Read 100 bits |
| **M** | **M Write** | `M_W_Base` (3000) | M3000-M3099 | Write 100 bits |
| **X** | **X Read** | `X_R_Base` (0) | X0-X99 | Read 100 bits |
| **X** | **X Write** | `X_W_Base` (100) | X100-X199 | Write 100 bits |
| **Y** | **Y Read** | `Y_R_Base` (0) | Y0-Y99 | Read 100 bits |
| **Y** | **Y Write** | `Y_W_Base` (100) | Y100-Y199 | Write 100 bits |

### Jog Mark Details
```
M_W_Base + 0 = X+ (Right)
M_W_Base + 1 = X- (Left)
M_W_Base + 2 = Y+ (Down)
M_W_Base + 3 = Y- (Up)
M_W_Base + 4 = Z+ (Up)
M_W_Base + 5 = Z- (Down)
```

**Default**: M_W_Base = 3000
- M3000 = X+
- M3001 = X-
- M3002 = Y+
- M3003 = Y-
- M3004 = Z+
- M3005 = Z-

### 32-Bit Position Combination
```csharp
arr_R32[0] = D1000 | (D1001 << 16)   // Position X
arr_R32[1] = D1002 | (D1003 << 16)   // Position Y
arr_R32[2] = D1004 | (D1005 << 16)   // Position Z
arr_R32[3] = D2000 | (D2001 << 16)   // Extra 1
arr_R32[4] = D2002 | (D2003 << 16)   // Extra 2
arr_R32[5] = D2004 | (D2005 << 16)   // Extra 3
```

### Position Display Formula
```csharp
scaledValue = (rawValue × scale) + offset
display = scaledValue.ToString("F" + decimals) + " " + unit

// Example:
// rawValue = 100, scale = 1.5, offset = 10, decimals = 2, unit = "mm"
// → (100 × 1.5) + 10 = 160.00 mm
```

---

## 🚀 Quy trình Startup

```
1. App.xaml.cs → OnStartup()
   ├─ ServiceCollection setup
   ├─ AddWpfBlazorWebView()
   ├─ AddSingleton<MainViewModel>() [seed logs]
   └─ BuildServiceProvider()

2. MainWindow.xaml
   └─ BlazorWebView → BlazorApp.razor → MainLayout.razor

3. MainLayout.razor
   ├─ Inject NavigationManager
   ├─ Set up LocationChanged subscription
   └─ Render navigation + @Body

4. Dashboard page loads
   ├─ Inject MainViewModel
   ├─ Show Connection panel
   ├─ Show Jog controls (disabled until connected)
   └─ Show Custom memory grid

5. User clicks "Connect"
   ├─ MainViewModel.ConnectCommand.Execute()
   ├─ ConnectPLC()
   ├─ Log: "Connection attempt..."
   ├─ ePLC.Open()
   ├─ Thread Monitor started
   ├─ Log: "Connected!" (or error)
   └─ Status property changed → UI updates

6. Monitor thread loop (100Hz)
   ├─ Read() from PLC
   ├─ Write() pending commands
   ├─ RefreshCustomMemory()
   └─ PropertyChanged → UI refresh
```

---

## 💡 Ví dụ Sử dụng

### Ví dụ 1: Jog Axis
```
User clicks X+ button
├─ Dashboard.Jog('X', true)
├─ markAddress = M_W_Base + 0 = M3000
├─ AddLog("UI", "info", "Jog X+ pressed")
├─ JogStart(3000)
│  └─ WriteDeviceBlock(Bit, M, "3000", {1})
└─ PLC moves X axis +

User releases button
├─ Dashboard.StopJog('X')
├─ AddLog("UI", "info", "Jog X released")
├─ JogStop(3000), JogStop(3001)
│  └─ WriteDeviceBlock(Bit, M, "3000/3001", {0})
└─ PLC stops X axis
```

### Ví dụ 2: Đọc Position
```
Monitor loop every 10ms
├─ Read()
├─ flag = ReadDeviceBlock(D3000)
├─ if (flag != 0)
│  ├─ b1 = ReadDeviceBlock(D1000, 6)
│  ├─ b2 = ReadDeviceBlock(D2000, 6)
│  └─ arr_R32[0] = b1[0] | (b1[1] << 16)  // X
├─ PropertyChanged event
└─ Dashboard Position Card updated
```

### Ví dụ 3: Custom Memory
```
User adds D4000 to monitor
├─ AddCustomMemoryEntry("D", 4000)
├─ List added + Log: "Added custom memory: D4000"
├─ Monitor loop calls RefreshCustomMemory()
│  ├─ ReadDeviceBlock(D, "4000", 1)
│  ├─ entry.CurrentValue = result[0]
│  ├─ entry.LastUpdate = DateTime.Now
│  └─ UI updated
└─ Dashboard shows D4000 with real-time value
```

### Ví dụ 4: SystemSettings
```
User opens Settings page
├─ Show Job Configuration
├─ Input: D_R_V = 4000 (can change)
├─ Input: D32Base1 = 1000 (can change)
├─ Input: PosScaleX = 1.5
├─ Input: PosOffsetX = 10
├─ Input: PosUnit = "mm"
└─ Apply → PropertyChanged → Position display updates
```

---

**Document Version**: 2.0 (Updated with all current features)
**Last Updated**: 2026-04-18
**Status**: Complete & Accurate
