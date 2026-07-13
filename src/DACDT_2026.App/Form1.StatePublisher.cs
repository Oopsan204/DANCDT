using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DACDT_2026
{
    public partial class Form1
    {
        private Task PushAllStateAsync()
            => Task.WhenAll(PushControlStateAsync(), PushDxfStateAsync(), PushTelemetryStateAsync(), PushLogsStateAsync());

        private Task PushNavigationStateAsync()
        {
            var snapCurrentView = currentView;
            var snapCurrentTheme = currentTheme;

            return RunOnUiAsync(() =>
            {
                ui.CurrentView = snapCurrentView;
                ui.CurrentTheme = snapCurrentTheme;
            });
        }

        private static string FormatPositionMm(int rawValue) => QD75BufferWriter.FormatPositionMm(rawValue);
        private static string FormatSpeedMm(int rawValue) => QD75BufferWriter.FormatSpeedMm(rawValue);
        private static string FormatAxisStatus(int status) => QD75BufferWriter.FormatAxisStatus(status);

        private async Task PushFastControlStateAsync(bool includeTracking = false)
        {
            bool connected = PlcConnectionGuard.CanUsePlc(plcComm != null, plcComm != null && plcComm.IsConnected);
            string dash = "--";

            await RunOnUiAsync(() =>
            {
                ui.IsConnected = connected;
                ui.ConnectionBanner = connectionBanner;
                ui.ConnectionButtonText = connected ? "DISCONNECT PLC Q" : "CONNECT PLC Q";

                for (int i = 0; i < ui.Axes.Count && i < 4; i++)
                {
                    int mb = MonitorBaseG[i];
                    int rawStatus = axAxisStatus[i];
                    if (rawStatus > 32767) rawStatus -= 65536;

                    AxisStatusViewModel axis = ui.Axes[i];
                    axis.CurrentPos = connected ? FormatPositionMm(axCurrentPos[i]) : dash;
                    axis.CurrentSpeed = connected ? FormatSpeedMm(axCurrentSpeed[i]) : dash;
                    axis.MCode = connected ? axMCode[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.ErrorCode = connected ? axErrorCode[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.ErrorCodeAddr = $"U0\\G{mb + OffErrorCode}";
                    axis.WarningCode = connected ? axWarningCode[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.WarningCodeAddr = $"U0\\G{mb + OffWarningCode}";
                    axis.AxisStatus = connected ? FormatAxisStatus(rawStatus) : dash;
                    axis.CurrentDataNo = connected ? axCurrentDataNo[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.LastDataNo = connected ? axLastDataNo[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.LimitMinus = connected && (axSignals[i] & 0x01) != 0;
                    axis.LimitPlus = connected && (axSignals[i] & 0x02) != 0;
                    axis.HomeDog = connected && (axSignals[i] & 0x40) != 0;
                    axis.IsComplete = connected && rawStatus == 0;
                }

                ui.ApplyActiveProgramIndex(GetActiveProgramIndex(), ensureProcessVisible: false);

                if (includeTracking)
                {
                    var trackingPoints = BuildRobotTrackingPoints(
                        activeCadDocument,
                        workspaceWidth,
                        workspaceHeight,
                        connected,
                        axCurrentPos[0],
                        axCurrentPos[1]);
                    ui.UpdateCadTrackingPoint(trackingPoints.FirstOrDefault());
                }
            });
        }

        private async Task PushControlStateAsync(bool includeTracking = true)
        {
            bool connected = PlcConnectionGuard.CanUsePlc(plcComm != null, plcComm != null && plcComm.IsConnected);
            string dash = "--";

            await RunOnUiAsync(() =>
            {
                ui.CurrentView = currentView;
                ui.CurrentTheme = currentTheme;
                ui.IsConnected = connected;
                ui.ConnectionBanner = connectionBanner;
                ui.ConnectionMeta = $"MX Component logical station: {logicalStation}";
                ui.ConnectionButtonText = connected ? "DISCONNECT PLC Q" : "CONNECT PLC Q";
                ui.JogSpeedD406 = currentJogSpeedD406;
                ui.SetJogSpeedInputFromPlc(currentJogSpeedD406);

                for (int i = 0; i < ui.Axes.Count && i < 4; i++)
                {
                    int mb = MonitorBaseG[i];
                    int rawStatus = axAxisStatus[i];
                    if (rawStatus > 32767) rawStatus -= 65536;

                    AxisStatusViewModel axis = ui.Axes[i];
                    axis.CurrentPos = connected ? FormatPositionMm(axCurrentPos[i]) : dash;
                    axis.CurrentPosAddr = $"D{i * 10}";
                    axis.CurrentSpeed = connected ? FormatSpeedMm(axCurrentSpeed[i]) : dash;
                    axis.CurrentSpeedAddr = $"D{i * 10 + 4}";
                    axis.MCode = connected ? axMCode[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.MCodeAddr = $"D{i * 10 + 104}";

                    string errStr = connected ? axErrorCode[i].ToString(CultureInfo.InvariantCulture) : "0";
                    string warnStr = connected ? axWarningCode[i].ToString(CultureInfo.InvariantCulture) : "0";

                    axis.ErrorCode = connected ? axErrorCode[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.ErrorCodeAddr = $"U0\\G{mb + OffErrorCode}";
                    var errLookup = ErrorCodeRegistry.Lookup(errStr);
                    axis.ErrorDescription = errLookup != null
                        ? $"{errLookup.Description}\nNguyên nhân: {errLookup.Cause}\nKhắc phục: {errLookup.Remedy}"
                        : (connected && axErrorCode[i] != 0 ? "Lỗi không xác định" : "");

                    axis.WarningCode = connected ? axWarningCode[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.WarningCodeAddr = $"U0\\G{mb + OffWarningCode}";
                    var warnLookup = ErrorCodeRegistry.Lookup(warnStr);
                    axis.WarningDescription = warnLookup != null
                        ? $"{warnLookup.Description}\nNguyên nhân: {warnLookup.Cause}\nKhắc phục: {warnLookup.Remedy}"
                        : (connected && axWarningCode[i] != 0 ? "Cảnh báo không xác định" : "");

                    axis.AxisStatus = connected ? FormatAxisStatus(rawStatus) : dash;
                    axis.AxisStatusAddr = $"U0\\G{mb + OffAxisStatus}";
                    axis.CurrentDataNo = connected ? axCurrentDataNo[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.CurrentDataNoAddr = $"U0\\G{mb + 35}";
                    axis.LastDataNo = connected ? axLastDataNo[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.LastDataNoAddr = $"U0\\G{mb + 37}";
                    axis.LimitMinus = connected && (axSignals[i] & 0x01) != 0;
                    axis.LimitPlus = connected && (axSignals[i] & 0x02) != 0;
                    axis.HomeDog = connected && (axSignals[i] & 0x40) != 0;
                    axis.IsComplete = connected && rawStatus == 0;
                }

                UpdateActiveProgramHighlight(ui, GetActiveProgramIndex());

                if (includeTracking)
                {
                    ReplaceCollection(ui.CadTrackingPoints, BuildRobotTrackingPoints(
                        activeCadDocument,
                        workspaceWidth,
                        workspaceHeight,
                        connected,
                        axCurrentPos[0],
                        axCurrentPos[1]));
                }
            });

        }

        /// <summary>
        /// Publishes CAD state to MQTT on explicit web request.
        /// Machine/monitor state are published by the PLC polling loop.
        /// </summary>
        private async Task PublishAllMqttAsync()
        {
            bool connected = PlcConnectionGuard.CanUsePlc(plcComm != null, plcComm != null && plcComm.IsConnected);
            await PublishCadStateToMqttAsync(connected);
        }

        private async Task PublishCadStateToMqttAsync(bool connected)
        {
            if (!mqttService.IsConnected)
                return;

            try
            {
                var sb = new StringBuilder(2048);
                sb.Append("{");
                bool isGcodeKind = string.Equals(activeDocumentKind, "GCODE", StringComparison.OrdinalIgnoreCase);
                var rawDoc = CloneCadDocumentForUi(activeCadDocument);
                var displayDoc = CreateDisplayCadDocument(
                    rawDoc,
                    isGcodeKind,
                    offsetX,
                    offsetY,
                    wcsOffsetX.ToArray(),
                    wcsOffsetY.ToArray());
                var viewBounds = BuildCadViewBounds(rawDoc, workspaceWidth, workspaceHeight);

                sb.AppendFormat("\"fileKind\":\"{0}\"", EscapeJson(activeDocumentKind ?? string.Empty));
                sb.AppendFormat(",\"fileName\":\"{0}\"", EscapeJson(displayDoc?.FileName ?? string.Empty));
                sb.AppendFormat(",\"filePath\":\"{0}\"", EscapeJson(displayDoc?.FilePath ?? string.Empty));
                sb.AppendFormat(",\"workspaceWidth\":{0}", workspaceWidth.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendFormat(",\"workspaceHeight\":{0}", workspaceHeight.ToString("0.###", CultureInfo.InvariantCulture));
                sb.Append(",\"bounds\":");
                AppendCadBoundsJson(sb, displayDoc?.Bounds);
                sb.Append(",\"viewBounds\":");
                AppendCadBoundsJson(sb, viewBounds);

                sb.Append(",\"cadPrimitives\":[");
                if (displayDoc != null && displayDoc.Primitives != null)
                {
                    bool first = true;
                    foreach (var prim in displayDoc.Primitives.Take(50000))
                    {
                        if (!first) sb.Append(",");
                        first = false;

                        sb.Append("{");
                        sb.AppendFormat("\"sourceType\":\"{0}\"", EscapeJson(prim.SourceType ?? string.Empty));
                        sb.AppendFormat(",\"isCw\":{0}", prim.IsCw ? "true" : "false");
                        sb.AppendFormat(",\"isCircle\":{0}", prim.IsCircle ? "true" : "false");
                        sb.AppendFormat(",\"wcsIndex\":{0}", prim.WcsIndex);
                        sb.AppendFormat(",\"mCode\":\"{0}\"", EscapeJson(prim.MCodeValue ?? string.Empty));
                        sb.AppendFormat(",\"speed\":\"{0}\"", EscapeJson(prim.Speed ?? string.Empty));
                        sb.AppendFormat(",\"dwell\":\"{0}\"", EscapeJson(prim.Dwell ?? string.Empty));
                        sb.AppendFormat(",\"processKind\":\"{0}\"", EscapeJson(prim.ProcessKind ?? string.Empty));
                        sb.Append(",\"center\":");
                        AppendCadCoordinateJson(sb, prim.Center);
                        sb.Append(",\"points\":[");
                        if (prim.Points != null)
                        {
                            for (int i = 0; i < prim.Points.Count; i++)
                            {
                                if (i > 0) sb.Append(",");
                                AppendCadCoordinateJson(sb, prim.Points[i]);
                            }
                        }
                        sb.Append("]}");
                    }
                }
                sb.Append("]");

                sb.Append(",\"trackingPoints\":[");
                var trackingPoints = BuildRobotTrackingPoints(
                    activeCadDocument,
                    workspaceWidth,
                    workspaceHeight,
                    connected,
                    axCurrentPos[0],
                    axCurrentPos[1]);

                bool firstTp = true;
                foreach(var tp in trackingPoints)
                {
                    if (!firstTp) sb.Append(",");
                    firstTp = false;
                    sb.Append("{");
                    sb.AppendFormat("\"x\":{0}", tp.X.ToString(CultureInfo.InvariantCulture));
                    sb.AppendFormat(",\"y\":{0}", tp.Y.ToString(CultureInfo.InvariantCulture));
                    sb.AppendFormat(",\"size\":{0}", tp.Size.ToString(CultureInfo.InvariantCulture));
                    sb.AppendFormat(",\"label\":\"{0}\"", EscapeJson(tp.Label ?? string.Empty));
                    sb.AppendFormat(",\"tooltip\":\"{0}\"", EscapeJson(tp.ToolTip ?? string.Empty));
                    sb.Append("}");
                }
                sb.Append("]");

                sb.AppendFormat(",\"timestamp\":\"{0}\"", DateTime.UtcNow.ToString("o"));
                sb.Append("}");

                await mqttService.PublishAsync("DACDT/cad/state", sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT cad publish error: {ex.Message}");
            }
        }

        private static void AppendCadBoundsJson(StringBuilder sb, CadDocumentService.CadBounds bounds)
        {
            if (bounds == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append("{");
            sb.AppendFormat("\"left\":{0}", bounds.Left.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"top\":{0}", bounds.Top.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"right\":{0}", bounds.Right.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"bottom\":{0}", bounds.Bottom.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"width\":{0}", bounds.Width.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"height\":{0}", bounds.Height.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"minZ\":{0}", bounds.MinZ.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"maxZ\":{0}", bounds.MaxZ.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append("}");
        }

        private static void AppendCadCoordinateJson(StringBuilder sb, CadDocumentService.CadCoordinate point)
        {
            if (point == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append("{");
            sb.AppendFormat("\"x\":{0}", point.X.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"y\":{0}", point.Y.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"z\":{0}", point.Z.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append("}");
        }

        private async Task PublishMachineStateToMqttAsync(bool connected)
        {
            if (!mqttService.IsConnected)
            {
                Console.WriteLine($"[DEBUG] MQTT not connected, skipping publish. IsConnected={mqttService.IsConnected}");
                return;
            }

            try
            {
                string dash = "--";
                var sb = new StringBuilder(512);
                sb.Append("{");
                sb.AppendFormat("\"connected\":{0}", connected ? "true" : "false");
                sb.AppendFormat(",\"connectionBanner\":\"{0}\"", EscapeJson(connectionBanner));
                sb.AppendFormat(",\"integrityState\":\"{0}\"", EscapeJson(integrityState));
                sb.AppendFormat(",\"integrityDetail\":\"{0}\"", EscapeJson(integrityDetail));
                sb.AppendFormat(",\"integrityTone\":\"{0}\"", EscapeJson(integrityTone));
                sb.AppendFormat(",\"jogSpeed\":{0}", currentJogSpeedD406.ToString(CultureInfo.InvariantCulture));
                sb.AppendFormat(",\"laserPower\":{0}", FormatJsonNumber(laserPower, "0"));
                sb.Append(",\"axes\":[");
                for (int i = 0; i < 4; i++)
                {
                    if (i > 0) sb.Append(",");
                    int rawStatus = axAxisStatus[i];
                    if (rawStatus > 32767) rawStatus -= 65536;
                    sb.Append("{");
                    sb.AppendFormat("\"idx\":{0}", i);
                    sb.AppendFormat(",\"pos\":\"{0}\"", connected ? FormatPositionMm(axCurrentPos[i]) : dash);
                    sb.AppendFormat(",\"speed\":\"{0}\"", connected ? FormatSpeedMm(axCurrentSpeed[i]) : dash);
                    sb.AppendFormat(",\"mCode\":{0}", connected ? axMCode[i] : 0);
                    sb.AppendFormat(",\"error\":{0}", connected ? axErrorCode[i] : 0);
                    
                    var errLookup = ErrorCodeRegistry.Lookup(connected ? axErrorCode[i].ToString(CultureInfo.InvariantCulture) : "0");
                    string errDesc = errLookup != null ? $"{errLookup.Description} | Nguyên nhân: {errLookup.Cause} | Khắc phục: {errLookup.Remedy}" : "";
                    sb.AppendFormat(",\"errorDesc\":\"{0}\"", EscapeJson(errDesc));

                    sb.AppendFormat(",\"warning\":{0}", connected ? axWarningCode[i] : 0);
                    var warnLookup = ErrorCodeRegistry.Lookup(connected ? axWarningCode[i].ToString(CultureInfo.InvariantCulture) : "0");
                    string warnDesc = warnLookup != null ? $"{warnLookup.Description} | Nguyên nhân: {warnLookup.Cause} | Khắc phục: {warnLookup.Remedy}" : "";
                    sb.AppendFormat(",\"warningDesc\":\"{0}\"", EscapeJson(warnDesc));

                    sb.AppendFormat(",\"status\":\"{0}\"", connected ? FormatAxisStatus(rawStatus) : dash);
                    sb.AppendFormat(",\"dataNo\":{0}", connected ? axCurrentDataNo[i] : 0);
                    sb.AppendFormat(",\"limitMinus\":{0}", (connected && (axSignals[i] & 0x01) != 0) ? "true" : "false");
                    sb.AppendFormat(",\"limitPlus\":{0}", (connected && (axSignals[i] & 0x02) != 0) ? "true" : "false");
                    sb.AppendFormat(",\"homeDog\":{0}", (connected && (axSignals[i] & 0x40) != 0) ? "true" : "false");
                    sb.AppendFormat(",\"isComplete\":{0}", (connected && rawStatus == 0) ? "true" : "false");
                    sb.Append("}");
                }
                sb.Append("]");
                sb.AppendFormat(",\"timestamp\":\"{0}\"", DateTime.UtcNow.ToString("o"));
                sb.Append("}");

                Console.WriteLine($"[DEBUG] Publishing to DACDT/machine/state: {sb.ToString().Substring(0, Math.Min(100, sb.Length))}...");
                await mqttService.PublishAsync("DACDT/machine/state", sb.ToString());
                await PublishMonitorStateToMqttAsync(connected);
                Console.WriteLine($"[DEBUG] Successfully published to DACDT/machine/state");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT publish error: {ex.Message}");
            }
        }

        private async Task PublishMonitorStateToMqttAsync(bool connected)
        {
            if (!mqttService.IsConnected)
                return;

            try
            {
                var sb = new StringBuilder(512);
                AppendMonitorStateJson(sb, connected);
                await mqttService.PublishAsync("DACDT/monitor/state", sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT monitor publish error: {ex.Message}");
            }
        }

        private void AppendMonitorStateJson(StringBuilder sb, bool connected)
        {
            int activeDataNo = connected
                ? GetActiveProgramIndex()
                : Math.Max(0, ui.ActiveProgramIndex);
            ProcessRow[] rowsSnapshot = processRows.ToArray();
            int totalPoints = rowsSnapshot.Length;
            int cadPointCount = activeCadDocument?.Points?.Count ?? 0;
            ProcessRow activeRow = activeDataNo > 0 && activeDataNo <= totalPoints
                ? rowsSnapshot[activeDataNo - 1]
                : null;

            sb.Append("{");
            sb.AppendFormat("\"fileKind\":\"{0}\"", EscapeJson(activeDocumentKind ?? string.Empty));
            sb.AppendFormat(",\"fileName\":\"{0}\"", EscapeJson(activeCadDocument?.FileName ?? string.Empty));
            sb.AppendFormat(",\"filePath\":\"{0}\"", EscapeJson(activeCadDocument?.FilePath ?? string.Empty));
            sb.AppendFormat(",\"currentView\":\"{0}\"", EscapeJson(currentView ?? string.Empty));
            sb.Append(",\"dxfCompletion\":{");
            bool isRunning = activeDataNo > 0 && totalPoints > 0;
            bool visible = isRunning || ui.ProgressVisible;
            int percent = isRunning ? ui.RunProgressPercent : ui.ProgressPercent;
            string text = isRunning ? $"{ui.RunProgressPercent}%" : (ui.ProgressText ?? string.Empty);
            
            sb.AppendFormat("\"visible\":{0}", visible ? "true" : "false");
            sb.AppendFormat(",\"percent\":{0}", percent);
            sb.AppendFormat(",\"text\":\"{0}\"", EscapeJson(text));
            sb.Append("}");
            sb.Append(",\"dxfPoint\":{");
            sb.AppendFormat("\"activeDataNo\":{0}", activeDataNo);
            sb.AppendFormat(",\"activeText\":\"{0}\"", EscapeJson(ui.ActiveProgramText ?? string.Empty));
            sb.AppendFormat(",\"totalPoints\":{0}", totalPoints);
            sb.AppendFormat(",\"cadPointCount\":{0}", cadPointCount);
            sb.Append(",\"activeRow\":");
            AppendProcessRowJson(sb, activeRow, activeDataNo);
            sb.Append("}");
            
            // Append real-time robot tracking points
            var trackingPoints = BuildRobotTrackingPoints(
                activeCadDocument,
                workspaceWidth,
                workspaceHeight,
                connected,
                axCurrentPos[0],
                axCurrentPos[1]);

            sb.Append(",\"trackingPoints\":[");
            bool firstTp = true;
            foreach (var tp in trackingPoints)
            {
                if (!firstTp) sb.Append(",");
                firstTp = false;
                sb.Append("{");
                sb.AppendFormat("\"x\":{0}", tp.X.ToString(CultureInfo.InvariantCulture));
                sb.AppendFormat(",\"y\":{0}", tp.Y.ToString(CultureInfo.InvariantCulture));
                sb.AppendFormat(",\"size\":{0}", tp.Size.ToString(CultureInfo.InvariantCulture));
                sb.AppendFormat(",\"label\":\"{0}\"", EscapeJson(tp.Label ?? string.Empty));
                sb.Append("}");
            }
            sb.Append("]");

            sb.AppendFormat(",\"timestamp\":\"{0}\"", DateTime.UtcNow.ToString("o"));
            sb.Append("}");
        }

        private void AppendProcessRowJson(StringBuilder sb, ProcessRow row, int index)
        {
            if (row == null)
            {
                sb.Append("null");
                return;
            }

            double rowOx;
            double rowOy;
            if (row.MCodeValue == "0" && string.Equals(row.EndCoordinate, "0;0"))
            {
                rowOx = 0.0;
                rowOy = 0.0;
            }
            else if (string.Equals(activeDocumentKind, "GCODE", StringComparison.OrdinalIgnoreCase))
            {
                int wIdx = Math.Max(0, Math.Min(5, row.WcsIndex));
                rowOx = wcsOffsetX[wIdx];
                rowOy = wcsOffsetY[wIdx];
            }
            else
            {
                rowOx = offsetX;
                rowOy = offsetY;
            }

            sb.Append("{");
            sb.AppendFormat("\"index\":{0}", index);
            sb.AppendFormat(",\"key\":\"{0}\"", EscapeJson(row.Key ?? string.Empty));
            sb.AppendFormat(",\"motionType\":\"{0}\"", EscapeJson(row.MotionType ?? string.Empty));
            sb.AppendFormat(",\"mCode\":\"{0}\"", EscapeJson(row.MCodeValue ?? string.Empty));
            sb.AppendFormat(",\"dwell\":\"{0}\"", EscapeJson(row.Dwell ?? string.Empty));
            sb.AppendFormat(",\"speed\":\"{0}\"", EscapeJson(row.Speed ?? string.Empty));
            sb.AppendFormat(",\"processKind\":\"{0}\"", EscapeJson(row.ProcessKind ?? string.Empty));
            sb.AppendFormat(",\"laserPower\":\"{0}\"", EscapeJson(row.LaserPower ?? string.Empty));
            sb.AppendFormat(",\"endCoordinate\":\"{0}\"", EscapeJson(ApplyOffsetToCoord(row.EndCoordinate, rowOx, rowOy)));
            sb.AppendFormat(",\"centerCoordinate\":\"{0}\"", EscapeJson(ApplyOffsetToCoord(row.CenterCoordinate, rowOx, rowOy)));
            sb.AppendFormat(",\"endZ\":{0}", row.EndZ.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append("}");
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string FormatJsonNumber(string text, string fallback)
        {
            double value;
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
                ? value.ToString("0.###", CultureInfo.InvariantCulture)
                : fallback;
        }

        /// <summary>
        /// Publish the current camera frame to MQTT as a Base64-encoded JPEG.
        /// Topic: DACDT/camera/frame
        /// Quality is kept at 60 to limit message size (~10-30 KB per frame).
        /// </summary>


        private async Task PushDxfStateAsync()
        {
            var snapDocSource = activeCadDocument;
            var snapRowsSource = processRows.ToArray();
            var snapKind = activeDocumentKind;
            var snapRawText = snapKind == "GCODE" ? rawGcodeText : string.Empty;
            var snapProfiles = GetProfilesList();
            var snapPointKey = selectedCadPointKey ?? string.Empty;
            var snapOx = offsetX;
            var snapOy = offsetY;
            var snapWorkspaceWidth = workspaceWidth;
            var snapWorkspaceHeight = workspaceHeight;
            var snapWcsOffsetX = wcsOffsetX.ToArray();
            var snapWcsOffsetY = wcsOffsetY.ToArray();
            var snapConnected = PlcConnectionGuard.CanUsePlc(plcComm != null, plcComm != null && plcComm.IsConnected);
            var snapRobotRawX = axCurrentPos[0];
            var snapRobotRawY = axCurrentPos[1];
            var snapActiveProgramIndex = GetActiveProgramIndex();
            var snapCurrentView = currentView;
            var snapCurrentTheme = currentTheme;
            var snapGlobalSpeed = globalSpeed;
            var snapGlobalSpeedM3 = globalSpeedM3;
            var snapGcodeSpeedM3 = gcodeSpeedM3;
            var snapRapidSpeed = rapidSpeed;
            var snapEngraveSpeed = engraveSpeed;
            var snapEngravePower = engravePower;
            var snapCutSpeed = cutSpeed;
            var snapCutPower = cutPower;
            var snapGlobalDwellM3 = globalDwellM3;
            var snapGlobalDwellM4 = globalDwellM4;
            var snapActiveWcs = activeWcs;
            var snapMixedEngraveCut = isMixedEngraveCutProgram;

            if (!string.Equals(snapCurrentView, "dxf", StringComparison.OrdinalIgnoreCase))
            {
                await RunOnUiAsync(() =>
                {
                    ui.CurrentView = snapCurrentView;
                    ui.CurrentTheme = snapCurrentTheme;
                    ui.FileKind = snapKind ?? string.Empty;
                    ui.FilePath = snapDocSource?.FilePath ?? string.Empty;
                    ui.FileName = snapDocSource?.FileName ?? string.Empty;
                    ui.ActiveWcs = snapActiveWcs;
                    ReplaceCollection(ui.Profiles, snapProfiles);
                });
                return;
            }

            var model = await Task.Run(() =>
            {
                bool isGcodeKind = string.Equals(snapKind, "GCODE", StringComparison.OrdinalIgnoreCase);
                var rawDoc = CloneCadDocumentForUi(snapDocSource);
                var snapDoc = CreateDisplayCadDocument(
                    rawDoc,
                    isGcodeKind,
                    snapOx,
                    snapOy,
                    snapWcsOffsetX,
                    snapWcsOffsetY);
                var snapRows = snapRowsSource.Select(CloneProcessRowForUi).Where(row => row != null).ToList();

                var points = snapDoc == null
                    ? new List<CadPointViewModel>()
                    : snapDoc.Points.Select(pt => new CadPointViewModel
                    {
                        Index = pt.Index,
                        LineType = pt.LineType,
                        X = Math.Round(pt.X, 3).ToString("0.###", CultureInfo.InvariantCulture),
                        Y = Math.Round(pt.Y, 3).ToString("0.###", CultureInfo.InvariantCulture),
                        Z = Math.Round(pt.Z, 3).ToString("0.###", CultureInfo.InvariantCulture),
                        Key = pt.Key,
                        IsActive = snapActiveProgramIndex > 0 && pt.Index == snapActiveProgramIndex
                    }).ToList();

                var geometryRows = BuildGeometryRows(snapDoc);

                var rows = snapRows.Select((row, rowIndex) =>
                {
                    double rowOx;
                    double rowOy;
                    if (row.MCodeValue == "0" && string.Equals(row.EndCoordinate, "0;0"))
                    {
                        rowOx = 0.0;
                        rowOy = 0.0;
                    }
                    else if (isGcodeKind)
                    {
                        int wIdx = Math.Max(0, Math.Min(5, row.WcsIndex));
                        rowOx = snapWcsOffsetX[wIdx];
                        rowOy = snapWcsOffsetY[wIdx];
                    }
                    else
                    {
                        rowOx = snapOx;
                        rowOy = snapOy;
                    }

                    return new ProcessRowViewModel
                    {
                        Index = rowIndex + 1,
                        Key = row.Key,
                        MotionType = row.MotionType,
                        MCodeValue = row.MCodeValue ?? string.Empty,
                        Dwell = row.Dwell ?? string.Empty,
                        Speed = row.Speed ?? string.Empty,
                        ProcessKind = row.ProcessKind ?? string.Empty,
                        LaserPower = row.LaserPower ?? string.Empty,
                        EndCoordinate = ApplyOffsetToCoord(row.EndCoordinate, rowOx, rowOy),
                        CenterCoordinate = ApplyOffsetToCoord(row.CenterCoordinate, rowOx, rowOy),
                        EndZ = row.EndZ.ToString("0.###", CultureInfo.InvariantCulture),
                        IsActive = snapActiveProgramIndex > 0 && rowIndex + 1 == snapActiveProgramIndex
                    };
                }).ToList();

                var projection = CreateCadProjection(rawDoc, snapWorkspaceWidth, snapWorkspaceHeight);
                var cadPreviewImage = BuildCadPreviewImage(snapDoc, projection);
                var cadPreviewGeometry = snapMixedEngraveCut ? null : BuildCadPreviewGeometry(snapDoc, projection);
                var cadEngravePreviewGeometry = BuildCadPreviewGeometry(snapDoc, projection, EngraveCutProcessComposer.EngraveKind);
                var cadCutPreviewGeometry = BuildCadPreviewGeometry(snapDoc, projection, EngraveCutProcessComposer.CutKind);
                var limitAreas = BuildCadLimitAreas(snapWorkspaceWidth, snapWorkspaceHeight, projection);
                var axisLines = BuildCadAxisLines(snapDoc, projection);
                var axisLabels = BuildCadAxisLabels(snapDoc, projection);
                var trackingPoints = BuildRobotTrackingPoints(
                    rawDoc,
                    snapWorkspaceWidth,
                    snapWorkspaceHeight,
                    snapConnected,
                    snapRobotRawX,
                    snapRobotRawY);
                return new { doc = snapDoc, points, geometryRows, rows, cadPreviewImage, cadPreviewGeometry, cadEngravePreviewGeometry, cadCutPreviewGeometry, limitAreas, axisLines, axisLabels, trackingPoints };
            });

            await RunOnUiAsync(() =>
            {
                ui.CurrentView = snapCurrentView;
                ui.CurrentTheme = snapCurrentTheme;
                ui.FileKind = snapKind ?? string.Empty;
                ui.FilePath = model.doc?.FilePath ?? string.Empty;
                ui.FileName = model.doc?.FileName ?? string.Empty;
                ui.RawGcodeText = snapRawText != null && snapRawText.Length > 200000
                    ? snapRawText.Substring(0, 200000) + "\n... [TRUNCATED FOR UI]"
                    : snapRawText ?? string.Empty;
                if (!string.Equals(snapCurrentView, "settings", StringComparison.OrdinalIgnoreCase))
                {
                    ui.GlobalSpeedInput = snapGlobalSpeed;
                    ui.GlobalSpeedM3Input = snapGlobalSpeedM3;
                    ui.GcodeSpeedM3Input = snapGcodeSpeedM3;
                    ui.RapidSpeedInput = snapRapidSpeed;
                    ui.EngraveSpeedInput = snapEngraveSpeed;
                    ui.EngravePowerInput = snapEngravePower;
                    ui.CutSpeedInput = snapCutSpeed;
                    ui.CutPowerInput = snapCutPower;
                    ui.GlobalDwellM3Input = snapGlobalDwellM3;
                    ui.GlobalDwellM4Input = snapGlobalDwellM4;
                    ui.OffsetXInput = snapOx;
                    ui.OffsetYInput = snapOy;
                    ui.WorkspaceWidthInput = snapWorkspaceWidth;
                    ui.WorkspaceHeightInput = snapWorkspaceHeight;
                }
                ui.ActiveWcs = snapActiveWcs;
                ui.ActiveProgramIndex = snapActiveProgramIndex;
                int wIdx = GetWcsIndex(snapActiveWcs);
                if (!string.Equals(snapCurrentView, "settings", StringComparison.OrdinalIgnoreCase))
                {
                    ui.WcsOffsetXInput = snapWcsOffsetX[wIdx];
                    ui.WcsOffsetYInput = snapWcsOffsetY[wIdx];
                }
                ui.SelectedPointKey = snapPointKey;

                ui.SetCadPointRows(model.points, snapActiveProgramIndex);
                ui.SetGeometryRows(model.geometryRows);
                ui.SetProcessRows(model.rows, snapActiveProgramIndex);
                ui.CadPreviewImage = model.cadPreviewImage;
                ui.CadPreviewGeometry = model.cadPreviewGeometry;
                ui.CadEngravePreviewGeometry = model.cadEngravePreviewGeometry;
                ui.CadCutPreviewGeometry = model.cadCutPreviewGeometry;
                ReplaceCollection(ui.CadPrimitives, Enumerable.Empty<CadPrimitiveViewModel>());
                ReplaceCollection(ui.CadLimitAreas, model.limitAreas);
                ReplaceCollection(ui.CadAxisLines, model.axisLines);
                ReplaceCollection(ui.CadAxisLabels, model.axisLabels);
                ReplaceCollection(ui.CadTrackingPoints, model.trackingPoints);
                ReplaceCollection(ui.Profiles, snapProfiles);
            });

        }

        private static void UpdateActiveProgramHighlight(WpfUiState state, int activeIndex)
        {
            if (state == null)
                return;

            state.ApplyActiveProgramIndex(activeIndex, ensureProcessVisible: true);
        }

        private static CadDocumentService.CadLoadResult CloneCadDocumentForUi(CadDocumentService.CadLoadResult doc)
        {
            if (doc == null) return null;

            return new CadDocumentService.CadLoadResult
            {
                FilePath = doc.FilePath,
                DirectoryPath = doc.DirectoryPath,
                FileName = doc.FileName,
                Bounds = doc.Bounds == null ? null : new CadDocumentService.CadBounds
                {
                    Left = doc.Bounds.Left,
                    Top = doc.Bounds.Top,
                    Right = doc.Bounds.Right,
                    Bottom = doc.Bounds.Bottom,
                    Width = doc.Bounds.Width,
                    Height = doc.Bounds.Height,
                    MinZ = doc.Bounds.MinZ,
                    MaxZ = doc.Bounds.MaxZ
                },
                Primitives = doc.Primitives == null
                    ? new List<CadDocumentService.CadPrimitiveData>()
                    : doc.Primitives.Select(CloneCadPrimitiveForUi).ToList(),
                Points = doc.Points == null
                    ? new List<CadDocumentService.CadPointData>()
                    : doc.Points.Select(CloneCadPointForUi).ToList()
            };
        }

        private static CadDocumentService.CadLoadResult CreateDisplayCadDocument(
            CadDocumentService.CadLoadResult rawDoc,
            bool isGcodeKind,
            double dxfOffsetX,
            double dxfOffsetY,
            double[] displayWcsOffsetX,
            double[] displayWcsOffsetY)
        {
            if (rawDoc == null) return null;

            var displayDoc = CloneCadDocumentForUi(rawDoc);
            if (displayDoc == null) return null;

            bool anyOffset = isGcodeKind
                ? HasAnyOffset(displayWcsOffsetX) || HasAnyOffset(displayWcsOffsetY)
                : Math.Abs(dxfOffsetX) > 1e-9 || Math.Abs(dxfOffsetY) > 1e-9;

            if (!anyOffset)
                return displayDoc;

            if (displayDoc.Primitives != null)
            {
                foreach (var primitive in displayDoc.Primitives)
                {
                    GetDisplayOffsetForPrimitive(
                        primitive,
                        isGcodeKind,
                        dxfOffsetX,
                        dxfOffsetY,
                        displayWcsOffsetX,
                        displayWcsOffsetY,
                        out double ox,
                        out double oy);

                    OffsetCoordinateList(primitive.Points, ox, oy);
                    OffsetCoordinate(primitive.Center, ox, oy);
                }
            }

            displayDoc.Points = RebuildPointRowsForDisplay(displayDoc.Primitives);
            displayDoc.Bounds = BuildDisplayBounds(displayDoc.Primitives, displayDoc.Points);
            return displayDoc;
        }

        private static bool HasAnyOffset(double[] values)
            => values != null && values.Any(value => Math.Abs(value) > 1e-9);

        private static void GetDisplayOffsetForPrimitive(
            CadDocumentService.CadPrimitiveData primitive,
            bool isGcodeKind,
            double dxfOffsetX,
            double dxfOffsetY,
            double[] displayWcsOffsetX,
            double[] displayWcsOffsetY,
            out double ox,
            out double oy)
        {
            if (isGcodeKind)
            {
                int wIdx = Math.Max(0, Math.Min(5, primitive?.WcsIndex ?? 0));
                ox = displayWcsOffsetX != null && displayWcsOffsetX.Length > wIdx ? displayWcsOffsetX[wIdx] : 0.0;
                oy = displayWcsOffsetY != null && displayWcsOffsetY.Length > wIdx ? displayWcsOffsetY[wIdx] : 0.0;
            }
            else
            {
                ox = dxfOffsetX;
                oy = dxfOffsetY;
            }
        }

        private static void OffsetCoordinateList(List<CadDocumentService.CadCoordinate> points, double ox, double oy)
        {
            if (points == null || (Math.Abs(ox) < 1e-9 && Math.Abs(oy) < 1e-9))
                return;

            foreach (var point in points)
                OffsetCoordinate(point, ox, oy);
        }

        private static void OffsetCoordinate(CadDocumentService.CadCoordinate point, double ox, double oy)
        {
            if (point == null) return;
            point.X += ox;
            point.Y += oy;
        }

        private static List<CadDocumentService.CadPointData> RebuildPointRowsForDisplay(
            List<CadDocumentService.CadPrimitiveData> primitives)
        {
            var rows = new List<CadDocumentService.CadPointData>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (primitives == null)
                return rows;

            foreach (var primitive in primitives)
            {
                if (primitive?.Points == null || primitive.Points.Count == 0)
                    continue;

                string sourceType = primitive.SourceType ?? "Point";
                string lower = sourceType.ToLowerInvariant();
                bool sampledCurve = lower.Contains("arc") || lower.Contains("circle");

                if (sampledCurve)
                {
                    AddDisplayPointRow(rows, seen, primitive.Points[0], sourceType);
                    AddDisplayPointRow(rows, seen, primitive.Points[primitive.Points.Count - 1], sourceType);
                    AddDisplayPointRow(rows, seen, primitive.Center, sourceType + " center");
                    continue;
                }

                foreach (var point in primitive.Points)
                    AddDisplayPointRow(rows, seen, point, sourceType);
            }

            return rows;
        }

        private static void AddDisplayPointRow(
            List<CadDocumentService.CadPointData> rows,
            HashSet<string> seen,
            CadDocumentService.CadCoordinate point,
            string lineType)
        {
            if (point == null)
                return;

            string key = MakeGeometryPointKey(point.X, point.Y, point.Z);
            if (!seen.Add(key))
                return;

            rows.Add(new CadDocumentService.CadPointData
            {
                Index = rows.Count + 1,
                LineType = lineType,
                X = point.X,
                Y = point.Y,
                Z = point.Z,
                XDisplay = FormatGeometryNumber(point.X),
                YDisplay = FormatGeometryNumber(point.Y),
                ZDisplay = FormatGeometryNumber(point.Z),
                Key = key
            });
        }

        private static CadDocumentService.CadBounds BuildDisplayBounds(
            List<CadDocumentService.CadPrimitiveData> primitives,
            List<CadDocumentService.CadPointData> points)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double minZ = double.MaxValue;
            double maxZ = double.MinValue;

            void IncludePoint(double x, double y, double z)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                minZ = Math.Min(minZ, z);
                maxZ = Math.Max(maxZ, z);
            }

            if (primitives != null)
            {
                foreach (var primitive in primitives)
                {
                    if (primitive?.Points == null) continue;
                    foreach (var point in primitive.Points)
                        IncludePoint(point.X, point.Y, point.Z);
                    if (primitive.Center != null)
                        IncludePoint(primitive.Center.X, primitive.Center.Y, primitive.Center.Z);
                }
            }

            if (minX == double.MaxValue && points != null)
            {
                foreach (var point in points)
                    IncludePoint(point.X, point.Y, point.Z);
            }

            if (minX == double.MaxValue)
                return new CadDocumentService.CadBounds { Left = 0, Top = 0, Right = 100, Bottom = 100, Width = 100, Height = 100, MinZ = 0, MaxZ = 0 };

            return new CadDocumentService.CadBounds
            {
                Left = minX,
                Top = minY,
                Right = maxX,
                Bottom = maxY,
                Width = Math.Max(maxX - minX, 1.0),
                Height = Math.Max(maxY - minY, 1.0),
                MinZ = minZ == double.MaxValue ? 0.0 : minZ,
                MaxZ = maxZ == double.MinValue ? 0.0 : maxZ
            };
        }

        private static CadDocumentService.CadBounds BuildCadViewBounds(
            CadDocumentService.CadLoadResult doc,
            double workspaceWidthValue,
            double workspaceHeightValue)
        {
            var projection = CreateCadProjection(doc, workspaceWidthValue, workspaceHeightValue);
            if (projection == null)
                return null;

            return new CadDocumentService.CadBounds
            {
                Left = projection.Left,
                Top = projection.Top,
                Right = projection.Right,
                Bottom = projection.Bottom,
                Width = Math.Max(projection.Right - projection.Left, 1.0),
                Height = Math.Max(projection.Bottom - projection.Top, 1.0),
                MinZ = doc?.Bounds?.MinZ ?? 0.0,
                MaxZ = doc?.Bounds?.MaxZ ?? 0.0
            };
        }

        private static CadDocumentService.CadPrimitiveData CloneCadPrimitiveForUi(CadDocumentService.CadPrimitiveData primitive)
        {
            if (primitive == null) return null;

            return new CadDocumentService.CadPrimitiveData
            {
                SourceType = primitive.SourceType,
                Points = primitive.Points == null
                    ? new List<CadDocumentService.CadCoordinate>()
                    : primitive.Points.Select(CloneCadCoordinateForUi).ToList(),
                Center = CloneCadCoordinateForUi(primitive.Center),
                IsCw = primitive.IsCw,
                IsCircle = primitive.IsCircle,
                MCodeValue = primitive.MCodeValue,
                Speed = primitive.Speed,
                Dwell = primitive.Dwell,
                ProcessKind = primitive.ProcessKind,
                WcsIndex = primitive.WcsIndex
            };
        }

        private static CadDocumentService.CadPointData CloneCadPointForUi(CadDocumentService.CadPointData point)
        {
            if (point == null) return null;

            return new CadDocumentService.CadPointData
            {
                Index = point.Index,
                LineType = point.LineType,
                X = point.X,
                Y = point.Y,
                Z = point.Z,
                XDisplay = point.XDisplay,
                YDisplay = point.YDisplay,
                ZDisplay = point.ZDisplay,
                Key = point.Key
            };
        }

        private static CadDocumentService.CadCoordinate CloneCadCoordinateForUi(CadDocumentService.CadCoordinate point)
            => point == null ? null : new CadDocumentService.CadCoordinate(point.X, point.Y, point.Z);

        private static ProcessRow CloneProcessRowForUi(ProcessRow row)
        {
            if (row == null) return null;

            return new ProcessRow
            {
                Key = row.Key,
                MotionType = row.MotionType,
                MCodeValue = row.MCodeValue,
                Dwell = row.Dwell,
                Speed = row.Speed,
                ProcessKind = row.ProcessKind,
                LaserPower = row.LaserPower,
                EndCoordinate = row.EndCoordinate,
                CenterCoordinate = row.CenterCoordinate,
                EndXMm = row.EndXMm,
                EndYMm = row.EndYMm,
                CenterXMm = row.CenterXMm,
                CenterYMm = row.CenterYMm,
                EndZ = row.EndZ,
                WcsIndex = row.WcsIndex
            };
        }

        private static List<GeometryRowViewModel> BuildGeometryRows(CadDocumentService.CadLoadResult doc)
        {
            var rows = new List<GeometryRowViewModel>();
            if (doc?.Primitives == null || doc.Primitives.Count == 0)
                return rows;

            var pointMap = new Dictionary<string, CadDocumentService.CadPointData>(StringComparer.OrdinalIgnoreCase);
            if (doc.Points != null)
            {
                foreach (var point in doc.Points)
                {
                    string key = MakeGeometryPointKey(point.X, point.Y, point.Z);
                    if (!pointMap.ContainsKey(key))
                        pointMap.Add(key, point);
                }
            }

            const int MaxGeometryRows = 100000;
            int fallbackIndex = 1;

            foreach (var primitive in doc.Primitives)
            {
                if (primitive?.Points == null || primitive.Points.Count < 2)
                    continue;

                string lineType = GetGeometryLineType(primitive);
                bool isLinearSegments = string.Equals(lineType, "Line", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(lineType, "Rapid (G0)", StringComparison.OrdinalIgnoreCase);

                if (isLinearSegments)
                {
                    for (int i = 0; i < primitive.Points.Count - 1; i++)
                    {
                        if (rows.Count >= MaxGeometryRows)
                            return rows;

                        var start = primitive.Points[i];
                        var end = primitive.Points[i + 1];
                        rows.Add(CreateGeometryRow(rows.Count + 1, lineType, start, end, null, pointMap, ref fallbackIndex));
                    }
                }
                else
                {
                    if (rows.Count >= MaxGeometryRows)
                        return rows;

                    rows.Add(CreateGeometryRow(
                        rows.Count + 1,
                        lineType,
                        primitive.Points[0],
                        primitive.Points[primitive.Points.Count - 1],
                        primitive.Center,
                        pointMap,
                        ref fallbackIndex));
                }
            }

            return rows;
        }

        private static GeometryRowViewModel CreateGeometryRow(
            int displayIndex,
            string lineType,
            CadDocumentService.CadCoordinate start,
            CadDocumentService.CadCoordinate end,
            CadDocumentService.CadCoordinate center,
            Dictionary<string, CadDocumentService.CadPointData> pointMap,
            ref int fallbackIndex)
        {
            CadDocumentService.CadPointData found = null;
            string key = MakeGeometryPointKey(start.X, start.Y, start.Z);
            bool hasPointIndex = pointMap != null && pointMap.TryGetValue(key, out found);

            return new GeometryRowViewModel
            {
                Index = hasPointIndex ? found.Index : fallbackIndex++,
                LineType = lineType,
                StartX = FormatGeometryNumber(start.X),
                StartY = FormatGeometryNumber(start.Y),
                StartZ = FormatGeometryNumber(start.Z),
                EndX = FormatGeometryNumber(end.X),
                EndY = FormatGeometryNumber(end.Y),
                EndZ = FormatGeometryNumber(end.Z),
                CenterX = center != null ? FormatGeometryNumber(center.X) : string.Empty,
                CenterY = center != null ? FormatGeometryNumber(center.Y) : string.Empty,
                CenterZ = center != null ? FormatGeometryNumber(center.Z) : string.Empty,
                Key = hasPointIndex ? found.Key : string.Empty
            };
        }

        private static string GetGeometryLineType(CadDocumentService.CadPrimitiveData primitive)
        {
            string sourceType = primitive?.SourceType ?? string.Empty;
            string normalized = sourceType.ToLowerInvariant();

            if (normalized.Contains("arc"))
                return "Arc";
            if (normalized.Contains("circle"))
                return "Circle";
            if (normalized.Contains("g0") || normalized.Contains("rapid"))
                return "Rapid (G0)";

            return "Line";
        }

        private static string MakeGeometryPointKey(double x, double y, double z)
            => string.Format(CultureInfo.InvariantCulture, "{0:0.###}|{1:0.###}|{2:0.###}", x, y, z);

        private static string FormatGeometryNumber(double value)
            => value.ToString("0.000", CultureInfo.InvariantCulture);

        private static System.Windows.Media.Geometry BuildCadPreviewGeometry(
            CadDocumentService.CadLoadResult doc,
            CadProjection projection,
            string processKind = null)
        {
            if (doc?.Primitives == null || doc.Primitives.Count == 0 || projection == null)
                return null;

            var geometry = new StreamGeometry { FillRule = FillRule.EvenOdd };
            using (var context = geometry.Open())
            {
                foreach (var primitive in doc.Primitives)
                {
                    if (primitive?.Points == null || primitive.Points.Count < 2)
                        continue;
                    if (!string.IsNullOrWhiteSpace(processKind)
                        && !string.Equals(primitive.ProcessKind, processKind, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (IsRapidPrimitive(primitive))
                        continue;

                    var start = projection.Project(primitive.Points[0].X, primitive.Points[0].Y);
                    var points = new List<System.Windows.Point>(primitive.Points.Count - 1);
                    for (int i = 1; i < primitive.Points.Count; i++)
                    {
                        var pt = primitive.Points[i];
                        points.Add(projection.Project(pt.X, pt.Y));
                    }

                    context.BeginFigure(start, isFilled: false, isClosed: false);
                    context.PolyLineTo(points, isStroked: true, isSmoothJoin: true);
                }
            }
            geometry.Freeze();
            return geometry;
        }

        private static ImageSource BuildCadPreviewImage(CadDocumentService.CadLoadResult doc, CadProjection projection)
        {
            if (doc?.Primitives == null || doc.Primitives.Count == 0 || projection == null)
                return null;

            var geometry = new StreamGeometry { FillRule = FillRule.EvenOdd };
            using (var context = geometry.Open())
            {
                foreach (var primitive in doc.Primitives)
                {
                    if (primitive?.Points == null || primitive.Points.Count < 2)
                        continue;
                    if (IsRapidPrimitive(primitive))
                        continue;

                    var start = projection.Project(primitive.Points[0].X, primitive.Points[0].Y);
                    var points = new List<System.Windows.Point>(primitive.Points.Count - 1);
                    for (int i = 1; i < primitive.Points.Count; i++)
                    {
                        var pt = primitive.Points[i];
                        points.Add(projection.Project(pt.X, pt.Y));
                    }

                    context.BeginFigure(start, isFilled: false, isClosed: false);
                    context.PolyLineTo(points, isStroked: true, isSmoothJoin: true);
                }
            }
            geometry.Freeze();

            var group = new DrawingGroup();
            using (var context = group.Open())
            {
                context.DrawRectangle(
                    Brushes.Transparent,
                    null,
                    new System.Windows.Rect(0.0, 0.0, CadProjection.CanvasWidth, CadProjection.CanvasHeight));

                var pen = new Pen(Brushes.DeepSkyBlue, 0.65);
                pen.Freeze();
                context.DrawGeometry(null, pen, geometry);
            }
            group.Freeze();

            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }

        private static List<CadPrimitiveViewModel> BuildCadPrimitiveLines(CadDocumentService.CadLoadResult doc, CadProjection projection)
        {
            var lines = new List<CadPrimitiveViewModel>();
            if (doc?.Primitives == null || doc.Primitives.Count == 0 || projection == null)
                return lines;

            foreach (var primitive in doc.Primitives.Take(50000))
            {
                if (primitive.Points == null || primitive.Points.Count < 2)
                    continue;
                if (IsRapidPrimitive(primitive))
                    continue;

                var pointCollection = new PointCollection();
                foreach (var pt in primitive.Points)
                {
                    pointCollection.Add(projection.Project(pt.X, pt.Y));
                }
                pointCollection.Freeze();

                lines.Add(new CadPrimitiveViewModel
                {
                    Points = pointCollection,
                    Stroke = Brushes.DeepSkyBlue,
                    StrokeThickness = 0.65
                });
            }

            return lines;
        }

        private static bool IsRapidPrimitive(CadDocumentService.CadPrimitiveData primitive)
        {
            string sourceType = primitive?.SourceType ?? string.Empty;
            return sourceType.IndexOf("G0", StringComparison.OrdinalIgnoreCase) >= 0
                || sourceType.IndexOf("Rapid", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static List<CadLimitAreaViewModel> BuildCadLimitAreas(
            double workspaceWidthValue,
            double workspaceHeightValue,
            CadProjection projection)
        {
            var areas = new List<CadLimitAreaViewModel>();
            if (projection == null || workspaceWidthValue <= 0.0 || workspaceHeightValue <= 0.0)
                return areas;

            var points = new PointCollection
            {
                projection.Project(0.0, 0.0),
                projection.Project(workspaceWidthValue, 0.0),
                projection.Project(workspaceWidthValue, workspaceHeightValue),
                projection.Project(0.0, workspaceHeightValue)
            };
            points.Freeze();

            var fill = new SolidColorBrush(Color.FromArgb(22, 70, 170, 255));
            fill.Freeze();

            var dash = new DoubleCollection { 6.0, 4.0 };
            dash.Freeze();

            areas.Add(new CadLimitAreaViewModel
            {
                Points = points,
                Fill = fill,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 0.85,
                StrokeDashArray = dash
            });

            return areas;
        }

        private static List<CadAxisLineViewModel> BuildCadAxisLines(CadDocumentService.CadLoadResult doc, CadProjection projection)
        {
            var lines = new List<CadAxisLineViewModel>();
            if (doc == null || projection == null)
                return lines;

            const double axisVectorLength = 92.0;
            var origin = projection.Project(0.0, 0.0);
            var xEnd = new System.Windows.Point(
                Clamp(origin.X + axisVectorLength, 10.0, CadProjection.CanvasWidth - 12.0),
                origin.Y);
            var yEnd = new System.Windows.Point(
                origin.X,
                Clamp(origin.Y - axisVectorLength, 10.0, CadProjection.CanvasHeight - 12.0));
            Brush xBrush = Brushes.IndianRed;
            Brush yBrush = Brushes.MediumSeaGreen;

            lines.Add(new CadAxisLineViewModel
            {
                X1 = origin.X,
                Y1 = origin.Y,
                X2 = xEnd.X,
                Y2 = xEnd.Y,
                Stroke = xBrush,
                StrokeThickness = 0.9,
                Opacity = 0.85
            });
            lines.Add(new CadAxisLineViewModel
            {
                X1 = xEnd.X,
                Y1 = xEnd.Y,
                X2 = xEnd.X - 12.0,
                Y2 = xEnd.Y - 5.0,
                Stroke = xBrush,
                StrokeThickness = 0.9,
                Opacity = 0.85
            });
            lines.Add(new CadAxisLineViewModel
            {
                X1 = xEnd.X,
                Y1 = xEnd.Y,
                X2 = xEnd.X - 12.0,
                Y2 = xEnd.Y + 5.0,
                Stroke = xBrush,
                StrokeThickness = 0.9,
                Opacity = 0.85
            });
            lines.Add(new CadAxisLineViewModel
            {
                X1 = origin.X,
                Y1 = origin.Y,
                X2 = yEnd.X,
                Y2 = yEnd.Y,
                Stroke = yBrush,
                StrokeThickness = 0.9,
                Opacity = 0.85
            });
            lines.Add(new CadAxisLineViewModel
            {
                X1 = yEnd.X,
                Y1 = yEnd.Y,
                X2 = yEnd.X - 5.0,
                Y2 = yEnd.Y + 12.0,
                Stroke = yBrush,
                StrokeThickness = 0.9,
                Opacity = 0.85
            });
            lines.Add(new CadAxisLineViewModel
            {
                X1 = yEnd.X,
                Y1 = yEnd.Y,
                X2 = yEnd.X + 5.0,
                Y2 = yEnd.Y + 12.0,
                Stroke = yBrush,
                StrokeThickness = 0.9,
                Opacity = 0.85
            });

            return lines;
        }

        private static List<CadAxisLabelViewModel> BuildCadAxisLabels(CadDocumentService.CadLoadResult doc, CadProjection projection)
        {
            var labels = new List<CadAxisLabelViewModel>();
            if (doc == null || projection == null)
                return labels;

            var origin = projection.Project(0.0, 0.0);
            const double axisVectorLength = 92.0;
            var xEnd = new System.Windows.Point(
                Clamp(origin.X + axisVectorLength, 10.0, CadProjection.CanvasWidth - 12.0),
                origin.Y);
            var yEnd = new System.Windows.Point(
                origin.X,
                Clamp(origin.Y - axisVectorLength, 10.0, CadProjection.CanvasHeight - 12.0));

            labels.Add(new CadAxisLabelViewModel
            {
                X = Clamp(xEnd.X - 22.0, 4.0, CadProjection.CanvasWidth - 24.0),
                Y = Clamp(origin.Y + 10.0, 4.0, CadProjection.CanvasHeight - 24.0),
                Text = "X",
                Foreground = Brushes.IndianRed
            });
            labels.Add(new CadAxisLabelViewModel
            {
                X = Clamp(origin.X + 12.0, 4.0, CadProjection.CanvasWidth - 24.0),
                Y = Clamp(yEnd.Y + 18.0, 4.0, CadProjection.CanvasHeight - 24.0),
                Text = "Y",
                Foreground = Brushes.MediumSeaGreen
            });

            return labels;
        }

        private static List<CadTrackingPointViewModel> BuildRobotTrackingPoints(
            CadDocumentService.CadLoadResult doc,
            double workspaceWidthValue,
            double workspaceHeightValue,
            bool connected,
            int rawX,
            int rawY)
        {
            var points = new List<CadTrackingPointViewModel>();

            var projection = doc == null
                ? new CadProjection(0.0, 0.0, Math.Max(workspaceWidthValue, 1.0), Math.Max(workspaceHeightValue, 1.0))
                : CreateCadProjection(doc, workspaceWidthValue, workspaceHeightValue);
            if (projection == null)
                return points;

            double robotX = rawX / QD75BufferWriter.CoordinateMultiplier;
            double robotY = rawY / QD75BufferWriter.CoordinateMultiplier;
            var projected = projection.Project(robotX, robotY);

            points.Add(new CadTrackingPointViewModel
            {
                X = projected.X,
                Y = projected.Y,
                Size = 4.0,
                Fill = Brushes.Lime,
                Stroke = Brushes.White,
                Label = "",
                ToolTip = string.Format(CultureInfo.InvariantCulture, "Robot actual position: X={0:0.0000} mm, Y={1:0.0000} mm", robotX, robotY)
            });

            return points;
        }

        private static CadProjection CreateCadProjection(
            CadDocumentService.CadLoadResult doc,
            double workspaceWidthValue,
            double workspaceHeightValue)
        {
            if (workspaceWidthValue > 0.0 && workspaceHeightValue > 0.0)
                return new CadProjection(0.0, 0.0, workspaceWidthValue, workspaceHeightValue);

            if (doc?.Bounds == null)
                return null;

            double left = doc.Bounds.Left;
            double top = doc.Bounds.Top;
            double right = doc.Bounds.Right;
            double bottom = doc.Bounds.Bottom;

            if (right <= left) right = left + Math.Max(doc.Bounds.Width, 1.0);
            if (bottom <= top) bottom = top + Math.Max(doc.Bounds.Height, 1.0);

            Include(ref left, ref top, ref right, ref bottom, 0.0, 0.0);

            if (workspaceWidthValue > 0.0)
            {
                Include(ref left, ref top, ref right, ref bottom, workspaceWidthValue, 0.0);
                Include(ref left, ref top, ref right, ref bottom, workspaceWidthValue, bottom);
            }

            if (workspaceHeightValue > 0.0)
            {
                Include(ref left, ref top, ref right, ref bottom, 0.0, workspaceHeightValue);
                Include(ref left, ref top, ref right, ref bottom, right, workspaceHeightValue);
            }

            return new CadProjection(left, top, right, bottom);
        }

        private static void Include(ref double left, ref double top, ref double right, ref double bottom, double x, double y)
        {
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private sealed class CadProjection
        {
            public const double CanvasWidth = 1000.0;
            public const double CanvasHeight = 620.0;
            private const double Padding = 24.0;

            public CadProjection(double left, double top, double right, double bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
                Width = Math.Max(right - left, 0.001);
                Height = Math.Max(bottom - top, 0.001);
                Scale = Math.Min(
                    (CanvasWidth - Padding * 2.0) / Width,
                    (CanvasHeight - Padding * 2.0) / Height);
                ContentWidth = Width * Scale;
                ContentHeight = Height * Scale;
                MarginX = (CanvasWidth - ContentWidth) / 2.0;
                MarginY = (CanvasHeight - ContentHeight) / 2.0;
            }

            public double Left { get; }
            public double Top { get; }
            public double Right { get; }
            public double Bottom { get; }
            private double Width { get; }
            private double Height { get; }
            private double Scale { get; }
            private double ContentWidth { get; }
            private double ContentHeight { get; }
            private double MarginX { get; }
            private double MarginY { get; }

            public System.Windows.Point Project(double x, double y)
            {
                double px = MarginX + (x - Left) * Scale;
                double py = MarginY + ContentHeight - (y - Top) * Scale;
                return new System.Windows.Point(px, py);
            }
        }

        private static string ApplyOffsetToCoord(string coord, double ox, double oy)
        {
            if (string.IsNullOrWhiteSpace(coord)) return string.Empty;

            string[] parts = coord.Split(';');
            if (parts.Length < 2) return coord;

            double x;
            double y;
            if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out x))
                return coord;
            if (!double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                return coord;

            return string.Format(CultureInfo.InvariantCulture, "{0:0.###};{1:0.###}", x + ox, y + oy);
        }

        private async Task PushTelemetryStateAsync()
        {
            var comm = plcComm;
            bool connected = comm != null && comm.IsConnected;
            var regs = telemetryRegisters.ToArray();
            var bufs = telemetryBuffers.Select(buf => new TelemetryBuffer { Path = buf.Path, Length = buf.Length }).ToArray();

            var model = await Task.Run(() =>
            {
                var dValues = new List<TelemetryRegisterViewModel>();
                var buffers = new List<TelemetryBufferViewModel>();

                foreach (var reg in regs)
                {
                    if (connected)
                    {
                        try
                        {
                            int v = comm.ReadDeviceValue(reg);
                            dValues.Add(new TelemetryRegisterViewModel { Register = reg, Value = v.ToString(CultureInfo.InvariantCulture), Status = "OK" });
                        }
                        catch (Exception ex)
                        {
                            dValues.Add(new TelemetryRegisterViewModel { Register = reg, Value = "--", Status = ex.Message });
                        }
                    }
                    else
                    {
                        dValues.Add(new TelemetryRegisterViewModel { Register = reg, Value = "--", Status = "Disconnected" });
                    }
                }

                foreach (var buf in bufs)
                {
                    if (connected)
                    {
                        try
                        {
                            int[] arr = comm.ReadDeviceRange(buf.Path, buf.Length);
                            buffers.Add(new TelemetryBufferViewModel { Path = buf.Path, Values = string.Join(", ", arr), Status = "OK" });
                        }
                        catch (Exception ex)
                        {
                            buffers.Add(new TelemetryBufferViewModel { Path = buf.Path, Values = "", Status = ex.Message });
                        }
                    }
                    else
                    {
                        buffers.Add(new TelemetryBufferViewModel { Path = buf.Path, Values = "", Status = "Disconnected" });
                    }
                }

                return new { dValues, buffers };
            });

            await RunOnUiAsync(() =>
            {
                ReplaceCollection(ui.TelemetryRegisters, model.dValues);
                ReplaceCollection(ui.TelemetryBuffers, model.buffers);
            });
        }

        private async Task PushLogsStateAsync()
        {
            List<LogRowViewModel> outLogs;
            int snapVersion;

            lock (logsLock)
            {
                snapVersion = logVersion;
                outLogs = logs.Select(l => new LogRowViewModel
                {
                    Timestamp = l.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    Direction = l.Direction,
                    Address = l.Address,
                    Value = l.Value,
                    Status = l.Status,
                    Message = l.Message
                }).ToList();
            }

            await RunOnUiAsync(() => ReplaceCollection(ui.Logs, outLogs));
            Volatile.Write(ref logPushedVersion, snapVersion);
        }

        protected Task NotifyAsync(string kind, string title, string message)
            => PostToUiAsync("notify", new { kind, title, message });

        protected Task LogUIAsync(string title, string message)
            => PostToUiAsync("log", new { title, message });

        protected Task SendProgressAsync(bool visible, int percent = 0)
            => PostToUiAsync("progress", new { visible, percent });

        private void AddLogEntry(string address, string value,
            string direction = "Write", string status = "OK", string message = null)
        {
            try
            {
                lock (logsLock)
                {
                    logs.Insert(0, new LogEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        Direction = direction,
                        Address = address,
                        Value = value,
                        Status = status,
                        Message = message
                    });

                    if (logs.Count > 500) logs.RemoveRange(500, logs.Count - 500);
                    Interlocked.Increment(ref logVersion);
                }

                ScheduleLogUiRefresh();
            }
            catch { }
        }

        private Task HandleClearLogsAsync()
        {
            lock (logsLock)
            {
                logs.Clear();
                Interlocked.Increment(ref logVersion);
            }

            return PushLogsStateAsync();
        }

        private void ScheduleLogUiRefresh()
        {
            if (isClosing || !webReady)
                return;

            if (Interlocked.CompareExchange(ref logUiRefreshPending, 1, 0) != 0)
                return;

            _ = DebouncedPushLogsStateAsync();
        }

        private async Task DebouncedPushLogsStateAsync()
        {
            try
            {
                while (!isClosing)
                {
                    await Task.Delay(PerformanceTuning.LogUiDebounceMs);
                    await PushLogsStateAsync();

                    if (Volatile.Read(ref logPushedVersion) == Volatile.Read(ref logVersion))
                        break;
                }
            }
            catch
            {
            }
            finally
            {
                Interlocked.Exchange(ref logUiRefreshPending, 0);
                if (!isClosing && Volatile.Read(ref logPushedVersion) != Volatile.Read(ref logVersion))
                    ScheduleLogUiRefresh();
            }
        }

        private Task PostToUiAsync(string type, object payload)
        {
            if (isClosing || !webReady) return Task.CompletedTask;

            return RunOnUiAsync(() =>
            {
                if (type == "progress")
                {
                    ui.ProgressVisible = GetPayloadBool(payload, "visible");
                    ui.ProgressPercent = GetPayloadInt(payload, "percent", 0);
                    _ = PublishMonitorStateToMqttAsync(PlcConnectionGuard.CanUsePlc(plcComm != null, plcComm != null && plcComm.IsConnected));
                    return;
                }

                string kind = GetPayloadString(payload, "kind", "info");
                string title = GetPayloadString(payload, "title", type);
                string message = GetPayloadString(payload, "message", "");
                string text = string.IsNullOrWhiteSpace(message) ? title : $"{title}: {message}";

                ui.ActiveNotice = text;
                ui.Events.Insert(0, new UiEventViewModel
                {
                    Time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                    Kind = kind,
                    Title = title,
                    Message = message
                });

                if (ui.Events.Count > 200)
                    ui.Events.RemoveAt(ui.Events.Count - 1);
            });
        }

        private Task RunOnUiAsync(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            return tcs.Task;
        }

        private static void ReplaceCollection<T>(System.Collections.ObjectModel.ObservableCollection<T> target, IEnumerable<T> source)
        {
            if (target is BulkObservableCollection<T> bulkTarget)
            {
                bulkTarget.ReplaceWith(source);
                return;
            }

            target.Clear();
            foreach (T item in source)
                target.Add(item);
        }

        private static string GetPayloadString(object payload, string name, string fallback)
        {
            if (payload == null) return fallback;
            var prop = payload.GetType().GetProperty(name);
            object value = prop?.GetValue(payload, null);
            return value == null ? fallback : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static int GetPayloadInt(object payload, string name, int fallback)
        {
            if (payload == null) return fallback;
            var prop = payload.GetType().GetProperty(name);
            object value = prop?.GetValue(payload, null);
            if (value == null) return fallback;
            try { return Convert.ToInt32(value, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        private static bool GetPayloadBool(object payload, string name)
        {
            if (payload == null) return false;
            var prop = payload.GetType().GetProperty(name);
            object value = prop?.GetValue(payload, null);
            if (value == null) return false;
            try { return Convert.ToBoolean(value, CultureInfo.InvariantCulture); }
            catch { return false; }
        }

        private static Dictionary<string, object> GetMap(Dictionary<string, object> source, string key)
        {
            object value;
            if (source == null || !source.TryGetValue(key, out value))
                return new Dictionary<string, object>();
            return value as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        private static string GetString(Dictionary<string, object> source, string key, string fallback = "")
        {
            object value;
            if (source == null || !source.TryGetValue(key, out value) || value == null)
                return fallback;
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback;
        }

        private static int GetInt(Dictionary<string, object> source, string key, int fallback = 0)
        {
            object value;
            if (source == null || !source.TryGetValue(key, out value) || value == null) return fallback;
            if (value is int) return (int)value;
            if (value is long) return Convert.ToInt32((long)value, CultureInfo.InvariantCulture);
            if (value is double) return Convert.ToInt32((double)value, CultureInfo.InvariantCulture);
            int parsed;
            return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed : fallback;
        }

        private static double GetDouble(Dictionary<string, object> source, string key, double fallback = 0.0)
        {
            object value;
            if (source == null || !source.TryGetValue(key, out value) || value == null) return fallback;
            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }
    }
}
