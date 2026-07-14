# Antigravity UI Delegation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a guarded workflow that lets Codex delegate UI-only work to Antigravity CLI.

**Architecture:** Keep the integration as documentation plus one PowerShell runner. The runner invokes `agy -p`, passes the UI contract/task, and validates the Git diff after Antigravity edits.

**Tech Stack:** PowerShell, Git, existing .NET Framework source-level test executable.

## Global Constraints

- Do not change PLC, QD75, laser power, motion, camera, MQTT, or WebRTC logic.
- Antigravity may only edit explicitly allowed UI paths.
- Do not use `--dangerously-skip-permissions`.
- Use the existing test executable for verification.

---

### Task 1: Guarded Antigravity UI Workflow

**Files:**
- Modify: `AGENTS.md`
- Modify: `tests/DACDT_2026.Tests/Program.cs`
- Create: `docs/ui-contract.md`
- Create: `docs/ui-task.md`
- Create: `tools/run-antigravity-ui.ps1`

**Interfaces:**
- Consumes: Git diff and local `agy` executable.
- Produces: `tools/run-antigravity-ui.ps1`, callable by Codex for UI-only delegation.

- [x] **Step 1: Write the failing test**

Add `AntigravityUiWorkflowIsGuarded()` to `tests/DACDT_2026.Tests/Program.cs`.

- [x] **Step 2: Run test to verify it fails**

Run the test executable and confirm it fails because `docs/ui-contract.md` is missing.

- [x] **Step 3: Add the minimal workflow files**

Add AGENTS instructions, `docs/ui-contract.md`, `docs/ui-task.md`, and `tools/run-antigravity-ui.ps1`.

- [x] **Step 4: Run test to verify it passes**

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' 'tests\DACDT_2026.Tests\DACDT_2026.Tests.csproj' /p:Configuration=Debug /v:minimal
& 'tests\DACDT_2026.Tests\bin\Debug\DACDT_2026.Tests.exe'
```

- [x] **Step 5: Verify CLI state and diff**

Run `agy --version`, `git diff --check`, and `git diff --name-only`.
