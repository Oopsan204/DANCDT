using System;
using System.Collections.Generic;
using System.Windows;

namespace DACDT_2026
{
    internal static class CadPathPointSampler
    {
        public static IReadOnlyList<Point> Sample(IReadOnlyList<Point> source, int maxPointCount)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (maxPointCount < 2)
                throw new ArgumentOutOfRangeException(nameof(maxPointCount));
            if (source.Count <= maxPointCount)
                return source;

            var sampled = new List<Point>(maxPointCount);
            long denominator = maxPointCount - 1L;
            long sourceLastIndex = source.Count - 1L;
            for (int i = 0; i < maxPointCount; i++)
            {
                int sourceIndex =
                    (int)((i * sourceLastIndex + denominator / 2L) / denominator);
                sampled.Add(source[sourceIndex]);
            }

            return sampled;
        }
    }
}
