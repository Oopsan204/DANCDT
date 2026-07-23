# CAD lớn và chế độ offline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task with verification checkpoints.

**Goal:** Mở và hiển thị file CAD rất lớn mà không làm treo UI, giữ nguyên dữ liệu chạy PLC, đồng thời tắt toàn bộ runtime MQTT/web nhưng giữ PLC trực tiếp và camera cục bộ.

**Architecture:** Tách dữ liệu CAD gốc dùng cho chương trình PLC khỏi dữ liệu preview dùng cho WPF. Preview được lấy mẫu với giới hạn điểm/primitive cố định và được dựng một lần. Runtime offline được thể hiện bằng một policy dùng chung để ngăn khởi tạo MQTT, nhận/gửi CAD web và khởi động dịch vụ WebRTC web, không thay đổi giao tiếp PLC trực tiếp.

**Tech Stack:** C# .NET Framework 4.8, WPF, netDxf, MSBuild/Visual Studio, test console hiện có.

## Global Constraints

- Không cắt hoặc lấy mẫu dữ liệu CAD gốc dùng để tạo chương trình PLC.
- Không khởi tạo, kết nối, publish, subscribe hoặc xử lý MQTT/web trong runtime.
- Giữ điều khiển PLC trực tiếp và camera cục bộ.
- Chỉ thay đổi các luồng CAD lớn, MQTT/web và các test/build liên quan.
- Mọi mã production mới phải có test thất bại trước khi triển khai.

---

### Task 1: Tạo test đỏ cho giới hạn preview và chế độ offline

**Files:**
- Create: `src/DACDT_2026.App/CadPreviewBuilder.cs`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- Produces `CadPreviewBuilder.Build(CadDocumentService.CadLoadResult source, CadPreviewBuilder.Limits limits)`.
- Produces `OfflineRuntimePolicy.Enabled` and `OfflineRuntimePolicy.ShouldStartMqtt`, `OfflineRuntimePolicy.ShouldStartWebRtc`.

- [ ] **Step 1: Viết test thất bại cho preview lớn**

Thêm các lời gọi sau vào `Main()` trước nhóm test giao diện:

```csharp
LargeCadPreviewKeepsFullSourceAndCapsPreviewPoints();
LargeCadPreviewSamplesOneHugePolyline();
OfflineRuntimeDoesNotStartMqttOrWebRtc();
```

Thêm test tạo một primitive 500.000 điểm, gọi `CadPreviewBuilder.Build`, rồi kiểm tra:

```csharp
private static void LargeCadPreviewKeepsFullSourceAndCapsPreviewPoints()
{
    var source = NewCadDocumentWithPrimitive(500000);
    var preview = CadPreviewBuilder.Build(source, CadPreviewBuilder.DefaultLimits);

    Assert(source.Primitives[0].Points.Count == 500000, "source CAD data must remain complete");
    Assert(preview.Primitives.Sum(p => p.Points.Count) <= CadPreviewBuilder.DefaultLimits.MaxPreviewPoints,
        "preview must be capped");
}

private static void LargeCadPreviewSamplesOneHugePolyline()
{
    var source = NewCadDocumentWithPrimitive(500000);
    var preview = CadPreviewBuilder.Build(source, CadPreviewBuilder.DefaultLimits);

    Assert(preview.Primitives.Count == 1, "preview must keep the path as one primitive");
    Assert(preview.Primitives[0].Points.Count >= 2, "preview path must remain drawable");
    Assert(preview.Primitives[0].Points[0].X == source.Primitives[0].Points[0].X,
        "preview must keep the first point");
    Assert(preview.Primitives[0].Points[preview.Primitives[0].Points.Count - 1].X
        == source.Primitives[0].Points[source.Primitives[0].Points.Count - 1].X,
        "preview must keep the last point");
}

private static void OfflineRuntimeDoesNotStartMqttOrWebRtc()
{
    Assert(OfflineRuntimePolicy.Enabled, "offline runtime must be enabled");
    Assert(!OfflineRuntimePolicy.ShouldStartMqtt, "MQTT must not start");
    Assert(!OfflineRuntimePolicy.ShouldStartWebRtc, "WebRTC web service must not start");
}
```

Use a helper that creates `CadLoadResult` with one `CadPrimitiveData` containing the requested number of `CadCoordinate` instances. The helper must not call file I/O.

- [ ] **Step 2: Chạy test để xác nhận RED**

Run:

```powershell
dotnet msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug /v:minimal
& .\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected: build/test fail because `CadPreviewBuilder` and `OfflineRuntimePolicy` chưa tồn tại.

- [ ] **Step 3: Commit test đỏ**

```powershell
git add tests\DACDT_2026.Tests\Program.cs tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj
git commit -m "test: define large CAD offline behavior"
```

### Task 2: Implement preview CAD nhẹ và policy offline

**Files:**
- Create: `src/DACDT_2026.App/CadPreviewBuilder.cs`
- Create: `src/DACDT_2026.App/OfflineRuntimePolicy.cs`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`

