using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                CameraRecordingPathAndCommandsAreBound();
                CameraRecordingCreatesMp4VideoFile();
                CameraRecordingNormalizesNativeFrames();
                CameraRecordingUsesMpeg4CodecForX86Ffmpeg();
                CameraRecordingSummaryFormatsElapsedTimeAndFileSize();
                CameraRecordingStatusUsesDurationAndMp4Size();
                CameraRecordingDoesNotRequireWebRtcForLocalFrames();
                AxisMonitorUpdateCadenceStaysResponsive();
                PlcMonitoringUsesOneBatchedReader();
                FastPlcUiUpdatesUseRenderPriority();
                CadTrackingMarkerMovesWithoutInvalidatingCanvasLayout();
                ExitShutdownSendsM210WheneverPlcIsConnected();
                ConfigurationFilePathIsRememberedAndMissingFilesNeedSelection();
                PortableConfigurationIsLoadedSavedAndRecoveredAtStartup();
                FirstRunCreatesDefaultConfigurationFile();
                SettingsUsesOnePortableConfigurationFileWorkflow();
                ConfigurationSaveDoesNotBlockExitForUnreachableNetworkPaths();
                ExitShutdownDoesNotBlockOnLostLan();
                WorkspaceLimitPolicyUsesConfiguredDimensions();
                WorkspaceSettingsDriveScanAndTestAreaLimits();
                PlcConnectionStartsMonitoringBeforeStartupClear();
                PlcConnectionGuardBlocksMissingOrDisconnectedPlc();
                RunProgressIsLimitedToEngraveAndCutPrograms();
                CompletedTestAreaUsesLastExecutedDataNumberToUnlockSelection();
                NormalRunCompletionUnlocksAfterPlcResetsCurrentDataNumber();
                NavigationRefreshesAreSingleFlightAndLatestWins();
                StartCannotRaceWithTestAreaExecution();
                HoldButtonsReleaseWhenMouseCaptureIsLost();
                CadTouchSessionKeepsFixedPinchPairAndResetsOnFingerRelease();
                CadTouchGesturesSupportPinchZoomAndSingleFingerSelection();
                D406JogSpeedUsesFloatWordEncoding();
                DecimalJogSpeedInputAcceptsDotAndComma();
                ZHeightConversionUsesTenThousandScale();
                ZHeightCommandUsesD110ThenPulsesM212();
                LargeCadPreviewKeepsFullSourceAndCapsPreviewPoints();
                LargeCadPreviewSamplesOneHugePolyline();
                CadPreviewSamplesEveryPrimitiveWhenBudgetIsCapped();
                CadPreviewReservesPointsForTrailingPrimitives();
                CadPreviewSamplesAcrossPrimitiveCap();
                CadDisplayPreviewAppliesOffsetWithoutChangingSource();
                CadDisplayPreviewHonorsCancellation();
                CadOverlaySamplingKeepsEndpointsAndCapsPointCount();
                LargeCadProcessPathDoesNotCloneSourceCoordinates();
                LargeCadPreviewAvoidsHiddenCoordinateRowsAndUsesCombinedGeometry();
                DxfRunViewRemovesProcessTableButKeepsPlcProcessData();
                OfflineRuntimeDoesNotStartMqttOrWebRtc();
                WpfThemeManagerAppliesLightAndDarkPalettes();
                EngraveCutComposerKeepsOneOrderedProcessListWithPerRowParameters();
                LaserPowerPercentMapsToPlcRange();
                ActiveEngraveCutRowsSelectDifferentLaserPowerValues();
                EngraveCutPowerSwitchUsesFirstCutRow();
                IntermediateEngraveEndContinuesBeforeCutRows();
                EngraveHomeRowIsDroppedWhenCutFollows();
                MixedProgramUsesM03SpeedForNonCutRowsAndProcessSpeedForWorkRows();
                MixedRunRebuildsSelectedDxfAfterTestArea();
                MixedRunPreservesCurrentViewWhileRefreshingRows();
                CadPathSelectionGroupsConnectedLineSegments();
                CadPathSelectionDoesNotReverseSourceCoordinates();
                CadPathSelectionTogglesEveryPrimitiveInSelectedPath();
                CadPathSelectionToggleTwiceRestoresEngrave();
                CadPathHitIndexFindsNearestHorizontalSegment();
                CadPathHitIndexRejectsMissOutsideRadius();
                CadPathHitIndexUsesPathIdForDeterministicTies();
                CadPathHitIndexHandlesIntMaxValueGridBoundary();
                CadPathHitIndexKeepsTheTrulyNearestPathDespiteTinyDistanceDifference();
                CadPathHitIndexFindsOnlyTheNearbyPathInALargeSet();
                CadPathSelectionUpdatesImmediatelyAndExplainsRunLock();
                CadProgramCompilationStartsAtVersionZero();
                CadProgramCompilationMarksDirtyWithoutPublishing();
                CadProgramCompilationPublishesCurrentVersion();
                CadProgramCompilationRejectsStaleVersionAfterNewerRequest();
                CadProgramCompilationPreservesPublishedVersionWhenRejecting();
                CadPathGroupingObservesPreCancelledToken();
                CadSelectionSchedulesLatestCompilationWithoutAwaitingRows();
                CadCompilationUsesExactDebounceAndPublicationGuards();
                CadCompilationChecksCancellationThroughoutLargeLoops();
                CadExecutionConsumersEnsureCurrentRows();
                TestAreaInvalidatesCadRowsWithoutSchedulingCompilation();
                CadCompilationIsCancelledWhenDocumentClearsOrAppCloses();
                LargeRingBufferRunsPlcIoOutsideUiThread();
                ProgramMonitorAutoScrollIsLatestOnlyAndThrottled();
                CadInteractionAvoidsExpensiveHitTestingAndFullStateRebuild();
                CadInteractionUsesTemporaryBitmapCacheOnlyWhileInteracting();
                SettingsViewUsesApprovedEnglishContract();
                NonHelpViewsDoNotUseKnownVietnameseOperatorLabels();
                SettingsViewExposesSaveSettingsCommand();
                ViewsExposeSharedStylesToXamlDesigner();
                ViewsDeclareConvertersUsedByXamlDesigner();
                WpfXamlUsesValidResourceAndGridSyntax();
                CadPreviewClipsOnlyAtOuterViewport();
                DxfRunViewShowsVirtualizedPointMonitor();
                DxfOnlyViewsRemoveGcodeAndWcsControls();
                DxfRuntimeHasNoGcodeEntryPoints();
                AntigravityUiWorkflowIsGuarded();
                HelpViewContainsVietnameseOperationalGuide();
                TelemetryFeatureIsRemoved();
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
            AssertEqual("42", PerformanceTuning.CameraRecordingFrameIntervalMs.ToString(), "Camera recording should target approximately 24 fps.");
        }

        private static void CameraRecordingPathAndCommandsAreBound()
        {
            string stateSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "WpfUiState.cs"));
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string cameraSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.Camera.cs"));
            string monitorView = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "MonitorView.xaml"));
            string projectSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "DACDT_2026.csproj"));

            AssertTrue(stateSource.Contains("CameraRecordingFolderInput"), "Camera UI state must expose the recording folder input.");
            AssertTrue(stateSource.Contains("BrowseCameraRecordingFolderCommand"), "Camera UI state must expose the browse command.");
            AssertTrue(stateSource.Contains("SetCameraRecordingFolderCommand"), "Camera UI state must expose the set-path command.");
            AssertTrue(formSource.Contains("BrowseCameraRecordingFolderCommand"), "Form command setup must bind the browse command.");
            AssertTrue(formSource.Contains("SetCameraRecordingFolderCommand"), "Form command setup must bind the set-path command.");
            AssertTrue(cameraSource.Contains("FolderBrowserDialog"), "Camera path selection must use a folder picker.");
            AssertTrue(cameraSource.Contains("cameraRecordingDir ="), "Camera recording must use the configured recording directory.");
            AssertTrue(monitorView.Contains("{Binding CameraRecordingFolderInput"), "Monitor UI must bind the recording folder input.");
            AssertTrue(monitorView.Contains("{Binding BrowseCameraRecordingFolderCommand}"), "Monitor UI must expose Browse for the recording folder.");
            AssertTrue(monitorView.Contains("{Binding SetCameraRecordingFolderCommand}"), "Monitor UI must expose Set Path for the recording folder.");
            AssertTrue(projectSource.Contains("System.Windows.Forms"), "The app must reference Windows Forms for the folder picker.");
        }

        private static void CameraRecordingCreatesMp4VideoFile()
        {
            string cameraSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.Camera.cs"));
            string stateSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "WpfUiState.cs"));
            string projectSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "DACDT_2026.csproj"));
            string packagesSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "packages.config"));

            AssertTrue(cameraSource.Contains("CameraVideoRecorder"), "Camera recording must use a video writer.");
            AssertTrue(cameraSource.Contains(".mp4"), "Camera recording output must be MP4.");
            AssertTrue(cameraSource.Contains("WriteFrame"), "Camera frames must be written to the MP4 recorder.");
            AssertTrue(!cameraSource.Contains("frame_{frameNo:D6}.jpg"), "Camera recording must not create one JPEG file per frame.");
            AssertTrue(stateSource.Contains("Recording MP4"), "Camera UI must describe MP4 recording.");
            AssertTrue(projectSource.Contains("Accord.Video.FFMPEG"), "The app must reference the MP4 video writer.");
            AssertTrue(packagesSource.Contains("Accord.Video.FFMPEG"), "The MP4 video writer dependency must be restorable.");
        }

        private static void CameraRecordingNormalizesNativeFrames()
        {
            using (var source = new Bitmap(5, 3, PixelFormat.Format32bppArgb))
            using (var normalized = CameraVideoFrameNormalizer.CreateRgb24Frame(source, 4, 2))
            {
                AssertEqual("4", normalized.Width.ToString(), "The normalized frame must keep the writer width.");
                AssertEqual("2", normalized.Height.ToString(), "The normalized frame must keep the writer height.");
                AssertEqual(PixelFormat.Format24bppRgb.ToString(), normalized.PixelFormat.ToString(), "The native video writer must receive an owned RGB24 bitmap.");
            }

            string recorderSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "CameraVideoRecorder.cs"));
            AssertTrue(recorderSource.Contains("CameraVideoFrameNormalizer.CreateRgb24Frame"), "The recorder must normalize every frame before native FFmpeg writing.");
            AssertTrue(!recorderSource.Contains("writer.WriteVideoFrame(source)"), "The recorder must not pass a camera-owned bitmap directly to FFmpeg.");
        }

        private static void CameraRecordingUsesMpeg4CodecForX86Ffmpeg()
        {
            string recorderSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "CameraVideoRecorder.cs"));

            AssertTrue(recorderSource.Contains("VideoCodec.MPEG4"), "The x86 recorder must use the MPEG4 encoder that writes MP4 safely.");
            AssertTrue(!recorderSource.Contains("VideoCodec.H264"), "The x86 recorder must not use the crashing H264 encoder from the bundled FFmpeg build.");
        }

        private static void CameraRecordingSummaryFormatsElapsedTimeAndFileSize()
        {
            AssertEqual("00:00:00", CameraRecordingSummary.FormatElapsed(TimeSpan.Zero), "A new recording must show zero elapsed time.");
            AssertEqual("01:02:03", CameraRecordingSummary.FormatElapsed(new TimeSpan(1, 2, 3)), "Elapsed recording time must use hours, minutes, and seconds.");
            AssertEqual("1.5 MB", CameraRecordingSummary.FormatFileSize(1572864), "Completed MP4 size must be formatted for operators.");
            AssertEqual("MP4 saved: 00:01:23 (12.4 MB)", CameraRecordingSummary.FormatSavedText(new TimeSpan(0, 1, 23), 13002342), "Saved text must include elapsed recording time and MP4 size.");
        }

        private static void CameraRecordingStatusUsesDurationAndMp4Size()
        {
            string stateSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "WpfUiState.cs"));
            string cameraSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.Camera.cs"));

            AssertTrue(stateSource.Contains("CameraRecordingElapsed"), "Camera UI state must expose elapsed recording time.");
            AssertTrue(!stateSource.Contains("CameraRecordedFrames + \" frames\""), "Camera UI must not show frame count as recording duration.");
            AssertTrue(cameraSource.Contains("cameraRecordingDurationTimer"), "Camera recording must update elapsed time with a dedicated timer.");
            AssertTrue(cameraSource.Contains("CameraRecordingSummary.FormatSavedText"), "Stopping recording must show elapsed time and MP4 file size.");
            AssertTrue(cameraSource.Contains("new FileInfo(recordingPath).Length"), "MP4 size must be read after recording completes.");
        }

        private static void CameraRecordingDoesNotRequireWebRtcForLocalFrames()
        {
            string cameraSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.Camera.cs"));

            AssertTrue(!cameraSource.Contains("webRtcBridgeClient"), "Local camera must not depend on WebRTC.");
            AssertTrue(!cameraSource.Contains("webReady"), "Local camera must not depend on web runtime state.");
        }

        private static void AxisMonitorUpdateCadenceStaysResponsive()
        {
            AssertEqual("10", PerformanceTuning.PlcPollIntervalMs.ToString(), "PLC axis poll should be the fastest path in the application.");
            AssertEqual("1", PerformanceTuning.PlcPollMinimumDelayMs.ToString(), "PLC polling should not add a large artificial delay when the PLC call is already fast.");
            AssertEqual("16", PerformanceTuning.ControlUiPushIntervalMs.ToString(), "Axis monitor UI should target smooth local display cadence without waiting for MQTT.");
            AssertEqual("16", PerformanceTuning.ControlTrackingUiPushIntervalMs.ToString(), "CAD tracking marker should update at the smooth local display cadence.");
            AssertEqual("1000", PerformanceTuning.SlowPlcMonitorPollIntervalMs.ToString(), "Non-axis PLC monitor rows should not block the fast axis path.");
        }

        private static void PlcMonitoringUsesOneBatchedReader()
        {
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string plcSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.PlcControl.cs"));
            string communicationSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "PLCCommunication.cs"));
            string dxfSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));

            AssertTrue(formSource.Contains("private PLCCommunication plcMonitorComm;"), "PLC monitoring must use a dedicated connection so coordinate writes cannot block the display.");
            int monitorGetterStart = plcSource.IndexOf("private bool TryGetMonitoringPlc", StringComparison.Ordinal);
            int monitorGetterEnd = plcSource.IndexOf("private bool ShouldPausePlcPollingForWrite", monitorGetterStart, StringComparison.Ordinal);
            string monitorGetterSource = plcSource.Substring(monitorGetterStart, monitorGetterEnd - monitorGetterStart);
            AssertTrue(!monitorGetterSource.Contains("TryGetConnectedPlc"), "The monitor reader must never fall back to the coordinate-write connection.");
            AssertTrue(!plcSource.Contains("Task.WhenAll("), "PLC reads must not use competing poll loops on the same MX Component connection.");
            AssertTrue(!plcSource.Contains("PlcMonitorLoopAsync"), "Motion and U0\\G values must be captured by one ordered reader loop.");
            AssertTrue(communicationSource.Contains("ReadDeviceRandom2"), "Monitoring should use MX Component random batch read instead of many GetDevice calls.");
            AssertTrue(plcSource.Contains("TryReadDeviceWords"), "The PLC poll should request one batched monitoring snapshot.");
            AssertTrue(plcSource.Contains("monitorBase + 35"), "The batched snapshot must include each axis current QD75 data number.");
            AssertTrue(plcSource.Contains("private const int FastSnapshotWordCount = 46") && plcSource.Contains("private static readonly string[] FastMonitorDeviceList"), "The fast snapshot must read displayed U0\\G monitor values for all four axes in the same cycle.");
            AssertTrue(plcSource.Contains("devices.Add(\"D406\")") && plcSource.Contains("devices.Add(\"D407\")"), "The batched snapshot must load the live jog speed without waiting for the slow monitor pass.");
            AssertTrue(!plcSource.Contains("comm.ReadBuffer(0, ControlBaseG"), "The slow path must not read unused control blocks on the coordinate-write connection.");
            AssertTrue(dxfSource.Contains("ShouldPausePlcPollingForWrite(comm)"), "Bulk coordinate writes must keep the dedicated monitoring connection running.");
            AssertTrue(dxfSource.Contains("Interlocked.Increment(ref plcWriteInFlight)"), "Bulk coordinate writes must suspend low-priority reads on the write connection.");
            AssertTrue(communicationSource.Contains("randomReadRetryAfterUtc"), "A transient MX random-read error must be retried instead of disabling batching for the connection lifetime.");
        }

        private static void FastPlcUiUpdatesUseRenderPriority()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.StatePublisher.cs"));

            AssertTrue(source.IndexOf("RunOnUiAsync(() =>", StringComparison.Ordinal) >= 0, "PLC state publisher must marshal updates to the WPF dispatcher.");
            AssertTrue(source.IndexOf("DispatcherPriority.Render", StringComparison.Ordinal) >= 0, "Fast PLC updates must use WPF render priority so axis values and the CAD marker are not delayed behind normal UI work.");
        }

        private static void CadTrackingMarkerMovesWithoutInvalidatingCanvasLayout()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
            int start = source.IndexOf("<ItemsControl ItemsSource=\"{Binding CadTrackingPoints}\"", StringComparison.Ordinal);
            int end = source.IndexOf("</ItemsControl>", start, StringComparison.Ordinal);
            AssertTrue(start >= 0 && end > start, "DXF view must contain the CAD tracking marker layer.");

            string markerLayer = source.Substring(start, end - start);
            AssertTrue(markerLayer.IndexOf("Canvas.Left\" Value=\"{Binding X}", StringComparison.Ordinal) < 0, "Tracking marker movement must not invalidate the CAD canvas layout.");
            AssertTrue(markerLayer.IndexOf("<TranslateTransform X=\"{Binding X}\" Y=\"{Binding Y}\"/>", StringComparison.Ordinal) >= 0, "Tracking marker must move with a render transform.");
        }

        private static void ExitShutdownSendsM210WheneverPlcIsConnected()
        {
            AssertTrue(ExitShutdownPolicy.ShouldSendExitStop(plcConnected: true), "Exit should send M210 whenever PLC is connected, even if the robot is not running.");
            AssertTrue(!ExitShutdownPolicy.ShouldSendExitStop(plcConnected: false), "Exit cannot send M210 when PLC is disconnected.");
            AssertEqual("100", PerformanceTuning.ExitStopPulseMs.ToString(), "Exit should pulse M210 briefly instead of holding it ON.");
            AssertEqual("100", PerformanceTuning.ExitStopDelayMs.ToString(), "Exit should move to HOME quickly after pulsing M210.");
            AssertEqual("100", PerformanceTuning.ExitHomePulseMs.ToString(), "Exit should pulse HOME ALL briefly.");
            AssertEqual("100", PerformanceTuning.ExitHomeDelayMs.ToString(), "Exit should close shortly after HOME ALL.");
        }

        private static void ConfigurationFilePathIsRememberedAndMissingFilesNeedSelection()
        {
            string root = Path.Combine(Path.GetTempPath(), "dacdt-config-test-" + Guid.NewGuid().ToString("N"));
            string defaultPath = Path.Combine(root, "Documents", "DACDT_2026_settings.txt");
            string statePath = Path.Combine(root, "state", "config_path.txt");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(defaultPath));
                var store = new ConfigurationFilePathStore(defaultPath, statePath);

                AssertEqual(defaultPath, store.GetSelectedPath(), "The default configuration file must be used before a path is selected.");
                AssertEqual(Path.GetDirectoryName(defaultPath), store.GetBrowseDirectory(defaultPath), "Browse must open the folder that contains the selected configuration file.");
                AssertTrue(store.TrySaveSelectedPath(@"\\server\dacdt\machine.txt"), "A UNC configuration path must be remembered.");
                AssertEqual(@"\\server\dacdt\machine.txt", store.GetSelectedPath(), "The remembered path must be restored.");
                AssertTrue(store.NeedsSelection(store.GetSelectedPath()), "A missing selected file must request a replacement selection.");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        private static void PortableConfigurationIsLoadedSavedAndRecoveredAtStartup()
        {
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));

            AssertTrue(formSource.Contains("LoadSelectedConfigurationAtStartup"), "Startup must load the remembered configuration file.");
            AssertTrue(formSource.Contains("PromptForConfigurationFileAsync"), "A missing configuration file must prompt for replacement selection.");
            AssertTrue(formSource.Contains("InitialDirectory = configurationFilePathStore.GetBrowseDirectory(configurationFilePath)"), "Browse must open directly in the selected configuration folder.");
            AssertTrue(formSource.Contains("SaveSettingsToFile(selectedPath)"), "Save Settings must write to the selected portable file.");
            AssertTrue(formSource.Contains("SyncSettingsFromUiForPersistence();"), "Closing must snapshot current UI values before saving.");
        }

        private static void FirstRunCreatesDefaultConfigurationFile()
        {
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));

            AssertTrue(formSource.Contains("File.Exists(DefaultConfigurationFilePath)"), "Startup must check whether the default configuration file exists.");
            AssertTrue(formSource.Contains("SaveSettingsToFile(DefaultConfigurationFilePath)"), "First startup must create the default configuration file when it is missing.");
            AssertTrue(formSource.Contains("configurationFileSelectionRequired = false"), "A successfully created default configuration must not prompt for file selection.");
        }

        private static void SettingsUsesOnePortableConfigurationFileWorkflow()
        {
            string xaml = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "SettingsView.xaml"));
            string stateSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "WpfUiState.cs"));

            AssertTrue(xaml.Contains("Configuration File"), "Settings must show the selected configuration file.");
            AssertTrue(xaml.Contains("BrowseConfigurationFileCommand"), "Settings must let the operator choose a portable configuration file.");
            AssertTrue(!xaml.Contains("Configuration Profiles"), "Named profiles must be removed from Settings.");
            AssertTrue(stateSource.Contains("ConfigurationFilePathInput"), "The UI state must expose the configuration-file path.");
        }

        private static void ConfigurationSaveDoesNotBlockExitForUnreachableNetworkPaths()
        {
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));

            AssertTrue(formSource.Contains("IsUncConfigurationPath"), "Configuration persistence must recognize UNC network paths.");
            AssertTrue(formSource.Contains("SaveConfigurationToNetworkPathInBackground"), "Exit must save a network configuration path without blocking the UI thread.");
        }

        private static void ExitShutdownDoesNotBlockOnLostLan()
        {
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string policySource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "ExitShutdownPolicy.cs"));

            AssertTrue(policySource.Contains("PlcExitWaitTimeoutMs = 600"), "PLC shutdown must have a short, explicit maximum wait time.");
            AssertTrue(policySource.Contains("Task.WhenAny"), "PLC shutdown must race an unresponsive operation against the timeout.");
            AssertTrue(formSource.Contains("await ExitShutdownPolicy.WaitForBestEffortAsync(SendStopForExitAsync())"), "Exit must close after the bounded PLC command wait.");
            AssertTrue(formSource.Contains("QueuePlcDisposeForShutdown"), "PLC connections must be disposed in the background during shutdown.");

            var stuckOperation = new TaskCompletionSource<bool>();
            var timer = Stopwatch.StartNew();
            ExitShutdownPolicy.WaitForBestEffortAsync(stuckOperation.Task, 25).GetAwaiter().GetResult();
            AssertTrue(timer.ElapsedMilliseconds < 250, "A stuck PLC operation must return control after the specified timeout.");
        }

        private static void PlcConnectionStartsMonitoringBeforeStartupClear()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.PlcControl.cs"));
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            int connectStart = source.IndexOf("private async Task HandleConnectToggleAsync", StringComparison.Ordinal);
            int assignmentIndex = source.IndexOf("plcComm = connectedComm", connectStart, StringComparison.Ordinal);
            int monitorAssignmentIndex = source.IndexOf("plcMonitorComm = monitorComm", assignmentIndex, StringComparison.Ordinal);
            int pollingIndex = source.IndexOf("StartPlcPolling()", assignmentIndex, StringComparison.Ordinal);
            int clearIndex = source.IndexOf("ClearAllBuffers(connectedComm", connectStart, StringComparison.Ordinal);

            AssertTrue(connectStart >= 0, "PLC connect handler should exist.");
            AssertTrue(assignmentIndex > connectStart && monitorAssignmentIndex > assignmentIndex && pollingIndex > monitorAssignmentIndex && clearIndex > pollingIndex, "The dedicated monitor must connect and polling must start before the slow 600-point startup clear.");
            AssertTrue(source.Contains("if (monitorComm == null)"), "Connection must fail clearly when the dedicated monitoring channel cannot open.");
            AssertTrue(formSource.Contains("private volatile bool plcStartupReady;"), "PLC commands must remain guarded until startup buffer clear finishes.");
            AssertTrue(formSource.Contains("private int plcConnectionChangeInFlight;"), "PLC connect/disconnect commands must be single-flight.");
            AssertTrue(source.Contains("CompareExchange(ref plcConnectionChangeInFlight"), "PLC connect/disconnect handler must reject overlapping operations.");
            AssertTrue(source.Contains("plcStartupReady = true;"), "PLC commands should become ready only after startup clear succeeds.");
            string dxfSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            int sendStart = dxfSource.IndexOf("private async Task<bool> HandleSendCadXAsync", StringComparison.Ordinal);
            int sendEnd = dxfSource.IndexOf("private async Task HandleTestEngraveAreaAsync", sendStart, StringComparison.Ordinal);
            string sendSource = dxfSource.Substring(sendStart, sendEnd - sendStart);
            AssertTrue(sendSource.Contains("RequirePlcStartupReadyAsync(\"Send CAD\")"), "Direct coordinate upload must wait until startup clear succeeds.");
        }

        private static void PlcConnectionGuardBlocksMissingOrDisconnectedPlc()
        {
            AssertTrue(!PlcConnectionGuard.CanUsePlc(communicationObjectExists: false, isConnected: false), "PLC operations must be blocked when no PLC communication object exists.");
            AssertTrue(!PlcConnectionGuard.CanUsePlc(communicationObjectExists: true, isConnected: false), "PLC operations must be blocked when the PLC communication object is disconnected.");
            AssertTrue(PlcConnectionGuard.CanUsePlc(communicationObjectExists: true, isConnected: true), "PLC operations are allowed only after a live PLC connection exists.");
            AssertEqual("PLC is not connected.", PlcConnectionGuard.NotConnectedMessage, "Disconnected PLC operations should use one consistent message.");
        }

        private static void RunProgressIsLimitedToEngraveAndCutPrograms()
        {
            string stateSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "WpfUiState.cs"));
            string dashboardView = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DashboardView.xaml"));
            string monitorView = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "MonitorView.xaml"));

            AssertTrue(stateSource.Contains("RunProgressVisible"), "Run progress should expose a dedicated visibility state.");
            AssertTrue(stateSource.Contains("EngraveCutProcessComposer.EngraveKind") && stateSource.Contains("EngraveCutProcessComposer.CutKind"), "Run progress should recognize only engrave and cut rows.");
            AssertTrue(dashboardView.Contains("Visibility=\"{Binding RunProgressVisible"), "Dashboard should hide run progress for non-engrave/cut operations.");
            AssertTrue(monitorView.Contains("Visibility=\"{Binding RunProgressVisible"), "Monitor should hide run progress for non-engrave/cut operations.");
        }

        private static void CompletedTestAreaUsesLastExecutedDataNumberToUnlockSelection()
        {
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.PlcControl.cs"));
            string dxfSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            int start = source.IndexOf("private Task PollPlcOnceAsync", StringComparison.Ordinal);
            int end = source.IndexOf("private void ScheduleBackgroundPlcWork", start, StringComparison.Ordinal);
            AssertTrue(start >= 0 && end > start, "The PLC polling completion handler must exist.");

            string handler = source.Substring(start, end - start);
            AssertTrue(formSource.Contains("private readonly ProgramRunCompletionTracker programRunCompletionTracker"), "Program completion must keep dedicated state for each RUN.");
            AssertTrue(dxfSource.Contains("programRunCompletionTracker.Begin();"), "Starting Test Area must reset completion tracking before its start pulse.");
            AssertTrue(
                handler.Contains("programRunCompletionTracker.Observe("),
                "PLC polling must use the shared completion tracker for every program type.");
            AssertTrue(
                handler.Contains("Math.Max(0, axLastDataNo[0])"),
                "Program completion must include the PLC last-executed data number because the PLC can reset the current number to zero at completion.");
            AssertTrue(handler.Contains("bool allAxesStopped = true"), "Cut-path selection must remain locked until every axis has stopped.");
        }

        private static void NormalRunCompletionUnlocksAfterPlcResetsCurrentDataNumber()
        {
            var tracker = new ProgramRunCompletionTracker();
            tracker.Begin();

            AssertTrue(
                !tracker.Observe(activeDataNo: 0, lastDataNo: 5, processRowCount: 5, allAxesStopped: true),
                "A stale final data number must not unlock RUN before the new program has executed a row.");
            AssertTrue(
                !tracker.Observe(activeDataNo: 1, lastDataNo: 5, processRowCount: 5, allAxesStopped: false),
                "A program that is moving must remain locked.");
            AssertTrue(
                !tracker.Observe(activeDataNo: 5, lastDataNo: 4, processRowCount: 5, allAxesStopped: true),
                "RUN must remain locked until the PLC last data number confirms every point was executed.");
            AssertTrue(
                tracker.Observe(activeDataNo: 0, lastDataNo: 5, processRowCount: 5, allAxesStopped: true),
                "RUN must unlock after an executed program returns current data number to zero at its final row.");
        }

        private static void NavigationRefreshesAreSingleFlightAndLatestWins()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));

            AssertTrue(source.Contains("private readonly SemaphoreSlim viewRefreshGate"), "Navigation refreshes must share one UI refresh gate.");
            AssertTrue(source.Contains("private int navigationRefreshVersion;"), "Navigation refreshes must track the newest requested view.");
            AssertTrue(source.Contains("Interlocked.Increment(ref navigationRefreshVersion)"), "Each navigation request must advance the refresh version.");
            AssertTrue(source.Contains("RefreshViewDataAfterNavigationAsync(requestedView, requestVersion)"), "Navigation must refresh the requested view with its version.");
            AssertTrue(source.Contains("private async Task RefreshViewDataAfterNavigationAsync(string viewName, int requestVersion)"), "View refresh must receive the navigation version.");
            AssertTrue(source.Contains("await viewRefreshGate.WaitAsync()"), "View refresh must be single-flight.");
            AssertTrue(source.Contains("requestVersion != Volatile.Read(ref navigationRefreshVersion)"), "Stale navigation refreshes must be discarded.");
            AssertTrue(source.Contains("viewRefreshGate.Release()"), "View refresh gate must always be released.");
        }

        private static void StartCannotRaceWithTestAreaExecution()
        {
            string plcSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.PlcControl.cs"));
            string dxfSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            int startIndex = plcSource.IndexOf("private async Task HandleStartWriteAsync", StringComparison.Ordinal);
            int startEnd = plcSource.IndexOf("private async Task HandleMixedEngraveCutStartAsync", startIndex, StringComparison.Ordinal);
            string startHandler = plcSource.Substring(startIndex, startEnd - startIndex);
            int testIndex = dxfSource.IndexOf("private async Task HandleTestEngraveAreaAsync", StringComparison.Ordinal);
            string testHandler = dxfSource.Substring(testIndex);

            AssertTrue(startHandler.Contains("if (IsProgramRunning())"), "RUN must be blocked while Test Area or another program is still running.");
            AssertTrue(startHandler.Contains("Wait for the current program to finish"), "RUN must explain why it was blocked instead of silently queuing a second run.");
            AssertTrue(testHandler.Contains("if (IsProgramRunning())"), "Test Area must be blocked while another program is still running.");
            AssertTrue(testHandler.Contains("programRunCompletionTracker.Begin();"), "Test Area must reset the shared PLC completion tracker before it starts.");
        }

        private static void HoldButtonsReleaseWhenMouseCaptureIsLost()
        {
            string dashboardXaml = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DashboardView.xaml"));
            string dxfXaml = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
            string sidebarXaml = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "Panels", "SidebarControl.xaml"));
            string dashboardCode = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DashboardView.xaml.cs"));
            string dxfCode = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml.cs"));
            string sidebarCode = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "Panels", "SidebarControl.xaml.cs"));
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string plcSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.PlcControl.cs"));

            AssertTrue(dashboardXaml.Contains("LostMouseCapture=\"HoldButton_LostMouseCapture\""), "Dashboard hold buttons must release on lost mouse capture.");
            AssertTrue(dxfXaml.Contains("LostMouseCapture=\"HoldButton_LostMouseCapture\""), "DXF hold buttons must release on lost mouse capture.");
            AssertTrue(sidebarXaml.Contains("LostMouseCapture=\"JogButton_LostMouseCapture\""), "Jog buttons must release on lost mouse capture.");
            AssertTrue(dashboardCode.Contains("CaptureMouse()") && dashboardCode.Contains("HoldButton_LostMouseCapture"), "Dashboard hold handling must capture and safely release the mouse.");
            AssertTrue(dxfCode.Contains("CaptureMouse()") && dxfCode.Contains("HoldButton_LostMouseCapture"), "DXF hold handling must capture and safely release the mouse.");
            AssertTrue(sidebarCode.Contains("CaptureMouse()") && sidebarCode.Contains("JogButton_LostMouseCapture"), "Jog handling must capture and safely release the mouse.");
            AssertTrue(formSource.Contains("private readonly SemaphoreSlim plcDeviceWriteGate"), "PLC device writes must share a serialization gate.");
            AssertTrue(plcSource.Contains("private async Task WriteDeviceValueSerializedAsync") && plcSource.Contains("await plcDeviceWriteGate.WaitAsync()"), "PLC device writes must preserve ON/OFF ordering.");
        }

        private static void CadTouchSessionKeepsFixedPinchPairAndResetsOnFingerRelease()
        {
            var session = new CadTouchGestureSession();
            session.BeginTouch(11, new System.Windows.Point(10, 10));
            session.BeginTouch(22, new System.Windows.Point(30, 10));
            session.UpdateTouch(22, new System.Windows.Point(50, 10));

            AssertTrue(session.TryTakePinchFrame(out CadPinchFrame frame), "A two-finger move must produce one pinch frame.");
            AssertEqual("11", frame.PrimaryTouchId.ToString(), "The first touch must remain the primary pinch touch.");
            AssertEqual("22", frame.SecondaryTouchId.ToString(), "The second touch must remain the secondary pinch touch.");
            AssertTrue(!session.TryTakePinchFrame(out frame), "A frame must be consumed once rather than applied repeatedly.");

            session.EndTouch(11);
            AssertTrue(!session.IsPinching, "Releasing either pinch finger must end the pinch session.");
        }

        private static void CadTouchGesturesSupportPinchZoomAndSingleFingerSelection()
        {
            string xaml = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
            string code = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml.cs"));

            AssertTrue(xaml.Contains("PreviewTouchDown=\"CadViewport_PreviewTouchDown\""), "CAD viewport must receive the first touch point before it becomes a mouse event.");
            AssertTrue(xaml.Contains("PreviewTouchMove=\"CadViewport_PreviewTouchMove\""), "CAD viewport must track touch movement for pinch zoom.");
            AssertTrue(xaml.Contains("PreviewTouchUp=\"CadViewport_PreviewTouchUp\""), "CAD viewport must finish touch selection and release captures.");
            AssertTrue(!xaml.Contains("<Binding Source=\"24\"/>"), "Tablet CAD selection must not create per-path 24 DIP hit-test strokes.");
            AssertTrue(code.Contains("CadTouchGestureSession"), "CAD touch handling must use one deterministic touch session.");
            AssertTrue(code.Contains("CompositionTarget.Rendering"), "Pinch transforms must be coalesced to render cadence.");
            AssertTrue(code.Contains("touchSession.IsPinchTouch"), "Only a released pinch finger may end the active pinch session.");
            AssertTrue(code.Contains("e.StylusDevice != null"), "Promoted touch mouse events must not trigger CAD mouse commands.");
            AssertTrue(code.Contains("ApplyCadPinchTransform"), "Pinch movement must update zoom around the two-finger midpoint.");
            AssertTrue(code.Contains("TryFindNearest"), "A one-finger tap must query the spatial CAD path index.");
            AssertTrue(code.Contains("ToggleCadPathCommand.Execute(pathId)"), "A one-finger tap must preserve direct CAD path selection.");
            AssertTrue(code.Contains("TouchDevice.Capture(CadViewport)"), "Touch points must stay captured while the finger moves across the CAD viewport.");
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

        private static void WorkspaceLimitPolicyUsesConfiguredDimensions()
        {
            AssertTrue(WorkspaceLimitPolicy.IsValid(175.0, 175.0),
                "175 x 175 must be a valid Workspace.");
            AssertTrue(!WorkspaceLimitPolicy.IsValid(0.0, 175.0),
                "Zero width must be rejected.");
            AssertTrue(!WorkspaceLimitPolicy.IsValid(double.NaN, 175.0),
                "NaN width must be rejected.");
            AssertTrue(!WorkspaceLimitPolicy.IsValid(175.0, double.PositiveInfinity),
                "Infinite height must be rejected.");
            AssertTrue(WorkspaceLimitPolicy.IsRangeWithin(0.0, 171.0, 175.0),
                "Coordinate 171 must fit a configured 175 mm Workspace.");
            AssertTrue(!WorkspaceLimitPolicy.IsRangeWithin(0.0, 171.0, 170.0),
                "Coordinate 171 must exceed a configured 170 mm Workspace.");
        }

        private static void WorkspaceSettingsDriveScanAndTestAreaLimits()
        {
            string form = File.ReadAllText(GetRepositoryPath(
                "src", "DACDT_2026.App", "Form1.cs"));
            string handler = File.ReadAllText(GetRepositoryPath(
                "src", "DACDT_2026.App", "Form1.DxfHandler.cs"));

            AssertTrue(form.Contains("WorkspaceLimitPolicy.IsValid(requestedWidth, requestedHeight)"),
                "Workspace Apply must validate both configured dimensions.");
            AssertTrue(form.Contains("workspaceWidth = requestedWidth;")
                && form.Contains("workspaceHeight = requestedHeight;"),
                "Workspace Apply must update runtime state before scan and preview.");
            AssertTrue(handler.Contains("double snapLimitX = workspaceWidth;")
                && handler.Contains("double snapLimitY = workspaceHeight;"),
                "Scan Limits must snapshot the configured Workspace.");
            AssertTrue(!handler.Contains("const double LimitX = 170.0")
                && !handler.Contains("const double LimitY = 170.0")
                && !handler.Contains("170x170"),
                "DXF limit checks must not retain fixed 170 mm constants.");
        }

        #if false
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

        private static void WebCadUploadReassemblesBinaryChunks()
        {
            var upload = new WebCadUploadSession();
            upload.Begin("job-binary", "part.nc", totalChunks: 2, totalBytes: 10);

            bool complete1 = upload.AddBinaryChunk("job-binary", 1, System.Text.Encoding.UTF8.GetBytes("Y10"));
            bool complete2 = upload.AddBinaryChunk("job-binary", 0, System.Text.Encoding.UTF8.GetBytes("G1 X10 "));

            AssertTrue(!complete1, "Binary upload should not complete before all chunks arrive.");
            AssertTrue(complete2, "Binary upload should complete when the last missing chunk arrives.");
            AssertEqual("G1 X10 Y10", System.Text.Encoding.UTF8.GetString(upload.Assemble()), "Binary upload chunks should reassemble in index order.");
        }

        private static void WebCadUploadReportsMissingChunksForRetry()
        {
            var upload = new WebCadUploadSession();
            upload.Begin("job-missing", "part.nc", totalChunks: 3, totalBytes: 6);
            upload.AddBinaryChunk("job-missing", 0, new byte[] { 1, 2 });
            upload.AddBinaryChunk("job-missing", 2, new byte[] { 5, 6 });

            AssertEqual("1", string.Join(",", upload.GetMissingChunkIndexes()), "Upload should report missing chunk indexes for retry.");
        }

        private static void WebCadBinaryUploadUsesRawMqttPayloads()
        {
            string webSource = File.ReadAllText(GetRepositoryPath("docs", "index.html"));
            string mqttSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "MqttPublishService.cs"));
            string formSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string uploadSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.WebCadUpload.cs"));

            AssertTrue(webSource.Contains("new Paho.MQTT.Message(bytes)"), "Web CAD upload should construct raw MQTT binary messages.");
            AssertTrue(webSource.Contains("DACDT/cad/upload/binary/"), "Web CAD binary chunks should carry job and index in the topic.");
            int binaryMethodIndex = webSource.IndexOf("publishBinaryCadChunk", StringComparison.Ordinal);
            AssertTrue(binaryMethodIndex >= 0 && webSource.IndexOf("msg.qos = 0;", binaryMethodIndex, StringComparison.Ordinal) >= 0, "Binary CAD chunks should use QoS 0 for continuous transfer.");
            AssertTrue(mqttSource.Contains("BinaryMessageReceived"), "MQTT service should expose raw binary upload messages.");
            AssertTrue(formSource.Contains("DACDT/cad/upload/binary/#"), "App should subscribe to binary CAD upload chunks.");
            AssertTrue(uploadSource.Contains("AddBinaryChunk"), "App upload handler should assemble raw binary chunks.");
            AssertTrue(uploadSource.Contains("GetMissingChunkIndexes"), "App upload handler should report missing chunks for retry.");
        }

        private static void WebCadUploadRejectsUnsupportedFiles()
        {
            AssertTrue(WebCadUploadSession.IsAllowedFileName("shape.dxf"), "DXF upload should be accepted.");
            AssertTrue(WebCadUploadSession.IsAllowedFileName("laser.nc"), "NC/G-code upload should be accepted.");
            AssertTrue(!WebCadUploadSession.IsAllowedFileName("notes.pdf"), "Non-CAD upload should be rejected.");
        }

        private static void CadMqttTransferSplitsJsonItemsByUtf8Size()
        {
            var items = new[]
            {
                "{\"id\":0,\"name\":\"đường thẳng\"}",
                "{\"id\":1,\"points\":[1,2,3,4]}",
                "{\"id\":2,\"points\":[5,6,7,8]}"
            };

            var chunks = CadMqttTransfer.SplitJsonItems(items, 64);

            AssertTrue(chunks.Count > 1, "CAD transfer should split items into multiple chunks when the byte limit is reached.");
            AssertEqual("3", chunks.SelectMany(chunk => chunk).Count().ToString(), "CAD transfer should keep every primitive exactly once.");
            AssertEqual(items[0], chunks.SelectMany(chunk => chunk).First(), "CAD transfer should preserve item order.");
            AssertTrue(chunks.All(chunk => System.Text.Encoding.UTF8.GetByteCount("[" + string.Join(",", chunk) + "]") <= 64), "CAD chunks should respect the UTF-8 byte limit.");
        }

        private static void CadMqttCadDirectionsUseChunkedProtocol()
        {
            string statePublisher = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.StatePublisher.cs"));
            string uploadHandler = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.WebCadUpload.cs"));
            string web = File.ReadAllText(GetRepositoryPath("docs", "index.html"));

            AssertTrue(statePublisher.Contains("cadTransfer\\\":\\\"start"), "App-to-web CAD must publish a transfer start envelope.");
            AssertTrue(statePublisher.Contains("cadTransfer\\\":\\\"chunk"), "App-to-web CAD must publish chunk envelopes.");
            AssertTrue(statePublisher.Contains("PublishDirectAsync(\"DACDT/cad/state\""), "App-to-web CAD must bypass the bounded general MQTT queue.");
            AssertTrue(uploadHandler.Contains("webCadUploadMessageGate"), "Web-to-app CAD chunks must be processed sequentially.");
            AssertTrue(web.Contains("const chunkSize = 128 * 1024;"), "Web-to-app CAD must use larger upload chunks.");
            AssertTrue(web.Contains("handleCadTransferMessage"), "Web must reassemble app-to-web CAD transfers before rendering.");
        }

        #endif

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
            AssertEqual("#FFE3EAF2", ((System.Windows.Media.SolidColorBrush)resources["BgBrush"]).Color.ToString(), "Light theme should apply a soft slate background.");
            AssertEqual("#FF102033", ((System.Windows.Media.SolidColorBrush)resources["TextBrush"]).Color.ToString(), "Light theme should keep readable dark text.");
            AssertEqual("#FFD4E4F3", ((System.Windows.Media.SolidColorBrush)resources["CardHeaderBrush"]).Color.ToString(), "Light axis card header should use a calm blue header.");
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

        private static void MixedRunRebuildsSelectedDxfAfterTestArea()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.PlcControl.cs"));
            int start = source.IndexOf("private async Task HandleMixedEngraveCutStartAsync", StringComparison.Ordinal);
            int end = source.IndexOf("private async Task<bool> SetMixedLaserPowerAsync", start, StringComparison.Ordinal);
            AssertTrue(start >= 0 && end > start, "The mixed engrave/cut RUN handler must exist.");

            string handler = source.Substring(start, end - start);
            int rebuildIndex = handler.IndexOf("await EnsureCadProgramCurrentAsync();", StringComparison.Ordinal);
            int snapshotIndex = handler.IndexOf("var allRows = processRows.ToList();", StringComparison.Ordinal);
            int sendIndex = handler.IndexOf("await HandleSendCadXAsync();", StringComparison.Ordinal);

            AssertTrue(rebuildIndex >= 0, "Mixed DXF RUN must ensure the selected contour is current after Test Area replaced the process rows.");
            AssertTrue(rebuildIndex < snapshotIndex, "Mixed DXF RUN must ensure current rows before snapshotting process rows.");
            AssertTrue(snapshotIndex < sendIndex, "Mixed DXF RUN must snapshot the current contour before writing it to the PLC.");
            AssertTrue(!handler.Contains("await RebuildMixedEngraveCutProgramAsync();"),
                "Mixed DXF RUN must not unconditionally rebuild rows when the current version is already published.");
        }

        private static void MixedRunPreservesCurrentViewWhileRefreshingRows()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.PlcControl.cs"));
            int start = source.IndexOf("private async Task HandleMixedEngraveCutStartAsync", StringComparison.Ordinal);
            int end = source.IndexOf("private async Task<bool> SetMixedLaserPowerAsync", start, StringComparison.Ordinal);
            AssertTrue(start >= 0 && end > start, "The mixed engrave/cut RUN handler must exist.");

            string handler = source.Substring(start, end - start);
            AssertTrue(handler.Contains("await EnsureCadProgramCurrentAsync();"),
                "Mixed DXF RUN must wait for the latest requested CAD program.");
            AssertTrue(!handler.Contains("currentView ="),
                "Ensuring current PLC rows during RUN must not change the operator's current view.");
            AssertTrue(!handler.Contains("await PushDxfStateAsync();"),
                "RUN must not rebuild the removed Process Table UI.");
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

        private static void CadPathSelectionDoesNotReverseSourceCoordinates()
        {
            var first = NewCadLine(0, 0, 10, 0);
            var reversedCandidate = NewCadLine(20, 0, 10, 0);

            var paths = CadPathSelection.GroupConnectedPaths(
                new List<CadDocumentService.CadPrimitiveData> { first, reversedCandidate });

            AssertEqual("10", paths[0][1].Points[0].X.ToString(CultureInfo.InvariantCulture),
                "The process path should expose the reversed orientation.");
            AssertEqual("20", reversedCandidate.Points[0].X.ToString(CultureInfo.InvariantCulture),
                "Source primitive coordinates must not be reversed in place.");
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

        private static void CadPathSelectionUpdatesImmediatelyAndExplainsRunLock()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string stateSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "WpfUiState.cs"));
            string handler = ExtractMethodBody(source, "private async Task HandleToggleCadPathAsync");
            AssertTrue(stateSource.Contains("public void UpdateCadPathStroke(int pathId, bool isCut)"), "The UI state must update one selected CAD path without rebuilding the canvas.");
            AssertTrue(handler.Contains("cadProgramCompilationState.MarkDirty()"), "Each CAD tap must request a new program version.");
            AssertTrue(handler.Contains("ScheduleCadProgramCompilation(selectedDocument"), "Each CAD tap must schedule only the latest deferred compile.");
            AssertTrue(!handler.Contains("await PublishAllMqttAsync();"), "A path tap must not wait for MQTT publication.");
            AssertTrue(source.Contains("private void ScheduleCadProgramCompilation"),
                "The immediate CAD path toggle must schedule latest-wins compilation.");
            AssertTrue(!handler.Contains("cadLoadGate.WaitAsync"), "A CAD tap must not wait for the import gate before changing its local color.");
            AssertTrue(handler.Contains("Stop the active program before changing cut paths."), "A running program must explain why cut-path selection is locked.");
            AssertTrue(handler.Contains("programCommandGate.CurrentCount == 0"),
                "CAD path selection must be rejected while RUN or Test Area owns the program command gate.");
            AssertTrue(!handler.Contains("await RebuildMixedEngraveCutProgramAsync"),
                "A CAD tap must not await the expensive PLC row rebuild.");
            AssertTrue(!handler.Contains("PushDxfStateAsync"),
                "A CAD tap must not rebuild the removed Process Table UI.");
        }

        private static void CadPathHitIndexFindsNearestHorizontalSegment()
        {
            var pathPoints = new[]
            {
                new System.Windows.Point(0, 0),
                new System.Windows.Point(100, 0)
            };
            var index = CadPathHitIndex.Build(
                new[] { new CadHitPath(7, pathPoints) },
                10);

            int pathId;
            AssertTrue(index.TryFindNearest(new System.Windows.Point(40, 4), 5, out pathId),
                "spatial index should find a segment within the hit radius");
            AssertEqual("7", pathId.ToString(CultureInfo.InvariantCulture),
                "spatial index should return the nearest path id");

            IReadOnlyList<System.Windows.Point> returnedPoints;
            AssertTrue(index.TryGetPathPoints(7, out returnedPoints),
                "spatial index should return points for the selected path");
            AssertEqual("2", returnedPoints.Count.ToString(CultureInfo.InvariantCulture),
                "selected path should retain its projected points");
        }

        private static void CadPathHitIndexRejectsMissOutsideRadius()
        {
            var index = CadPathHitIndex.Build(
                new[]
                {
                    new CadHitPath(1, new[]
                    {
                        new System.Windows.Point(0, 0),
                        new System.Windows.Point(100, 0)
                    })
                },
                10);

            int pathId;
            AssertTrue(!index.TryFindNearest(new System.Windows.Point(40, 5.1), 5, out pathId),
                "spatial index should reject a point outside the hit radius");
        }

        private static void CadPathHitIndexUsesPathIdForDeterministicTies()
        {
            var index = CadPathHitIndex.Build(
                new[]
                {
                    new CadHitPath(20, new[]
                    {
                        new System.Windows.Point(0, 0),
                        new System.Windows.Point(100, 0)
                    }),
                    new CadHitPath(3, new[]
                    {
                        new System.Windows.Point(0, 10),
                        new System.Windows.Point(100, 10)
                    })
                },
                10);

            int pathId;
            AssertTrue(index.TryFindNearest(new System.Windows.Point(40, 5), 5, out pathId),
                "spatial index should accept an exact tie at the hit radius");
            AssertEqual("3", pathId.ToString(CultureInfo.InvariantCulture),
                "equal-distance paths should be resolved by the smaller path id");
        }

        private static void CadPathHitIndexKeepsTheTrulyNearestPathDespiteTinyDistanceDifference()
        {
            var index = CadPathHitIndex.Build(
                new[]
                {
                    new CadHitPath(1, new[]
                    {
                        new System.Windows.Point(0, 0.0000010),
                        new System.Windows.Point(100, 0.0000010)
                    }),
                    new CadHitPath(2, new[]
                    {
                        new System.Windows.Point(0, 0.0000009),
                        new System.Windows.Point(100, 0.0000009)
                    })
                },
                10);

            int pathId;
            AssertTrue(index.TryFindNearest(new System.Windows.Point(40, 0), 0.000002, out pathId),
                "spatial index should accept both nearly coincident paths within the hit radius");
            AssertEqual("2", pathId.ToString(CultureInfo.InvariantCulture),
                "a tiny but real distance difference must beat the path-id tie-breaker");
        }

        private static void CadPathHitIndexHandlesIntMaxValueGridBoundary()
        {
            const double boundary = 2147483647.0;
            var buildTask = Task.Run(() => CadPathHitIndex.Build(
                new[]
                {
                    new CadHitPath(42, new[]
                    {
                        new System.Windows.Point(boundary, boundary),
                        new System.Windows.Point(boundary, boundary)
                    })
                },
                1));

            AssertTrue(buildTask.Wait(TimeSpan.FromSeconds(1)),
                "spatial index build must terminate at the int.MaxValue grid boundary");

            CadPathHitIndex index = buildTask.Result;
            int pathId;
            AssertTrue(index.TryFindNearest(new System.Windows.Point(boundary, boundary), 0, out pathId),
                "spatial index should query a path stored in the int.MaxValue grid cell");
            AssertEqual("42", pathId.ToString(CultureInfo.InvariantCulture),
                "the int.MaxValue boundary query should return the stored path");
        }

        private static void CadPathHitIndexFindsOnlyTheNearbyPathInALargeSet()
        {
            const int pathCount = 5000;
            var paths = new List<CadHitPath>(pathCount);
            for (int i = 0; i < pathCount; i++)
            {
                double x = i * 100;
                paths.Add(new CadHitPath(i, new[]
                {
                    new System.Windows.Point(x, 0),
                    new System.Windows.Point(x + 50, 0)
                }));
            }

            var index = CadPathHitIndex.Build(paths, 16);
            int pathId;
            AssertTrue(index.TryFindNearest(new System.Windows.Point(4321 * 100 + 25, 2), 3, out pathId),
                "spatial index should find the nearby path in a large path set");
            AssertEqual("4321", pathId.ToString(CultureInfo.InvariantCulture),
                "spatial index should not return a distant path from a large set");
        }

        private static void CadInteractionAvoidsExpensiveHitTestingAndFullStateRebuild()
        {
            string viewSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
            string codeSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml.cs"));
            string handlerSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string stateSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "WpfUiState.cs"));
            string publisherSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.StatePublisher.cs"));

            AssertTrue(!viewSource.Contains("CadSelectionLayer"), "DXF view must remove the per-path selection ItemsControl.");
            AssertTrue(!viewSource.Contains("SelectableCadPath_MouseLeftButtonDown"), "DXF view must not attach per-path mouse handlers.");
            AssertTrue(viewSource.Contains("x:Name=\"CadPreviewViewbox\""), "DXF view must name the Viewbox for screen-DIP hit radius conversion.");
            AssertTrue(viewSource.Contains("Data=\"{Binding CadSelectionOverlayGeometry}\""), "DXF view must render the one temporary selection overlay.");
            AssertTrue(viewSource.Contains("Stroke=\"{Binding CadSelectionOverlayStroke}\""), "DXF selection overlay must use the immediate selection stroke.");

            AssertTrue(codeSource.Contains("TryFindNearest"), "CAD taps must use the spatial hit index.");
            AssertTrue(codeSource.Contains("e.GetPosition(CadContent)"), "Mouse selection must query content coordinates directly.");
            AssertTrue(codeSource.Contains("GetTouchPoint(CadContent)"), "Touch selection must query content coordinates directly.");
            AssertTrue(codeSource.Contains("12.0 / Math.Max(GetCadViewboxScale() * cadZoom"), "CAD hit radius must remain about 12 screen DIPs as zoom changes.");
            AssertTrue(codeSource.Contains("Distance(cadPanStartPoint, current) >= TouchPanThreshold"), "Small mouse movement must remain a tap instead of panning before selection.");
            AssertTrue(codeSource.Contains("private bool mousePanExceededThreshold;"), "CAD mouse gestures must retain whether the pan threshold was crossed.");
            AssertTrue(codeSource.Contains("private bool touchPanExceededThreshold;"), "CAD touch gestures must retain whether the pan threshold was crossed.");
            AssertTrue(codeSource.Contains("mousePanExceededThreshold = false;"), "A new mouse gesture must reset its sticky pan state.");
            AssertTrue(codeSource.Contains("mousePanExceededThreshold = true;"), "A mouse drag crossing the threshold must become a pan permanently for that gesture.");
            AssertTrue(codeSource.Contains("&& !mousePanExceededThreshold"), "Mouse release must not select after a drag has become a pan.");
            AssertTrue(codeSource.Contains("touchPanExceededThreshold = false;"), "A new or reset touch gesture must clear its sticky pan state.");
            AssertTrue(codeSource.Contains("touchPanExceededThreshold = true;"), "A touch drag crossing the threshold must become a pan permanently for that gesture.");
            AssertTrue(codeSource.Contains("!touchPanExceededThreshold && Distance(touchStartPoint, position) < TouchPanThreshold"), "Touch release must not select after a drag has become a pan.");
            AssertTrue(!codeSource.Contains("OriginalSource"), "CAD selection must not walk the visual tree.");
            AssertTrue(!codeSource.Contains("FindCadPrimitive"), "CAD selection must not discover a path through WPF elements.");
            AssertTrue(!codeSource.Contains("SelectableCadPath_MouseLeftButtonDown"), "CAD selection must not keep the old per-path handler.");
            AssertTrue(!codeSource.Contains("SetCadSelectionHitTesting"), "CAD interaction must not manage a hidden per-path hit-test layer.");

            AssertTrue(stateSource.Contains("public CadPathHitIndex CadPathHitIndex"), "UI state must expose the immutable CAD hit index.");
            AssertTrue(stateSource.Contains("CadSelectionOverlayGeometry"), "UI state must expose one selection overlay geometry.");
            AssertTrue(stateSource.Contains("CadSelectionOverlayStroke"), "UI state must expose the selection overlay stroke.");
            AssertTrue(stateSource.Contains("TryGetPathPoints"), "Immediate selection feedback must read one path from the spatial index.");
            AssertTrue(publisherSource.Contains("ClearCadSelectionOverlay"), "Refresh of combined CAD geometry must clear the temporary selection overlay.");

            string refresh = ExtractMethodBody(
                handlerSource,
                "private async Task CompileCadProgramAsync");
            AssertTrue(refresh.Contains("RefreshCadSelectionPreviewAsync"), "Path selection must use a lightweight preview refresh.");
            AssertTrue(!refresh.Contains("await PushDxfStateAsync();"), "Selecting a path must not rebuild the complete UI state.");
        }

        private static void LargeRingBufferRunsPlcIoOutsideUiThread()
        {
            string runner = File.ReadAllText(GetRepositoryPath(
                "src", "DACDT_2026.App", "QD75RingBufferRunner.cs"));
            string handler = File.ReadAllText(GetRepositoryPath(
                "src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string monitor = ExtractMethodBody(
                runner, "private async Task MonitorMd44AndRefillAsync");
            string send = ExtractMethodBody(
                handler, "private async Task<bool> HandleSendCadXAsync");

            AssertTrue(runner.Contains(
                    "await Task.Run(() => LoadInitialBuffer(), cts.Token).ConfigureAwait(false);"),
                "Ring initial PLC writes must run outside the UI thread.");
            AssertTrue(runner.Contains("_ = Task.Run(() => MonitorAndFinalizeAsync());"),
                "Ring monitoring and refill must be launched on the thread pool.");
            AssertTrue(monitor.Contains(
                    "await Task.Delay(PollIntervalMs, ct).ConfigureAwait(false);"),
                "Ring polling continuations must not capture the WPF UI context.");

            int awaitReady = send.IndexOf(
                "bool ringReady = await ringRunner.StartAsync();",
                StringComparison.Ordinal);
            int rejectFailure = send.IndexOf("if (!ringReady)", awaitReady, StringComparison.Ordinal);
            int enableRun = send.IndexOf(
                "ui.IsStartActionEnabled = true;",
                Math.Max(0, awaitReady),
                StringComparison.Ordinal);
            AssertTrue(awaitReady >= 0 && rejectFailure > awaitReady,
                "The send workflow must await ring initialisation and reject failure.");
            AssertTrue(enableRun > rejectFailure,
                "RUN must only be enabled after ring initialisation succeeds.");
        }

        private static void ProgramMonitorAutoScrollIsLatestOnlyAndThrottled()
        {
            string[] files =
            {
                "DashboardView.xaml.cs",
                "MonitorView.xaml.cs",
                "DxfRunView.xaml.cs"
            };
            foreach (string file in files)
            {
                string source = File.ReadAllText(GetRepositoryPath(
                    "src", "DACDT_2026.App", "Views", file));
                string propertyChanged = ExtractMethodBody(
                    source, "private void ObservedState_PropertyChanged");
                string rowsChanged = ExtractMethodBody(
                    source, "private void ProgramRows_CollectionChanged");

                AssertTrue(source.Contains("DispatcherTimer activeProgramScrollTimer"),
                    file + " must use one reusable auto-scroll timer.");
                AssertTrue(source.Contains("TimeSpan.FromMilliseconds(100)"),
                    file + " must limit auto-scroll work to 10 updates per second.");
                AssertTrue(propertyChanged.Contains("QueueActiveProgramScroll();")
                    && rowsChanged.Contains("QueueActiveProgramScroll();"),
                    file + " must coalesce active-row and row-window changes.");
                AssertTrue(source.Contains("activeProgramScrollTimer.Stop();")
                    && source.Contains("activeProgramScrollPending = false;"),
                    file + " must consume only the latest pending scroll.");
                AssertTrue(!source.Contains(
                        "Dispatcher.BeginInvoke(new Action(() => ProgramGrid.ScrollIntoView(activeRow)))"),
                    file + " must not queue one Dispatcher operation per active row.");
            }
        }

        private static void CadInteractionUsesTemporaryBitmapCacheOnlyWhileInteracting()
        {
            string codeSource = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml.cs"));
            string wheelHandler = ExtractMethodBody(codeSource, "private void CadViewport_PreviewMouseWheel");
            string touchDownHandler = ExtractMethodBody(codeSource, "private void CadViewport_PreviewTouchDown");
            string touchMoveHandler = ExtractMethodBody(codeSource, "private void CadViewport_PreviewTouchMove");
            string touchUpHandler = ExtractMethodBody(codeSource, "private void CadViewport_PreviewTouchUp");
            string mouseDownHandler = ExtractMethodBody(codeSource, "private void CadViewport_MouseLeftButtonDown");
            string mouseMoveHandler = ExtractMethodBody(codeSource, "private void CadViewport_MouseMove");
            string mouseUpHandler = ExtractMethodBody(codeSource, "private void CadViewport_MouseLeftButtonUp");
            string endPanHandler = ExtractMethodBody(codeSource, "private void EndCadPan");
            string resetTouchHandler = ExtractMethodBody(codeSource, "private void ResetTouchGesture");
            string resetViewHandler = ExtractMethodBody(codeSource, "private void ResetCadView");
            string wheelTickHandler = ExtractMethodBody(codeSource, "private void CadWheelIdleTimer_Tick");
            string lostMouseHandler = ExtractMethodBody(codeSource, "private void CadViewport_LostMouseCapture");
            string lostTouchHandler = ExtractMethodBody(codeSource, "private void CadViewport_LostTouchCapture");
            string cancelWheelHandler = ExtractMethodBody(codeSource, "private void CancelPendingWheelInteraction");

            AssertTrue(codeSource.Contains("private readonly BitmapCache cadInteractionCache"),
                "CAD interaction must reuse one BitmapCache instance.");
            AssertTrue(codeSource.Contains("EnableClearType = false"),
                "CAD interaction cache must disable ClearType for fast bitmap rendering.");
            AssertTrue(codeSource.Contains("RenderAtScale = 1"),
                "CAD interaction cache must render at a stable scale.");
            AssertTrue(codeSource.Contains("private void BeginCadInteractionRendering()"),
                "CAD interaction must have an explicit cache-start lifecycle method.");
            AssertTrue(codeSource.Contains("CadContent.CacheMode = cadInteractionCache"),
                "CAD interaction start must cache only CadContent.");
            AssertTrue(codeSource.Contains("private void EndCadInteractionRendering()"),
                "CAD interaction must have an explicit cache-end lifecycle method.");
            AssertTrue(codeSource.Contains("CadContent.CacheMode = null"),
                "CAD interaction end must restore sharp vector rendering.");
            AssertTrue(codeSource.Contains("private readonly DispatcherTimer cadWheelIdleTimer"),
                "Mouse-wheel CAD zoom must use one reusable idle timer.");
            AssertTrue(codeSource.Contains("Interval = TimeSpan.FromMilliseconds(150)"),
                "Mouse-wheel CAD zoom must restore vectors after a short idle interval.");
            AssertTrue(codeSource.Contains("cadWheelIdleTimer.Stop();")
                && codeSource.Contains("cadWheelIdleTimer.Start();"),
                "Each mouse-wheel event must restart the reusable idle timer.");
            AssertTrue(codeSource.Contains("BeginCadInteractionRendering();")
                && codeSource.Contains("EndCadInteractionRendering();"),
                "CAD pan, pinch, wheel, reset, and capture-loss paths must use cache lifecycle methods.");

            AssertTrue(touchDownHandler.Contains("EndCadPan();")
                && touchDownHandler.IndexOf("EndCadPan();", StringComparison.Ordinal)
                    < touchDownHandler.IndexOf("touchSession.BeginTouch", StringComparison.Ordinal),
                "Valid touch down must cancel an active mouse pan before touch state starts.");
            int mouseGuard = touchDownHandler.IndexOf("if (isCadPanning || CadViewport.IsMouseCaptured)", StringComparison.Ordinal);
            int guardedEndPan = touchDownHandler.IndexOf("EndCadPan();", StringComparison.Ordinal);
            AssertTrue(mouseGuard >= 0 && guardedEndPan > mouseGuard,
                "A third touch during pinch must not call EndCadPan unconditionally.");
            AssertTrue(endPanHandler.Contains("if (!isCadPanning && !CadViewport.IsMouseCaptured)")
                && endPanHandler.Contains("return;"),
                "EndCadPan must be a no-op when no mouse pan or capture is active.");
            AssertTrue(!touchDownHandler.Contains("BeginCadInteractionRendering();"),
                "A simple touch press must not enable the bitmap cache.");
            AssertTrue(!mouseDownHandler.Contains("BeginCadInteractionRendering();"),
                "A simple mouse press must not enable the bitmap cache.");
            AssertTrue(touchDownHandler.Contains("if (!touchSession.IsTouchActive)")
                && touchDownHandler.Contains("CancelPendingWheelInteraction();"),
                "Only the first touch must cancel pending wheel rendering.");
            AssertTrue(mouseDownHandler.Contains("CancelPendingWheelInteraction();"),
                "The first mouse press must cancel pending wheel rendering.");
            AssertTrue(cancelWheelHandler.Contains("cadWheelIdleTimer.Stop();")
                && cancelWheelHandler.Contains("EndCadInteractionRendering();"),
                "Cancelling pending wheel rendering must stop its timer and remove its cache.");

            int mouseThreshold = mouseMoveHandler.IndexOf("mousePanExceededThreshold = true;", StringComparison.Ordinal);
            int mouseBegin = mouseMoveHandler.IndexOf("BeginCadInteractionRendering();", StringComparison.Ordinal);
            AssertTrue(mouseThreshold >= 0 && mouseBegin > mouseThreshold
                && CountOccurrences(mouseMoveHandler, "BeginCadInteractionRendering();") == 1,
                "Mouse pan must enable the cache once at threshold transition.");

            int touchThreshold = touchMoveHandler.IndexOf("touchPanExceededThreshold = true;", StringComparison.Ordinal);
            int touchBegin = touchMoveHandler.IndexOf("BeginCadInteractionRendering();", StringComparison.Ordinal);
            AssertTrue(touchThreshold >= 0 && touchBegin > touchThreshold
                && CountOccurrences(touchMoveHandler, "BeginCadInteractionRendering();") == 1,
                "Touch pan must enable the cache once at threshold transition.");

            AssertTrue(wheelHandler.Contains("isCadPanning")
                && wheelHandler.Contains("touchSession.IsTouchActive"),
                "Mouse wheel must reject active mouse pan and touch gestures.");
            AssertTrue(wheelHandler.Contains("cadWheelIdleTimer.Stop();")
                && wheelHandler.Contains("cadWheelIdleTimer.Start();"),
                "Each accepted mouse-wheel event must restart the reusable idle timer.");
            AssertTrue(wheelTickHandler.Contains("isCadPanning")
                && wheelTickHandler.Contains("touchSession.IsTouchActive")
                && wheelTickHandler.Contains("return;"),
                "Wheel idle expiry must not clear the cache while pan or touch is active.");
            int tickStop = wheelTickHandler.IndexOf("cadWheelIdleTimer.Stop();", StringComparison.Ordinal);
            int tickGuard = wheelTickHandler.IndexOf("if (isCadPanning || touchSession.IsTouchActive)", StringComparison.Ordinal);
            AssertTrue(tickStop >= 0 && tickGuard > tickStop,
                "Wheel idle expiry must stop the timer before an active gesture guard returns.");

            AssertTrue(mouseUpHandler.Contains("EndCadPan();") && endPanHandler.Contains("EndCadInteractionRendering();"),
                "Mouse release must end CAD interaction rendering.");
            AssertTrue(touchUpHandler.Contains("ResetTouchGesture();") && resetTouchHandler.Contains("EndCadInteractionRendering();"),
                "Touch completion must end CAD interaction rendering.");
            AssertTrue(resetViewHandler.Contains("EndCadInteractionRendering();"),
                "Double-click/reset must end CAD interaction rendering.");
            AssertTrue(lostMouseHandler.Contains("EndCadPan();")
                && lostMouseHandler.Contains("EndCadInteractionRendering();"),
                "Mouse capture loss must end CAD interaction rendering.");
            AssertTrue(lostTouchHandler.Contains("ResetTouchGesture();"),
                "Touch capture loss must reset and end CAD interaction rendering.");
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
                "Machine Config &amp; Workspace",
                "G-code Motion",
                "Rapid Travel Speed (G00) (mm/min)",
                "G54-G59 WCS Offsets"
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
            AssertTrue(formSource.Contains("DACDT_2026_settings.txt"), "Save Settings must use the portable TXT configuration file.");
            AssertTrue(formSource.Contains("ConfigurationSelectionStatePath"), "The selected configuration path must be remembered separately.");
            AssertTrue(!settingsView.Contains("Import Settings"), "Settings must not add a separate import workflow.");
            AssertTrue(!settingsView.Contains("Export Settings"), "Settings must not add a separate export workflow.");
        }

        private static void ViewsExposeSharedStylesToXamlDesigner()
        {
            string viewsRoot = GetRepositoryPath("src", "DACDT_2026.App", "Views");
            string[] viewFiles = Directory.GetFiles(viewsRoot, "*.xaml", SearchOption.TopDirectoryOnly)
                .Where(file => !string.Equals(Path.GetFileName(file), "Styles.xaml", StringComparison.OrdinalIgnoreCase))
                .Concat(Directory.GetFiles(Path.Combine(viewsRoot, "Panels"), "*.xaml", SearchOption.TopDirectoryOnly))
                .ToArray();

            foreach (string file in viewFiles)
            {
                string source = File.ReadAllText(file);
                AssertTrue(source.Contains("<UserControl.Resources>"), Path.GetFileName(file) + " must define local resources for standalone XAML design.");
                AssertTrue(source.Contains("Source=\"/DACDT_2026;component/Views/Styles.xaml\""), Path.GetFileName(file) + " must merge the shared Styles.xaml dictionary.");
            }
        }

        private static void ViewsDeclareConvertersUsedByXamlDesigner()
        {
            string viewsRoot = GetRepositoryPath("src", "DACDT_2026.App", "Views");
            foreach (string file in Directory.GetFiles(viewsRoot, "*.xaml", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(file);
                if (source.Contains("{StaticResource BoolToVisibilityConverter}"))
                    AssertTrue(source.Contains("<local:BoolToVisibilityConverter x:Key=\"BoolToVisibilityConverter\"/>"), Path.GetFileName(file) + " must declare BoolToVisibilityConverter for standalone XAML design.");

                if (source.Contains("{StaticResource BoolToStatusBrushConverter}"))
                    AssertTrue(source.Contains("<local:BoolToStatusBrushConverter x:Key=\"BoolToStatusBrushConverter\"/>"), Path.GetFileName(file) + " must declare BoolToStatusBrushConverter for standalone XAML design.");
            }
        }

        private static void WpfXamlUsesValidResourceAndGridSyntax()
        {
            string sidebar = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "Panels", "SidebarControl.xaml"));
            string dxfRun = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));

            AssertTrue(!sidebar.Contains("<Grid.ColumnDefinition "), "Sidebar must use ColumnDefinition children inside Grid.ColumnDefinitions.");
            string normalizedDxfRun = dxfRun.Replace("\r\n", "\n");
            AssertTrue(normalizedDxfRun.Contains("<UserControl.Resources>\n        <ResourceDictionary>\n            <ResourceDictionary.MergedDictionaries>"), "DxfRunView resources must wrap merged dictionaries in ResourceDictionary.");
        }

        private static void CadPreviewClipsOnlyAtOuterViewport()
        {
            string xaml = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
            int viewportStart = xaml.IndexOf("<Border x:Name=\"CadViewport\"", StringComparison.Ordinal);
            int viewportEnd = xaml.IndexOf('>', viewportStart);
            int viewboxStart = xaml.IndexOf("<Viewbox x:Name=\"CadPreviewViewbox\"", StringComparison.Ordinal);
            int viewboxEnd = xaml.IndexOf('>', viewboxStart);
            int surfaceStart = xaml.IndexOf("<Canvas x:Name=\"CadSurface\"", StringComparison.Ordinal);
            int surfaceEnd = xaml.IndexOf('>', surfaceStart);

            AssertTrue(viewportStart >= 0 && viewportEnd > viewportStart,
                "CAD preview must declare its outer viewport.");
            AssertTrue(xaml.Substring(viewportStart, viewportEnd - viewportStart).Contains("ClipToBounds=\"True\""),
                "CAD preview must clip at its outer viewport.");
            AssertTrue(viewboxStart >= 0 && viewboxEnd > viewboxStart
                && xaml.Substring(viewboxStart, viewboxEnd - viewboxStart).Contains("ClipToBounds=\"False\""),
                "CAD Viewbox must allow panned content to render into letterbox space.");
            AssertTrue(surfaceStart >= 0 && surfaceEnd > surfaceStart
                && xaml.Substring(surfaceStart, surfaceEnd - surfaceStart).Contains("ClipToBounds=\"False\""),
                "CAD surface must not clip content before it reaches the outer viewport.");
        }

        private static void DxfRunViewShowsVirtualizedPointMonitor()
        {
            string xaml = File.ReadAllText(GetRepositoryPath(
                "src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
            string code = File.ReadAllText(GetRepositoryPath(
                "src", "DACDT_2026.App", "Views", "DxfRunView.xaml.cs"));

            AssertTrue(xaml.Contains("Text=\"DXF Point Monitor\""),
                "The DXF tab must show the DXF Point Monitor title.");
            AssertTrue(xaml.Contains("ItemsSource=\"{Binding ProgramRows}\""),
                "The point monitor must reuse the existing ProgramRows window.");
            AssertTrue(xaml.Contains("Header=\"DXF Point\"")
                && xaml.Contains("Binding=\"{Binding MotionType}\""),
                "The table must expose the DXF Point column.");
            AssertTrue(xaml.Contains("Header=\"End X;Y\"")
                && xaml.Contains("Binding=\"{Binding EndCoordinate}\""),
                "The table must expose the endpoint column.");
            AssertTrue(xaml.Contains("EnableRowVirtualization=\"True\"")
                && xaml.Contains("EnableColumnVirtualization=\"True\"")
                && xaml.Contains("ScrollViewer.CanContentScroll=\"True\""),
                "The DXF point table must virtualize rows and columns.");
            AssertTrue(xaml.Contains("ScrollViewer.ScrollChanged=\"ProgramGrid_ScrollChanged\""),
                "The point table must lazy-load its existing row window.");
            AssertTrue(!xaml.Contains("G-code Editor")
                && !xaml.Contains("PreviewGcodeCommand")
                && !xaml.Contains("SaveGcodeCommand"),
                "The old editor and editor actions must be removed.");
            AssertTrue(code.Contains("DispatcherTimer activeProgramScrollTimer")
                && code.Contains("TimeSpan.FromMilliseconds(100)"),
                "The DXF tab must coalesce auto-scroll requests at 10 Hz.");
        }

        private static void DxfOnlyViewsRemoveGcodeAndWcsControls()
        {
            string appRoot = GetRepositoryPath("src", "DACDT_2026.App");
            string dxf = File.ReadAllText(Path.Combine(appRoot, "Views", "DxfRunView.xaml"));
            string settings = File.ReadAllText(Path.Combine(appRoot, "Views", "SettingsView.xaml"));
            string sidebar = File.ReadAllText(Path.Combine(appRoot, "Views", "Panels", "SidebarControl.xaml"));
            string dashboard = File.ReadAllText(Path.Combine(appRoot, "Views", "DashboardView.xaml"));
            string monitor = File.ReadAllText(Path.Combine(appRoot, "Views", "MonitorView.xaml"));
            string help = File.ReadAllText(Path.Combine(appRoot, "Views", "HelpView.xaml"));

            AssertTrue(!dxf.Contains("New Gcode"), "The DXF toolbar must not create G-code.");
            AssertTrue(sidebar.Contains("Content=\"DXF Run\"")
                && !sidebar.Contains("DXF / GCODE Run"),
                "Navigation must expose a DXF-only route label.");
            AssertTrue(!settings.Contains("G-code Motion")
                && !settings.Contains("G54-G59")
                && !settings.Contains("WcsGrid"),
                "Settings must not expose G-code or WCS controls.");
            AssertTrue(dashboard.Contains("Header=\"DXF Point\"")
                && monitor.Contains("Header=\"DXF Point\""),
                "All program tables must use the DXF-only column label.");
            AssertTrue(help.Contains("Mở và kiểm tra file DXF")
                && !help.Contains("G-code")
                && !help.Contains("GCODE")
                && !help.Contains("WCS"),
                "The Vietnamese guide must describe DXF-only operation.");
        }

        private static void DxfRuntimeHasNoGcodeEntryPoints()
        {
            string form = File.ReadAllText(GetRepositoryPath(
                "src", "DACDT_2026.App", "Form1.cs"));
            string handler = File.ReadAllText(GetRepositoryPath(
                "src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string state = File.ReadAllText(GetRepositoryPath(
                "src", "DACDT_2026.App", "WpfUiState.cs"));

            string[] removedMembers =
            {
                "NewGcodeCommand",
                "SaveGcodeCommand",
                "PreviewGcodeCommand",
                "ApplyGcodeSettingsCommand",
                "HandlePreviewGcodeAsync",
                "HandleNewGcodeAsync",
                "HandleSaveGcodeAsync",
                "ShowSaveGcodeDialog",
                "IsGcodeFile",
                "BuildGcodeProcessRows",
                "UpdateGcodeFromProcessTable",
                "HandleOpenDxfAsync",
                "ShowOpenFileDialog"
            };

            foreach (string member in removedMembers)
            {
                AssertTrue(!form.Contains(member)
                    && !handler.Contains(member)
                    && !state.Contains(member),
                    "DXF-only runtime must remove member: " + member);
            }

            AssertTrue(handler.Contains("Filter = \"DXF files (*.dxf)|*.dxf\""),
                "The only open-file filter must accept DXF.");
            AssertTrue(!handler.Contains("*.nc")
                && !handler.Contains("*.ngc")
                && !handler.Contains("*.cnc")
                && !handler.Contains("*.tap"),
                "The runtime must not recognize CNC/G-code extensions.");
        }

        private static void TelemetryFeatureIsRemoved()
        {
            string appRoot = GetRepositoryPath("src", "DACDT_2026.App");
            string sidebar = File.ReadAllText(Path.Combine(appRoot, "Views", "Panels", "SidebarControl.xaml"));
            string rootView = File.ReadAllText(Path.Combine(appRoot, "Form1.xaml"));
            string project = File.ReadAllText(Path.Combine(appRoot, "DACDT_2026.csproj"));

            AssertTrue(!sidebar.Contains("Content=\"Telemetry\""), "Sidebar must not expose the Telemetry navigation button.");
            AssertTrue(!sidebar.Contains("CommandParameter=\"telemetry\""), "Sidebar must not expose the telemetry route.");
            AssertTrue(!rootView.Contains("TelemetryView"), "Root view must not instantiate TelemetryView.");
            AssertTrue(!project.Contains("Views\\TelemetryView.xaml"), "The application project must not compile TelemetryView.xaml.");
            AssertTrue(!File.Exists(Path.Combine(appRoot, "Views", "TelemetryView.xaml")), "TelemetryView.xaml must be removed.");
            AssertTrue(!File.Exists(Path.Combine(appRoot, "Views", "TelemetryView.xaml.cs")), "TelemetryView.xaml.cs must be removed.");
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

        private static void HelpViewContainsVietnameseOperationalGuide()
        {
            string help = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "HelpView.xaml"));
            string[] requiredSections =
            {
                "An toàn trước khi vận hành",
                "Khởi động và kết nối PLC",
                "Jog tay, HOME và RESET",
                "Mở và kiểm tra file DXF",
                "Gửi dữ liệu và chạy chương trình",
                "RUN / PAUSE / CONTINUE / STOP",
                "Camera, Logs và Settings",
                "Thoát app, xử lý lỗi và checklist"
            };

            foreach (string section in requiredSections)
                AssertTrue(help.Contains(section), "Help must contain the Vietnamese operator section: " + section);

            string[] forbiddenContent =
            {
                "Telemetry",
                "TelemetryView",
                "MarkupCompilePass1",
                "WebRtcCameraService",
                "M2000",
                "M210",
                "M213",
                "App không build được",
                "G-code",
                "GCODE",
                "WCS"
            };

            foreach (string phrase in forbiddenContent)
                AssertTrue(!help.Contains(phrase), "Help must not contain developer/internal content: " + phrase);
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

        private static void LargeCadPreviewKeepsFullSourceAndCapsPreviewPoints()
        {
            AssertEqual("1000000", CadPreviewBuilder.DefaultLimits.MaxPreviewPoints.ToString(CultureInfo.InvariantCulture),
                "large CAD preview must support up to 1,000,000 points");
            AssertEqual("100000", CadPreviewBuilder.DefaultLimits.MaxPreviewPrimitives.ToString(CultureInfo.InvariantCulture),
                "DXF preview must retain every primitive allowed by the CAD loader");

            CadDocumentService.CadLoadResult source = NewCadDocumentWithPrimitive(500000);
            CadDocumentService.CadLoadResult preview = CadPreviewBuilder.Build(
                source,
                CadPreviewBuilder.DefaultLimits);

            AssertTrue(source.Primitives.Count == 1, "source primitive count must remain unchanged");
            AssertTrue(source.Primitives[0].Points.Count == 500000, "source CAD data must remain complete");
            AssertTrue(preview.Points.Count == 0,
                "preview must not duplicate hidden coordinate rows");
            AssertTrue(preview.Primitives.Sum(p => p.Points.Count)
                <= CadPreviewBuilder.DefaultLimits.MaxPreviewPoints,
                "preview must be capped");
        }

        private static void LargeCadPreviewSamplesOneHugePolyline()
        {
            CadDocumentService.CadLoadResult source = NewCadDocumentWithPrimitive(500000);
            CadDocumentService.CadLoadResult preview = CadPreviewBuilder.Build(
                source,
                CadPreviewBuilder.DefaultLimits);

            AssertTrue(preview.Primitives.Count == 1, "preview must keep the path as one primitive");
            AssertTrue(preview.Primitives[0].Points.Count >= 2, "preview path must remain drawable");
            AssertTrue(preview.Primitives[0].Points[0].X == source.Primitives[0].Points[0].X,
                "preview must keep the first point");

            int lastPreviewIndex = preview.Primitives[0].Points.Count - 1;
            AssertTrue(preview.Primitives[0].Points[lastPreviewIndex].X
                == source.Primitives[0].Points[source.Primitives[0].Points.Count - 1].X,
                "preview must keep the last point");
        }

        private static void CadDisplayPreviewAppliesOffsetWithoutChangingSource()
        {
            CadDocumentService.CadLoadResult source = NewCadDocumentWithPrimitive(1000);
            double sourceFirstX = source.Primitives[0].Points[0].X;
            double sourceFirstY = source.Primitives[0].Points[0].Y;

            CadDocumentService.CadLoadResult display = CadDisplayDocumentBuilder.Build(
                source,
                isGcodeKind: false,
                dxfOffsetX: 12.5,
                dxfOffsetY: -7.25,
                displayWcsOffsetX: null,
                displayWcsOffsetY: null,
                cancellationToken: CancellationToken.None);

            AssertEqual(
                (sourceFirstX + 12.5).ToString(CultureInfo.InvariantCulture),
                display.Primitives[0].Points[0].X.ToString(CultureInfo.InvariantCulture),
                "display preview must apply the DXF X offset");
            AssertEqual(
                (sourceFirstY - 7.25).ToString(CultureInfo.InvariantCulture),
                display.Primitives[0].Points[0].Y.ToString(CultureInfo.InvariantCulture),
                "display preview must apply the DXF Y offset");
            AssertEqual(
                sourceFirstX.ToString(CultureInfo.InvariantCulture),
                source.Primitives[0].Points[0].X.ToString(CultureInfo.InvariantCulture),
                "display preview must not mutate source CAD coordinates");
            AssertTrue(display.Points.Count == 0,
                "display preview must not recreate hidden coordinate rows");
        }

        private static void CadPreviewSamplesEveryPrimitiveWhenBudgetIsCapped()
        {
            var primitives = new List<CadDocumentService.CadPrimitiveData>();
            for (int primitiveIndex = 0; primitiveIndex < 4; primitiveIndex++)
            {
                var points = new List<CadDocumentService.CadCoordinate>();
                for (int pointIndex = 0; pointIndex < 500; pointIndex++)
                {
                    points.Add(new CadDocumentService.CadCoordinate(
                        primitiveIndex * 1000 + pointIndex,
                        primitiveIndex));
                }

                primitives.Add(new CadDocumentService.CadPrimitiveData
                {
                    SourceType = "Polyline",
                    Points = points
                });
            }

            var source = new CadDocumentService.CadLoadResult
            {
                Bounds = new CadDocumentService.CadBounds(),
                Primitives = primitives,
                Points = new List<CadDocumentService.CadPointData>()
            };
            CadDocumentService.CadLoadResult preview = CadPreviewBuilder.Build(
                source,
                new CadPreviewBuilder.Limits(1000, 10));

            AssertEqual("4", preview.Primitives.Count.ToString(CultureInfo.InvariantCulture),
                "preview budget must sample the whole drawing instead of dropping trailing primitives");
            AssertTrue(preview.Primitives.All(primitive => primitive.Points.Count >= 2),
                "every drawable primitive must remain represented");
            AssertTrue(preview.Primitives.Sum(primitive => primitive.Points.Count) <= 1000,
                "whole-drawing sampling must honor the point budget");
        }

        private static void CadPreviewReservesPointsForTrailingPrimitives()
        {
            var primitives = new List<CadDocumentService.CadPrimitiveData>();
            primitives.Add(NewCadPrimitiveWithPoints(100000, 0));
            primitives.Add(NewCadPrimitiveWithPoints(10, 200000));
            primitives.Add(NewCadPrimitiveWithPoints(10, 300000));
            primitives.Add(NewCadPrimitiveWithPoints(10, 400000));

            var source = new CadDocumentService.CadLoadResult
            {
                Bounds = new CadDocumentService.CadBounds(),
                Primitives = primitives,
                Points = new List<CadDocumentService.CadPointData>()
            };
            CadDocumentService.CadLoadResult preview = CadPreviewBuilder.Build(
                source,
                new CadPreviewBuilder.Limits(1000, 10));

            AssertEqual("4", preview.Primitives.Count.ToString(CultureInfo.InvariantCulture),
                "one huge primitive must not consume the budget reserved for trailing paths");
            AssertTrue(preview.Primitives.All(primitive => primitive.Points.Count >= 2),
                "every retained path must keep at least two drawable points");
        }

        private static void CadPreviewSamplesAcrossPrimitiveCap()
        {
            var primitives = new List<CadDocumentService.CadPrimitiveData>();
            for (int i = 0; i < 6; i++)
                primitives.Add(NewCadPrimitiveWithPoints(2, i * 100));

            var source = new CadDocumentService.CadLoadResult
            {
                Bounds = new CadDocumentService.CadBounds(),
                Primitives = primitives,
                Points = new List<CadDocumentService.CadPointData>()
            };
            CadDocumentService.CadLoadResult preview = CadPreviewBuilder.Build(
                source,
                new CadPreviewBuilder.Limits(100, 3));

            AssertEqual("3", preview.Primitives.Count.ToString(CultureInfo.InvariantCulture),
                "primitive cap must be honored");
            AssertEqual("0", preview.Primitives[0].Points[0].X.ToString(CultureInfo.InvariantCulture),
                "primitive sampling must keep the beginning of the drawing");
            AssertEqual("500", preview.Primitives[preview.Primitives.Count - 1].Points[0].X.ToString(CultureInfo.InvariantCulture),
                "primitive sampling must keep the end of the drawing");
        }

        private static CadDocumentService.CadPrimitiveData NewCadPrimitiveWithPoints(
            int pointCount,
            double startX)
        {
            var points = new List<CadDocumentService.CadCoordinate>(pointCount);
            for (int i = 0; i < pointCount; i++)
                points.Add(new CadDocumentService.CadCoordinate(startX + i, i % 10));

            return new CadDocumentService.CadPrimitiveData
            {
                SourceType = "Polyline",
                Points = points
            };
        }

        private static void CadDisplayPreviewHonorsCancellation()
        {
            var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            bool cancelled = false;
            try
            {
                CadDisplayDocumentBuilder.Build(
                    NewCadDocumentWithPrimitive(1000),
                    isGcodeKind: false,
                    dxfOffsetX: 0,
                    dxfOffsetY: 0,
                    displayWcsOffsetX: null,
                    displayWcsOffsetY: null,
                    cancellationToken: cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            AssertTrue(cancelled, "display preview build must honor cancellation");
        }

        private static void CadOverlaySamplingKeepsEndpointsAndCapsPointCount()
        {
            var points = new List<System.Windows.Point>();
            for (int i = 0; i < 100000; i++)
                points.Add(new System.Windows.Point(i, i % 100));

            IReadOnlyList<System.Windows.Point> sampled =
                CadPathPointSampler.Sample(points, 10000);

            AssertTrue(sampled.Count <= 10000, "selection overlay must cap its point count");
            AssertEqual("0", sampled[0].X.ToString(CultureInfo.InvariantCulture),
                "selection overlay must keep the first point");
            AssertEqual("99999", sampled[sampled.Count - 1].X.ToString(CultureInfo.InvariantCulture),
                "selection overlay must keep the last point");
        }

        private static void LargeCadProcessPathDoesNotCloneSourceCoordinates()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            int start = source.IndexOf("private CadDocumentService.CadLoadResult CreateProcessDocumentForKind", StringComparison.Ordinal);
            int end = source.IndexOf("private static CadDocumentService.CadPrimitiveData CloneCadPrimitiveForProcess", start, StringComparison.Ordinal);
            AssertTrue(start >= 0 && end > start, "large CAD process document path must be present");

            string processPath = source.Substring(start, end - start);
            AssertTrue(processPath.Contains("Points = source.Points"), "process subsets must reuse source point rows");
            AssertTrue(!processPath.Contains("CloneCadPrimitiveForUi"), "process subsets must not deep-clone CAD coordinates");
            AssertTrue(!processPath.Contains("RebuildPointRowsForDisplay"), "process subsets must not rebuild a second full point table");
        }

        private static void LargeCadPreviewAvoidsHiddenCoordinateRowsAndUsesCombinedGeometry()
        {
            string publisher = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.StatePublisher.cs"));
            string dxfRun = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
            string cadLoader = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "CadDocumentService.cs"));

            AssertTrue(!publisher.Contains("CadPointViewModel"), "large CAD publication must not create hidden coordinate view-model rows.");
            AssertTrue(!publisher.Contains("SetCadPointRows("), "large CAD publication must not publish hidden coordinate rows.");
            AssertTrue(!publisher.Contains("BuildCadPreviewImage("), "large CAD publication must not build an unused duplicate preview image.");
            AssertTrue(!publisher.Contains("doc.Points.Select(CloneCadPointForUi)"),
                "CAD display documents must not clone hidden point rows.");
            AssertTrue(!publisher.Contains("displayDoc.Points = RebuildPointRowsForDisplay"),
                "offset preview publication must not rebuild hidden point rows.");
            int displayBuildCount = publisher
                .Split(new[] { "CadDisplayDocumentBuilder.Build(" }, StringSplitOptions.None)
                .Length - 1;
            AssertTrue(displayBuildCount >= 2,
                "initial and selection-refresh previews must share the same offset-aware display builder.");
            AssertTrue(publisher.Contains("CancellationToken cancellationToken = default(CancellationToken)"),
                "combined preview geometry must accept cancellation.");
            AssertTrue(dxfRun.Contains("Data=\"{Binding CadPreviewGeometry}\""), "DXF view must render the combined preview geometry.");
            AssertTrue(dxfRun.Contains("Data=\"{Binding CadEngravePreviewGeometry}\""), "DXF view must render combined engrave geometry.");
            AssertTrue(dxfRun.Contains("Data=\"{Binding CadCutPreviewGeometry}\""), "DXF view must render combined cut geometry.");
            AssertTrue(!publisher.Contains("BuildCadPrimitiveLines"),
                "CAD publication must not create per-path WPF selection view models.");
            AssertTrue(cadLoader.Contains("context.Primitives.Count >= 100000"),
                "CAD loader and preview must share the exact 100,000 primitive limit.");
            AssertTrue(publisher.Contains("BuildCadPathHitIndex"),
                "CAD publication must build the immutable spatial hit index.");
            AssertTrue(publisher.Contains("CadPathHitIndex.Build"),
                "CAD publication must publish spatial hit data instead of visual hit targets.");
            string hitIndexBuilder = ExtractMethodBody(
                publisher,
                "private static CadPathHitIndex BuildCadPathHitIndex");
            AssertTrue(!hitIndexBuilder.Contains(".Take(50000)"),
                "every primitive retained by preview must remain selectable.");
            AssertTrue(!dxfRun.Contains("<Polyline Points="),
                "DXF view must not create one transparent Polyline for every CAD path.");
        }

        private static void DxfRunViewRemovesProcessTableButKeepsPlcProcessData()
        {
            string dxfRun = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml"));
            string dxfRunCode = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Views", "DxfRunView.xaml.cs"));
            string dxfHandler = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string publisher = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.StatePublisher.cs"));
            string state = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "WpfUiState.cs"));

            AssertTrue(!dxfRun.Contains("Process Table"), "DxfRunView must not show the Process Table panel.");
            AssertTrue(!dxfRun.Contains("ProcessTableGrid"), "DxfRunView must not declare the Process Table grid.");
            AssertTrue(!dxfRunCode.Contains("LazyTable_ScrollChanged"), "DxfRunView must not keep the removed table scroll handler.");
            AssertTrue(dxfHandler.Contains("private List<ProcessRow> BuildDxfProcessRows("),
                "DXF process rows must remain available for PLC processing.");
            AssertTrue(dxfHandler.Contains("processRows"), "PLC process data must remain in the application state.");
            AssertTrue(!publisher.Contains("snapRowsSource.Select(CloneProcessRowForUi)"),
                "DXF publication must not clone every PLC row into hidden UI rows.");
            AssertTrue(publisher.Contains("BuildProcessRowViewModelWindow"),
                "Dashboard and Monitor must materialize only the requested process-row window.");
            AssertTrue(!state.Contains("allProcessRows"),
                "WPF state must not retain a second full process-row table.");
            AssertTrue(state.Contains("processRowWindowLoader"),
                "WPF state must load process rows in bounded windows.");
            int viewCheck = publisher.IndexOf(
                "if (!string.Equals(snapCurrentView, \"dxf\"",
                StringComparison.Ordinal);
            int rowSnapshot = publisher.IndexOf(
                "var snapRowsSource = processRows.ToArray()",
                StringComparison.Ordinal);
            AssertTrue(viewCheck >= 0 && rowSnapshot > viewCheck,
                "non-DXF views must return before copying a million PLC rows.");
            AssertTrue(publisher.Contains("Interlocked.Increment(ref dxfStatePushVersion)")
                && publisher.Contains("Volatile.Read(ref dxfStatePushVersion)"),
                "older DXF state builds must not overwrite a newer preview.");
        }

        private static void OfflineRuntimeDoesNotStartMqttOrWebRtc()
        {
            AssertTrue(OfflineRuntimePolicy.Enabled, "offline runtime must be enabled");
            AssertTrue(!OfflineRuntimePolicy.ShouldStartMqtt, "MQTT must not start");
            AssertTrue(!OfflineRuntimePolicy.ShouldStartWebRtc, "WebRTC service must not start");

            string form1 = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string dxf = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string state = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.StatePublisher.cs"));
            string camera = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.Camera.cs"));
            string project = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "DACDT_2026.csproj"));
            string installer = File.ReadAllText(GetRepositoryPath("tools", "installer.iss"));

            AssertTrue(!form1.Contains("await InitMqttAsync();"), "startup must not initialize MQTT");
            AssertTrue(!form1.Contains("new MqttPublishService"), "app must not instantiate MQTT");
            AssertTrue(!form1.Contains("new WebCadUploadSession"), "app must not create web CAD upload state");
            AssertTrue(!form1.Contains("new WebRtcBridgeClient"), "app must not create a WebRTC bridge");
            AssertTrue(!form1.Contains("mqttService.ConnectAsync"), "app must not connect to MQTT");
            AssertTrue(!form1.Contains("mqttService.SubscribeAsync"), "app must not subscribe to MQTT");
            AssertTrue(!form1.Contains("StartBackgroundVideoService();"), "startup must not launch WebRTC web service");
            AssertTrue(!dxf.Contains("await PublishAllMqttAsync();"), "local CAD flow must not publish to MQTT");
            AssertTrue(!state.Contains("private async Task PublishCadStateToMqttAsync"), "CAD MQTT publisher must be removed");
            AssertTrue(!state.Contains("PublishMachineStateToMqttAsync"), "machine MQTT publisher must be removed");
            AssertTrue(!camera.Contains("webRtcBridgeClient.SendFrame"), "local camera must not send WebRTC frames");
            AssertTrue(!camera.Contains("webRtcBridgeClient.Connect"), "local camera must not open a web bridge");
            AssertTrue(!camera.Contains("mqttService.PublishAsync"), "local camera must not publish to MQTT");
            AssertTrue(!project.Contains("MqttPublishService.cs"), "MQTT service must not be compiled into the app");
            AssertTrue(!project.Contains("MQTTnet"), "app must not reference MQTTnet");
            AssertTrue(!project.Contains("Form1.WebCadUpload.cs"), "web CAD upload handlers must not be compiled into the app");
            AssertTrue(!project.Contains("WebRtcBridgeClient.cs"), "WebRTC bridge must not be compiled into the app");
            AssertTrue(!installer.Contains("WebRtcCameraService.exe"), "installer must not package the WebRTC service");
            AssertTrue(!installer.Contains("docs\\index.html"), "installer must not package the web dashboard");
            AssertTrue(installer.Contains("Excludes: \"MQTTnet.dll\""), "installer must not package MQTT runtime DLL");
            AssertTrue(!File.Exists(GetRepositoryPath("src", "WebRtcCameraService", "Program.cs")), "WebRTC service source must be removed");
        }

        private static void CadProgramCompilationStartsAtVersionZero()
        {
            var state = new CadProgramCompilationState();

            AssertTrue(state.RequestedVersion == 0, "compilation requested version must start at zero");
            AssertTrue(state.PublishedVersion == 0, "compilation published version must start at zero");
            AssertTrue(state.IsCurrent(0), "version zero must initially be current");
        }

        private static void CadProgramCompilationMarksDirtyWithoutPublishing()
        {
            var state = new CadProgramCompilationState();

            int requestedVersion = state.MarkDirty();

            AssertTrue(requestedVersion == 1, "first dirty mark must request version one");
            AssertTrue(state.RequestedVersion == requestedVersion, "dirty mark must advance requested version");
            AssertTrue(state.PublishedVersion == 0, "dirty mark must not publish rows");
            AssertTrue(!state.IsCurrent(requestedVersion), "dirty version must not be current before publication");
            AssertTrue(!state.IsCurrent(0), "previous published version must become stale after a dirty mark");
        }

        private static void CadProgramCompilationPublishesCurrentVersion()
        {
            var state = new CadProgramCompilationState();
            int requestedVersion = state.MarkDirty();

            AssertTrue(state.TryPublish(requestedVersion), "current compilation version must publish successfully");
            AssertTrue(state.PublishedVersion == requestedVersion, "published version must match the compiled request");
            AssertTrue(state.IsCurrent(requestedVersion), "published current version must be current");
        }

        private static void CadProgramCompilationRejectsStaleVersionAfterNewerRequest()
        {
            var state = new CadProgramCompilationState();
            int staleVersion = state.MarkDirty();
            int currentVersion = state.MarkDirty();

            AssertTrue(!state.TryPublish(staleVersion), "stale compilation result must be rejected");
            AssertTrue(state.PublishedVersion == 0, "stale result must not advance published version");
            AssertTrue(state.TryPublish(currentVersion), "newest compilation result must publish");
            AssertTrue(state.IsCurrent(currentVersion), "newest published result must be current");
        }

        private static void CadProgramCompilationPreservesPublishedVersionWhenRejecting()
        {
            var state = new CadProgramCompilationState();
            int firstVersion = state.MarkDirty();

            AssertTrue(state.TryPublish(firstVersion), "first compilation version must publish");
            int publishedVersion = state.PublishedVersion;
            int secondVersion = state.MarkDirty();

            AssertTrue(!state.TryPublish(firstVersion), "previously published version must not publish again after a newer request");
            AssertTrue(state.PublishedVersion == publishedVersion, "rejected result must preserve the last published version");
            AssertTrue(!state.IsCurrent(publishedVersion), "last published version must be non-current while a newer request is pending");
            AssertTrue(state.RequestedVersion == secondVersion, "newer request must remain the requested version");
        }

        private static void CadPathGroupingObservesPreCancelledToken()
        {
            var first = NewCadLine(0, 0, 10, 0);
            var second = NewCadLine(10, 0, 20, 0);
            var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var cancellableOverload = typeof(CadPathSelection).GetMethod(
                "GroupConnectedPaths",
                new[]
                {
                    typeof(List<CadDocumentService.CadPrimitiveData>),
                    typeof(bool),
                    typeof(CancellationToken)
                });
            AssertTrue(cancellableOverload != null,
                "connected-path grouping must expose an optional CancellationToken overload");

            bool cancelled = false;
            try
            {
                cancellableOverload.Invoke(
                    null,
                    new object[]
                    {
                        new List<CadDocumentService.CadPrimitiveData> { first, second },
                        false,
                        cancellation.Token
                    });
            }
            catch (System.Reflection.TargetInvocationException ex)
                when (ex.InnerException is OperationCanceledException)
            {
                cancelled = true;
            }
            finally
            {
                cancellation.Dispose();
            }

            AssertTrue(cancelled, "connected-path grouping must stop immediately for an already-cancelled build");

            var defaultPaths = CadPathSelection.GroupConnectedPaths(
                new List<CadDocumentService.CadPrimitiveData> { first, second });
            AssertEqual("1", defaultPaths.Count.ToString(),
                "existing callers that omit the cancellation token must keep their current behavior");
        }

        private static void CadSelectionSchedulesLatestCompilationWithoutAwaitingRows()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string toggle = ExtractMethodBody(source, "private async Task HandleToggleCadPathAsync");

            AssertTrue(toggle.Contains("cadProgramCompilationState.MarkDirty()"),
                "a selected contour must mark PLC rows dirty immediately");
            AssertTrue(toggle.Contains("ScheduleCadProgramCompilation(selectedDocument"),
                "a selected contour must schedule latest-wins background compilation");
            AssertTrue(!toggle.Contains("await RebuildMixedEngraveCutProgramAsync"),
                "selection must not await row rebuilding");
            AssertTrue(!toggle.Contains("await EnsureCadProgramCurrentAsync"),
                "selection must not force an immediate compile");
            AssertTrue(!toggle.Contains("PushDxfStateAsync"),
                "selection must not rebuild the removed Process Table UI");
        }

        private static void CadCompilationUsesExactDebounceAndPublicationGuards()
        {
            string form = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string schedule = ExtractMethodBody(source, "private void ScheduleCadProgramCompilation");
            string start = ExtractMethodBody(source, "private Task StartCadProgramCompilation");
            string runner = ExtractMethodBody(source, "private async Task RunCadProgramCompilationAsync");
            string compile = ExtractMethodBody(source, "private async Task CompileCadProgramAsync");
            string ensure = ExtractMethodBody(source, "private async Task EnsureCadProgramCurrentAsync");
            string rebuild = ExtractMethodBody(source, "private async Task RebuildMixedEngraveCutProgramAsync");

            AssertTrue(form.Contains("private const int CadProgramCompilationDebounceMs = 350;"),
                "selection compilation debounce must be exactly 350 ms");
            AssertTrue(schedule.Contains("delay: true")
                && runner.Contains("Task.Delay(CadProgramCompilationDebounceMs, cancellationToken)"),
                "normal selection compilation must use the exact debounce constant");
            AssertTrue(start.Contains("CancelCadProgramCompilationLocked"),
                "a newer scheduled compile must cancel the older delay or build");
            AssertTrue(form.Contains("cadProgramCompilationPropagatesFailures")
                && start.Contains("cadProgramCompilationPropagatesFailures"),
                "EnsureCurrent must not reuse a fire-and-forget build that swallowed a real compilation failure");

            int documentGuard = compile.IndexOf("ReferenceEquals(activeCadDocument, document)", StringComparison.Ordinal);
            int kindGuard = compile.IndexOf("activeDocumentKind", StringComparison.Ordinal);
            int versionGuard = compile.IndexOf("cadProgramCompilationState.RequestedVersion", StringComparison.Ordinal);
            int publish = compile.IndexOf("cadProgramCompilationState.TryPublish(version)", StringComparison.Ordinal);
            int mutateRows = compile.IndexOf("processRows.Clear()", StringComparison.Ordinal);
            int publishWindow = compile.IndexOf("PublishProcessRowWindowState(", StringComparison.Ordinal);

            AssertTrue(documentGuard >= 0 && kindGuard >= 0 && versionGuard >= 0,
                "publish must guard active document reference, DXF kind, and requested version");
            AssertTrue(publish > documentGuard && publish > kindGuard && publish > versionGuard,
                "all stale-result guards must run before atomic publication");
            AssertTrue(mutateRows >= 0 && mutateRows < publish,
                "processRows must be installed before the version is committed as published");
            AssertTrue(publishWindow > mutateRows && publishWindow < publish,
                "the paged Dashboard/Monitor provider must update before the version is committed");
            AssertTrue(compile.Contains("await RunOnUiAsync"),
                "active documents and processRows must publish on the UI thread");
            AssertTrue(compile.Contains("await RefreshCadSelectionPreviewAsync(document,"),
                "successful publication must refresh only engrave/cut preview geometries");
            AssertTrue(!compile.Contains("PushDxfStateAsync"),
                "successful selection compilation must not rebuild full DXF UI state");

            AssertTrue(ensure.Contains("while (true)"),
                "EnsureCurrent must retry when a newer selection arrives during an awaited build");
            AssertTrue(ensure.Contains("delay: false"),
                "EnsureCurrent must cancel debounce and force immediate compilation");
            AssertTrue(ensure.Contains("cadProgramCompilationState.IsCurrent"),
                "EnsureCurrent may return only for the requested and published version");
            AssertTrue(ensure.Contains("ReferenceEquals(cadProgramPublishedDocument, document)"),
                "EnsureCurrent must require rows published for the same active DXF document");
            AssertTrue(ensure.Contains("if (isClosing)"),
                "an EnsureCurrent loop cancelled by app shutdown must not restart compilation");

            AssertTrue(rebuild.Contains("cadProgramCompilationState.MarkDirty()"),
                "legacy rebuild callers must invalidate their previous rows");
            AssertTrue(rebuild.Contains("await EnsureCadProgramCurrentAsync()"),
                "legacy rebuild callers must use the new immediate safety gate");
        }

        private static void CadCompilationChecksCancellationThroughoutLargeLoops()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string selection = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "CadPathSelection.cs"));

            string create = ExtractMethodBody(source, "private CadDocumentService.CadLoadResult CreateProcessDocumentForKind");
            string mixed = ExtractMethodBody(source, "private MixedEngraveCutBuildResult BuildMixedEngraveCutProgram");
            string buildRows = ExtractMethodBody(source, "private List<ProcessRow> BuildDxfRowsForProcessDocument");
            string processRowsBuilder = ExtractMethodBody(source, "private List<ProcessRow> BuildDxfProcessRows");
            string postProcess = ExtractMethodBody(source, "private List<ProcessRow> PostProcessCompiledRows");
            string applyParameters = ExtractMethodBody(source, "private static void ApplyProcessParameters");
            string normalize = ExtractMethodBody(source, "private static void NormalizeMixedProgramMotionTypes");
            string groupPaths = ExtractMethodBody(selection, "public static List<List<CadDocumentService.CadPrimitiveData>> GroupConnectedPaths");

            AssertTrue(create.Contains("cancellationToken.ThrowIfCancellationRequested()"),
                "primitive filtering must be cancellable");
            AssertTrue(CountOccurrences(mixed, "cancellationToken.ThrowIfCancellationRequested()") >= 3,
                "mixed compilation must check cancellation between major phases");
            AssertTrue(CountOccurrences(buildRows, "cancellationToken.ThrowIfCancellationRequested()") >= 2,
                "DXF row pipeline phases must be cancellable");
            AssertTrue(CountOccurrences(processRowsBuilder, "cancellationToken.ThrowIfCancellationRequested()") >= 5,
                "million-point primitive, point, and post-processing loops must stop stale builds promptly");
            AssertTrue(postProcess.Contains("cancellationToken.ThrowIfCancellationRequested()"),
                "row post-processing must be cancellable");
            AssertTrue(applyParameters.Contains("cancellationToken.ThrowIfCancellationRequested()"),
                "process-parameter loops must be cancellable");
            AssertTrue(normalize.Contains("cancellationToken.ThrowIfCancellationRequested()"),
                "mixed motion normalization must be cancellable");
            AssertTrue(CountOccurrences(groupPaths, "cancellationToken.ThrowIfCancellationRequested()") >= 5,
                "connected-path primitive and candidate loops must stop stale builds promptly");
        }

        private static void CadExecutionConsumersEnsureCurrentRows()
        {
            string dxf = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string plc = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.PlcControl.cs"));
            string mixedRun = ExtractMethodBody(plc, "private async Task HandleMixedEngraveCutStartAsync");
            string send = ExtractMethodBody(dxf, "private async Task<bool> HandleSendCadXAsync");
            string export = ExtractMethodBody(dxf, "private async Task HandleExportQD75Async");

            int mixedEnsure = mixedRun.IndexOf("await EnsureCadProgramCurrentAsync()", StringComparison.Ordinal);
            int mixedSync = mixedRun.IndexOf("bool settingsChanged = SyncEngraveCutSettingsFromUi()", StringComparison.Ordinal);
            int mixedDirty = mixedRun.IndexOf("cadProgramCompilationState.MarkDirty()", StringComparison.Ordinal);
            int mixedRead = mixedRun.IndexOf("processRows.ToList()", StringComparison.Ordinal);
            int sendEnsure = send.IndexOf("await EnsureCadProgramCurrentAsync()", StringComparison.Ordinal);
            int sendRead = send.IndexOf("processRows.Count", StringComparison.Ordinal);
            int exportEnsure = export.IndexOf("await EnsureCadProgramCurrentAsync()", StringComparison.Ordinal);
            int exportRead = export.IndexOf("processRows == null", StringComparison.Ordinal);

            AssertTrue(mixedSync >= 0 && mixedDirty > mixedSync && mixedEnsure > mixedDirty,
                "mixed RUN must dirty compiled rows when live engrave/cut settings changed");
            AssertTrue(mixedEnsure >= 0 && mixedEnsure < mixedRead,
                "mixed RUN must ensure latest CAD rows before taking its PLC snapshot");
            AssertTrue(sendEnsure >= 0 && sendEnsure < sendRead,
                "Send CAD must ensure latest DXF rows before inspecting processRows");
            AssertTrue(exportEnsure >= 0 && exportEnsure < exportRead,
                "QD75 export must ensure latest DXF rows before inspecting processRows");
            AssertTrue(!mixedRun.Contains("await RebuildMixedEngraveCutProgramAsync"),
                "RUN must not mark an already-current program dirty");
            AssertTrue(!mixedRun.Contains("PushDxfStateAsync"),
                "RUN must not trigger a duplicate full DXF refresh");
        }

        private static void TestAreaInvalidatesCadRowsWithoutSchedulingCompilation()
        {
            string source = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string testArea = ExtractMethodBody(source, "private async Task HandleTestEngraveAreaAsync");
            string invalidate = ExtractMethodBody(source, "private void InvalidateCadProgramCompilation");

            int installRows = testArea.IndexOf("processRows.Add(new ProcessRow", StringComparison.Ordinal);
            int invalidateRows = testArea.IndexOf("InvalidateCadProgramCompilation()", StringComparison.Ordinal);
            int publishUi = testArea.IndexOf("await PushDxfStateAsync()", StringComparison.Ordinal);

            AssertTrue(installRows >= 0 && invalidateRows > installRows,
                "Test Area must invalidate CAD compilation immediately after installing temporary rows");
            AssertTrue(publishUi < 0 || invalidateRows < publishUi,
                "temporary Test Area rows must be marked as non-CAD before other awaited work");
            AssertTrue(!testArea.Contains("ScheduleCadProgramCompilation"),
                "Test Area must not start a background CAD build while its rows are running");
            AssertTrue(!testArea.Contains("EnsureCadProgramCurrentAsync"),
                "Test Area must retain its temporary rows until a later CAD consumer requests current rows");
            AssertTrue(invalidate.Contains("cadProgramCompilationState.MarkDirty()")
                && invalidate.Contains("CancelCadProgramCompilation()"),
                "Test Area invalidation must both dirty the CAD version and cancel stale work");
        }

        private static void CadCompilationIsCancelledWhenDocumentClearsOrAppCloses()
        {
            string dxf = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.DxfHandler.cs"));
            string form = File.ReadAllText(GetRepositoryPath("src", "DACDT_2026.App", "Form1.cs"));
            string clear = ExtractMethodBody(dxf, "private void ClearLoadedFileState");
            string close = ExtractMethodBody(form, "protected override void OnClosing");

            AssertTrue(clear.Contains("CancelCadProgramCompilation()"),
                "clearing a loaded file must prevent its stale background result from publishing");
            AssertTrue(close.Contains("CancelCadProgramCompilation()"),
                "closing the app must cancel delayed or active CAD compilation");
        }

        private static CadDocumentService.CadLoadResult NewCadDocumentWithPrimitive(int pointCount)
        {
            var points = new List<CadDocumentService.CadCoordinate>(pointCount);
            for (int i = 0; i < pointCount; i++)
                points.Add(new CadDocumentService.CadCoordinate(i, i % 1000));

            return new CadDocumentService.CadLoadResult
            {
                FileName = "large-test.dxf",
                FilePath = "large-test.dxf",
                Primitives = new List<CadDocumentService.CadPrimitiveData>
                {
                    new CadDocumentService.CadPrimitiveData
                    {
                        SourceType = "Polyline2D",
                        Points = points,
                        ProcessKind = EngraveCutProcessComposer.EngraveKind,
                        PathId = 1
                    }
                },
                Points = points.Select((point, index) => new CadDocumentService.CadPointData
                {
                    Index = index + 1,
                    LineType = "Polyline vertex",
                    X = point.X,
                    Y = point.Y,
                    Z = point.Z,
                    Key = index.ToString()
                }).ToList()
            };
        }

        private static string ExtractMethodBody(string source, string signature)
        {
            int signatureStart = source.IndexOf(signature, StringComparison.Ordinal);
            AssertTrue(signatureStart >= 0, "source contract method is missing: " + signature);

            int openingBrace = source.IndexOf('{', signatureStart);
            AssertTrue(openingBrace >= 0, "source contract method has no body: " + signature);

            int depth = 0;
            for (int index = openingBrace; index < source.Length; index++)
            {
                if (source[index] == '{')
                    depth++;
                else if (source[index] == '}' && --depth == 0)
                    return source.Substring(openingBrace, index - openingBrace + 1);
            }

            throw new Exception("source contract method body is not balanced: " + signature);
        }

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int offset = 0;
            while (offset >= 0 && offset < source.Length)
            {
                int match = source.IndexOf(value, offset, StringComparison.Ordinal);
                if (match < 0)
                    break;

                count++;
                offset = match + value.Length;
            }

            return count;
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
