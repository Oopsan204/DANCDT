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
        private string ipAddress = "192.168.3.39";
        private int port = 3000;
        private int networkNo = 0;
        private int stationNo = 0;
        private int stationPLCNo = 255;
        private bool status;
        #endregion

        #region Connection Properties
        public string IpAddress { get => ipAddress; set { ipAddress = value; OnPropertyChanged(); } }
        public int Port { get => port; set { port = value; OnPropertyChanged(); } }
        public int NetworkNo { get => networkNo; set { networkNo = value; } }
        public int StationNo { get => stationNo; set { stationNo = value; OnPropertyChanged(); } }
        public int StationPLCNo { get => stationPLCNo; set { stationPLCNo = value; OnPropertyChanged(); } }
        public bool Status { get => status; set { SetProperty(ref status, value); } }
        #endregion

        #region Display Properties (Read from PLC)
        private int _currentSpeed;
        public int CurrentSpeed { get => _currentSpeed; set => SetProperty(ref _currentSpeed, value); }

        private double _currentPosX;
        public double CurrentPosX { get => _currentPosX; set => SetProperty(ref _currentPosX, value); }

        private double _currentPosY;
        public double CurrentPosY { get => _currentPosY; set => SetProperty(ref _currentPosY, value); }

        private bool _m3000_XPlus;
        public bool M3000_XPlus { get => _m3000_XPlus; set => SetProperty(ref _m3000_XPlus, value); }

        private bool _m3001_XMinus;
        public bool M3001_XMinus { get => _m3001_XMinus; set => SetProperty(ref _m3001_XMinus, value); }

        private bool _m3002_YPlus;
        public bool M3002_YPlus { get => _m3002_YPlus; set => SetProperty(ref _m3002_YPlus, value); }

        private bool _m3003_YMinus;
        public bool M3003_YMinus { get => _m3003_YMinus; set => SetProperty(ref _m3003_YMinus, value); }

        private bool _m3004_ZPlus;
        public bool M3004_ZPlus { get => _m3004_ZPlus; set => SetProperty(ref _m3004_ZPlus, value); }

        private bool _m3005_ZMinus;
        public bool M3005_ZMinus { get => _m3005_ZMinus; set => SetProperty(ref _m3005_ZMinus, value); }
        #endregion

        #region User Variables (Write to PLC)
        private int _setSpeed = 1000;
        public int SetSpeed { get => _setSpeed; set => SetProperty(ref _setSpeed, value); }

        private int _setPosX;
        public int SetPosX { get => _setPosX; set => SetProperty(ref _setPosX, value); }

        private int _setPosY;
        public int SetPosY { get => _setPosY; set => SetProperty(ref _setPosY, value); }
        #endregion

        #region Telemetry Properties
        private string _telemetryReadDeviceTypeStr = "D";
        public string TelemetryReadDeviceTypeStr { get => _telemetryReadDeviceTypeStr; set => SetProperty(ref _telemetryReadDeviceTypeStr, value); }
        private string _telemetryReadAddress = "100";
        public string TelemetryReadAddress { get => _telemetryReadAddress; set => SetProperty(ref _telemetryReadAddress, value); }
        private string _telemetryReadValue = "0";
        public string TelemetryReadValue { get => _telemetryReadValue; set => SetProperty(ref _telemetryReadValue, value); }

        private string _telemetryWriteDeviceTypeStr = "D";
        public string TelemetryWriteDeviceTypeStr { get => _telemetryWriteDeviceTypeStr; set => SetProperty(ref _telemetryWriteDeviceTypeStr, value); }
        private string _telemetryWriteAddress = "100";
        public string TelemetryWriteAddress { get => _telemetryWriteAddress; set => SetProperty(ref _telemetryWriteAddress, value); }
        private string _telemetryWriteValue = "0";
        public string TelemetryWriteValue { get => _telemetryWriteValue; set => SetProperty(ref _telemetryWriteValue, value); }
        #endregion

        #region Commands
        public ICommand ConnectCommand { get; set; }
        public ICommand DisconnectCommand { get; set; }
        public ICommand WriteDataCommand { get; set; }
        public ICommand WriteTelemetryCommand { get; set; }
        #endregion

        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(ConnectPLC);
            DisconnectCommand = new RelayCommand(DisconnectPLC);
            WriteDataCommand = new RelayCommand(WriteDataToPLC);
            WriteTelemetryCommand = new RelayCommand(WriteTelemetryToPLC);
        }

        private void ConnectPLC()
        {
            ePLC = new ePLCControl();
            ePLC.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
            ePLC.Open();
            Status = ePLC.IsConnected;

            if (Status)
            {
                Thread t1 = new Thread(Monitor);
                t1.IsBackground = true;
                t1.Start();
            }
        }

        private void DisconnectPLC()
        {
            Status = false;
            if (ePLC != null && ePLC.IsConnected)
            {
                ePLC.Close();
            }
        }

        private void Monitor()
        {
            while (Status)
            {
                Thread.Sleep(50); // Mức độ cập nhật 50ms là đủ nhanh cho UI
                
                if (ePLC != null) 
                {
                    Status = ePLC.IsConnected;
                }
                
                if (Status)
                {
                    Read();
                }
            }
        }

        private void Read()
        {
            try
            {
                // Read Speed (D1000)
                int[] speedArr = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, "1000", 1);
                if (speedArr != null && speedArr.Length > 0) 
                    CurrentSpeed = speedArr[0];

                // Read POS_X (D2000 - 32 bit)
                int[] posxArr = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, "2000", 2);
                if (posxArr != null && posxArr.Length > 1) 
                    CurrentPosX = posxArr[0] | (posxArr[1] << 16);

                // Read POS_Y (D3000 - 32 bit)
                int[] posyArr = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, "3000", 2);
                if (posyArr != null && posyArr.Length > 1) 
                    CurrentPosY = posyArr[0] | (posyArr[1] << 16);

                // Read M state
                int[] mArr = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.M, "3000", 6);
                if (mArr != null && mArr.Length >= 6)
                {
                    M3000_XPlus = mArr[0] == 1;
                    M3001_XMinus = mArr[1] == 1;
                    M3002_YPlus = mArr[2] == 1;
                    M3003_YMinus = mArr[3] == 1;
                    M3004_ZPlus = mArr[4] == 1;
                    M3005_ZMinus = mArr[5] == 1;
                }

                // Telemetry Continuous Read
                if (!string.IsNullOrEmpty(TelemetryReadAddress))
                {
                    try 
                    {
                        var dt = (ePLCControl.DeviceName)Enum.Parse(typeof(ePLCControl.DeviceName), TelemetryReadDeviceTypeStr);
                        var subCmd = dt == ePLCControl.DeviceName.D || dt == ePLCControl.DeviceName.W ? ePLCControl.SubCommand.Word : ePLCControl.SubCommand.Bit;
                        int[] tResult = ePLC.ReadDeviceBlock(subCmd, dt, TelemetryReadAddress, 1);
                        if (tResult != null && tResult.Length > 0)
                        {
                            TelemetryReadValue = tResult[0].ToString();
                        }
                    } 
                    catch { /* Fallback to not crashing loop on bad address */ }
                }

            }
            catch (Exception)
            {
                // Handle read exception (ignore to keep loop running or mark status disconnected if fatal)
            }
        }

        private void WriteTelemetryToPLC()
        {
            if (!Status || ePLC == null || string.IsNullOrEmpty(TelemetryWriteAddress)) return;
            try 
            {
                var dt = (ePLCControl.DeviceName)Enum.Parse(typeof(ePLCControl.DeviceName), TelemetryWriteDeviceTypeStr);
                var subCmd = dt == ePLCControl.DeviceName.D || dt == ePLCControl.DeviceName.W ? ePLCControl.SubCommand.Word : ePLCControl.SubCommand.Bit;
                if (int.TryParse(TelemetryWriteValue, out int val))
                {
                    ePLC.WriteDeviceBlock(subCmd, dt, TelemetryWriteAddress, new int[] { val });
                }
            } 
            catch { }
        }

        private void WriteDataToPLC()
        {
            if (!Status || ePLC == null) return;
            
            try
            {
                // Write Speed (D1000)
                ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, "1000", new int[] { SetSpeed });
                
                // Write POS_X (D2000)
                int[] posXArr = new int[2];
                posXArr[0] = SetPosX & 0xFFFF;
                posXArr[1] = (SetPosX >> 16) & 0xFFFF;
                ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, "2000", posXArr);

                // Write POS_Y (D3000)
                int[] posYArr = new int[2];
                posYArr[0] = SetPosY & 0xFFFF;
                posYArr[1] = (SetPosY >> 16) & 0xFFFF;
                ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, "3000", posYArr);
            }
            catch (Exception) { }
        }

        // Method used by View (MainWindow) on MouseDown / MouseUp to jog axes
        public void SetMBit(string addressStr, bool state)
        {
            if (Status && ePLC != null && ePLC.IsConnected)
            {
                if (int.TryParse(addressStr, out int address))
                {
                    try
                    {
                        ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.M, address.ToString(), new int[] { state ? 1 : 0 });
                    }
                    catch { }
                }
            }
        }
    }
}
