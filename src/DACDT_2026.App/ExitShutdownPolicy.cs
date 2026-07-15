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
            return "Confirm exit?";
        }
    }
}
