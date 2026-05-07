using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WPF_Test_PLC20260124
{
    public sealed class ePLCControl
    {
        public enum SubCommand
        {
            Word,
            Bit
        }

        public enum DeviceName
        {
            D,
            M,
            X,
            Y,
            Buffer
        }

        private string _ipAddress = "127.0.0.1";
        private int _port = 5000;
        private bool _isConnected;
        private int _mxLogicalStationNo;
        private int _mxNetworkNo;
        private int _mxStationPlcNo;
        private PLCCommunication? _mx;

        public bool IsConnected => _isConnected;

        public void SetPLCProperties(string ipAddress, int port, int networkNo, int stationPLCNo, int stationNo)
        {
            _ipAddress = ipAddress;
            _port = port;
            _mxLogicalStationNo = stationNo;
            _mxNetworkNo = networkNo;
            _mxStationPlcNo = stationPLCNo;
            if (_mx != null)
            {
                _mx.LogicalStationNumber = _mxLogicalStationNo;
                _mx.IPAddress = _ipAddress;
                _mx.Port = _port;
            }
        }

        public void Open()
        {
            _mx ??= new PLCCommunication(_ipAddress, _port, _mxLogicalStationNo);
            _mx.IPAddress = _ipAddress;
            _mx.Port = _port;
            _mx.LogicalStationNumber = _mxLogicalStationNo;

            bool ok = _mx.Connect();
            _isConnected = ok && _mx.IsConnected;
            if (!_isConnected)
                throw new InvalidOperationException($"MX Component connect failed. LogicalStation={_mxLogicalStationNo}, Network={_mxNetworkNo}, Station={_mxStationPlcNo}, Host={_ipAddress}:{_port}.");
        }

        public void Close()
        {
            try
            {
                _mx?.Disconnect();
            }
            finally
            {
                _isConnected = false;
            }
        }

        public int[] ReadDeviceBlock(SubCommand subCommand, DeviceName deviceName, string address, int length)
        {
            if (_mx == null || !_mx.IsConnected)
                throw new InvalidOperationException("MX Component is not connected");

            string fullAddress = BuildAddress(deviceName, address);

            if (deviceName == DeviceName.Buffer)
            {
                if (!TryParseUDevicePath(fullAddress, out int startIO, out int gAddress))
                    throw new ArgumentException($"Invalid buffer address: {fullAddress}", nameof(address));

                return _mx.ReadBuffer(startIO, gAddress, length);
            }

            // For D/M/X/Y, MX Component APIs differ between device types and driver versions.
            // Delegate to PLCCommunication which already falls back appropriately.
            return _mx.ReadDeviceRange(fullAddress, length);
        }

        public void WriteDeviceBlock(SubCommand subCommand, DeviceName deviceName, string address, int[] values)
        {
            if (_mx == null || !_mx.IsConnected)
                throw new InvalidOperationException("MX Component is not connected");

            string fullAddress = BuildAddress(deviceName, address);

            if (deviceName == DeviceName.Buffer)
            {
                if (!TryParseUDevicePath(fullAddress, out int startIO, out int gAddress))
                    throw new ArgumentException($"Invalid buffer address: {fullAddress}", nameof(address));

                short[] data = new short[values.Length];
                for (int i = 0; i < values.Length; i++)
                    data[i] = unchecked((short)values[i]);

                _mx.WriteBuffer(startIO, gAddress, data);
                return;
            }

            // Delegate to MX Component helper.
            // For bit devices, PLCCommunication will use SetDevice/SetDevice2 as needed.
            for (int i = 0; i < values.Length; i++)
            {
                string elementAddress = values.Length == 1 ? fullAddress : IncrementDeviceAddress(fullAddress, i);
                _mx.WriteDeviceValue(elementAddress, values[i]);
            }
        }

        public int[] WordToBit(int word)
        {
            int[] bits = new int[16];
            for (int i = 0; i < 16; i++)
            {
                bits[i] = ((word >> i) & 1) == 1 ? 1 : 0;
            }

            return bits;
        }

        private static string BuildAddress(DeviceName deviceName, string address)
        {
            if (deviceName == DeviceName.Buffer)
            {
                string raw = string.IsNullOrWhiteSpace(address)
                    ? "U0G0"
                    : address.Trim().ToUpperInvariant();
                raw = raw.Replace("/", string.Empty).Replace("\\", string.Empty);
                if (!raw.Contains("G"))
                    raw += "G";
                if (!raw.Contains("\\G"))
                    raw = raw.Replace("G", "\\G");
                return raw;
            }

            string prefix = deviceName.ToString();
            return string.IsNullOrWhiteSpace(address)
                ? prefix + "0"
                : prefix + address.Trim();
        }

        private static bool TryParseUDevicePath(string devicePath, out int uNumber, out int gAddress)
        {
            uNumber = 0;
            gAddress = 0;
            string s = devicePath.Replace("\\\\", "\\").Trim();
            Match m = Regex.Match(s, @"^U([0-9A-F]+)\\G(\d+)$", RegexOptions.IgnoreCase);
            if (!m.Success) return false;
            return int.TryParse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uNumber)
                && int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out gAddress);
        }

        private static string IncrementDeviceAddress(string device, int offset)
        {
            Match m = Regex.Match(device.Trim(), @"^(?<prefix>[A-Za-z]+)(?<addr>\d+)$", RegexOptions.IgnoreCase);
            if (!m.Success) return device;
            string prefix = m.Groups["prefix"].Value;
            int addr = int.Parse(m.Groups["addr"].Value, CultureInfo.InvariantCulture);
            return prefix + (addr + offset).ToString(CultureInfo.InvariantCulture);
        }
    }
}
