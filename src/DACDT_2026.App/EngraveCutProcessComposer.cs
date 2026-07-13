using System.Collections.Generic;

namespace DACDT_2026
{
    public static class EngraveCutProcessComposer
    {
        public const string EngraveKind = "engrave";
        public const string CutKind = "cut";

        public sealed class ProcessRowData
        {
            public string Key { get; set; }
            public string ProcessKind { get; set; }
            public string Speed { get; set; }
            public string LaserPower { get; set; }
        }

        public static List<ProcessRowData> Compose(
            IEnumerable<ProcessRowData> engraveRows,
            IEnumerable<ProcessRowData> cutRows,
            string engraveSpeed,
            string engravePower,
            string cutSpeed,
            string cutPower)
        {
            var result = new List<ProcessRowData>();
            AddRows(result, engraveRows, EngraveKind, engraveSpeed, engravePower);
            AddRows(result, cutRows, CutKind, cutSpeed, cutPower);
            return result;
        }

        public static int MapLaserPowerPercentToPlcValue(double percent)
        {
            int plcValue = (int)System.Math.Round(450.0 + (percent / 100.0) * (2000.0 - 450.0));
            if (plcValue < 450) return 450;
            if (plcValue > 2000) return 2000;
            return plcValue;
        }

        public static bool TryMapLaserPowerText(string powerText, out int plcValue)
        {
            plcValue = 0;
            if (!DecimalInputParser.TryParseFlexibleDouble(powerText, out double percent))
                return false;

            plcValue = MapLaserPowerPercentToPlcValue(percent);
            return true;
        }

        public static bool TryGetLaserPowerPlcValue(
            IList<ProcessRowData> rows,
            int oneBasedActiveIndex,
            out int plcValue)
        {
            plcValue = 0;
            if (rows == null || oneBasedActiveIndex <= 0 || oneBasedActiveIndex > rows.Count)
                return false;

            return TryMapLaserPowerText(rows[oneBasedActiveIndex - 1]?.LaserPower, out plcValue);
        }

        private static void AddRows(
            List<ProcessRowData> result,
            IEnumerable<ProcessRowData> rows,
            string kind,
            string speed,
            string power)
        {
            if (rows == null)
                return;

            foreach (var row in rows)
            {
                if (row == null)
                    continue;

                result.Add(new ProcessRowData
                {
                    Key = row.Key,
                    ProcessKind = kind,
                    Speed = string.IsNullOrWhiteSpace(row.Speed) ? speed : row.Speed,
                    LaserPower = string.IsNullOrWhiteSpace(row.LaserPower) ? power : row.LaserPower
                });
            }
        }
    }
}
