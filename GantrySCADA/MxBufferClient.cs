using System;
using System.Collections;

namespace NVKProject.PLC
{
    public sealed class MxBufferClient
    {
        private dynamic? _actUtl;

        public int LogicalStationNumber { get; set; }
        public bool IsConnected { get; private set; }

        public void Open()
        {
            if (_actUtl != null && IsConnected)
                return;

            Type? actType = Type.GetTypeFromProgID("ActUtlType64.ActUtlType")
                ?? Type.GetTypeFromProgID("ActUtlType.ActUtlType");
            if (actType == null)
                throw new InvalidOperationException("MX Component ActUtlType COM is not registered (ActUtlType64/ActUtlType).");

            _actUtl = Activator.CreateInstance(actType);
            _actUtl!.ActLogicalStationNumber = LogicalStationNumber;

            int rc = _actUtl!.Open();
            if (rc != 0)
                throw new InvalidOperationException($"MX Open failed: {rc}");

            IsConnected = true;
        }

        public void Close()
        {
            if (_actUtl == null)
                return;

            try
            {
                _actUtl.Close();
            }
            catch
            {
                // Ignore close errors.
            }

            IsConnected = false;
        }

        public int[] ReadWords(string device, int length)
        {
            if (_actUtl == null || !IsConnected)
                throw new InvalidOperationException("MX Component is not connected.");

            short[] data = new short[length];
            int rc;

            if (TryParseUDevicePath(device, out int startIO, out int address))
            {
                rc = _actUtl!.ReadBuffer(startIO, address, length, ref data[0]);
                if (rc != 0)
                    throw new InvalidOperationException($"MX ReadBuffer failed: {rc}");
            }
            else
            {
                rc = _actUtl!.ReadDeviceBlock2(device, length, ref data[0]);
                if (rc != 0)
                    throw new InvalidOperationException($"MX ReadDeviceBlock2 failed: {rc}");
            }

            // Convert signed short to unsigned int (treat as ushort)
            int[] result = new int[length];
            for (int i = 0; i < length; i++)
                result[i] = (int)(ushort)data[i];  // Cast to ushort first to get unsigned value
            return result;
        }

        public void WriteWords(string device, int[] values)
        {
            if (_actUtl == null || !IsConnected)
                throw new InvalidOperationException("MX Component is not connected.");

            short[] shorts = new short[values.Length];
            for (int i = 0; i < values.Length; i++)
                shorts[i] = unchecked((short)values[i]);

            int rc;
            if (TryParseUDevicePath(device, out int startIO, out int address))
            {
                rc = _actUtl!.WriteBuffer(startIO, address, values.Length, ref shorts[0]);
                if (rc != 0)
                    throw new InvalidOperationException($"MX WriteBuffer failed: {rc}");
            }
            else
            {
                rc = _actUtl!.WriteDeviceBlock2(device, values.Length, ref shorts[0]);
                if (rc != 0)
                    throw new InvalidOperationException($"MX WriteDeviceBlock2 failed: {rc}");
            }
        }

        private static bool TryParseUDevicePath(string devicePath, out int uNumber, out int gAddress)
        {
            uNumber = 0; gAddress = 0;
            string s = devicePath.Replace("\\\\", "\\").Trim();
            var m = System.Text.RegularExpressions.Regex.Match(s, @"^U([0-9A-F]+)\\G(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) return false;
            return int.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out uNumber)
                && int.TryParse(m.Groups[2].Value, out gAddress);
        }

        private static int[] ToIntArray(object data, int length)
        {
            if (data is int[] ints)
                return ints;

            if (data is short[] shorts)
            {
                int[] result = new int[shorts.Length];
                for (int i = 0; i < shorts.Length; i++)
                    result[i] = shorts[i];
                return result;
            }

            if (data is Array arr)
            {
                int[] result = new int[arr.Length];
                int i = 0;
                foreach (var item in arr)
                {
                    result[i++] = Convert.ToInt32(item);
                }
                return result;
            }

            if (data is ICollection collection)
            {
                int[] result = new int[collection.Count];
                int idx = 0;
                foreach (var item in collection)
                    result[idx++] = Convert.ToInt32(item);
                return result;
            }

            return new int[length];
        }
    }
}