# Migration Guide: Custom PLC Library ? Mitsubishi ActUTI

## Overview
This guide explains how to migrate from your custom `ePLCControl` (NVKProject.PLC) library to Mitsubishi's official **ActUTI (ActUtility Type Library)** package.

---

## Step 1: Install Mitsubishi ActUTI NuGet Package

Add the ActUTI library to your project:

```bash
dotnet add package MITSUBISHI.ActUTIDataLogging64.Control
dotnet add package MITSUBISHI.ActUTIType.Controls
```

Or via Package Manager:
1. Right-click project ? **Manage NuGet Packages**
2. Search for: `MITSUBISHI.ActUTIDataLogging64 Control`
3. Click **Install**

---

## Step 2: Update ePLCControl Wrapper

Replace the custom `ePLCControl` class with a **wrapper** that uses ActUTI internally. This maintains compatibility with existing code.

### Old Code (Custom Library)
```csharp
ePLC = new ePLCControl();
ePLC.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
ePLC.Open();
Status = ePLC.IsConnected;

int[] data = ePLC.ReadDeviceBlock(SubCommand.Word, DeviceName.D, "4000", 99);
ePLC.WriteDeviceBlock(SubCommand.Word, DeviceName.D, "5000", arr_W_V);
```

### New Code (ActUTI Wrapper)
```csharp
ePLC = new ePLCControlActUTI();  // ? New wrapper using ActUTI
ePLC.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
ePLC.Open();
Status = ePLC.IsConnected;

// Same API - no changes needed to MainViewModel!
int[] data = ePLC.ReadDeviceBlock(SubCommand.Word, DeviceName.D, "4000", 99);
ePLC.WriteDeviceBlock(SubCommand.Word, DeviceName.D, "5000", arr_W_V);
```

---

## Step 3: Create ActUTI Adapter Wrapper

This wrapper class maintains the same interface but uses ActUTI internally.

**File**: `PlcAdapters/ePLCControlActUTI.cs`

```csharp
using System;
using System.Collections.Generic;
using ActUtilTypeLib;  // Mitsubishi ActUTI namespace

namespace WPF_Test_PLC20260124
{
    /// <summary>
    /// Adapter wrapper that implements the same interface as ePLCControl
    /// but uses Mitsubishi's ActUTI (ActUtility) library internally.
    /// </summary>
    public class ePLCControlActUTI
    {
        private Act2 _act;
        private string _ipAddress = "";
        private int _port = 3000;
        private int _networkNo = 0;
        private int _stationNo = 0;
        private int _stationPLCNo = 255;

        // SubCommand enums - mirror from original
        public enum SubCommand { Bit, Word }

        // DeviceName enums - mirror from original
        public enum DeviceName { D, M, X, Y, G, Q }

        public ePLCControlActUTI()
        {
            // Create ActUTI Act2 instance
            _act = new Act2();
        }

        public void SetPLCProperties(string ipAddress, int port, int networkNo, int stationPLCNo, int stationNo)
        {
            _ipAddress = ipAddress;
            _port = port;
            _networkNo = networkNo;
            _stationPLCNo = stationPLCNo;
            _stationNo = stationNo;
        }

        public void Open()
        {
            try
            {
                // Set communication parameters
                _act.ActLogicalStationNumber = _stationNo;
                _act.ActEthernetPortNumber = _port;
                _act.ActEthernetHostName = _ipAddress;

                // Open TCP/IP connection
                _act.Open();
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                throw new InvalidOperationException($"Failed to open ActUTI connection: {ex.Message}", ex);
            }
        }

        public void Close()
        {
            try
            {
                if (_act != null && IsConnected)
                {
                    _act.Close();
                    IsConnected = false;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to close ActUTI connection: {ex.Message}", ex);
            }
        }

        public bool IsConnected { get; private set; }

        /// <summary>
        /// Read device block - mirrors ePLCControl.ReadDeviceBlock
        /// </summary>
        public int[] ReadDeviceBlock(SubCommand subCommand, DeviceName deviceName, string startAddress, int length)
        {
            if (!IsConnected)
                throw new InvalidOperationException("PLC not connected");

            try
            {
                string deviceString = GetDeviceString(deviceName, subCommand);
                object readValue = null;
                
                // Use ActUTI to read
                _act.ReadDevice($"{deviceString}{startAddress}", length, out readValue);

                // Convert to int[]
                if (readValue is int[] intArray)
                    return intArray;
                else if (readValue is short[] shortArray)
                    return Array.ConvertAll(shortArray, x => (int)x);
                else if (readValue is object[] objArray)
                    return Array.ConvertAll(objArray, x => Convert.ToInt32(x));
                else
                    throw new InvalidOperationException("Unexpected read value type");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to read {GetDeviceString(deviceName, subCommand)}{startAddress}, length={length}: {ex.Message}", 
                    ex);
            }
        }

        /// <summary>
        /// Write device block - mirrors ePLCControl.WriteDeviceBlock
        /// </summary>
        public void WriteDeviceBlock(SubCommand subCommand, DeviceName deviceName, string startAddress, int[] values)
        {
            if (!IsConnected)
                throw new InvalidOperationException("PLC not connected");

            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty");

            try
            {
                string deviceString = GetDeviceString(deviceName, subCommand);
                
                // Use ActUTI to write
                _act.WriteDevice($"{deviceString}{startAddress}", values.Length, values);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to write {GetDeviceString(deviceName, subCommand)}{startAddress}, values={values.Length}: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Helper: Convert DeviceName enum to ActUTI device string
        /// </summary>
        private string GetDeviceString(DeviceName deviceName, SubCommand subCommand)
        {
            return deviceName switch
            {
                DeviceName.D => subCommand == SubCommand.Bit ? "M" : "D",  // D for word, M for bit
                DeviceName.M => "M",
                DeviceName.X => "X",
                DeviceName.Y => "Y",
                DeviceName.G => "G",
                DeviceName.Q => "Q",
                _ => throw new ArgumentException($"Unknown device: {deviceName}")
            };
        }

        /// <summary>
        /// Helper: Convert bit string to word (for compatibility with original library)
        /// </summary>
        public bool[] WordToBit(int word)
        {
            bool[] bits = new bool[16];
            for (int i = 0; i < 16; i++)
                bits[i] = ((word >> i) & 1) == 1;
            return bits;
        }

        ~ePLCControlActUTI()
        {
            Close();
        }
    }
}
```

