using System;
using DACDT.PlcAdapters;

namespace DACDT.PLC
{
    // Compatibility shim that exposes the original NVKProject.PLC.ePLCControl API
    // and delegates to the internal ActUTI adapter implemented in the project.
    public class ePLCControl
    {
        private readonly ePLCControlActUTI _inner;

        public ePLCControl()
        {
            _inner = new ePLCControlActUTI();
        }

        // Keep the same enums used throughout the codebase
        public enum SubCommand { Bit = 0, Word = 1 }
        public enum DeviceName { D = 0, M = 1, X = 2, Y = 3, G = 4, Q = 5 }

        public void SetPLCProperties(string ipAddress, int port, int networkNo, int stationPLCNo, int stationNo)
        {
            _inner.SetPLCProperties(ipAddress, port, networkNo, stationPLCNo, stationNo);
        }

        public void Open() => _inner.Open();

        public void Close() => _inner.Close();

        public bool IsConnected => _inner.IsConnected;

        public int[] ReadDeviceBlock(SubCommand subCommand, DeviceName deviceName, string startAddress, int length)
        {
            return _inner.ReadDeviceBlock(
                (ePLCControlActUTI.SubCommand)(int)subCommand,
                (ePLCControlActUTI.DeviceName)(int)deviceName,
                startAddress, length);
        }

        public void WriteDeviceBlock(SubCommand subCommand, DeviceName deviceName, string startAddress, int[] values)
        {
            _inner.WriteDeviceBlock(
                (ePLCControlActUTI.SubCommand)(int)subCommand,
                (ePLCControlActUTI.DeviceName)(int)deviceName,
                startAddress, values);
        }

        public bool[] WordToBit(int word) => _inner.WordToBit(word);

        // Expose underlying object for advanced scenarios (keeps parity with adapter)
        public object UnderlyingAct => _inner.UnderlyingAct;
    }
}
