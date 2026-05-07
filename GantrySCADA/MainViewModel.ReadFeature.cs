using CommunityToolkit.Mvvm.ComponentModel;
using NVKProject.PLC;
using System;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
        private void Read()
        {
            var plc = ePLC;
            if (plc == null) return;

            bool enableLegacyD32 = false;

            int[] newR32 = (arr_R32 != null && arr_R32.Length == 6)
                ? (int[])arr_R32.Clone()
                : new int[6];

            try
            {
                int[] flag = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{DReadEnable}", 1);
                enableLegacyD32 = flag != null && flag.Length > 0 && flag[0] != 0;
            }
            catch { }

            try
            {
                int[] newData = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D_R_V}", Length);
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
                    int[] b1 = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D32Base1}", 6);
                    int[] b2 = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, ePLCControl.DeviceName.D, $"{D32Base2}", 6);

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

            Axis1PositionValue = ReadCoordinateSourceValue(Axis1PositionAddrType, Axis1PositionAddrIndex, Axis1PositionRead32, Axis1PositionValue);
            Axis1SpeedValue = ReadCoordinateSourceValue(Axis1SpeedAddrType, Axis1SpeedAddrIndex, Axis1SpeedRead32, Axis1SpeedValue);
            Axis1ErrorValue = ReadCoordinateSourceValue(Axis1ErrorAddrType, Axis1ErrorAddrIndex, false, Axis1ErrorValue);
            Axis1WarningValue = ReadCoordinateSourceValue(Axis1WarningAddrType, Axis1WarningAddrIndex, false, Axis1WarningValue);
            Axis1StatusValue = ReadCoordinateSourceValue(Axis1StatusAddrType, Axis1StatusAddrIndex, false, Axis1StatusValue);
            Axis1StartValue = ReadCoordinateSourceValue(Axis1StartAddrType, Axis1StartAddrIndex, false, Axis1StartValue);

            Axis2PositionValue = ReadCoordinateSourceValue(Axis2PositionAddrType, Axis2PositionAddrIndex, Axis2PositionRead32, Axis2PositionValue);
            Axis2SpeedValue = ReadCoordinateSourceValue(Axis2SpeedAddrType, Axis2SpeedAddrIndex, Axis2SpeedRead32, Axis2SpeedValue);
            Axis2ErrorValue = ReadCoordinateSourceValue(Axis2ErrorAddrType, Axis2ErrorAddrIndex, false, Axis2ErrorValue);
            Axis2WarningValue = ReadCoordinateSourceValue(Axis2WarningAddrType, Axis2WarningAddrIndex, false, Axis2WarningValue);
            Axis2StatusValue = ReadCoordinateSourceValue(Axis2StatusAddrType, Axis2StatusAddrIndex, false, Axis2StatusValue);
            Axis2StartValue = ReadCoordinateSourceValue(Axis2StartAddrType, Axis2StartAddrIndex, false, Axis2StartValue);

            Axis3PositionValue = ReadCoordinateSourceValue(Axis3PositionAddrType, Axis3PositionAddrIndex, Axis3PositionRead32, Axis3PositionValue);
            Axis3SpeedValue = ReadCoordinateSourceValue(Axis3SpeedAddrType, Axis3SpeedAddrIndex, Axis3SpeedRead32, Axis3SpeedValue);
            Axis3ErrorValue = ReadCoordinateSourceValue(Axis3ErrorAddrType, Axis3ErrorAddrIndex, false, Axis3ErrorValue);
            Axis3WarningValue = ReadCoordinateSourceValue(Axis3WarningAddrType, Axis3WarningAddrIndex, false, Axis3WarningValue);
            Axis3StatusValue = ReadCoordinateSourceValue(Axis3StatusAddrType, Axis3StatusAddrIndex, false, Axis3StatusValue);
            Axis3StartValue = ReadCoordinateSourceValue(Axis3StartAddrType, Axis3StartAddrIndex, false, Axis3StartValue);

            Axis4PositionValue = ReadCoordinateSourceValue(Axis4PositionAddrType, Axis4PositionAddrIndex, Axis4PositionRead32, Axis4PositionValue);
            Axis4SpeedValue = ReadCoordinateSourceValue(Axis4SpeedAddrType, Axis4SpeedAddrIndex, Axis4SpeedRead32, Axis4SpeedValue);
            Axis4ErrorValue = ReadCoordinateSourceValue(Axis4ErrorAddrType, Axis4ErrorAddrIndex, false, Axis4ErrorValue);
            Axis4WarningValue = ReadCoordinateSourceValue(Axis4WarningAddrType, Axis4WarningAddrIndex, false, Axis4WarningValue);
            Axis4StatusValue = ReadCoordinateSourceValue(Axis4StatusAddrType, Axis4StatusAddrIndex, false, Axis4StatusValue);
            Axis4StartValue = ReadCoordinateSourceValue(Axis4StartAddrType, Axis4StartAddrIndex, false, Axis4StartValue);

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
            var plc = ePLC;
            if (plc == null) return;

            try
            {
                int[] mData = plc.ReadDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.M, $"{M_R_Base}", 100);
                if (mData != null && mData.Length > 0)
                {
                    int[] newM = new int[_arr_R_M.Length];
                    Array.Copy(mData, newM, Math.Min(mData.Length, newM.Length));
                    arr_R_M = newM;
                }

                int[] xData = plc.ReadDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.X, $"{X_R_Base}", 100);
                if (xData != null && xData.Length > 0)
                {
                    int[] newX = new int[_arr_R_X.Length];
                    Array.Copy(xData, newX, Math.Min(xData.Length, newX.Length));
                    arr_R_X = newX;
                }

                int[] yData = plc.ReadDeviceBlock(ePLCControl.SubCommand.Bit, ePLCControl.DeviceName.Y, $"{Y_R_Base}", 100);
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
