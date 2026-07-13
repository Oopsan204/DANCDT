using System;
using System.Globalization;

namespace DACDT_2026
{
    public static class BackgroundVideoServiceProcess
    {
        public const string ParentPidSwitch = "--parent-pid";

        public static string BuildParentPidArguments(int parentProcessId)
        {
            return ParentPidSwitch + " " + parentProcessId.ToString(CultureInfo.InvariantCulture);
        }

        public static int TryGetParentPid(string[] args)
        {
            if (args == null)
                return 0;

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], ParentPidSwitch, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pid) && pid > 0)
                    return pid;
            }

            return 0;
        }
    }
}
