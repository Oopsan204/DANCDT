using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
        private bool TryReadValueCore(string addrType, int addrIndex, out int value, string? addrIndexText = null, bool read32 = false, bool addrIndexIsHex = false)
        {
            value = 0;

            var plc = ePLC;
            if (!Status || plc == null)
                return false;

            string normalizedType = NormalizeAddrType(addrType);

            if (IsBufferType(normalizedType))
            {
                try
                {
                    if (read32)
                    {
                        int[] words32 = plc.ReadDeviceBlock(
                            ePLCControl.SubCommand.Word,
                            ePLCControl.DeviceName.Buffer,
                            BuildBufferAddress(normalizedType, addrIndex, addrIndexText, addrIndexIsHex),
                            2);
                        if (words32 != null && words32.Length >= 2)
                        {
                            value = words32[0] | (words32[1] << 16);
                            return true;
                        }
                    }
                    else
                    {
                        int[] word = plc.ReadDeviceBlock(
                            ePLCControl.SubCommand.Word,
                            ePLCControl.DeviceName.Buffer,
                            BuildBufferAddress(normalizedType, addrIndex, addrIndexText, addrIndexIsHex),
                            1);
                        if (word != null && word.Length > 0)
                        {
                            value = word[0];
                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    AddLog("PC", "error", $"Read Buffer error {normalizedType}{addrIndexText}: {ex.Message}");
                    return false;
                }
            }

            ePLCControl.DeviceName devName = normalizedType switch
            {
                "M" => ePLCControl.DeviceName.M,
                "X" => ePLCControl.DeviceName.X,
                "Y" => ePLCControl.DeviceName.Y,
                _ => ePLCControl.DeviceName.D
            };

            bool isBitDevice = normalizedType == "M" || normalizedType == "X" || normalizedType == "Y";
            int bitOffset = Math.Abs(addrIndex) & 0x0F;

            try
            {
                if (isBitDevice)
                {
                    // Some PLC/driver combinations expose bit devices more reliably via BIT reads,
                    // while others may return data only through WORD reads. Try BIT first.
                    int[] bit = plc.ReadDeviceBlock(ePLCControl.SubCommand.Bit, devName, $"{addrIndex}", 1);
                    if (bit != null && bit.Length > 0)
                    {
                        int raw = bit[0];
                        if (raw == 0 || raw == 1)
                        {
                            value = raw;
                        }
                        else
                        {
                            // Packed form: raw contains 16 bits; extract the requested address bit.
                            value = ((raw >> bitOffset) & 1) != 0 ? 1 : 0;
                        }

                        return true;
                    }

                    int[] wordFallback = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, devName, $"{addrIndex}", 1);
                    if (wordFallback != null && wordFallback.Length > 0)
                    {
                        int raw = wordFallback[0];
                        if (raw == 0 || raw == 1)
                        {
                            value = raw;
                        }
                        else
                        {
                            value = ((raw >> bitOffset) & 1) != 0 ? 1 : 0;
                        }

                        return true;
                    }

                    return false;
                }

                int[] word = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, devName, $"{addrIndex}", 1);
                if (word != null && word.Length > 0)
                {
                    value = word[0];
                    return true;
                }

                return false;
            }
            catch
            {
                if (isBitDevice)
                {
                    try
                    {
                        int[] wordFallback = plc.ReadDeviceBlock(ePLCControl.SubCommand.Word, devName, $"{addrIndex}", 1);
                        if (wordFallback != null && wordFallback.Length > 0)
                        {
                            int raw = wordFallback[0];
                            if (raw == 0 || raw == 1)
                            {
                                value = raw;
                            }
                            else
                            {
                                value = ((raw >> bitOffset) & 1) != 0 ? 1 : 0;
                            }

                            return true;
                        }
                    }
                    catch { }
                }

                return false;
            }
        }

        public bool TryReadSingleValue(string addrType, int addrIndex, out int value)
        {
            value = 0;

            try
            {
                return TryReadValueCore(addrType, addrIndex, out value);
            }
            catch
            {
                return false;
            }
        }

        public void AddCustomMemoryEntry(string addrType, int addrIndex)
        {
            try
            {
                string normalizedType = NormalizeAddrType(addrType);

                bool exists = CustomMemoryEntries.Exists(x =>
                    x.AddrType.Equals(normalizedType, StringComparison.OrdinalIgnoreCase)
                    && x.AddrIndex == addrIndex
                    && string.Equals(x.AddrIndexText ?? string.Empty, string.Empty, StringComparison.OrdinalIgnoreCase));

                if (exists)
                    return;

                var entry = new CustomMemoryEntry { AddrType = normalizedType, AddrIndex = addrIndex, AddrIndexText = string.Empty, DataType = PlcDataType.Int16, AddrIndexIsHex = false };
                CustomMemoryEntries.Add(entry);
                AddLog("UI", "info", $"Added custom memory: {normalizedType}{addrIndex}");
                OnPropertyChanged(nameof(CustomMemoryEntries));
            }
            catch (Exception ex)
            {
                AddLog("PC", "error", $"Failed to add entry: {ex.Message}");
            }
        }

        public void AddCustomMemoryEntry(string addrType, int addrIndex, string? addrIndexText, PlcDataType dataType = PlcDataType.Int16, bool addrIndexIsHex = false)
        {
            try
            {
                string normalizedType = NormalizeAddrType(addrType);
                string normalizedText = addrIndexText?.Trim().ToUpperInvariant() ?? string.Empty;

                bool exists = CustomMemoryEntries.Exists(x =>
                    x.AddrType.Equals(normalizedType, StringComparison.OrdinalIgnoreCase)
                    && x.AddrIndex == addrIndex
                    && string.Equals(x.AddrIndexText ?? string.Empty, normalizedText, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    // Cập nhật DataType nếu entry đã tồn tại
                    var existingEntry = CustomMemoryEntries.First(x =>
                        x.AddrType.Equals(normalizedType, StringComparison.OrdinalIgnoreCase)
                        && x.AddrIndex == addrIndex
                        && string.Equals(x.AddrIndexText ?? string.Empty, normalizedText, StringComparison.OrdinalIgnoreCase));
                    existingEntry.DataType = dataType;
                    return;
                }

                var entry = new CustomMemoryEntry
                {
                    AddrType = normalizedType,
                    AddrIndex = addrIndex,
                    AddrIndexText = normalizedText,
                    DataType = dataType,
                    AddrIndexIsHex = addrIndexIsHex
                };

                CustomMemoryEntries.Add(entry);
                string addrLabel = IsBufferType(normalizedType)
                    ? BuildBufferAddress(normalizedType, addrIndex, normalizedText, addrIndexIsHex)
                    : $"{normalizedType}{addrIndex}";
                AddLog("UI", "info", $"Added custom memory: {addrLabel}");
                OnPropertyChanged(nameof(CustomMemoryEntries));
            }
            catch (Exception ex)
            {
                AddLog("PC", "error", $"Failed to add entry: {ex.Message}");
            }
        }

        public void RemoveCustomMemoryEntry(CustomMemoryEntry entry)
        {
            try
            {
                CustomMemoryEntries.Remove(entry);
                AddLog("UI", "info", $"Removed custom memory: {entry.AddrType}{entry.AddrIndex}");
                OnPropertyChanged(nameof(CustomMemoryEntries));
            }
            catch (Exception ex)
            {
                AddLog("PC", "error", $"Failed to remove entry: {ex.Message}");
            }
        }

        public void RefreshCustomMemory()
        {
            try
            {
                var plc = ePLC;
                if (!Status || plc == null || CustomMemoryEntries.Count == 0)
                    return;

                var snapshot = CustomMemoryEntries.ToList();
                foreach (var entry in snapshot)
                {
                    try
                    {
                        if (TryReadValueCore(entry.AddrType, entry.AddrIndex, out int newValue, entry.AddrIndexText, entry.Read32, entry.AddrIndexIsHex))
                        {
                            entry.CurrentValue = newValue;
                            entry.LastUpdate = DateTime.Now;
                        }
                    }
                    catch { }
                }

                OnPropertyChanged(nameof(CustomMemoryEntries));
            }
            catch (Exception ex)
            {
                AddLog("PC", "error", $"RefreshCustomMemory error: {ex.Message}");
            }
        }
    }
}
