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


        public int D_R_V = 4000;
        public int D_W_V = 5000;
        public int D_W_P = 3000;

        // New: addresses to read as 32-bit (pairs)
        // Default pairs: D1000+D1001, D1002+D1003, D1004+D1005, D2000+D2001, D2002+D2003, D2004+D2005
        private int[] _arr_R32 = new int[6];

        public int[] arr_W_Position = new int[99];
        private int[] _arr_R_V = new int[99];
        public int[] arr_W_V = new int[99];
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
        #endregion
        #region Commands
        public ICommand ConnectCommand { get; set; }
        public ICommand TestReadCommand { get; set; }
        public ICommand TestWriteCommand { get; set; }
        public ICommand TestBit { get; set; }
        #endregion
    }
    public partial class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(ConnectPLC);
            TestReadCommand = new RelayCommand(new Action(() => { }));
            TestWriteCommand = new RelayCommand(new Action(() => { }));

        }
        private void ConnectPLC()
        {
            ePLC = new ePLCControl();
             ePLC.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
            ePLC.Open();
            Status = ePLC.IsConnected;

            Thread t1 = new Thread(Monitor);
            t1.IsBackground = true;
            t1.Start();
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
                }
            }
        }
        private void Write()
        {

            ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_W_V}", arr_W_V);
            ePLC.WriteDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_W_P}", arr_W_Position);
        }
        private void Read()
        {
            // Read enable flag at configurable address (DReadEnable). If non-zero -> proceed; otherwise skip reading 32-bit values.
            try
            {
                int[] flag = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{DReadEnable}", 1);
                if (flag == null || flag.Length == 0)
                {
                    // fallback: still read default block
                    arr_R_V = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_R_V}", Length);
                    return;
                }

                if (flag[0] != 0)
                {
                    // Read normal block (if still needed)
                    arr_R_V = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_R_V}", Length);

                    // Read blocks for 32-bit values using configurable bases
                    int[] b1 = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D32Base1}", 6);
                    int[] b2 = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D32Base2}", 6);

                    // Build new array so SetProperty detects reference change -> triggers PropertyChanged
                    int[] newR32 = new int[6];

                    if (b1 != null && b1.Length >= 6)
                    {
                        // combine pairs (low word | high word << 16): (0,1), (2,3), (4,5)
                        for (int i = 0; i < 3; i++)
                            newR32[i] = b1[i * 2] | (b1[i * 2 + 1] << 16);
                    }

                    if (b2 != null && b2.Length >= 6)
                    {
                        for (int i = 0; i < 3; i++)
                            newR32[3 + i] = b2[i * 2] | (b2[i * 2 + 1] << 16);
                    }

                    // Assign via property setter -> SetProperty -> OnPropertyChanged -> UI refresh
                    arr_R32 = newR32;
                }
                else
                {
                    // If flag is 0, still update arr_R_V (optional) or skip reading arr_R32
                    arr_R_V = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_R_V}", Length);
                }
            }
            catch (Exception)
            {
                // on exception, try to read fallback block
                try { arr_R_V = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_R_V}", Length); } catch { }
            }
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
                return null;
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
                return null;

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
    }
    public static class PlcBitHelper
    {
        public static int[] BoolArrayToIntArray(bool[] bits)
        {
            if (bits == null) return null;

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
