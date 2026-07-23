using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DACDT_2026.Views
{
    public partial class DxfRunView : UserControl
    {
        private const double MinCadZoom = 0.2;
        private const double MaxCadZoom = 20.0;
        private const double CadZoomStep = 1.15;
        private const double TouchPanThreshold = 8.0;

        public static readonly DependencyProperty CadDisplayZoomProperty =
            DependencyProperty.Register(
                nameof(CadDisplayZoom),
                typeof(double),
                typeof(DxfRunView),
                new PropertyMetadata(1.0));

        private string activeHold;
        private Button activeHoldButton;
        private bool isCadPanning;
        private Point cadPanStartPoint;
        private double cadPanStartX;
        private double cadPanStartY;
        private double cadZoom = 1.0;
        private readonly CadTouchGestureSession touchSession = new CadTouchGestureSession();
        private CadPrimitiveViewModel touchStartCadItem;
        private Point touchStartPoint;
        private Point touchLastPoint;
        private bool isCadPinchRenderSubscribed;

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

        private void CadViewport_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.StylusDevice != null || touchSession.IsTouchActive)
            {
                e.Handled = true;
                return;
            }

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

        private void CadViewport_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (CadSurface == null || e.TouchDevice == null)
                return;

            Point position = e.GetTouchPoint(CadSurface).Position;
            touchSession.BeginTouch(e.TouchDevice.Id, position);

            if (touchSession.IsPinching)
            {
                isCadPanning = false;
                touchStartCadItem = null;
                CadViewport.ReleaseMouseCapture();
                StartCadPinchRenderLoop();
            }
            else
            {
                touchStartCadItem = FindCadPrimitive(e.OriginalSource as DependencyObject);
                touchStartPoint = position;
                touchLastPoint = position;
            }

            e.TouchDevice.Capture(CadViewport);
            e.Handled = true;
        }

        private void CadViewport_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            if (CadSurface == null || e.TouchDevice == null)
                return;

            Point current = e.GetTouchPoint(CadSurface).Position;
            touchSession.UpdateTouch(e.TouchDevice.Id, current);
            if (touchSession.IsPinching)
            {
                e.Handled = true;
                return;
            }

            if (touchSession.IsTouchActive)
            {
                Vector delta = current - touchLastPoint;
                if (Distance(touchStartPoint, current) >= TouchPanThreshold)
                {
                    CadPanTransform.X += delta.X;
                    CadPanTransform.Y += delta.Y;
                }
                touchLastPoint = current;
            }

            e.Handled = true;
        }

        private void CadViewport_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (CadSurface == null || e.TouchDevice == null || !touchSession.IsTouchActive)
                return;

            Point position = e.GetTouchPoint(CadSurface).Position;
            bool endedPinch = touchSession.IsPinching && touchSession.IsPinchTouch(e.TouchDevice.Id);
            bool wasPinching = touchSession.IsPinching;
            bool shouldSelect = !wasPinching && Distance(touchStartPoint, position) < TouchPanThreshold;
            touchSession.EndTouch(e.TouchDevice.Id);

            if (endedPinch)
                StopCadPinchRenderLoop();

            if (shouldSelect)
                SelectCadPrimitive(touchStartCadItem);

            if (!touchSession.IsTouchActive)
                ResetTouchGesture();

            e.Handled = true;
        }

        private void CadViewport_LostTouchCapture(object sender, TouchEventArgs e)
        {
            ResetTouchGesture();
            e.Handled = true;
        }

        private void StartCadPinchRenderLoop()
        {
            if (isCadPinchRenderSubscribed)
                return;

            CompositionTarget.Rendering += ApplyPendingCadPinchFrame;
            isCadPinchRenderSubscribed = true;
        }

        private void StopCadPinchRenderLoop()
        {
            if (!isCadPinchRenderSubscribed)
                return;

            CompositionTarget.Rendering -= ApplyPendingCadPinchFrame;
            isCadPinchRenderSubscribed = false;
        }

        private void ApplyPendingCadPinchFrame(object sender, EventArgs e)
        {
            if (!touchSession.TryTakePinchFrame(out CadPinchFrame frame))
                return;

            ApplyCadPinchTransform(
                Distance(frame.PreviousPrimary, frame.PreviousSecondary),
                Distance(frame.Primary, frame.Secondary),
                Midpoint(frame.PreviousPrimary, frame.PreviousSecondary),
                Midpoint(frame.Primary, frame.Secondary));
        }

        private void ApplyCadPinchTransform(double oldDistance, double newDistance, Point oldMidpoint, Point newMidpoint)
        {
            double ratio = oldDistance > 0.001 ? newDistance / oldDistance : 1.0;
            double oldZoom = cadZoom;
            double nextZoom = Math.Max(MinCadZoom, Math.Min(MaxCadZoom, oldZoom * ratio));
            double appliedRatio = oldZoom > 0.001 ? nextZoom / oldZoom : 1.0;

            CadPanTransform.X = oldMidpoint.X - ((oldMidpoint.X - CadPanTransform.X) * appliedRatio)
                + (newMidpoint.X - oldMidpoint.X);
            CadPanTransform.Y = oldMidpoint.Y - ((oldMidpoint.Y - CadPanTransform.Y) * appliedRatio)
                + (newMidpoint.Y - oldMidpoint.Y);
            ApplyCadZoom(nextZoom);
        }

        private static double Distance(Point first, Point second)
        {
            return (first - second).Length;
        }

        private static Point Midpoint(Point first, Point second)
        {
            return new Point((first.X + second.X) / 2.0, (first.Y + second.Y) / 2.0);
        }

        private static CadPrimitiveViewModel FindCadPrimitive(DependencyObject source)
        {
            DependencyObject current = source;
            while (current != null)
            {
                if (current is FrameworkElement element && element.DataContext is CadPrimitiveViewModel item)
                    return item;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void SelectCadPrimitive(CadPrimitiveViewModel item)
        {
            var state = DataContext as WpfUiState;
            if (item == null || state?.ToggleCadPathCommand == null)
                return;

            if (state.ToggleCadPathCommand.CanExecute(item.PathId))
                state.ToggleCadPathCommand.Execute(item.PathId);
        }

        private void ResetTouchGesture()
        {
            StopCadPinchRenderLoop();
            touchSession.Reset();
            touchStartCadItem = null;
        }

        private void CadViewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null || touchSession.IsTouchActive)
            {
                e.Handled = true;
                return;
            }

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

        private void SelectableCadPath_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null || touchSession.IsTouchActive)
            {
                e.Handled = true;
                return;
            }

            if (e.ClickCount >= 2)
            {
                ResetCadView();
                e.Handled = true;
                return;
            }

            var item = (sender as FrameworkElement)?.DataContext as CadPrimitiveViewModel;
            var state = DataContext as WpfUiState;
            if (item == null || state?.ToggleCadPathCommand == null)
            {
                e.Handled = true;
                return;
            }

            if (state.ToggleCadPathCommand.CanExecute(item.PathId))
                state.ToggleCadPathCommand.Execute(item.PathId);

            e.Handled = true;
        }

        private void CadViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.StylusDevice != null || touchSession.IsTouchActive)
            {
                e.Handled = true;
                return;
            }

            if (!isCadPanning || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point current = e.GetPosition(CadSurface);
            CadPanTransform.X = cadPanStartX + current.X - cadPanStartPoint.X;
            CadPanTransform.Y = cadPanStartY + current.Y - cadPanStartPoint.Y;
            e.Handled = true;
        }

        private void CadViewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null || touchSession.IsTouchActive)
            {
                e.Handled = true;
                return;
            }

            EndCadPan();
            e.Handled = true;
        }

        private void CadViewport_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.StylusDevice != null || touchSession.IsTouchActive)
                return;

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
