using System;

namespace DACDT_2026
{
    public static class ZHeightSetting
    {
        private const double PlcUnitsPerMillimetre = 10000d;

        public static bool TryConvertToPlcUnits(string text, out int plcValue)
        {
            plcValue = 0;
            if (!DecimalInputParser.TryParseFlexibleDouble(text, out double millimetres)
                || double.IsNaN(millimetres)
                || double.IsInfinity(millimetres)
                || millimetres < 0)
            {
                return false;
            }

            double scaled = millimetres * PlcUnitsPerMillimetre;
            if (scaled > int.MaxValue)
                return false;

            plcValue = checked((int)Math.Round(scaled, MidpointRounding.AwayFromZero));
            return true;
        }
    }
}
