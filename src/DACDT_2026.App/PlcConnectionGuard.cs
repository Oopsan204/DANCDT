namespace DACDT_2026
{
    public static class PlcConnectionGuard
    {
        public const string NotConnectedMessage = "PLC is not connected.";

        public static bool CanUsePlc(bool communicationObjectExists, bool isConnected)
        {
            return communicationObjectExists && isConnected;
        }
    }
}
