# DXF Engrave Cut Import Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add two DXF import buttons, one for engraving and one for cutting, then compile both files into one `processRows` program with separate colors, speed, and laser power.

**Architecture:** Reuse the current DXF parser and QD75 row pipeline. Add a small tested helper that tags and merges engrave/cut rows, then wire the WPF commands and preview layers around that helper.

**Tech Stack:** C# 7.3, WPF, .NET Framework 4.8, existing assert-style console test project.

## Global Constraints

- Keep one final `processRows` list; do not introduce separate runtime process tables.
- Use two import buttons: `Import Khac` and `Import Cat`.
- Show engraving and cutting paths in different preview colors.
- Power can change continuously; write the row's laser power when it changes.
- Run order is engraving first, cutting second.
- Do not add new dependencies.

---

### Task 1: Tested Row Composition

**Files:**
- Create: `src/DACDT_2026.App/EngraveCutProcessComposer.cs`
- Modify: `tests/DACDT_2026.Tests/Program.cs`
- Modify: `tests/DACDT_2026.Tests/DACDT_2026.Tests.csproj`

**Interfaces:**
- Produces: `EngraveCutProcessComposer.ProcessRowData`
- Produces: `EngraveCutProcessComposer.Compose(...)`

- [ ] Add a failing test proving engrave rows come before cut rows and each row receives the correct kind, speed, and power.
- [ ] Add the helper with the smallest implementation that passes the test.

### Task 2: Runtime Model And Settings

**Files:**
- Modify: `src/DACDT_2026.App/Form1.Models.cs`
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `src/DACDT_2026.App/Views/SettingsView.xaml`

**Interfaces:**
- Consumes: `EngraveCutProcessComposer`
- Produces: `ProcessRow.ProcessKind`
- Produces: `ProcessRow.LaserPower`

- [ ] Add engrave/cut speed and power fields with settings load/save.
- [ ] Surface those fields in WPF state and settings UI.

### Task 3: Two Import Buttons And Mixed Preview

**Files:**
- Modify: `src/DACDT_2026.App/WpfUiState.cs`
- Modify: `src/DACDT_2026.App/Form1.cs`
- Modify: `src/DACDT_2026.App/Form1.DxfHandler.cs`
- Modify: `src/DACDT_2026.App/Form1.StatePublisher.cs`
- Modify: `src/DACDT_2026.App/Views/DxfRunView.xaml`

**Interfaces:**
- Consumes: `ProcessRow.ProcessKind`
- Produces: `WpfUiState.CadEngravePreviewGeometry`
- Produces: `WpfUiState.CadCutPreviewGeometry`

- [ ] Add `ImportEngraveDxfCommand` and `ImportCutDxfCommand`.
- [ ] Load each DXF into its own document slot.
- [ ] Compose both slots into one `processRows`.
- [ ] Draw the engrave layer and cut layer with different strokes.

### Task 4: PLC Power Application

**Files:**
- Modify: `src/DACDT_2026.App/Form1.PlcControl.cs`
- Modify: `src/DACDT_2026.App/QD75BufferWriter.cs` if the write path needs row metadata copied to QD75 row DTOs.

**Interfaces:**
- Consumes: `ProcessRow.LaserPower`

- [ ] Preserve row power while sending QD75 data.
- [ ] Write laser power only when the next row's requested power differs from the last written value.

### Task 5: Verification

**Files:**
- No production changes unless a verification failure exposes a bug.

- [ ] Run the console tests.
- [ ] Run the solution build for Debug x86.
- [ ] Inspect `git diff --check`.
