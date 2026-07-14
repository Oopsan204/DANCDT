# UI Contract

Codex owns architecture, data flow, state, validation, PLC communication, QD75 buffers, camera, MQTT, WebRTC, tests, and final review.

Antigravity owns visual layout and styling only.

## Allowed UI paths

- `src/DACDT_2026.App/Views/**`
- `src/DACDT_2026.App/Assets/**`
- `src/DACDT_2026.App/app_icon.*`
- `assets/design/**`

## Forbidden logic paths

- `src/DACDT_2026.App/Form1.cs`
- `src/DACDT_2026.App/Form1.PlcControl.cs`
- `src/DACDT_2026.App/Form1.DxfHandler.cs`
- `src/DACDT_2026.App/Form1.Camera.cs`
- `src/DACDT_2026.App/Form1.StatePublisher.cs`
- `src/DACDT_2026.App/Form1.WebCadUpload.cs`
- `src/DACDT_2026.App/PLCCommunication.cs`
- `src/DACDT_2026.App/QD75BufferWriter.cs`
- `src/DACDT_2026.App/QD75RingBufferRunner.cs`
- `src/DACDT_2026.App/EngraveCutProcessComposer.cs`
- `src/DACDT_2026.App/CadDocumentService.cs`
- `src/DACDT_2026.App/CadPathSelection.cs`
- `src/DACDT_2026.App/WebRtcCameraServer.cs`
- `src/DACDT_2026.App/WebRtcBridgeClient.cs`
- `tests/**`

## Rules for Antigravity

- Do not add business logic.
- Do not change PLC addresses, motion commands, laser power writes, timing, QD75 rows, or camera lifecycle behavior.
- Use existing commands, bindings, properties, converters, and view models.
- If a needed binding or API does not exist, stop and report it in the final response instead of creating logic.
- Keep operator UI text in English, except Help content when the user asks for Vietnamese guidance.
- Keep the app work-focused and dense; avoid marketing-style layout.

