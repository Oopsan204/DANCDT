using System;
using System.Collections.Generic;
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
        private readonly Dictionary<int, CadTouchPoint> activeTouchPoints = new Dictionary<int, CadTouchPoint>();
        private CadPrimitiveViewModel touchStartCadItem;
        private Point touchStartPoint;
        private Point touchLastPoint;
        private bool isTouchPinching;
        private double touchPreviousDistance;
        private Point touchPreviousMidpoint;

        private sealed class CadTouchPoint
        {
            public TouchDevice Device { get; set; }
            public Point Position { get; set; }
        }

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
            activeTouchPoints[e.TouchDevice.Id] = new CadTouchPoint
            {
                Device = e.TouchDevice,
                Position = position
            };

            if (activeTouchPoints.Count == 1)
            {
                touchStartCadItem = FindCadPrimitive(e.OriginalSource as DependencyObject);
                touchStartPoint = position;
                touchLastPoint = position;
                isTouchPinching = false;
                e.TouchDevice.Capture(CadViewport);
            }
            else if (activeTouchPoints.Count == 2)
            {
                isTouchPinching = true;
                isCadPanning = false;
                CadViewport.ReleaseMouseCapture();

                var pair = GetPinchPair();
                touchPreviousDistance = Distance(pair[0].Position, pair[1].Position);
                touchPreviousMidpoint = Midpoint(pair[0].Position, pair[1].Position);
                e.TouchDevice.Capture(CadViewport);
            }

            e.Handled = true;
        }

        private void CadViewport_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            if (!activeTouchPoints.TryGetValue(e.TouchDevice.Id, out CadTouchPoint touchPoint))
                return;

            touchPoint.Position = e.GetTouchPoint(CadSurface).Position;
            if (activeTouchPoints.Count >= 2)
            {
                var pair = GetPinchPair();
                Point midpoint = Midpoint(pair[0].Position, pair[1].Position);
                double distance = Distance(pair[0].Position, pair[1].Position);
                ApplyCadPinchTransform(touchPreviousDistance, distance, touchPreviousMidpoint, midpoint);
                touchPreviousDistance = distance;
                touchPreviousMidpoint = midpoint;
                e.Handled = true;
                return;
            }

            if (!isTouchPinching)
            {
                Point current = touchPoint.Position;
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
            if (!activeTouchPoints.TryGetValue(e.TouchDevice.Id, out CadTouchPoint touchPoint))
                return;

            touchPoint.Position = e.GetTouchPoint(CadSurface).Position;
            activeTouchPoints.Remove(e.TouchDevice.Id);

            if (activeTouchPoints.Count == 0)
            {
                if (!isTouchPinching && Distance(touchStartPoint, touchPoint.Position) < TouchPanThreshold)
                    SelectCadPrimitive(touchStartCadItem);

                ResetTouchGesture();
            }

            e.Handled = true;
        }

        private void CadViewport_LostTouchCapture(object sender, TouchEventArgs e)
        {
            activeTouchPoints.Remove(e.TouchDevice.Id);
            if (activeTouchPoints.Count == 0)
                ResetTouchGesture();
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

        private CadTouchPoint[] GetPinchPair()
        {
            var pair = new CadTouchPoint[2];
            int index = 0;
            foreach (CadTouchPoint point in activeTouchPoints.Values)
            {
                pair[index++] = point;
                if (index == pair.Length)
                    break;
            }
            return pair;
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
            activeTouchPoints.Clear();
            touchStartCadItem = null;
            isTouchPinching = false;
            touchPreviousDistance = 0.0;
            touchPreviousMidpoint = new Point();
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

        private void SelectableCadPath_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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

        private void LazyTable_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!IsNearScrollEnd(e))
                return;

            var state = DataContext as WpfUiState;
            if (state == null)
                return;

            if (ReferenceEquals(sender, ProcessTableGrid))
                state.LoadMoreProcessRows();
            else if (ReferenceEquals(sender, GeometryDataGrid))
                state.LoadMoreGeometryRows();
        }

        private static bool IsNearScrollEnd(ScrollChangedEventArgs e)
        {
            if (e.ExtentHeight <= 0.0 || e.ViewportHeight <= 0.0)
                return false;

            return e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 8.0;
        }
    }
}
