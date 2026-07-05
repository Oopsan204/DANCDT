namespace DACDT_2026
{
    public static class ExitShutdownPolicy
    {
        public static bool ShouldSendExitStop(bool plcConnected)
        {
            return plcConnected;
        }

        public static string GetConfirmationMessage(bool plcConnected)
        {
            return plcConnected
                ? "Exit will pulse M210, wait 500 ms, HOME ALL, wait 500 ms, clear buffers, then close the application. Continue?"
                : "Exit application?";
        }
    }
}
