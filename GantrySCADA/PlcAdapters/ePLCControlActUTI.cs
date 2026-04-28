using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DACDT.PlcAdapters
{
    /// <summary>
    /// Adapter wrapper that implements the same interface as the original ePLCControl
    /// but uses Mitsubishi's ActUTI (ActUtility) COM object internally when available.
    ///
    /// This version attempts to use the interop types generated from Mitsubishi's
    /// ActUtlType/ActUtlType64 COM libraries (if referenced in the project). If
    /// those interop types are not available at compile-time the adapter falls
    /// back to late-bound COM activation via ProgID and reflection.
    /// </summary>
    public class ePLCControlActUTI
    {
        // Late-bound or interop COM object for ActUTI (created via ProgID or interop type)
        private object _act;

        // Connection parameters
        private string _ipAddress = string.Empty;
        private int _port = 3000;
        private int _networkNo = 0;
        private int _stationNo = 0;
        private int _stationPLCNo = 255;

        // Mirror original ePLCControl enums
        public enum SubCommand { Bit = 0, Word = 1 }
        public enum DeviceName { D, M, X, Y, G, Q }

        public ePLCControlActUTI()
        {
            // Try to instantiate COM object if registered (non-fatal). Actual connection
            // (Open) will validate availability and throw a clear error if missing.
            _act = TryCreateActUtlInstance();
            IsConnected = false;
        }

        /// <summary>
        /// Attempt to create an ActUTI COM instance using generated interop types first
        /// (if present in the project) and fall back to ProgID-based late-binding.
        /// Returns null if not registered on the system.
        /// </summary>
        private static object TryCreateActUtlInstance()
        {
            // 1) Try to use the interop type if the project has the COMReference and
            //    the generated interop namespace is available at runtime.
            //    Common interop type names (may vary depending on TLB import):
            string[] interopTypeNames = new[]
            {
                "ActUtlTypeLib.ActUtlType",
                "ActUtlTypeLib.ActUtlTypeClass",
                "ActUtlType.ActUtlType",
                "ActUtlType.ActUtlTypeCtrl.1"
            };

            foreach (var typeName in interopTypeNames)
            {
                try
                {
                    var t = Type.GetType(typeName);
                    if (t != null)
                    {
                        return Activator.CreateInstance(t);
                    }
                }
                catch
                {
                    // ignore and try next
                }
            }

            // 2) Fall back to ProgID-based activation (works when ActUtility is registered)
            string[] progIds = new[]
            {
                "ActUtlType.ActUtlType",
                "ActUtlType.ActUtlTypeCtrl.1",
                "ActUtlTypeLib.ActUtlType",
                "Act2.Act2",
                "ActUtilTypeLib.Act2"
            };

            foreach (var pid in progIds)
            {
                try
                {
                    var t = Type.GetTypeFromProgID(pid);
                    if (t != null)
                    {
                        return Activator.CreateInstance(t);
                    }
                }
                catch
                {
                    // ignore and try next
                }
            }

            return null;
        }

        public void SetPLCProperties(string ipAddress, int port, int networkNo, int stationPLCNo, int stationNo)
        {
            _ipAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            _port = port;
            _networkNo = networkNo;
            _stationPLCNo = stationPLCNo;
            _stationNo = stationNo;
        }

        public void Open()
        {
            if (IsConnected)
                throw new InvalidOperationException("PLC connection is already open");

            if (_act == null)
            {
                // Try once more to create the COM instance before failing
                _act = TryCreateActUtlInstance();
                if (_act == null)
                    throw new InvalidOperationException("ActUTI COM object not available. Ensure Mitsubishi Act Utility (ActUTI) is installed and registered on this machine.");
            }

            try
            {
                Type t = _act.GetType();

                // Prefer strongly-typed property names used by ActUtl interop
                // but use reflection so both late-bound and interop-backed objects work.
                t.InvokeMember("ActLogicalStationNumber", BindingFlags.SetProperty, null, _act, new object[] { _stationNo });
                t.InvokeMember("ActEthernetPortNumber", BindingFlags.SetProperty, null, _act, new object[] { _port });
                t.InvokeMember("ActEthernetHostName", BindingFlags.SetProperty, null, _act, new object[] { _ipAddress });

                if (_networkNo > 0)
                {
                    try
                    {
                        t.InvokeMember("ActNetworkNumber", BindingFlags.SetProperty, null, _act, new object[] { _networkNo });
                    }
                    catch { }
                }

                // Call Open()
                t.InvokeMember("Open", BindingFlags.InvokeMethod, null, _act, null);

                IsConnected = true;
            }
            catch (TargetInvocationException tie) when (tie.InnerException is COMException cex)
            {
                IsConnected = false;
                throw new InvalidOperationException($"ActUTI Open() failed for {_ipAddress}:{_port} - Error 0x{cex.ErrorCode:X}: {cex.Message}. Check IP, port, firewall, and PLC RUN mode.", cex);
            }
            catch (COMException ex)
            {
                IsConnected = false;
                throw new InvalidOperationException($"ActUTI Open() failed for {_ipAddress}:{_port} - Error 0x{ex.ErrorCode:X}: {ex.Message}. Check IP, port, firewall, and PLC RUN mode.", ex);
            }
            catch (Exception ex)
            {
                IsConnected = false;
                throw new InvalidOperationException($"Failed to open ActUTI connection to {_ipAddress}:{_port}: {ex.Message}", ex);
            }
        }

        public void Close()
        {
            if (!IsConnected)
                return;

            if (_act == null)
            {
                IsConnected = false;
                return;
            }

            try
            {
                Type t = _act.GetType();
                try { t.InvokeMember("Close", BindingFlags.InvokeMethod, null, _act, null); } catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: ActUTI Close() error: {ex.Message}");
            }
            finally
            {
                IsConnected = false;
            }
        }

        public bool IsConnected { get; private set; }

        public int[] ReadDeviceBlock(SubCommand subCommand, DeviceName deviceName, string startAddress, int length)
        {
            if (!IsConnected)
                throw new InvalidOperationException("PLC not connected. Call Open() first.");

            if (string.IsNullOrWhiteSpace(startAddress))
                throw new ArgumentException("Start address cannot be null or empty", nameof(startAddress));

            if (length <= 0)
                throw new ArgumentException("Length must be > 0", nameof(length));

            if (_act == null)
                throw new InvalidOperationException("ActUTI COM object not available.");

            try
            {
                string deviceString = GetActUTIDeviceString(deviceName, subCommand);
                string fullAddress = $"{deviceString}{startAddress}";

                Type t = _act.GetType();
                object[] args = new object[] { fullAddress, length, null };
                t.InvokeMember("ReadDevice", BindingFlags.InvokeMethod, null, _act, args);

                object readBuffer = args[2];
                return ConvertToIntArray(readBuffer, length);
            }
            catch (TargetInvocationException tie) when (tie.InnerException is COMException cex)
            {
                throw new InvalidOperationException($"ActUTI ReadDevice() failed: {GetDeviceName(deviceName)}{startAddress}, length={length}, Error=0x{cex.ErrorCode:X}: {cex.Message}", cex);
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"ActUTI ReadDevice() failed: {GetDeviceName(deviceName)}{startAddress}, length={length}, Error=0x{ex.ErrorCode:X}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Read failed for {GetDeviceName(deviceName)}{startAddress} ({length} items): {ex.Message}", ex);
            }
        }

        public void WriteDeviceBlock(SubCommand subCommand, DeviceName deviceName, string startAddress, int[] values)
        {
            if (!IsConnected)
                throw new InvalidOperationException("PLC not connected. Call Open() first.");

            if (string.IsNullOrWhiteSpace(startAddress))
                throw new ArgumentException("Start address cannot be null or empty", nameof(startAddress));

            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty", nameof(values));

            if (_act == null)
                throw new InvalidOperationException("ActUTI COM object not available.");

            try
            {
                string deviceString = GetActUTIDeviceString(deviceName, subCommand);
                string fullAddress = $"{deviceString}{startAddress}";

                Type t = _act.GetType();
                t.InvokeMember("WriteDevice", BindingFlags.InvokeMethod, null, _act, new object[] { fullAddress, values.Length, values });
            }
            catch (TargetInvocationException tie) when (tie.InnerException is COMException cex)
            {
                throw new InvalidOperationException($"ActUTI WriteDevice() failed: {GetDeviceName(deviceName)}{startAddress}, values={values.Length}, Error=0x{cex.ErrorCode:X}: {cex.Message}", cex);
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException($"ActUTI WriteDevice() failed: {GetDeviceName(deviceName)}{startAddress}, values={values.Length}, Error=0x{ex.ErrorCode:X}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Write failed for {GetDeviceName(deviceName)}{startAddress} ({values.Length} items): {ex.Message}", ex);
            }
        }

        private int[] ConvertToIntArray(object readBuffer, int expectedLength)
        {
            if (readBuffer == null)
                return new int[expectedLength];

            switch (readBuffer)
            {
                case int[] intArray:
                    return intArray;

                case short[] shortArray:
                    return Array.ConvertAll(shortArray, x => (int)x);

                case long[] longArray:
                    return Array.ConvertAll(longArray, x => (int)x);

                case object[] objArray:
                    return Array.ConvertAll(objArray, x =>
                    {
                        try { return Convert.ToInt32(x); }
                        catch { return 0; }
                    });

                case byte[] byteArray:
                    return Array.ConvertAll(byteArray, x => (int)x);

                default:
                    try { return new[] { Convert.ToInt32(readBuffer) }; }
                    catch { return new int[expectedLength]; }
            }
        }

        private string GetActUTIDeviceString(DeviceName deviceName, SubCommand subCommand)
        {
            return deviceName switch
            {
                DeviceName.D => "D",
                DeviceName.M => "M",
                DeviceName.X => "X",
                DeviceName.Y => "Y",
                DeviceName.G => "G",
                DeviceName.Q => "Q",
                _ => throw new ArgumentException($"Unknown device: {deviceName}")
            };
        }

        private string GetDeviceName(DeviceName deviceName) => deviceName.ToString();

        public bool[] WordToBit(int word)
        {
            bool[] bits = new bool[16];
            for (int i = 0; i < 16; i++)
                bits[i] = ((word >> i) & 1) == 1;
            return bits;
        }

        public object UnderlyingAct => _act;

        ~ePLCControlActUTI() { Close(); }
    }
}
