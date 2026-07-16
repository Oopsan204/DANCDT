# Camera Recording Duration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Display MP4 recording time while active and the final MP4 duration plus file size after stopping.

**Architecture:** A small formatting helper produces stable operator-facing strings. `Form1.Camera` owns the recording start timestamp and a one-second UI timer. `WpfUiState` exposes the elapsed and completed display text without exposing frame count.

**Tech Stack:** C# 7.3, .NET Framework 4.8, WPF DispatcherTimer, existing executable test runner.

## Global Constraints

- Duration is measured from real start/stop timestamps, not video frame count.
- The change must not modify camera capture, MP4 encoder selection, or WebRTC behavior.
- Do not commit because the working tree contains unrelated ongoing changes.

---

### Task 1: Add tested recording-summary formatting

**Files:**
- Create: `src/DACDT_2026.App/CameraRecordingSummary.cs`
- Modify: `src/DACDT_2026.App/DACDT_2026.csproj`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

- [x] Write a failing test for `00:00:00`, `01:02:03`, and a MB-size summary.
- [x] Run the test runner and observe the missing-helper failure.
- [x] Implement `FormatElapsed`, `FormatFileSize`, and `FormatSavedText`.
- [x] Run the test runner and observe all tests pass.

### Task 2: Update WPF recording status

**Files:**
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/Form1.Camera.cs`
- Modify: `tests/DACDT_2026.Tests/Program.cs`

- [x] Write a failing source contract test requiring elapsed-time UI state, timer updates, and saved file-size reporting.
- [x] Run the test runner and observe the expected failure.
- [x] Store recording start time, update elapsed display once per second, and publish duration plus MP4 file size after close.
- [x] Run the full test suite and Release x86 build.
