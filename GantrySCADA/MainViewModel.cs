using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NVKProject.PLC;
using System;
using System.Threading;
using System.Windows.Input;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
        #region Commands
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand TestReadCommand { get; }
        public ICommand TestWriteCommand { get; }
        public ICommand TestBit { get; }
        #endregion

        public MainViewModel()
        {
            ePLC = new ePLCControl();
            ConnectCommand = new RelayCommand(ConnectPLC);
            DisconnectCommand = new RelayCommand(DisconnectPLC);
            TestReadCommand = new RelayCommand(new Action(() => { }));
            TestWriteCommand = new RelayCommand(new Action(() => { }));
            TestBit = new RelayCommand(new Action(() => { }));

            AddLog("UI", "info", "Application started");
            AddLog("PC", "info", "MainViewModel initialized");
            AddLog("PC", "info", "Monitor thread started @ 100Hz");

            InitializeDxfFeature();
        }

        private void ConnectPLC()
        {
            AddLog("PLC", "info", $"Connection attempt -> {IpAddress}:{Port}");

            try
            {
                lock (_plcSync)
                {
                    _monitorStopRequested = false;

                    try
                    {
                        ePLC?.Close();
                    }
                    catch
                    {
                        // Ignore close errors from previous stale connection instance.
                    }

                    var newPlc = new ePLCControl();
                    newPlc.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
                    newPlc.Open();
                    ePLC = newPlc;
                    Status = ePLC.IsConnected;
                }

                AddLog("PLC", Status ? "success" : "error",
                    Status ? "PLC connection established" : "PLC connection failed");

                if (Status)
                {
                    StartMonitorThread();
                }
            }
            catch (Exception ex)
            {
                Status = false;
                AddLog("PLC", "error", $"PLC connection failed: {ex.Message}", "ConnectPLC");
            }
        }

        private void DisconnectPLC()
        {
            _monitorStopRequested = true;
            Status = false;
            AddLog("PLC", "warning", "Connection closed by user");

            lock (_plcSync)
            {
                try
                {
                    ePLC?.Close();
                }
                catch
                {
                    // Ignore close errors during manual disconnect.
                }
            }
        }

        private void StartMonitorThread()
        {
            lock (_plcSync)
            {
                if (_monitorThread != null && _monitorThread.IsAlive)
                    return;

                _monitorThread = new Thread(Monitor)
                {
                    IsBackground = true,
                    Name = "PLC-Monitor"
                };

                _monitorThread.Start();
            }
        }

        private void TryReconnectIfDue()
        {
            if (_monitorStopRequested)
                return;

            DateTime now = DateTime.Now;
            if ((now - _lastReconnectAttempt) < _reconnectInterval)
                return;

            _lastReconnectAttempt = now;
            AddLog("PLC", "warning", $"Reconnect attempt -> {IpAddress}:{Port}", "AutoReconnect 5s");

            try
            {
                lock (_plcSync)
                {
                    try
                    {
                        ePLC?.Close();
                    }
                    catch
                    {
                        // Ignore stale transport close failures.
                    }

                    var newPlc = new ePLCControl();
                    newPlc.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
                    newPlc.Open();
                    ePLC = newPlc;
                    Status = ePLC.IsConnected;
                }

                if (Status)
                {
                    AddLog("PLC", "success", "PLC reconnected", "AutoReconnect 5s");
                }
            }
            catch (Exception ex)
            {
                Status = false;
                AddLog("PLC", "warning", $"Reconnect failed: {ex.Message}", "AutoReconnect 5s");
            }
        }

        private bool IsConnectedSafe()
        {
            lock (_plcSync)
            {
                try
                {
                    return ePLC != null && ePLC.IsConnected;
                }
                catch
                {
                    return false;
                }
            }
        }

        private void Monitor()
        {
            if (_monitorRunning)
                return;

            _monitorRunning = true;
            bool lostLogged = false;

            try
            {
                while (!_monitorStopRequested)
                {
                    Thread.Sleep(10);

                    bool connected = IsConnectedSafe();
                    Status = connected;

                    if (!connected)
                    {
                        if (!lostLogged && !_monitorStopRequested)
                        {
                            AddLog("PLC", "warning", "PLC connection lost unexpectedly");
                            lostLogged = true;
                        }

                        TryReconnectIfDue();

                        continue;
                    }

                    lostLogged = false;

                    try
                    {
                        Read();
                        Write();
                        RefreshCustomMemory();
                    }
                    catch (Exception ex)
                    {
                        Status = false;
                        AddLog("PC", "error", $"Monitor cycle error: {ex.Message}", ex.GetType().Name);
                    }
                }
            }
            finally
            {
                _monitorRunning = false;
            }
        }
    }
}