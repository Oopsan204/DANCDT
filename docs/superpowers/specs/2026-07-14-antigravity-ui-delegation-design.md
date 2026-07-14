# Antigravity UI Delegation Design

## Goal

Let Codex coordinate UI work through Antigravity CLI while keeping machine logic, PLC writes, QD75 motion, camera, WebRTC, state, validation, and tests under Codex control.

## Approach

Use a small repository workflow instead of a custom integration. Codex writes the UI contract and task files, then calls `tools/run-antigravity-ui.ps1`. The script checks that `agy` exists, passes a non-interactive `agy -p` prompt, and rejects any resulting diff outside approved UI paths.

## Boundaries

Antigravity may edit WPF views, view-local styling, UI assets, app icons, and design references. It must not edit `Form1.cs`, PLC control files, QD75 files, DXF/G-code composition logic, camera/WebRTC code, or tests. Missing bindings or commands must be reported back to Codex instead of being invented in UI code.

## Error Handling

If `agy` is not installed or not in PATH, the runner stops with a clear message. If Antigravity changes a forbidden file or any file outside the UI allow-list, the runner fails so Codex can reject the diff.

## Testing

A source-level test verifies the contract, task file, AGENTS instructions, and runner guardrails. Build and test verification remain under Codex.

