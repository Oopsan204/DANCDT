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

        // ── Velocity (placeholder) ───────────────────────────────────────────────
        private async Task HandleSetVelocityAsync(int value)
        {
            await NotifyAsync("info", "PLC", "Velocity control via Cd.14 buffer not yet implemented.");
            await PushControlStateAsync();
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
                await WriteDeviceValueAsync("M502", v);
                UpdateIntegrityState(true);
                AddLogEntry("M502", v.ToString(CultureInfo.InvariantCulture), "Write", "OK", "GoHome");
            }
            catch (Exception ex)
            {
                if (active)
                {
                    UpdateIntegrityFault(ex.Message);
                    AddLogEntry("M502", (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                    await NotifyAsync("error", "Go Home", ex.Message);
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
                await WriteDeviceValueAsync("M400", v);
                UpdateIntegrityState(true);
                AddLogEntry("M400", v.ToString(CultureInfo.InvariantCulture), "Write", "OK", "ResetError");
            }
            catch (Exception ex)
            {
                if (active)
                {
                    UpdateIntegrityFault(ex.Message);
                    AddLogEntry("M400", (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                    await NotifyAsync("error", "Reset Error", ex.Message);
                    await PushControlStateAsync();
                }
            }
        }

        // ── Start ────────────────────────────────────────────────────────────────
        private async Task HandleStartWriteAsync(bool active)
        {
            try
            {
                int v = active ? 1 : 0;
                await WriteDeviceValueAsync("M2000", v);
                UpdateIntegrityState(true);
                AddLogEntry("M2000", v.ToString(CultureInfo.InvariantCulture), "Write", "OK", "Start");
            }
            catch (Exception ex)
            {
                if (active)
                {
                    UpdateIntegrityFault(ex.Message);
                    AddLogEntry("M2000", (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                    await NotifyAsync("error", "Start", ex.Message);
                    await PushControlStateAsync();
                }
            }
        }

        // ── Jog Speed ────────────────────────────────────────────────────────────
        private async Task HandleContinueWriteAsync(bool active)
        {
            try
            {
                int v = active ? 1 : 0;
                await WriteDeviceValueAsync("M401", v);
                UpdateIntegrityState(true);
                AddLogEntry("M401", v.ToString(CultureInfo.InvariantCulture), "Write", "OK", "Continue");
            }
            catch (Exception ex)
            {
                if (active)
                {
                    UpdateIntegrityFault(ex.Message);
                    AddLogEntry("M401", (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                    await NotifyAsync("error", "Continue", ex.Message);
                    await PushControlStateAsync();
                }
            }
        }

        private async Task HandlePauseWriteAsync(bool active)
        {
            try
            {
                int v = active ? 1 : 0;
                await WriteDeviceValueAsync("M402", v);
                UpdateIntegrityState(true);
                AddLogEntry("M402", v.ToString(CultureInfo.InvariantCulture), "Write", "OK", "Pause");
            }
            catch (Exception ex)
            {
                if (active)
                {
                    UpdateIntegrityFault(ex.Message);
                    AddLogEntry("M402", (active ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Write", "Error", ex.Message);
                    await NotifyAsync("error", "Pause", ex.Message);
                    await PushControlStateAsync();
                }
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
