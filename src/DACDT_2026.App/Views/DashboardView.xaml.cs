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
        private WpfUiState observedState;

        public DashboardView()
        {
            InitializeComponent();
            DataContextChanged += DashboardView_DataContextChanged;
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
            HoldCommandRouter.Execute(DataContext as WpfUiState, activeHold, start);

            if (!start)
                activeHold = null;
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
