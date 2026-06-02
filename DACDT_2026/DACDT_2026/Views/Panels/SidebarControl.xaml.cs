using System.Windows.Controls;
using System.Windows.Input;

namespace DACDT_2026.Views.Panels
{
    public partial class SidebarControl : UserControl
    {
        private object activeJogOffset;

        public SidebarControl()
        {
            InitializeComponent();
        }

        private void JogButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            activeJogOffset = (sender as Button)?.Tag;
            var command = (DataContext as WpfUiState)?.JogStartCommand;
            if (command != null && command.CanExecute(activeJogOffset))
                command.Execute(activeJogOffset);
        }

        private void JogButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            StopJog();
        }

        private void JogButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                StopJog();
        }

        private void StopJog()
        {
            var command = (DataContext as WpfUiState)?.JogStopCommand;
            if (command != null && command.CanExecute(activeJogOffset))
                command.Execute(activeJogOffset);
            activeJogOffset = null;
        }
    }
}
