using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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
        private Point touchStartPoint;
        private Point touchLastPoint;
        private bool isCadPinchRenderSubscribed;
        private bool mousePanExceededThreshold;
        private bool touchPanExceededThreshold;
        private readonly BitmapCache cadInteractionCache = new BitmapCache
        {
            EnableClearType = false,
            RenderAtScale = 1
        };
        private readonly DispatcherTimer cadWheelIdleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };

        public DxfRunView()
        {
            InitializeComponent();
            cadWheelIdleTimer.Tick += CadWheelIdleTimer_Tick;
            CadViewport.LostMouseCapture += CadViewport_LostMouseCapture;
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

            BeginCadInteractionRendering();
            Point mouse = e.GetPosition(CadSurface);
            double oldZoom = cadZoom;
            double factor = e.Delta > 0 ? CadZoomStep : 1.0 / CadZoomStep;
            double nextZoom = Math.Max(MinCadZoom, Math.Min(MaxCadZoom, oldZoom * factor));

            if (Math.Abs(nextZoom - oldZoom) < 0.0001)
            {
                EndCadInteractionRendering();
                return;
            }

            double ratio = nextZoom / oldZoom;
            CadPanTransform.X = mouse.X - ((mouse.X - CadPanTransform.X) * ratio);
            CadPanTransform.Y = mouse.Y - ((mouse.Y - CadPanTransform.Y) * ratio);
            ApplyCadZoom(nextZoom);
            cadWheelIdleTimer.Stop();
            cadWheelIdleTimer.Start();

            e.Handled = true;
        }

        private void CadViewport_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (CadSurface == null || e.TouchDevice == null)
                return;

            BeginCadInteractionRendering();
            Point position = e.GetTouchPoint(CadSurface).Position;
            touchSession.BeginTouch(e.TouchDevice.Id, position);

            if (touchSession.IsPinching)
            {
                isCadPanning = false;
                touchPanExceededThreshold = true;
                CadViewport.ReleaseMouseCapture();
                StartCadPinchRenderLoop();
            }
            else
            {
                touchPanExceededThreshold = false;
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
                if (touchPanExceededThreshold
                    || Distance(touchStartPoint, current) >= TouchPanThreshold)
                {
                    touchPanExceededThreshold = true;
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
            bool shouldSelect = !wasPinching && !touchPanExceededThreshold && Distance(touchStartPoint, position) < TouchPanThreshold;
            touchSession.EndTouch(e.TouchDevice.Id);

            if (endedPinch)
                StopCadPinchRenderLoop();

            if (shouldSelect)
                SelectCadPathAt(e.GetTouchPoint(CadContent).Position);

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

            BeginCadInteractionRendering();
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

        private void SelectCadPathAt(Point contentPoint)
        {
            var state = DataContext as WpfUiState;
            if (state?.CadPathHitIndex == null || state.ToggleCadPathCommand == null)
                return;

            double contentRadius = 12.0 / Math.Max(GetCadViewboxScale() * cadZoom, 0.0001);
            int pathId;
            if (state.CadPathHitIndex.TryFindNearest(contentPoint, contentRadius, out pathId)
                && state.ToggleCadPathCommand.CanExecute(pathId))
            {
                state.ToggleCadPathCommand.Execute(pathId);
            }
        }

        private double GetCadViewboxScale()
        {
            if (CadPreviewViewbox == null || CadSurface == null
                || CadSurface.Width <= 0.0 || CadSurface.Height <= 0.0)
            {
                return 1.0;
            }

            double scaleX = CadPreviewViewbox.ActualWidth / CadSurface.Width;
            double scaleY = CadPreviewViewbox.ActualHeight / CadSurface.Height;
            return Math.Max(0.0001, Math.Min(scaleX, scaleY));
        }

        private void ResetTouchGesture()
        {
            StopCadPinchRenderLoop();
            touchSession.Reset();
            touchPanExceededThreshold = false;
            EndCadInteractionRendering();
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

            BeginCadInteractionRendering();
            isCadPanning = true;
            mousePanExceededThreshold = false;
            cadPanStartPoint = e.GetPosition(CadSurface);
            cadPanStartX = CadPanTransform.X;
            cadPanStartY = CadPanTransform.Y;
            CadViewport.CaptureMouse();
            CadViewport.Cursor = Cursors.SizeAll;
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
            if (mousePanExceededThreshold
                || Distance(cadPanStartPoint, current) >= TouchPanThreshold)
            {
                mousePanExceededThreshold = true;
                CadPanTransform.X = cadPanStartX + current.X - cadPanStartPoint.X;
                CadPanTransform.Y = cadPanStartY + current.Y - cadPanStartPoint.Y;
            }
            e.Handled = true;
        }

        private void CadViewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null || touchSession.IsTouchActive)
            {
                e.Handled = true;
                return;
            }

            bool shouldSelect = isCadPanning
                && !mousePanExceededThreshold
                && Distance(cadPanStartPoint, e.GetPosition(CadSurface)) < TouchPanThreshold;
            Point contentPoint = e.GetPosition(CadContent);
            EndCadPan();
            if (shouldSelect)
                SelectCadPathAt(contentPoint);
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
            isCadPanning = false;
            mousePanExceededThreshold = false;
            if (CadViewport.IsMouseCaptured)
                CadViewport.ReleaseMouseCapture();
            CadViewport.Cursor = Cursors.Hand;
            EndCadInteractionRendering();
        }

        private void ResetCadView()
        {
            EndCadInteractionRendering();
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

        private void CadWheelIdleTimer_Tick(object sender, EventArgs e)
        {
            cadWheelIdleTimer.Stop();
            EndCadInteractionRendering();
        }

        private void CadViewport_LostMouseCapture(object sender, MouseEventArgs e)
        {
            EndCadPan();
            EndCadInteractionRendering();
        }

        private void BeginCadInteractionRendering()
        {
            cadWheelIdleTimer.Stop();
            if (CadContent != null)
                CadContent.CacheMode = cadInteractionCache;
        }

        private void EndCadInteractionRendering()
        {
            cadWheelIdleTimer.Stop();
            if (CadContent != null)
                CadContent.CacheMode = null;
        }

    }
}
