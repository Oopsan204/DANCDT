using CommunityToolkit.Mvvm.ComponentModel;
using NVKProject.PLC;
using System;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
        private bool TryReadValueCore(string addrType, int addrIndex, out int value, string? addrIndexText = null)
        {
            value = 0;

            if (!Status || ePLC == null)
                return false;

            string normalizedType = NormalizeAddrType(addrType);

            if (IsBufferType(normalizedType))
            {
                try
                {
                    int[] word = ePLC.ReadDeviceBlock(
                        ePLCControl.SubCommand.Word,
                        ePLCControl.DeviceName.Buffer,
                        BuildBufferAddress(normalizedType, addrIndex, addrIndexText),
                        1);
                    if (word != null && word.Length > 0)
                    {
                        value = word[0];
                        return true;
                    }

                    return false;
                }
                catch
                {
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
                    int[] bit = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Bit, devName, $"{addrIndex}", 1);
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

                    int[] wordFallback = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, devName, $"{addrIndex}", 1);
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

                int[] word = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, devName, $"{addrIndex}", 1);
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
                        int[] wordFallback = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Word, devName, $"{addrIndex}", 1);
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

                var entry = new CustomMemoryEntry { AddrType = normalizedType, AddrIndex = addrIndex, AddrIndexText = string.Empty };
                CustomMemoryEntries.Add(entry);
                AddLog("UI", "info", $"Added custom memory: {normalizedType}{addrIndex}");
                OnPropertyChanged(nameof(CustomMemoryEntries));
            }
            catch (Exception ex)
            {
                AddLog("PC", "error", $"Failed to add entry: {ex.Message}");
            }
        }

        public void AddCustomMemoryEntry(string addrType, int addrIndex, string? addrIndexText)
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
                    return;

                var entry = new CustomMemoryEntry
                {
                    AddrType = normalizedType,
                    AddrIndex = addrIndex,
                    AddrIndexText = normalizedText
                };

                CustomMemoryEntries.Add(entry);
                string addrLabel = IsBufferType(normalizedType)
                    ? BuildBufferAddress(normalizedType, addrIndex, normalizedText)
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
                if (!Status || ePLC == null || CustomMemoryEntries.Count == 0)
                    return;

                var snapshot = CustomMemoryEntries.ToList();
                foreach (var entry in snapshot)
                {
                    try
                    {
                        if (TryReadValueCore(entry.AddrType, entry.AddrIndex, out int newValue, entry.AddrIndexText))
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
