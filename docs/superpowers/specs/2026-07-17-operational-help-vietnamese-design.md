# Vietnamese Operational Help Design

## Goal

Rewrite the Help screen as a concise Vietnamese operating guide for the DACDT 2026 system.

## Scope

- Keep only instructions needed by an operator to prepare, run, monitor, stop, and safely shut down the system.
- Cover PLC connection, safety checks, jog/home/reset, DXF/G-code loading, data transfer, run controls, basic camera/log/settings usage, shutdown, troubleshooting, and a pre-run checklist.
- Remove introductory filler, internal PLC memory addresses, MQTT/WebRTC implementation details, build/debug instructions, and other developer-facing information.
- Keep the existing Help view layout, shared styles, scrolling behavior, and XAML resources unchanged.
- Write all Help copy in Vietnamese; keep visible control names such as `RUN`, `PAUSE`, `STOP`, `EXIT APP`, and `EMERGENCY STOP` where they match the UI.

## Design

Replace the current long mixed-purpose document with eight operator-focused sections:

1. An toàn trước khi vận hành.
2. Khởi động và kết nối PLC.
3. Jog tay, HOME và RESET.
4. Mở và kiểm tra file DXF/G-code.
5. Gửi dữ liệu và chạy chương trình.
6. Ý nghĩa các nút RUN / PAUSE / CONTINUE / STOP.
7. Camera, Logs và Settings cơ bản.
8. Thoát app, xử lý lỗi và checklist trước khi chạy.

The content should use short paragraphs and numbered actions. Safety guidance must clearly distinguish normal `STOP` from `EMERGENCY STOP`. The page must not mention removed Telemetry functionality or developer-only setup details.

## Verification

- Add a focused regression check that the Help view contains the Vietnamese operator sections and does not contain removed developer-facing phrases or Telemetry references.
- Run the existing test executable.
- Run `git diff --check` and review that only Help content and its related test/spec files changed.
