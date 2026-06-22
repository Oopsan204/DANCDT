using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NDA_DXF
{
    public static class DxfReader
    {
        public static DxfLoadResult Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("DXF path is empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("DXF file was not found.", filePath);
            }

            string[] lines = ReadAllLinesShared(filePath);
            var result = new DxfLoadResult
            {
                FilePath = Path.GetFullPath(filePath)
            };

            var bounds = new BoundsAccumulator();
            int index = FindEntitiesStart(lines);
            if (index < 0)
            {
                throw new InvalidDataException("DXF file has no ENTITIES section.");
            }

            while (index < lines.Length - 1)
            {
                string code = Clean(lines[index]);
                string value = Clean(lines[index + 1]);

                if (code != "0")
                {
                    index++;
                    continue;
                }

                if (EqualsDxf(value, "ENDSEC") || EqualsDxf(value, "EOF"))
                {
                    break;
                }

                index += 2;
                string entityType = value.ToUpperInvariant();
                DxfSegment segment = null;

                if (entityType == "LINE")
                {
                    segment = ParseLine(lines, ref index);
                }
                else if (entityType == "ARC")
                {
                    segment = ParseArc(lines, ref index);
                }
                else if (entityType == "CIRCLE")
                {
                    segment = ParseCircle(lines, ref index);
                }
                else
                {
                    SkipEntity(lines, ref index);
                }

                if (segment != null)
                {
                    segment.Index = result.Segments.Count + 1;
                    result.Segments.Add(segment);
                    bounds.Include(segment);
                }
            }

            result.Bounds = bounds.ToBounds();
            return result;
        }

        private static int FindEntitiesStart(string[] lines)
        {
            for (int i = 0; i < lines.Length - 1; i++)
            {
                if (Clean(lines[i]) == "2" && EqualsDxf(Clean(lines[i + 1]), "ENTITIES"))
                {
                    return i + 2;
                }

                if (Clean(lines[i]) == "0" && EqualsDxf(Clean(lines[i + 1]), "ENTITIES"))
                {
                    return i + 2;
                }
            }

            return -1;
        }

        private static string[] ReadAllLinesShared(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                var lines = new List<string>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }

                return lines.ToArray();
            }
        }

        private static DxfSegment ParseLine(string[] lines, ref int index)
        {
            double x1 = 0, y1 = 0, z1 = 0;
            double x2 = 0, y2 = 0, z2 = 0;

            ReadEntityPairs(lines, ref index, (code, value) =>
            {
                switch (code)
                {
                    case "10": x1 = ToDouble(value); break;
                    case "20": y1 = ToDouble(value); break;
                    case "30": z1 = ToDouble(value); break;
                    case "11": x2 = ToDouble(value); break;
                    case "21": y2 = ToDouble(value); break;
                    case "31": z2 = ToDouble(value); break;
                }
            });

            var start = new DxfPoint(x1, y1, z1);
            var end = new DxfPoint(x2, y2, z2);
            return new DxfSegment
            {
                MotionType = "Line",
                Start = start,
                End = end,
                Points = new List<DxfPoint> { start, end }
            };
        }

        private static DxfSegment ParseArc(string[] lines, ref int index)
        {
            double cx = 0, cy = 0, cz = 0;
            double radius = 0;
            double startAngle = 0;
            double endAngle = 0;

            ReadEntityPairs(lines, ref index, (code, value) =>
            {
                switch (code)
                {
                    case "10": cx = ToDouble(value); break;
                    case "20": cy = ToDouble(value); break;
                    case "30": cz = ToDouble(value); break;
                    case "40": radius = ToDouble(value); break;
                    case "50": startAngle = ToDouble(value); break;
                    case "51": endAngle = ToDouble(value); break;
                }
            });

            List<DxfPoint> points = SampleArc(cx, cy, cz, radius, startAngle, endAngle);
            return new DxfSegment
            {
                MotionType = "Arc CCW",
                Start = points.Count > 0 ? points[0] : new DxfPoint(cx, cy, cz),
                End = points.Count > 0 ? points[points.Count - 1] : new DxfPoint(cx, cy, cz),
                Center = new DxfPoint(cx, cy, cz),
                Radius = radius,
                IsClockwise = false,
                Points = points
            };
        }

        private static DxfSegment ParseCircle(string[] lines, ref int index)
        {
            double cx = 0, cy = 0, cz = 0;
            double radius = 0;

            ReadEntityPairs(lines, ref index, (code, value) =>
            {
                switch (code)
                {
                    case "10": cx = ToDouble(value); break;
                    case "20": cy = ToDouble(value); break;
                    case "30": cz = ToDouble(value); break;
                    case "40": radius = ToDouble(value); break;
                }
            });

            List<DxfPoint> points = SampleArc(cx, cy, cz, radius, 0, 360);
            return new DxfSegment
            {
                MotionType = "Circle",
                Start = points.Count > 0 ? points[0] : new DxfPoint(cx + radius, cy, cz),
                End = points.Count > 0 ? points[points.Count - 1] : new DxfPoint(cx + radius, cy, cz),
                Center = new DxfPoint(cx, cy, cz),
                Radius = radius,
                IsClockwise = false,
                Points = points
            };
        }

        private static void ReadEntityPairs(string[] lines, ref int index, Action<string, string> readPair)
        {
            while (index < lines.Length - 1)
            {
                string code = Clean(lines[index]);
                if (code == "0")
                {
                    break;
                }

                string value = Clean(lines[index + 1]);
                readPair(code, value);
                index += 2;
            }
        }

        private static void SkipEntity(string[] lines, ref int index)
        {
            while (index < lines.Length - 1 && Clean(lines[index]) != "0")
            {
                index += 2;
            }
        }

        private static List<DxfPoint> SampleArc(
            double centerX,
            double centerY,
            double centerZ,
            double radius,
            double startAngle,
            double endAngle)
        {
            var points = new List<DxfPoint>();
            double sweep = NormalizeAngle(endAngle) - NormalizeAngle(startAngle);
            if (Math.Abs(endAngle - startAngle) >= 360.0)
            {
                sweep = 360.0;
            }
            else if (sweep <= 0)
            {
                sweep += 360.0;
            }

            int steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweep) / 5.0));
            for (int i = 0; i <= steps; i++)
            {
                double angle = startAngle + sweep * i / steps;
                double radians = angle * Math.PI / 180.0;
                points.Add(new DxfPoint(
                    centerX + radius * Math.Cos(radians),
                    centerY + radius * Math.Sin(radians),
                    centerZ));
            }

            return points;
        }

        private static double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 360.0;
            while (angle >= 360.0) angle -= 360.0;
            return angle;
        }

        private static double ToDouble(string value)
        {
            double parsed;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return 0.0;
        }

        private static bool EqualsDxf(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string Clean(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private sealed class BoundsAccumulator
        {
            private bool hasPoint;
            private double minX;
            private double minY;
            private double minZ;
            private double maxX;
            private double maxY;
            private double maxZ;

            public void Include(DxfSegment segment)
            {
                if (segment == null || segment.Points == null)
                {
                    return;
                }

                foreach (DxfPoint point in segment.Points)
                {
                    Include(point);
                }
            }

            private void Include(DxfPoint point)
            {
                if (point == null)
                {
                    return;
                }

                if (!hasPoint)
                {
                    minX = maxX = point.X;
                    minY = maxY = point.Y;
                    minZ = maxZ = point.Z;
                    hasPoint = true;
                    return;
                }

                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                minZ = Math.Min(minZ, point.Z);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
                maxZ = Math.Max(maxZ, point.Z);
            }

            public DxfBounds ToBounds()
            {
                if (!hasPoint)
                {
                    return new DxfBounds();
                }

                return new DxfBounds
                {
                    MinX = minX,
                    MinY = minY,
                    MinZ = minZ,
                    MaxX = maxX,
                    MaxY = maxY,
                    MaxZ = maxZ,
                    Width = maxX - minX,
                    Height = maxY - minY
                };
            }
        }
    }
}
