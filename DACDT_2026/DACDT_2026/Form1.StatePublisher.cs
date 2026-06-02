using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DACDT_2026
{
    public partial class Form1
    {
        private Task PushAllStateAsync()
            => Task.WhenAll(PushControlStateAsync(), PushDxfStateAsync(), PushTelemetryStateAsync(), PushLogsStateAsync());

        private static string FormatPositionMm(int rawValue) => QD75BufferWriter.FormatPositionMm(rawValue);
        private static string FormatSpeedMm(int rawValue) => QD75BufferWriter.FormatSpeedMm(rawValue);
        private static string FormatAxisStatus(int status) => QD75BufferWriter.FormatAxisStatus(status);

        private Task PushControlStateAsync()
        {
            bool connected = plcComm != null && plcComm.IsConnected;
            string dash = "--";

            return RunOnUiAsync(() =>
            {
                ui.CurrentView = currentView;
                ui.CurrentTheme = currentTheme;
                ui.IsConnected = connected;
                ui.ConnectionBanner = connectionBanner;
                ui.ConnectionMeta = $"MX Component logical station: {logicalStation}";
                ui.ConnectionButtonText = connected ? "DISCONNECT PLC Q" : "CONNECT PLC Q";
                ui.JogSpeedD406 = currentJogSpeedD406;

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
                    axis.ErrorCode = connected ? axErrorCode[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.ErrorCodeAddr = $"U0\\G{mb + OffErrorCode}";
                    axis.WarningCode = connected ? axWarningCode[i].ToString(CultureInfo.InvariantCulture) : dash;
                    axis.WarningCodeAddr = $"U0\\G{mb + OffWarningCode}";
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

                ReplaceCollection(ui.CadTrackingPoints, BuildRobotTrackingPoints(
                    activeCadDocument,
                    workspaceWidth,
                    workspaceHeight,
                    connected,
                    axCurrentPos[0],
                    axCurrentPos[1]));
            });
        }

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
            var snapConnected = plcComm != null && plcComm.IsConnected;
            var snapRobotRawX = axCurrentPos[0];
            var snapRobotRawY = axCurrentPos[1];
            var snapCurrentView = currentView;
            var snapCurrentTheme = currentTheme;
            var snapGlobalSpeed = globalSpeed;
            var snapGlobalSpeedM3 = globalSpeedM3;
            var snapRapidSpeed = rapidSpeed;
            var snapGlobalDwellM3 = globalDwellM3;
            var snapGlobalDwellM4 = globalDwellM4;
            var snapActiveWcs = activeWcs;

            var model = await Task.Run(() =>
            {
                var snapDoc = CloneCadDocumentForUi(snapDocSource);
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
                        Key = pt.Key
                    }).ToList();

                var geometryRows = BuildGeometryRows(snapDoc);

                bool isGcodeKind = string.Equals(snapKind, "GCODE", StringComparison.OrdinalIgnoreCase);
                var rows = snapRows.Select((row, rowIndex) =>
                {
                    double rowOx;
                    double rowOy;
                    if (isGcodeKind)
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
                        EndCoordinate = ApplyOffsetToCoord(row.EndCoordinate, rowOx, rowOy),
                        CenterCoordinate = ApplyOffsetToCoord(row.CenterCoordinate, rowOx, rowOy),
                        EndZ = row.EndZ.ToString("0.###", CultureInfo.InvariantCulture)
                    };
                }).ToList();

                var projection = CreateCadProjection(snapDoc, snapWorkspaceWidth, snapWorkspaceHeight);
                var primitiveLines = BuildCadPrimitiveLines(snapDoc, projection);
                var limitAreas = BuildCadLimitAreas(snapWorkspaceWidth, snapWorkspaceHeight, projection);
                var axisLines = BuildCadAxisLines(snapDoc, projection);
                var axisLabels = BuildCadAxisLabels(snapDoc, projection);
                var trackingPoints = BuildRobotTrackingPoints(
                    snapDoc,
                    snapWorkspaceWidth,
                    snapWorkspaceHeight,
                    snapConnected,
                    snapRobotRawX,
                    snapRobotRawY);
                return new { doc = snapDoc, points, geometryRows, rows, primitiveLines, limitAreas, axisLines, axisLabels, trackingPoints };
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
                ui.GlobalSpeedInput = snapGlobalSpeed;
                ui.GlobalSpeedM3Input = snapGlobalSpeedM3;
                ui.RapidSpeedInput = snapRapidSpeed;
                ui.GlobalDwellM3Input = snapGlobalDwellM3;
                ui.GlobalDwellM4Input = snapGlobalDwellM4;
                ui.OffsetXInput = snapOx;
                ui.OffsetYInput = snapOy;
                ui.WorkspaceWidthInput = snapWorkspaceWidth;
                ui.WorkspaceHeightInput = snapWorkspaceHeight;
                ui.ActiveWcs = snapActiveWcs;
                int wIdx = GetWcsIndex(snapActiveWcs);
                ui.WcsOffsetXInput = snapWcsOffsetX[wIdx];
                ui.WcsOffsetYInput = snapWcsOffsetY[wIdx];
                ui.SelectedPointKey = snapPointKey;

                ReplaceCollection(ui.CadPoints, model.points);
                ReplaceCollection(ui.GeometryRows, model.geometryRows);
                ReplaceCollection(ui.ProcessRows, model.rows);
                ReplaceCollection(ui.CadPrimitives, model.primitiveLines);
                ReplaceCollection(ui.CadLimitAreas, model.limitAreas);
                ReplaceCollection(ui.CadAxisLines, model.axisLines);
                ReplaceCollection(ui.CadAxisLabels, model.axisLabels);
                ReplaceCollection(ui.CadTrackingPoints, model.trackingPoints);
                ReplaceCollection(ui.Profiles, snapProfiles);
            });
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

        private static List<CadPrimitiveViewModel> BuildCadPrimitiveLines(CadDocumentService.CadLoadResult doc, CadProjection projection)
        {
            var lines = new List<CadPrimitiveViewModel>();
            if (doc?.Primitives == null || doc.Primitives.Count == 0 || projection == null)
                return lines;

            foreach (var primitive in doc.Primitives.Take(5000))
            {
                if (primitive.Points == null || primitive.Points.Count < 2)
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
                X = Clamp(xEnd.X - 18.0, 4.0, CadProjection.CanvasWidth - 24.0),
                Y = Clamp(xEnd.Y - 24.0, 4.0, CadProjection.CanvasHeight - 24.0),
                Text = "X",
                Foreground = Brushes.IndianRed
            });
            labels.Add(new CadAxisLabelViewModel
            {
                X = Clamp(yEnd.X + 8.0, 4.0, CadProjection.CanvasWidth - 24.0),
                Y = Clamp(yEnd.Y + 2.0, 4.0, CadProjection.CanvasHeight - 24.0),
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
            if (!connected)
                return points;

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
                Size = 14.0,
                Fill = Brushes.Lime,
                Stroke = Brushes.White,
                Label = "Robot",
                ToolTip = string.Format(CultureInfo.InvariantCulture, "Robot actual position: X={0:0.0000} mm, Y={1:0.0000} mm", robotX, robotY)
            });

            return points;
        }

        private static CadProjection CreateCadProjection(
            CadDocumentService.CadLoadResult doc,
            double workspaceWidthValue,
            double workspaceHeightValue)
        {
            if (doc == null)
                return null;

            double left = doc.Bounds.Left;
            double top = doc.Bounds.Top;
            double right = doc.Bounds.Right;
            double bottom = doc.Bounds.Bottom;

            if (right <= left) right = left + Math.Max(doc.Bounds.Width, 1.0);
            if (bottom <= top) bottom = top + Math.Max(doc.Bounds.Height, 1.0);

            Include(ref left, ref top, ref right, ref bottom, 0.0, 0.0);

            if (workspaceWidthValue > 0.0 || workspaceHeightValue > 0.0)
            {
                double w = Math.Max(workspaceWidthValue, 0.0);
                double h = Math.Max(workspaceHeightValue, 0.0);
                Include(ref left, ref top, ref right, ref bottom, w, 0.0);
                Include(ref left, ref top, ref right, ref bottom, 0.0, h);
                Include(ref left, ref top, ref right, ref bottom, w, h);
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
            public const double CanvasHeight = 560.0;
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

        private Task PushTelemetryStateAsync()
        {
            bool connected = plcComm != null && plcComm.IsConnected;
            var dValues = new List<TelemetryRegisterViewModel>();
            var buffers = new List<TelemetryBufferViewModel>();

            foreach (var reg in telemetryRegisters)
            {
                if (connected)
                {
                    try
                    {
                        int v = plcComm.ReadDeviceValue(reg);
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

            foreach (var buf in telemetryBuffers)
            {
                if (connected)
                {
                    try
                    {
                        int[] arr = plcComm.ReadDeviceRange(buf.Path, buf.Length);
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

            return RunOnUiAsync(() =>
            {
                ReplaceCollection(ui.TelemetryRegisters, dValues);
                ReplaceCollection(ui.TelemetryBuffers, buffers);
            });
        }

        private Task PushLogsStateAsync()
        {
            var outLogs = logs.Select(l => new LogRowViewModel
            {
                Timestamp = l.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                Direction = l.Direction,
                Address = l.Address,
                Value = l.Value,
                Status = l.Status,
                Message = l.Message
            }).ToList();

            return RunOnUiAsync(() => ReplaceCollection(ui.Logs, outLogs));
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
                _ = PushLogsStateAsync();
            }
            catch { }
        }

        private Task HandleClearLogsAsync()
        {
            logs.Clear();
            return PushLogsStateAsync();
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
