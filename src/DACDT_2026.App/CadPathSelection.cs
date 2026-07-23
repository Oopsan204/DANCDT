using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DACDT_2026
{
    public static class CadPathSelection
    {
        public static List<List<CadDocumentService.CadPrimitiveData>> GroupConnectedPaths(
            List<CadDocumentService.CadPrimitiveData> primitives,
            bool isGcode = false)
        {
            var paths = new List<List<CadDocumentService.CadPrimitiveData>>();
            if (primitives == null || primitives.Count == 0)
                return paths;

            string KeyOf(CadDocumentService.CadCoordinate point) => isGcode
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.000}|{1:0.000}|{2:0.000}", point.X, point.Y, point.Z)
                : string.Format(CultureInfo.InvariantCulture, "{0:0.000}|{1:0.000}", point.X, point.Y);

            var startMap = new Dictionary<string, List<int>>(primitives.Count);
            var endMap = new Dictionary<string, List<int>>(primitives.Count);
            var assigned = new bool[primitives.Count];

            for (int i = 0; i < primitives.Count; i++)
            {
                var primitive = primitives[i];
                if (primitive.Points == null || primitive.Points.Count == 0)
                {
                    assigned[i] = true;
                    continue;
                }

                string startKey = KeyOf(primitive.Points[0]);
                string endKey = KeyOf(primitive.Points[primitive.Points.Count - 1]);

                if (!startMap.TryGetValue(startKey, out var starts))
                {
                    starts = new List<int>();
                    startMap[startKey] = starts;
                }
                starts.Add(i);

                if (!endMap.TryGetValue(endKey, out var ends))
                {
                    ends = new List<int>();
                    endMap[endKey] = ends;
                }
                ends.Add(i);
            }

            int searchFrom = 0;
            while (true)
            {
                int seed = -1;
                for (int i = searchFrom; i < primitives.Count; i++)
                {
                    if (!assigned[i])
                    {
                        seed = i;
                        searchFrom = i + 1;
                        break;
                    }
                }
                if (seed < 0)
                    break;

                var currentPath = new List<CadDocumentService.CadPrimitiveData>
                {
                    primitives[seed]
                };
                assigned[seed] = true;

                bool grew = true;
                while (grew)
                {
                    grew = false;
                    var tail = currentPath[currentPath.Count - 1];
                    if (tail.Points == null || tail.Points.Count == 0)
                        break;
                    string tailKey = KeyOf(tail.Points[tail.Points.Count - 1]);

                    if (startMap.TryGetValue(tailKey, out var candidateStarts))
                    {
                        foreach (int candidateIndex in candidateStarts)
                        {
                            if (assigned[candidateIndex])
                                continue;
                            currentPath.Add(primitives[candidateIndex]);
                            assigned[candidateIndex] = true;
                            grew = true;
                            break;
                        }
                    }
                    if (grew)
                        continue;

                    if (endMap.TryGetValue(tailKey, out var candidateEnds))
                    {
                        foreach (int candidateIndex in candidateEnds)
                        {
                            if (assigned[candidateIndex])
                                continue;
                            var candidate = ReversePrimitiveForPath(primitives[candidateIndex]);
                            currentPath.Add(candidate);
                            assigned[candidateIndex] = true;
                            grew = true;
                            break;
                        }
                    }
                }

                grew = true;
                while (grew)
                {
                    grew = false;
                    var head = currentPath[0];
                    if (head.Points == null || head.Points.Count == 0)
                        break;
                    string headKey = KeyOf(head.Points[0]);

                    if (endMap.TryGetValue(headKey, out var candidateEnds))
                    {
                        foreach (int candidateIndex in candidateEnds)
                        {
                            if (assigned[candidateIndex])
                                continue;
                            currentPath.Insert(0, primitives[candidateIndex]);
                            assigned[candidateIndex] = true;
                            grew = true;
                            break;
                        }
                    }
                    if (grew)
                        continue;

                    if (startMap.TryGetValue(headKey, out var candidateStarts))
                    {
                        foreach (int candidateIndex in candidateStarts)
                        {
                            if (assigned[candidateIndex])
                                continue;
                            var candidate = ReversePrimitiveForPath(primitives[candidateIndex]);
                            currentPath.Insert(0, candidate);
                            assigned[candidateIndex] = true;
                            grew = true;
                            break;
                        }
                    }
                }

                paths.Add(currentPath);
            }

            return paths;
        }

        private static CadDocumentService.CadPrimitiveData ReversePrimitiveForPath(
            CadDocumentService.CadPrimitiveData source)
        {
            if (source == null)
                return null;

            var reversed = new CadDocumentService.CadPrimitiveData
            {
                SourceType = source.SourceType,
                Points = source.Points == null
                    ? null
                    : new CadDocumentService.ReversedCadCoordinateList(source.Points),
                Center = source.Center,
                IsCw = source.IsCw,
                IsCircle = source.IsCircle,
                MCodeValue = source.MCodeValue,
                Speed = source.Speed,
                Dwell = source.Dwell,
                ProcessKind = source.ProcessKind,
                PathId = source.PathId,
                WcsIndex = source.WcsIndex
            };

            if (reversed.SourceType != null && reversed.SourceType.Contains("Arc"))
                reversed.IsCw = !reversed.IsCw;

            return reversed;
        }

        public static int AssignPathIds(
            IEnumerable<List<CadDocumentService.CadPrimitiveData>> paths)
        {
            if (paths == null)
                return 0;

            int pathId = 0;
            foreach (var path in paths)
            {
                if (path != null)
                {
                    foreach (var primitive in path)
                    {
                        if (primitive != null)
                            primitive.PathId = pathId;
                    }
                }
                pathId++;
            }
            return pathId;
        }

        public static bool ToggleProcessKind(
            IEnumerable<CadDocumentService.CadPrimitiveData> primitives,
            int pathId,
            string engraveKind,
            string cutKind)
        {
            if (primitives == null || pathId < 0)
                return false;

            var selected = primitives
                .Where(primitive => primitive != null && primitive.PathId == pathId)
                .ToList();
            if (selected.Count == 0)
                return false;

            bool switchToCut = selected.Any(primitive =>
                !string.Equals(primitive.ProcessKind, cutKind, StringComparison.OrdinalIgnoreCase));
            string nextKind = switchToCut ? cutKind : engraveKind;

            foreach (var primitive in selected)
                primitive.ProcessKind = nextKind;
            return true;
        }
    }
}
