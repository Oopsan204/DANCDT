using System;
using System.IO;
using NDA_DXF;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            if (string.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
            {
                RunSelfTest();
                return 0;
            }

            PrintSegments(args[0]);
            return 0;
        }

        Console.WriteLine("Nhap hoac keo-tha duong dan file .dxf vao day.");
        Console.WriteLine("Nhan Enter de chay self-test mau:");
        Console.Write("> ");

        string inputPath = (Console.ReadLine() ?? string.Empty).Trim().Trim('"');
        if (!string.IsNullOrWhiteSpace(inputPath))
        {
            PrintSegments(inputPath);
            return 0;
        }

        RunSelfTest();
        return 0;
    }

    private static void RunSelfTest()
    {
        string samplePath = Path.Combine(AppContext.BaseDirectory, "Samples", "line_arc_circle.dxf");
        DxfLoadResult result = DxfReader.Load(samplePath);

        AssertEqual(3, result.Segments.Count, "segment count");

        DxfSegment line = result.Segments[0];
        AssertEqual("Line", line.MotionType, "line motion type");
        AssertClose(0, line.Start.X, "line start x");
        AssertClose(0, line.Start.Y, "line start y");
        AssertClose(10, line.End.X, "line end x");
        AssertClose(0, line.End.Y, "line end y");

        DxfSegment arc = result.Segments[1];
        AssertEqual("Arc CCW", arc.MotionType, "arc motion type");
        AssertClose(5, arc.Center.X, "arc center x");
        AssertClose(5, arc.Center.Y, "arc center y");
        AssertClose(5, arc.Radius, "arc radius");
        AssertClose(10, arc.Start.X, "arc start x");
        AssertClose(5, arc.Start.Y, "arc start y");
        AssertClose(5, arc.End.X, "arc end x");
        AssertClose(10, arc.End.Y, "arc end y");

        DxfSegment circle = result.Segments[2];
        AssertEqual("Circle", circle.MotionType, "circle motion type");
        AssertClose(20, circle.Center.X, "circle center x");
        AssertClose(20, circle.Center.Y, "circle center y");
        AssertClose(3, circle.Radius, "circle radius");
        AssertEqual(true, circle.Points.Count > 4, "circle sample points");

        Console.WriteLine("All DXF library tests passed.");
    }

    private static void PrintSegments(string filePath)
    {
        DxfLoadResult result = DxfReader.Load(filePath);
        Console.WriteLine($"File: {result.FilePath}");
        Console.WriteLine($"Segments: {result.Segments.Count}");
        Console.WriteLine($"Bounds: X {result.Bounds.MinX:0.###}..{result.Bounds.MaxX:0.###}, Y {result.Bounds.MinY:0.###}..{result.Bounds.MaxY:0.###}");

        foreach (DxfSegment segment in result.Segments)
        {
            string center = segment.Center == null
                ? ""
                : $" Center=({segment.Center.X:0.###},{segment.Center.Y:0.###},{segment.Center.Z:0.###}) R={segment.Radius:0.###}";

            Console.WriteLine(
                $"{segment.Index}. {segment.MotionType} " +
                $"Start=({segment.Start.X:0.###},{segment.Start.Y:0.###},{segment.Start.Z:0.###}) " +
                $"End=({segment.End.X:0.###},{segment.End.Y:0.###},{segment.End.Z:0.###})" +
                center);
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
        }
    }

    private static void AssertClose(double expected, double actual, string name)
    {
        if (Math.Abs(expected - actual) > 0.001)
        {
            throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
        }
    }
}
