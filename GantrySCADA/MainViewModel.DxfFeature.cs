using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
        private string _dxfFilePath = string.Empty;
        public string DxfFilePath { get => _dxfFilePath; set => SetProperty(ref _dxfFilePath, value); }

        private string _dxfSummary = "DXF chưa được nạp.";
        public string DxfSummary { get => _dxfSummary; private set => SetProperty(ref _dxfSummary, value); }

        private int _dxfEntityCount;
        public int DxfEntityCount { get => _dxfEntityCount; set => SetProperty(ref _dxfEntityCount, value); }

        private int _dxfLayerCount;
        public int DxfLayerCount { get => _dxfLayerCount; set => SetProperty(ref _dxfLayerCount, value); }

        private int _dxfSampleCount;
        public int DxfSampleCount { get => _dxfSampleCount; set => SetProperty(ref _dxfSampleCount, value); }

        private string _dxfPreviewPathData = "";
        public string DxfPreviewPathData { get => _dxfPreviewPathData; set => SetProperty(ref _dxfPreviewPathData, value); }

        private List<string> _dxfLayers = new();
        public List<string> DxfLayers { get => _dxfLayers; set => SetProperty(ref _dxfLayers, value); }

        private string _selectedDxfLayer = "All";
        public string SelectedDxfLayer { get => _selectedDxfLayer; set => SetProperty(ref _selectedDxfLayer, value); }

        public class DxfContourInfo
        {
            public string Name { get; set; } = "";
            public double Length { get; set; }
            public bool IsClosed { get; set; }
            public List<(double X, double Y)> Points { get; set; } = new();
            public string Layer { get; set; } = "";
            public bool HasCenter { get; set; }
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public bool IsCircle { get; set; }
            public bool IsArc { get; set; }
            public double Radius { get; set; }
            public double StartAngleDeg { get; set; }
            public double EndAngleDeg { get; set; }
            public bool ArcClockwise { get; set; }
            public double ArcSweepDeg { get; set; }
            public bool UsePump { get; set; } = true;
        }

        private List<DxfContourInfo> _dxfContours = new();
        public List<DxfContourInfo> DxfContours { get => _dxfContours; set => SetProperty(ref _dxfContours, value); }

        private DxfContourInfo? _selectedContour;
        public DxfContourInfo? SelectedContour { get => _selectedContour; set => SetProperty(ref _selectedContour, value); }

        private int _selectedPointIndex = -1;
        public int SelectedPointIndex { get => _selectedPointIndex; set => SetProperty(ref _selectedPointIndex, value); }

        // Robot Coordinate Setup
        private double _dxfScaleX = 1.0;
        public double DxfScaleX { get => _dxfScaleX; set => SetProperty(ref _dxfScaleX, value); }
        private double _dxfScaleY = 1.0;
        public double DxfScaleY { get => _dxfScaleY; set => SetProperty(ref _dxfScaleY, value); }
        private double _dxfOffsetX = 0.0;
        public double DxfOffsetX { get => _dxfOffsetX; set => SetProperty(ref _dxfOffsetX, value); }
        private double _dxfOffsetY = 0.0;
        public double DxfOffsetY { get => _dxfOffsetY; set => SetProperty(ref _dxfOffsetY, value); }
        private bool _dxfMirrorY = false;
        public bool DxfMirrorY { get => _dxfMirrorY; set => SetProperty(ref _dxfMirrorY, value); }
        private string _dxfOriginMode = "Min"; // Min, Center, Custom
        public string DxfOriginMode { get => _dxfOriginMode; set => SetProperty(ref _dxfOriginMode, value); }

        private double _dxfXMin, _dxfXMax, _dxfYMin, _dxfYMax, _dxfZSafe = 10.0;
        public double DxfXMin { get => _dxfXMin; set => SetProperty(ref _dxfXMin, value); }
        public double DxfXMax { get => _dxfXMax; set => SetProperty(ref _dxfXMax, value); }
        public double DxfYMin { get => _dxfYMin; set => SetProperty(ref _dxfYMin, value); }
        public double DxfYMax { get => _dxfYMax; set => SetProperty(ref _dxfYMax, value); }
        public double DxfZSafe { get => _dxfZSafe; set => SetProperty(ref _dxfZSafe, value); }

        private double _dxfSvgScale = 1.0;
        public double DxfSvgScale { get => _dxfSvgScale; set => SetProperty(ref _dxfSvgScale, value); }
        private double _dxfSvgMinX;
        public double DxfSvgMinX { get => _dxfSvgMinX; set => SetProperty(ref _dxfSvgMinX, value); }
        private double _dxfSvgMinY;
        public double DxfSvgMinY { get => _dxfSvgMinY; set => SetProperty(ref _dxfSvgMinY, value); }

        // Trajectory Parameters
        private int _dxfStartPointIndex = 1;
        public int DxfStartPointIndex { get => _dxfStartPointIndex; set => SetProperty(ref _dxfStartPointIndex, value); }
        private int _dxfGlueStartIndex;
        public int DxfGlueStartIndex { get => _dxfGlueStartIndex; set => SetProperty(ref _dxfGlueStartIndex, value); }
        private int _dxfGlueEndIndex;
        public int DxfGlueEndIndex { get => _dxfGlueEndIndex; set => SetProperty(ref _dxfGlueEndIndex, value); }
        private double _dxfZDown = -5.0;
        public double DxfZDown { get => _dxfZDown; set => SetProperty(ref _dxfZDown, value); }
        private uint _dxfDefaultSpeed = 1000;
        public uint DxfDefaultSpeed { get => _dxfDefaultSpeed; set => SetProperty(ref _dxfDefaultSpeed, value); }

        // Run State
        private bool _isDxfRunning;
        public bool IsDxfRunning { get => _isDxfRunning; set => SetProperty(ref _isDxfRunning, value); }
        private bool _isDxfPaused;
        public bool IsDxfPaused { get => _isDxfPaused; set => SetProperty(ref _isDxfPaused, value); }
        private bool _isDryRun;
        public bool IsDryRun { get => _isDryRun; set => SetProperty(ref _isDryRun, value); }
        private int _dxfProgressCurrent, _dxfProgressTotal;
        public int DxfProgressCurrent { get => _dxfProgressCurrent; set => SetProperty(ref _dxfProgressCurrent, value); }
        public int DxfProgressTotal { get => _dxfProgressTotal; set => SetProperty(ref _dxfProgressTotal, value); }
        private double _dxfCurrX, _dxfCurrY, _dxfCurrZ, _dxfCurrFeed;
        public double DxfCurrX { get => _dxfCurrX; set => SetProperty(ref _dxfCurrX, value); }
        public double DxfCurrY { get => _dxfCurrY; set => SetProperty(ref _dxfCurrY, value); }
        public double DxfCurrZ { get => _dxfCurrZ; set => SetProperty(ref _dxfCurrZ, value); }
        public double DxfCurrFeed { get => _dxfCurrFeed; set => SetProperty(ref _dxfCurrFeed, value); }
        private bool _dxfStepMode;
        public bool DxfStepMode { get => _dxfStepMode; set => SetProperty(ref _dxfStepMode, value); }

        private void InitializeDxfFeature()
        {
            DxfSummary = "DXF feature ready.";
        }

        public event EventHandler<string>? DxfDownloadComplete;

        private void OnDxfDownloadComplete(string message)
        {
            DxfDownloadComplete?.Invoke(this, message);
        }

        public async Task DownloadTrajectoryToPlcAsync(CancellationToken cancellationToken = default)
        {
            if (DxfContours == null || DxfContours.Count == 0)
            {
                AddLog("PC", "warning", "Không có dữ liệu DXF để truyền xuống PLC.");
                return;
            }

            IsDxfSending = true;
            IsDxfRunning = true;
            IsDxfPaused = false;
            DxfSendStatus = "Chuẩn bị...";
            AddLog("UI", "info", "Bắt đầu truyền quỹ đạo DXF (Async)...");

            try
            {
                // 1. Biên dịch quỹ đạo trên thread riêng để không chặn UI
                DxfSendStatus = "Đang biên dịch...";
                var (a1Arr, a2Arr, pointCount) = await Task.Run(() => CompileDxfTrajectory(cancellationToken), cancellationToken);

                if (pointCount == 0)
                {
                    DxfSendStatus = "Lỗi: Không có điểm.";
                    return;
                }

                // 2. Ghi xuống PLC
                DxfSendStatus = $"Đang truyền {pointCount} điểm...";
                await Task.Run(() => SendTrajectoryToPLC(a1Arr, a2Arr, pointCount), cancellationToken);

                // 3. Hoàn thành ngay (Bỏ logic đợi M300)
                DxfSendStatus = "Hoàn thành";
                AddLog("PLC", "success", $"Đã truyền thành công {pointCount} điểm quỹ đạo xuống PLC.");
                OnDxfDownloadComplete($"Đã truyền thành công {pointCount} điểm quỹ đạo!");
            }
            catch (OperationCanceledException)
            {
                DxfSendStatus = "Đã hủy";
                AddLog("UI", "warning", "Hủy truyền quỹ đạo DXF.");
            }
            catch (Exception ex)
            {
                DxfSendStatus = "Lỗi hệ thống";
                AddLog("PC", "error", $"Lỗi khi truyền quỹ đạo Async: {ex.Message}");
            }
            finally
            {
                IsDxfSending = false;
                IsDxfRunning = false;
            }
        }

        private (int[] a1Arr, int[] a2Arr, int pointCount) CompileDxfTrajectory(CancellationToken ct)
        {
            List<int> axis1Data = new List<int>();
            List<int> axis2Data = new List<int>();
            int pointCount = 0;
            int absolutePointIndex = 1;

            foreach (var contour in DxfContours)
            {
                ct.ThrowIfCancellationRequested();
                if (contour.Points == null || contour.Points.Count == 0) continue;

                // Travel move (G0)
                ushort travelCmd = 0xD00A;
                ushort travelMCode = 0;
                if (absolutePointIndex >= DxfStartPointIndex)
                {
                    AddTrajectoryPoint(axis1Data, axis2Data, travelCmd, travelMCode, GetDwellForPoint(absolutePointIndex), GetSpeedForPoint(absolutePointIndex),
                        contour.Points[0].X, contour.Points[0].Y, 0, 0);
                    pointCount++;
                }
                absolutePointIndex++;

                // Actual contour
                if (contour.IsCircle)
                {
                    ushort cmd = 0xD00F;
                    ushort mcode = (ushort)(absolutePointIndex >= DxfGlueStartIndex && absolutePointIndex <= DxfGlueEndIndex ? 1 : 0);
                    if (absolutePointIndex >= DxfStartPointIndex)
                    {
                        AddTrajectoryPoint(axis1Data, axis2Data, cmd, mcode, GetDwellForPoint(absolutePointIndex), GetSpeedForPoint(absolutePointIndex),
                            contour.Points.Last().X, contour.Points.Last().Y, contour.CenterX, contour.CenterY);
                        pointCount++;
                    }
                    absolutePointIndex++;
                }
                else if (contour.IsArc)
                {
                    ushort cmd = contour.ArcClockwise ? (ushort)0xD00F : (ushort)0xD010;
                    ushort mcode = (ushort)(absolutePointIndex >= DxfGlueStartIndex && absolutePointIndex <= DxfGlueEndIndex ? 1 : 0);
                    if (absolutePointIndex >= DxfStartPointIndex)
                    {
                        AddTrajectoryPoint(axis1Data, axis2Data, cmd, mcode, GetDwellForPoint(absolutePointIndex), GetSpeedForPoint(absolutePointIndex),
                            contour.Points.Last().X, contour.Points.Last().Y, contour.CenterX, contour.CenterY);
                        pointCount++;
                    }
                    absolutePointIndex++;
                }
                else
                {
                    for (int i = 1; i < contour.Points.Count; i++)
                    {
                        ushort cmd = 0xD00A;
                        ushort mcode = (ushort)(absolutePointIndex >= DxfGlueStartIndex && absolutePointIndex <= DxfGlueEndIndex ? 1 : 0);
                        if (absolutePointIndex >= DxfStartPointIndex)
                        {
                            AddTrajectoryPoint(axis1Data, axis2Data, cmd, mcode, GetDwellForPoint(absolutePointIndex), GetSpeedForPoint(absolutePointIndex),
                                contour.Points[i].X, contour.Points[i].Y, 0, 0);
                            pointCount++;
                        }
                        absolutePointIndex++;
                    }
                }
            }

            if (pointCount > 0)
            {
                // Set FIRST point to END (1000 bits) -> "Chạy dừng"
                axis1Data[0] = (int)((ushort)axis1Data[0] & 0x0FFF | 0x1000);
                axis2Data[0] = (int)((ushort)axis2Data[0] & 0x0FFF | 0x1000);

                // Set LAST point to END (1000 bits)
                int lastIdx = (pointCount - 1) * 10;
                axis1Data[lastIdx] = (int)((ushort)axis1Data[lastIdx] & 0x0FFF | 0x1000);
                axis2Data[lastIdx] = (int)((ushort)axis2Data[lastIdx] & 0x0FFF | 0x1000);
            }

            return (axis1Data.ToArray(), axis2Data.ToArray(), pointCount);
        }

        private void SendTrajectoryToPLC(int[] a1Arr, int[] a2Arr, int pointCount)
        {
            lock (_plcSync)
            {
                var plc = ePLC;
                if (plc != null && plc.IsConnected)
                {
                    // Ghi trực tiếp vào Buffer memory của module (Simple Motion)
                    // Axis 1: U0\G2000, Axis 2: U0\G3000
                    plc.WriteDeviceBlock(ePLCControl.SubCommand.Word,
                                          ePLCControl.DeviceName.Buffer,
                                          "U0\\G2000", a1Arr);

                    plc.WriteDeviceBlock(ePLCControl.SubCommand.Word,
                                          ePLCControl.DeviceName.Buffer,
                                          "U0\\G3000", a2Arr);

                    // Update SentBufferRecords for visualization (up to first 30 words)
                    _sentBufferRecords.Clear();
                    _sentBufferRecordsAxis2.Clear();
                    int recordCount = Math.Min(a1Arr.Length, 30);
                    for (int i = 0; i < recordCount; i++)
                    {
                        _sentBufferRecords.Add(new BufferRegisterRecord
                        {
                            Address = $"U0\\G{2000 + i}",
                            Value = a1Arr[i]
                        });
                        _sentBufferRecordsAxis2.Add(new BufferRegisterRecord
                        {
                            Address = $"U0\\G{3000 + i}",
                            Value = a2Arr[i]
                        });
                    }
                    OnPropertyChanged(nameof(SentBufferRecords));
                    OnPropertyChanged(nameof(SentBufferRecordsAxis2));

                    AddLog("PLC", "success", $"Đã truyền {pointCount} điểm quỹ đạo xuống Buffer PLC (U0\\G2000 & U0\\G3000)");
                }
                else
                {
                    throw new InvalidOperationException("Chưa kết nối PLC, không thể truyền lệnh quỹ đạo.");
                }
            }
        }

        private void AddTrajectoryPoint(List<int> ax1, List<int> ax2, ushort cmd, ushort mcode, uint dwell, uint speed, double posX, double posY, double cx, double cy)
        {
            // Scale mm sang um (x1000)
            int scaledX = (int)Math.Round(posX * 1000.0);
            int scaledY = (int)Math.Round(posY * 1000.0);
            int scaledCx = (int)Math.Round(cx * 1000.0);
            int scaledCy = (int)Math.Round(cy * 1000.0);
            
            // --- AXIS 1 (X & Center X) ---
            ax1.Add((int)(ushort)cmd);                       // +0: Positioning Identifier (16-bit)
            ax1.Add((int)(ushort)mcode);                     // +1: M Code (16-bit)
            ax1.Add((int)(dwell & 0xFFFF));                  // +2: Dwell Time (Low 16-bit)
            ax1.Add(0);                                      // +3: Reserved / Empty (New Structure)
            ax1.Add((int)(speed & 0xFFFF));                  // +4: Command Speed (Low 16-bit)
            ax1.Add((int)((speed >> 16) & 0xFFFF));          // +5: Command Speed (High 16-bit)
            ax1.Add((int)(scaledX & 0xFFFF));                // +6: Position Address (Low 16-bit)
            ax1.Add((int)((scaledX >> 16) & 0xFFFF));        // +7: Position Address (High 16-bit)
            ax1.Add((int)(scaledCx & 0xFFFF));               // +8: Arc Address (Low 16-bit)
            ax1.Add((int)((scaledCx >> 16) & 0xFFFF));       // +9: Arc Address (High 16-bit)

            // --- AXIS 2 (Y & Center Y) ---
            ax2.Add((int)(ushort)cmd);                       // +0
            ax2.Add((int)(ushort)mcode);                     // +1
            ax2.Add((int)(dwell & 0xFFFF));                  // +2
            ax2.Add(0);                                      // +3: Reserved / Empty
            ax2.Add((int)(speed & 0xFFFF));                  // +4
            ax2.Add((int)((speed >> 16) & 0xFFFF));          // +5
            ax2.Add((int)(scaledY & 0xFFFF));                // +6
            ax2.Add((int)((scaledY >> 16) & 0xFFFF));        // +7
            ax2.Add((int)(scaledCy & 0xFFFF));               // +8
            ax2.Add((int)((scaledCy >> 16) & 0xFFFF));       // +9
        }

        public bool LoadDxfAdvanced(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                DxfSummary = "Lỗi: File không tồn tại hoặc đường dẫn trống.";
                return false;
            }

            try
            {
                DxfDocument doc = DxfDocument.Load(filePath);
                DxfFilePath = filePath;
                DxfLayers = doc.Layers.Select(l => l.Name).ToList();
                DxfLayers.Insert(0, "All");
                DxfLayerCount = doc.Layers.Count();
                DxfEntityCount = doc.Entities.All.Count();

                List<DxfContourInfo> contours = new();
                List<(double X, double Y)> allPoints = new();

                // Simple contour extraction from Polylines
                int cIdx = 1;
                foreach (var pl in doc.Entities.Polylines2D)
                {
                    var pts = pl.Vertexes.Select(v => (v.Position.X, v.Position.Y)).ToList();
                    
                    // Fix: If the polyline is closed, explicitly add the start point to the end
                    // so the motion kernel actually executes the closing segment.
                    if (pl.IsClosed && pts.Count > 0)
                    {
                        pts.Add(pts[0]);
                    }

                    contours.Add(new DxfContourInfo
                    {
                        Name = $"Contour {cIdx++}",
                        Points = pts,
                        IsClosed = pl.IsClosed,
                        Layer = pl.Layer.Name,
                        Length = CalculateLength(pts)
                    });
                    allPoints.AddRange(pts);
                }

                // Extract Standalone Points
                foreach (var pt in doc.Entities.Points)
                {
                    var pts = new List<(double X, double Y)> { (pt.Position.X, pt.Position.Y) };
                    contours.Add(new DxfContourInfo
                    {
                        Name = $"Point {cIdx++}",
                        Points = pts,
                        IsClosed = false,
                        Layer = pt.Layer.Name,
                        Length = 0
                    });
                    allPoints.AddRange(pts);
                }

                // Treat individual lines as contours for simplicity for now
                foreach (var ln in doc.Entities.Lines)
                {
                    var pts = new List<(double X, double Y)> { (ln.StartPoint.X, ln.StartPoint.Y), (ln.EndPoint.X, ln.EndPoint.Y) };
                    contours.Add(new DxfContourInfo
                    {
                        Name = $"Line {cIdx++}",
                        Points = pts,
                        IsClosed = false,
                        Layer = ln.Layer.Name,
                        Length = CalculateLength(pts)
                    });
                    allPoints.AddRange(pts);
                }

                // Circles: keep only start/end (same point) and center.
                foreach (var c in doc.Entities.Circles)
                {
                    var start = (X: c.Center.X + c.Radius, Y: c.Center.Y);
                    var pts = new List<(double X, double Y)> { start, start };
                    contours.Add(new DxfContourInfo
                    {
                        Name = $"Circle {cIdx++}",
                        Points = pts,
                        IsClosed = true,
                        Layer = c.Layer.Name,
                        Length = 2.0 * Math.PI * c.Radius,
                        HasCenter = true,
                        CenterX = c.Center.X,
                        CenterY = c.Center.Y,
                        IsCircle = true,
                        Radius = c.Radius,
                        StartAngleDeg = 0.0,
                        EndAngleDeg = 360.0
                    });
                    AddCircleBoundsPoints(allPoints, c.Center.X, c.Center.Y, c.Radius);
                }

                // Arcs: keep only start/end and center.
                foreach (var a in doc.Entities.Arcs)
                {
                    bool arcClockwise = a.Normal.Z < 0.0;
                    double arcSweep = arcClockwise
                        ? GetSweepDegreesClockwise(a.StartAngle, a.EndAngle)
                        : GetSweepDegrees(a.StartAngle, a.EndAngle);

                    var start = GetPointOnCircle(a.Center.X, a.Center.Y, a.Radius, a.StartAngle);
                    var end = GetPointOnCircle(a.Center.X, a.Center.Y, a.Radius, a.EndAngle);
                    var pts = new List<(double X, double Y)> { start, end };
                    contours.Add(new DxfContourInfo
                    {
                        Name = $"Arc {cIdx++}",
                        Points = pts,
                        IsClosed = false,
                        Layer = a.Layer.Name,
                        Length = CalculateArcLength(a.Radius, arcSweep),
                        HasCenter = true,
                        CenterX = a.Center.X,
                        CenterY = a.Center.Y,
                        IsArc = true,
                        Radius = a.Radius,
                        StartAngleDeg = a.StartAngle,
                        EndAngleDeg = a.EndAngle,
                        ArcClockwise = arcClockwise,
                        ArcSweepDeg = arcSweep
                    });
                    AddArcBoundsPoints(allPoints, a.Center.X, a.Center.Y, a.Radius, a.StartAngle, a.EndAngle, arcClockwise, start, end);
                }

                DxfContours = contours;
                DxfSampleCount = allPoints.Count;

                if (allPoints.Any())
                {
                    DxfXMin = allPoints.Min(p => p.X);
                    DxfXMax = allPoints.Max(p => p.X);
                    DxfYMin = allPoints.Min(p => p.Y);
                    DxfYMax = allPoints.Max(p => p.Y);
                    DxfPreviewPathData = GenerateSvgPath(contours, allPoints);
                }

                DxfSummary = $"Loaded OK: {Path.GetFileName(filePath)} | {DxfEntityCount} entities | {DxfContours.Count} contours";
                AddLog("PC", "success", DxfSummary);
                return true;
            }
            catch (Exception ex)
            {
                DxfSummary = $"DXF Load Error: {ex.Message}";
                AddLog("PC", "error", DxfSummary);
                return false;
            }
        }

        public bool BrowseAndLoadDxf()
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select DXF File",
                    Filter = "DXF files (*.dxf)|*.dxf|All files (*.*)|*.*",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false
                };

                if (!string.IsNullOrWhiteSpace(DxfFilePath))
                {
                    try
                    {
                        string? dir = Path.GetDirectoryName(DxfFilePath);
                        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                            dlg.InitialDirectory = dir;
                    }
                    catch
                    {
                        // Ignore invalid stored path and continue with system default dialog location.
                    }
                }

                bool? result = dlg.ShowDialog();
                if (result == true)
                {
                    DxfFilePath = dlg.FileName;
                    return LoadDxfAdvanced(DxfFilePath);
                }

                AddLog("UI", "info", "DXF file selection canceled");
                return false;
            }
            catch (Exception ex)
            {
                DxfSummary = $"Open file dialog error: {ex.Message}";
                AddLog("PC", "error", DxfSummary, "BrowseAndLoadDxf");
                return false;
            }
        }

        public void RefreshDxfData()
        {
            if (DxfContours == null || DxfContours.Count == 0) return;

            var allPoints = new List<(double X, double Y)>();
            foreach (var contour in DxfContours)
            {
                var pts = contour.Points;
                if (pts != null)
                {
                    allPoints.AddRange(pts);
                    if (contour.HasCenter)
                    {
                        if (contour.IsCircle)
                        {
                            AddCircleBoundsPoints(allPoints, contour.CenterX, contour.CenterY, contour.Radius);
                        }
                        else if (contour.IsArc && pts.Count > 0)
                        {
                            AddArcBoundsPoints(allPoints, contour.CenterX, contour.CenterY, contour.Radius, 
                                contour.StartAngleDeg, contour.EndAngleDeg, contour.ArcClockwise, 
                                pts[0], pts.Last());
                        }
                    }
                }
            }

            DxfSampleCount = allPoints.Count;

            if (allPoints.Any())
            {
                DxfXMin = allPoints.Min(p => p.X);
                DxfXMax = allPoints.Max(p => p.X);
                DxfYMin = allPoints.Min(p => p.Y);
                DxfYMax = allPoints.Max(p => p.Y);
                DxfPreviewPathData = GenerateSvgPath(DxfContours, allPoints);
            }
            else
            {
                DxfPreviewPathData = "";
            }

            OnPropertyChanged(nameof(DxfContours));
            OnPropertyChanged(nameof(DxfPreviewPathData));
        }

        private double CalculateLength(List<(double X, double Y)> pts)
        {
            double len = 0;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                len += Math.Sqrt(Math.Pow(pts[i + 1].X - pts[i].X, 2) + Math.Pow(pts[i + 1].Y - pts[i].Y, 2));
            }
            return len;
        }

        private string GenerateSvgPath(List<DxfContourInfo> contours, List<(double X, double Y)> points)
        {
            // Simple SVG generation logic: Scale visualization to fit roughly 200x200
            if (!points.Any()) return "";
            DxfSvgMinX = points.Min(p => p.X);
            DxfSvgMinY = points.Min(p => p.Y);
            double width = points.Max(p => p.X) - DxfSvgMinX;
            double height = points.Max(p => p.Y) - DxfSvgMinY;
            DxfSvgScale = 200 / Math.Max(width, height == 0 ? 1 : height);
            double scale = DxfSvgScale;
            double minX = DxfSvgMinX;
            double minY = DxfSvgMinY;

            var sb = new System.Text.StringBuilder();
            foreach (var contour in contours)
            {
                if (contour.Points == null || contour.Points.Count == 0)
                    continue;

                if (contour.IsCircle && contour.HasCenter && contour.Radius > 0)
                {
                    double cx = (contour.CenterX - minX) * scale;
                    double cy = 200 - (contour.CenterY - minY) * scale;
                    double r = contour.Radius * scale;
                    sb.Append($"M {cx + r} {cy} ");
                    sb.Append($"A {r} {r} 0 1 1 {cx - r} {cy} ");
                    sb.Append($"A {r} {r} 0 1 1 {cx + r} {cy} ");
                    sb.Append("Z ");
                    continue;
                }

                if (contour.IsArc && contour.HasCenter && contour.Points.Count >= 2 && contour.Radius > 0)
                {
                    var pStart = contour.Points[0];
                    var pEnd = contour.Points[1];
                    double sx = (pStart.X - minX) * scale;
                    double sy = 200 - (pStart.Y - minY) * scale;
                    double ex = (pEnd.X - minX) * scale;
                    double ey = 200 - (pEnd.Y - minY) * scale;
                    double r = contour.Radius * scale;
                    double sweep = contour.ArcSweepDeg > 0 ? contour.ArcSweepDeg : GetSweepDegrees(contour.StartAngleDeg, contour.EndAngleDeg);
                    int largeArcFlag = sweep > 180.0 ? 1 : 0;
                    // Y-axis is flipped in SVG projection, so sweep-flag is inverted from math-space orientation.
                    int sweepFlag = contour.ArcClockwise ? 1 : 0;
                    sb.Append($"M {sx} {sy} A {r} {r} 0 {largeArcFlag} {sweepFlag} {ex} {ey} ");
                    continue;
                }

                var p0 = contour.Points[0];
                sb.Append($"M {(p0.X - minX) * scale} {200 - (p0.Y - minY) * scale} ");

                foreach (var p in contour.Points.Skip(1))
                {
                    sb.Append($"L {(p.X - minX) * scale} {200 - (p.Y - minY) * scale} ");
                }

                if (contour.IsClosed)
                    sb.Append("Z ");
            }

            return sb.ToString();
        }

        private static void AddCircleBoundsPoints(List<(double X, double Y)> allPoints, double cx, double cy, double radius)
        {
            allPoints.Add((cx - radius, cy));
            allPoints.Add((cx + radius, cy));
            allPoints.Add((cx, cy - radius));
            allPoints.Add((cx, cy + radius));
        }

        private static void AddArcBoundsPoints(
            List<(double X, double Y)> allPoints,
            double cx,
            double cy,
            double radius,
            double startDeg,
            double endDeg,
            bool arcClockwise,
            (double X, double Y) start,
            (double X, double Y) end)
        {
            allPoints.Add(start);
            allPoints.Add(end);

            foreach (double candidate in new[] { 0.0, 90.0, 180.0, 270.0 })
            {
                if (IsAngleInArcSweep(candidate, startDeg, endDeg, arcClockwise))
                {
                    allPoints.Add(GetPointOnCircle(cx, cy, radius, candidate));
                }
            }
        }

        private static (double X, double Y) GetPointOnCircle(double cx, double cy, double radius, double angleDeg)
        {
            double a = angleDeg * Math.PI / 180.0;
            return (cx + radius * Math.Cos(a), cy + radius * Math.Sin(a));
        }

        private static double CalculateArcLength(double radius, double sweepDeg)
        {
            return radius * sweepDeg * Math.PI / 180.0;
        }

        private static double GetSweepDegrees(double startDeg, double endDeg)
        {
            double s = NormalizeDeg(startDeg);
            double e = NormalizeDeg(endDeg);
            if (e <= s)
                e += 360.0;
            return e - s;
        }

        private static double GetSweepDegreesClockwise(double startDeg, double endDeg)
        {
            double s = NormalizeDeg(startDeg);
            double e = NormalizeDeg(endDeg);
            if (e >= s)
                e -= 360.0;
            return s - e;
        }

        private static bool IsAngleInArcSweep(double angle, double startDeg, double endDeg, bool arcClockwise)
        {
            return arcClockwise
                ? IsAngleInClockwiseSweep(angle, startDeg, endDeg)
                : IsAngleInCcwSweep(angle, startDeg, endDeg);
        }

        private static bool IsAngleInCcwSweep(double angle, double startDeg, double endDeg)
        {
            double s = NormalizeDeg(startDeg);
            double e = NormalizeDeg(endDeg);
            double a = NormalizeDeg(angle);

            if (e <= s)
                e += 360.0;
            if (a < s)
                a += 360.0;

            return a >= s && a <= e;
        }

        private static bool IsAngleInClockwiseSweep(double angle, double startDeg, double endDeg)
        {
            double s = NormalizeDeg(startDeg);
            double e = NormalizeDeg(endDeg);
            double a = NormalizeDeg(angle);

            if (e >= s)
                e -= 360.0;
            if (a > s)
                a -= 360.0;

            return a <= s && a >= e;
        }

        private static double NormalizeDeg(double deg)
        {
            double d = deg % 360.0;
            return d < 0 ? d + 360.0 : d;
        }

        public void ValidateDxfSafety()
        {
            // Logic to check if any contour exceeds machine limits
            bool warning = false;
            foreach (var contour in DxfContours)
            {
                foreach (var p in contour.Points)
                {
                    double tx = p.X * DxfScaleX + DxfOffsetX;
                    double ty = p.Y * DxfScaleY + DxfOffsetY;
                    if (tx < 0 || tx > 1000 || ty < 0 || ty > 1000) warning = true; // Example hard limits 1000mm
                }
            }
            if (warning) AddLog("PC", "warning", "DXF Trajectory exceeds machine safety limits!", "Validation");
            else AddLog("PC", "success", "DXF Trajectory within safety limits.", "Validation");
        }
    }
}

