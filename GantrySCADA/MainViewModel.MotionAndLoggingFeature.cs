using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
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
                return Array.Empty<int>();
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
            return word & ~(1 << bit);
        }

        private int[] GetDataValue_(int[] arr, int index)
        {
            if (arr == null || index < 0 || index >= arr.Length)
                return Array.Empty<int>();

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
}