**Interfaces:**
- `CadPreviewBuilder.Build` returns a new lightweight document and never mutates `source`.
- `CadPreviewBuilder.DefaultLimits.MaxPreviewPoints` is `1000000`.
- `CadPreviewBuilder.DefaultLimits.MaxPreviewPrimitives` is `50000`.
- `OfflineRuntimePolicy.Enabled == true`, `ShouldStartMqtt == false`, and `ShouldStartWebRtc == false`.

- [ ] **Step 1: Add production files to the test project**

Add these links to the test `.csproj`:

```xml
<Compile Include="..\..\src\DACDT_2026.App\CadPreviewBuilder.cs">
  <Link>CadPreviewBuilder.cs</Link>
</Compile>
<Compile Include="..\..\src\DACDT_2026.App\OfflineRuntimePolicy.cs">
  <Link>OfflineRuntimePolicy.cs</Link>
</Compile>
```

- [ ] **Step 2: Implement the minimal preview builder**

For each source primitive, preserve its metadata and copy only sampled points. Keep the first and last point; choose evenly spaced source indexes until the total preview budget is exhausted. Copy bounds, file metadata, and point rows only for the sampled preview points. Do not modify `source.Primitives` or `source.Points`.

- [ ] **Step 3: Implement the offline policy**

```csharp
namespace DACDT_2026
{
    internal static class OfflineRuntimePolicy
    {
        public static bool Enabled { get { return true; } }
        public static bool ShouldStartMqtt { get { return false; } }
        public static bool ShouldStartWebRtc { get { return false; } }
    }
}
```

- [ ] **Step 4: Run the focused tests to confirm GREEN**

Run the same build/test command from Task 1. Expected: the three new tests pass.

- [ ] **Step 5: Commit the implementation**

```powershell
git add src\DACDT_2026.App\CadPreviewBuilder.cs src\DACDT_2026.App\OfflineRuntimePolicy.cs tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj
git commit -m "feat: add bounded CAD preview and offline policy"
```

### Task 3: Use bounded preview for WPF without reducing PLC data

**Files:**
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs:525-707, 758-828, 1077-1135, 1184-1319`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs:91-322, 2435-2511`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- `activeCadDocument` remains the full source used by `BuildDxfRowsForProcessDocument`.
- `PushDxfStateAsync` uses a bounded display document for points, rows, geometry, image, and primitive lines.

- [ ] **Step 1: Add a regression assertion that full PLC source remains unchanged**

Extend `LargeCadPreviewKeepsFullSourceAndCapsPreviewPoints()` with these exact assertions after the preview call:

```csharp
Assert(source.Primitives.Count == 1, "source primitive count must remain unchanged");
Assert(source.Primitives[0].Points.Count == 500000, "source points must remain unchanged");
Assert(preview.Primitives.Sum(p => p.Points.Count) <= CadPreviewBuilder.DefaultLimits.MaxPreviewPoints,
    "preview point count must stay bounded");
```

- [ ] **Step 2: Run the regression test and confirm it fails against the current full-clone path**

Run the test executable. Expected: the new assertion fails because the current UI path clones/builds the full document instead of using a bounded preview.

- [ ] **Step 3: Replace full UI cloning with preview construction**

In `PushDxfStateAsync`, keep `snapDocSource` as the full document for PLC-related state, then build `displayDoc` from `CadPreviewBuilder.Build(snapDocSource, CadPreviewBuilder.DefaultLimits)` before applying display offsets. Do not call `CloneCadDocumentForUi` for the full document in the preview path.

- [ ] **Step 4: Build only one bounded preview geometry**

Use the bounded `displayDoc` for `BuildCadPreviewImage`, `BuildCadPreviewGeometry`, `BuildGeometryRows`, and `BuildCadPrimitiveLines`. Preserve process rows from the full source. Remove duplicate engrave/cut preview construction for large CAD and use the same bounded geometry unless the existing process split requires separate strokes.

- [ ] **Step 5: Replace `HandleScanLimitsAsync` list allocation with one-pass bounds**

Track `minX`, `maxX`, `minY`, and `maxY` while traversing primitive points. Do not allocate `allX` or `allY`.

- [ ] **Step 6: Run all tests and verify GREEN**

Run:

