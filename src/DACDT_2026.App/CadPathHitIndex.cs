using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace DACDT_2026
{
    public sealed class CadHitPath
    {
        public CadHitPath(int pathId, IEnumerable<Point> points)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            PathId = pathId;
            Points = new ReadOnlyCollection<Point>(new List<Point>(points));
        }

        public int PathId { get; private set; }

        public IReadOnlyList<Point> Points { get; private set; }
    }

    public sealed class CadPathHitIndex
    {
        private const double TieToleranceSquared = 1e-12;

        private readonly Dictionary<GridCell, List<int>> segmentIdsByCell;
        private readonly Dictionary<int, IReadOnlyList<Point>> pointsByPathId;
        private readonly Segment[] segments;
        private readonly double cellSize;

        private CadPathHitIndex(
            double cellSize,
            Dictionary<GridCell, List<int>> segmentIdsByCell,
            Dictionary<int, IReadOnlyList<Point>> pointsByPathId,
            Segment[] segments)
        {
            this.cellSize = cellSize;
            this.segmentIdsByCell = segmentIdsByCell;
            this.pointsByPathId = pointsByPathId;
            this.segments = segments;
        }

        public static CadPathHitIndex Build(IEnumerable<CadHitPath> paths, double cellSize)
        {
            if (paths == null)
                throw new ArgumentNullException("paths");
            if (double.IsNaN(cellSize) || double.IsInfinity(cellSize) || cellSize <= 0)
                throw new ArgumentOutOfRangeException("cellSize");

            var segmentIdsByCell = new Dictionary<GridCell, List<int>>();
            var pointsByPathId = new Dictionary<int, IReadOnlyList<Point>>();
            var segments = new List<Segment>();

            foreach (CadHitPath path in paths)
            {
                if (path == null)
                    throw new ArgumentException("paths cannot contain null items.", "paths");
                if (!pointsByPathId.ContainsKey(path.PathId))
                    pointsByPathId.Add(path.PathId, path.Points);
                else
                    throw new ArgumentException("Each path must have a unique path id.", "paths");

                for (int i = 1; i < path.Points.Count; i++)
                {
                    var segment = new Segment(
                        segments.Count,
                        path.PathId,
                        path.Points[i - 1],
                        path.Points[i]);
                    segments.Add(segment);

                    int minCellX = ToCell(segment.MinX, cellSize);
                    int maxCellX = ToCell(segment.MaxX, cellSize);
                    int minCellY = ToCell(segment.MinY, cellSize);
                    int maxCellY = ToCell(segment.MaxY, cellSize);

                    for (int cellX = minCellX; cellX <= maxCellX; cellX++)
                    {
                        for (int cellY = minCellY; cellY <= maxCellY; cellY++)
                        {
                            var cell = new GridCell(cellX, cellY);
                            List<int> segmentIds;
                            if (!segmentIdsByCell.TryGetValue(cell, out segmentIds))
                            {
                                segmentIds = new List<int>();
                                segmentIdsByCell.Add(cell, segmentIds);
                            }

                            segmentIds.Add(segment.Id);
                        }
                    }
                }
            }

            return new CadPathHitIndex(
                cellSize,
                segmentIdsByCell,
                pointsByPathId,
                segments.ToArray());
        }

        public bool TryFindNearest(Point point, double radius, out int pathId)
        {
            pathId = 0;
            if (!IsFinite(point.X) || !IsFinite(point.Y)
                || double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0)
                return false;

            double radiusSquared = radius * radius;
            if (double.IsInfinity(radiusSquared))
                radiusSquared = double.MaxValue;

            int minCellX = ToCell(point.X - radius, cellSize);
            int maxCellX = ToCell(point.X + radius, cellSize);
            int minCellY = ToCell(point.Y - radius, cellSize);
            int maxCellY = ToCell(point.Y + radius, cellSize);
            var candidateSegmentIds = new HashSet<int>();

            for (int cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                for (int cellY = minCellY; cellY <= maxCellY; cellY++)
                {
                    List<int> segmentIds;
                    if (!segmentIdsByCell.TryGetValue(new GridCell(cellX, cellY), out segmentIds))
                        continue;

                    foreach (int segmentId in segmentIds)
                        candidateSegmentIds.Add(segmentId);
                }
            }

            bool found = false;
            double bestDistanceSquared = double.MaxValue;
            int bestPathId = 0;
            foreach (int segmentId in candidateSegmentIds)
            {
                Segment segment = segments[segmentId];
                double distanceSquared = DistanceSquaredToSegment(point, segment.Start, segment.End);
                if (distanceSquared > radiusSquared)
                    continue;

                if (!found
                    || distanceSquared < bestDistanceSquared - TieToleranceSquared
                    || (Math.Abs(distanceSquared - bestDistanceSquared) <= TieToleranceSquared
                        && segment.PathId < bestPathId))
                {
                    found = true;
                    bestDistanceSquared = distanceSquared;
                    bestPathId = segment.PathId;
                }
            }

            if (found)
                pathId = bestPathId;
            return found;
        }

        public bool TryGetPathPoints(int pathId, out IReadOnlyList<Point> points)
        {
            return pointsByPathId.TryGetValue(pathId, out points);
        }

        private static int ToCell(double coordinate, double cellSize)
        {
            double cell = Math.Floor(coordinate / cellSize);
            if (cell < int.MinValue || cell > int.MaxValue)
                throw new ArgumentOutOfRangeException("coordinate", "The projected coordinate is outside the index range.");
            return (int)cell;
        }

        private static double DistanceSquaredToSegment(Point point, Point start, Point end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= 0)
                return DistanceSquared(point, start);

            double projection = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared;
            if (projection < 0)
                projection = 0;
            else if (projection > 1)
                projection = 1;

            double closestX = start.X + projection * dx;
            double closestY = start.Y + projection * dy;
            double deltaX = point.X - closestX;
            double deltaY = point.Y - closestY;
            return deltaX * deltaX + deltaY * deltaY;
        }

        private static double DistanceSquared(Point first, Point second)
        {
            double dx = first.X - second.X;
            double dy = first.Y - second.Y;
            return dx * dx + dy * dy;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private struct GridCell : IEquatable<GridCell>
        {
            private readonly int x;
            private readonly int y;

            public GridCell(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public bool Equals(GridCell other)
            {
                return x == other.x && y == other.y;
            }

            public override bool Equals(object obj)
            {
                return obj is GridCell && Equals((GridCell)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (x * 397) ^ y;
                }
            }
        }

        private struct Segment
        {
            public Segment(int id, int pathId, Point start, Point end)
            {
                Id = id;
                PathId = pathId;
                Start = start;
                End = end;
                MinX = Math.Min(start.X, end.X);
                MaxX = Math.Max(start.X, end.X);
                MinY = Math.Min(start.Y, end.Y);
                MaxY = Math.Max(start.Y, end.Y);
            }

            public int Id { get; private set; }
            public int PathId { get; private set; }
            public Point Start { get; private set; }
            public Point End { get; private set; }
            public double MinX { get; private set; }
            public double MaxX { get; private set; }
            public double MinY { get; private set; }
            public double MaxY { get; private set; }
        }
    }
}
