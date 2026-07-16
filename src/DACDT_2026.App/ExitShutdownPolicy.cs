using System;
using System.Threading.Tasks;

namespace DACDT_2026
{
    public static class ExitShutdownPolicy
    {
        public const int PlcExitWaitTimeoutMs = 600;

        public static bool ShouldSendExitStop(bool plcConnected)
        {
            return plcConnected;
        }

        public static Task WaitForBestEffortAsync(Task operation)
        {
            return WaitForBestEffortAsync(operation, PlcExitWaitTimeoutMs);
        }

        public static async Task WaitForBestEffortAsync(Task operation, int timeoutMs)
        {
            if (operation == null)
                return;

            Task completed = await Task.WhenAny(operation, Task.Delay(Math.Max(0, timeoutMs))).ConfigureAwait(false);
            if (completed == operation)
                await operation.ConfigureAwait(false);
        }

        public static string GetConfirmationMessage(bool plcConnected)
        {
            return "Confirm exit?";
        }
    }
}
