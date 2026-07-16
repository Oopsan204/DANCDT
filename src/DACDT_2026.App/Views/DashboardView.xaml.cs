using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DACDT_2026.Views
{
    public partial class DashboardView : UserControl
    {
        private string activeHold;
        private Button activeHoldButton;
        private WpfUiState observedState;

        public DashboardView()
        {
            InitializeComponent();
            DataContextChanged += DashboardView_DataContextChanged;
        }

        private void HoldButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Button button))
                return;

            StopHold();
            activeHold = button.Tag as string;
            activeHoldButton = button;
            button.CaptureMouse();
            ExecuteHoldCommand(true);
        }

        private void HoldButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            StopHold();
        }

        private void HoldButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && activeHoldButton == null)
                StopHold();
        }

        private void HoldButton_LostMouseCapture(object sender, MouseEventArgs e)
        {
            StopHold();
        }

        private void ExecuteHoldCommand(bool start)
        {
            if (!start)
            {
                StopHold();
                return;
            }

            HoldCommandRouter.Execute(DataContext as WpfUiState, activeHold, start);
        }

        private void StopHold()
        {
            string action = activeHold;
            Button button = activeHoldButton;
            activeHold = null;
            activeHoldButton = null;

            if (button != null && button.IsMouseCaptured)
                button.ReleaseMouseCapture();

            if (!string.IsNullOrEmpty(action))
                HoldCommandRouter.Execute(DataContext as WpfUiState, action, false);
        }

        private void DashboardView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (observedState != null)
            {
                observedState.PropertyChanged -= ObservedState_PropertyChanged;
                observedState.ProgramRows.CollectionChanged -= ProgramRows_CollectionChanged;
            }

            observedState = e.NewValue as WpfUiState;
            if (observedState != null)
            {
                observedState.PropertyChanged += ObservedState_PropertyChanged;
                observedState.ProgramRows.CollectionChanged += ProgramRows_CollectionChanged;
            }

            ScrollActiveProgramRow();
        }

        private void ObservedState_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WpfUiState.ActiveProgramIndex))
                ScrollActiveProgramRow();
        }

        private void ProgramRows_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ScrollActiveProgramRow();
        }

        private void ScrollActiveProgramRow()
        {
            if (observedState == null || ProgramGrid == null || observedState.ActiveProgramIndex <= 0)
                return;

            observedState.EnsureProcessRowVisible(observedState.ActiveProgramIndex);

            ProcessRowViewModel activeRow = null;
            foreach (var row in observedState.ProgramRows)
            {
                if (row.Index == observedState.ActiveProgramIndex)
                {
                    activeRow = row;
                    break;
                }
            }

            if (activeRow == null)
                return;

            Dispatcher.BeginInvoke(new Action(() => ProgramGrid.ScrollIntoView(activeRow)));
        }

        private void ProgramGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (observedState == null || !IsNearScrollEnd(e))
                return;

            observedState.LoadMoreProgramRows();
        }

        private static bool IsNearScrollEnd(ScrollChangedEventArgs e)
        {
            if (e.ExtentHeight <= 0.0 || e.ViewportHeight <= 0.0)
                return false;

            return e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 8.0;
        }
    }
}
