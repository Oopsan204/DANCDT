using System;
using System.Threading;

namespace DACDT_2026
{
    public static class PerformanceTuning
    {
        public const int PlcPollIntervalMs = 10;
        public const int PlcPollMinimumDelayMs = 1;
        public const int PlcHeartbeatIntervalMs = 500;
        public const int SlowPlcMonitorPollIntervalMs = 1000;
        public const int CameraPreviewIntervalMs = 100;
        public const int CameraRecordingFrameIntervalMs = 100;
        public const int WebRtcFrameIntervalMs = 66;
        public const int ControlUiPushIntervalMs = 16;
        public const int ControlTrackingUiPushIntervalMs = 16;
        public const int MachineMqttPublishIntervalMs = 1000;
        public const int ExitStopPulseMs = 150;
        public const int ExitStopDelayMs = 500;
        public const int ExitHomePulseMs = 150;
        public const int ExitHomeDelayMs = 500;
        public const int LogUiDebounceMs = 200;
    }

    public sealed class IntervalGate
    {
        private readonly long intervalTicks;
        private long lastTicks;

        public IntervalGate(int intervalMilliseconds)
        {
            if (intervalMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds));

            intervalTicks = TimeSpan.FromMilliseconds(intervalMilliseconds).Ticks;
        }

        public bool TryEnter(DateTime utcNow)
        {
            long nowTicks = utcNow.Ticks;

            while (true)
            {
                long previousTicks = Interlocked.Read(ref lastTicks);
                if (previousTicks != 0 && nowTicks - previousTicks < intervalTicks)
                    return false;

                if (Interlocked.CompareExchange(ref lastTicks, nowTicks, previousTicks) == previousTicks)
                    return true;
            }
        }

        public void Reset()
        {
            Interlocked.Exchange(ref lastTicks, 0);
        }
    }

    public sealed class SingleFlightGate
    {
        private int isBusy;

        public bool TryEnter()
        {
            return Interlocked.CompareExchange(ref isBusy, 1, 0) == 0;
        }

        public void Exit()
        {
            Interlocked.Exchange(ref isBusy, 0);
        }
    }
}
