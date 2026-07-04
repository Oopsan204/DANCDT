using System;
using System.Collections.Generic;
using System.Linq;

namespace DACDT_2026
{
    public static class CameraDeviceSelection
    {
        public const int ReconnectDelayMs = 1000;

        public sealed class CameraDevice
        {
            public CameraDevice(string name, string monikerString)
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Camera" : name.Trim();
                MonikerString = monikerString ?? string.Empty;
            }

            public string Name { get; }
            public string MonikerString { get; }
            public string DisplayName => Name;

            public override string ToString()
            {
                return DisplayName;
            }
        }

        public static CameraDevice FindByMonikerOrPreferred(IEnumerable<CameraDevice> cameras, string moniker)
        {
            var list = cameras?.Where(c => c != null).ToList() ?? new List<CameraDevice>();
            if (list.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(moniker))
            {
                var exact = list.FirstOrDefault(c => string.Equals(c.MonikerString, moniker, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
            }

            return list.FirstOrDefault(c =>
                       c.MonikerString.IndexOf("usb#", StringComparison.OrdinalIgnoreCase) >= 0
                       || c.MonikerString.StartsWith("@device:pnp", StringComparison.OrdinalIgnoreCase))
                   ?? list[0];
        }

        public static bool ShouldSwitch(string activeMoniker, string selectedMoniker)
        {
            return !string.IsNullOrWhiteSpace(activeMoniker)
                   && !string.IsNullOrWhiteSpace(selectedMoniker)
                   && !string.Equals(activeMoniker, selectedMoniker, StringComparison.OrdinalIgnoreCase);
        }
    }
}
