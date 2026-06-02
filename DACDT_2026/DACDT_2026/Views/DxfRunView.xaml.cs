using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DACDT_2026.Views
{
    public partial class DxfRunView : UserControl
    {
        private const double MinCadZoom = 0.2;
        private const double MaxCadZoom = 20.0;
        private const double CadZoomStep = 1.15;

        public static readonly DependencyProperty CadDisplayZoomProperty =
            DependencyProperty.Register(
                nameof(CadDisplayZoom),
                typeof(double),
                typeof(DxfRunView),
                new PropertyMetadata(1.0));

        private string activeHold;
        private bool isCadPanning;
        private Point cadPanStartPoint;
        private double cadPanStartX;
        private double cadPanStartY;
        private double cadZoom = 1.0;

        public DxfRunView()
        {
            InitializeComponent();
        }

        public double CadDisplayZoom
        {
            get => (double)GetValue(CadDisplayZoomProperty);
            set => SetValue(CadDisplayZoomProperty, value);
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

        private void CadViewport_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CadSurface == null)
                return;

            Point mouse = e.GetPosition(CadSurface);
            double oldZoom = cadZoom;
            double factor = e.Delta > 0 ? CadZoomStep : 1.0 / CadZoomStep;
            double nextZoom = Math.Max(MinCadZoom, Math.Min(MaxCadZoom, oldZoom * factor));

            if (Math.Abs(nextZoom - oldZoom) < 0.0001)
                return;

            double ratio = nextZoom / oldZoom;
            CadPanTransform.X = mouse.X - ((mouse.X - CadPanTransform.X) * ratio);
            CadPanTransform.Y = mouse.Y - ((mouse.Y - CadPanTransform.Y) * ratio);
            ApplyCadZoom(nextZoom);

            e.Handled = true;
        }

        private void CadViewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                ResetCadView();
                e.Handled = true;
                return;
            }

            isCadPanning = true;
            cadPanStartPoint = e.GetPosition(CadSurface);
            cadPanStartX = CadPanTransform.X;
            cadPanStartY = CadPanTransform.Y;
            CadViewport.CaptureMouse();
            CadViewport.Cursor = Cursors.SizeAll;
            e.Handled = true;
        }

        private void CadViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isCadPanning || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point current = e.GetPosition(CadSurface);
            CadPanTransform.X = cadPanStartX + current.X - cadPanStartPoint.X;
            CadPanTransform.Y = cadPanStartY + current.Y - cadPanStartPoint.Y;
            e.Handled = true;
        }

        private void CadViewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EndCadPan();
            e.Handled = true;
        }

        private void CadViewport_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isCadPanning && e.LeftButton != MouseButtonState.Pressed)
                EndCadPan();
        }

        private void EndCadPan()
        {
            if (!isCadPanning)
                return;

            isCadPanning = false;
            CadViewport.ReleaseMouseCapture();
            CadViewport.Cursor = Cursors.Hand;
        }

        private void ResetCadView()
        {
            ApplyCadZoom(1.0);
            CadPanTransform.X = 0.0;
            CadPanTransform.Y = 0.0;
        }

        private void ApplyCadZoom(double zoom)
        {
            cadZoom = zoom;
            CadDisplayZoom = zoom;
            CadZoomTransform.ScaleX = zoom;
            CadZoomTransform.ScaleY = zoom;
        }
    }
}
