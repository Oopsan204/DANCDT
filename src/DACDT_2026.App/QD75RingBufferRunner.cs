using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DACDT_2026
{
    /// <summary>
    /// Streams positioning data longer than the 600-point QD75 buffer by using
    /// two refill zones and a fixed JUMP command at point No.600.
    /// </summary>
    public sealed class QD75RingBufferRunner
    {
        private const int BufferSize = 600;
        private const int Zone1Offset = 0;      // Point No.1
        private const int Zone1Size = 300;      // Point No.1-No.300
        private const int Zone2Offset = 300;    // Point No.301
        private const int Zone2Size = 299;      // Point No.301-No.599
        private const int JumpPointOffset = 599; // Point No.600
        private const int Md44Axis1 = 835;      // U0\G835: positioning data No. being executed
        private const int PollIntervalMs = 50;

        private readonly PLCCommunication plcComm;
        private readonly List<QD75BufferWriter.PositioningDataRow> allRows;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private int nextRowIndex;
        private int totalPointsLoaded;
        private bool completionRaised;
        private int zone1StartRowIndex;
        private int zone2StartRowIndex;
        private int finalTargetBufferIndex;

        public event Action<int, int> OnProgress;
        public event Action<string> OnLog;
        public event Action OnComplete;
        public event Action<string> OnError;

        public bool IsRunning { get; private set; }

        public QD75RingBufferRunner(PLCCommunication plcComm, List<QD75BufferWriter.PositioningDataRow> allRows)
        {
            this.plcComm = plcComm;
            this.allRows = allRows ?? new List<QD75BufferWriter.PositioningDataRow>();
        }

        public async Task<bool> StartAsync()
        {
            if (IsRunning)
                return true;

            IsRunning = true;
            completionRaised = false;

            try
            {
                if (allRows.Count == 0)
                    throw new InvalidOperationException("No positioning data rows to stream.");

                if (allRows.Count <= BufferSize)
                {
                    await Task.Run(() =>
                    {
                        Log($"Ring buffer not required for {allRows.Count} points.");
                        WriteBufferRange(CloneRows(allRows), Zone1Offset);
                        totalPointsLoaded = allRows.Count;
                        OnProgress?.Invoke(totalPointsLoaded, allRows.Count);
                        RaiseCompleteOnce();
                    }, cts.Token).ConfigureAwait(false);
                    IsRunning = false;
                    return true;
                }

                await Task.Run(() => LoadInitialBuffer(), cts.Token).ConfigureAwait(false);
                _ = Task.Run(() => MonitorAndFinalizeAsync());
                return true;
            }
            catch (OperationCanceledException)
            {
                Log("Ring buffer stopped.");
                IsRunning = false;
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                IsRunning = false;
                return false;
            }
        }

        private async Task MonitorAndFinalizeAsync()
        {
            try
            {
                await MonitorMd44AndRefillAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Log("Ring buffer stopped.");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
            finally
            {
                IsRunning = false;
            }
        }

        public void Stop()
        {
            cts.Cancel();
        }

        private void LoadInitialBuffer()
        {
            nextRowIndex = 0;
            totalPointsLoaded = 0;
            zone1StartRowIndex = 0;
            zone2StartRowIndex = 300;

            WriteNextZone(Zone1Offset, Zone1Size, zoneName: "1-300");
            WriteNextZone(Zone2Offset, Zone2Size, zoneName: "301-599", forceLastBeforeJump: true);
            WriteJumpPoint();

            Log($"Ring buffer started: loaded {totalPointsLoaded}/{allRows.Count} source points, point 600 is fixed JUMP to point 1.");
            OnProgress?.Invoke(totalPointsLoaded, allRows.Count);
        }

        private async Task MonitorMd44AndRefillAsync(CancellationToken ct)
        {
            RefillZone pendingZone = RefillZone.Zone1;

            while (!ct.IsCancellationRequested)
            {
                int md44 = ReadMd44Word();

                if (nextRowIndex >= allRows.Count)
                {
                    // Đã tải hết điểm lên PLC, chờ chạy nốt các điểm còn lại trong buffer
                    bool isFinished = false;
                    if (md44 == 0)
                    {
                        isFinished = true;
                    }
                    else if (finalTargetBufferIndex <= Zone1Size)
                    {
                        // Final batch is in Zone 1 (points 1 to 300)
                        if (md44 >= 1 && md44 <= Zone1Size && md44 >= finalTargetBufferIndex)
                        {
                            isFinished = true;
                        }
                    }
                    else
                    {
                        // Final batch is in Zone 2 (points 301 to 599)
                        if (md44 >= Zone2Offset + 1 && md44 <= BufferSize - 1 && md44 >= finalTargetBufferIndex)
                        {
                            isFinished = true;
                        }
                    }

                    if (isFinished || GetContinuousIndex(md44) >= allRows.Count)
                    {
                        RaiseCompleteOnce();
                        return;
                    }
                }
                else
                {
                    if (pendingZone == RefillZone.Zone1 && IsInZone2(md44))
                    {
                        WriteNextZone(Zone1Offset, Zone1Size, zoneName: "1-300");
                        pendingZone = RefillZone.Zone2;
                    }
                    else if (pendingZone == RefillZone.Zone2 && IsInZone1(md44))
                    {
                        WriteNextZone(Zone2Offset, Zone2Size, zoneName: "301-599", forceLastBeforeJump: true);
                        pendingZone = RefillZone.Zone1;
                    }
                }

                await Task.Delay(PollIntervalMs, ct).ConfigureAwait(false);
            }
        }

        private bool WriteNextZone(int pointOffset, int zoneSize, string zoneName, bool forceLastBeforeJump = false)
        {
            int remaining = allRows.Count - nextRowIndex;
            if (remaining <= 0)
                return false;

            int countToWrite = Math.Min(zoneSize, remaining);
            bool isLastBatch = nextRowIndex + countToWrite >= allRows.Count;
            var batch = CloneRows(allRows.GetRange(nextRowIndex, countToWrite));

            if (pointOffset == Zone1Offset)
            {
                zone1StartRowIndex = nextRowIndex;
            }
            else if (pointOffset == Zone2Offset)
            {
                zone2StartRowIndex = nextRowIndex;
            }

            if (isLastBatch)
            {
                ForceLastRowToEnd(batch);
                finalTargetBufferIndex = pointOffset + countToWrite;
            }
            else if (forceLastBeforeJump)
            {
                ForceLastRowToContinuousPositioning(batch);
            }

            WriteBufferRange(batch, pointOffset);

            nextRowIndex += countToWrite;
            totalPointsLoaded += countToWrite;

            string tail = isLastBatch ? " final batch with END" : string.Empty;
            Log($"Refilled zone {zoneName}: wrote {countToWrite} source points, loaded {totalPointsLoaded}/{allRows.Count}.{tail}");
            OnProgress?.Invoke(totalPointsLoaded, allRows.Count);

            if (isLastBatch)
                RaiseCompleteOnce();

            return true;
        }

        private void WriteBufferRange(List<QD75BufferWriter.PositioningDataRow> rows, int pointOffset)
        {
            int masterBaseG = QD75BufferWriter.ProgramBaseG[0];
            int slaveBaseG = QD75BufferWriter.ProgramBaseG[1];
            int gOffset = pointOffset * QD75BufferWriter.Stride;

            int totalWords = rows.Count * QD75BufferWriter.Stride;
            short[] masterData = new short[totalWords];
            short[] slaveData = new short[totalWords];

            for (int i = 0; i < rows.Count; i++)
                FillPointWords(rows[i], masterData, slaveData, i * QD75BufferWriter.Stride);

            int masterRes = plcComm.WriteBuffer(0, masterBaseG + gOffset, masterData);
            int slaveRes = plcComm.WriteBuffer(0, slaveBaseG + gOffset, slaveData);

            if (masterRes != 0 || slaveRes != 0)
                throw new Exception($"WriteBuffer failed at point offset {pointOffset}: master={masterRes}, slave={slaveRes}");
        }

        private void WriteJumpPoint()
        {
            var jumpRow = new QD75BufferWriter.PositioningDataRow
            {
                MotionType = "JUMP_TO_1",
                EndCoordinate = "0;0",
                CenterCoordinate = string.Empty,
                Speed = "0",
                MCodeValue = "0",
                Dwell = "1"
            };

            WriteBufferRange(new List<QD75BufferWriter.PositioningDataRow> { jumpRow }, JumpPointOffset);
            Log("Point 600 fixed as JUMP_TO_1.");
        }

        private static void FillPointWords(
            QD75BufferWriter.PositioningDataRow row,
            short[] masterData,
            short[] slaveData,
            int blockOffset)
        {
            if (row.MotionType == "JUMP_TO_1")
            {
                short jumpId = QD75BufferWriter.BuildIdentifierWord(
                    QD75BufferWriter.OperationPattern.PositioningComplete,
                    QD75BufferWriter.ControlSystem.JUMP,
                    QD75BufferWriter.PartnerAxis.Axis2);

                masterData[blockOffset + QD75BufferWriter.OffsetMoveCode] = jumpId;
                masterData[blockOffset + QD75BufferWriter.OffsetDwell] = 1;

                slaveData[blockOffset + QD75BufferWriter.OffsetMoveCode] = jumpId;
                slaveData[blockOffset + QD75BufferWriter.OffsetDwell] = 1;
                return;
            }

            short moveCode = QD75BufferWriter.BuildPositioningIdentifierWord(row.MotionType);

            masterData[blockOffset + QD75BufferWriter.OffsetMoveCode] = moveCode;
            masterData[blockOffset + QD75BufferWriter.OffsetMCode] = (short)ParseInt(row.MCodeValue);
            masterData[blockOffset + QD75BufferWriter.OffsetDwell] = (short)ParseInt(row.Dwell);

            int speedVal = ParseInt(row.Speed) * QD75BufferWriter.SpeedMultiplier;
            masterData[blockOffset + QD75BufferWriter.OffsetSpeed] = (short)(speedVal & 0xFFFF);
            masterData[blockOffset + QD75BufferWriter.OffsetSpeed + 1] = (short)((speedVal >> 16) & 0xFFFF);

            int endX = ParseCoord(row.EndCoordinate, 0, row.EndXMm);
            masterData[blockOffset + QD75BufferWriter.OffsetPosX] = (short)(endX & 0xFFFF);
            masterData[blockOffset + QD75BufferWriter.OffsetPosX + 1] = (short)((endX >> 16) & 0xFFFF);

            int centerX = ParseCoord(row.CenterCoordinate, 0, row.CenterXMm);
            masterData[blockOffset + QD75BufferWriter.OffsetCenterX] = (short)(centerX & 0xFFFF);
            masterData[blockOffset + QD75BufferWriter.OffsetCenterX + 1] = (short)((centerX >> 16) & 0xFFFF);

            slaveData[blockOffset + QD75BufferWriter.OffsetMoveCode] = moveCode;

            int endY = ParseCoord(row.EndCoordinate, 1, row.EndYMm);
            slaveData[blockOffset + QD75BufferWriter.OffsetPosX] = (short)(endY & 0xFFFF);
            slaveData[blockOffset + QD75BufferWriter.OffsetPosX + 1] = (short)((endY >> 16) & 0xFFFF);

            int centerY = ParseCoord(row.CenterCoordinate, 1, row.CenterYMm);
            slaveData[blockOffset + QD75BufferWriter.OffsetCenterX] = (short)(centerY & 0xFFFF);
            slaveData[blockOffset + QD75BufferWriter.OffsetCenterX + 1] = (short)((centerY >> 16) & 0xFFFF);
        }

        private int ReadMd44Word()
        {
            try
            {
                int[] values = plcComm.ReadBuffer(0, Md44Axis1, 1);
                return values != null && values.Length > 0 ? values[0] & 0xFFFF : 0;
            }
            catch (Exception ex)
            {
                Log($"Md.44 read failed: {ex.Message}");
                return 0;
            }
        }

        private static bool IsInZone1(int md44)
            => md44 >= 1 && md44 <= Zone1Size;

        private static bool IsInZone2(int md44)
            => md44 >= Zone2Offset + 1 && md44 <= BufferSize - 1;

        private static List<QD75BufferWriter.PositioningDataRow> CloneRows(
            IList<QD75BufferWriter.PositioningDataRow> source)
        {
            var rows = new List<QD75BufferWriter.PositioningDataRow>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var row = source[i];
                rows.Add(new QD75BufferWriter.PositioningDataRow
                {
                    MotionType = row.MotionType,
                    MCodeValue = row.MCodeValue,
                    Dwell = row.Dwell,
                    Speed = row.Speed,
                    EndCoordinate = row.EndCoordinate,
                    CenterCoordinate = row.CenterCoordinate,
                    EndXMm = row.EndXMm,
                    EndYMm = row.EndYMm,
                    CenterXMm = row.CenterXMm,
                    CenterYMm = row.CenterYMm,
                    EndZ = row.EndZ
                });
            }

            return rows;
        }

        private static void ForceLastRowToEnd(List<QD75BufferWriter.PositioningDataRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return;

            var last = rows[rows.Count - 1];
            string motion = last.MotionType ?? string.Empty;
            if (motion.Contains("(End)") || motion.Contains(" (End)"))
                return;

            if (motion.Contains("(Continuous Path)"))
                last.MotionType = motion.Replace("(Continuous Path)", " (End)");
            else if (motion.Contains("(Continuous Positioning)"))
                last.MotionType = motion.Replace("(Continuous Positioning)", " (End)");
            else
                last.MotionType = motion + " (End)";
        }

        private static void ForceLastRowToContinuousPositioning(List<QD75BufferWriter.PositioningDataRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return;

            var last = rows[rows.Count - 1];
            string motion = last.MotionType ?? string.Empty;
            if (motion.Contains("(Continuous Path)"))
                last.MotionType = motion.Replace("(Continuous Path)", "(Continuous Positioning)");
        }

        private static int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            int.TryParse(
                value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out int parsed);
            return parsed;
        }

        private static int ParseCoord(string coordinate, int axis, double fallbackMm)
        {
            if (!string.IsNullOrWhiteSpace(coordinate))
            {
                string[] parts = coordinate.Split(';');
                if (parts.Length > axis &&
                    double.TryParse(
                        parts[axis],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double value))
                {
                    return Convert.ToInt32(Math.Round(value * QD75BufferWriter.CoordinateMultiplier));
                }
            }

            return Convert.ToInt32(Math.Round(fallbackMm * QD75BufferWriter.CoordinateMultiplier));
        }

        private void RaiseCompleteOnce()
        {
            if (completionRaised)
                return;

            completionRaised = true;
            Log($"All {allRows.Count} source points have been loaded into the ring buffer.");
            OnComplete?.Invoke();
        }

        public int GetContinuousIndex(int md44)
        {
            if (allRows.Count <= BufferSize)
            {
                return md44;
            }

            if (md44 <= 0)
                return 0;

            if (md44 >= 1 && md44 <= 300)
            {
                int val = zone1StartRowIndex + md44;
                return Math.Min(val, allRows.Count);
            }
            else if (md44 >= 301 && md44 <= 599)
            {
                int val = zone2StartRowIndex + (md44 - 300);
                return Math.Min(val, allRows.Count);
            }
            else // md44 == 600
            {
                // JUMP command is executing. It represents the point just after the end of Zone 2's loaded data.
                int val = zone2StartRowIndex + 299;
                return Math.Min(val, allRows.Count);
            }
        }

        private void Log(string message)
            => OnLog?.Invoke(message);

        private enum RefillZone
        {
            Zone1,
            Zone2
        }
    }
}
