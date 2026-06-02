using System.Windows.Controls;
using System.Windows.Input;

namespace DACDT_2026.Views
{
    public partial class DashboardView : UserControl
    {
        private string activeHold;

        public DashboardView()
        {
            InitializeComponent();
        }

        private void HoldButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            activeHold = (sender as Button)?.Tag as string;
            ExecuteHoldCommand(true);
        }

        private void HoldButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ExecuteHoldCommand(false);
        }

        private void HoldButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                ExecuteHoldCommand(false);
        }

        private void ExecuteHoldCommand(bool start)
        {
            var state = DataContext as WpfUiState;
            if (state == null || string.IsNullOrEmpty(activeHold)) return;

            ICommand command = null;
            if (activeHold == "home") command = start ? state.GoHomeStartCommand : state.GoHomeStopCommand;
            else if (activeHold == "reset") command = start ? state.ResetErrorStartCommand : state.ResetErrorStopCommand;
            else if (activeHold == "start") command = start ? state.StartActionStartCommand : state.StartActionStopCommand;
            else if (activeHold == "continue") command = start ? state.ContinueStartCommand : state.ContinueStopCommand;
            else if (activeHold == "pause") command = start ? state.PauseStartCommand : state.PauseStopCommand;

            if (command != null && command.CanExecute(null))
                command.Execute(null);

            if (!start)
                activeHold = null;
        }
    }
}
