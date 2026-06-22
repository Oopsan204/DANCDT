using System.Collections.Generic;

namespace NDA_DXF
{
    public sealed class DxfSegment
    {
        public DxfSegment()
        {
            Points = new List<DxfPoint>();
        }

        public int Index { get; set; }
        public string MotionType { get; set; }
        public DxfPoint Start { get; set; }
        public DxfPoint End { get; set; }
        public DxfPoint Center { get; set; }
        public double Radius { get; set; }
        public bool IsClockwise { get; set; }
        public List<DxfPoint> Points { get; set; }
    }
}
