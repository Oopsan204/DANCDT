using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DACDT_2026
{
    /// <summary>
    /// Main WPF window. The existing PLC, DXF, logging and state logic remains
    /// split across the Form1 partial files; the UI host is WPF/XAML.
    /// </summary>
    public partial class Form1 : Window
    {
        private static int[] MonitorBaseG => QD75BufferWriter.MonitorBaseG;
        private static int[] ControlBaseG => QD75BufferWriter.ControlBaseG;
        private static int[] ProgramBaseG => QD75BufferWriter.ProgramBaseG;
        private const int OffCurrentPos   = QD75BufferWriter.OffCurrentPos;
        private const int OffCurrentSpeed = QD75BufferWriter.OffCurrentSpeed;
        private const int OffErrorCode    = QD75BufferWriter.OffErrorCode;
        private const int OffWarningCode  = QD75BufferWriter.OffWarningCode;
        private const int OffAxisStatus   = QD75BufferWriter.OffAxisStatus;
        private const int OffStartNo      = QD75BufferWriter.OffStartNo;
        private const int OffErrorReset   = QD75BufferWriter.OffErrorReset;
        private const int OffJogSpeed     = QD75BufferWriter.OffJogSpeed;
        private const int OffNewSpeed     = QD75BufferWriter.OffNewSpeed;

        private const string JogBaseRegister = "M3000";
        private const string EmergencyStopRegister = "M3100";
        private const string HeartbeatRegister = "M4000";
        private const string StopRunRegister = "M212";
        private const string ExitStopRegister = "M210";
        private const string ContinueRegister = "M211";
        private const string PauseRegister = "M210";
        private const int PlcPollIntervalMs = PerformanceTuning.PlcPollIntervalMs;
        private const int CadProgramCompilationDebounceMs = 350;

        private readonly WpfUiState ui = new WpfUiState();
        private readonly SemaphoreSlim cadLoadGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim viewRefreshGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim programCommandGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim plcDeviceWriteGate = new SemaphoreSlim(1, 1);
        private readonly object plcPollSync = new object();
        private readonly object cadProgramCompilationLock = new object();
        private readonly CadProgramCompilationState cadProgramCompilationState =
            new CadProgramCompilationState();

        private readonly CadDocumentService cadService = new CadDocumentService();
        private readonly ConfigurationFilePathStore configurationFilePathStore;

        private readonly List<MonitorRow> monitorRows = new List<MonitorRow>();
        private readonly List<ProcessRow> processRows = new List<ProcessRow>();
        private readonly Dictionary<string, string> assignedPointKeys =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly int[] axCurrentPos = new int[4];
        private readonly int[] axCurrentSpeed = new int[4];
        private readonly int[] axMCode = new int[4];
        private readonly int[] axErrorCode = new int[4];
        private readonly int[] axWarningCode = new int[4];
        private readonly int[] axAxisStatus = new int[4];
        private readonly int[] axSignals = new int[4];
        private readonly int[] axCurrentDataNo = new int[4];
        private readonly int[] axLastDataNo = new int[4];
        private readonly int[] axErrorReset = new int[4];
        private readonly int[] axJogSpeed = new int[4];
        private readonly int[] axNewSpeed = new int[4];
        private int logicalStation = 0;

        private readonly List<LogEntry> logs = new List<LogEntry>();
        private readonly object logsLock = new object();
        private int logVersion;
        private int logPushedVersion;
        private int logUiRefreshPending;
        private PLCCommunication plcComm;
        private PLCCommunication plcMonitorComm;

        private CadDocumentService.CadLoadResult activeCadDocument;
        private CadDocumentService.CadLoadResult activeEngraveCadDocument;
        private CadDocumentService.CadLoadResult activeCutCadDocument;
        private CadDocumentService.CadLoadResult cadProgramPublishedDocument;
        private CadDocumentService.CadLoadResult cadProgramCompilationDocument;
        private CancellationTokenSource cadProgramCompilationCts;
        private Task cadProgramCompilationTask = Task.CompletedTask;
        private int cadProgramCompilationVersion;
        private bool cadProgramCompilationDelayed;
        private bool cadProgramCompilationPropagatesFailures;
        private string selectedCadPointKey;
        private string activeDocumentKind = "DXF";
        private bool isMixedEngraveCutProgram;
        private string globalZDown = "";
        private string globalZSafe = "";
        private string globalZStart = "";
        private string globalSpeed = "1000";
        private string globalSpeedM3 = "10000";
        private string testEngraveSpeed = "10000";
        private string engraveSpeed = "1200";
        private string engravePower = "35";
        private string cutSpeed = "500";
        private string cutPower = "80";
        private string laserPower = "100";
        private double offsetX = 0.0;
        private double offsetY = 0.0;
        private double workspaceWidth = 170.0;
        private double workspaceHeight = 170.0;
        private string globalDwellM3 = "100";
        private string globalDwellM4 = "100";
        private string memberPassword = "";
        private QD75RingBufferRunner activeRingRunner;
        private readonly ProgramRunCompletionTracker programRunCompletionTracker = new ProgramRunCompletionTracker();
        private readonly IntervalGate controlUiPushGate = new IntervalGate(PerformanceTuning.ControlUiPushIntervalMs);
        private readonly IntervalGate axisMonitorUiPushGate = new IntervalGate(PerformanceTuning.ControlUiPushIntervalMs);
        private readonly IntervalGate controlTrackingUiPushGate = new IntervalGate(PerformanceTuning.ControlTrackingUiPushIntervalMs);
        private readonly IntervalGate slowPlcMonitorPollGate = new IntervalGate(PerformanceTuning.SlowPlcMonitorPollIntervalMs);
        private readonly IntervalGate plcHeartbeatGate = new IntervalGate(PerformanceTuning.PlcHeartbeatIntervalMs);
        private int plcConnectionChangeInFlight;
        private int slowPlcMonitorInFlight;
        private int plcHeartbeatInFlight;
        private int controlUiPushInFlight;
        private int axisMonitorUiPushInFlight;
        private int plcWriteInFlight;
        private int nextFastMonitorAxis = -1;

        private int GetActiveProgramIndex()
        {
            var runner = activeRingRunner;
            int raw = (plcComm != null && plcComm.IsConnected) ? Math.Max(0, axCurrentDataNo[0]) : 0;
            return (runner != null) ? runner.GetContinuousIndex(raw) : raw;
        }

        private bool IsProgramRunning()
        {
            return isProgramRunning;
        }

        private volatile bool isProgramRunning;
        private volatile bool isClosing;
        private volatile bool isPolling;
        private volatile bool plcStartupReady;
        private CancellationTokenSource plcPollCts;
        private Task plcPollTask;
        
        private string currentView = "control";
        private int navigationRefreshVersion;
        private int dxfStatePushVersion;
        private string currentTheme = "dark";
        private string plcIpAddress = "192.168.3.39";
        private int plcPort = 3000;
        private string connectionBanner = "PLC disconnected";
        private string integrityState = "IDLE";
        private string integrityDetail = "STOP";
        private string integrityTone = "idle";
        private float currentJogSpeedD406 = 1000f;
        private bool allowClose;
        private bool isShutdownInitiated;
        private string configurationFilePath = string.Empty;
        private bool configurationFileSelectionRequired;

        public Form1()
        {
            InitializeComponent();
            DataContext = ui;
            InitializeCameraRecordingDurationTimer();

            InitializeProcessRows();
            UpdateConnectionState(false, "PLC disconnected");
            UpdateIntegrityState(false);

            configurationFilePathStore = new ConfigurationFilePathStore(
                DefaultConfigurationFilePath,
                ConfigurationSelectionStatePath);
            LoadSelectedConfigurationAtStartup();
            ConfigureCommands();
            SyncSettingsToUi();
            currentTheme = WpfThemeManager.Apply(currentTheme, Resources, this);
            ui.CurrentTheme = currentTheme;

            StateChanged += (sender, e) =>
            {
                if (!isClosing && WindowState != WindowState.Maximized)
                    WindowState = WindowState.Maximized;
            };

            Loaded += async (sender, e) =>
            {
                if (configurationFileSelectionRequired)
                    await PromptForConfigurationFileAsync();

                // Apply after WPF has built the complete visual tree, including each view's local styles.
                currentTheme = WpfThemeManager.Apply(currentTheme, Resources, this);
                ui.CurrentTheme = currentTheme;
                WindowState = WindowState.Maximized;
                await PushAllStateAsync();
                _ = RefreshCamerasAsync();
                // Start PLC polling if not already started
                if (!isPolling)
                    StartPlcPolling();
                    
            };
        }

        private static string SettingsDataDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DACDT_2026");

        private static string DefaultConfigurationFilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DACDT_2026", "DACDT_2026_settings.txt");

        private static string ConfigurationSelectionStatePath =>
            Path.Combine(SettingsDataDirectory, "configuration_path.txt");

        private static string PreviousSettingsFilePath =>
            Path.Combine(SettingsDataDirectory, "app_settings.txt");

        private static string LegacySettingsFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.txt");

        private void ConfigureCommands()
        {
            ui.SwitchViewCommand = new RelayCommand(async p =>
            {
                await HandleSwitchViewAsync(p);
            });
            ui.ToggleThemeCommand = new RelayCommand(async () =>
            {
                currentTheme = WpfThemeManager.Apply(WpfThemeManager.Toggle(currentTheme), Resources, this);
                ui.CurrentTheme = currentTheme;
                SaveSettingsToFile();
                await PushNavigationStateAsync();
            });
            ui.ConnectToggleCommand = new RelayCommand(() => HandleConnectToggleAsync(Payload("station", ui.LogicalStationInput)));
            ui.EmergencyStopCommand = new RelayCommand(HandleEmergencyStopAsync);
            ui.StopRunCommand = new RelayCommand(HandleStopRunAsync);
            ui.ExitCommand = new RelayCommand(() =>
            {
                Close();
            });
            ui.JogStartCommand = new RelayCommand(p => HandleJogWriteAsync(ToInt(p, -1), true));
            ui.JogStopCommand = new RelayCommand(p => HandleJogWriteAsync(ToInt(p, -1), false));
            ui.GoHomeStartCommand = new RelayCommand(() => HandleGoHomeWriteAsync(true));
            ui.GoHomeStopCommand = new RelayCommand(() => HandleGoHomeWriteAsync(false));
            ui.HomeAllStartCommand = new RelayCommand(() => HandleHomeAllWriteAsync(true));
            ui.HomeAllStopCommand = new RelayCommand(() => HandleHomeAllWriteAsync(false));
            ui.ResetErrorStartCommand = new RelayCommand(() => HandleResetErrorWriteAsync(true));
            ui.ResetErrorStopCommand = new RelayCommand(() => HandleResetErrorWriteAsync(false));
            ui.StartActionStartCommand = new RelayCommand(() => HandleStartWriteAsync(true));
            ui.StartActionStopCommand = new RelayCommand(() => HandleStartWriteAsync(false));
            ui.ContinueStartCommand = new RelayCommand(() => HandleContinueWriteAsync(true));
            ui.ContinueStopCommand = new RelayCommand(() => HandleContinueWriteAsync(false));
            ui.PauseStartCommand = new RelayCommand(() => HandlePauseWriteAsync(true));
            ui.PauseStopCommand = new RelayCommand(() => HandlePauseWriteAsync(false));
            ui.SetJogSpeedCommand = new RelayCommand(() => HandleSetJogSpeedAsync(ui.JogSpeedInput));
            ui.SetZHeightCommand = new RelayCommand(() => HandleSetZHeightAsync(ui.ZHeightInput));
            ui.SetLaserPowerCommand = new RelayCommand(async () =>
            {
                if (double.TryParse(ui.LaserPowerInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                {
                    if (val < 0 || val > 100)
                    {
                        await NotifyAsync("warning", "Laser Power", "Laser power must be between 0% and 100%.");
                        ui.LaserPowerInput = laserPower;
                        return;
                    }
                    await HandleSetLaserPowerAsync(val);
                }
                else
                {
                    await NotifyAsync("error", "Laser Power", "Invalid laser power value.");
                    ui.LaserPowerInput = laserPower;
                }
            });
            ui.ImportDxfCommand = new RelayCommand(HandleImportDxfAsync);
            ui.ToggleCadPathCommand = new RelayCommand(p => HandleToggleCadPathAsync(ToInt(p, -1)));
            ui.ClearBufferCommand = new RelayCommand(HandleClearBufferAsync);
            ui.SendCadXCommand = new RelayCommand(async () => await HandleSendCadXAsync());
            ui.TestEngraveAreaCommand = new RelayCommand(HandleTestEngraveAreaAsync);
            ui.ClearLogsCommand = new RelayCommand(HandleClearLogsAsync);
            ui.ApplyDxfSettingsCommand = new RelayCommand(ApplyDxfSettingsAsync);
            ui.SaveSettingsCommand = new RelayCommand(async () =>
            {
                await SaveSelectedConfigurationAsync(showSuccess: true);
            });
            ui.BrowseConfigurationFileCommand = new RelayCommand(PromptForConfigurationFileAsync);
            ui.SetWorkspaceCommand = new RelayCommand(ApplyWorkspaceSettingsAsync);
            ui.ApplyPlcConnectionCommand = new RelayCommand(async () =>
            {
                plcIpAddress = ui.PlcIpAddressInput;
                plcPort = ui.PlcPortInput;
                SaveSettingsToFile();
                await PushControlStateAsync();
                await NotifyAsync("success", "PLC", "PLC connection settings saved.");
            });
            ui.RefreshCamerasCommand = new RelayCommand(RefreshCamerasAsync);
            ui.StartCameraCommand = new RelayCommand(StartCameraAsync);
            ui.StopCameraCommand = new RelayCommand(StopCameraAsync);
            ui.StartCameraRecordingCommand = new RelayCommand(StartCameraRecordingAsync);
            ui.StopCameraRecordingCommand = new RelayCommand(StopCameraRecordingAsync);
            ui.BrowseCameraRecordingFolderCommand = new RelayCommand(BrowseCameraRecordingFolderAsync);
            ui.SetCameraRecordingFolderCommand = new RelayCommand(() => SetCameraRecordingFolderAsync(ui.CameraRecordingFolderInput));
            ui.ExportQD75Command = new RelayCommand(() => _ = HandleExportQD75Async());
            ui.BrowseSvgCommand = new RelayCommand(BrowseSvgAsync);
            ui.BrowseSvgOutputCommand = new RelayCommand(BrowseSvgOutputAsync);
            ui.ConvertSvgToDxfCommand = new RelayCommand(ConvertSvgToDxfAsync);
            ui.LoadConvertedDxfToRunCommand = new RelayCommand(LoadConvertedDxfToRunAsync);
        }

        private async Task BrowseSvgAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "SVG files (*.svg)|*.svg|All files (*.*)|*.*",
                DefaultExt = "svg",
                AddExtension = true,
                Title = "Select an SVG file",
                CheckFileExists = true,
                Multiselect = false,
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() != true)
                return;

            string filePath = dialog.FileName;
            ui.SvgInputPath = filePath;
            ui.SvgOutputPath = Path.ChangeExtension(filePath, ".dxf");
            ui.SvgConversionStatus = "Loading SVG preview...";

            try
            {
                var previewTuple = await Task.Run(() => BuildSvgVectorPreviewGeometry(filePath));
                if (previewTuple.Item1 != null)
                {
                    ui.SvgDxfPreviewGeometry = previewTuple.Item1;
                    ui.SvgDxfPreviewBoundsText = previewTuple.Item2;
                    ui.SvgDxfPreviewPathCount = previewTuple.Item3;
                    ui.SvgDxfPreviewVertexCount = previewTuple.Item4;
                    ui.SvgConversionStatus = string.Format(
                        CultureInfo.InvariantCulture,
                        "SVG loaded: {0} path(s), {1} vertices. Ready to save DXF.",
                        previewTuple.Item3, previewTuple.Item4);
                }
                else
                {
                    ui.SvgConversionStatus = "SVG loaded. Ready to save DXF.";
                }
            }
            catch (Exception ex)
            {
                ui.SvgConversionStatus = "SVG loaded with preview warning: " + ex.Message;
            }
        }

        private static Tuple<System.Windows.Media.Geometry, string, int, int> BuildSvgVectorPreviewGeometry(string svgPath)
        {
            try
            {
                var converter = new SvgToDxfConverter();
                var paths = converter.ExtractPaths(svgPath);
                if (paths == null || paths.Count == 0)
                    return Tuple.Create<System.Windows.Media.Geometry, string, int, int>(null, string.Empty, 0, 0);

                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                int totalVertices = 0;

                foreach (var path in paths)
                {
                    if (path == null) continue;
                    foreach (var pt in path)
                    {
                        totalVertices++;
                        if (pt.X < minX) minX = pt.X;
                        if (pt.X > maxX) maxX = pt.X;
                        if (pt.Y < minY) minY = pt.Y;
                        if (pt.Y > maxY) maxY = pt.Y;
                    }
                }

                if (totalVertices == 0 || minX > maxX || minY > maxY)
                    return Tuple.Create<System.Windows.Media.Geometry, string, int, int>(null, string.Empty, 0, 0);

                double width = Math.Max(maxX - minX, 1.0);
                double height = Math.Max(maxY - minY, 1.0);
                string boundsText = string.Format(CultureInfo.InvariantCulture, "{0:0.##} mm × {1:0.##} mm", width, height);

                const double CanvasWidth = 800.0;
                const double CanvasHeight = 480.0;
                const double Padding = 24.0;

                double scale = Math.Min(
                    (CanvasWidth - Padding * 2.0) / width,
                    (CanvasHeight - Padding * 2.0) / height);
                double contentWidth = width * scale;
                double contentHeight = height * scale;
                double marginX = (CanvasWidth - contentWidth) / 2.0;
                double marginY = (CanvasHeight - contentHeight) / 2.0;

                var geometry = new System.Windows.Media.StreamGeometry { FillRule = System.Windows.Media.FillRule.EvenOdd };
                using (var ctx = geometry.Open())
                {
                    foreach (var path in paths)
                    {
                        if (path == null || path.Count < 2)
                            continue;

                        var firstPt = path[0];
                        double px0 = marginX + (firstPt.X - minX) * scale;
                        double py0 = marginY + (firstPt.Y - minY) * scale;
                        var start = new System.Windows.Point(px0, py0);

                        var linePoints = new System.Collections.Generic.List<System.Windows.Point>(path.Count - 1);
                        for (int i = 1; i < path.Count; i++)
                        {
                            var pt = path[i];
                            double px = marginX + (pt.X - minX) * scale;
                            double py = marginY + (pt.Y - minY) * scale;
                            linePoints.Add(new System.Windows.Point(px, py));
                        }

                        ctx.BeginFigure(start, isFilled: false, isClosed: false);
                        ctx.PolyLineTo(linePoints, isStroked: true, isSmoothJoin: true);
                    }
                }
                geometry.Freeze();
                return Tuple.Create<System.Windows.Media.Geometry, string, int, int>(geometry, boundsText, paths.Count, totalVertices);
            }
            catch
            {
                return Tuple.Create<System.Windows.Media.Geometry, string, int, int>(null, string.Empty, 0, 0);
            }
        }

        private async Task BrowseSvgOutputAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "DXF files (*.dxf)|*.dxf|All files (*.*)|*.*",
                DefaultExt = "dxf",
                AddExtension = true,
                Title = "Select DXF Output Destination",
                OverwritePrompt = true,
                RestoreDirectory = true
            };

            if (!string.IsNullOrWhiteSpace(ui.SvgOutputPath))
            {
                try
                {
                    string dir = Path.GetDirectoryName(ui.SvgOutputPath);
                    string name = Path.GetFileName(ui.SvgOutputPath);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        dialog.InitialDirectory = dir;
                    if (!string.IsNullOrWhiteSpace(name))
                        dialog.FileName = name;
                }
                catch { }
            }
            else if (!string.IsNullOrWhiteSpace(ui.SvgInputPath))
            {
                try
                {
                    string dir = Path.GetDirectoryName(ui.SvgInputPath);
                    string name = Path.GetFileNameWithoutExtension(ui.SvgInputPath) + ".dxf";
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        dialog.InitialDirectory = dir;
                    dialog.FileName = name;
                }
                catch { }
            }

            if (dialog.ShowDialog() != true)
                return;

            ui.SvgOutputPath = dialog.FileName;
        }

        private async Task LoadConvertedDxfToRunAsync()
        {
            string output = ui.SvgOutputPath?.Trim();
            if (string.IsNullOrWhiteSpace(output))
            {
                await NotifyAsync("warning", "SVG to DXF", "No converted DXF file found. Convert an SVG file first.");
                return;
            }

            // Resolve a relative output (e.g. a bare filename typed in the editor)
            // against the source SVG directory before checking existence.
            try
            {
                if (!Path.IsPathRooted(output))
                {
                    string input = ui.SvgInputPath?.Trim();
                    string inputDir = string.IsNullOrWhiteSpace(input)
                        ? null
                        : Path.GetDirectoryName(input);
                    output = !string.IsNullOrWhiteSpace(inputDir)
                        ? Path.Combine(inputDir, output)
                        : Path.GetFullPath(output);
                }
                if (!string.Equals(Path.GetExtension(output), ".dxf", StringComparison.OrdinalIgnoreCase))
                {
                    output = Path.ChangeExtension(output, ".dxf");
                }
            }
            catch
            {
                // Fall through with the original value; existence check below reports the issue.
            }

            if (!File.Exists(output))
            {
                await NotifyAsync("warning", "SVG to DXF", "No converted DXF file found at the specified output path.");
                return;
            }

            await ImportDxfPathAsync(output);
            await HandleSwitchViewAsync("dxf");
        }

        private async Task ConvertSvgToDxfAsync()
        {
            string input = ui.SvgInputPath?.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                ui.SvgConversionStatus = "Select an SVG file first.";
                await NotifyAsync("warning", "SVG to DXF", "Select an SVG file before converting.");
                return;
            }

            string output = ui.SvgOutputPath?.Trim();
            if (string.IsNullOrWhiteSpace(output))
            {
                output = Path.ChangeExtension(input, ".dxf");
            }
            else
            {
                try
                {
                    if (!Path.IsPathRooted(output))
                    {
                        string inputDir = Path.GetDirectoryName(input);
                        output = !string.IsNullOrWhiteSpace(inputDir)
                            ? Path.Combine(inputDir, output)
                            : Path.GetFullPath(output);
                    }
                    if (!string.Equals(Path.GetExtension(output), ".dxf", StringComparison.OrdinalIgnoreCase))
                    {
                        output = Path.ChangeExtension(output, ".dxf");
                    }
                }
                catch
                {
                    output = Path.ChangeExtension(input, ".dxf");
                }
            }

            try
            {
                var converter = new SvgToDxfConverter();
                SvgToDxfConverter.SvgConversionResult result =
                    await Task.Run(() => converter.ConvertTo(input, output));
                ui.SvgOutputPath = result.OutputPath;
                ui.SvgConversionStatus = string.Format(
                    CultureInfo.InvariantCulture,
                    "Successfully converted {0} path(s), {1} vertices -> {2}",
                    result.PathCount, result.VertexCount, Path.GetFileName(result.OutputPath));
                ui.SvgDxfPreviewPathCount = result.PathCount;
                ui.SvgDxfPreviewVertexCount = result.VertexCount;

                // Build a lightweight DXF preview geometry for the converted file
                string previewPath = result.OutputPath;
                var previewTuple = await Task.Run(() => BuildSvgDxfPreviewGeometry(previewPath));
                ui.SvgDxfPreviewGeometry = previewTuple.Item1;
                ui.SvgDxfPreviewBoundsText = previewTuple.Item2;

                await NotifyAsync("success", "SVG to DXF", "DXF file saved: " + result.OutputPath);
            }
            catch (Exception ex)
            {
                ui.SvgConversionStatus = "Conversion failed: " + ex.Message;
                ui.SvgDxfPreviewGeometry = null;
                ui.SvgDxfPreviewBoundsText = string.Empty;
                await NotifyAsync("error", "SVG to DXF", ex.Message);
            }
        }

        private static Tuple<System.Windows.Media.Geometry, string> BuildSvgDxfPreviewGeometry(string dxfPath)
        {
            try
            {
                var cadService = new CadDocumentService();
                CadDocumentService.CadLoadResult doc = cadService.Load(dxfPath);
                if (doc?.Primitives == null || doc.Primitives.Count == 0)
                    return Tuple.Create<System.Windows.Media.Geometry, string>(null, string.Empty);

                CadDocumentService.CadBounds bounds = doc.Bounds;
                if (bounds == null)
                    return Tuple.Create<System.Windows.Media.Geometry, string>(null, string.Empty);

                double left = bounds.Left;
                double top = bounds.Top;
                double right = bounds.Right;
                double bottom = bounds.Bottom;
                if (right <= left) right = left + Math.Max(bounds.Width, 1.0);
                if (bottom <= top) bottom = top + Math.Max(bounds.Height, 1.0);

                string boundsText = string.Format(CultureInfo.InvariantCulture, "{0:0.##} mm × {1:0.##} mm", bounds.Width, bounds.Height);

                const double CanvasWidth = 800.0;
                const double CanvasHeight = 480.0;
                const double Padding = 24.0;

                double docWidth = Math.Max(right - left, 0.001);
                double docHeight = Math.Max(bottom - top, 0.001);
                double scale = Math.Min(
                    (CanvasWidth - Padding * 2.0) / docWidth,
                    (CanvasHeight - Padding * 2.0) / docHeight);
                double contentWidth = docWidth * scale;
                double contentHeight = docHeight * scale;
                double marginX = (CanvasWidth - contentWidth) / 2.0;
                double marginY = (CanvasHeight - contentHeight) / 2.0;

                var geometry = new System.Windows.Media.StreamGeometry { FillRule = System.Windows.Media.FillRule.EvenOdd };
                using (var ctx = geometry.Open())
                {
                    foreach (var primitive in doc.Primitives)
                    {
                        if (primitive?.Points == null || primitive.Points.Count < 2)
                            continue;

                        var firstPt = primitive.Points[0];
                        double px0 = marginX + (firstPt.X - left) * scale;
                        double py0 = marginY + contentHeight - (firstPt.Y - top) * scale;
                        var start = new System.Windows.Point(px0, py0);

                        var linePoints = new System.Collections.Generic.List<System.Windows.Point>(primitive.Points.Count - 1);
                        for (int i = 1; i < primitive.Points.Count; i++)
                        {
                            var pt = primitive.Points[i];
                            double px = marginX + (pt.X - left) * scale;
                            double py = marginY + contentHeight - (pt.Y - top) * scale;
                            linePoints.Add(new System.Windows.Point(px, py));
                        }

                        ctx.BeginFigure(start, isFilled: false, isClosed: false);
                        ctx.PolyLineTo(linePoints, isStroked: true, isSmoothJoin: true);
                    }
                }
                geometry.Freeze();
                return Tuple.Create<System.Windows.Media.Geometry, string>(geometry, boundsText);
            }
            catch
            {
                return Tuple.Create<System.Windows.Media.Geometry, string>(null, string.Empty);
            }
        }

        private async Task HandleSwitchViewAsync(object viewPayload)
        {
            currentView = Convert.ToString(viewPayload, CultureInfo.InvariantCulture) ?? "control";
            string requestedView = currentView;
            int requestVersion = Interlocked.Increment(ref navigationRefreshVersion);
            await PushNavigationStateAsync();
            _ = RefreshViewDataAfterNavigationAsync(requestedView, requestVersion);
        }

        private async Task RefreshViewDataAfterNavigationAsync(string viewName, int requestVersion)
        {
            await viewRefreshGate.WaitAsync();
            try
            {
                if (requestVersion != Volatile.Read(ref navigationRefreshVersion))
                    return;

                if (string.Equals(viewName, "logs", StringComparison.OrdinalIgnoreCase))
                {
                    await PushLogsStateAsync();
                }
                else if (string.Equals(viewName, "control", StringComparison.OrdinalIgnoreCase))
                {
                    await PushControlStateAsync();
                }
                else if (string.Equals(viewName, "monitor", StringComparison.OrdinalIgnoreCase))
                {
                    await PushControlStateAsync();
                    if (ui.Cameras.Count == 0)
                        await RefreshCamerasAsync();
                }
                else if (string.Equals(viewName, "dxf", StringComparison.OrdinalIgnoreCase))
                {
                    await PushDxfStateAsync();
                }
            }
            catch
            {
            }
            finally
            {
                viewRefreshGate.Release();
            }
        }

        private async Task<bool> TryEnterProgramCommandAsync(string action)
        {
            if (await programCommandGate.WaitAsync(0))
                return true;

            await NotifyAsync("info", action, "Another program command is already being processed.");
            return false;
        }

        private bool TryApplyWorkspaceInputs(out string errorMessage)
        {
            double requestedWidth = ui.WorkspaceWidthInput;
            double requestedHeight = ui.WorkspaceHeightInput;
            if (!WorkspaceLimitPolicy.IsValid(requestedWidth, requestedHeight))
            {
                errorMessage = "Workspace Width and Height must be finite values greater than 0.";
                ui.WorkspaceWidthInput = workspaceWidth;
                ui.WorkspaceHeightInput = workspaceHeight;
                return false;
            }

            workspaceWidth = requestedWidth;
            workspaceHeight = requestedHeight;
            errorMessage = string.Empty;
            return true;
        }

        private async Task ApplyWorkspaceSettingsAsync()
        {
            if (!TryApplyWorkspaceInputs(out string errorMessage))
            {
                await NotifyAsync("error", "Workspace", errorMessage);
                return;
            }

            SaveSettingsToFile();
            await HandleScanLimitsAsync();
            await PushDxfStateAsync();
            await NotifyAsync("success", "Settings", "Updated workspace size.");
        }

        private async Task ApplyDxfSettingsAsync()
        {
            if (!TryApplyWorkspaceInputs(out string workspaceError))
            {
                await NotifyAsync("error", "Workspace", workspaceError);
                return;
            }

            string viewBeforeApply = currentView;
            offsetX = ui.OffsetXInput;
            offsetY = ui.OffsetYInput;
            globalSpeed = ui.GlobalSpeedInput;
            globalSpeedM3 = ui.GlobalSpeedM3Input;
            globalDwellM3 = ui.GlobalDwellM3Input;
            globalDwellM4 = ui.GlobalDwellM4Input;
            testEngraveSpeed = ui.TestEngraveSpeedInput;
            SyncEngraveCutSettingsFromUi();

            if (isMixedEngraveCutProgram)
            {
                await RebuildMixedEngraveCutProgramAsync();
                currentView = viewBeforeApply;
                SaveSettingsToFile();
                await HandleScanLimitsAsync();
                await PushDxfStateAsync();
                await NotifyAsync("success", "Settings", "Updated engrave/cut DXF settings.");
                return;
            }

            await HandleProcessValueAsync("speed", ui.GlobalSpeedInput);
            await HandleProcessValueAsync("globalSpeedM3", ui.GlobalSpeedM3Input);
            await HandleProcessValueAsync("dwellM3", ui.GlobalDwellM3Input);
            await HandleProcessValueAsync("dwellM4", ui.GlobalDwellM4Input);
            await HandleProcessValueAsync("testEngraveSpeed", ui.TestEngraveSpeedInput);
            SaveSettingsToFile();
            await HandleScanLimitsAsync();
            await PushDxfStateAsync();
        }

        private void SyncSettingsToUi()
        {
            ui.ConfigurationFilePathInput = configurationFilePath;
            ui.LogicalStationInput = logicalStation;
            ui.PlcIpAddressInput = plcIpAddress;
            ui.PlcPortInput = plcPort;
            ui.SetJogSpeedInputFromPlc(currentJogSpeedD406);
            ui.GlobalSpeedInput = globalSpeed;
            ui.GlobalSpeedM3Input = globalSpeedM3;
            ui.TestEngraveSpeedInput = testEngraveSpeed;
            ui.EngraveSpeedInput = engraveSpeed;
            ui.EngravePowerInput = engravePower;
            ui.CutSpeedInput = cutSpeed;
            ui.CutPowerInput = cutPower;
            ui.GlobalDwellM3Input = globalDwellM3;
            ui.GlobalDwellM4Input = globalDwellM4;
            ui.OffsetXInput = offsetX;
            ui.OffsetYInput = offsetY;
            ui.WorkspaceWidthInput = workspaceWidth;
            ui.WorkspaceHeightInput = workspaceHeight;
            ui.LaserPowerInput = laserPower;
            ui.CurrentTheme = currentTheme;
            ui.CameraRecordingFolderInput = cameraRecordingDir;
        }

        private void SyncSettingsFromUiForPersistence()
        {
            logicalStation = ui.LogicalStationInput;
            plcIpAddress = ui.PlcIpAddressInput;
            plcPort = ui.PlcPortInput;
            globalSpeed = ui.GlobalSpeedInput;
            globalSpeedM3 = ui.GlobalSpeedM3Input;
            testEngraveSpeed = ui.TestEngraveSpeedInput;
            engraveSpeed = ui.EngraveSpeedInput;
            engravePower = ui.EngravePowerInput;
            cutSpeed = ui.CutSpeedInput;
            cutPower = ui.CutPowerInput;
            globalDwellM3 = ui.GlobalDwellM3Input;
            globalDwellM4 = ui.GlobalDwellM4Input;
            offsetX = ui.OffsetXInput;
            offsetY = ui.OffsetYInput;
            double requestedWidth = ui.WorkspaceWidthInput;
            double requestedHeight = ui.WorkspaceHeightInput;
            if (WorkspaceLimitPolicy.IsValid(requestedWidth, requestedHeight))
            {
                workspaceWidth = requestedWidth;
                workspaceHeight = requestedHeight;
            }
            else
            {
                ui.WorkspaceWidthInput = workspaceWidth;
                ui.WorkspaceHeightInput = workspaceHeight;
            }
            laserPower = ui.LaserPowerInput;
            currentTheme = WpfThemeManager.Normalize(ui.CurrentTheme);
        }

        private bool LoadSettingsFromFile(string path = null)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    path = configurationFilePath;
                if (!File.Exists(path)) return false;

                foreach (string line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    switch (key)
                    {
                        case "globalSpeed": globalSpeed = val; break;
                        case "globalSpeedM3": globalSpeedM3 = val; break;
                        case "testEngraveSpeed": testEngraveSpeed = val; break;
                        case "engraveSpeed": engraveSpeed = val; break;
                        case "engravePower": engravePower = val; break;
                        case "cutSpeed": cutSpeed = val; break;
                        case "cutPower": cutPower = val; break;
                        case "workspaceWidth": double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out workspaceWidth); break;
                        case "workspaceHeight": double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out workspaceHeight); break;
                        case "offsetX": double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out offsetX); break;
                        case "offsetY": double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out offsetY); break;
                        case "plcIpAddress": plcIpAddress = val; break;
                        case "plcPort": int.TryParse(val, out plcPort); break;
                        case "logicalStation": int.TryParse(val, out logicalStation); break;
                        case "globalZStart": globalZStart = val; break;
                        case "globalZDown": globalZDown = val; break;
                        case "globalZSafe": globalZSafe = val; break;
                        case "globalDwellM3": globalDwellM3 = val; break;
                        case "globalDwellM4": globalDwellM4 = val; break;
                        case "memberPassword": memberPassword = val; break;
                        case "laserPower": laserPower = val; break;
                        case "theme": currentTheme = WpfThemeManager.Normalize(val); break;
                        case "cameraRecordingDir": cameraRecordingDir = val; break;
                        default: break;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool SaveSettingsToFile(string path = null)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    path = configurationFilePath;
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var lines = new List<string>
                {
                    "# DACDT_2026 Settings",
                    $"globalSpeed={globalSpeed}",
                    $"workspaceWidth={workspaceWidth.ToString("0.###", CultureInfo.InvariantCulture)}",
                    $"workspaceHeight={workspaceHeight.ToString("0.###", CultureInfo.InvariantCulture)}",
                    $"offsetX={offsetX.ToString("0.###", CultureInfo.InvariantCulture)}",
                    $"offsetY={offsetY.ToString("0.###", CultureInfo.InvariantCulture)}",
                    $"plcIpAddress={plcIpAddress}",
                    $"plcPort={plcPort}",
                    $"logicalStation={logicalStation}",
                    $"globalZStart={globalZStart}",
                    $"globalZDown={globalZDown}",
                    $"globalZSafe={globalZSafe}",
                    $"globalDwellM3={globalDwellM3}",
                    $"globalDwellM4={globalDwellM4}",
                    $"globalSpeedM3={globalSpeedM3}",
                    $"testEngraveSpeed={testEngraveSpeed}",
                    $"engraveSpeed={engraveSpeed}",
                    $"engravePower={engravePower}",
                    $"cutSpeed={cutSpeed}",
                    $"cutPower={cutPower}",
                    $"memberPassword={memberPassword}",
                    $"laserPower={laserPower}",
                    $"theme={currentTheme}",
                    $"cameraRecordingDir={cameraRecordingDir}",
                };
                File.WriteAllLines(path, lines);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LoadSelectedConfigurationAtStartup()
        {
            configurationFilePath = configurationFilePathStore.GetSelectedPath();
            if (string.Equals(configurationFilePath, DefaultConfigurationFilePath, StringComparison.OrdinalIgnoreCase)
                && !File.Exists(DefaultConfigurationFilePath)
                && SaveSettingsToFile(DefaultConfigurationFilePath))
            {
                configurationFileSelectionRequired = false;
                return;
            }

            if (!configurationFilePathStore.NeedsSelection(configurationFilePath))
            {
                LoadSettingsFromFile(configurationFilePath);
                return;
            }

            if (string.Equals(configurationFilePath, DefaultConfigurationFilePath, StringComparison.OrdinalIgnoreCase))
            {
                string legacyPath = File.Exists(PreviousSettingsFilePath)
                    ? PreviousSettingsFilePath
                    : LegacySettingsFilePath;
                if (File.Exists(legacyPath) && LoadSettingsFromFile(legacyPath))
                    return;
            }

            configurationFileSelectionRequired = true;
        }

        private async Task PromptForConfigurationFileAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Configuration files (*.txt)|*.txt|All files (*.*)|*.*",
                InitialDirectory = configurationFilePathStore.GetBrowseDirectory(configurationFilePath),
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) == true)
                await SelectConfigurationFileAsync(dialog.FileName);
        }

        private async Task SelectConfigurationFileAsync(string path)
        {
            string selectedPath = path == null ? string.Empty : path.Trim();
            if (configurationFilePathStore.NeedsSelection(selectedPath) || !LoadSettingsFromFile(selectedPath))
            {
                await NotifyAsync("error", "Settings", "The selected configuration file could not be loaded.");
                return;
            }

            if (!configurationFilePathStore.TrySaveSelectedPath(selectedPath))
            {
                await NotifyAsync("error", "Settings", "The selected configuration path could not be remembered.");
                return;
            }

            configurationFilePath = selectedPath;
            configurationFileSelectionRequired = false;
            SyncSettingsToUi();
            currentTheme = WpfThemeManager.Apply(currentTheme, Resources, this);
            ui.CurrentTheme = currentTheme;
            await NotifyAsync("success", "Settings", "Configuration file loaded.");
        }

        private async Task SaveSelectedConfigurationAsync(bool showSuccess)
        {
            string selectedPath = ui.ConfigurationFilePathInput == null
                ? string.Empty
                : ui.ConfigurationFilePathInput.Trim();
            if (string.IsNullOrWhiteSpace(selectedPath))
                selectedPath = configurationFilePath;

            SyncSettingsFromUiForPersistence();
            if (!SaveSettingsToFile(selectedPath))
            {
                await NotifyAsync("error", "Settings", "The configuration file could not be saved.");
                return;
            }

            if (!configurationFilePathStore.TrySaveSelectedPath(selectedPath))
            {
                await NotifyAsync("error", "Settings", "The configuration file was saved, but its path could not be remembered.");
                return;
            }

            configurationFilePath = selectedPath;
            configurationFileSelectionRequired = false;
            ui.ConfigurationFilePathInput = configurationFilePath;
            if (showSuccess)
                await NotifyAsync("success", "Settings", "Settings saved to the configuration file.");
        }

        private void SaveSelectedConfigurationOnClose()
        {
            try
            {
                string selectedPath = ui.ConfigurationFilePathInput == null
                    ? string.Empty
                    : ui.ConfigurationFilePathInput.Trim();
                if (string.IsNullOrWhiteSpace(selectedPath))
                    selectedPath = configurationFilePath;

                SyncSettingsFromUiForPersistence();
                if (IsUncConfigurationPath(selectedPath))
                {
                    SaveConfigurationToNetworkPathInBackground(selectedPath);
                    return;
                }

                if (SaveSettingsToFile(selectedPath) && configurationFilePathStore.TrySaveSelectedPath(selectedPath))
                    configurationFilePath = selectedPath;
                else
                    LogLifecycle("Configuration file was not saved during shutdown.");
            }
            catch (Exception ex)
            {
                LogLifecycle("Configuration save during shutdown failed: " + ex.Message);
            }
        }

        private static bool IsUncConfigurationPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && path.TrimStart().StartsWith(@"\\", StringComparison.Ordinal);
        }

        private void SaveConfigurationToNetworkPathInBackground(string path)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    if (!SaveSettingsToFile(path) || !configurationFilePathStore.TrySaveSelectedPath(path))
                        LogLifecycle("Network configuration file was not saved during shutdown.");
                }
                catch (Exception ex)
                {
                    LogLifecycle("Network configuration save during shutdown failed: " + ex.Message);
                }
            });
        }

        private static Dictionary<string, object> Payload(params object[] keyValues)
        {
            var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i + 1 < keyValues.Length; i += 2)
                payload[Convert.ToString(keyValues[i], CultureInfo.InvariantCulture)] = keyValues[i + 1];
            return payload;
        }

        private static int ToInt(object value, int fallback)
        {
            if (value == null) return fallback;
            int parsed;
            return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : fallback;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!allowClose)
            {
                e.Cancel = true;

                if (isShutdownInitiated)
                {
                    return;
                }

                bool plcConnected = plcComm != null && plcComm.IsConnected;
                bool shouldSendExitStop = ExitShutdownPolicy.ShouldSendExitStop(plcConnected);
                string message = ExitShutdownPolicy.GetConfirmationMessage(plcConnected);

                var result = MessageBox.Show(
                    message,
                    "Exit App",
                    MessageBoxButton.YesNo,
                    shouldSendExitStop ? MessageBoxImage.Warning : MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                isShutdownInitiated = true;
                _ = ShutdownSequenceAsync();
                return;
            }

            isClosing = true;
            CancelCadProgramCompilation();
            SaveSelectedConfigurationOnClose();
            StopCameraCore();
            StopPlcPolling();

            if (plcComm != null)
            {
                QueuePlcDisposeForShutdown(plcComm);
                plcComm = null;
            }
            if (plcMonitorComm != null)
            {
                QueuePlcDisposeForShutdown(plcMonitorComm);
                plcMonitorComm = null;
            }

            base.OnClosing(e);
        }

        private async Task ShutdownSequenceAsync()
        {
            try
            {
                if (ExitShutdownPolicy.ShouldSendExitStop(plcComm != null && plcComm.IsConnected))
                {
                    Dispatcher.Invoke(() =>
                    {
                        ui.CameraStatus = "Sending M210, HOME ALL, then closing...";
                    });

                    await ExitShutdownPolicy.WaitForBestEffortAsync(SendStopForExitAsync());
                }
            }
            catch (Exception ex)
            {
                LogLifecycle("Error in shutdown sequence: " + ex.Message);
            }
            finally
            {
                LogLifecycle("Exit shutdown sequence completed. Closing application.");
                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (isClosing)
                        return;

                    allowClose = true;
                    try
                    {
                        Close();
                    }
                    catch (InvalidOperationException ex)
                    {
                        LogLifecycle("Close skipped because window is already closing: " + ex.Message);
                    }
                }));
            }
        }

        private async Task SendStopForExitAsync()
        {
            isProgramRunning = false;
            activeRingRunner?.Stop();

            PLCCommunication comm;
            if (!TryGetConnectedPlc(out comm))
                return;

            await WriteDeviceValueAsync(ExitStopRegister, 1);
            AddLogEntry(ExitStopRegister, "1", "Write", "OK", "Exit stop");
            await Task.Delay(PerformanceTuning.ExitStopPulseMs);
            await WriteDeviceValueAsync(ExitStopRegister, 0);
            AddLogEntry(ExitStopRegister, "0", "Write", "OK", "Exit stop reset");
            await Task.Delay(PerformanceTuning.ExitStopDelayMs);

            await WriteDeviceValueAsync("M502", 1);
            AddLogEntry("M502", "1", "Write", "OK", "Exit home all");
            await Task.Delay(PerformanceTuning.ExitHomePulseMs);
            await WriteDeviceValueAsync("M502", 0);
            AddLogEntry("M502", "0", "Write", "OK", "Exit home all reset");
            await Task.Delay(PerformanceTuning.ExitHomeDelayMs);
        }

        private static void QueuePlcDisposeForShutdown(PLCCommunication comm)
        {
            if (comm == null)
                return;

            _ = Task.Run(() =>
            {
                try { comm.Dispose(); } catch { }
            });
        }

        private static void LogLifecycle(string message)
        {
            try
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"),
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Lifecycle] " + message + "\r\n");
            }
            catch
            {
            }
        }

    }
}
