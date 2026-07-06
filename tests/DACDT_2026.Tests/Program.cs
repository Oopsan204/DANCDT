using System;
using DACDT_2026;

namespace DACDT_2026.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                CleansMastercamNcAndNormalizesLaserCommands();
                SplitsMastercamModalCodesFromMotionLine();
                GcodeLineSanitizerAcceptsTrailingDecimalPoint();
                PreservesLeadingDecimalArcOffsets();
                DropsZOnlyMovesFromMastercamNc();
                MovesLaserOnFromRapidToFirstCutMove();
                ConvertsCutterCompLeadInToRapidPositioning();
                ConvertsCutterCompLeadOutToRapidPositioning();
                PreservesSupportedArcAndMotionCommands();
                CameraSelectionUsesFriendlyNameAndDetectsSwitch();
                CameraReconnectDelayIsOneSecond();
                IntervalGateThrottlesRepeatedWork();
                SingleFlightGateAllowsOnlyOneInFlightOperation();
                CameraRecordingFrameIntervalIsThrottled();
                AxisMonitorUpdateCadenceStaysResponsive();
                ExitShutdownSendsM210WheneverPlcIsConnected();
                PlcConnectionGuardBlocksMissingOrDisconnectedPlc();
                D406JogSpeedUsesFloatWordEncoding();
                DecimalJogSpeedInputAcceptsDotAndComma();
                WebCadUploadReassemblesChunks();
                WebCadUploadRejectsUnsupportedFiles();
                Console.WriteLine("All tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static void CleansMastercamNcAndNormalizesLaserCommands()
        {
            string input = string.Join(Environment.NewLine, new[]
            {
                "%",
                "O0001 (MASTER CAM FILE)",
                "N10 G17 G21 G90 G54",
                "N20 T1 M6",
                "N30 G43 H1 Z15.",
                "N40 M8",
                "N50 G0 X10. Y20.",
                "N60 M3 S12000",
                "N70 G1 X30. Y20. F800.",
                "N80 M5",
                "N90 M9",
                "N100 G0 X0. Y0.",
                "N110 M30",
                "%"
            });

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            string expected = string.Join(Environment.NewLine, new[]
            {
                "G21 G90 G54",
                "G0 X10. Y20.",
                "M3",
                "G1 X30. Y20. F800.",
                "M4",
                "G0 X0. Y0.",
                "M30"
            });

            AssertEqual(expected, result.Text.Trim(), "Mastercam NC should be filtered to safe laser G-code.");
            AssertTrue(result.RemovedLineCount >= 5, "Cleaner should report removed unsupported/header lines.");
            AssertTrue(result.Warnings.Count > 0, "Cleaner should report warnings for removed or normalized lines.");
        }

        private static void PreservesSupportedArcAndMotionCommands()
        {
            string input = string.Join(Environment.NewLine, new[]
            {
                "G21",
                "G90",
                "G0 X0 Y0",
                "M3",
                "G2 X10 Y0 I5 J0 F600",
                "G3 X0 Y0 R5",
                "M4"
            });

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            AssertEqual(input, result.Text.Trim(), "Cleaner should preserve supported G0/G1/G2/G3 commands.");
            AssertEqual("0", result.RemovedLineCount.ToString(), "Supported file should not remove lines.");
        }

        private static void SplitsMastercamModalCodesFromMotionLine()
        {
            string input = "N124 G00 G90 G17 G54 X90.5 Y13.9 S4000 M03";

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            string expected = string.Join(Environment.NewLine, new[]
            {
                "G90",
                "G54",
                "G0 X90.5 Y13.9"
            });

            AssertEqual(expected, result.Text.Trim(), "Mastercam modal setup G-codes should be split away from the motion line and M3 should not stay on a rapid G0 move.");
        }

        private static void GcodeLineSanitizerAcceptsTrailingDecimalPoint()
        {
            string normalized = GcodeLineSanitizer.NormalizeForParser("N150 G02 X4.9 Y20. I0. J5.1");

            AssertEqual("N150 G02 X4.9 Y20. I0. J5.1", normalized, "NC numbers ending with a decimal point, such as Y20., are valid and must not drop the whole line.");
            AssertEqual(string.Empty, GcodeLineSanitizer.NormalizeForParser("G01 X-"), "Clearly incomplete numeric input should still be rejected.");
        }

        private static void PreservesLeadingDecimalArcOffsets()
        {
            string input = "N134 G03 X90. Y14.9 I-.5 J0.";

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            AssertEqual("G3 X90. Y14.9 I-.5 J0.", result.Text.Trim(), "NC arc offsets like I-.5 are valid and must be preserved.");
        }

        private static void DropsZOnlyMovesFromMastercamNc()
        {
            string input = string.Join(Environment.NewLine, new[]
            {
                "N126 G43 H10 Z25. M08",
                "N128 Z2.2",
                "N130 G01 Z-5. F2000.",
                "N132 G41 D10 Y14.4 F1500.",
                "G91 Z0.",
                "N188 G91 G28 Z0.",
                "N190 G91 G28 X0. Y0."
            });

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            string expected = string.Join(Environment.NewLine, new[]
            {
                "G1",
                "G0 Y14.4"
            });

            AssertEqual(expected, result.Text.Trim(), "Z-only CNC setup/plunge/retract moves should be removed for 2D laser cutting.");
        }

        private static void MovesLaserOnFromRapidToFirstCutMove()
        {
            string input = string.Join(Environment.NewLine, new[]
            {
                "N124 G00 G90 G17 G54 X90.5 Y13.9 S4000 M03",
                "N130 G01 Z-5. F2000.",
                "N134 G03 X90. Y14.9 I-.5 J0."
            });

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            string expected = string.Join(Environment.NewLine, new[]
            {
                "G90",
                "G54",
                "G0 X90.5 Y13.9",
                "G1",
                "G3 X90. Y14.9 I-.5 J0. M3"
            });

            AssertEqual(expected, result.Text.Trim(), "M3 on a rapid G0 approach should move to the first real cut move to avoid an unwanted lead-in burn line.");
        }

        private static void ConvertsCutterCompLeadInToRapidPositioning()
        {
            string input = string.Join(Environment.NewLine, new[]
            {
                "N124 G00 G90 G17 G54 X90.5 Y13.9 S4000 M03",
                "N130 G01 Z-5. F2000.",
                "N132 G41 D10 Y14.4 F1500.",
                "N134 G03 X90. Y14.9 I-.5 J0.",
                "N136 G01 X84.213"
            });

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            string expected = string.Join(Environment.NewLine, new[]
            {
                "G90",
                "G54",
                "G0 X90.5 Y13.9",
                "G1",
                "G0 Y14.4",
                "G0 X90. Y14.9",
                "G1 X84.213 M3"
            });

            AssertEqual(expected, result.Text.Trim(), "Cutter-comp lead-in moves should become rapid positioning so they do not create a burned/visible entry line.");
        }

        private static void ConvertsCutterCompLeadOutToRapidPositioning()
        {
            string input = string.Join(Environment.NewLine, new[]
            {
                "N170 G02 X95.1 Y40. I0. J-5.1",
                "N172 G01 Y20.",
                "N174 G02 X90. Y14.9 I-5.1 J0.",
                "N176 G01 X89.5",
                "N178 G03 X89. Y14.4 I0. J-.5",
                "N180 G01 G40 Y13.9",
                "N186 M05"
            });

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            string expected = string.Join(Environment.NewLine, new[]
            {
                "G2 X95.1 Y40. I0. J-5.1",
                "G1 Y20.",
                "G2 X90. Y14.9 I-5.1 J0. M4",
                "G0 X89.5",
                "G0 X89. Y14.4",
                "G0 Y13.9",
                "M4"
            });

            AssertEqual(expected, result.Text.Trim(), "Cutter-comp lead-out moves should become rapid positioning and laser should turn off before exiting the contour.");
        }

        private static void CameraSelectionUsesFriendlyNameAndDetectsSwitch()
        {
            var cameras = new[]
            {
                new CameraDeviceSelection.CameraDevice("Integrated Camera", "@device:pnp:camera0"),
                new CameraDeviceSelection.CameraDevice("USB Camera", "@device:pnp:usb#vid_1234")
            };

            CameraDeviceSelection.CameraDevice selected = CameraDeviceSelection.FindByMonikerOrPreferred(cameras, "@device:pnp:camera0");

            AssertEqual("Integrated Camera", selected.DisplayName, "Camera UI should display the friendly camera name.");
            AssertTrue(CameraDeviceSelection.ShouldSwitch("@device:pnp:camera0", "@device:pnp:usb#vid_1234"), "Selecting a different camera while running should trigger a switch.");
            AssertTrue(!CameraDeviceSelection.ShouldSwitch("@device:pnp:camera0", "@device:pnp:camera0"), "Selecting the same camera should not trigger a switch.");
        }

        private static void CameraReconnectDelayIsOneSecond()
        {
            AssertEqual("1000", CameraDeviceSelection.ReconnectDelayMs.ToString(), "Camera reconnect delay should be 1 second.");
        }

        private static void IntervalGateThrottlesRepeatedWork()
        {
            var gate = new IntervalGate(100);
            var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            AssertTrue(gate.TryEnter(t0), "First interval gate entry should pass.");
            AssertTrue(!gate.TryEnter(t0.AddMilliseconds(50)), "Second entry inside interval should be blocked.");
            AssertTrue(gate.TryEnter(t0.AddMilliseconds(100)), "Entry at interval boundary should pass.");
        }

        private static void SingleFlightGateAllowsOnlyOneInFlightOperation()
        {
            var gate = new SingleFlightGate();

            AssertTrue(gate.TryEnter(), "First single-flight entry should pass.");
            AssertTrue(!gate.TryEnter(), "Second single-flight entry should be blocked while busy.");
            gate.Exit();
            AssertTrue(gate.TryEnter(), "Single-flight entry should pass again after exit.");
            gate.Exit();
        }

        private static void CameraRecordingFrameIntervalIsThrottled()
        {
            AssertEqual("100", PerformanceTuning.CameraRecordingFrameIntervalMs.ToString(), "Camera recording should be throttled to 10 fps.");
        }

        private static void AxisMonitorUpdateCadenceStaysResponsive()
        {
            AssertEqual("10", PerformanceTuning.PlcPollIntervalMs.ToString(), "PLC axis poll should be the fastest path in the application.");
            AssertEqual("1", PerformanceTuning.PlcPollMinimumDelayMs.ToString(), "PLC polling should not add a large artificial delay when the PLC call is already fast.");
            AssertEqual("16", PerformanceTuning.ControlUiPushIntervalMs.ToString(), "Axis monitor UI should target smooth local display cadence without waiting for MQTT.");
            AssertEqual("16", PerformanceTuning.ControlTrackingUiPushIntervalMs.ToString(), "CAD tracking marker should update at the smooth local display cadence.");
            AssertEqual("1000", PerformanceTuning.SlowPlcMonitorPollIntervalMs.ToString(), "Non-axis PLC monitor rows should not block the fast axis path.");
            AssertEqual("1000", PerformanceTuning.MachineMqttPublishIntervalMs.ToString(), "MQTT/web publish must stay secondary to the local PLC monitor path.");
            AssertTrue(PerformanceTuning.MachineMqttPublishIntervalMs >= PerformanceTuning.PlcPollIntervalMs * 100, "MQTT cadence should be at least 100x slower than PLC polling.");
        }

        private static void ExitShutdownSendsM210WheneverPlcIsConnected()
        {
            AssertTrue(ExitShutdownPolicy.ShouldSendExitStop(plcConnected: true), "Exit should send M210 whenever PLC is connected, even if the robot is not running.");
            AssertTrue(!ExitShutdownPolicy.ShouldSendExitStop(plcConnected: false), "Exit cannot send M210 when PLC is disconnected.");
            AssertEqual("150", PerformanceTuning.ExitStopPulseMs.ToString(), "Exit should pulse M210 briefly instead of holding it ON.");
            AssertEqual("500", PerformanceTuning.ExitStopDelayMs.ToString(), "Exit should wait 500 ms after pulsing M210 before HOME ALL.");
            AssertEqual("150", PerformanceTuning.ExitHomePulseMs.ToString(), "Exit should pulse HOME ALL briefly.");
            AssertEqual("500", PerformanceTuning.ExitHomeDelayMs.ToString(), "Exit should wait 500 ms after HOME ALL before clearing buffers and closing.");
        }

        private static void PlcConnectionGuardBlocksMissingOrDisconnectedPlc()
        {
            AssertTrue(!PlcConnectionGuard.CanUsePlc(communicationObjectExists: false, isConnected: false), "PLC operations must be blocked when no PLC communication object exists.");
            AssertTrue(!PlcConnectionGuard.CanUsePlc(communicationObjectExists: true, isConnected: false), "PLC operations must be blocked when the PLC communication object is disconnected.");
            AssertTrue(PlcConnectionGuard.CanUsePlc(communicationObjectExists: true, isConnected: true), "PLC operations are allowed only after a live PLC connection exists.");
            AssertEqual("PLC is not connected.", PlcConnectionGuard.NotConnectedMessage, "Disconnected PLC operations should use one consistent message.");
        }

        private static void D406JogSpeedUsesFloatWordEncoding()
        {
            float expected = 12.345f;
            int bits = PlcFloatWordCodec.ToInt32Bits(expected);
            int lowWord = bits & 0xFFFF;
            int highWord = (bits >> 16) & 0xFFFF;
            float actual = PlcFloatWordCodec.FromWords(lowWord, highWord);

            AssertTrue(Math.Abs(expected - actual) < 0.0001f, "D406 jog speed should round-trip as a 32-bit float across two PLC words.");
        }

        private static void DecimalJogSpeedInputAcceptsDotAndComma()
        {
            AssertTrue(DecimalInputParser.TryParseFlexibleDouble("0.5", out double dot), "Jog speed input should accept decimal point.");
            AssertTrue(Math.Abs(dot - 0.5) < 0.000001, "Decimal point input should keep fractional value.");

            AssertTrue(DecimalInputParser.TryParseFlexibleDouble("12,345", out double comma), "Jog speed input should accept decimal comma.");
            AssertTrue(Math.Abs(comma - 12.345) < 0.000001, "Decimal comma input should keep fractional value.");

            AssertEqual("12.5", DecimalInputParser.FormatFloat(12.5f), "PLC jog speed should format back to editable decimal text.");
        }

        private static void WebCadUploadReassemblesChunks()
        {
            var upload = new WebCadUploadSession();
            upload.Begin("job-1", "part.nc", totalChunks: 2, totalBytes: 10);

            bool complete1 = upload.AddChunk("job-1", 1, Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Y10")));
            bool complete2 = upload.AddChunk("job-1", 0, Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("G1 X10 ")));

            AssertTrue(!complete1, "Upload should not complete before all chunks arrive.");
            AssertTrue(complete2, "Upload should complete when the last missing chunk arrives.");
            AssertEqual("G1 X10 Y10", System.Text.Encoding.UTF8.GetString(upload.Assemble()), "Upload chunks should reassemble in index order.");
        }

        private static void WebCadUploadRejectsUnsupportedFiles()
        {
            AssertTrue(WebCadUploadSession.IsAllowedFileName("shape.dxf"), "DXF upload should be accepted.");
            AssertTrue(WebCadUploadSession.IsAllowedFileName("laser.nc"), "NC/G-code upload should be accepted.");
            AssertTrue(!WebCadUploadSession.IsAllowedFileName("notes.pdf"), "Non-CAD upload should be rejected.");
        }

        private static void AssertEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new Exception(message + Environment.NewLine
                    + "Expected:" + Environment.NewLine + expected + Environment.NewLine
                    + "Actual:" + Environment.NewLine + actual);
            }
        }

        private static void AssertTrue(bool value, string message)
        {
            if (!value)
            {
                throw new Exception(message);
            }
        }
    }
}