```powershell
dotnet msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug /v:minimal
& .\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected: all tests pass and the new large-CAD regression tests pass.

- [ ] **Step 7: Commit CAD optimization**

```powershell
git add src\DACDT_2026.App\Form1.StatePublisher.cs src\DACDT_2026.App\Form1.DxfHandler.cs tests\DACDT_2026.Tests\Program.cs
git commit -m "perf: bound large CAD UI preview"
```

### Task 4: Remove MQTT/web runtime while preserving PLC and local camera

**Files:**
- Modify: `src/DACDT_2026.App/Form1.cs:168-215, 840-916, 1084-1140`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs`
- Modify: `src/DACDT_2026.App/Form1.WebCadUpload.cs`
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs:176-279, 316-460`
- Modify: `src/DACDT_2026.App/Form1.Camera.cs`
- Modify: `src/DACDT_2026.App/WebRtcCameraServer.cs`
- Modify: `src/WebRtcCameraService/Program.cs`
- Modify: `tools/installer.iss`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- PLC polling and direct PLC commands remain callable.
- MQTT event handlers and web CAD upload handlers are not subscribed or invoked.
- Background WebRTC web service is not started or packaged as a runtime dependency.

- [ ] **Step 1: Add failing offline runtime integration assertions**

Add this source-level test to `Program.cs`; it reads the repository source without starting the WPF window:

```csharp
private static void OfflineRuntimeDoesNotStartMqttOrWebRtc()
{
    string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
    string form1 = File.ReadAllText(Path.Combine(root, @"src\DACDT_2026.App\Form1.cs"));
    string dxf = File.ReadAllText(Path.Combine(root, @"src\DACDT_2026.App\Form1.DxfHandler.cs"));
    string state = File.ReadAllText(Path.Combine(root, @"src\DACDT_2026.App\Form1.StatePublisher.cs"));
    string camera = File.ReadAllText(Path.Combine(root, @"src\DACDT_2026.App\Form1.Camera.cs"));

    Assert(!form1.Contains("await InitMqttAsync();"), "startup must not initialize MQTT");
    Assert(!form1.Contains("StartBackgroundVideoService();"), "startup must not launch WebRTC web service");
    Assert(!dxf.Contains("await PublishAllMqttAsync();"), "local CAD flow must not publish to MQTT");
    Assert(!state.Contains("private async Task PublishCadStateToMqttAsync"), "CAD MQTT publisher must be removed");
    Assert(!camera.Contains("webRtcBridgeClient.SendFrame"), "local camera must not send WebRTC frames");
}
```

- [ ] **Step 2: Run the assertions and confirm RED**

Run the test executable. Expected: current startup/open path still contains those calls, so the assertions fail.

- [ ] **Step 3: Disable MQTT/web startup and subscriptions**

Remove constructor event subscriptions and the `InitMqttAsync` call from the `Loaded` handler. Keep PLC polling and local camera refresh. Guard any remaining MQTT/web initialization with `OfflineRuntimePolicy` so the runtime cannot connect accidentally.

- [ ] **Step 4: Remove CAD MQTT/web calls from local CAD operations**

Remove `PublishAllMqttAsync` from local CAD open/preview/toggle flows. Local CAD open must finish after updating the WPF state and PLC-related data.

- [ ] **Step 5: Disable inbound MQTT/web command handling**

Do not subscribe to MQTT topics and do not route received messages to machine commands, camera web commands, or web CAD upload. Keep direct PLC methods untouched.

- [ ] **Step 6: Stop starting WebRTC web service while preserving local camera**

Guard `StartBackgroundVideoService` and `StopBackgroundVideoService` with the offline policy. Do not remove AForge/local camera capture paths.

- [ ] **Step 7: Remove web/MQTT runtime payloads from the installer**

Update `tools/installer.iss` so the app installer does not package `docs/index.html` or `WebRtcCameraService.exe` as runtime features. Do not remove local camera DLLs.

- [ ] **Step 8: Run all tests and verify GREEN**

Run the test build and executable. Expected: all existing PLC/camera/configuration tests pass and offline assertions pass.

- [ ] **Step 9: Commit offline runtime changes**

```powershell
git add src tools tests
git commit -m "refactor: remove MQTT and web runtime"
```

### Task 5: Release verification and deployment artifact

**Files:**
- Verify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Verify: `tools/installer.iss`
- Output: `artifacts/installer/DACDT_2026_Setup_V1.2.1.exe`

- [ ] **Step 1: Run the complete test suite**

```powershell
dotnet msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug /v:minimal
& .\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected output: `All tests passed.`

- [ ] **Step 2: Build Release x86**

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' src\DACDT_2026.App\DACDT_2026.csproj /t:Rebuild /p:Configuration=Release /p:Platform=x86 /v:minimal
```

Expected: `DACDT_2026.exe` and its local camera dependencies build successfully.

- [ ] **Step 3: Compile the installer**

```powershell
& 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' tools\installer.iss
```

Expected output: `artifacts\installer\DACDT_2026_Setup_V1.2.1.exe`.

- [ ] **Step 4: Verify artifact and working tree**

```powershell
Get-Item artifacts\installer\DACDT_2026_Setup_V1.2.1.exe | Select-Object FullName,Length,LastWriteTime
Get-FileHash artifacts\installer\DACDT_2026_Setup_V1.2.1.exe -Algorithm SHA256
git diff --check
git status --short
```

- [ ] **Step 5: Commit only if release metadata changed**

```powershell
git add tools\installer.iss src\DACDT_2026.App\Properties\AssemblyInfo.cs
git commit -m "build: prepare offline CAD release"
```
