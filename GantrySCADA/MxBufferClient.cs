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

            Type? actType = Type.GetTypeFromProgID("ActUtlType.ActUtlType");
            if (actType == null)
                throw new InvalidOperationException("MX Component ActUtlType COM is not registered.");

            _actUtl = Activator.CreateInstance(actType);
            _actUtl.ActLogicalStationNumber = LogicalStationNumber;

            int rc = _actUtl.Open();
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

            object data;
            int rc = _actUtl.ReadDeviceBlock2(device, length, out data);
            if (rc != 0)
                throw new InvalidOperationException($"MX ReadDeviceBlock2 failed: {rc}");

            return ToIntArray(data, length);
        }

        public void WriteWords(string device, int[] values)
        {
            if (_actUtl == null || !IsConnected)
                throw new InvalidOperationException("MX Component is not connected.");

            object data = values;
            int rc = _actUtl.WriteDeviceBlock2(device, values.Length, ref data);
            if (rc != 0)
                throw new InvalidOperationException($"MX WriteDeviceBlock2 failed: {rc}");
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

            if (data is Array array)
            {
                int[] result = new int[array.Length];
                for (int i = 0; i < array.Length; i++)
                    result[i] = Convert.ToInt32(array.GetValue(i));
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