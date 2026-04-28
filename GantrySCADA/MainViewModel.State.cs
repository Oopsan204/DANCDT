using CommunityToolkit.Mvvm.ComponentModel;
using NVKProject.PLC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        private int _M_R_Base = 0;
        private int _M_W_Base = 3000;
        private int _jogXPosAddress = 3001;
        private int _jogXNegAddress = 3000;
        private int _jogYPosAddress = 3002;
        private int _jogYNegAddress = 3003;
        private int _jogZPosAddress = 3004;
        private int _jogZNegAddress = 3005;
        private int _X_R_Base = 0;
        private int _X_W_Base = 100;
        private int _Y_R_Base = 0;
        private int _Y_W_Base = 100;
        private int _velocityWriteAddress = 406;
        private double _velocityWriteScale = 10.0;
        private double _velocitySetpoint = 1.5;

        private int[] _arr_R32 = new int[6];

        public int[] arr_W_Position = new int[99];
        public int[] arr_W_P = new int[6];
        public int[] arr_W_M = new int[100];
        public int[] arr_W_X = new int[100];
        public int[] arr_W_Y = new int[100];
        private int[] _arr_R_V = new int[99];
        private int[] _arr_R_M = new int[100];
        private int[] _arr_R_X = new int[100];
        private int[] _arr_R_Y = new int[100];
        public int[] arr_W_V = new int[99];

        private DateTime _lastWriteLogTime = DateTime.MinValue;
        private DateTime _lastReadLogTime = DateTime.MinValue;

        private bool _hasPendingWrites = false;
        private readonly object _pendingWriteLock = new();
        private readonly List<PendingWriteItem> _pendingWriteItems = new();
        private readonly object _plcSync = new();
        private Thread? _monitorThread;
        private volatile bool _monitorStopRequested;
        private volatile bool _monitorRunning;
        private DateTime _lastReconnectAttempt = DateTime.MinValue;
        private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(5);

        private sealed class PendingWriteItem
        {
            public string AddrType { get; set; } = "D";
            public int AddrIndex { get; set; }
            public int Value { get; set; }
            public string AddrIndexText { get; set; } = "";
        }

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

        public event EventHandler<LogItem>? LogAdded;

        public class CustomMemoryEntry
        {
            public string AddrType { get; set; } = "D";
            public int AddrIndex { get; set; }
            public string AddrIndexText { get; set; } = "";
            public int CurrentValue { get; set; }
            public DateTime LastUpdate { get; set; } = DateTime.Now;
        }

        private List<CustomMemoryEntry> _customMemoryEntries = new();
        public List<CustomMemoryEntry> CustomMemoryEntries => _customMemoryEntries;
        #endregion

        #region Properties
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

        public int[] arr_R_V
        {
            get { return _arr_R_V; }
            private set { SetProperty(ref _arr_R_V, value); }
        }

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

        public int[] arr_R32
        {
            get { return _arr_R32; }
            set { SetProperty(ref _arr_R32, value); }
        }

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

        private int _dReadEnable = 3000;
        public int DReadEnable
        {
            get { return _dReadEnable; }
            set { _dReadEnable = value; OnPropertyChanged(); }
        }

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

        private int _posAddrIndexX = 4000;
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

        private int _posAddrIndexY = 4002;
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

        private int _posAddrIndexZ = 4004;
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

        private int _customMemoryRefresh = 0;
        public int CustomMemoryRefresh
        {
            get { return _customMemoryRefresh; }
            set { _customMemoryRefresh = value; OnPropertyChanged(); }
        }

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

        public int JogXPosAddress
        {
            get => _jogXPosAddress;
            set { _jogXPosAddress = value; OnPropertyChanged(); }
        }

        public int JogXNegAddress
        {
            get => _jogXNegAddress;
            set { _jogXNegAddress = value; OnPropertyChanged(); }
        }

        public int JogYPosAddress
        {
            get => _jogYPosAddress;
            set { _jogYPosAddress = value; OnPropertyChanged(); }
        }

        public int JogYNegAddress
        {
            get => _jogYNegAddress;
            set { _jogYNegAddress = value; OnPropertyChanged(); }
        }

        public int JogZPosAddress
        {
            get => _jogZPosAddress;
            set { _jogZPosAddress = value; OnPropertyChanged(); }
        }

        public int JogZNegAddress
        {
            get => _jogZNegAddress;
            set { _jogZNegAddress = value; OnPropertyChanged(); }
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

        public int VelocityWriteAddress
        {
            get => _velocityWriteAddress;
            set { _velocityWriteAddress = value; OnPropertyChanged(); }
        }

        public double VelocityWriteScale
        {
            get => _velocityWriteScale;
            set { _velocityWriteScale = value; OnPropertyChanged(); }
        }

        public double VelocitySetpoint
        {
            get => _velocitySetpoint;
            set { _velocitySetpoint = value; OnPropertyChanged(); }
        }

        private string _posUnit = "µm";
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

        private double _posScaleX = 0.1;
        public double PosScaleX
        {
            get => _posScaleX;
            set { _posScaleX = value; OnPropertyChanged(); }
        }

        private double _posScaleY = 0.1;
        public double PosScaleY
        {
            get => _posScaleY;
            set { _posScaleY = value; OnPropertyChanged(); }
        }

        private double _posScaleZ = 0.1;
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
        #endregion

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
            if (t.StartsWith("U", StringComparison.OrdinalIgnoreCase))
                return t;
            return t is "D" or "M" or "X" or "Y" ? t : "D";
        }

        private static bool IsBufferType(string addrType)
        {
            return addrType.StartsWith("U", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildBufferAddress(string addrType, int addrIndex, string? addrIndexText = null)
        {
            string prefix = addrType.Trim().ToUpperInvariant();
            prefix = prefix.Replace("\\", string.Empty).Replace("/", string.Empty);
            if (!prefix.Contains("G", StringComparison.OrdinalIgnoreCase))
                prefix += "G";

            string hexIndex = !string.IsNullOrWhiteSpace(addrIndexText)
                ? addrIndexText.Trim().ToUpperInvariant()
                : addrIndex.ToString("X");

            return prefix + hexIndex;
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

                if (IsBufferType(t))
                {
                    if (read32Bit)
                    {
                        int[] words32 = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.Buffer, BuildBufferAddress(t, addrIndex), 2);
                        if (words32 != null && words32.Length >= 2)
                            return words32[0] | (words32[1] << 16);
                    }
                    else
                    {
                        int[] words16 = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.Buffer, BuildBufferAddress(t, addrIndex), 1);
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
    }
}
