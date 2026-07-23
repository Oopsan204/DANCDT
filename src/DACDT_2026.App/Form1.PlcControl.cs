using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        private bool HasEngraveCutProcessRows()
        {
            return processRows.Any(row =>
                string.Equals(row.ProcessKind, EngraveCutProcessComposer.EngraveKind, StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.ProcessKind, EngraveCutProcessComposer.CutKind, StringComparison.OrdinalIgnoreCase));
        }

        // ── Connection ───────────────────────────────────────────────────────────
        private async Task HandleConnectToggleAsync(System.Collections.Generic.Dictionary<string, object> payload)
        {
            if (Interlocked.CompareExchange(ref plcConnectionChangeInFlight, 1, 0) != 0)
                return;

            try
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
                plcStartupReady = false;
                UpdateConnectionState(true, "PLC connected - preparing buffers");
                UpdateIntegrityState(true);
                await PushControlStateAsync();

                PLCCommunication monitorComm = null;
                try
                {
                    monitorComm = await Task.Run(() =>
                    {
                        var comm = new PLCCommunication(plcIpAddress, plcPort, logicalStation);
                        if (comm.Connect())
                            return comm;

                        comm.Dispose();
                        return null;
                    });
                }
                catch
                {
                    try { monitorComm?.Dispose(); } catch { }
                    monitorComm = null;
                }

                if (!ReferenceEquals(plcComm, connectedComm) || isClosing)
                {
                    try { monitorComm?.Dispose(); } catch { }
                    return;
                }

                if (monitorComm == null)
                {
                    await DisconnectPlcAsync(false);
                    UpdateConnectionState(false, "PLC disconnected");
                    UpdateIntegrityFault("Dedicated PLC monitor connection failed.");
                    await PushControlStateAsync();
                    await NotifyAsync("error", "PLC", "Dedicated PLC monitor connection failed.");
                    return;
                }

                plcMonitorComm = monitorComm;
                StartPlcPolling();

                QD75BufferWriter.SendResult startupClearResult;
                Interlocked.Increment(ref plcWriteInFlight);
                try
                {
                    startupClearResult = await Task.Run(() =>
                        QD75BufferWriter.ClearAllBuffers(connectedComm, maxPoints: 600));
                }
                finally
                {
                    Interlocked.Decrement(ref plcWriteInFlight);
                }
                foreach (var wr in startupClearResult.WriteResults)
                {
                    AddLogEntry(wr.Address, wr.Value, "Startup clear", wr.Status, wr.Message);
                }

                if (!ReferenceEquals(plcComm, connectedComm) || isClosing)
                    return;

                if (!startupClearResult.Success)
                {
                    await DisconnectPlcAsync(false);
                    UpdateConnectionState(false, "PLC disconnected");
                    UpdateIntegrityFault("Startup PLC buffer clear failed: " + startupClearResult.ErrorMessage);
                    await NotifyAsync("error", "PLC", "PLC buffer clear failed. Connection was closed for safety.");
                    await PushControlStateAsync();
                    return;
                }

                plcStartupReady = true;
                UpdateConnectionState(true, "PLC connected");
                await PushControlStateAsync();
                await NotifyAsync("success", "PLC", "PLC connected successfully.");
            }
            catch (Exception ex)
            {
                try { await DisconnectPlcAsync(false); } catch { }
                UpdateConnectionState(false, "PLC disconnected");
                UpdateIntegrityFault(ex.Message);
                await PushControlStateAsync();
                await NotifyAsync("error", "PLC", ex.Message);
            }
            }
            finally
            {
                Interlocked.Exchange(ref plcConnectionChangeInFlight, 0);
            }
        }

        private void DisconnectPlc(bool updateUi = true)
        {
            StopPlcPolling();

            var comm = plcComm;
            var monitorComm = plcMonitorComm;
            plcStartupReady = false;
            plcComm = null;
            plcMonitorComm = null;

            if (comm != null)
            {
                try { if (comm.IsConnected) comm.WriteDeviceValue(HeartbeatRegister, 0); } catch { }
                try { comm.Dispose(); } catch { }
            }
            try { monitorComm?.Dispose(); } catch { }

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
            isProgramRunning = false;
            programRunCompletionTracker.Reset();
            await StopPlcPollingAsync();

            var comm = plcComm;
            var monitorComm = plcMonitorComm;
            plcStartupReady = false;
            plcComm = null;
            plcMonitorComm = null;
            if (comm != null)
            {
                try { if (comm.IsConnected) await Task.Run(() => comm.WriteDeviceValue(HeartbeatRegister, 0)); } catch { }
                try { await Task.Run(() => comm.Dispose()); } catch { }
            }
            if (monitorComm != null)
            {
                try { await Task.Run(() => monitorComm.Dispose()); } catch { }
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
            PLCCommunication comm;
            if (!TryGetConnectedPlc(out comm))
                return false;

            await speedChangeSemaphore.WaitAsync();
            try
            {
                Interlocked.Increment(ref plcWriteInFlight);

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
                Interlocked.Decrement(ref plcWriteInFlight);
                speedChangeSemaphore.Release();
            }
        }

        // ── Velocity ─────────────────────────────────────────────────────────────
        private async Task HandleSetVelocityAsync(int value)
        {
            try
            {
                if (value < 0) value = 0;

                if (!await RequirePlcConnectedAsync("Velocity"))
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
            if (!await RequirePlcConnectedAsync("Jog"))
                return;

            try
            {
                string register = GetSequentialDevice(JogBaseRegister, offset);
                if (active)
                    ui.RunProgressVisible = false;

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
                UpdateIntegrityFault(ex.Message);
                AddLogEntry(GetSequentialDevice(JogBaseRegister, offset), (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                await NotifyAsync("error", "Jog", active ? ex.Message : "Failed to release Jog: " + ex.Message);
                await PushControlStateAsync();
            }
        }

        // ── Go Home ──────────────────────────────────────────────────────────────
        private async Task HandleGoHomeWriteAsync(bool active)
        {
            if (!await RequirePlcConnectedAsync("Go Home"))
                return;

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
            if (!await RequirePlcConnectedAsync("Home All"))
                return;

            try
            {
                if (active)
                    ui.RunProgressVisible = false;

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
            if (!await RequirePlcConnectedAsync("Reset Error"))
                return;

            try
            {
                int v = active ? 1 : 0;
                await WriteDeviceValueAsync("M300", v);
                if (active)
                {
                    try
                    {
                        await WriteDeviceValueAsync("D104", 0);
                        await WriteDeviceValueAsync("D114", 0);
                        await WriteDeviceValueAsync("D124", 0);
                    }
                    catch { }
                }
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
            if (!await TryEnterProgramCommandAsync("Start"))
                return;

            try
            {
                if (IsProgramRunning())
                {
                    await NotifyAsync("info", "Start", "Wait for the current program to finish before starting a new program.");
                    return;
                }
                if (!await RequirePlcConnectedAsync("Start"))
                    return;
                if (!await RequirePlcStartupReadyAsync("Start"))
                    return;

                try
                {
                if (isMixedEngraveCutProgram)
                {
                    await HandleMixedEngraveCutStartAsync();
                    return;
                }

                // Rebuild drawing process rows from active CAD/G-code document if loaded, to clear any Test Area data
                if (activeCadDocument != null && !isMixedEngraveCutProgram)
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

                ui.RunProgressVisible = HasEngraveCutProcessRows();
                programRunCompletionTracker.Begin();
                await WriteDeviceValueAsync("M2000", 1);
                isProgramRunning = true;
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
            finally
            {
                programCommandGate.Release();
            }
        }

        // ── Jog Speed ────────────────────────────────────────────────────────────
        private async Task HandleMixedEngraveCutStartAsync()
        {
            await EnsureCadProgramCurrentAsync();

            var allRows = processRows.ToList();
            if (allRows.Count == 0)
            {
                await NotifyAsync("info", "Start", "No points to send.");
                return;
            }

            SyncEngraveCutSettingsFromUi();

            bool hasEngraveRows = allRows.Any(row => string.Equals(row.ProcessKind, EngraveCutProcessComposer.EngraveKind, StringComparison.OrdinalIgnoreCase));
            bool hasCutRows = allRows.Any(row => string.Equals(row.ProcessKind, EngraveCutProcessComposer.CutKind, StringComparison.OrdinalIgnoreCase));

            double ignoredCutPower;
            if (hasEngraveRows && hasCutRows && !DecimalInputParser.TryParseFlexibleDouble(cutPower, out ignoredCutPower))
            {
                await NotifyAsync("error", "Laser Power", $"Cat power khong hop le: {cutPower}");
                return;
            }

            string startPower = hasEngraveRows ? engravePower : cutPower;
            string startPhaseName = hasEngraveRows ? "Khac" : "Cat";
            if (!await SetMixedLaserPowerAsync(startPhaseName, startPower))
                return;

            bool sendOk = await HandleSendCadXAsync();
            if (!sendOk)
                return;

            ui.RunProgressVisible = HasEngraveCutProcessRows();
            int cutPowerSwitchIndex = 0;
            if (hasEngraveRows && hasCutRows
                && EngraveCutProcessComposer.TryGetFirstCutRowIndex(allRows.Select(row => row.ProcessKind), out int firstCutIndex))
            {
                cutPowerSwitchIndex = EngraveCutProcessComposer.GetCutPowerSwitchMonitorIndex(firstCutIndex);
            }

            axCurrentDataNo[0] = 0;
            programRunCompletionTracker.Begin();
            await WriteDeviceValueAsync("M2000", 1);
            isProgramRunning = true;
            UpdateIntegrityState(true);
            AddLogEntry("M2000", "1", "Write", "OK", "Start Khac+Cat");
            await Task.Delay(100);
            await WriteDeviceValueAsync("M2000", 0);
            AddLogEntry("M2000", "0", "Write", "OK", "Start reset Khac+Cat");

            if (cutPowerSwitchIndex > 0)
                _ = MonitorMixedCutPowerSwitchAsync(cutPowerSwitchIndex, cutPower);
        }

        private async Task<bool> SetMixedLaserPowerAsync(string phaseName, string powerText)
        {
            if (!DecimalInputParser.TryParseFlexibleDouble(powerText, out double powerPercent))
            {
                await NotifyAsync("error", "Laser Power", $"{phaseName} power khong hop le: {powerText}");
                return false;
            }

            await HandleSetLaserPowerAsync(powerPercent);
            return true;
        }

        private async Task MonitorMixedCutPowerSwitchAsync(int switchAtDataNo, string powerText)
        {
            try
            {
                bool sawNewRunBeforeSwitch = switchAtDataNo <= 1;
                while (!isClosing && isProgramRunning)
                {
                    int activeIndex = GetActiveProgramIndex();
                    if (!sawNewRunBeforeSwitch)
                    {
                        if (activeIndex > 0 && activeIndex < switchAtDataNo)
                            sawNewRunBeforeSwitch = true;

                        await Task.Delay(50);
                        continue;
                    }

                    if (activeIndex >= switchAtDataNo)
                        break;

                    await Task.Delay(50);
                }

                if (isClosing || !isProgramRunning)
                    return;

                await WriteDeviceValueAsync(PauseRegister, 1);
                AddLogEntry(PauseRegister, "1", "Write", "OK", "Pause before Cat power");
                await Task.Delay(100);
                await WriteDeviceValueAsync(PauseRegister, 0);
                AddLogEntry(PauseRegister, "0", "Write", "OK", "Pause reset before Cat power");

                await Task.Delay(200);
                if (!await SetMixedLaserPowerAsync("Cat", powerText))
                    return;

                await Task.Delay(200);
                if (isClosing || !isProgramRunning)
                    return;

                await WriteDeviceValueAsync(ContinueRegister, 1);
                AddLogEntry(ContinueRegister, "1", "Write", "OK", "Continue after Cat power");
                await Task.Delay(100);
                await WriteDeviceValueAsync(ContinueRegister, 0);
                AddLogEntry(ContinueRegister, "0", "Write", "OK", "Continue reset after Cat power");
            }
            catch (Exception ex)
            {
                UpdateIntegrityFault(ex.Message);
                AddLogEntry("KhacCatPower", powerText ?? string.Empty, "Write", "Error", ex.Message);
                await NotifyAsync("error", "Laser Power", "Loi doi cong suat Cat: " + ex.Message);
            }
        }

        private async Task HandleContinueWriteAsync(bool active)
        {
            if (!active)
                return;
            if (!await RequirePlcConnectedAsync("Continue"))
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
            if (!await RequirePlcConnectedAsync("Pause"))
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
            isProgramRunning = false;
            programRunCompletionTracker.Reset();
            ui.RunProgressVisible = false;
            PLCCommunication comm;
            if (!TryGetConnectedPlc(out comm))
            {
                await NotifyAsync("error", "Stop", PlcConnectionGuard.NotConnectedMessage);
                return;
            }

            activeRingRunner?.Stop();
            bool pollingPausedForWrite = ShouldPausePlcPollingForWrite(comm);
            if (pollingPausedForWrite)
                await StopPlcPollingAsync();

            Interlocked.Increment(ref plcWriteInFlight);
            try
            {
                await LogUIAsync("Stop", "STOP writes M210 ON, then clears PLC positioning buffers and run commands...");

                await WriteDeviceValueAsync(PauseRegister, 1);
                AddLogEntry(PauseRegister, "1", "Write", "OK", "Stop");
                await Task.Delay(500);

                // Clear coordinate and run-command buffers immediately after STOP is ON.
                var clearResult = await Task.Run(() => QD75BufferWriter.ClearAllBuffers(comm, maxPoints: 600));
                foreach (var wr in clearResult.WriteResults)
                {
                    AddLogEntry(wr.Address, wr.Value, "Clear", wr.Status, wr.Message);
                    if (!wr.Status.StartsWith("OK"))
                        await NotifyAsync("warning", "Stop", $"{wr.Address}: {wr.Message}");
                }

                await WriteDeviceValueAsync(PauseRegister, 0);
                AddLogEntry(PauseRegister, "0", "Write", "OK", "Stop reset");

                if (clearResult.Success)
                {
                    ui.IsStartActionEnabled = processRows.Count > 0;
                    UpdateIntegrityFault("STOP M210 pulsed and run buffers cleared");
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
                    await WriteDeviceValueAsync(PauseRegister, 0);
                    AddLogEntry(PauseRegister, "0", "Write", "OK", "Stop reset after error");
                }
                catch
                {
                }

                UpdateIntegrityFault(ex.Message);
                AddLogEntry(PauseRegister, "1", "Write", "Error", ex.Message);
                await PushControlStateAsync();
                await NotifyAsync("error", "Stop", ex.Message);
            }
            finally
            {
                Interlocked.Decrement(ref plcWriteInFlight);
                _ = SendProgressAsync(false, 0);
                if (pollingPausedForWrite && plcComm != null && plcComm.IsConnected && !isClosing)
                    StartPlcPolling();
            }
        }

        private async Task HandleSetJogSpeedAsync(string text)
        {
            try
            {
                if (!await RequirePlcConnectedAsync("Settings"))
                    return;

                if (!DecimalInputParser.TryParseFlexibleDouble(text, out double value))
                {
                    await NotifyAsync("error", "Settings", "Jog speed phải là số thập phân hợp lệ.");
                    return;
                }

                float fVal = (float)value;
                int intVal = PlcFloatWordCodec.ToInt32Bits(fVal);
                await WriteDeviceValueAsync("D406", intVal);
                currentJogSpeedD406 = fVal;
                ui.JogSpeedD406 = fVal;
                ui.AcceptJogSpeedInputAsSynced();
                AddLogEntry("D406", value.ToString("F3", CultureInfo.InvariantCulture), "Write", "OK", "SetJogSpeed(mm/min)");
                await NotifyAsync("success", "Settings", $"Updated Jog speed: {value:F3} mm/min (D406)");
            }
            catch (Exception ex)
            {
                await NotifyAsync("error", "Settings", "Error updating Jog speed: " + ex.Message);
            }
        }

        private async Task HandleSetZHeightAsync(string text)
        {
            try
            {
                if (!await RequirePlcConnectedAsync("Z Height"))
                    return;

                if (!ZHeightSetting.TryConvertToPlcUnits(text, out int plcValue))
                {
                    await NotifyAsync("error", "Z Height", "Z height must be a non-negative decimal in millimetres.");
                    return;
                }

                await WriteDeviceValueAsync("D110", plcValue);
                AddLogEntry("D110", plcValue.ToString(CultureInfo.InvariantCulture), "Write", "OK", "Set Z Height");

                await WriteDeviceValueAsync(StopRunRegister, 1);
                AddLogEntry(StopRunRegister, "1", "Write", "OK", "Set Z Height trigger");
                await WriteDeviceValueAsync(StopRunRegister, 0);
                AddLogEntry(StopRunRegister, "0", "Write", "OK", "Set Z Height trigger reset");

                await NotifyAsync("success", "Z Height", $"Z height set to {text} mm.");
            }
            catch (Exception ex)
            {
                await NotifyAsync("error", "Z Height", "Error setting Z height: " + ex.Message);
            }
        }

        private async Task HandleSetLaserPowerAsync(double value)
        {
            try
            {
                // UI inputs percentage (0-100%). Map it to PLC value (450-2000).
                int plcValue = EngraveCutProcessComposer.MapLaserPowerPercentToPlcValue(value);

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
            if (!await RequirePlcConnectedAsync("PLC"))
                return;

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
                CancellationToken token = plcPollCts.Token;
                plcPollTask = Task.Run(() => PlcPollLoopAsync(token));
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

                int delayMs = Math.Max(PerformanceTuning.PlcPollMinimumDelayMs, PlcPollIntervalMs - (int)elapsed.ElapsedMilliseconds);
                await Task.Delay(delayMs, token);
            }
        }

        private Task PollPlcOnceAsync(CancellationToken token)
        {
            if (isClosing || isPolling)
                return Task.CompletedTask;

            PLCCommunication comm;
            if (!TryGetMonitoringPlc(out comm))
                return Task.CompletedTask;

            isPolling = true;
            try
            {
                token.ThrowIfCancellationRequested();
                if (comm.TryReadDeviceWords(FastMonitorDeviceList, out int[] snapshot))
                    ApplyFastMonitorSnapshot(snapshot);
                else
                    ReadFastMonitorSnapshotFallback(comm, GetNextFastMonitorAxis());

                if (isProgramRunning)
                {
                    int activeDataNo = GetActiveProgramIndex();
                    bool allAxesStopped = true;
                    for (int i = 0; i < 4; i++)
                    {
                        if (axAxisStatus[i] > 1 || axCurrentSpeed[i] > 0)
                        {
                            allAxesStopped = false;
                            break;
                        }
                    }

                    if (programRunCompletionTracker.Observe(
                        activeDataNo,
                        Math.Max(0, axLastDataNo[0]),
                        processRows.Count,
                        allAxesStopped))
                    {
                        isProgramRunning = false;
                    }
                }

                token.ThrowIfCancellationRequested();
                UpdateIntegrityState(true);

                DateTime nowUtc = DateTime.UtcNow;
                if (controlUiPushGate.TryEnter(nowUtc))
                    ScheduleControlStatePushFromPoll(includeTracking: controlTrackingUiPushGate.TryEnter(nowUtc));

                if (axisMonitorUiPushGate.TryEnter(nowUtc))
                    ScheduleAxisMonitorStatePushFromPoll();

                ScheduleBackgroundPlcWork(nowUtc);
            }
            finally
            {
                isPolling = false;
            }

            return Task.CompletedTask;
        }
        // ── Shared helpers ───────────────────────────────────────────────────────
        private const int FastMCodeStartIndex = 16;
        private const int FastJogSpeedStartIndex = 20;
        private const int FastAxisMonitorStartIndex = 22;
        private const int FastAxisMonitorWordCount = 6;
        private const int FastSnapshotWordCount = 46;
        private static readonly string[] FastMonitorDeviceList = CreateFastMonitorDeviceList();

        private static string[] CreateFastMonitorDeviceList()
        {
            var devices = new List<string>(FastSnapshotWordCount);
            for (int axis = 0; axis < 4; axis++)
            {
                int dBase = axis * 10;
                devices.Add($"D{dBase}");
                devices.Add($"D{dBase + 1}");
                devices.Add($"D{dBase + 4}");
                devices.Add($"D{dBase + 5}");
            }

            for (int axis = 0; axis < 4; axis++)
                devices.Add($"D{axis * 10 + 104}");

            devices.Add("D406");
            devices.Add("D407");
            for (int axis = 0; axis < 4; axis++)
            {
                int monitorBase = MonitorBaseG[axis];
                devices.Add($"U0\\G{monitorBase + OffErrorCode}");
                devices.Add($"U0\\G{monitorBase + OffWarningCode}");
                devices.Add($"U0\\G{monitorBase + OffAxisStatus}");
                devices.Add($"U0\\G{monitorBase + 16}");
                devices.Add($"U0\\G{monitorBase + 35}");
                devices.Add($"U0\\G{monitorBase + 37}");
            }

            return devices.ToArray();
        }

        private void ApplyFastMonitorSnapshot(int[] snapshot)
        {
            if (snapshot == null || snapshot.Length < FastSnapshotWordCount)
                return;

            for (int axis = 0; axis < 4; axis++)
            {
                int wordIndex = axis * 4;
                axCurrentPos[axis] = CombineWords(snapshot[wordIndex], snapshot[wordIndex + 1]);
                axCurrentSpeed[axis] = CombineWords(snapshot[wordIndex + 2], snapshot[wordIndex + 3]);
                axMCode[axis] = snapshot[FastMCodeStartIndex + axis];
            }

            currentJogSpeedD406 = PlcFloatWordCodec.FromWords(
                snapshot[FastJogSpeedStartIndex],
                snapshot[FastJogSpeedStartIndex + 1]);
            for (int axis = 0; axis < 4; axis++)
            {
                int monitorIndex = FastAxisMonitorStartIndex + axis * FastAxisMonitorWordCount;
                axErrorCode[axis] = snapshot[monitorIndex++];
                axWarningCode[axis] = snapshot[monitorIndex++];
                axAxisStatus[axis] = snapshot[monitorIndex++];
                axSignals[axis] = snapshot[monitorIndex++];
                axCurrentDataNo[axis] = snapshot[monitorIndex++];
                axLastDataNo[axis] = snapshot[monitorIndex];
            }
        }

        private void ReadFastMonitorSnapshotFallback(PLCCommunication comm, int monitorAxis)
        {
            try
            {
                int[] motionWords = comm.ReadDeviceRange("D0", 36);
                for (int axis = 0; axis < 4; axis++)
                {
                    int dBase = axis * 10;
                    axCurrentPos[axis] = CombineWords(motionWords[dBase], motionWords[dBase + 1]);
                    axCurrentSpeed[axis] = CombineWords(motionWords[dBase + 4], motionWords[dBase + 5]);
                }
            }
            catch
            {
            }

            try
            {
                int[] mCodes = comm.ReadDeviceRange("D104", 31);
                for (int axis = 0; axis < 4; axis++)
                    axMCode[axis] = mCodes[axis * 10];
            }
            catch
            {
            }

            try
            {
                int[] d406Raw = comm.ReadDeviceRange("D406", 2);
                currentJogSpeedD406 = PlcFloatWordCodec.FromWords(d406Raw[0], d406Raw[1]);
            }
            catch
            {
            }

            if (monitorAxis != 0)
            {
                try
                {
                    axCurrentDataNo[0] = comm.ReadBufferSelected(0, MonitorBaseG[0], 35)[0];
                }
                catch
                {
                }
            }

            ReadFastAxisMonitorFallback(comm, monitorAxis);
        }

        private void ReadFastAxisMonitorFallback(PLCCommunication comm, int axisIndex)
        {
            try
            {
                int[] values = comm.ReadBufferSelected(
                    0,
                    MonitorBaseG[axisIndex],
                    OffErrorCode,
                    OffWarningCode,
                    OffAxisStatus,
                    16,
                    35,
                    37);

                axErrorCode[axisIndex] = values[0];
                axWarningCode[axisIndex] = values[1];
                axAxisStatus[axisIndex] = values[2];
                axSignals[axisIndex] = values[3];
                axCurrentDataNo[axisIndex] = values[4];
                axLastDataNo[axisIndex] = values[5];
            }
            catch
            {
            }
        }

        private static int CombineWords(int low, int high)
            => unchecked(((high & 0xFFFF) << 16) | (low & 0xFFFF));

        private int GetNextFastMonitorAxis()
        {
            int value = Interlocked.Increment(ref nextFastMonitorAxis);
            if (value == int.MaxValue)
                Interlocked.Exchange(ref nextFastMonitorAxis, -1);

            return Math.Abs(value) % 4;
        }

        private void ScheduleBackgroundPlcWork(DateTime nowUtc)
        {
            SchedulePlcHeartbeat(nowUtc);

            if (slowPlcMonitorPollGate.TryEnter(nowUtc)
                && Interlocked.CompareExchange(ref slowPlcMonitorInFlight, 1, 0) == 0)
            {
                _ = Task.Run(RunSlowPlcMonitorAsync);
            }

        }

        private void SchedulePlcHeartbeat(DateTime nowUtc)
        {
            if (!plcHeartbeatGate.TryEnter(nowUtc)
                || Volatile.Read(ref plcWriteInFlight) > 0
                || Interlocked.CompareExchange(ref plcHeartbeatInFlight, 1, 0) != 0
                || !TryGetConnectedPlc(out PLCCommunication comm))
            {
                return;
            }

            _ = Task.Run(() =>
            {
                try
                {
                    if (ReferenceEquals(plcComm, comm) && comm.IsConnected)
                        comm.WriteDeviceValue(HeartbeatRegister, 1);
                }
                catch
                {
                }
                finally
                {
                    Interlocked.Exchange(ref plcHeartbeatInFlight, 0);
                }
            });
        }

        private async Task RunSlowPlcMonitorAsync()
        {
            try
            {
                if (Volatile.Read(ref plcWriteInFlight) > 0 || !TryGetConnectedPlc(out PLCCommunication comm))
                    return;

                foreach (var row in monitorRows)
                {
                    if (Volatile.Read(ref plcWriteInFlight) > 0)
                        return;

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

                ScheduleFullControlStatePushFromPoll(includeTracking: false);
            }
            finally
            {
                Interlocked.Exchange(ref slowPlcMonitorInFlight, 0);
            }
        }

        private void ScheduleControlStatePushFromPoll(bool includeTracking)
            => ScheduleControlStatePushFromPoll(includeTracking, full: false);

        private void ScheduleFullControlStatePushFromPoll(bool includeTracking)
            => ScheduleControlStatePushFromPoll(includeTracking, full: true);

        private void ScheduleAxisMonitorStatePushFromPoll()
        {
            if (Interlocked.CompareExchange(ref axisMonitorUiPushInFlight, 1, 0) != 0)
                return;

            _ = CompleteAxisMonitorStatePushAsync();
        }

        private async Task CompleteAxisMonitorStatePushAsync()
        {
            try
            {
                await PushAxisMonitorStateAsync();
            }
            catch
            {
            }
            finally
            {
                Interlocked.Exchange(ref axisMonitorUiPushInFlight, 0);
            }
        }

        private void ScheduleControlStatePushFromPoll(bool includeTracking, bool full)
        {
            if (Interlocked.CompareExchange(ref controlUiPushInFlight, 1, 0) != 0)
                return;

            _ = CompleteControlStatePushAsync(includeTracking, full);
        }

        private async Task CompleteControlStatePushAsync(bool includeTracking, bool full)
        {
            try
            {
                if (full)
                    await PushControlStateAsync(includeTracking);
                else
                    await PushFastControlStateAsync(includeTracking);
            }
            catch
            {
            }
            finally
            {
                Interlocked.Exchange(ref controlUiPushInFlight, 0);
            }
        }

        private Task WriteDeviceValueAsync(string deviceName, int value)
        {
            Interlocked.Increment(ref plcWriteInFlight);
            return WriteDeviceValueSerializedAsync(deviceName, value);
        }

        private async Task WriteDeviceValueSerializedAsync(string deviceName, int value)
        {
            await plcDeviceWriteGate.WaitAsync();
            try
            {
                await Task.Run(() =>
                {
                    PLCCommunication comm = GetConnectedPlcOrThrow();

                    comm.WriteDeviceValue(deviceName, value);
                });
            }
            finally
            {
                plcDeviceWriteGate.Release();
                Interlocked.Decrement(ref plcWriteInFlight);
            }
        }

        private void EnsureConnected()
        {
            GetConnectedPlcOrThrow();
        }

        private PLCCommunication GetConnectedPlcOrThrow()
        {
            PLCCommunication comm;
            if (!TryGetConnectedPlc(out comm))
                throw new InvalidOperationException(PlcConnectionGuard.NotConnectedMessage);

            return comm;
        }

        private bool TryGetConnectedPlc(out PLCCommunication comm)
        {
            comm = plcComm;
            return PlcConnectionGuard.CanUsePlc(comm != null, comm != null && comm.IsConnected);
        }

        private bool TryGetMonitoringPlc(out PLCCommunication comm)
        {
            comm = plcMonitorComm;
            return PlcConnectionGuard.CanUsePlc(comm != null, comm != null && comm.IsConnected);
        }

        private bool ShouldPausePlcPollingForWrite(PLCCommunication writeComm)
        {
            PLCCommunication monitorComm;
            return !TryGetMonitoringPlc(out monitorComm) || ReferenceEquals(writeComm, monitorComm);
        }

        private async Task<bool> RequirePlcConnectedAsync(string title)
        {
            PLCCommunication comm;
            if (TryGetConnectedPlc(out comm))
                return true;

            await NotifyAsync("error", title, PlcConnectionGuard.NotConnectedMessage);
            return false;
        }

        private async Task<bool> RequirePlcStartupReadyAsync(string title)
        {
            if (plcStartupReady)
                return true;

            await NotifyAsync("info", title, "PLC is preparing startup buffers. Please wait.");
            return false;
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
