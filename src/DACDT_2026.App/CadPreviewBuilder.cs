using System;
using System.Collections.Generic;
using System.Globalization;

namespace DACDT_2026
{
    internal static class CadPreviewBuilder
    {
        public sealed class Limits
        {
            public Limits(int maxPreviewPoints, int maxPreviewPrimitives)
            {
                if (maxPreviewPoints < 2)
                    throw new ArgumentOutOfRangeException(nameof(maxPreviewPoints));
                if (maxPreviewPrimitives < 1)
                    throw new ArgumentOutOfRangeException(nameof(maxPreviewPrimitives));

                MaxPreviewPoints = maxPreviewPoints;
                MaxPreviewPrimitives = maxPreviewPrimitives;
            }

            public int MaxPreviewPoints { get; private set; }
            public int MaxPreviewPrimitives { get; private set; }
        }

        public static readonly Limits DefaultLimits = new Limits(50000, 50000);

        public static CadDocumentService.CadLoadResult Build(
            CadDocumentService.CadLoadResult source,
            Limits limits)
        {
            if (source == null)
                return null;

            limits = limits ?? DefaultLimits;

            var preview = new CadDocumentService.CadLoadResult
            {
                FilePath = source.FilePath,
                DirectoryPath = source.DirectoryPath,
                FileName = source.FileName,
                Bounds = CloneBounds(source.Bounds),
                Primitives = new List<CadDocumentService.CadPrimitiveData>(),
                Points = new List<CadDocumentService.CadPointData>()
            };

            if (source.Primitives == null || source.Primitives.Count == 0)
                return preview;

            int primitiveLimit = Math.Min(limits.MaxPreviewPrimitives, source.Primitives.Count);
            long totalSourcePoints = 0;
            for (int i = 0; i < primitiveLimit; i++)
            {
                List<CadDocumentService.CadCoordinate> points = source.Primitives[i]?.Points;
                if (points != null && points.Count > 1)
                    totalSourcePoints += points.Count;
            }

            if (totalSourcePoints == 0)
                return preview;

            int remainingPoints = limits.MaxPreviewPoints;
            var seenPointKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < primitiveLimit && remainingPoints >= 2; i++)
            {
                CadDocumentService.CadPrimitiveData sourcePrimitive = source.Primitives[i];
                if (sourcePrimitive?.Points == null || sourcePrimitive.Points.Count < 2)
                    continue;

                int allowedPoints = CalculatePointBudget(
                    sourcePrimitive.Points.Count,
                    totalSourcePoints,
                    remainingPoints);
                if (allowedPoints < 2)
                    break;

                var previewPoints = SamplePoints(sourcePrimitive.Points, allowedPoints);
                preview.Primitives.Add(new CadDocumentService.CadPrimitiveData
                {
                    SourceType = sourcePrimitive.SourceType,
                    Points = previewPoints,
                    Center = CloneCoordinate(sourcePrimitive.Center),
                    IsCw = sourcePrimitive.IsCw,
                    IsCircle = sourcePrimitive.IsCircle,
                    MCodeValue = sourcePrimitive.MCodeValue,
                    Speed = sourcePrimitive.Speed,
                    Dwell = sourcePrimitive.Dwell,
                    ProcessKind = sourcePrimitive.ProcessKind,
                    PathId = sourcePrimitive.PathId,
                    WcsIndex = sourcePrimitive.WcsIndex
                });

                string lineType = sourcePrimitive.SourceType ?? "CAD";
                for (int pointIndex = 0; pointIndex < previewPoints.Count; pointIndex++)
                {
                    CadDocumentService.CadCoordinate point = previewPoints[pointIndex];
                    string key = MakePointKey(point);
                    if (!seenPointKeys.Add(key))
                        continue;

                    preview.Points.Add(new CadDocumentService.CadPointData
                    {
                        Index = preview.Points.Count + 1,
                        LineType = lineType,
                        X = point.X,
                        Y = point.Y,
                        Z = point.Z,
                        XDisplay = point.X.ToString("0.###", CultureInfo.InvariantCulture),
                        YDisplay = point.Y.ToString("0.###", CultureInfo.InvariantCulture),
                        ZDisplay = point.Z.ToString("0.###", CultureInfo.InvariantCulture),
                        Key = key
                    });
                }

                remainingPoints -= previewPoints.Count;
            }

            return preview;
        }

        private static int CalculatePointBudget(int sourceCount, long totalSourcePoints, int remainingPoints)
        {
            if (sourceCount <= remainingPoints)
                return sourceCount;

            long proportional = ((long)sourceCount * remainingPoints + totalSourcePoints - 1)
                / totalSourcePoints;
            int budget = (int)Math.Min(sourceCount, proportional);
            if (budget < 2 && remainingPoints >= 2)
                budget = 2;
            return Math.Min(budget, remainingPoints);
        }

        private static List<CadDocumentService.CadCoordinate> SamplePoints(
            List<CadDocumentService.CadCoordinate> source,
            int count)
        {
            if (count >= source.Count)
            {
                var copy = new List<CadDocumentService.CadCoordinate>(source.Count);
                for (int i = 0; i < source.Count; i++)
                    copy.Add(CloneCoordinate(source[i]));
                return copy;
            }

            var sampled = new List<CadDocumentService.CadCoordinate>(count);
            long denominator = count - 1L;
            long sourceLastIndex = source.Count - 1L;
            for (int i = 0; i < count; i++)
            {
                int sourceIndex = (int)((i * sourceLastIndex + denominator / 2L) / denominator);
                sampled.Add(CloneCoordinate(source[sourceIndex]));
            }

            return sampled;
        }

        private static CadDocumentService.CadCoordinate CloneCoordinate(
            CadDocumentService.CadCoordinate coordinate)
        {
            return coordinate == null
                ? null
                : new CadDocumentService.CadCoordinate(coordinate.X, coordinate.Y, coordinate.Z);
        }

        private static CadDocumentService.CadBounds CloneBounds(CadDocumentService.CadBounds bounds)
        {
            return bounds == null
                ? null
                : new CadDocumentService.CadBounds
                {
                    Left = bounds.Left,
                    Top = bounds.Top,
                    Right = bounds.Right,
                    Bottom = bounds.Bottom,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    MinZ = bounds.MinZ,
                    MaxZ = bounds.MaxZ
                };
        }

        private static string MakePointKey(CadDocumentService.CadCoordinate point)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.###}|{1:0.###}|{2:0.###}",
                point.X,
                point.Y,
                point.Z);
        }
    }
}
