using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using netDxf;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Tables;

namespace DACDT_2026
{
    public sealed class SvgToDxfConverter
    {
        public SvgConversionResult Convert(string svgPath, double curveTolerance = 1.0)
        {
            if (string.IsNullOrWhiteSpace(svgPath)) throw new ArgumentException("SVG path is empty.", nameof(svgPath));
            return ConvertTo(svgPath, Path.ChangeExtension(svgPath, ".dxf"), curveTolerance);
        }

        public List<List<SvgPoint>> ExtractPaths(string svgPath, double curveTolerance = 1.0)
        {
            if (string.IsNullOrWhiteSpace(svgPath)) throw new ArgumentException("SVG path is empty.", nameof(svgPath));
            if (!File.Exists(svgPath)) throw new FileNotFoundException("SVG file was not found.", svgPath);
            if (curveTolerance <= 0) throw new ArgumentOutOfRangeException(nameof(curveTolerance));

            XDocument document = XDocument.Load(svgPath, LoadOptions.PreserveWhitespace);
            XElement root = document.Root;
            if (root == null || root.Name.LocalName != "svg") throw new InvalidDataException("The file is not an SVG document.");

            var paths = new List<List<SvgPoint>>();
            foreach (XElement element in root.Descendants())
            {
                string name = element.Name.LocalName;
                if (name == "path")
                {
                    string data = (string)element.Attribute("d");
                    if (!string.IsNullOrWhiteSpace(data)) paths.Add(SvgPathParser.Parse(data, curveTolerance));
                }
                else if (name == "polyline" || name == "polygon")
                {
                    string points = (string)element.Attribute("points");
                    List<SvgPoint> parsed = SvgPathParser.ParsePoints(points);
                    if (name == "polygon" && parsed.Count > 1) parsed.Add(parsed[0]);
                    if (parsed.Count > 1) paths.Add(parsed);
                }
                else if (name == "line")
                {
                    paths.Add(new List<SvgPoint> { new SvgPoint(ParseNumber(element, "x1"), ParseNumber(element, "y1")), new SvgPoint(ParseNumber(element, "x2"), ParseNumber(element, "y2")) });
                }
                else if (name == "rect")
                {
                    double x = ParseNumber(element, "x"), y = ParseNumber(element, "y"), w = ParseNumber(element, "width"), h = ParseNumber(element, "height");
                    paths.Add(new List<SvgPoint> { new SvgPoint(x, y), new SvgPoint(x + w, y), new SvgPoint(x + w, y + h), new SvgPoint(x, y + h), new SvgPoint(x, y) });
                }
                else if (name == "circle" || name == "ellipse")
                {
                    double cx = ParseNumber(element, "cx"), cy = ParseNumber(element, "cy"), rx = ParseNumber(element, name == "circle" ? "r" : "rx"), ry = name == "circle" ? rx : ParseNumber(element, "ry");
                    var points = new List<SvgPoint>();
                    for (int i = 0; i <= 72; i++) { double a = i * Math.PI * 2 / 72; points.Add(new SvgPoint(cx + rx * Math.Cos(a), cy + ry * Math.Sin(a))); }
                    paths.Add(points);
                }
            }
            return paths;
        }

        public SvgConversionResult ConvertTo(string svgPath, string dxfPath, double curveTolerance = 1.0)
        {
            if (string.IsNullOrWhiteSpace(svgPath)) throw new ArgumentException("SVG path is empty.", nameof(svgPath));
            if (string.IsNullOrWhiteSpace(dxfPath)) throw new ArgumentException("DXF output path is empty.", nameof(dxfPath));

            var paths = ExtractPaths(svgPath, curveTolerance);
            string output = Path.GetFullPath(dxfPath);
            WriteDxf(output, paths);
            return new SvgConversionResult { InputPath = Path.GetFullPath(svgPath), OutputPath = output, PathCount = paths.Count, VertexCount = CountVertices(paths) };
        }

