namespace NDA_DXF
{
    public sealed class DxfPoint
    {
        public DxfPoint()
        {
        }

        public DxfPoint(double x, double y, double z = 0.0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
}
