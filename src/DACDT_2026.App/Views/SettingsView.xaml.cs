using System.Windows.Controls;

namespace DACDT_2026.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void WcsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var state = DataContext as WpfUiState;
            var grid = sender as DataGrid;
            var selectedWcs = grid?.SelectedValue as string;

            if (state?.SelectWcsCommand == null || string.IsNullOrWhiteSpace(selectedWcs))
                return;

            if (state.SelectWcsCommand.CanExecute(selectedWcs))
                state.SelectWcsCommand.Execute(selectedWcs);
        }
    }
}
