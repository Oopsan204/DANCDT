using System;
using System.Collections.Generic;
using System.Threading;

namespace DACDT_2026
{
    internal static class CadDisplayDocumentBuilder
    {
        public static CadDocumentService.CadLoadResult Build(
            CadDocumentService.CadLoadResult source,
            double offsetX,
            double offsetY,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CadDocumentService.CadLoadResult display =
                CadPreviewBuilder.Build(source, CadPreviewBuilder.DefaultLimits, cancellationToken);
            if (display == null)
                return null;

            bool anyOffset = Math.Abs(offsetX) > 1e-9 || Math.Abs(offsetY) > 1e-9;
            if (!anyOffset)
                return display;

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double minZ = double.MaxValue;
            double maxZ = double.MinValue;

            List<CadDocumentService.CadPrimitiveData> primitives = display.Primitives;
            if (primitives != null)
            {
                for (int primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CadDocumentService.CadPrimitiveData primitive = primitives[primitiveIndex];

                    IList<CadDocumentService.CadCoordinate> points = primitive?.Points;
                    if (points != null)
                    {
                        for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
                        {
                            if ((pointIndex & 2047) == 0)
                                cancellationToken.ThrowIfCancellationRequested();

                            CadDocumentService.CadCoordinate point = points[pointIndex];
                            if (point == null)
                                continue;

                            point.X += offsetX;
                            point.Y += offsetY;
                            Include(
                                point.X,
                                point.Y,
                                point.Z,
                                ref minX,
                                ref minY,
                                ref maxX,
                                ref maxY,
                                ref minZ,
                                ref maxZ);
                        }
                    }

                    if (primitive?.Center != null)
                    {
                        primitive.Center.X += offsetX;
                        primitive.Center.Y += offsetY;
                        Include(
                            primitive.Center.X,
                            primitive.Center.Y,
                            primitive.Center.Z,
                            ref minX,
                            ref minY,
                            ref maxX,
                            ref maxY,
                            ref minZ,
                            ref maxZ);
                    }
                }
            }

            display.Bounds = BuildBounds(minX, minY, maxX, maxY, minZ, maxZ);
            return display;
        }

        private static void Include(
            double x,
            double y,
            double z,
            ref double minX,
            ref double minY,
            ref double maxX,
            ref double maxY,
            ref double minZ,
            ref double maxZ)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            minZ = Math.Min(minZ, z);
            maxZ = Math.Max(maxZ, z);
        }

        private static CadDocumentService.CadBounds BuildBounds(
            double minX,
            double minY,
            double maxX,
            double maxY,
            double minZ,
            double maxZ)
        {
            if (minX == double.MaxValue)
            {
                return new CadDocumentService.CadBounds
                {
                    Left = 0,
                    Top = 0,
                    Right = 100,
                    Bottom = 100,
                    Width = 100,
                    Height = 100,
                    MinZ = 0,
                    MaxZ = 0
                };
            }

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
    }
}
