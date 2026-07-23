using System;
using System.Collections.Generic;
using System.Threading;

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

        public static readonly Limits DefaultLimits = new Limits(1000000, 500000);

        public static CadDocumentService.CadLoadResult Build(
            CadDocumentService.CadLoadResult source,
            Limits limits,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
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

            int maximumRetainedPrimitives = Math.Min(
                limits.MaxPreviewPrimitives,
                limits.MaxPreviewPoints / 2);
            List<int> primitiveIndices = SelectPrimitiveIndices(
                source.Primitives,
                maximumRetainedPrimitives,
                cancellationToken);
            long totalSourcePoints = 0;
            for (int i = 0; i < primitiveIndices.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totalSourcePoints += source.Primitives[primitiveIndices[i]].Points.Count;
            }

            if (totalSourcePoints == 0)
                return preview;

            int remainingPoints = limits.MaxPreviewPoints;
            long remainingSourcePoints = totalSourcePoints;

            for (int i = 0; i < primitiveIndices.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CadDocumentService.CadPrimitiveData sourcePrimitive =
                    source.Primitives[primitiveIndices[i]];

                int allowedPoints = CalculatePointBudget(
                    sourcePrimitive.Points.Count,
                    remainingSourcePoints,
                    remainingPoints,
                    primitiveIndices.Count - i);
                remainingSourcePoints -= sourcePrimitive.Points.Count;

                var previewPoints = SamplePoints(
                    sourcePrimitive.Points,
                    allowedPoints,
                    cancellationToken);
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

                remainingPoints -= previewPoints.Count;
            }

            return preview;
        }

        private static List<int> SelectPrimitiveIndices(
            IList<CadDocumentService.CadPrimitiveData> primitives,
            int maximumCount,
            CancellationToken cancellationToken)
        {
            var drawableIndices = new List<int>();
            for (int i = 0; i < primitives.Count; i++)
            {
                if ((i & 2047) == 0)
                    cancellationToken.ThrowIfCancellationRequested();
                CadDocumentService.CadPrimitiveData primitive = primitives[i];
                if (primitive?.Points != null && primitive.Points.Count >= 2)
                    drawableIndices.Add(i);
            }

            if (drawableIndices.Count <= maximumCount)
                return drawableIndices;
            if (maximumCount <= 1)
                return new List<int> { drawableIndices[0] };

            var selected = new List<int>(maximumCount);
            long denominator = maximumCount - 1L;
            long lastDrawableIndex = drawableIndices.Count - 1L;
            for (int i = 0; i < maximumCount; i++)
            {
                if ((i & 2047) == 0)
                    cancellationToken.ThrowIfCancellationRequested();
                int sourceIndex =
                    (int)((i * lastDrawableIndex + denominator / 2L) / denominator);
                selected.Add(drawableIndices[sourceIndex]);
            }

            return selected;
        }

        private static int CalculatePointBudget(
            int sourceCount,
            long totalSourcePoints,
            int remainingPoints,
            int remainingPrimitiveCount)
        {
            int reservedForLater = Math.Max(0, remainingPrimitiveCount - 1) * 2;
            int maximumForCurrent = Math.Max(2, remainingPoints - reservedForLater);
            if (totalSourcePoints <= remainingPoints)
                return Math.Min(sourceCount, maximumForCurrent);

            long proportional = ((long)sourceCount * remainingPoints + totalSourcePoints - 1)
                / totalSourcePoints;
            int budget = (int)Math.Min(sourceCount, proportional);
            budget = Math.Max(2, budget);
            return Math.Min(budget, maximumForCurrent);
        }

        private static List<CadDocumentService.CadCoordinate> SamplePoints(
            IList<CadDocumentService.CadCoordinate> source,
            int count,
            CancellationToken cancellationToken)
        {
            if (count >= source.Count)
            {
                var copy = new List<CadDocumentService.CadCoordinate>(source.Count);
                for (int i = 0; i < source.Count; i++)
                {
                    if ((i & 2047) == 0)
                        cancellationToken.ThrowIfCancellationRequested();
                    copy.Add(CloneCoordinate(source[i]));
                }
                return copy;
            }

            var sampled = new List<CadDocumentService.CadCoordinate>(count);
            long denominator = count - 1L;
            long sourceLastIndex = source.Count - 1L;
            for (int i = 0; i < count; i++)
            {
                if ((i & 2047) == 0)
                    cancellationToken.ThrowIfCancellationRequested();
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

    }
}
