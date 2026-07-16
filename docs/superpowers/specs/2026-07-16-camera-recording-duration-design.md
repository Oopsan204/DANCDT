# Camera Recording Duration Design

## Goal

Show recording duration instead of frame count in the camera monitor.

## Display

- While recording, show `Recording MP4: HH:MM:SS`.
- When recording stops, show `MP4 saved: HH:MM:SS (SIZE)`.
- `SIZE` is the completed MP4 file length formatted in B, KB, or MB.
- Frame count remains internal to the video writer and is not shown to the operator.

## Behavior

- The duration starts when recording becomes active and updates once per second.
- Duration is wall-clock elapsed time, independent from the camera frame rate.
- Stopping recording closes the writer before reading the MP4 file size.
- The existing video codec, capture path, and WebRTC stream are unchanged.

## Verification

- Unit-test elapsed-time and file-size formatting.
- Assert that the camera view model no longer constructs visible text from frame count.
- Build and run the existing executable test suite plus the x86 Release build.