        private static double ParseNumber(XElement e, string name)
        {
            double value;
            return double.TryParse((string)e.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        private static int CountVertices(List<List<SvgPoint>> paths)
        {
            int n = 0;
            foreach (List<SvgPoint> p in paths) n += p.Count;
            return n;
        }

        private static void WriteDxf(string path, List<List<SvgPoint>> paths)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var doc = new DxfDocument(DxfVersion.AutoCad2000);
            var svgLayer = new Layer("SVG")
            {
                Color = AciColor.Default
            };
            doc.Layers.Add(svgLayer);

            foreach (List<SvgPoint> points in paths)
            {
                if (points == null || points.Count < 2) continue;

                var vertices = new List<Vector2>(points.Count);
                for (int i = 0; i < points.Count; i++)
                {
                    vertices.Add(new Vector2(points[i].X, -points[i].Y));
                }

                bool isClosed = points.Count > 2 &&
                    Math.Abs(points[0].X - points[points.Count - 1].X) < 1e-6 &&
                    Math.Abs(points[0].Y - points[points.Count - 1].Y) < 1e-6;

                if (isClosed && vertices.Count > 2)
                {
                    vertices.RemoveAt(vertices.Count - 1);
                }

                var polyline = new Polyline2D(vertices, isClosed)
                {
                    Layer = svgLayer
                };

                doc.Entities.Add(polyline);
            }

            doc.Save(path);
        }

        public sealed class SvgConversionResult
        {
            public string InputPath { get; set; }
            public string OutputPath { get; set; }
            public int PathCount { get; set; }
            public int VertexCount { get; set; }
        }

        public struct SvgPoint
        {
            public double X;
            public double Y;
            public SvgPoint(double x, double y) { X = x; Y = y; }
        }
    }

