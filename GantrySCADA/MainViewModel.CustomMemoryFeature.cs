using CommunityToolkit.Mvvm.ComponentModel;
using NVKProject.PLC;
using System;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
        private bool TryReadValueCore(string addrType, int addrIndex, out int value)
        {
            value = 0;

            if (!Status || ePLC == null)
                return false;

            string normalizedType = NormalizeAddrType(addrType);

            ePLCControl.DeviceName devName = normalizedType switch
            {
                "M" => ePLCControl.DeviceName.M,
                "X" => ePLCControl.DeviceName.X,
                "Y" => ePLCControl.DeviceName.Y,
                _ => ePLCControl.DeviceName.D
            };

            bool isBitDevice = normalizedType == "M" || normalizedType == "X" || normalizedType == "Y";

            try
            {
                if (isBitDevice)
                {
                    int[] bit = ePLC.ReadDeviceBlock(ePLCControl.SubCommand.Bit, devName, $"{addrIndex}", 1);
                    if (bit != null && bit.Length > 0)
                    {
                        value = bit[0] != 0 ? 1 : 0;
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
                    && x.AddrIndex == addrIndex);

                if (exists)
                    return;

                var entry = new CustomMemoryEntry { AddrType = normalizedType, AddrIndex = addrIndex };
                CustomMemoryEntries.Add(entry);
                AddLog("UI", "info", $"Added custom memory: {normalizedType}{addrIndex}");
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
                        if (TryReadValueCore(entry.AddrType, entry.AddrIndex, out int newValue))
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
