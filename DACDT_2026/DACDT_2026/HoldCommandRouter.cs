using System.Windows.Input;

namespace DACDT_2026
{
    internal static class HoldCommandRouter
    {
        public static void Execute(WpfUiState state, string action, bool start)
        {
            if (state == null || string.IsNullOrEmpty(action))
                return;

            ICommand command = null;
            if (action == "home") command = start ? state.GoHomeStartCommand : state.GoHomeStopCommand;
            else if (action == "homeall") command = start ? state.HomeAllStartCommand : state.HomeAllStopCommand;
            else if (action == "reset") command = start ? state.ResetErrorStartCommand : state.ResetErrorStopCommand;
            else if (action == "start") command = start ? state.StartActionStartCommand : state.StartActionStopCommand;
            else if (action == "continue") command = start ? state.ContinueStartCommand : state.ContinueStopCommand;
            else if (action == "pause") command = start ? state.PauseStartCommand : state.PauseStopCommand;

            if (command != null && command.CanExecute(null))
                command.Execute(null);
        }
    }
}
