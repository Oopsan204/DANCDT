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
            ePLC = new ePLCControl();
            ePLC.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
            ePLC.Open();
            Status = ePLC.IsConnected;

            AddLog("PLC", "info", $"Connection attempt -> {IpAddress}:{Port}");
            AddLog("PLC", Status ? "success" : "error",
                Status ? "PLC connection established" : "PLC connection failed");

            Thread t1 = new Thread(Monitor)
            {
                IsBackground = true
            };
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
                    Write();
                    RefreshCustomMemory();
                }
            }
        }
    }
}