using System;
using System.Globalization;

namespace DACDT_2026
{
    internal static class CameraRecordingSummary
    {
        public static string FormatElapsed(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
                elapsed = TimeSpan.Zero;

            int totalHours = (int)Math.Floor(elapsed.TotalHours);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}:{2:00}",
                totalHours,
                elapsed.Minutes,
                elapsed.Seconds);
        }

        public static string FormatFileSize(long bytes)
        {
            long safeBytes = Math.Max(0, bytes);
            if (safeBytes < 1024)
                return safeBytes.ToString(CultureInfo.InvariantCulture) + " B";

            if (safeBytes < 1024L * 1024L)
                return string.Format(CultureInfo.InvariantCulture, "{0:0.0} KB", safeBytes / 1024d);

            return string.Format(CultureInfo.InvariantCulture, "{0:0.0} MB", safeBytes / (1024d * 1024d));
        }

        public static string FormatSavedText(TimeSpan elapsed, long bytes)
        {
            return "MP4 saved: " + FormatElapsed(elapsed) + " (" + FormatFileSize(bytes) + ")";
        }
    }
}