---

## Step 4: Update MainViewModel.cs

Simply change the instantiation:

```csharp
// OLD
private ePLCControl ePLC;  // NVKProject.PLC

// NEW
private ePLCControlActUTI ePLC;  // Mitsubishi ActUTI wrapper

// In ConnectPLC():
private void ConnectPLC()
{
    ePLC = new ePLCControlActUTI();  // ? Changed line
    ePLC.SetPLCProperties(IpAddress, Port, NetworkNo, StationPLCNo, StationNo);
    ePLC.Open();
    Status = ePLC.IsConnected;
    
    // Rest stays the same...
}
```

---

## Step 5: Test the Migration

1. **Build the project** - should compile without errors
2. **Connect to PLC** - click "Connect System" in Dashboard
3. **Test Read** - verify position cards show values
4. **Test Write** - adjust velocity slider and confirm data written
5. **Test Jog** - move axes and verify in LogMonitor

---

## Key Differences: Custom Library vs. ActUTI

| Feature | Custom Library | ActUTI |
|---------|---|---|
| **Connection** | TCP/IP direct | ActUTI manages socket |
| **Device Read** | ReadDeviceBlock() | ReadDevice() |
| **Device Write** | WriteDeviceBlock() | WriteDevice() |
| **Error Handling** | Custom exceptions | ActUTI exceptions |
| **Performance** | Custom optimization | Mitsubishi optimized |
| **Support** | Community | Official Mitsubishi |
| **32-bit Handling** | Manual (2 words) | Native support |

---

## Troubleshooting

### "ActUTI not found" error
- Install NuGet package: `MITSUBISHI.ActUTIDataLogging64.Control`
- Ensure .NET Framework is 4.5+ or .NET 5+

### Connection timeout
- Check IP address and port are correct
- Verify PLC network cable is connected
- Ensure PLC is in RUN mode
- Check Windows Firewall allows TCP port 3000

### Read/Write failures after migration
- ActUTI device names are slightly different (see GetDeviceString mapping)
- Verify addresses are valid (D4000, M0, etc.)
- Check PLC memory regions are accessible

---

## Advanced: Custom ActUTI Integration (Optional)

If you need more direct ActUTI features, you can access the underlying Act2 object:

```csharp
public class ePLCControlActUTI
{
    public Act2 UnderlyingAct => _act;  // Access raw ActUTI
    
    // Use raw ActUTI for advanced operations
    public void AdvancedOperation()
    {
        _act.GetDeviceBlock("D4000", 99, out object data);  // Direct ActUTI call
    }
}
```

---

## Migration Checklist

- [ ] Install ActUTI NuGet package
- [ ] Create `ePLCControlActUTI.cs` wrapper class
- [ ] Update `MainViewModel.cs` to use new wrapper
- [ ] Build project - verify no compilation errors
- [ ] Test PLC connection
- [ ] Test data read (position cards)
- [ ] Test data write (velocity slider)
- [ ] Test jog controls
- [ ] Remove old `NVKProject.PLC` package references
- [ ] Update project documentation

---

## References

- **Mitsubishi ActUTI Documentation**: [Official Docs](https://www.mitsubishielectric.com)
- **ActUTI API Reference**: ActUtilTypeLib namespace documentation
- **Your Application**: Telemetry.razor, Dashboard.razor use this library

---

**Migration Status**: Ready for implementation  
**Estimated Time**: 30 minutes  
**Risk Level**: Low (wrapper maintains compatibility)

