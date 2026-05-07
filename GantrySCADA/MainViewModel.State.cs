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
        private ePLCControl? ePLC;
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
        private int _globalEmergencyStopAddress = 3100;
        private int _globalGoHomeAddress = 502;
        private int _globalResetFaultAddress = 300;
        private int _globalStartAddress = 2000;
        private bool _globalEmergencyStopEnabled;
        private bool _globalGoHomeEnabled;
        private bool _globalResetFaultEnabled;
        private bool _globalStartEnabled;
        private int _X_R_Base = 0;
        private int _X_W_Base = 100;
        private int _Y_R_Base = 0;
        private int _Y_W_Base = 100;
        private int _velocityWriteAddress = 406;
        private double _velocityWriteScale = 10.0;
        private double _velocitySetpoint = 1.5;

        private string _axis1PositionAddrType = "D";
        private int _axis1PositionAddrIndex = 0;
        private bool _axis1PositionRead32 = true;
        private string _axis1SpeedAddrType = "D";
        private int _axis1SpeedAddrIndex = 4;
        private bool _axis1SpeedRead32 = true;
        private string _axis1ErrorAddrType = "U0G";
        private int _axis1ErrorAddrIndex = 806;
        private string _axis1WarningAddrType = "U0G";
        private int _axis1WarningAddrIndex = 807;
        private string _axis1StatusAddrType = "U0G";
        private int _axis1StatusAddrIndex = 809;
        private string _axis1StartAddrType = "U0G";
        private int _axis1StartAddrIndex = 1500;

        private string _axis2PositionAddrType = "D";
        private int _axis2PositionAddrIndex = 10;
        private bool _axis2PositionRead32 = true;
        private string _axis2SpeedAddrType = "D";
        private int _axis2SpeedAddrIndex = 14;
        private bool _axis2SpeedRead32 = true;
        private string _axis2ErrorAddrType = "U0G";
        private int _axis2ErrorAddrIndex = 906;
        private string _axis2WarningAddrType = "U0G";
        private int _axis2WarningAddrIndex = 907;
        private string _axis2StatusAddrType = "U0G";
        private int _axis2StatusAddrIndex = 909;
        private string _axis2StartAddrType = "U0G";
        private int _axis2StartAddrIndex = 1600;

        private string _axis3PositionAddrType = "D";
        private int _axis3PositionAddrIndex = 20;
        private bool _axis3PositionRead32 = true;
        private string _axis3SpeedAddrType = "D";
        private int _axis3SpeedAddrIndex = 24;
        private bool _axis3SpeedRead32 = true;
        private string _axis3ErrorAddrType = "U0G";
        private int _axis3ErrorAddrIndex = 1006;
        private string _axis3WarningAddrType = "U0G";
        private int _axis3WarningAddrIndex = 1007;
        private string _axis3StatusAddrType = "U0G";
        private int _axis3StatusAddrIndex = 1009;
        private string _axis3StartAddrType = "U0G";
        private int _axis3StartAddrIndex = 1700;

        private string _axis4PositionAddrType = "D";
        private int _axis4PositionAddrIndex = 30;
        private bool _axis4PositionRead32 = true;
        private string _axis4SpeedAddrType = "D";
        private int _axis4SpeedAddrIndex = 34;
        private bool _axis4SpeedRead32 = true;
        private string _axis4ErrorAddrType = "U0G";
        private int _axis4ErrorAddrIndex = 1106;
        private string _axis4WarningAddrType = "U0G";
        private int _axis4WarningAddrIndex = 1107;
        private string _axis4StatusAddrType = "U0G";
        private int _axis4StatusAddrIndex = 1109;
        private string _axis4StartAddrType = "U0G";
        private int _axis4StartAddrIndex = 1800;

        private int _axis1PositionValue;
        private int _axis1SpeedValue;
        private int _axis1ErrorValue;
        private int _axis1WarningValue;
        private int _axis1StatusValue;
        private int _axis1StartValue;

        private int _axis2PositionValue;
        private int _axis2SpeedValue;
        private int _axis2ErrorValue;
        private int _axis2WarningValue;
        private int _axis2StatusValue;
        private int _axis2StartValue;

        private int _axis3PositionValue;
        private int _axis3SpeedValue;
        private int _axis3ErrorValue;
        private int _axis3WarningValue;
        private int _axis3StatusValue;
        private int _axis3StartValue;

        private int _axis4PositionValue;
        private int _axis4SpeedValue;
        private int _axis4ErrorValue;
        private int _axis4WarningValue;
        private int _axis4StatusValue;
        private int _axis4StartValue;

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
            public bool AddrIndexIsHex { get; set; }
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
        private readonly object _logLock = new();  // Thread-safe logging
        public List<LogItem> AllLogs => _allLogs;

        public event EventHandler<LogItem>? LogAdded;

        public class CustomMemoryEntry
        {
            public string AddrType { get; set; } = "D";
            public int AddrIndex { get; set; }
            public string AddrIndexText { get; set; } = "";
            public bool Read32 { get; set; }
            public bool AddrIndexIsHex { get; set; }
            public int CurrentValue { get; set; }
            public DateTime LastUpdate { get; set; } = DateTime.Now;
        }

        private List<CustomMemoryEntry> _customMemoryEntries = new();
        public List<CustomMemoryEntry> CustomMemoryEntries => _customMemoryEntries;

        public class BufferRegisterRecord
        {
            public string Address { get; set; } = "";
            public int Value { get; set; }
            public string BinaryString 
            {
                get 
                {
                    // Display as unsigned 16-bit
                    ushort uval = (ushort)(Value & 0xFFFF);
                    string bin = Convert.ToString(uval, 2).PadLeft(16, '0');
                    return string.Join(" ", bin.ToCharArray());
                }
            }
            public string HexString
            {
                get { return ((ushort)(Value & 0xFFFF)).ToString("X4"); }
            }
            public int DecimalValue
            {
                get { return (int)(ushort)(Value & 0xFFFF); }
            }
        }

        private List<BufferRegisterRecord> _sentBufferRecords = new();
        public List<BufferRegisterRecord> SentBufferRecords => _sentBufferRecords;

        private List<BufferRegisterRecord> _sentBufferRecordsAxis2 = new();
        public List<BufferRegisterRecord> SentBufferRecordsAxis2 => _sentBufferRecordsAxis2;
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

        private bool _isDxfSending;
        public bool IsDxfSending { get => _isDxfSending; set => SetProperty(ref _isDxfSending, value); }

        private string _dxfSendStatus = "Ready";
        public string DxfSendStatus { get => _dxfSendStatus; set => SetProperty(ref _dxfSendStatus, value); }

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

        public int GlobalEmergencyStopAddress
        {
            get => _globalEmergencyStopAddress;
            set { _globalEmergencyStopAddress = value; OnPropertyChanged(); }
        }

        public int GlobalGoHomeAddress
        {
            get => _globalGoHomeAddress;
            set { _globalGoHomeAddress = value; OnPropertyChanged(); }
        }

        public int GlobalResetFaultAddress
        {
            get => _globalResetFaultAddress;
            set { _globalResetFaultAddress = value; OnPropertyChanged(); }
        }

        public int GlobalStartAddress
        {
            get => _globalStartAddress;
            set { _globalStartAddress = value; OnPropertyChanged(); }
        }

        public bool GlobalEmergencyStopEnabled
        {
            get => _globalEmergencyStopEnabled;
            set => SetProperty(ref _globalEmergencyStopEnabled, value);
        }

        public bool GlobalGoHomeEnabled
        {
            get => _globalGoHomeEnabled;
            set => SetProperty(ref _globalGoHomeEnabled, value);
        }

        public bool GlobalResetFaultEnabled
        {
            get => _globalResetFaultEnabled;
            set => SetProperty(ref _globalResetFaultEnabled, value);
        }

        public bool GlobalStartEnabled
        {
            get => _globalStartEnabled;
            set => SetProperty(ref _globalStartEnabled, value);
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

        public string Axis1PositionAddrType
        {
            get => _axis1PositionAddrType;
            set { _axis1PositionAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis1PositionAddrIndex
        {
            get => _axis1PositionAddrIndex;
            set { _axis1PositionAddrIndex = value; OnPropertyChanged(); }
        }

        public bool Axis1PositionRead32
        {
            get => _axis1PositionRead32;
            set { _axis1PositionRead32 = value; OnPropertyChanged(); }
        }

        public string Axis1SpeedAddrType
        {
            get => _axis1SpeedAddrType;
            set { _axis1SpeedAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis1SpeedAddrIndex
        {
            get => _axis1SpeedAddrIndex;
            set { _axis1SpeedAddrIndex = value; OnPropertyChanged(); }
        }

        public bool Axis1SpeedRead32
        {
            get => _axis1SpeedRead32;
            set { _axis1SpeedRead32 = value; OnPropertyChanged(); }
        }

        public string Axis1ErrorAddrType
        {
            get => _axis1ErrorAddrType;
            set { _axis1ErrorAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis1ErrorAddrIndex
        {
            get => _axis1ErrorAddrIndex;
            set { _axis1ErrorAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis1WarningAddrType
        {
            get => _axis1WarningAddrType;
            set { _axis1WarningAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis1WarningAddrIndex
        {
            get => _axis1WarningAddrIndex;
            set { _axis1WarningAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis1StatusAddrType
        {
            get => _axis1StatusAddrType;
            set { _axis1StatusAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis1StatusAddrIndex
        {
            get => _axis1StatusAddrIndex;
            set { _axis1StatusAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis1StartAddrType
        {
            get => _axis1StartAddrType;
            set { _axis1StartAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis1StartAddrIndex
        {
            get => _axis1StartAddrIndex;
            set { _axis1StartAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis2PositionAddrType
        {
            get => _axis2PositionAddrType;
            set { _axis2PositionAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis2PositionAddrIndex
        {
            get => _axis2PositionAddrIndex;
            set { _axis2PositionAddrIndex = value; OnPropertyChanged(); }
        }

        public bool Axis2PositionRead32
        {
            get => _axis2PositionRead32;
            set { _axis2PositionRead32 = value; OnPropertyChanged(); }
        }

        public string Axis2SpeedAddrType
        {
            get => _axis2SpeedAddrType;
            set { _axis2SpeedAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis2SpeedAddrIndex
        {
            get => _axis2SpeedAddrIndex;
            set { _axis2SpeedAddrIndex = value; OnPropertyChanged(); }
        }

        public bool Axis2SpeedRead32
        {
            get => _axis2SpeedRead32;
            set { _axis2SpeedRead32 = value; OnPropertyChanged(); }
        }

        public string Axis2ErrorAddrType
        {
            get => _axis2ErrorAddrType;
            set { _axis2ErrorAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis2ErrorAddrIndex
        {
            get => _axis2ErrorAddrIndex;
            set { _axis2ErrorAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis2WarningAddrType
        {
            get => _axis2WarningAddrType;
            set { _axis2WarningAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis2WarningAddrIndex
        {
            get => _axis2WarningAddrIndex;
            set { _axis2WarningAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis2StatusAddrType
        {
            get => _axis2StatusAddrType;
            set { _axis2StatusAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis2StatusAddrIndex
        {
            get => _axis2StatusAddrIndex;
            set { _axis2StatusAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis2StartAddrType
        {
            get => _axis2StartAddrType;
            set { _axis2StartAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis2StartAddrIndex
        {
            get => _axis2StartAddrIndex;
            set { _axis2StartAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis3PositionAddrType
        {
            get => _axis3PositionAddrType;
            set { _axis3PositionAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis3PositionAddrIndex
        {
            get => _axis3PositionAddrIndex;
            set { _axis3PositionAddrIndex = value; OnPropertyChanged(); }
        }

        public bool Axis3PositionRead32
        {
            get => _axis3PositionRead32;
            set { _axis3PositionRead32 = value; OnPropertyChanged(); }
        }

        public string Axis3SpeedAddrType
        {
            get => _axis3SpeedAddrType;
            set { _axis3SpeedAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis3SpeedAddrIndex
        {
            get => _axis3SpeedAddrIndex;
            set { _axis3SpeedAddrIndex = value; OnPropertyChanged(); }
        }

        public bool Axis3SpeedRead32
        {
            get => _axis3SpeedRead32;
            set { _axis3SpeedRead32 = value; OnPropertyChanged(); }
        }

        public string Axis3ErrorAddrType
        {
            get => _axis3ErrorAddrType;
            set { _axis3ErrorAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis3ErrorAddrIndex
        {
            get => _axis3ErrorAddrIndex;
            set { _axis3ErrorAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis3WarningAddrType
        {
            get => _axis3WarningAddrType;
            set { _axis3WarningAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis3WarningAddrIndex
        {
            get => _axis3WarningAddrIndex;
            set { _axis3WarningAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis3StatusAddrType
        {
            get => _axis3StatusAddrType;
            set { _axis3StatusAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis3StatusAddrIndex
        {
            get => _axis3StatusAddrIndex;
            set { _axis3StatusAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis3StartAddrType
        {
            get => _axis3StartAddrType;
            set { _axis3StartAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis3StartAddrIndex
        {
            get => _axis3StartAddrIndex;
            set { _axis3StartAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis4PositionAddrType
        {
            get => _axis4PositionAddrType;
            set { _axis4PositionAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis4PositionAddrIndex
        {
            get => _axis4PositionAddrIndex;
            set { _axis4PositionAddrIndex = value; OnPropertyChanged(); }
        }

        public bool Axis4PositionRead32
        {
            get => _axis4PositionRead32;
            set { _axis4PositionRead32 = value; OnPropertyChanged(); }
        }

        public string Axis4SpeedAddrType
        {
            get => _axis4SpeedAddrType;
            set { _axis4SpeedAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis4SpeedAddrIndex
        {
            get => _axis4SpeedAddrIndex;
            set { _axis4SpeedAddrIndex = value; OnPropertyChanged(); }
        }

        public bool Axis4SpeedRead32
        {
            get => _axis4SpeedRead32;
            set { _axis4SpeedRead32 = value; OnPropertyChanged(); }
        }

        public string Axis4ErrorAddrType
        {
            get => _axis4ErrorAddrType;
            set { _axis4ErrorAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis4ErrorAddrIndex
        {
            get => _axis4ErrorAddrIndex;
            set { _axis4ErrorAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis4WarningAddrType
        {
            get => _axis4WarningAddrType;
            set { _axis4WarningAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis4WarningAddrIndex
        {
            get => _axis4WarningAddrIndex;
            set { _axis4WarningAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis4StatusAddrType
        {
            get => _axis4StatusAddrType;
            set { _axis4StatusAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis4StatusAddrIndex
        {
            get => _axis4StatusAddrIndex;
            set { _axis4StatusAddrIndex = value; OnPropertyChanged(); }
        }

        public string Axis4StartAddrType
        {
            get => _axis4StartAddrType;
            set { _axis4StartAddrType = NormalizeAddrType(value); OnPropertyChanged(); }
        }

        public int Axis4StartAddrIndex
        {
            get => _axis4StartAddrIndex;
            set { _axis4StartAddrIndex = value; OnPropertyChanged(); }
        }

        public int Axis1PositionValue
        {
            get => _axis1PositionValue;
            set => SetProperty(ref _axis1PositionValue, value);
        }

        public int Axis1SpeedValue
        {
            get => _axis1SpeedValue;
            set => SetProperty(ref _axis1SpeedValue, value);
        }

        public int Axis1ErrorValue
        {
            get => _axis1ErrorValue;
            set => SetProperty(ref _axis1ErrorValue, value);
        }

        public int Axis1WarningValue
        {
            get => _axis1WarningValue;
            set => SetProperty(ref _axis1WarningValue, value);
        }

        public int Axis1StatusValue
        {
            get => _axis1StatusValue;
            set => SetProperty(ref _axis1StatusValue, value);
        }

        public int Axis1StartValue
        {
            get => _axis1StartValue;
            set => SetProperty(ref _axis1StartValue, value);
        }

        public int Axis2PositionValue
        {
            get => _axis2PositionValue;
            set => SetProperty(ref _axis2PositionValue, value);
        }

        public int Axis2SpeedValue
        {
            get => _axis2SpeedValue;
            set => SetProperty(ref _axis2SpeedValue, value);
        }

        public int Axis2ErrorValue
        {
            get => _axis2ErrorValue;
            set => SetProperty(ref _axis2ErrorValue, value);
        }

        public int Axis2WarningValue
        {
            get => _axis2WarningValue;
            set => SetProperty(ref _axis2WarningValue, value);
        }

        public int Axis2StatusValue
        {
            get => _axis2StatusValue;
            set => SetProperty(ref _axis2StatusValue, value);
        }

        public int Axis2StartValue
        {
            get => _axis2StartValue;
            set => SetProperty(ref _axis2StartValue, value);
        }

        public int Axis3PositionValue
        {
            get => _axis3PositionValue;
            set => SetProperty(ref _axis3PositionValue, value);
        }

        public int Axis3SpeedValue
        {
            get => _axis3SpeedValue;
            set => SetProperty(ref _axis3SpeedValue, value);
        }

        public int Axis3ErrorValue
        {
            get => _axis3ErrorValue;
            set => SetProperty(ref _axis3ErrorValue, value);
        }

        public int Axis3WarningValue
        {
            get => _axis3WarningValue;
            set => SetProperty(ref _axis3WarningValue, value);
        }

        public int Axis3StatusValue
        {
            get => _axis3StatusValue;
            set => SetProperty(ref _axis3StatusValue, value);
        }

        public int Axis3StartValue
        {
            get => _axis3StartValue;
            set => SetProperty(ref _axis3StartValue, value);
        }

        public int Axis4PositionValue
        {
            get => _axis4PositionValue;
            set => SetProperty(ref _axis4PositionValue, value);
        }

        public int Axis4SpeedValue
        {
            get => _axis4SpeedValue;
            set => SetProperty(ref _axis4SpeedValue, value);
        }

        public int Axis4ErrorValue
        {
            get => _axis4ErrorValue;
            set => SetProperty(ref _axis4ErrorValue, value);
        }

        public int Axis4WarningValue
        {
            get => _axis4WarningValue;
            set => SetProperty(ref _axis4WarningValue, value);
        }

        public int Axis4StatusValue
        {
            get => _axis4StatusValue;
            set => SetProperty(ref _axis4StatusValue, value);
        }

        public int Axis4StartValue
        {
            get => _axis4StartValue;
            set => SetProperty(ref _axis4StartValue, value);
        }

        private Dictionary<int, uint> _dxfPointSpeeds = new();
        public Dictionary<int, uint> DxfPointSpeeds
        {
            get => _dxfPointSpeeds;
            set { _dxfPointSpeeds = value; OnPropertyChanged(); }
        }

        public uint GetSpeedForPoint(int pointIndex)
        {
            if (_dxfPointSpeeds.TryGetValue(pointIndex, out uint speed))
                return speed;
            return DxfDefaultSpeed;
        }

        public void SetSpeedForPoint(int pointIndex, uint speed)
        {
            _dxfPointSpeeds[pointIndex] = speed;
            OnPropertyChanged(nameof(DxfPointSpeeds));
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

        private static string BuildBufferAddress(string addrType, int addrIndex, string? addrIndexText = null, bool addrIndexIsHex = false)
        {
            string prefix = addrType.Trim().ToUpperInvariant();
            prefix = prefix.Replace("/", string.Empty).Replace("\\", string.Empty);
            if (!prefix.Contains("G", StringComparison.OrdinalIgnoreCase))
                prefix += "G";

            if (!prefix.Contains("\\G", StringComparison.OrdinalIgnoreCase))
                prefix = prefix.Replace("G", "\\G");

            string indexText = addrIndexIsHex
                ? (string.IsNullOrWhiteSpace(addrIndexText) ? addrIndex.ToString("X") : addrIndexText.Trim().ToUpperInvariant())
                : addrIndex.ToString();

            return prefix + indexText;
        }

        private int ReadCoordinateSourceValue(string addrType, int addrIndex, bool read32Bit, int fallbackValue)
        {
            var plc = ePLC;
            if (plc == null) return fallbackValue;

            try
            {
                string t = NormalizeAddrType(addrType);
                if (t == "D")
                {
                    if (read32Bit)
                    {
                        int[] words32 = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{addrIndex}", 2);
                        if (words32 != null && words32.Length >= 2)
                            return words32[0] | (words32[1] << 16);
                    }
                    else
                    {
                        int[] words16 = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{addrIndex}", 1);
                        if (words16 != null && words16.Length >= 1)
                            return words16[0];
                    }

                    return fallbackValue;
                }

                if (IsBufferType(t))
                {
                    if (read32Bit)
                    {
                        int[] words32 = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.Buffer, BuildBufferAddress(t, addrIndex), 2);
                        if (words32 != null && words32.Length >= 2)
                            return words32[0] | (words32[1] << 16);
                    }
                    else
                    {
                        int[] words16 = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.Buffer, BuildBufferAddress(t, addrIndex), 1);
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

                int[] bit = plc.ReadDeviceBlock(ePLCControl.SubCommand.Bit, device, $"{addrIndex}", 1);
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
