using System;

namespace DACDT_2026
{
    internal static class WorkspaceLimitPolicy
    {
        public static bool IsValid(double width, double height)
            => IsFinitePositive(width) && IsFinitePositive(height);

        public static bool IsRangeWithin(double minimum, double maximum, double limit)
            => IsFinitePositive(limit)
               && !double.IsNaN(minimum)
               && !double.IsInfinity(minimum)
               && !double.IsNaN(maximum)
               && !double.IsInfinity(maximum)
               && minimum >= 0.0
               && maximum <= limit;

        private static bool IsFinitePositive(double value)
            => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
    }
}
