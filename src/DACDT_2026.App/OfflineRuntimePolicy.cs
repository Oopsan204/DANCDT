namespace DACDT_2026
{
    internal static class OfflineRuntimePolicy
    {
        public static bool Enabled { get { return true; } }
        public static bool ShouldStartMqtt { get { return false; } }
        public static bool ShouldStartWebRtc { get { return false; } }
    }
}