    internal static class SvgPathParser
    {
        public static List<SvgToDxfConverter.SvgPoint> ParsePoints(string text)
        {
            var result = new List<SvgToDxfConverter.SvgPoint>();
            if (string.IsNullOrWhiteSpace(text)) return result;
            string[] pairs = text.Trim().Replace(',', ' ').Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                double x, y;
                if (double.TryParse(pairs[i], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    && double.TryParse(pairs[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                {
                    result.Add(new SvgToDxfConverter.SvgPoint(x, y));
                }
            }
            return result;
        }

        public static List<SvgToDxfConverter.SvgPoint> Parse(string data, double tolerance)
        {
            var result = new List<SvgToDxfConverter.SvgPoint>();
            if (string.IsNullOrWhiteSpace(data)) return result;

            string[] values = SplitTokens(data);
            if (values.Length == 0) return result;

            double x = 0, y = 0, sx = 0, sy = 0, cx = 0, cy = 0, qx = 0, qy = 0;
            char command, lastCmd = ' ';
            int i = 0;

            if (values[0].Length == 1 && char.IsLetter(values[0][0])) { command = values[0][0]; i = 1; }
            else command = 'M';

            while (i < values.Length)
            {
                char upper = char.ToUpperInvariant(command);
                bool rel = char.IsLower(command);
                int paramCount;
                switch (upper)
                {
                    case 'M': paramCount = 2; break;
                    case 'L': paramCount = 2; break;
                    case 'H': paramCount = 1; break;
                    case 'V': paramCount = 1; break;
                    case 'C': paramCount = 6; break;
                    case 'S': paramCount = 4; break;
                    case 'Q': paramCount = 4; break;
                    case 'T': paramCount = 2; break;
                    case 'A': paramCount = 7; break;
                    case 'Z': paramCount = 0; break;
                    default: return result;
                }

                if (i + paramCount > values.Length) break;

                double[] p = new double[paramCount];
                bool ok = true;
                for (int k = 0; k < paramCount; k++)
                    if (!double.TryParse(values[i + k], NumberStyles.Float, CultureInfo.InvariantCulture, out p[k])) { ok = false; break; }
                if (!ok) break;

                i += paramCount;

                switch (upper)
                {
                    case 'M':
                    {
                        double nx = rel ? x + p[0] : p[0], ny = rel ? y + p[1] : p[1];
                        x = nx; y = ny; sx = nx; sy = ny;
                        result.Add(new SvgToDxfConverter.SvgPoint(x, y));
                        command = rel ? 'l' : 'L';
                        break;
                    }
                    case 'L':
                        x = rel ? x + p[0] : p[0]; y = rel ? y + p[1] : p[1];
                        result.Add(new SvgToDxfConverter.SvgPoint(x, y));
                        break;
                    case 'H':
                        x = rel ? x + p[0] : p[0];
                        result.Add(new SvgToDxfConverter.SvgPoint(x, y));
                        break;
                    case 'V':
                        y = rel ? y + p[0] : p[0];
                        result.Add(new SvgToDxfConverter.SvgPoint(x, y));
                        break;
                    case 'C':
                    {
                        double x1 = rel ? x + p[0] : p[0], y1 = rel ? y + p[1] : p[1];
                        double x2 = rel ? x + p[2] : p[2], y2 = rel ? y + p[3] : p[3];
                        double x3 = rel ? x + p[4] : p[4], y3 = rel ? y + p[5] : p[5];
                        AddCubic(result, tolerance, x, y, x1, y1, x2, y2, x3, y3);
                        cx = x2; cy = y2; qx = x2; qy = y2; x = x3; y = y3;
                        break;
                    }
                    case 'S':
                    {
                        double x1 = rel ? x + p[0] : p[0], y1 = rel ? y + p[1] : p[1];
                        double x2 = rel ? x + p[2] : p[2], y2 = rel ? y + p[3] : p[3];
                        double cx1 = (lastCmd == 'C' || lastCmd == 'S') ? 2 * x - cx : x;
                        double cy1 = (lastCmd == 'C' || lastCmd == 'S') ? 2 * y - cy : y;
                        AddCubic(result, tolerance, x, y, cx1, cy1, x1, y1, x2, y2);
                        cx = x1; cy = y1; qx = x1; qy = y1; x = x2; y = y2;
                        break;
                    }
                    case 'Q':
                    {
                        double x1 = rel ? x + p[0] : p[0], y1 = rel ? y + p[1] : p[1];
                        double x2 = rel ? x + p[2] : p[2], y2 = rel ? y + p[3] : p[3];
                        AddQuadratic(result, tolerance, x, y, x1, y1, x2, y2);
                        qx = x1; qy = y1; cx = x1; cy = y1; x = x2; y = y2;
                        break;
                    }
                    case 'T':
                    {
                        double x1 = (lastCmd == 'Q' || lastCmd == 'T') ? 2 * x - qx : x;
                        double y1 = (lastCmd == 'Q' || lastCmd == 'T') ? 2 * y - qy : y;
                        double x2 = rel ? x + p[0] : p[0], y2 = rel ? y + p[1] : p[1];
                        AddQuadratic(result, tolerance, x, y, x1, y1, x2, y2);
                        qx = x1; qy = y1; cx = x1; cy = y1; x = x2; y = y2;
                        break;
                    }
                    case 'A':
                    {
                        double rx = Math.Abs(p[0]), ry = Math.Abs(p[1]), phi = p[2];
                        double ex = rel ? x + p[5] : p[5], ey = rel ? y + p[6] : p[6];
                        AddArc(result, tolerance, x, y, rx, ry, phi, p[3], p[4], ex, ey);
                        x = ex; y = ey;
                        break;
                    }
                    case 'Z':
                        if (result.Count == 0 || result[result.Count - 1].X != sx || result[result.Count - 1].Y != sy)
                            result.Add(new SvgToDxfConverter.SvgPoint(sx, sy));
                        x = sx; y = sy;
                        break;
                }

                lastCmd = upper;
                if (i < values.Length && values[i].Length == 1 && char.IsLetter(values[i][0])) { command = values[i][0]; i++; }
            }

            return result;
        }

        private static void AddCubic(List<SvgToDxfConverter.SvgPoint> result, double tolerance, double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double dx = x3 - x0, dy = y3 - y0;
            double len2 = dx * dx + dy * dy;
            if (len2 <= tolerance * tolerance) { result.Add(new SvgToDxfConverter.SvgPoint(x3, y3)); return; }

            double d1 = Math.Abs((x1 - x0) * dy - (y1 - y0) * dx);
            double d2 = Math.Abs((x2 - x0) * dy - (y2 - y0) * dx);
            if (Math.Max(d1, d2) <= tolerance * Math.Sqrt(len2)) { result.Add(new SvgToDxfConverter.SvgPoint(x3, y3)); return; }

            double x01 = (x0 + x1) / 2, y01 = (y0 + y1) / 2;
            double x12 = (x1 + x2) / 2, y12 = (y1 + y2) / 2;
            double x23 = (x2 + x3) / 2, y23 = (y2 + y3) / 2;
            double x012 = (x01 + x12) / 2, y012 = (y01 + y12) / 2;
            double x123 = (x12 + x23) / 2, y123 = (y12 + y23) / 2;
            double xm = (x012 + x123) / 2, ym = (y012 + y123) / 2;

            AddCubic(result, tolerance, x0, y0, x01, y01, x012, y012, xm, ym);
            AddCubic(result, tolerance, xm, ym, x123, y123, x23, y23, x3, y3);
        }

        private static void AddQuadratic(List<SvgToDxfConverter.SvgPoint> result, double tolerance, double x0, double y0, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x0, dy = y2 - y0;
            double len2 = dx * dx + dy * dy;
            if (len2 <= tolerance * tolerance) { result.Add(new SvgToDxfConverter.SvgPoint(x2, y2)); return; }

            double d = Math.Abs((x1 - x0) * dy - (y1 - y0) * dx);
            if (d <= tolerance * Math.Sqrt(len2)) { result.Add(new SvgToDxfConverter.SvgPoint(x2, y2)); return; }

            double x01 = (x0 + x1) / 2, y01 = (y0 + y1) / 2;
            double x12 = (x1 + x2) / 2, y12 = (y1 + y2) / 2;
            double xm = (x01 + x12) / 2, ym = (y01 + y12) / 2;

            AddQuadratic(result, tolerance, x0, y0, x01, y01, xm, ym);
            AddQuadratic(result, tolerance, xm, ym, x12, y12, x2, y2);
        }

        private static void AddArc(List<SvgToDxfConverter.SvgPoint> result, double tolerance, double x0, double y0, double rx, double ry, double phiDeg, double largeArcFlag, double sweepFlag, double x1, double y1)
        {
            if (Math.Abs(x1 - x0) < 1e-12 && Math.Abs(y1 - y0) < 1e-12) return;
            if (rx < 1e-12 || ry < 1e-12) { result.Add(new SvgToDxfConverter.SvgPoint(x1, y1)); return; }

            double phi = phiDeg * Math.PI / 180.0;
            double cosPhi = Math.Cos(phi), sinPhi = Math.Sin(phi);

            double dx = (x0 - x1) / 2.0, dy = (y0 - y1) / 2.0;
            double x1p = cosPhi * dx + sinPhi * dy;
            double y1p = -sinPhi * dx + cosPhi * dy;

            double rx2 = rx * rx, ry2 = ry * ry;
            double lambda = (x1p * x1p) / rx2 + (y1p * y1p) / ry2;
            if (lambda > 1.0)
            {
                double s = Math.Sqrt(lambda);
                rx *= s; ry *= s; rx2 = rx * rx; ry2 = ry * ry;
            }

            double numerator = rx2 * ry2 - rx2 * y1p * y1p - ry2 * x1p * x1p;
            double denominator = rx2 * y1p * y1p + ry2 * x1p * x1p;
            bool fA = largeArcFlag != 0, fS = sweepFlag != 0;
            double coef = (fA == fS ? -1.0 : 1.0) * Math.Sqrt(Math.Max(0.0, numerator / denominator));

            double cxp = coef * (rx * y1p / ry);
            double cyp = coef * (-(ry * x1p / rx));

            double cx = cosPhi * cxp - sinPhi * cyp + (x0 + x1) / 2.0;
            double cy = sinPhi * cxp + cosPhi * cyp + (y0 + y1) / 2.0;

            double ux = (x1p - cxp) / rx, uy = (y1p - cyp) / ry;
            double vx = (-x1p - cxp) / rx, vy = (-y1p - cyp) / ry;

            double theta1 = VectorAngle(1.0, 0.0, ux, uy);
            double dTheta = VectorAngle(ux, uy, vx, vy);

            if (!fS && dTheta > 0) dTheta -= 2 * Math.PI;
            else if (fS && dTheta < 0) dTheta += 2 * Math.PI;

            double maxRadius = Math.Max(rx, ry);
            double step = 2.0 * Math.Acos(Math.Max(-1.0, Math.Min(1.0, 1.0 - tolerance / maxRadius)));
            int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(dTheta) / step));
            if (segments > 360) segments = 360;

            for (int s = 1; s <= segments; s++)
            {
                double t = (double)s / segments;
                double a = theta1 + dTheta * t;
                double ca = Math.Cos(a), sa = Math.Sin(a);
                double px = cosPhi * rx * ca - sinPhi * ry * sa + cx;
                double py = sinPhi * rx * ca + cosPhi * ry * sa + cy;
                result.Add(new SvgToDxfConverter.SvgPoint(px, py));
            }
        }

        private static double VectorAngle(double ux, double uy, double vx, double vy)
        {
            double dot = ux * vx + uy * vy;
            double len = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            if (len < 1e-12) return 0.0;

            double angle = Math.Acos(Math.Max(-1.0, Math.Min(1.0, dot / len)));
            if (ux * vy - uy * vx < 0) angle = -angle;
            return angle;
        }

        private static string[] SplitTokens(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return new string[0];
            var result = new List<string>();
            int i = 0;
            int n = data.Length;
            while (i < n)
            {
                char c = data[i];
                if (char.IsWhiteSpace(c) || c == ',') { i++; continue; }

                if (char.IsLetter(c)) { result.Add(c.ToString()); i++; continue; }

                int start = i;
                if (c == '+' || c == '-') i++;

                bool seenDot = false;
                while (i < n)
                {
                    char d = data[i];
                    if (char.IsDigit(d)) { i++; continue; }
                    if (d == '.' && !seenDot) { seenDot = true; i++; continue; }
                    break;
                }

                if (i < n && (data[i] == 'e' || data[i] == 'E'))
                {
                    int j = i + 1;
                    if (j < n && (data[j] == '+' || data[j] == '-')) j++;
                    if (j < n && char.IsDigit(data[j]))
                    {
                        i = j;
                        while (i < n && char.IsDigit(data[i])) i++;
                    }
                }

                result.Add(data.Substring(start, i - start));
            }
            return result.ToArray();
        }
    }
}
