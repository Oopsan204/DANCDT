# Huong Dan Su Dung NDA_DXF.dll

## 1. File can copy

Sau khi build, copy file:

```text
DxfLibrary\dist\NDA_DXF.dll
```

vao du an moi cua ban.

## 2. Them reference vao du an moi

Voi Visual Studio:

1. Chuot phai vao project can dung.
2. Chon `Add` -> `Reference...`.
3. Chon `Browse`.
4. Chon file `NDA_DXF.dll`.
5. Bam `OK`.

## 3. Code mau

```csharp
using System;
using NDA_DXF;

class Program
{
    static void Main()
    {
        DxfLoadResult result = DxfReader.Load(@"D:\path\file.dxf");

        foreach (DxfSegment segment in result.Segments)
        {
            Console.WriteLine(segment.MotionType);
            Console.WriteLine("Start: " + segment.Start.X + ";" + segment.Start.Y + ";" + segment.Start.Z);
            Console.WriteLine("End: " + segment.End.X + ";" + segment.End.Y + ";" + segment.End.Z);

            if (segment.Center != null)
            {
                Console.WriteLine("Center: " + segment.Center.X + ";" + segment.Center.Y + ";" + segment.Center.Z);
                Console.WriteLine("Radius: " + segment.Radius);
            }
        }
    }
}
```

## 4. Du lieu tra ve

`DxfReader.Load(...)` tra ve `DxfLoadResult`.

`DxfLoadResult.Segments` la danh sach cac duong doc duoc trong file DXF.

Moi `DxfSegment` co cac truong chinh:

- `Index`: so thu tu
- `MotionType`: `Line`, `Arc CCW`, hoac `Circle`
- `Start`: diem bat dau
- `End`: diem ket thuc
- `Center`: tam cung/tam tron, chi co voi `Arc CCW` va `Circle`
- `Radius`: ban kinh, chi co voi `Arc CCW` va `Circle`
- `Points`: danh sach diem noi suy de ve lai hoac ghep bien dang

