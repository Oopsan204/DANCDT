# NDA_DXF.dll

Standalone DXF geometry reader.

Vietnamese usage guide: `HUONG_DAN_SU_DUNG.md`.

## Scope

This library reads only:

- `LINE`
- `ARC`
- `CIRCLE`

It ignores `LWPOLYLINE`, `POLYLINE`, `SPLINE`, `ELLIPSE`, `TEXT`, blocks, and all other DXF entities.

The output is pure geometry only. It does not include MCode, Dwell, Speed, PLC, QD75, offset, WPF, or machine-control logic.

## Usage

```csharp
using NDA_DXF;

DxfLoadResult result = DxfReader.Load(@"C:\path\file.dxf");

foreach (DxfSegment segment in result.Segments)
{
    Console.WriteLine(segment.MotionType);
    Console.WriteLine($"{segment.Start.X};{segment.Start.Y} -> {segment.End.X};{segment.End.Y}");
}
```

## Output

`DxfLoadResult.Segments` is a flat list. Each `DxfSegment` contains:

- `MotionType`: `Line`, `Arc CCW`, or `Circle`
- `Start`
- `End`
- `Center`: for arc/circle
- `Radius`: for arc/circle
- `IsClockwise`
- `Points`: sampled points for preview or path reconstruction

## Test Program

Run self-test:

```powershell
dotnet run --project DxfLibrary.Tests\DxfLibrary.Tests.csproj
```

Print segments from a DXF file:

```powershell
dotnet run --project DxfLibrary.Tests\DxfLibrary.Tests.csproj -- "D:\path\file.dxf"
```
