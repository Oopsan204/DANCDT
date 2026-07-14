using System;
using System.Collections.Generic;
using System.IO;
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
                BackgroundVideoServiceArgumentsIncludeParentPid();
                ExitShutdownSendsM210WheneverPlcIsConnected();
                PlcConnectionGuardBlocksMissingOrDisconnectedPlc();
                D406JogSpeedUsesFloatWordEncoding();
                DecimalJogSpeedInputAcceptsDotAndComma();
                ZHeightConversionUsesTenThousandScale();
                ZHeightCommandUsesD110ThenPulsesM212();
                WebCadUploadReassemblesChunks();
                WebCadUploadRejectsUnsupportedFiles();
                WpfThemeManagerAppliesLightAndDarkPalettes();
                EngraveCutComposerKeepsOneOrderedProcessListWithPerRowParameters();
                LaserPowerPercentMapsToPlcRange();
                ActiveEngraveCutRowsSelectDifferentLaserPowerValues();
                EngraveCutPowerSwitchUsesFirstCutRow();
                IntermediateEngraveEndContinuesBeforeCutRows();
                EngraveHomeRowIsDroppedWhenCutFollows();
                MixedProgramUsesM03SpeedForNonCutRowsAndProcessSpeedForWorkRows();
                CadPathSelectionGroupsConnectedLineSegments();
                CadPathSelectionTogglesEveryPrimitiveInSelectedPath();
                CadPathSelectionToggleTwiceRestoresEngrave();
                SettingsViewUsesApprovedEnglishContract();
                NonHelpViewsDoNotUseKnownVietnameseOperatorLabels();
                SettingsViewExposesSaveSettingsCommand();
                AntigravityUiWorkflowIsGuarded();
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

        private static void BackgroundVideoServiceArgumentsIncludeParentPid()
        {
            string args = BackgroundVideoServiceProcess.BuildParentPidArguments(12345);

            AssertEqual("--parent-pid 12345", args, "WebRTC service should receive the owning app process id.");
            AssertEqual("12345", BackgroundVideoServiceProcess.TryGetParentPid(new[] { "--parent-pid", "12345" }).ToString(), "WebRTC service should parse parent pid arguments.");
            AssertEqual("0", BackgroundVideoServiceProcess.TryGetParentPid(new[] { "--parent-pid", "abc" }).ToString(), "Invalid parent pid should be ignored.");
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

        private static void ZHeightConversionUsesTenThousandScale()
        {
            AssertTrue(ZHeightSetting.TryConvertToPlcUnits("1.25", out int dotValue), "Z height should accept decimal point input.");
            AssertEqual("12500", dotValue.ToString(), "Z height millimetres should convert to 0.1 micrometre PLC units.");

            AssertTrue(ZHeightSetting.TryConvertToPlcUnits("1,25", out int commaValue), "Z height should accept decimal comma input.");
            AssertEqual("12500", commaValue.ToString(), "Z height comma decimal should use the same PLC conversion.");

            AssertTrue(!ZHeightSetting.TryConvertToPlcUnits("abc", out _), "Invalid Z height input should be rejected.");
            AssertTrue(!ZHeightSetting.TryConvertToPlcUnits("-0.1", out _), "Negative Z height input should be rejected.");
        }

        private static void ZHeightCommandUsesD110ThenPulsesM212()
        {
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string plcSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.PlcControl.cs"));
            string stateSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "WpfUiState.cs"));

            AssertTrue(stateSource.Contains("ZHeightInput"), "UI state must expose the Z height input.");
            AssertTrue(stateSource.Contains("SetZHeightCommand"), "UI state must expose the Z height command.");
            int processRowViewModelIndex = stateSource.IndexOf("public sealed class ProcessRowViewModel", StringComparison.Ordinal);
            int zHeightInputIndex = stateSource.IndexOf("public string ZHeightInput", StringComparison.Ordinal);
            AssertTrue(zHeightInputIndex >= 0 && zHeightInputIndex < processRowViewModelIndex, "Z height input must belong to WpfUiState, not a process-row model.");
            AssertTrue(formSource.Contains("SetZHeightCommand"), "Form command setup must bind the Z height command.");
            AssertTrue(plcSource.Contains("WriteDeviceValueAsync(\"D110\", plcValue)"), "Z height must write the converted value to D110.");
            int d110Index = plcSource.IndexOf("WriteDeviceValueAsync(\"D110\", plcValue)", StringComparison.Ordinal);
            int m212OnIndex = plcSource.IndexOf("WriteDeviceValueAsync(StopRunRegister, 1)", d110Index, StringComparison.Ordinal);
            int m212OffIndex = plcSource.IndexOf("WriteDeviceValueAsync(StopRunRegister, 0)", m212OnIndex, StringComparison.Ordinal);
            AssertTrue(d110Index >= 0 && m212OnIndex > d110Index && m212OffIndex > m212OnIndex, "Z height command must write D110 before pulsing M212 on then off.");
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

        private static void WpfThemeManagerAppliesLightAndDarkPalettes()
        {
            var resources = new System.Windows.ResourceDictionary
            {
                ["BgBrush"] = new System.Windows.Media.SolidColorBrush(),
                ["TextBrush"] = new System.Windows.Media.SolidColorBrush(),
                ["PanelBrush"] = new System.Windows.Media.SolidColorBrush(),
                ["CardHeaderBrush"] = new System.Windows.Media.SolidColorBrush(),
                ["CardHeaderTextBrush"] = new System.Windows.Media.SolidColorBrush()
            };

            AssertEqual("light", WpfThemeManager.Apply("light", resources), "Theme manager should accept light theme.");
            AssertEqual("#FFE9EEF5", ((System.Windows.Media.SolidColorBrush)resources["BgBrush"]).Color.ToString(), "Light theme should apply a soft slate background.");
            AssertEqual("#FF102033", ((System.Windows.Media.SolidColorBrush)resources["TextBrush"]).Color.ToString(), "Light theme should keep readable dark text.");
            AssertEqual("#FFDCEAFE", ((System.Windows.Media.SolidColorBrush)resources["CardHeaderBrush"]).Color.ToString(), "Light axis card header should use a calm blue header.");
            AssertEqual("#FF0C2540", ((System.Windows.Media.SolidColorBrush)resources["CardHeaderTextBrush"]).Color.ToString(), "Light axis card header text should be readable.");

            AssertEqual("dark", WpfThemeManager.Apply("bad-value", resources), "Unknown theme should fall back to dark.");
            AssertEqual("#FF0B1120", ((System.Windows.Media.SolidColorBrush)resources["BgBrush"]).Color.ToString(), "Dark theme should restore the dark app background.");
            AssertEqual("#FF18233A", ((System.Windows.Media.SolidColorBrush)resources["CardHeaderBrush"]).Color.ToString(), "Dark axis card header should keep the dark dashboard style.");
            AssertEqual("#FFE5E7EB", ((System.Windows.Media.SolidColorBrush)resources["CardHeaderTextBrush"]).Color.ToString(), "Dark axis card header text should stay light.");
            AssertEqual("light", WpfThemeManager.Toggle("dark"), "Dark should toggle to light.");
            AssertEqual("dark", WpfThemeManager.Toggle("light"), "Light should toggle to dark.");
        }

        private static void EngraveCutComposerKeepsOneOrderedProcessListWithPerRowParameters()
        {
            var engraveRows = new[]
            {
                new EngraveCutProcessComposer.ProcessRowData { Key = "e1", Speed = "", LaserPower = "" },
                new EngraveCutProcessComposer.ProcessRowData { Key = "e2", Speed = "400", LaserPower = "1" }
            };
            var cutRows = new[]
            {
                new EngraveCutProcessComposer.ProcessRowData { Key = "c1", Speed = "", LaserPower = "" }
            };

            var result = EngraveCutProcessComposer.Compose(
                engraveRows,
                cutRows,
                engraveSpeed: "1200",
                engravePower: "35",
                cutSpeed: "500",
                cutPower: "80");

            AssertEqual("3", result.Count.ToString(), "Engrave and cut rows should be merged into one process list.");
            AssertEqual("engrave", result[0].ProcessKind, "Engrave rows should come first.");
            AssertEqual("engrave", result[1].ProcessKind, "All engrave rows should keep engrave kind.");
            AssertEqual("cut", result[2].ProcessKind, "Cut rows should come after engrave rows.");
            AssertEqual("1200", result[0].Speed, "Blank engrave speed should use engrave config.");
            AssertEqual("400", result[1].Speed, "Existing row speed should not be overwritten.");
            AssertEqual("35", result[0].LaserPower, "Blank engrave power should use engrave config.");
            AssertEqual("1", result[1].LaserPower, "Existing row power should not be overwritten.");
            AssertEqual("500", result[2].Speed, "Cut rows should use cut speed.");
            AssertEqual("80", result[2].LaserPower, "Cut rows should use cut power.");
        }

        private static void LaserPowerPercentMapsToPlcRange()
        {
            AssertEqual("450", EngraveCutProcessComposer.MapLaserPowerPercentToPlcValue(-10).ToString(), "Power below 0% should clamp to the PLC minimum.");
            AssertEqual("450", EngraveCutProcessComposer.MapLaserPowerPercentToPlcValue(0).ToString(), "0% should map to the PLC minimum.");
            AssertEqual("1225", EngraveCutProcessComposer.MapLaserPowerPercentToPlcValue(50).ToString(), "50% should map to the midpoint.");
            AssertEqual("2000", EngraveCutProcessComposer.MapLaserPowerPercentToPlcValue(100).ToString(), "100% should map to the PLC maximum.");
            AssertEqual("2000", EngraveCutProcessComposer.MapLaserPowerPercentToPlcValue(120).ToString(), "Power above 100% should clamp to the PLC maximum.");
        }

        private static void ActiveEngraveCutRowsSelectDifferentLaserPowerValues()
        {
            var rows = new[]
            {
                new EngraveCutProcessComposer.ProcessRowData { ProcessKind = "engrave", LaserPower = "35" },
                new EngraveCutProcessComposer.ProcessRowData { ProcessKind = "cut", LaserPower = "80" }
            };

            AssertTrue(EngraveCutProcessComposer.TryGetLaserPowerPlcValue(rows, 1, out int engravePower), "Engrave row should produce a PLC laser power value.");
            AssertTrue(EngraveCutProcessComposer.TryGetLaserPowerPlcValue(rows, 2, out int cutPower), "Cut row should produce a PLC laser power value.");
            AssertTrue(engravePower != cutPower, "Engrave and cut rows must not write the same laser power when configured differently.");
            AssertEqual(EngraveCutProcessComposer.MapLaserPowerPercentToPlcValue(35).ToString(), engravePower.ToString(), "Engrave row should use engrave power.");
            AssertEqual(EngraveCutProcessComposer.MapLaserPowerPercentToPlcValue(80).ToString(), cutPower.ToString(), "Cut row should use cut power.");
        }

        private static void EngraveCutPowerSwitchUsesFirstCutRow()
        {
            var kinds = new[]
            {
                EngraveCutProcessComposer.EngraveKind,
                EngraveCutProcessComposer.EngraveKind,
                EngraveCutProcessComposer.CutKind,
                EngraveCutProcessComposer.CutKind
            };

            AssertTrue(EngraveCutProcessComposer.TryGetFirstCutRowIndex(kinds, out int firstCutIndex), "Mixed rows should expose the first cut row.");
            AssertEqual("3", firstCutIndex.ToString(), "First cut row should be one-based.");
            AssertEqual("3", EngraveCutProcessComposer.GetCutPowerSwitchMonitorIndex(firstCutIndex).ToString(), "Cut power should switch only when the first cut row is reached.");
        }

        private static void IntermediateEngraveEndContinuesBeforeCutRows()
        {
            AssertEqual(
                "Line (Continuous Positioning)",
                EngraveCutProcessComposer.NormalizeMixedProgramMotionType("Line (End)", isLastRow: false),
                "The engrave-to-cut transition must not stop the QD75 program.");
            AssertEqual(
                "Line (End)",
                EngraveCutProcessComposer.NormalizeMixedProgramMotionType("Line (End)", isLastRow: true),
                "Only the final row of the full mixed program should remain End.");
        }

        private static void EngraveHomeRowIsDroppedWhenCutFollows()
        {
            AssertTrue(
                EngraveCutProcessComposer.ShouldDropHomeBeforeFollowingCut(
                    EngraveCutProcessComposer.EngraveKind,
                    "0",
                    "0;0",
                    true),
                "The intermediate engrave home row must be removed when cut rows follow.");
            AssertTrue(
                !EngraveCutProcessComposer.ShouldDropHomeBeforeFollowingCut(
                    EngraveCutProcessComposer.EngraveKind,
                    "0",
                    "0;0",
                    false),
                "A standalone engrave program should keep its final home row.");
            AssertTrue(
                !EngraveCutProcessComposer.ShouldDropHomeBeforeFollowingCut(
                    EngraveCutProcessComposer.CutKind,
                    "0",
                    "0;0",
                    true),
                "The final cut home row should remain.");
        }

        private static void MixedProgramUsesM03SpeedForNonCutRowsAndProcessSpeedForWorkRows()
        {
            AssertEqual(
                "5000",
                EngraveCutProcessComposer.ResolveMixedRowSpeed("3", "10;10", "1200", "5000"),
                "M03 path-start rows should use the non-cut M03 speed.");
            AssertEqual(
                "1200",
                EngraveCutProcessComposer.ResolveMixedRowSpeed("", "20;20", "1200", "5000"),
                "Engrave/cut work rows should use their process speed.");
            AssertEqual(
                "1200",
                EngraveCutProcessComposer.ResolveMixedRowSpeed("4", "30;30", "1200", "5000"),
                "M04 rows are still work rows and should not fall back to DXF M04 speed in mixed mode.");
            AssertEqual(
                "5000",
                EngraveCutProcessComposer.ResolveMixedRowSpeed("0", "0;0", "1200", "5000"),
                "Final home rows should use the non-cut M03 speed.");
        }

        private static void CadPathSelectionGroupsConnectedLineSegments()
        {
            var first = NewCadLine(0, 0, 10, 0);
            var second = NewCadLine(10, 0, 10, 10);
            var separate = NewCadLine(100, 100, 110, 100);

            var paths = CadPathSelection.GroupConnectedPaths(
                new List<CadDocumentService.CadPrimitiveData> { first, second, separate });

            AssertEqual("2", paths.Count.ToString(), "Connected segments should form one path and disconnected geometry another.");
            AssertEqual("2", CadPathSelection.AssignPathIds(paths).ToString(), "Two groups should receive two path ids.");
            AssertEqual("0", first.PathId.ToString(), "The first chain should receive path id zero.");
            AssertEqual("0", second.PathId.ToString(), "Connected segments should share a path id.");
            AssertEqual("1", separate.PathId.ToString(), "Disconnected geometry should receive a different path id.");
        }

        private static void CadPathSelectionTogglesEveryPrimitiveInSelectedPath()
        {
            var first = NewCadLine(0, 0, 10, 0);
            var second = NewCadLine(10, 0, 10, 10);
            var separate = NewCadLine(100, 100, 110, 100);
            first.PathId = 4;
            second.PathId = 4;
            separate.PathId = 5;

            bool changed = CadPathSelection.ToggleProcessKind(
                new[] { first, second, separate },
                4,
                EngraveCutProcessComposer.EngraveKind,
                EngraveCutProcessComposer.CutKind);

            AssertTrue(changed, "A valid path id should toggle.");
            AssertEqual(EngraveCutProcessComposer.CutKind, first.ProcessKind, "The first segment should become Cut.");
            AssertEqual(EngraveCutProcessComposer.CutKind, second.ProcessKind, "Every selected segment should become Cut.");
            AssertEqual(EngraveCutProcessComposer.EngraveKind, separate.ProcessKind, "An unrelated contour must not change.");
        }

        private static void CadPathSelectionToggleTwiceRestoresEngrave()
        {
            var first = NewCadLine(0, 0, 10, 0);
            first.PathId = 7;
            first.ProcessKind = EngraveCutProcessComposer.EngraveKind;

            CadPathSelection.ToggleProcessKind(
                new[] { first }, 7,
                EngraveCutProcessComposer.EngraveKind,
                EngraveCutProcessComposer.CutKind);
            CadPathSelection.ToggleProcessKind(
                new[] { first }, 7,
                EngraveCutProcessComposer.EngraveKind,
                EngraveCutProcessComposer.CutKind);

            AssertEqual(EngraveCutProcessComposer.EngraveKind, first.ProcessKind, "Two toggles should restore Engrave.");
        }

        private static void SettingsViewUsesApprovedEnglishContract()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "SettingsView.xaml"));
            string[] requiredLabels =
            {
                "DXF Processing",
                "Travel Speed (M03 / Home) (mm/min)",
                "Laser On Delay (M03) (ms)",
                "Laser Off Delay (M04) (ms)",
                "G-code Motion",
                "Laser-On Speed (M03) (mm/min)",
                "Rapid Travel Speed (G00) (mm/min)",
                "Workspace Width (mm)",
                "Workspace Height (mm)"
            };

            foreach (string label in requiredLabels)
            {
                AssertTrue(source.Contains(label), "Settings must contain the approved label: " + label);
            }

            string[] obsoleteLabels =
            {
                "Single DXF Speed M04",
                "Apply Speed",
                "Machine Config &amp; Workspace"
            };

            foreach (string label in obsoleteLabels)
            {
                AssertTrue(!source.Contains(label), "Settings must remove the obsolete label: " + label);
            }
        }

        private static void NonHelpViewsDoNotUseKnownVietnameseOperatorLabels()
        {
            string appDirectory = GetRepositoryPath("src", "DACDT_2026.App");
            string[] prohibitedLabels = { "Text=\"Khac\"", "Text=\"Cat\"" };

            foreach (string file in Directory.GetFiles(appDirectory, "*.xaml", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(file), "HelpView.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string source = File.ReadAllText(file);
                foreach (string label in prohibitedLabels)
                {
                    AssertTrue(!source.Contains(label), Path.GetFileName(file) + " must not contain operator label " + label);
                }
            }
        }

        private static void SettingsViewExposesSaveSettingsCommand()
        {
            string settingsView = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "SettingsView.xaml"));
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));

            AssertTrue(settingsView.Contains("Save Settings"), "Settings must expose a Save Settings action.");
            AssertTrue(settingsView.Contains("{Binding SaveSettingsCommand}"), "Save Settings must bind to SaveSettingsCommand.");
            AssertTrue(formSource.Contains("app_settings.txt"), "Save Settings must keep using the existing app_settings.txt format.");
            AssertTrue(!settingsView.Contains("Import Settings"), "Settings must not add a separate import workflow.");
            AssertTrue(!settingsView.Contains("Export Settings"), "Settings must not add a separate export workflow.");
        }

        private static void AntigravityUiWorkflowIsGuarded()
        {
            string agents = File.ReadAllText(GetRepositoryPath("AGENTS.md"));
            string contract = File.ReadAllText(GetRepositoryPath("docs", "ui-contract.md"));
            string task = File.ReadAllText(GetRepositoryPath("docs", "ui-task.md"));
            string runner = File.ReadAllText(GetRepositoryPath("tools", "run-antigravity-ui.ps1"));

            AssertTrue(agents.Contains("Antigravity CLI is the UI implementation agent."), "AGENTS.md must define Antigravity as the UI agent.");
            AssertTrue(agents.Contains("Never allow Antigravity to redesign API contracts without Codex review."), "AGENTS.md must keep API contracts under Codex review.");
            AssertTrue(contract.Contains("Allowed UI paths"), "The UI contract must list allowed UI paths.");
            AssertTrue(contract.Contains("Forbidden logic paths"), "The UI contract must list forbidden logic paths.");
            AssertTrue(task.Contains("Current UI task"), "The UI task file must provide a concrete task slot.");
            AssertTrue(runner.Contains("agy"), "The runner must call the Antigravity CLI.");
            AssertTrue(runner.Contains("-p"), "The runner must use non-interactive print mode.");
            AssertTrue(runner.Contains("src/DACDT_2026.App/Views/**"), "The runner must allow WPF view edits.");
            AssertTrue(runner.Contains("src/DACDT_2026.App/Form1.PlcControl.cs"), "The runner must block PLC control edits.");
            AssertTrue(runner.Contains("--dangerously-skip-permissions") && runner.Contains("throw"), "The runner must reject dangerous permission bypass flags.");
            AssertTrue(runner.Contains("LOCALAPPDATA") && runner.Contains("agy.exe"), "The runner must find the default Windows Antigravity install path when PATH has not refreshed.");
        }

        private static string GetRepositoryPath(params string[] segments)
        {
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "src", "DACDT_2026.App")))
                {
                    string path = directory.FullName;
                    foreach (string segment in segments)
                    {
                        path = Path.Combine(path, segment);
                    }

                    return path;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the repository root from the test executable.");
        }

        private static CadDocumentService.CadPrimitiveData NewCadLine(double x1, double y1, double x2, double y2)
        {
            return new CadDocumentService.CadPrimitiveData
            {
                SourceType = "Line",
                Points = new List<CadDocumentService.CadCoordinate>
                {
                    new CadDocumentService.CadCoordinate(x1, y1),
                    new CadDocumentService.CadCoordinate(x2, y2)
                },
                ProcessKind = EngraveCutProcessComposer.EngraveKind,
                PathId = -1
            };
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
