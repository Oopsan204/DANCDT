using System;
using System.Collections.Generic;
using System.Threading;

namespace DACDT_2026
{
    internal static class CadDisplayDocumentBuilder
    {
        public static CadDocumentService.CadLoadResult Build(
            CadDocumentService.CadLoadResult source,
            bool isGcodeKind,
            double dxfOffsetX,
            double dxfOffsetY,
            double[] displayWcsOffsetX,
            double[] displayWcsOffsetY,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CadDocumentService.CadLoadResult display =
                CadPreviewBuilder.Build(source, CadPreviewBuilder.DefaultLimits, cancellationToken);
            if (display == null)
                return null;

            bool anyOffset = isGcodeKind
                ? HasAnyOffset(displayWcsOffsetX) || HasAnyOffset(displayWcsOffsetY)
                : Math.Abs(dxfOffsetX) > 1e-9 || Math.Abs(dxfOffsetY) > 1e-9;
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
                    GetDisplayOffset(
                        primitive,
                        isGcodeKind,
                        dxfOffsetX,
                        dxfOffsetY,
                        displayWcsOffsetX,
                        displayWcsOffsetY,
                        out double ox,
                        out double oy);

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

                            point.X += ox;
                            point.Y += oy;
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
                        primitive.Center.X += ox;
                        primitive.Center.Y += oy;
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

        private static bool HasAnyOffset(double[] values)
        {
            if (values == null)
                return false;

            for (int i = 0; i < values.Length; i++)
            {
                if (Math.Abs(values[i]) > 1e-9)
                    return true;
            }

            return false;
        }

        private static void GetDisplayOffset(
            CadDocumentService.CadPrimitiveData primitive,
            bool isGcodeKind,
            double dxfOffsetX,
            double dxfOffsetY,
            double[] displayWcsOffsetX,
            double[] displayWcsOffsetY,
            out double ox,
            out double oy)
        {
            if (!isGcodeKind)
            {
                ox = dxfOffsetX;
                oy = dxfOffsetY;
                return;
            }

            int wcsIndex = Math.Max(0, Math.Min(5, primitive?.WcsIndex ?? 0));
            ox = displayWcsOffsetX != null && displayWcsOffsetX.Length > wcsIndex
                ? displayWcsOffsetX[wcsIndex]
                : 0.0;
            oy = displayWcsOffsetY != null && displayWcsOffsetY.Length > wcsIndex
                ? displayWcsOffsetY[wcsIndex]
                : 0.0;
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
