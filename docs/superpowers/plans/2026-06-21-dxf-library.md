# DXF Library Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a standalone `NDA_DXF.dll` library that reads only DXF `LINE`, `ARC`, and `CIRCLE` entities and returns reusable coordinate/motion segment data.

**Architecture:** Create a new `DxfLibrary` folder independent from the current WPF application. The library exposes `DxfReader.Load(path)` returning `DxfLoadResult` with a flat `Segments` list. A small console test project exercises the public API against a sample DXF file.

**Tech Stack:** C# SDK-style projects, `netstandard2.0` library, `net8.0` console test app, no external DXF package.

## Global Constraints

- Do not modify the current `DACDT_2026` application or solution.
- Output assembly name must be `NDA_DXF.dll`.
- Parse only `LINE`, `ARC`, and `CIRCLE`.
- Ignore `LWPOLYLINE`, `POLYLINE`, `SPLINE`, `ELLIPSE`, `TEXT`, and all other entity types.
- Output coordinate/motion geometry only: no MCode, no Dwell, no Speed, no PLC/QD75 logic.

---

### Task 1: Console Test Harness

**Files:**
- Create: `DxfLibrary/NDA_DXF.csproj`
- Create: `DxfLibrary.Tests/DxfLibrary.Tests.csproj`
- Create: `DxfLibrary.Tests/Program.cs`
- Create: `DxfLibrary.Tests/Samples/line_arc_circle.dxf`

**Interfaces:**
- Consumes: future `NDA_DXF.DxfReader.Load(string filePath)` API.
- Produces: executable test app that exits non-zero on geometry mismatch.

- [x] **Step 1: Write failing test**

Create a console app that calls `DxfReader.Load`, asserts 3 segments, and checks line, arc, and circle geometry.

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet run --project DxfLibrary.Tests/DxfLibrary.Tests.csproj`
Expected: compile failure because `DxfReader` does not exist yet.

### Task 2: DXF Reader Library

**Files:**
- Create: `DxfLibrary/DxfPoint.cs`
- Create: `DxfLibrary/DxfBounds.cs`
- Create: `DxfLibrary/DxfSegment.cs`
- Create: `DxfLibrary/DxfLoadResult.cs`
- Create: `DxfLibrary/DxfReader.cs`

**Interfaces:**
- Produces: `DxfReader.Load(string filePath)`, `DxfLoadResult.Segments`, and geometry model classes.

- [x] **Step 1: Implement minimal reader**

Read DXF group-code pairs, walk the `ENTITIES` section, parse `LINE`, `ARC`, and `CIRCLE`, and ignore all other entity types.

- [x] **Step 2: Run test to verify it passes**

Run: `dotnet run --project DxfLibrary.Tests/DxfLibrary.Tests.csproj`
Expected: `All DXF library tests passed.`

### Task 3: Release Build

**Files:**
- Verify: `DxfLibrary/bin/Release/netstandard2.0/NDA_DXF.dll`

- [x] **Step 1: Build release dll**

Run: `dotnet build DxfLibrary/NDA_DXF.csproj -c Release`
Expected: build succeeds and creates `NDA_DXF.dll`.
