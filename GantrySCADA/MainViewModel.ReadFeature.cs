using CommunityToolkit.Mvvm.ComponentModel;
using NVKProject.PLC;
using System;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
        private void Read()
        {
            bool enableLegacyD32 = false;

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

            newR32[0] = ReadCoordinateSourceValue(PosAddrTypeX, PosAddrIndexX, PosAddrXRead32, newR32[0]);
            newR32[1] = ReadCoordinateSourceValue(PosAddrTypeY, PosAddrIndexY, PosAddrYRead32, newR32[1]);
            newR32[2] = ReadCoordinateSourceValue(PosAddrTypeZ, PosAddrIndexZ, PosAddrZRead32, newR32[2]);

            arr_R32 = newR32;

            ReadBitRegisters();

            if ((DateTime.Now - _lastReadLogTime).TotalSeconds >= 1.0)
            {
                string legacyState = enableLegacyD32 ? "D32-on" : "D32-off";
                AddLog("PC", "success", $"Read D{D_R_V}({Length}) + CoordMap(XYZ) + M/X/Y [{legacyState}] -> OK", "Monitor cycle");
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
            if ((iAddress - D_R_V) >= 0 && (iAddress - D_R_V) < arr_R_V.Length)
            {
                return arr_R_V[iAddress - D_R_V] != 0;
            }

            return false;
        }

        private void WriteDevice(int iAddress, bool value)
        {
            if ((iAddress - D_W_V) >= 0 && (iAddress - D_W_V) < arr_W_V.Length)
            {
                arr_W_V[iAddress - D_W_V] = value ? 1 : 0;
            }
        }
    }
}
