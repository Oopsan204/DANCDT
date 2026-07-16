# Camera 24 FPS Design

**Goal:** Raise the camera recording and WebRTC target cadence from the current values to approximately 24 FPS with the smallest possible change.

**Scope:** Change only the camera timing constants and their regression coverage. Keep the existing MP4 encoder, WebRTC VP8 encoder, 640 px web stream cap, 2 Mbps MP4 bitrate, 1 Mbps WebRTC target, camera pipeline, PLC, and DXF behavior unchanged.

**Design:** Set both `CameraRecordingFrameIntervalMs` and `WebRtcFrameIntervalMs` to `42`, which targets approximately 24 frames per second using the existing millisecond `IntervalGate`. Existing single-flight gates continue dropping work when an encoder is busy instead of building latency.

**Expected trade-offs:** CPU use rises because MP4 encoding increases from 10 to approximately 24 FPS and WebRTC input increases from approximately 15 to approximately 24 FPS. MP4 size stays governed by the existing 2 Mbps bitrate, so quality per frame may be slightly lower while duration-based size remains approximately unchanged. Actual FPS remains limited by the camera driver and encoder throughput.

**Verification:** Update the existing timing test to require `42` ms for both paths, run the test executable, rebuild Debug and Release x86, and rebuild the installer.
