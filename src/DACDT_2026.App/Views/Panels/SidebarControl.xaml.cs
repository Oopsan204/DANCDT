using System.Windows.Controls;
using System.Windows.Input;

namespace DACDT_2026.Views.Panels
{
    public partial class SidebarControl : UserControl
    {
        private object activeJogOffset;
        private Button activeJogButton;

        public SidebarControl()
        {
            InitializeComponent();
        }

        private void JogButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Button button))
                return;

            activeJogOffset = button.Tag;
            activeJogButton = button;
            button.CaptureMouse();
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
            if (e.LeftButton == MouseButtonState.Pressed && activeJogButton == null)
                StopJog();
        }

        private void JogButton_LostMouseCapture(object sender, MouseEventArgs e)
        {
            StopJog();
        }

        private void StopJog()
        {
            object offset = activeJogOffset;
            Button button = activeJogButton;
            activeJogOffset = null;
            activeJogButton = null;

            if (button != null && button.IsMouseCaptured)
                button.ReleaseMouseCapture();

            if (offset == null)
                return;

            var command = (DataContext as WpfUiState)?.JogStopCommand;
            if (command != null && command.CanExecute(offset))
                command.Execute(offset);
        }
    }
}
