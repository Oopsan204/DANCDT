using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NVKProject.PLC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
        #region Fields
        private ePLCControl ePLC;
        private int _ValuePLC = 1;
        private int _Length = 99;
        private int _StartAddress = 700;
        private string ipAddress = "192.168.3.39";
        private int port = 3000;
        private int networkNo = 0;
        private int stationNo = 0;
        private int stationPLCNo = 255;
        private bool status;


        private int _D_R_V = 4000;
        private int _D_W_V = 5000;
        private int _D_W_P = 2000;
        private int _M_R_Base = 0;     // M register read base (M0+)
        private int _M_W_Base = 3000;  // M register write base (M3000+)
        private int _X_R_Base = 0;     // X register read base (X0+)
        private int _X_W_Base = 100;   // X register write base (X100+)
        private int _Y_R_Base = 0;     // Y register read base (Y0+)
        private int _Y_W_Base = 100;   // Y register write base (Y100+)

        // New: addresses to read as 32-bit (pairs)
        // Default pairs: D1000+D1001, D1002+D1003, D1004+D1005, D2000+D2001, D2002+D2003, D2004+D2005
        private int[] _arr_R32 = new int[6];

        public int[] arr_W_Position = new int[99];
        public int[] arr_W_P = new int[6];  // Write Position D2000-D2005 (6 words)
        public int[] arr_W_M = new int[100];  // Write M registers M3000-M3099 (100 words)
        public int[] arr_W_X = new int[100];  // Write X registers X100-X199 (100 words)
        public int[] arr_W_Y = new int[100];  // Write Y registers Y100-Y199 (100 words)
        private int[] _arr_R_V = new int[99];
        private int[] _arr_R_M = new int[100];  // Read M registers M0-M99
        private int[] _arr_R_X = new int[100];  // Read X registers X0-X99
        private int[] _arr_R_Y = new int[100];  // Read Y registers Y0-Y99
        public int[] arr_W_V = new int[99];
        
        // Properties for arr_R_M, arr_R_X, arr_R_Y to trigger PropertyChanged when updated
        public int[] arr_R_M
        {
            get { return _arr_R_M; }
            private set { SetProperty(ref _arr_R_M, value); }
        }
        public int[] arr_R_X
        {
            get { return _arr_R_X; }
            private set { SetProperty(ref _arr_R_X, value); }
        }
        public int[] arr_R_Y
        {
            get { return _arr_R_Y; }
            private set { SetProperty(ref _arr_R_Y, value); }
        }

        // ✓ Throttle excessive logs (Write/Read happen 100x/sec)
        private DateTime _lastWriteLogTime = DateTime.MinValue;
        private DateTime _lastReadLogTime = DateTime.MinValue;

        // Write command flags - track if there are pending write operations
        private bool _hasPendingWrites = false;
        private readonly object _pendingWriteLock = new();
        private readonly List<PendingWriteItem> _pendingWriteItems = new();

        private sealed class PendingWriteItem
        {
            public string AddrType { get; set; } = "D";
            public int AddrIndex { get; set; }
            public int Value { get; set; }
        }

        // Global log storage
        public class LogItem
        {
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string Source { get; set; } = "UI";
            public string Message { get; set; } = "";
            public string Status { get; set; } = "info";
            public string Detail { get; set; } = "";
            public bool Tagged { get; set; }
            public bool IsNewest { get; set; }
        }
        private List<LogItem> _allLogs = new();
        public List<LogItem> AllLogs => _allLogs;
        
        // Event for new log
        public event EventHandler<LogItem>? LogAdded;

        // Custom Memory Entry: for user to read arbitrary addresses
        public class CustomMemoryEntry
        {
            public string AddrType { get; set; } = "D";  // D, M, X, Y
            public int AddrIndex { get; set; }
            public int CurrentValue { get; set; }
            public DateTime LastUpdate { get; set; } = DateTime.Now;
        }
        
        private List<CustomMemoryEntry> _customMemoryEntries = new();
        public List<CustomMemoryEntry> CustomMemoryEntries => _customMemoryEntries;
        #endregion
        #region Propeties
        public int ValuePLC
        {
            get { return _ValuePLC; }
            set { _ValuePLC = value; }
        }
        public int Length
        {
            get { return _Length; }
            set { _Length = value; OnPropertyChanged(); }
        }
        public int StartAddress
        {
            get { return _StartAddress; }
            set { _StartAddress = value; OnPropertyChanged(); }
        }
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
        public int StationPLCNo
        {
            get { return stationPLCNo; }
            set { stationPLCNo = value; OnPropertyChanged(); }
        }
        public bool Status
        {
            get { return status; }
            set { status = value; OnPropertyChanged(); }
        }

        // arr_R_V: expose as property to trigger UI refresh on each PLC read cycle
        public int[] arr_R_V
        {
            get { return _arr_R_V; }
            private set { SetProperty(ref _arr_R_V, value); }
        }

        // New: expose arr_R32 as property so UI can bind and get notifications
        public int[] arr_R32
        {
            get { return _arr_R32; }
            set { SetProperty(ref _arr_R32, value); }
        }

        // New: configurable base addresses and enable flag for 32-bit reads
        private int _d32Base1 = 1000;
        public int D32Base1
        {
            get { return _d32Base1; }
            set { _d32Base1 = value; OnPropertyChanged(); }
        }

        private int _d32Base2 = 2000;
        public int D32Base2
        {
            get { return _d32Base2; }
            set { _d32Base2 = value; OnPropertyChanged(); }
        }

        // Address of the enable flag (default D3000)
        private int _dReadEnable = 3000;
        public int DReadEnable
        {
            get { return _dReadEnable; }
            set { _dReadEnable = value; OnPropertyChanged(); }
        }

        // Configurable source mapping for X/Y/Z coordinate display (Dashboard/Control tab)
        private string _posAddrTypeX = "D";
        public string PosAddrTypeX
        {
            get => _posAddrTypeX;
            set
            {
                _posAddrTypeX = NormalizeAddrType(value);
                OnPropertyChanged();
            }
        }

        private int _posAddrIndexX = 1000;
        public int PosAddrIndexX
        {
            get => _posAddrIndexX;
            set { _posAddrIndexX = value; OnPropertyChanged(); }
        }

        private bool _posAddrXRead32 = true;
        public bool PosAddrXRead32
        {
            get => _posAddrXRead32;
            set { _posAddrXRead32 = value; OnPropertyChanged(); }
        }

        private string _posAddrTypeY = "D";
        public string PosAddrTypeY
        {
            get => _posAddrTypeY;
            set
            {
                _posAddrTypeY = NormalizeAddrType(value);
                OnPropertyChanged();
            }
        }

        private int _posAddrIndexY = 1002;
        public int PosAddrIndexY
        {
            get => _posAddrIndexY;
            set { _posAddrIndexY = value; OnPropertyChanged(); }
        }

        private bool _posAddrYRead32 = true;
        public bool PosAddrYRead32
        {
            get => _posAddrYRead32;
            set { _posAddrYRead32 = value; OnPropertyChanged(); }
        }

        private string _posAddrTypeZ = "D";
        public string PosAddrTypeZ
        {
            get => _posAddrTypeZ;
            set
            {
                _posAddrTypeZ = NormalizeAddrType(value);
                OnPropertyChanged();
            }
        }

        private int _posAddrIndexZ = 1004;
        public int PosAddrIndexZ
        {
            get => _posAddrIndexZ;
            set { _posAddrIndexZ = value; OnPropertyChanged(); }
        }

        private bool _posAddrZRead32 = true;
        public bool PosAddrZRead32
        {
            get => _posAddrZRead32;
            set { _posAddrZRead32 = value; OnPropertyChanged(); }
        }

        // Custom Memory Stream: for user to read arbitrary addresses
        private int _customMemoryRefresh = 0;
        public int CustomMemoryRefresh
        {
            get { return _customMemoryRefresh; }
            set { _customMemoryRefresh = value; OnPropertyChanged(); }
        }

        // --- Configurable PLC Base Addresses ---
        public int D_R_V
        {
            get => _D_R_V;
            set { _D_R_V = value; OnPropertyChanged(); }
        }
        public int D_W_V
        {
            get => _D_W_V;
            set { _D_W_V = value; OnPropertyChanged(); }
        }
        public int D_W_P
        {
            get => _D_W_P;
            set { _D_W_P = value; OnPropertyChanged(); }
        }
        public int M_W_Base
        {
            get => _M_W_Base;
            set { _M_W_Base = value; OnPropertyChanged(); }
        }
        public int M_R_Base
        {
            get => _M_R_Base;
            set { _M_R_Base = value; OnPropertyChanged(); }
        }
        public int X_R_Base
        {
            get => _X_R_Base;
            set { _X_R_Base = value; OnPropertyChanged(); }
        }
        public int X_W_Base
        {
            get => _X_W_Base;
            set { _X_W_Base = value; OnPropertyChanged(); }
        }
        public int Y_R_Base
        {
            get => _Y_R_Base;
            set { _Y_R_Base = value; OnPropertyChanged(); }
        }
        public int Y_W_Base
        {
            get => _Y_W_Base;
            set { _Y_W_Base = value; OnPropertyChanged(); }
        }

        // --- Position Scaling & Offset Configuration ---
        private string _posUnit = "mm";
        public string PosUnit
        {
            get => _posUnit;
            set { _posUnit = value; OnPropertyChanged(); }
        }

        private int _posDecimals = 2;
        public int PosDecimals
        {
            get => _posDecimals;
            set { _posDecimals = value; OnPropertyChanged(); }
        }

        private double _posScaleX = 1.0;
        public double PosScaleX
        {
            get => _posScaleX;
            set { _posScaleX = value; OnPropertyChanged(); }
        }

        private double _posScaleY = 1.0;
        public double PosScaleY
        {
            get => _posScaleY;
            set { _posScaleY = value; OnPropertyChanged(); }
        }

        private double _posScaleZ = 1.0;
        public double PosScaleZ
        {
            get => _posScaleZ;
            set { _posScaleZ = value; OnPropertyChanged(); }
        }

        private double _posOffsetX = 0;
        public double PosOffsetX
        {
            get => _posOffsetX;
            set { _posOffsetX = value; OnPropertyChanged(); }
        }

        private double _posOffsetY = 0;
        public double PosOffsetY
        {
            get => _posOffsetY;
            set { _posOffsetY = value; OnPropertyChanged(); }
        }

        private double _posOffsetZ = 0;
        public double PosOffsetZ
        {
            get => _posOffsetZ;
            set { _posOffsetZ = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Formats a raw PLC 32-bit register value based on the axis scaling settings.
        /// </summary>
        public string GetFormattedPosition(int rawValue, string axis)
        {
            double scale = axis switch { "X" => PosScaleX, "Y" => PosScaleY, "Z" => PosScaleZ, _ => 1.0 };
            double offset = axis switch { "X" => PosOffsetX, "Y" => PosOffsetY, "Z" => PosOffsetZ, _ => 0.0 };
            double scaledValue = (rawValue * scale) + offset;
            return scaledValue.ToString("F" + PosDecimals);
        }

        private static string NormalizeAddrType(string? addrType)
        {
            string t = string.IsNullOrWhiteSpace(addrType) ? "D" : addrType.Trim().ToUpperInvariant();
            return t is "D" or "M" or "X" or "Y" ? t : "D";
        }

        private int ReadCoordinateSourceValue(string addrType, int addrIndex, bool read32Bit, int fallbackValue)
        {
            try
            {
                string t = NormalizeAddrType(addrType);
                if (t == "D")
                {
                    if (read32Bit)
                    {
                        int[] words32 = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{addrIndex}", 2);
                        if (words32 != null && words32.Length >= 2)
                            return words32[0] | (words32[1] << 16);
                    }
                    else
                    {
                        int[] words16 = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{addrIndex}", 1);
                        if (words16 != null && words16.Length >= 1)
                            return words16[0];
                    }

                    return fallbackValue;
                }

                ePLCControl.DeviceName device = t switch
                {
                    "M" => ePLCControl.DeviceName.M,
                    "X" => ePLCControl.DeviceName.X,
                    "Y" => ePLCControl.DeviceName.Y,
                    _ => ePLCControl.DeviceName.D
                };

                int[] bit = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Bit, device, $"{addrIndex}", 1);
                if (bit != null && bit.Length >= 1)
                    return bit[0];

                return fallbackValue;
            }
            catch
            {
                return fallbackValue;
            }
        }
        #endregion
        #region Commands
        public ICommand ConnectCommand { get; set; }
        public ICommand DisconnectCommand { get; set; }
        public ICommand TestReadCommand { get; set; }
        public ICommand TestWriteCommand { get; set; }
        public ICommand TestBit { get; set; }
        #endregion
    }
    public partial class MainViewModel : ObservableObject
    {
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

        private void DisconnectPLC()
        {
            Status = false;
            AddLog("PLC", "warning", "Connection closed by user");
            if (ePLC != null)
            {
                ePLC.Close();
            }
        }

        private void Monitor()
        {
            while (Status)
            {
                Thread.Sleep(10);
                Status = ePLC.IsConnected;
                if (Status)
                {
                    Read();
                    Write();  // ✓ Send pending write commands to PLC
                    RefreshCustomMemory();
                }
            }
        }
        private void Write()
        {
            // Only write if there are pending commands
            if (!HasPendingWrites())
                return;

            try
            {
                List<PendingWriteItem> pendingSnapshot;
                lock (_pendingWriteLock)
                {
                    pendingSnapshot = _pendingWriteItems
                        .Select(x => new PendingWriteItem { AddrType = x.AddrType, AddrIndex = x.AddrIndex, Value = x.Value })
                        .ToList();
                }

                bool anyWrite = false;
                bool hasWriteError = false;
                bool dVOk = true, dPOk = true, mOk = true, xOk = true, yOk = true;
                string dVErr = "", dPErr = "", mErr = "", xErr = "", yErr = "";

                foreach (var p in pendingSnapshot)
                    AddLog("PC", "info", $"WRITE sent: {p.AddrType}{p.AddrIndex}={p.Value}", "sent");

                // Write D registers (Word)
                try
                {
                    ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_W_V}", arr_W_V);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    dVOk = false;
                    dVErr = ex.Message;
                    AddLog("PC", "error", $"Write D{D_W_V} failed: {ex.Message}", "Write-D");
                }

                try
                {
                    ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_W_P}", arr_W_P);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    dPOk = false;
                    dPErr = ex.Message;
                    AddLog("PC", "error", $"Write D{D_W_P} failed: {ex.Message}", "Write-D");
                }

                // Write M registers (Bit)
                try
                {
                    ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.M, $"{M_W_Base}", arr_W_M);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    mOk = false;
                    mErr = ex.Message;
                    AddLog("PC", "error", $"Write M{M_W_Base} failed: {ex.Message}", "Write-M");
                }

                // Write X registers (Bit)
                try
                {
                    ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.X, $"{X_W_Base}", arr_W_X);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    xOk = false;
                    xErr = ex.Message;
                    AddLog("PC", "error", $"Write X{X_W_Base} failed: {ex.Message}", "Write-X");
                }

                // Write Y registers (Bit)
                try
                {
                    ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.Y, $"{Y_W_Base}", arr_W_Y);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    yOk = false;
                    yErr = ex.Message;
                    AddLog("PC", "error", $"Write Y{Y_W_Base} failed: {ex.Message}", "Write-Y");
                }

                foreach (var p in pendingSnapshot)
                {
                    bool ok = true;
                    string reason = "";
                    string t = p.AddrType.ToUpperInvariant();

                    if (t == "D")
                    {
                        bool inDP = p.AddrIndex >= D_W_P && p.AddrIndex < D_W_P + arr_W_P.Length;
                        bool inDV = p.AddrIndex >= D_W_V && p.AddrIndex < D_W_V + arr_W_V.Length;
                        if (inDP)
                        {
                            ok = dPOk;
                            reason = dPErr;
                        }
                        else if (inDV)
                        {
                            ok = dVOk;
                            reason = dVErr;
                        }
                        else
                        {
                            ok = false;
                            reason = "D address out of configured write ranges";
                        }
                    }
                    else if (t == "M")
                    {
                        ok = mOk;
                        reason = mErr;
                    }
                    else if (t == "X")
                    {
                        ok = xOk;
                        reason = xErr;
                    }
                    else if (t == "Y")
                    {
                        ok = yOk;
                        reason = yErr;
                    }
                    else
                    {
                        ok = false;
                        reason = "Unsupported address type";
                    }

                    if (ok)
                        AddLog("PC", "success", $"WRITE ack: {p.AddrType}{p.AddrIndex}={p.Value}", "ack");
                    else
                        AddLog("PC", "error", $"WRITE failed: {p.AddrType}{p.AddrIndex}={p.Value}", reason);
                }
                
                // ✓ Log and clear after successful write
                if (anyWrite && !hasWriteError)
                {
                    AddLog("PC", "success", $"Write commands sent to PLC → D{D_W_V}/D{D_W_P}/M{M_W_Base}/X{X_W_Base}/Y{Y_W_Base}", "Write cycle");
                    ClearPendingWrites();  // ← Clear arrays after sending to PLC
                    _lastWriteLogTime = DateTime.Now;
                }
                else if (hasWriteError)
                {
                    // Keep pending flag so the next monitor cycle retries.
                    _hasPendingWrites = true;
                }
            }
            catch (Exception ex)
            {
                AddLog("PC", "error", $"Write cycle failed: {ex.Message}", ex.GetType().Name);
                _lastWriteLogTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Check if there are any pending write commands (non-zero values in write arrays)
        /// </summary>
        private bool HasPendingWrites()
        {
            return _hasPendingWrites;
        }

        public void MarkPendingWrite()
        {
            _hasPendingWrites = true;
        }

        public void MarkPendingWrite(string addrType, int addrIndex, int value)
        {
            string normType = string.IsNullOrWhiteSpace(addrType)
                ? "D"
                : addrType.Trim().ToUpperInvariant();

            lock (_pendingWriteLock)
            {
                var existing = _pendingWriteItems.FirstOrDefault(x => x.AddrType == normType && x.AddrIndex == addrIndex);
                if (existing == null)
                {
                    _pendingWriteItems.Add(new PendingWriteItem
                    {
                        AddrType = normType,
                        AddrIndex = addrIndex,
                        Value = value
                    });
                }
                else
                {
                    existing.Value = value;
                }
            }

            _hasPendingWrites = true;
            AddLog("PC", "info", $"WRITE queued: {normType}{addrIndex}={value}", "queued");
        }

        /// <summary>
        /// Clear all pending write commands (reset all write arrays to 0)
        /// </summary>
        private void ClearPendingWrites()
        {
            // Clear D registers
            if (arr_W_V != null)
                Array.Clear(arr_W_V, 0, arr_W_V.Length);
            if (arr_W_P != null)
                Array.Clear(arr_W_P, 0, arr_W_P.Length);

            // Clear M registers
            if (arr_W_M != null)
                Array.Clear(arr_W_M, 0, arr_W_M.Length);

            // Clear X registers
            if (arr_W_X != null)
                Array.Clear(arr_W_X, 0, arr_W_X.Length);

            // Clear Y registers
            if (arr_W_Y != null)
                Array.Clear(arr_W_Y, 0, arr_W_Y.Length);

            lock (_pendingWriteLock)
            {
                _pendingWriteItems.Clear();
            }

            _hasPendingWrites = false;
        }

        private void Read()
        {
            // Always update X/Y/Z from configured source mapping.
            // DReadEnable only controls legacy D32 block reads used as fallback values.
            bool enableLegacyD32 = false;

            // Keep previous values as fallback so one failed read does not freeze coordinates at 0.
            int[] newR32 = (arr_R32 != null && arr_R32.Length == 6)
                ? (int[])arr_R32.Clone()
                : new int[6];

            try
            {
                int[] flag = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{DReadEnable}", 1);
                enableLegacyD32 = flag != null && flag.Length > 0 && flag[0] != 0;
            }
            catch { }

            try
            {
                int[] newData = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_R_V}", Length);
                if (newData != null && newData.Length > 0)
                {
                    Array.Copy(newData, _arr_R_V, Math.Min(newData.Length, _arr_R_V.Length));
                    OnPropertyChanged(nameof(arr_R_V));
                }
            }
            catch (Exception ex)
            {
                if ((DateTime.Now - _lastReadLogTime).TotalSeconds >= 1.0)
                {
                    AddLog("PC", "warning", $"Read D{D_R_V} failed: {ex.Message}", "Read-D");
                }
            }

            if (enableLegacyD32)
            {
                try
                {
                    int[] b1 = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D32Base1}", 6);
                    int[] b2 = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D32Base2}", 6);

                    if (b1 != null && b1.Length >= 6)
                    {
                        for (int i = 0; i < 3; i++)
                            newR32[i] = b1[i * 2] | (b1[i * 2 + 1] << 16);
                    }

                    if (b2 != null && b2.Length >= 6)
                    {
                        for (int i = 0; i < 3; i++)
                            newR32[3 + i] = b2[i * 2] | (b2[i * 2 + 1] << 16);
                    }
                }
                catch (Exception ex)
                {
                    if ((DateTime.Now - _lastReadLogTime).TotalSeconds >= 1.0)
                    {
                        AddLog("PC", "warning", $"Legacy D32 read failed: {ex.Message}", "Read-D32");
                    }
                }
            }

            // These three mapped coordinates are independent from D_R_V / D32 blocks.
            newR32[0] = ReadCoordinateSourceValue(PosAddrTypeX, PosAddrIndexX, PosAddrXRead32, newR32[0]);
            newR32[1] = ReadCoordinateSourceValue(PosAddrTypeY, PosAddrIndexY, PosAddrYRead32, newR32[1]);
            newR32[2] = ReadCoordinateSourceValue(PosAddrTypeZ, PosAddrIndexZ, PosAddrZRead32, newR32[2]);

            arr_R32 = newR32;

            ReadBitRegisters();

            if ((DateTime.Now - _lastReadLogTime).TotalSeconds >= 1.0)
            {
                string legacyState = enableLegacyD32 ? "D32-on" : "D32-off";
                AddLog("PC", "success", $"Read D{D_R_V}({Length}) + CoordMap(XYZ) + M/X/Y [{legacyState}] → OK", "Monitor cycle");
                _lastReadLogTime = DateTime.Now;
            }
        }

        private void ReadBitRegisters()
        {
            try
            {
                int[] mData = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.M, $"{M_R_Base}", 100);
                if (mData != null && mData.Length > 0)
                {
                    int[] newM = new int[_arr_R_M.Length];
                    Array.Copy(mData, newM, Math.Min(mData.Length, newM.Length));
                    arr_R_M = newM;
                }

                int[] xData = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.X, $"{X_R_Base}", 100);
                if (xData != null && xData.Length > 0)
                {
                    int[] newX = new int[_arr_R_X.Length];
                    Array.Copy(xData, newX, Math.Min(xData.Length, newX.Length));
                    arr_R_X = newX;
                }

                int[] yData = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.Y, $"{Y_R_Base}", 100);
                if (yData != null && yData.Length > 0)
                {
                    int[] newY = new int[_arr_R_Y.Length];
                    Array.Copy(yData, newY, Math.Min(yData.Length, newY.Length));
                    arr_R_Y = newY;
                }
            }
            catch { }
        }
        private bool ReadDevice(int iAddress)
        {
            if ((iAddress - D_R_V) > 0 && (iAddress - D_R_V) < arr_R_V.Length)
            {
                return arr_R_V[iAddress - D_R_V] == 0 ? false : true;
            }
            else
            {
                return false;
            }
        }
        private void WriteDevice(int iAddress, bool value)
        {
            if ((iAddress - D_W_V) >= 0 && (iAddress - D_W_V) < arr_W_V.Length)
            {
                arr_W_V[iAddress - D_W_V] = value ? 1 : 0;
            }
        }

        // Read custom memory range for user-defined display
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

                        var subCommand = entry.AddrType switch
                        {
                            "M" => ePLCControl.SubCommand.Bit,
                            "X" => ePLCControl.SubCommand.Bit,
                            "Y" => ePLCControl.SubCommand.Bit,
                            _ => ePLCControl.SubCommand.Word
                        };

                        int[] result = ePLC.ReadDeviceBlock(subCommand, devName, $"{entry.AddrIndex}", 1);
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

        public void SetPosition(int iAddress, double value)
        {
            if ((iAddress - D_W_P) >= 0 && (iAddress - D_W_P + 1) < arr_W_Position.Length)
            {
                int index = iAddress - D_W_P;
                SetCurrentPosition(arr_W_Position, index, value);
            }
        }
        public double GetCurrentPosition(int[] arr, int index)
        {
            return arr[index] + (arr[index + 1] << 16);
        }
        public void SetCurrentPosition(int[] arr, int index, double value)
        {
            int v = (int)value;
            arr[index] = v & 0xFFFF;
            arr[index + 1] = (v >> 16) & 0xFFFF;
        }
        private int[] GetDataValue(int[] arr, int index)
        {
            if (arr.Length == 0)
            {
                return new int[0];
            }
            int iVal = arr[index];
            return ePLC.WordToBit(iVal).ToList().Select(x => int.Parse(x.ToString())).ToArray();
        }
        public bool GetBit(int word, int bit)
        {
            return ((word >> bit) & 1) == 1;
        }

        public int SetBit(int word, int bit, bool value)
        {
            if (value) return word | (1 << bit);
            else return word & ~(1 << bit);
        }
        private int[] GetDataValue_(int[] arr, int index)
        {
            if (arr == null || index < 0 || index >= arr.Length)
                return new int[0];

            int word = arr[index];
            int[] bits = new int[16];

            for (int i = 0; i < 16; i++)
                bits[i] = (word >> i) & 1;

            return bits;
        }
        private void SetDataValue_(int[] arr, int index, int[] bits)
        {
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

        // Jog control: Write marks for jog commands
        public void JogStart(int markAddress)
        {
            try
            {
                if (ePLC == null || !ePLC.IsConnected || !Status)
                {
                    AddLog("UI", "warning", $"Jog start ignored (PLC disconnected): M{markAddress}");
                    return;
                }

                int offset = markAddress - M_W_Base;
                if (offset < 0 || offset >= arr_W_M.Length)
                {
                    AddLog("UI", "error", $"Jog start out of range: M{markAddress}", $"Valid range M{M_W_Base}..M{M_W_Base + arr_W_M.Length - 1}");
                    return;
                }

                arr_W_M[offset] = 1;
                MarkPendingWrite("M", markAddress, 1);
                AddLog("UI", "info", $"Jog start queued M{markAddress}=1");
            }
            catch (Exception ex)
            {
                AddLog("PC", "error", $"Jog start failed M{markAddress}: {ex.Message}", "JogStart");
            }
        }

        public void JogStop(int markAddress)
        {
            try
            {
                if (ePLC == null || !ePLC.IsConnected || !Status)
                {
                    AddLog("UI", "warning", $"Jog stop ignored (PLC disconnected): M{markAddress}");
                    return;
                }

                int offset = markAddress - M_W_Base;
                if (offset < 0 || offset >= arr_W_M.Length)
                {
                    AddLog("UI", "error", $"Jog stop out of range: M{markAddress}", $"Valid range M{M_W_Base}..M{M_W_Base + arr_W_M.Length - 1}");
                    return;
                }

                arr_W_M[offset] = 0;
                MarkPendingWrite("M", markAddress, 0);
                AddLog("UI", "info", $"Jog stop queued M{markAddress}=0");
            }
            catch (Exception ex)
            {
                AddLog("PC", "error", $"Jog stop failed M{markAddress}: {ex.Message}", "JogStop");
            }
        }

        // Global log method for all components
        public void AddLog(string source, string status, string message, string detail = "")
        {
            if (_allLogs.Count > 0) 
                _allLogs[^1].IsNewest = false;
            
            var log = new LogItem 
            { 
                Source = source, 
                Status = status, 
                Message = message, 
                Detail = detail, 
                IsNewest = true 
            };
            
            _allLogs.Add(log);
            if (_allLogs.Count > 500) 
                _allLogs.RemoveAt(0);
            
            LogAdded?.Invoke(this, log);
        }
    }
    public static class PlcBitHelper
    {
        public static int[] BoolArrayToIntArray(bool[] bits)
        {
            if (bits == null) return new int[0];

            int[] arr = new int[bits.Length];
            for (int i = 0; i < bits.Length; i++)
                arr[i] = bits[i] ? 1 : 0;

            return arr;
        }
        public static bool[] IntArrayToBoolArray(int[] arr)
        {
            bool[] bits = new bool[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                bits[i] = arr[i] != 0;
            return bits;
        }
        public static bool[] WordToBits(int word)
        {
            bool[] bits = new bool[16];
            for (int i = 0; i < 16; i++)
                bits[i] = ((word >> i) & 1) == 1;
            return bits;
        }

        /// <summary>
        /// Convert bool[16] -> word (LSB first)
        /// </summary>
        public static int BitsToWord(bool[] bits)
        {
            if (bits == null) return 0;
            int word = 0;
            for (int i = 0; i < bits.Length && i < 16; i++)
                if (bits[i]) word |= (1 << i);
            return word;
        }

        // ================================
        // STRING BIT (DEBUG ONLY)
        // ================================

        /// <summary>
        /// Word -> bit string (LSB first)
        /// </summary>
        public static string WordToBitString(int word)
        {
            word &= 0xFFFF;
            char[] bits = new char[16];
            for (int i = 0; i < 16; i++)
                bits[i] = ((word >> i) & 1) == 1 ? '1' : '0';
            return new string(bits);
        }

        /// <summary>
        /// Bit string -> word (LSB first)
        /// </summary>
        public static int BitStringToWord(string bits)
        {
            if (string.IsNullOrEmpty(bits)) return 0;
            int word = 0;
            for (int i = 0; i < bits.Length && i < 16; i++)
                if (bits[i] == '1')
                    word |= (1 << i);
            return word;
        }

        // ================================
        // BIT GET / SET
        // ================================

        public static bool GetBit(int word, int bitIndex)
        {
            return ((word >> bitIndex) & 1) == 1;
        }

        public static int SetBit(int word, int bitIndex, bool value)
        {
            if (value)
                return word | (1 << bitIndex);
            else
                return word & ~(1 << bitIndex);
        }

        // ================================
        // 32-BIT POSITION (2 WORDS PLC)
        // ================================

        /// <summary>
        /// Get 32-bit position from 2 PLC registers
        /// </summary>
        public static int GetCurrentPosition(int[] arr, int index)
        {
            return arr[index] | (arr[index + 1] << 16);
        }

        /// <summary>
        /// Set 32-bit position into 2 PLC registers
        /// </summary>
        public static void SetCurrentPosition(int[] arr, int index, int value)
        {
            arr[index] = value & 0xFFFF;        // Low word
            arr[index + 1] = (value >> 16) & 0xFFFF; // High word
        }
    }

}
