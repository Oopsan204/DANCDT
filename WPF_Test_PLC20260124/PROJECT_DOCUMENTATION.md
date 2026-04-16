# WPF_Test_PLC20260124 - Project Documentation

## 📋 Mục Lục
1. [Giới Thiệu Dự Án](#giới-thiệu-dự-án)
2. [Kiến Trúc Hệ Thống](#kiến-trúc-hệ-thống)
3. [Công Nghệ Sử Dụng](#công-nghệ-sử-dụng)
4. [Thành Phần Cấu Trúc](#thành-phần-cấu-trúc)
5. [Tính Năng Chính](#tính-năng-chính)
6. [Luồng Dữ Liệu](#luồng-dữ-liệu)
7. [Chi Tiết Hàm & Code](#chi-tiết-hàm--code)

---

## 📌 Giới Thiệu Dự Án

**WPF_Test_PLC20260124** là một **ứng dụng Desktop** dùng để **kiểm thử và giao tiếp với PLC (Programmable Logic Controller)** của Mitsubishi Electric qua mạng **TCP/IP**.

### Mục Đích Chính
- ✅ Kết nối đến PLC qua TCP/IP
- ✅ Đọc dữ liệu từ các vùng nhớ PLC (D, M, X, Y)
- ✅ Ghi dữ liệu vào PLC
- ✅ Giám sát real-time dữ liệu PLC
- ✅ Xử lý dữ liệu 32-bit từ các thanh ghi 16-bit

---

## 🏗️ Kiến Trúc Hệ Thống

### MVVM Pattern (Model-View-ViewModel)
```
┌─────────────────────────────────────────────────┐
│             View Layer (XAML)                    │
│  ┌──────────────────────────────────────────┐   │
│  │  MainWindow.xaml                         │   │
│  │  - UI Controls & Bindings                │   │
│  │  - 2-Column Layout                       │   │
│  │  - Connection Panel, Data Controls       │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
                      ↓
                 DataContext
                      ↓
┌─────────────────────────────────────────────────┐
│          ViewModel Layer                         │
│  ┌──────────────────────────────────────────┐   │
│  │  MainViewModel.cs                        │   │
│  │  - Observable Properties                 │   │
│  │  - RelayCommands                         │   │
│  │  - Business Logic                        │   │
│  │  - Background Monitoring Thread          │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
                      ↓
         External Library & Helper
                      ↓
┌─────────────────────────────────────────────────┐
│          Model Layer                             │
│  ┌──────────────────────────────────────────┐   │
│  │  ePLCControl (NVKProject.PLC)            │   │
│  │  - TCP/IP Connection                     │   │
│  │  - Device Read/Write Operations          │   │
│  └──────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────┐   │
│  │  PlcBitHelper (Static Utility)           │   │
│  │  - Bit Manipulation Functions            │   │
│  │  - 32-bit Position Handling              │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
```

---

## 🛠️ Công Nghệ Sử Dụng

| Công Nghệ | Phiên Bản | Mục Đích |
|-----------|---------|---------|
| **.NET Framework** | 4.8 | Runtime Platform |
| **WPF** | .NET 4.8 | UI Framework (Desktop) |
| **CommunityToolkit.MVVM** | 8.4.0 | MVVM Pattern (ObservableObject, RelayCommand) |
| **MaterialDesignThemes** | 5.3.0 | Modern Material Design UI |
| **MaterialDesignColors** | 5.3.0 | Color Palette |
| **Microsoft.Xaml.Behaviors** | 1.1.77 | XAML Behaviors |
| **NVKProject.PLC** | Custom | PLC Communication Driver |
| **NVKProject.Logger** | Custom | Logging Library |

---

## 📦 Thành Phần Cấu Trúc

### 1. **MainWindow.xaml** (View)
File định nghĩa giao diện người dùng

#### Layout Structure
```
Window (800x600)
└── Grid (Background: White)
    └── Border
        └── UniformGrid (2 Columns)
            └── GroupBox: "PLC Communication"
                ├── GroupBox: "PLC Connection"
                │   ├── IP Address (TextBox)
                │   └── Port (TextBox)
                ├── GroupBox: "Network"
                │   ├── Network No (TextBox)
                │   └── Station No (TextBox)
                ├── GroupBox: "Data Area"
                │   ├── Device Type (ComboBox: D, M, X, Y)
                │   ├── Start Address (TextBox)
                │   ├── Length (TextBox)
                │   └── Value (TextBox)
                └── GroupBox: "Actions"
                    ├── Connect Button
                    ├── TestRead Button
                    └── TestWrite Button
```

#### Bindings (XAML ↔ ViewModel)
- `IpAddress` → TextBox.Text
- `Port` → TextBox.Text
- `NetworkNo` → TextBox.Text
- `StationNo` → TextBox.Text
- `DeviceType` → ComboBox.SelectedItem
- `StartAddress` → TextBox.Text
- `Length` → TextBox.Text
- `ValuePLC` → TextBox.Text

---

### 2. **MainWindow.xaml.cs** (Code-Behind)
```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Thiết lập DataContext liên kết với ViewModel
        DataContext = new MainViewModel();
    }
}
```

**Chức năng**: Khởi tạo cửa sổ chính và liên kết DataContext với MainViewModel

---

### 3. **MainViewModel.cs** (ViewModel - Business Logic)

#### A. Declare & Initialize Fields
```csharp
// PLC Communication Instance
private ePLCControl ePLC;

// Network Parameters
private string ipAddress = "192.168.3.39";
private int port = 3000;
private int networkNo = 0;
private int stationNo = 0;
private int stationPLCNo = 255;

// Data Area Addresses
public int D_R_V = 4000;      // Read values base address
public int D_W_V = 5000;      // Write values base address
public int D_W_P = 3000;      // Write position base address

// 32-bit Configuration
private int _d32Base1 = 1000;  // First 32-bit read base
private int _d32Base2 = 2000;  // Second 32-bit read base
private int _dReadEnable = 3000; // Enable flag address
```

#### B. Properties (Observable)
```csharp
public string IpAddress
{
    get { return ipAddress; }
    set { ipAddress = value; OnPropertyChanged(); }
}

public int Port
{
    get { return port; }
    set { port = value; OnPropertyChanged(); }
}

public int NetworkNo
{
    get { return networkNo; }
    set { networkNo = value; }
}

public int StationNo
{
    get { return stationNo; }
    set { stationNo = value; OnPropertyChanged(); }
}

public bool Status
{
    get { return status; }
    set { status = value; OnPropertyChanged(); }
}

// 32-bit Arrays
public int[] arr_R32
{
    get { return _arr_R32; }
    set { SetProperty(ref _arr_R32, value); }
}

// Regular Data Arrays
public int[] arr_R_V = new int[99];      // Read values (D4000-D4099)
public int[] arr_W_V = new int[99];      // Write values (D5000-D5099)
public int[] arr_W_Position = new int[99]; // Position (D3000-D3098)
```

#### C. Commands (RelayCommand)
```csharp
public ICommand ConnectCommand { get; set; }    // Kết nối PLC & bắt đầu giám sát
public ICommand TestReadCommand { get; set; }   // (Chưa triển khai)
public ICommand TestWriteCommand { get; set; }  // (Chưa triển khai)
public ICommand TestBit { get; set; }           // (Chưa triển khai)
```

#### D. Core Methods

##### **ConnectPLC()**
```csharp
private void ConnectPLC()
{
    // Khởi tạo đối tượng giao tiếp PLC
    ePLC = new ePLCControl();
    
    // Cấu hình thông số kết nối
    ePLC.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
    
    // Mở kết nối
    ePLC.Open();
    
    // Cập nhật trạng thái
    Status = ePLC.IsConnected;

    // Tạo thread giám sát nền
    Thread t1 = new Thread(Monitor);
    t1.IsBackground = true;  // Thread sẽ tự động dừng khi app đóng
    t1.Start();
}
```

**Mục đích**: Khởi tạo kết nối PLC và bắt đầu quá trình giám sát real-time

---

##### **Monitor()** (Background Thread)
```csharp
private void Monitor()
{
    while (Status)
    {
        Thread.Sleep(10);  // Chờ 10ms
        Status = ePLC.IsConnected;  // Kiểm tra kết nối
        
        if (Status)
        {
            Read();   // Đọc dữ liệu từ PLC
            Write();  // Ghi dữ liệu đến PLC
        }
    }
}
```

**Mục đích**: Vòng lặp giám sát liên tục ở nền, đọc/ghi dữ liệu mỗi 10ms

---

##### **Read()** (Đọc Dữ Liệu Từ PLC)
```csharp
private void Read()
{
    try
    {
        // Bước 1: Đọc flag enable tại địa chỉ DReadEnable (D3000)
        int[] flag = ePLC.ReadDeviceBlock(
            ePLCControl.SubCommand.Word, 
            ePLCControl.DeviceName.D, 
            $"{DReadEnable}", 
            1
        );
        
        if (flag == null || flag.Length == 0)
        {
            // Fallback: Vẫn đọc dữ liệu tiêu chuẩn
            arr_R_V = ePLC.ReadDeviceBlock(
                ePLCControl.SubCommand.Word, 
                ePLCControl.DeviceName.D, 
                $"{D_R_V}", 
                Length
            );
            return;
        }

        // Bước 2: Kiểm tra flag enable
        if (flag[0] != 0)
        {
            // Đọc dữ liệu thông thường (D4000-D4099)
            arr_R_V = ePLC.ReadDeviceBlock(
                ePLCControl.SubCommand.Word, 
                ePLCControl.DeviceName.D, 
                $"{D_R_V}", 
                Length
            );

            // Bước 3: Đọc dữ liệu 32-bit từ D32Base1 (D1000-D1005)
            int[] b1 = ePLC.ReadDeviceBlock(
                ePLCControl.SubCommand.Word, 
                ePLCControl.DeviceName.D, 
                $"{D32Base1}", 
                6
            );

            // Bước 4: Đọc dữ liệu 32-bit từ D32Base2 (D2000-D2005)
            int[] b2 = ePLC.ReadDeviceBlock(
                ePLCControl.SubCommand.Word, 
                ePLCControl.DeviceName.D, 
                $"{D32Base2}", 
                6
            );

            // Bước 5: Kết hợp 2 thanh ghi 16-bit thành 32-bit
            if (b1 != null && b1.Length >= 6)
            {
                // Cặp (0,1), (2,3), (4,5)
                for (int i = 0; i < 3; i++)
                {
                    int low = b1[i * 2];      // Byte thấp
                    int high = b1[i * 2 + 1]; // Byte cao
                    _arr_R32[i] = low | (high << 16);  // Kết hợp
                }
            }

            if (b2 != null && b2.Length >= 6)
            {
                for (int i = 0; i < 3; i++)
                {
                    int low = b2[i * 2];
                    int high = b2[i * 2 + 1];
                    _arr_R32[3 + i] = low | (high << 16);
                }
            }

            OnPropertyChanged(nameof(arr_R32));  // Cập nhật UI
        }
        else
        {
            // Nếu flag = 0, chỉ đọc arr_R_V
            arr_R_V = ePLC.ReadDeviceBlock(
                ePLCControl.SubCommand.Word, 
                ePLCControl.DeviceName.D, 
                $"{D_R_V}", 
                Length
            );
        }
    }
    catch (Exception)
    {
        // Fallback: Đọc dữ liệu tiêu chuẩn nếu có lỗi
        try 
        { 
            arr_R_V = ePLC.ReadDeviceBlock(
                ePLCControl.SubCommand.Word, 
                ePLCControl.DeviceName.D, 
                $"{D_R_V}", 
                Length
            ); 
        } 
        catch { }
    }
}
```

**Mục đích**: Đọc dữ liệu từ PLC, bao gồm:
- Kiểm tra flag enable
- Đọc dữ liệu thông thường
- Đọc và kết hợp dữ liệu 32-bit

---

##### **Write()** (Ghi Dữ Liệu Đến PLC)
```csharp
private void Write()
{
    // Ghi giá trị tiêu chuẩn (D5000-D5098)
    ePLC.WriteDeviceBlock(
        ePLCControl.SubCommand.Word, 
        ePLCControl.DeviceName.D, 
        $"{D_W_V}", 
        arr_W_V
    );

    // Ghi vị trí/position 32-bit (D3000-D3098)
    ePLC.WriteDeviceBlock(
        ePLCControl.SubCommand.Word, 
        ePLCControl.DeviceName.D, 
        $"{D_W_P}", 
        arr_W_Position
    );
}
```

**Mục đích**: Ghi các mảng dữ liệu đến PLC

---

#### E. Helper Methods (Bit & Position Operations)

##### **GetBit(int word, int bit)**
```csharp
public bool GetBit(int word, int bit)
{
    // Lấy bit tại vị trí 'bit' từ 'word'
    // Ví dụ: word=0b1010, bit=1 → true
    return ((word >> bit) & 1) == 1;
}
```

---

##### **SetBit(int word, int bit, bool value)**
```csharp
public int SetBit(int word, int bit, bool value)
{
    if (value)
        return word | (1 << bit);    // Set bit = 1
    else
        return word & ~(1 << bit);   // Set bit = 0
}
```

---

##### **GetCurrentPosition(int[] arr, int index)**
```csharp
public double GetCurrentPosition(int[] arr, int index)
{
    // Kết hợp 2 thanh ghi 16-bit thành 32-bit
    // arr[index] = LOW word, arr[index+1] = HIGH word
    return arr[index] + (arr[index + 1] << 16);
}
```

**Công thức**: `32-bit value = LOW + (HIGH << 16)`

---

##### **SetCurrentPosition(int[] arr, int index, double value)**
```csharp
public void SetCurrentPosition(int[] arr, int index, double value)
{
    int v = (int)value;
    
    // Tách 32-bit thành 2 thanh ghi 16-bit
    arr[index] = v & 0xFFFF;              // Byte thấp
    arr[index + 1] = (v >> 16) & 0xFFFF;  // Byte cao
}
```

---

##### **ReadDevice(int iAddress)**
```csharp
private bool ReadDevice(int iAddress)
{
    // Đọc giá trị (true/false) từ arr_R_V
    if ((iAddress - D_R_V) > 0 && (iAddress - D_R_V) < arr_R_V.Length)
    {
        return arr_R_V[iAddress - D_R_V] == 0 ? false : true;
    }
    return false;
}
```

---

##### **WriteDevice(int iAddress, bool value)**
```csharp
private void WriteDevice(int iAddress, bool value)
{
    // Ghi giá trị (true/false) đến arr_W_V
    if ((iAddress - D_W_V) >= 0 && (iAddress - D_W_V) < arr_W_V.Length)
    {
        arr_W_V[iAddress - D_W_V] = value ? 1 : 0;
    }
}
```

---

##### **SetPosition(int iAddress, double value)**
```csharp
public void SetPosition(int iAddress, double value)
{
    // Thiết lập position 32-bit tại địa chỉ
    if ((iAddress - D_W_P) >= 0 && (iAddress - D_W_P + 1) < arr_W_Position.Length)
    {
        int index = iAddress - D_W_P;
        SetCurrentPosition(arr_W_Position, index, value);
    }
}
```

---

#### F. Bit Manipulation Helper Methods

##### **WordToBits(int word)**
```csharp
public static bool[] WordToBits(int word)
{
    // Chuyển 16-bit word thành bool[16]
    // word = 0b1010 → [false, true, false, true, false, ...]
    bool[] bits = new bool[16];
    for (int i = 0; i < 16; i++)
        bits[i] = ((word >> i) & 1) == 1;
    return bits;
}
```

---

##### **BitsToWord(bool[] bits)**
```csharp
public static int BitsToWord(bool[] bits)
{
    // Chuyển bool[16] thành 16-bit word
    if (bits == null) return 0;
    int word = 0;
    for (int i = 0; i < bits.Length && i < 16; i++)
        if (bits[i]) word |= (1 << i);
    return word;
}
```

---

##### **GetDataValue_(int[] arr, int index)**
```csharp
private int[] GetDataValue_(int[] arr, int index)
{
    // Lấy 16 bit từ word ở index
    if (arr == null || index < 0 || index >= arr.Length)
        return null;

    int word = arr[index];
    int[] bits = new int[16];

    for (int i = 0; i < 16; i++)
        bits[i] = (word >> i) & 1;  // Trích bit thứ i

    return bits;
}
```

---

##### **SetDataValue_(int[] arr, int index, int[] bits)**
```csharp
private void SetDataValue_(int[] arr, int index, int[] bits)
{
    // Đặt 16 bit vào word ở index
    if (arr == null || bits == null)
        return;

    if (index < 0 || index >= arr.Length)
        return;

    if (bits.Length < 16)
        throw new ArgumentException("Bits must have at least 16 elements");

    int word = 0;

    for (int i = 0; i < 16; i++)
    {
        if (bits[i] == 1)
            word |= (1 << i);
    }

    arr[index] = word;
}
```

---

### 4. **PlcBitHelper** (Static Utility Class)

Static class chứa các hàm tiện ích cho xử lý bit và word

#### Bit Conversion Functions
```csharp
public static class PlcBitHelper
{
    // Bool Array ↔ Int Array
    public static int[] BoolArrayToIntArray(bool[] bits)
    public static bool[] IntArrayToBoolArray(int[] arr)

    // Word ↔ Bits (LSB First)
    public static bool[] WordToBits(int word)
    public static int BitsToWord(bool[] bits)

    // Word ↔ Bit String (Debug)
    public static string WordToBitString(int word)
    public static int BitStringToWord(string bits)

    // Individual Bit Operations
    public static bool GetBit(int word, int bitIndex)
    public static int SetBit(int word, int bitIndex, bool value)

    // 32-bit Position (2 Words)
    public static double GetCurrentPosition(int[] arr, int index)
    public static void SetCurrentPosition(int[] arr, int index, double value)
}
```

---

### 5. **App.xaml & App.xaml.cs**

#### App.xaml.cs
```csharp
public partial class App : Application
{
    // Ứng dụng WPF cơ bản, không logic đặc biệt
}
```

#### App.xaml
```xml
<Application x:Class="WPF_Test_PLC20260124.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <!-- Material Design Theme Resources -->
    </Application.Resources>
</Application>
```

---

## 🎯 Tính Năng Chính

### 1. **Kết Nối PLC**
- ✅ Cấu hình IP Address & Port
- ✅ Cấu hình Network No & Station No
- ✅ Khởi tạo giao tiếp TCP/IP
- ✅ Kiểm tra trạng thái kết nối

### 2. **Đọc Dữ Liệu (Read)**
- ✅ Đọc dữ liệu từ vùng D4000-D4099 (99 giá trị)
- ✅ Kiểm tra flag enable tại D3000
- ✅ Đọc dữ liệu 32-bit từ D1000-D1005 & D2000-D2005
- ✅ Kết hợp 2 thanh ghi 16-bit thành 32-bit
- ✅ Xử lý exception với fallback

### 3. **Ghi Dữ Liệu (Write)**
- ✅ Ghi dữ liệu đến D5000-D5099 (99 giá trị)
- ✅ Ghi position 32-bit đến D3000-D3098

### 4. **Giám Sát Real-Time (Monitoring)**
- ✅ Background thread chạy mỗi 10ms
- ✅ Liên tục kiểm tra kết nối
- ✅ Tự động đọc/ghi dữ liệu
- ✅ Cập nhật UI thông qua OnPropertyChanged()

### 5. **Xử Lý Bit & Word**
- ✅ Lấy/Đặt bit riêng lẻ
- ✅ Chuyển đổi word ↔ bits array
- ✅ Chuyển đổi word ↔ bit string
- ✅ Xử lý 32-bit position từ 2 thanh ghi 16-bit

---

## 📊 Luồng Dữ Liệu

### Khi Nhấn Nút "Connect"
```
Button Click
    ↓
ConnectCommand (RelayCommand)
    ↓
ConnectPLC() Method
    ├─ ePLC = new ePLCControl()
    ├─ ePLC.SetPLCProperties(IP, Port, NetworkNo, StationPLCNo, StationNo)
    ├─ ePLC.Open()
    ├─ Status = ePLC.IsConnected
    ├─ Create Background Thread
    └─ Start Monitor Thread
```

### Vòng Lặp Giám Sát (Monitor Loop - 10ms interval)
```
Monitor() Background Thread
    ↓
While (Status == true)
    ├─ Sleep(10ms)
    ├─ Check ePLC.IsConnected
    └─ If Connected:
        ├─ Read()
        │  ├─ Read DReadEnable flag (D3000)
        │  ├─ If flag != 0:
        │  │  ├─ Read arr_R_V (D4000-D4099)
        │  │  ├─ Read b1 (D1000-D1005)
        │  │  ├─ Read b2 (D2000-D2005)
        │  │  ├─ Combine to arr_R32[0-5]
        │  │  └─ OnPropertyChanged(arr_R32)
        │  └─ Else:
        │     └─ Read arr_R_V only
        │
        └─ Write()
           ├─ Write arr_W_V (D5000-D5099)
           └─ Write arr_W_Position (D3000-D3098)
```

### Cập Nhật UI
```
OnPropertyChanged(PropertyName)
    ↓
UI Binding
    ↓
TextBox/Label hiển thị giá trị mới
```

---

## 🔗 Sơ Đồ Liên Kết Hàm

```
MainWindow.xaml (View)
    ├─ Binding: IpAddress, Port, NetworkNo, StationNo
    ├─ Binding: StartAddress, Length, ValuePLC
    ├─ Button: ConnectCommand
    │
    └─ DataContext = MainViewModel

MainViewModel (ViewModel)
    ├─ Properties (Observable)
    │  ├─ IpAddress, Port, NetworkNo, StationNo
    │  ├─ StartAddress, Length, ValuePLC
    │  ├─ arr_R_V, arr_W_V, arr_W_Position
    │  ├─ arr_R32
    │  ├─ D32Base1, D32Base2, DReadEnable
    │  └─ Status
    │
    ├─ Commands
    │  ├─ ConnectCommand → ConnectPLC()
    │  ├─ TestReadCommand (empty)
    │  ├─ TestWriteCommand (empty)
    │  └─ TestBit (empty)
    │
    ├─ ConnectPLC()
    │  └─ Creates & Starts Monitor Thread
    │
    ├─ Monitor() [Background Thread]
    │  └─ Loop every 10ms:
    │     ├─ Read()
    │     └─ Write()
    │
    ├─ Read()
    │  ├─ Check DReadEnable flag
    │  ├─ Read arr_R_V
    │  ├─ Read D32Base1 blocks
    │  ├─ Read D32Base2 blocks
    │  └─ Combine to arr_R32
    │
    ├─ Write()
    │  ├─ Write arr_W_V
    │  └─ Write arr_W_Position
    │
    ├─ GetBit(word, bit)
    ├─ SetBit(word, bit, value)
    ├─ GetCurrentPosition(arr, index)
    ├─ SetCurrentPosition(arr, index, value)
    ├─ ReadDevice(iAddress)
    ├─ WriteDevice(iAddress, value)
    ├─ GetDataValue_(arr, index)
    ├─ SetDataValue_(arr, index, bits)
    │
    └─ Uses ePLCControl (NVKProject.PLC)
        ├─ SetPLCProperties()
        ├─ Open()
        ├─ IsConnected
        ├─ ReadDeviceBlock()
        ├─ WriteDeviceBlock()
        └─ WordToBit()

PlcBitHelper (Static Utility)
    ├─ BoolArrayToIntArray()
    ├─ IntArrayToBoolArray()
    ├─ WordToBits()
    ├─ BitsToWord()
    ├─ WordToBitString()
    ├─ BitStringToWord()
    ├─ GetBit()
    ├─ SetBit()
    ├─ GetCurrentPosition()
    └─ SetCurrentPosition()
```

---

## 📝 Bộ Nhớ PLC Được Sử Dụng

| Địa Chỉ | Phạm Vi | Số Lượng | Mục Đích |
|---------|--------|---------|---------|
| D1000-D1005 | 6 words | 6 | Read 32-bit Base 1 (3 cặp) |
| D2000-D2005 | 6 words | 6 | Read 32-bit Base 2 (3 cặp) |
| D3000 | Flag | 1 | Enable flag để kích hoạt đọc 32-bit |
| D3000-D3098 | Position | 99 | Write Position 32-bit (99 cặp) |
| D4000-D4099 | Read Values | 99 | Read Values (99 words) |
| D5000-D5098 | Write Values | 99 | Write Values (99 words) |

---

## ⚙️ Cấu Hình Mặc Định

```csharp
IP Address: 192.168.3.39
Port: 3000
Network No: 0
Station No: 0
Station PLC No: 255

D32Base1: 1000
D32Base2: 2000
DReadEnable: 3000
D_R_V: 4000
D_W_V: 5000
D_W_P: 3000
Length: 99

Monitor Interval: 10ms
```

---

## 🚀 Các Tính Năng Cần Phát Triển

- [ ] Triển khai `TestReadCommand`
- [ ] Triển khai `TestWriteCommand`
- [ ] Triển khai `TestBit` command
- [ ] Thêm UI để hiển thị arr_R32
- [ ] Thêm error logging
- [ ] Thêm connection retry logic
- [ ] Thêm graceful disconnect
- [ ] Tối ưu hóa thread management
- [ ] Thêm unit tests

---

## 📞 Liên Hệ & Support

**Project Name**: WPF_Test_PLC20260124  
**Technology**: .NET Framework 4.8 + WPF + MVVM  
**Last Updated**: 2026-04-16

---

**Tài Liệu này được tạo để cung cấp hiểu biết toàn diện về cấu trúc, tính năng và cách hoạt động của ứng dụng.**
