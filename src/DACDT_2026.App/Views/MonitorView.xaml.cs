using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DACDT_2026.Views
{
    public partial class MonitorView : UserControl
    {
        private WpfUiState observedState;
        private readonly DispatcherTimer activeProgramScrollTimer;
        private bool activeProgramScrollPending;

        public MonitorView()
        {
            InitializeComponent();
            activeProgramScrollTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            activeProgramScrollTimer.Tick += ActiveProgramScrollTimer_Tick;
            DataContextChanged += MonitorView_DataContextChanged;
        }

        private void MonitorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            activeProgramScrollTimer.Stop();
            activeProgramScrollPending = false;

            if (observedState != null)
            {
                observedState.PropertyChanged -= ObservedState_PropertyChanged;
            }

            observedState = e.NewValue as WpfUiState;
            if (observedState != null)
            {
                observedState.PropertyChanged += ObservedState_PropertyChanged;
            }

            QueueActiveProgramScroll();
        }

        private void ObservedState_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WpfUiState.ActiveProgramIndex))
                QueueActiveProgramScroll();
        }

        private void QueueActiveProgramScroll()
        {
            activeProgramScrollPending = true;
            if (!activeProgramScrollTimer.IsEnabled)
                activeProgramScrollTimer.Start();
        }

        private void ActiveProgramScrollTimer_Tick(object sender, EventArgs e)
        {
            activeProgramScrollTimer.Stop();
            if (!activeProgramScrollPending)
                return;

            activeProgramScrollPending = false;
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

            ProgramGrid.ScrollIntoView(activeRow);
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
