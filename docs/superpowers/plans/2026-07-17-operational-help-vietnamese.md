# Vietnamese Operational Help Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Help page copy with a concise Vietnamese guide for operating the DACDT 2026 system.

**Architecture:** Keep `HelpView.xaml` as the same scrollable, styled WPF view and replace only the text content inside its existing `StackPanel`. Extend the existing console regression suite with source-content assertions so the operator sections remain Vietnamese and developer-only material does not return.

**Tech Stack:** C#/.NET Framework 4.8, WPF/XAML, legacy MSBuild project files, custom console test executable.

## Global Constraints

- Keep only instructions needed by an operator to prepare, run, monitor, stop, and safely shut down the system.
- Cover PLC connection, safety checks, jog/home/reset, DXF/G-code loading, data transfer, run controls, basic camera/log/settings usage, shutdown, troubleshooting, and a pre-run checklist.
- Remove introductory filler, internal PLC memory addresses, MQTT/WebRTC implementation details, build/debug instructions, and other developer-facing information.
- Keep the existing Help view layout, shared styles, scrolling behavior, and XAML resources unchanged.
- Write all Help copy in Vietnamese; keep visible control names such as `RUN`, `PAUSE`, `STOP`, `EXIT APP`, and `EMERGENCY STOP` where they match the UI.
- Do not reintroduce Telemetry references.

---

### Task 1: Add the Help content regression test first

**Files:**
- Modify: `D:/DACDT_2026/DANCDT/tests/DACDT_2026.Tests/Program.cs`

**Interfaces:**
- Consumes: `GetRepositoryPath` and `AssertTrue` already used by the console test suite.
- Produces: `HelpViewContainsVietnameseOperationalGuide`, a source-level contract for the new Help content.

- [ ] **Step 1: Write the failing test**

Add `HelpViewContainsVietnameseOperationalGuide();` in `Main()` immediately before `TelemetryFeatureIsRemoved();`, then add this method next to the existing XAML contract checks:

```csharp
private static void HelpViewContainsVietnameseOperationalGuide()
{
    string help = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "HelpView.xaml"));
    string[] requiredSections =
    {
        "An toàn trước khi vận hành",
        "Khởi động và kết nối PLC",
        "Jog tay, HOME và RESET",
        "Mở và kiểm tra file DXF/G-code",
        "Gửi dữ liệu và chạy chương trình",
        "RUN / PAUSE / CONTINUE / STOP",
        "Camera, Logs và Settings",
        "Thoát app, xử lý lỗi và checklist"
    };

    foreach (string section in requiredSections)
        AssertTrue(help.Contains(section), "Help must contain the Vietnamese operator section: " + section);

    string[] forbiddenContent =
    {
        "Telemetry",
        "TelemetryView",
        "MarkupCompilePass1",
        "WebRtcCameraService",
        "M2000",
        "M210",
        "M213",
        "App không build được"
    };

    foreach (string phrase in forbiddenContent)
        AssertTrue(!help.Contains(phrase), "Help must not contain developer/internal content: " + phrase);
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run from `D:/DACDT_2026/DANCDT`:

```powershell
dotnet msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug
& .\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected: FAIL with a missing required section such as `Help must contain the Vietnamese operator section: An toàn trước khi vận hành`, because the current Help still uses the old section names.

### Task 2: Rewrite only HelpView content

**Files:**
- Modify: `D:/DACDT_2026/DANCDT/src/DACDT_2026.App/Views/HelpView.xaml`

**Interfaces:**
- Consumes: existing `PanelBorderStyle`, `PanelTitleStyle`, `PanelSubtitleStyle`, `HelpCalloutBrush`, `HelpCodeBrush`, and shared WPF resources.
- Produces: the same Help view layout with Vietnamese operator instructions.

- [ ] **Step 1: Preserve the existing view shell**

Keep the `<UserControl>`, resource dictionary, `<ScrollViewer>`, outer `<Border>`, and existing styles unchanged. Keep the title `HƯỚNG DẪN SỬ DỤNG` and replace the English subtitle with `Hướng dẫn vận hành hệ thống DACDT 2026`.

- [ ] **Step 2: Replace the body with eight operator sections**

Replace all existing body sections inside the `StackPanel` after the title/subtitle with these headings and content themes, in this order:

1. `1. An toàn trước khi vận hành`: kiểm tra vùng làm việc, vật cản, nguồn máy, vị trí EMERGENCY STOP; dùng EMERGENCY STOP khi có nguy hiểm.
2. `2. Khởi động và kết nối PLC`: bật máy, kiểm tra mạng/PLC, nhập thông tin kết nối, nhấn `CONNECT PLC Q`, xác nhận trạng thái Connected/Ready.
3. `3. Jog tay, HOME và RESET`: đặt tốc độ thấp, giữ nút jog để di chuyển và nhả để dừng, HOME khi vùng máy an toàn, RESET sau khi xử lý lỗi.
4. `4. Mở và kiểm tra file DXF/G-code`: mở file trong `DXF / GCODE Run`, kiểm tra preview, thứ tự đường chạy, điểm bắt đầu, workspace, tốc độ và WCS.
5. `5. Gửi dữ liệu và chạy chương trình`: kết nối PLC, mở file, kiểm tra dữ liệu, kiểm tra thông số, gửi dữ liệu, rồi mới nhấn `RUN`.
6. `6. RUN / PAUSE / CONTINUE / STOP`: giải thích tác dụng vận hành của từng nút; phân biệt `STOP` là dừng theo quy trình với `EMERGENCY STOP` là dừng khẩn cấp.
7. `7. Camera, Logs và Settings`: chỉ hướng dẫn mở camera trong `Monitor`, xem lỗi trong `Logs`, và chỉnh các thông số vận hành cần thiết trong `Settings`.
8. `8. Thoát app, xử lý lỗi và checklist`: dùng `EXIT APP`, kiểm tra nguyên nhân PLC/file/camera khi lỗi, và checklist ngắn trước khi chạy thật.

Use short Vietnamese paragraphs or numbered lines. Do not include raw PLC memory addresses, MQTT/WebRTC implementation terms, build instructions, internal class/service names, or Telemetry text.

- [ ] **Step 3: Run the regression test**

Run:

```powershell
dotnet msbuild tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj /t:Build /p:Configuration=Debug
& .\tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe
```

Expected: PASS with `All tests passed.`

### Task 3: Verify the focused change

**Files:**
- No additional source files; inspect the Help and test diffs only.

- [ ] **Step 1: Check the Help content and layout contract**

Run:

```powershell
rg -n -i 'Telemetry|TelemetryView|MarkupCompilePass1|WebRtcCameraService|M2000|M210|M213|App không build được' src\DACDT_2026.App\Views\HelpView.xaml
rg -n 'PanelBorderStyle|PanelTitleStyle|PanelSubtitleStyle|ScrollViewer|HelpCalloutBrush' src\DACDT_2026.App\Views\HelpView.xaml
```

Expected: the first command returns no matches; the second command confirms the existing view shell and shared resources remain present.

- [ ] **Step 2: Review diff hygiene**

Run:

```powershell
git diff --check
git diff -- src/DACDT_2026.App/Views/HelpView.xaml tests/DACDT_2026.Tests/Program.cs
```

Expected: only Help content and its focused regression test are added to the pre-existing worktree changes.
