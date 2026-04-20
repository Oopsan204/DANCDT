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
        }

        private List<DxfContourInfo> _dxfContours = new();
        public List<DxfContourInfo> DxfContours { get => _dxfContours; set => SetProperty(ref _dxfContours, value); }

        private DxfContourInfo? _selectedContour;
        public DxfContourInfo? SelectedContour { get => _selectedContour; set => SetProperty(ref _selectedContour, value); }

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

        // Run State
        private bool _isDxfRunning;
        public bool IsDxfRunning { get => _isDxfRunning; set => SetProperty(ref _isDxfRunning, value); }
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

                // Circles: sample into polyline points so they can be previewed/selected like other contours.
                foreach (var c in doc.Entities.Circles)
                {
                    var pts = SampleCirclePoints(c.Center.X, c.Center.Y, c.Radius, 96);
                    contours.Add(new DxfContourInfo
                    {
                        Name = $"Circle {cIdx++}",
                        Points = pts,
                        IsClosed = true,
                        Layer = c.Layer.Name,
                        Length = 2.0 * Math.PI * c.Radius,
                        HasCenter = true,
                        CenterX = c.Center.X,
                        CenterY = c.Center.Y
                    });
                    allPoints.AddRange(pts);
                }

                // Arcs: sample along start/end angles (degrees in DXF/netDxf).
                foreach (var a in doc.Entities.Arcs)
                {
                    var pts = SampleArcPoints(a.Center.X, a.Center.Y, a.Radius, a.StartAngle, a.EndAngle, 64);
                    contours.Add(new DxfContourInfo
                    {
                        Name = $"Arc {cIdx++}",
                        Points = pts,
                        IsClosed = false,
                        Layer = a.Layer.Name,
                        Length = CalculateLength(pts),
                        HasCenter = true,
                        CenterX = a.Center.X,
                        CenterY = a.Center.Y
                    });
                    allPoints.AddRange(pts);
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
            double minX = points.Min(p => p.X);
            double minY = points.Min(p => p.Y);
            double width = points.Max(p => p.X) - minX;
            double height = points.Max(p => p.Y) - minY;
            double scale = 200 / Math.Max(width, height == 0 ? 1 : height);

            var sb = new System.Text.StringBuilder();
            foreach (var contour in contours)
            {
                if (contour.Points == null || contour.Points.Count == 0)
                    continue;

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

        private static List<(double X, double Y)> SampleCirclePoints(double cx, double cy, double radius, int segments)
        {
            int n = Math.Max(segments, 12);
            var pts = new List<(double X, double Y)>(n);

            for (int i = 0; i < n; i++)
            {
                double t = (2.0 * Math.PI * i) / n;
                pts.Add((cx + radius * Math.Cos(t), cy + radius * Math.Sin(t)));
            }

            return pts;
        }

        private static List<(double X, double Y)> SampleArcPoints(double cx, double cy, double radius, double startDeg, double endDeg, int maxSegments)
        {
            double s = NormalizeDeg(startDeg);
            double e = NormalizeDeg(endDeg);
            if (e <= s)
                e += 360.0;

            double sweep = e - s;
            int seg = Math.Max(4, (int)Math.Ceiling(maxSegments * (sweep / 360.0)));

            var pts = new List<(double X, double Y)>(seg + 1);
            for (int i = 0; i <= seg; i++)
            {
                double aDeg = s + sweep * i / seg;
                double aRad = aDeg * Math.PI / 180.0;
                pts.Add((cx + radius * Math.Cos(aRad), cy + radius * Math.Sin(aRad)));
            }

            return pts;
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

