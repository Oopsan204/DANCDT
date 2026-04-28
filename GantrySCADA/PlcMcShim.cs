using System;
using HslCommunication;
using HslCommunication.Profinet.Melsec;

namespace NVKProject.PLC
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
            Y
        }

        private string _ipAddress = "127.0.0.1";
        private int _port = 5000;
        private MelsecMcNet? _client;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public void SetPLCProperties(string ipAddress, int port, int networkNo, int stationPLCNo, int stationNo)
        {
            _ipAddress = ipAddress;
            _port = port;
            if (_client != null)
            {
                _client.IpAddress = _ipAddress;
                _client.Port = _port;
            }
        }

        public void Open()
        {
            _client ??= new MelsecMcNet(_ipAddress, _port);
            _client.IpAddress = _ipAddress;
            _client.Port = _port;

            OperateResult connectResult = _client.ConnectServer();
            if (!connectResult.IsSuccess)
            {
                _isConnected = false;
                throw new InvalidOperationException($"MC Connect failed: {connectResult.Message}");
            }

            _isConnected = true;
        }

        public void Close()
        {
            _client?.ConnectClose();
            _isConnected = false;
        }

        public int[] ReadDeviceBlock(SubCommand subCommand, DeviceName deviceName, string address, int length)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("MC client is not initialized");
            }

            string fullAddress = BuildAddress(deviceName, address);

            if (subCommand == SubCommand.Bit)
            {
                OperateResult<bool[]> result = _client.ReadBool(fullAddress, (ushort)length);
                if (!result.IsSuccess || result.Content == null)
                {
                    throw new InvalidOperationException($"MC ReadBit failed: {result.Message}");
                }

                bool[] bits = result.Content;
                int[] values = new int[bits.Length];
                for (int i = 0; i < bits.Length; i++)
                {
                    values[i] = bits[i] ? 1 : 0;
                }

                return values;
            }

            OperateResult<short[]> wordResult = _client.ReadInt16(fullAddress, (ushort)length);
            if (!wordResult.IsSuccess || wordResult.Content == null)
            {
                throw new InvalidOperationException($"MC ReadWord failed: {wordResult.Message}");
            }

            short[] words = wordResult.Content;
            int[] data = new int[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                data[i] = words[i];
            }

            return data;
        }

        public void WriteDeviceBlock(SubCommand subCommand, DeviceName deviceName, string address, int[] values)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("MC client is not initialized");
            }

            string fullAddress = BuildAddress(deviceName, address);

            if (subCommand == SubCommand.Bit)
            {
                bool[] bits = new bool[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    bits[i] = values[i] != 0;
                }

                OperateResult writeBitResult = _client.Write(fullAddress, bits);
                if (!writeBitResult.IsSuccess)
                {
                    throw new InvalidOperationException($"MC WriteBit failed: {writeBitResult.Message}");
                }

                return;
            }

            short[] words = new short[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                words[i] = unchecked((short)values[i]);
            }

            OperateResult writeWordResult = _client.Write(fullAddress, words);
            if (!writeWordResult.IsSuccess)
            {
                throw new InvalidOperationException($"MC WriteWord failed: {writeWordResult.Message}");
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
            string prefix = deviceName.ToString();
            return string.IsNullOrWhiteSpace(address)
                ? prefix + "0"
                : prefix + address.Trim();
        }
    }
}
