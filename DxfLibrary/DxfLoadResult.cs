using System.Collections.Generic;

namespace NDA_DXF
{
    public sealed class DxfLoadResult
    {
        public DxfLoadResult()
        {
            Segments = new List<DxfSegment>();
            Bounds = new DxfBounds();
        }

        public string FilePath { get; set; }
        public DxfBounds Bounds { get; set; }
        public List<DxfSegment> Segments { get; set; }
    }
}
