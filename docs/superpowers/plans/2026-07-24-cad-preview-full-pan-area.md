# CAD Preview Full Pan Area Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cho phép CAD được pan vào toàn bộ vùng CAD Preview mà vẫn giữ đúng khung làm việc `1000×620`.

**Architecture:** Giữ nguyên `Viewbox`, hệ tọa độ CAD và toàn bộ code pan/zoom. Bỏ cắt ở hai lớp con `CadPreviewViewbox` và `CadSurface`; chỉ `CadViewport` được phép cắt nội dung tại biên panel.

**Tech Stack:** C#/.NET Framework, WPF XAML, bộ kiểm thử console `DACDT_2026.Tests`.

## Global Constraints

- Không thay đổi dữ liệu DXF, G-code hoặc dữ liệu lệnh PLC.
- Không thay đổi thuật toán pan, zoom, touch và hit-test.
- Khung vùng làm việc tiếp tục dùng hệ tọa độ `1000×620`.

---

### Task 1: Bảo vệ cấu trúc cắt của CAD Preview

**Files:**
- Modify: `tests/DACDT_2026.Tests/Program.cs`
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml`

**Interfaces:**
- Consumes: tên XAML `CadViewport`, `CadPreviewViewbox`, `CadSurface`.
- Produces: quy tắc chỉ `CadViewport` có `ClipToBounds="True"`.

- [ ] **Step 1: Viết kiểm thử thất bại**

Thêm lời gọi `CadPreviewClipsOnlyAtOuterViewport();` trong `Main`, rồi thêm:

```csharp
private static void CadPreviewClipsOnlyAtOuterViewport()
{
    string xaml = File.ReadAllText(GetRepositoryPath(
        "src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));

    AssertTrue(xaml.Contains(
            "<Border x:Name=\"CadViewport\"")
        && xaml.Contains("ClipToBounds=\"True\""),
        "CAD preview must clip at its outer viewport.");
    AssertTrue(xaml.Contains(
            "<Viewbox x:Name=\"CadPreviewViewbox\" Stretch=\"Uniform\" ClipToBounds=\"False\">"),
        "CAD Viewbox must allow panned content to render into letterbox space.");
    AssertTrue(xaml.Contains(
            "<Canvas x:Name=\"CadSurface\" Width=\"1000\" Height=\"620\" ClipToBounds=\"False\">"),
        "CAD surface must not clip content before it reaches the outer viewport.");
}
```

- [ ] **Step 2: Chạy kiểm thử để xác nhận RED**

Run:

```powershell
dotnet msbuild tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj -t:Build -p:Configuration=Debug -v:minimal
tests/DACDT_2026.Tests/bin/Debug/DACDT_2026.Tests.exe
```

Expected: FAIL với thông báo `CAD Viewbox must allow panned content to render into letterbox space.`

- [ ] **Step 3: Sửa XAML tối thiểu**

Đổi phần mở hai lớp con thành:

```xml
<Viewbox x:Name="CadPreviewViewbox" Stretch="Uniform" ClipToBounds="False">
    <Canvas x:Name="CadSurface" Width="1000" Height="620" ClipToBounds="False">
```

Giữ `ClipToBounds="True"` trên `CadViewport`.

- [ ] **Step 4: Chạy kiểm thử và build**

Run:

```powershell
dotnet msbuild tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj -t:Build -p:Configuration=Debug -v:minimal
tests/DACDT_2026.Tests/bin/Debug/DACDT_2026.Tests.exe
```

Expected: `All tests passed.`

Run:

```powershell
dotnet build src/DACDT_2026.App/DACDT_2026.csproj -c Release
```

Expected: exit code `0`.

- [ ] **Step 5: Rà soát thay đổi**

Run:

```powershell
git diff --check
git diff -- src/DACDT_2026.App/Views/DxfRunView.xaml tests/DACDT_2026.Tests/Program.cs
```

Expected: không có lỗi khoảng trắng; diff chỉ liên quan quy tắc cắt CAD Preview và kiểm thử hồi quy.
