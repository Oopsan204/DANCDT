using System.Collections.Generic;
using System.Windows;

namespace DACDT_2026
{
    public struct CadPinchFrame
    {
        public CadPinchFrame(
            int primaryTouchId,
            int secondaryTouchId,
            Point previousPrimary,
            Point previousSecondary,
            Point primary,
            Point secondary)
        {
            PrimaryTouchId = primaryTouchId;
            SecondaryTouchId = secondaryTouchId;
            PreviousPrimary = previousPrimary;
            PreviousSecondary = previousSecondary;
            Primary = primary;
            Secondary = secondary;
        }

        public int PrimaryTouchId { get; }
        public int SecondaryTouchId { get; }
        public Point PreviousPrimary { get; }
        public Point PreviousSecondary { get; }
        public Point Primary { get; }
        public Point Secondary { get; }
    }

    public sealed class CadTouchGestureSession
    {
        private readonly Dictionary<int, Point> touchPositions = new Dictionary<int, Point>();
        private int primaryTouchId = -1;
        private int secondaryTouchId = -1;
        private Point previousPrimary;
        private Point previousSecondary;
        private bool pinchFramePending;

        public bool IsTouchActive => touchPositions.Count > 0;

        public bool IsPinching => primaryTouchId >= 0 && secondaryTouchId >= 0;

        public bool IsPinchTouch(int touchId)
        {
            return touchId == primaryTouchId || touchId == secondaryTouchId;
        }

        public void BeginTouch(int touchId, Point position)
        {
            touchPositions[touchId] = position;

            if (primaryTouchId < 0)
            {
                primaryTouchId = touchId;
                return;
            }

            if (secondaryTouchId >= 0 || touchId == primaryTouchId)
                return;

            secondaryTouchId = touchId;
            previousPrimary = touchPositions[primaryTouchId];
            previousSecondary = position;
            pinchFramePending = false;
        }

        public void UpdateTouch(int touchId, Point position)
        {
            if (!touchPositions.ContainsKey(touchId))
                return;

            touchPositions[touchId] = position;
            if (IsPinching && (touchId == primaryTouchId || touchId == secondaryTouchId))
                pinchFramePending = true;
        }

        public void EndTouch(int touchId)
        {
            if (IsPinchTouch(touchId))
            {
                Reset();
                return;
            }

            touchPositions.Remove(touchId);
        }

        public bool TryTakePinchFrame(out CadPinchFrame frame)
        {
            if (!pinchFramePending
                || !IsPinching
                || !touchPositions.TryGetValue(primaryTouchId, out Point primary)
                || !touchPositions.TryGetValue(secondaryTouchId, out Point secondary))
            {
                frame = default(CadPinchFrame);
                return false;
            }

            pinchFramePending = false;
            frame = new CadPinchFrame(
                primaryTouchId,
                secondaryTouchId,
                previousPrimary,
                previousSecondary,
                primary,
                secondary);
            previousPrimary = primary;
            previousSecondary = secondary;
            return true;
        }

        public void Reset()
        {
            touchPositions.Clear();
            primaryTouchId = -1;
            secondaryTouchId = -1;
            previousPrimary = new Point();
            previousSecondary = new Point();
            pinchFramePending = false;
        }
    }
}
