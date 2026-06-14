using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace DACDT_2026
{
    /// <summary>
    /// Form1 — PLC control handlers: connect/disconnect, jog, go-home,
    /// reset error, start, set jog speed, emergency stop, polling timer.
    /// </summary>
    public partial class Form1
    {
        // ── Connection ───────────────────────────────────────────────────────────
        private async Task HandleConnectToggleAsync(System.Collections.Generic.Dictionary<string, object> payload)
        {
            logicalStation = GetInt(payload, "station", logicalStation);

            if (plcComm != null && plcComm.IsConnected)
            {
                await DisconnectPlcAsync();
                await NotifyAsync("info", "PLC", "Đã ngắt kết nối PLC.");
                await PushControlStateAsync();
                return;
            }

            try
            {
                await DisconnectPlcAsync(false);
                var connectedComm = await Task.Run(() =>
                {
                    var comm = new PLCCommunication(plcIpAddress, plcPort, logicalStation);
                    if (comm.Connect())
                        return comm;

                    comm.Dispose();
                    return null;
                });

                if (connectedComm == null)
                {
                    UpdateConnectionState(false, "PLC disconnected");
                    UpdateIntegrityFault("PLC connection returned an error.");
                    await NotifyAsync("error", "PLC", "PLC connect returned an error.");
                    await PushControlStateAsync();
                    return;
                }

                plcComm = connectedComm;
                UpdateConnectionState(true, "PLC connected");
                UpdateIntegrityState(true);
                StartPlcPolling();
                await PushControlStateAsync();
                await NotifyAsync("success", "PLC", "PLC connected successfully.");
            }
            catch (Exception ex)
            {
                UpdateConnectionState(false, "PLC disconnected");
                UpdateIntegrityFault(ex.Message);
                await PushControlStateAsync();
                await NotifyAsync("error", "PLC", ex.Message);
            }
        }

        private void DisconnectPlc(bool updateUi = true)
        {
            StopPlcPolling();

            if (plcComm != null)
            {
                try { plcComm.Dispose(); } catch { }
                plcComm = null;
            }

            foreach (var row in monitorRows)
                row.Status = "Disconnected";

            if (updateUi)
            {
                UpdateConnectionState(false, "PLC disconnected");
                UpdateIntegrityState(false);
            }
        }

        private async Task DisconnectPlcAsync(bool updateUi = true)
        {
            await StopPlcPollingAsync();

            var comm = plcComm;
            plcComm = null;
            if (comm != null)
            {
                try { await Task.Run(() => comm.Dispose()); } catch { }
            }

            foreach (var row in monitorRows)
                row.Status = "Disconnected";

            if (updateUi)
            {
                UpdateConnectionState(false, "PLC disconnected");
                UpdateIntegrityState(false);
            }
        }

        private readonly SemaphoreSlim speedChangeSemaphore = new SemaphoreSlim(1, 1);
        private DateTime lastSpeedChangeTime = DateTime.MinValue;

        private async Task<bool> ExecuteAxis4SpeedChangeAsync(int speedValue, string logContext)
        {
            var comm = plcComm;
            if (comm == null || !comm.IsConnected)
                return false;

            await speedChangeSemaphore.WaitAsync();
            try
            {
                var now = DateTime.UtcNow;
                var elapsedMs = (now - lastSpeedChangeTime).TotalMilliseconds;
                if (elapsedMs < 100)
                {
                    int delayNeeded = 100 - (int)elapsedMs;
                    await Task.Delay(delayNeeded);
                }
                lastSpeedChangeTime = DateTime.UtcNow;

                bool isAxis4Running = false;
                try
                {
                    // Read Md.26 for Axis 4 (MonitorBaseG[3] + OffAxisStatus = 1100 + 9 = 1109)
                    int[] statusData = comm.ReadBuffer(0, 1109, 1);
                    if (statusData != null && statusData.Length > 0)
                    {
                        int status = statusData[0] & 0xFFFF;
                        isAxis4Running = (status > 1);
                    }
                }
                catch
                {
                    // Fallback to polled status if direct read fails
                    isAxis4Running = (axAxisStatus[3] > 1);
                }

                await Task.Run(() =>
                {
                    string used;
                    // Step 1: Write 32-bit speed/power value to Cd.17 (JOG speed - address 1818)
                    comm.WriteDeviceValue("U0\\G1818", speedValue);

                    if (isAxis4Running)
                    {
                        // Step 2: Disable accel/decel time changes by writing 0 to Cd.12 (address 1812)
                        comm.WriteInt16ToDevicePath("U0\\G1812", 0, out used);

                        // Step 3: Write 32-bit new speed value to Cd.14 (address 1814)
                        comm.WriteDeviceValue("U0\\G1814", speedValue);

                        // Step 4: Trigger speed change by writing 1 to Cd.15 (address 1816)
                        comm.WriteInt16ToDevicePath("U0\\G1816", 1, out used);
                    }
                    else
                    {
                        // Safe state: clear speed change request flag if the axis is stopped to prevent stuck Cd.15
                        comm.WriteInt16ToDevicePath("U0\\G1816", 0, out used);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                UpdateIntegrityFault(ex.Message);
                AddLogEntry("U0\\G1812..G1818", speedValue.ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message + " (" + logContext + ")");
                return false;
            }
            finally
            {
                speedChangeSemaphore.Release();
            }
        }

        // ── Velocity ─────────────────────────────────────────────────────────────
        private async Task HandleSetVelocityAsync(int value)
        {
            try
            {
                if (value < 0) value = 0;

                if (plcComm == null || !plcComm.IsConnected)
                {
                    await NotifyAsync("error", "Velocity", "PLC chưa kết nối.");
                    return;
                }

                bool success = await ExecuteAxis4SpeedChangeAsync(value, "Set Velocity");
                
                if (success)
                {
                    AddLogEntry("U0\\G1812..G1816", value.ToString(CultureInfo.InvariantCulture), "Write", "OK", "Set Axis 4 Speed via Cd.14");
                    await NotifyAsync("success", "Velocity", $"Đã đặt tốc độ trục 4: {value} (Cd.14 = {value})");
                }
                else
                {
                    await NotifyAsync("error", "Velocity", "Không thể ghi tốc độ trục 4.");
                }
                await PushControlStateAsync();
            }
            catch (Exception ex)
            {
                await NotifyAsync("error", "Velocity", "Lỗi ghi tốc độ trục 4: " + ex.Message);
            }
        }

        // ── Jog ─────────────────────────────────────────────────────────────────
        private async Task HandleJogWriteAsync(int offset, bool active)
        {
            if (offset < 0) return;

            try
            {
                string register = GetSequentialDevice(JogBaseRegister, offset);
                int v = active ? 1 : 0;
                await WriteDeviceValueAsync(register, v);
                UpdateIntegrityState(true);
                AddLogEntry(register, v.ToString(CultureInfo.InvariantCulture), "Write", "OK", "Jog");

                if (active)
                {
                    string dir = "Unknown";
                    switch (offset)
                    {
                        case 0: dir = "Right (X+)"; break;
                        case 1: dir = "Left (X-)";  break;
                        case 2: dir = "Up (Y+)";    break;
                        case 3: dir = "Down (Y-)";  break;
                        case 4: dir = "Z+";         break;
                        case 5: dir = "Z-";         break;
                    }
                    await NotifyAsync("info", "Jog", $"Started Jog {dir} ({register})");
                }
            }
            catch (Exception ex)
            {
                if (active)
                {
                    UpdateIntegrityFault(ex.Message);
                    AddLogEntry(JogBaseRegister, (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                    await NotifyAsync("error", "Jog", ex.Message);
                    await PushControlStateAsync();
                }
            }
        }

        // ── Go Home ──────────────────────────────────────────────────────────────
        private async Task HandleGoHomeWriteAsync(bool active)
        {
            try
            {
                int v = active ? 1 : 0;
                await WriteDeviceValueAsync("M503", v);
                UpdateIntegrityState(true);
                AddLogEntry("M503", v.ToString(CultureInfo.InvariantCulture), "Write", "OK", "GoHome");
            }
            catch (Exception ex)
            {
                if (active)
                {
                    UpdateIntegrityFault(ex.Message);
                    AddLogEntry("M503", (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                    await NotifyAsync("error", "Go Home", ex.Message);
                    await PushControlStateAsync();
                }
            }
        }

        // ── Home All ─────────────────────────────────────────────────────────────
        private async Task HandleHomeAllWriteAsync(bool active)
        {
            try
            {
                int v = active ? 1 : 0;
                await WriteDeviceValueAsync("M502", v);
                UpdateIntegrityState(true);
                AddLogEntry("M502", v.ToString(CultureInfo.InvariantCulture), "Write", "OK", "HomeAll");
            }
            catch (Exception ex)
            {
                if (active)
                {
                    UpdateIntegrityFault(ex.Message);
                    AddLogEntry("M502", (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                    await NotifyAsync("error", "Home All", ex.Message);
                    await PushControlStateAsync();
                }
            }
        }

        // ── Reset Error ──────────────────────────────────────────────────────────
        private async Task HandleResetErrorWriteAsync(bool active)
        {
            try
            {
                int v = active ? 1 : 0;
                await WriteDeviceValueAsync("M300", v);
                UpdateIntegrityState(true);
                AddLogEntry("M300", v.ToString(CultureInfo.InvariantCulture), "Write", "OK", "ResetError");
            }
            catch (Exception ex)
            {
                if (active)
                {
                    UpdateIntegrityFault(ex.Message);
                    AddLogEntry("M300", (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                    await NotifyAsync("error", "Reset Error", ex.Message);
                    await PushControlStateAsync();
                }
            }
        }

        // ── Start ────────────────────────────────────────────────────────────────
        private async Task HandleStartWriteAsync(bool active)
        {
            if (!active)
                return;

            try
            {
                // Rebuild drawing process rows from active CAD/G-code document if loaded, to clear any Test Area data
                if (activeCadDocument != null)
                {
                    var drawingRows = BuildConnectedPathsFromCad();
                    drawingRows = PostProcessCompiledRows(drawingRows);
                    if (drawingRows != null && drawingRows.Count > 0)
                    {
                        processRows.Clear();
                        processRows.AddRange(drawingRows);
                        await PushDxfStateAsync();
                    }
                }

                // Tự động gửi dữ liệu xuống PLC trước khi chạy
                bool sendOk = await HandleSendCadXAsync();
                if (!sendOk)
                {
                    return; // Gửi không thành công (lỗi kết nối hoặc không có dữ liệu) -> Dừng, không chạy
                }

                await WriteDeviceValueAsync("M2000", 1);
                UpdateIntegrityState(true);
                AddLogEntry("M2000", "1", "Write", "OK", "Start");
                await Task.Delay(100);
                await WriteDeviceValueAsync("M2000", 0);
                AddLogEntry("M2000", "0", "Write", "OK", "Start reset");
            }
            catch (Exception ex)
            {
                try
                {
                    await WriteDeviceValueAsync("M2000", 0);
                }
                catch
                {
                }

                UpdateIntegrityFault(ex.Message);
                AddLogEntry("M2000", "1", "Write", "Error", ex.Message);
                await NotifyAsync("error", "Start", ex.Message);
                await PushControlStateAsync();
            }
        }

        // ── Jog Speed ────────────────────────────────────────────────────────────
        private async Task HandleContinueWriteAsync(bool active)
        {
            if (!active)
                return;

            int activeIdx = GetActiveProgramIndex();
            if (processRows.Count == 0 || activeIdx <= 0 || activeIdx > processRows.Count)
            {
                await NotifyAsync("warning", "Continue", "Không có tọa độ hoặc máy không ở trạng thái đang chạy.");
                return;
            }

            try
            {
                await WriteDeviceValueAsync(ContinueRegister, 1);
                UpdateIntegrityState(true);
                AddLogEntry(ContinueRegister, "1", "Write", "OK", "Continue");
                await Task.Delay(100);
                await WriteDeviceValueAsync(ContinueRegister, 0);
                AddLogEntry(ContinueRegister, "0", "Write", "OK", "Continue reset");
            }
            catch (Exception ex)
            {
                try
                {
                    await WriteDeviceValueAsync(ContinueRegister, 0);
                }
                catch
                {
                }

                UpdateIntegrityFault(ex.Message);
                AddLogEntry(ContinueRegister, "1", "Write", "Error", ex.Message);
                await NotifyAsync("error", "Continue", ex.Message);
                await PushControlStateAsync();
            }
        }

        private async Task HandlePauseWriteAsync(bool active)
        {
            if (!active)
                return;

            int activeIdx = GetActiveProgramIndex();
            if (processRows.Count == 0 || activeIdx <= 0 || activeIdx > processRows.Count)
            {
                await NotifyAsync("warning", "Pause", "Không có tọa độ hoặc máy không ở trạng thái đang chạy.");
                return;
            }

            try
            {
                await WriteDeviceValueAsync(PauseRegister, 1);
                UpdateIntegrityState(true);
                AddLogEntry(PauseRegister, "1", "Write", "OK", "Pause");
                await Task.Delay(100);
                await WriteDeviceValueAsync(PauseRegister, 0);
                AddLogEntry(PauseRegister, "0", "Write", "OK", "Pause reset");
            }
            catch (Exception ex)
            {
                try
                {
                    await WriteDeviceValueAsync(PauseRegister, 0);
                }
                catch
                {
                }

                UpdateIntegrityFault(ex.Message);
                AddLogEntry(PauseRegister, "1", "Write", "Error", ex.Message);
                await NotifyAsync("error", "Pause", ex.Message);
                await PushControlStateAsync();
            }
        }

        private async Task HandleStopRunAsync()
        {
            if (plcComm == null || !plcComm.IsConnected)
            {
                await NotifyAsync("error", "Stop", "PLC is not connected.");
                return;
            }

            activeRingRunner?.Stop();
            await StopPlcPollingAsync();

            try
            {
                await LogUIAsync("Stop", "Stopping run and clearing PLC positioning buffers...");

                // Step 1: Activate hardware stop signals (Y4, Y5, Y6, Y7) and StopRunRegister (M212)
                await Task.WhenAll(
                    WriteDeviceValueAsync("Y4", 1),
                    WriteDeviceValueAsync("Y5", 1),
                    WriteDeviceValueAsync("Y6", 1),
                    WriteDeviceValueAsync("Y7", 1),
                    WriteDeviceValueAsync(StopRunRegister, 1)
                );
                AddLogEntry("Y4", "1", "Write", "OK", "Stop Axis 1");
                AddLogEntry("Y5", "1", "Write", "OK", "Stop Axis 2");
                AddLogEntry("Y6", "1", "Write", "OK", "Stop Axis 3");
                AddLogEntry("Y7", "1", "Write", "OK", "Stop Axis 4");
                AddLogEntry(StopRunRegister, "1", "Write", "OK", "Stop");

                await Task.Delay(100);

                // Step 2: Clear positioning buffers
                var clearResult = await Task.Run(() => QD75BufferWriter.ClearAllBuffers(plcComm, maxPoints: 600));
                foreach (var wr in clearResult.WriteResults)
                {
                    AddLogEntry(wr.Address, wr.Value, "Clear", wr.Status, wr.Message);
                    if (!wr.Status.StartsWith("OK"))
                        await NotifyAsync("warning", "Stop", $"{wr.Address}: {wr.Message}");
                }

                // Step 3: Deactivate hardware stop signals (Y4, Y5, Y6, Y7) and StopRunRegister (M212) to release lock
                await Task.WhenAll(
                    WriteDeviceValueAsync("Y4", 0),
                    WriteDeviceValueAsync("Y5", 0),
                    WriteDeviceValueAsync("Y6", 0),
                    WriteDeviceValueAsync("Y7", 0),
                    WriteDeviceValueAsync(StopRunRegister, 0)
                );
                AddLogEntry("Y4", "0", "Write", "OK", "Stop release Axis 1");
                AddLogEntry("Y5", "0", "Write", "OK", "Stop release Axis 2");
                AddLogEntry("Y6", "0", "Write", "OK", "Stop release Axis 3");
                AddLogEntry("Y7", "0", "Write", "OK", "Stop release Axis 4");
                AddLogEntry(StopRunRegister, "0", "Write", "OK", "Stop reset");

                if (clearResult.Success)
                {
                    ui.IsStartActionEnabled = processRows.Count > 0;
                    UpdateIntegrityFault("Run stopped and buffer cleared");
                    await NotifyAsync("success", "Stop", "Đã Stop và xoá buffer tọa độ/lệnh chạy.");
                }
                else
                {
                    UpdateIntegrityFault(clearResult.ErrorMessage);
                    await NotifyAsync("error", "Stop", "Stop đã gửi, nhưng xoá buffer chưa hoàn tất. Kiểm tra log.");
                }

                await PushControlStateAsync();
            }
            catch (Exception ex)
            {
                try
                {
                    await Task.WhenAll(
                        WriteDeviceValueAsync("Y4", 0),
                        WriteDeviceValueAsync("Y5", 0),
                        WriteDeviceValueAsync("Y6", 0),
                        WriteDeviceValueAsync("Y7", 0),
                        WriteDeviceValueAsync(StopRunRegister, 0)
                    );
                    AddLogEntry("Y4-Y7, " + StopRunRegister, "0", "Write", "OK", "Stop signals reset after error");
                }
                catch
                {
                }

                UpdateIntegrityFault(ex.Message);
                AddLogEntry(StopRunRegister, "1", "Write", "Error", ex.Message);
                await PushControlStateAsync();
                await NotifyAsync("error", "Stop", ex.Message);
            }
            finally
            {
                _ = SendProgressAsync(false, 0);
                if (plcComm != null && plcComm.IsConnected && !isClosing)
                    StartPlcPolling();
            }
        }

        private async Task HandleSetJogSpeedAsync(double value)
        {
            try
            {
                float fVal = (float)value;
                byte[] bytes = BitConverter.GetBytes(fVal);
                int intVal = BitConverter.ToInt32(bytes, 0);
                await WriteDeviceValueAsync("D406", intVal);
                AddLogEntry("D406", value.ToString("F3", CultureInfo.InvariantCulture), "Write", "OK", "SetJogSpeed(mm/min)");
                await NotifyAsync("success", "Settings", $"Updated Jog speed: {value:F3} mm/min (D406)");
            }
            catch (Exception ex)
            {
                await NotifyAsync("error", "Settings", "Error updating Jog speed: " + ex.Message);
            }
        }

        private async Task HandleSetLaserPowerAsync(double value)
        {
            try
            {
                // UI inputs percentage (0-100%). Map it to PLC value (450-2000).
                int plcValue = (int)Math.Round(450.0 + (value / 100.0) * (2000.0 - 450.0));
                if (plcValue < 450) plcValue = 450;
                if (plcValue > 2000) plcValue = 2000;

                laserPower = value.ToString("0.##", CultureInfo.InvariantCulture);
                ui.LaserPowerInput = laserPower;
                SaveSettingsToFile();

                if (plcComm == null || !plcComm.IsConnected)
                {
                    await NotifyAsync("success", "Laser Power", $"Đã lưu cục bộ: Công suất laze = {value:0.##}% (PLC chưa kết nối, giá trị map PLC = {plcValue}).");
                    return;
                }

                bool success = await ExecuteAxis4SpeedChangeAsync(plcValue, "Set Laser Power");

                if (success)
                {
                    AddLogEntry("U0\\G1812..G1816", plcValue.ToString(CultureInfo.InvariantCulture), "Write", "OK", $"Set Laser Power: {value:0.##}% mapped to PLC speed change value {plcValue}");
                    await NotifyAsync("success", "Laser Power", $"Đã đặt công suất laze: {value:0.##}% (Cd.14 = {plcValue})");
                }
                else
                {
                    await NotifyAsync("error", "Laser Power", "Lỗi ghi công suất laze.");
                }
            }
            catch (Exception ex)
            {
                await NotifyAsync("error", "Laser Power", "Lỗi ghi công suất laze: " + ex.Message);
            }
        }

        // ── Emergency Stop ───────────────────────────────────────────────────────
        private async Task HandleEmergencyStopAsync()
        {
            try
            {
                await WriteDeviceValueAsync(EmergencyStopRegister, 1);
                AddLogEntry(EmergencyStopRegister, "1", "Write", "OK", "EmergencyStop");
                UpdateIntegrityFault("Emergency stop triggered");
                await PushControlStateAsync();
                await NotifyAsync("error", "PLC", "Emergency stop written to " + EmergencyStopRegister + ".");
            }
            catch (Exception ex)
            {
                UpdateIntegrityFault(ex.Message);
                AddLogEntry(EmergencyStopRegister, "1", "Write", "Error", ex.Message);
                await PushControlStateAsync();
                await NotifyAsync("error", "PLC", ex.Message);
            }
        }

        // ── Poll Timer ───────────────────────────────────────────────────────────
        private void StartPlcPolling()
        {
            if (isClosing)
                return;

            lock (plcPollSync)
            {
                if (plcPollCts != null && !plcPollCts.IsCancellationRequested)
                    return;

                plcPollCts = new CancellationTokenSource();
                plcPollTask = Task.Run(() => PlcPollLoopAsync(plcPollCts.Token));
            }
        }

        private void StopPlcPolling()
        {
            lock (plcPollSync)
            {
                if (plcPollCts == null)
                    return;

                plcPollCts.Cancel();
                plcPollCts = null;
                plcPollTask = null;
            }
        }

        private async Task StopPlcPollingAsync()
        {
            CancellationTokenSource cts;
            Task task;

            lock (plcPollSync)
            {
                cts = plcPollCts;
                task = plcPollTask;
                if (cts == null)
                    return;

                cts.Cancel();
                plcPollCts = null;
                plcPollTask = null;
            }

            try
            {
                if (task != null)
                    await task;
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
            finally
            {
                cts.Dispose();
            }
        }

        private async Task PlcPollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && !isClosing)
            {
                var elapsed = Stopwatch.StartNew();
                try
                {
                    await PollPlcOnceAsync(token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!isClosing)
                        UpdateIntegrityFault(ex.Message);
                }

                int delayMs = Math.Max(10, PlcPollIntervalMs - (int)elapsed.ElapsedMilliseconds);
                await Task.Delay(delayMs, token);
            }
        }

        private async Task PollPlcOnceAsync(CancellationToken token)
        {
            if (isClosing || isPolling)
                return;

            var comm = plcComm;
            if (comm == null || !comm.IsConnected)
                return;

            isPolling = true;
            try
            {
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < 4; i++)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        int dBase = i * 10;
                        int[] posData = comm.ReadDeviceRange($"D{dBase}", 2);
                        axCurrentPos[i] = (posData[1] << 16) | (posData[0] & 0xFFFF);

                        int[] speedData = comm.ReadDeviceRange($"D{dBase + 4}", 2);
                        axCurrentSpeed[i] = (speedData[1] << 16) | (speedData[0] & 0xFFFF);

                        try
                        {
                            int[] mcodeData = comm.ReadDeviceRange($"D{dBase + 104}", 1);
                            axMCode[i] = mcodeData[0];
                        }
                        catch
                        {
                            axMCode[i] = 0;
                        }

                        int[] mon = comm.ReadBuffer(0, MonitorBaseG[i], 38);
                        axErrorCode[i] = mon[OffErrorCode];
                        axWarningCode[i] = mon[OffWarningCode];
                        axAxisStatus[i] = mon[OffAxisStatus];
                        axSignals[i] = mon[16];
                        axCurrentDataNo[i] = mon[35];
                        axLastDataNo[i] = mon[37];

                        int[] ctl = comm.ReadBuffer(0, ControlBaseG[i], 20);
                        axErrorReset[i] = ctl[OffErrorReset];
                        axNewSpeed[i] = (ctl[OffNewSpeed + 1] << 16) | (ctl[OffNewSpeed] & 0xFFFF);
                    }
                    catch
                    {
                    }
                }

                try
                {
                    int[] d406Raw = comm.ReadDeviceRange("D406", 2);
                    byte[] bytes = BitConverter.GetBytes((d406Raw[1] << 16) | (d406Raw[0] & 0xFFFF));
                    currentJogSpeedD406 = BitConverter.ToSingle(bytes, 0);
                }
                catch
                {
                }

                foreach (var row in monitorRows)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        row.Value = comm.ReadDeviceValue(row.Register).ToString(CultureInfo.InvariantCulture);
                        row.Status = "OK";
                    }
                    catch (Exception ex)
                    {
                        row.Status = ex.Message;
                    }
                }

                token.ThrowIfCancellationRequested();
                UpdateIntegrityState(true);
                await PushControlStateAsync();

                var nowUtc = DateTime.UtcNow;
                if ((nowUtc - lastMachineMqttPublishUtc).TotalMilliseconds >= MachineMqttPublishIntervalMs)
                {
                    lastMachineMqttPublishUtc = nowUtc;
                    _ = Task.Run(() => PublishMachineStateToMqttAsync(connected: true));
                }

                if (currentView == "telemetry")
                    await PushTelemetryStateAsync();
            }
            finally
            {
                isPolling = false;
            }
        }
        // ── Shared helpers ───────────────────────────────────────────────────────
        private Task WriteDeviceValueAsync(string deviceName, int value)
        {
            return Task.Run(() =>
            {
                var comm = plcComm;
                if (comm == null || !comm.IsConnected)
                    throw new InvalidOperationException("PLC is not connected.");

                comm.WriteDeviceValue(deviceName, value);
            });
        }

        private void EnsureConnected()
        {
            if (plcComm == null || !plcComm.IsConnected)
                throw new InvalidOperationException("PLC is not connected.");
        }

        private void UpdateConnectionState(bool connected, string bannerText)
            => connectionBanner = bannerText;

        private void UpdateIntegrityState(bool connected)
        {
            integrityState  = connected ? "READY" : "IDLE";
            integrityDetail = connected ? "RUN"   : "STOP";
            integrityTone   = connected ? "ready" : "idle";
        }

        private void UpdateIntegrityFault(string errorMessage)
        {
            integrityState  = "FAULT";
            integrityDetail = string.IsNullOrWhiteSpace(errorMessage) ? "PLC error" : errorMessage;
            integrityTone   = "fault";
        }

        private static string GetSequentialDevice(string baseDevice, int offset)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                baseDevice, @"^(?<prefix>[A-Za-z]+)(?<address>\d+)$");
            if (!match.Success)
                throw new InvalidOperationException("Invalid base device: " + baseDevice);

            string prefix  = match.Groups["prefix"].Value;
            int    address = int.Parse(match.Groups["address"].Value, CultureInfo.InvariantCulture);
            return prefix + (address + offset).ToString(CultureInfo.InvariantCulture);
        }
    }
}
