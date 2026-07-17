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
    /// Main WPF window. The existing PLC, DXF/G-code, logging and state logic remains
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

        private readonly WpfUiState ui = new WpfUiState();
        private readonly SemaphoreSlim cadLoadGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim viewRefreshGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim programCommandGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim plcDeviceWriteGate = new SemaphoreSlim(1, 1);
        private readonly object plcPollSync = new object();

        private readonly CadDocumentService cadService = new CadDocumentService();
        private readonly GcodeCoordinateService gcodeCoordinateService = new GcodeCoordinateService();
        private readonly MqttPublishService mqttService = new MqttPublishService();
        private readonly WebCadUploadSession webCadUploadSession = new WebCadUploadSession();
        private readonly WebRtcBridgeClient webRtcBridgeClient = new WebRtcBridgeClient();
        private readonly ConfigurationFilePathStore configurationFilePathStore;
        private System.Diagnostics.Process backgroundServiceProcess;

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
        private string selectedCadPointKey;
        private string activeDocumentKind = "DXF";
        private bool isMixedEngraveCutProgram;
        private string globalZDown = "";
        private string globalZSafe = "";
        private string globalZStart = "";
        private string globalSpeed = "1000";
        private string globalSpeedM3 = "10000";
        private string gcodeSpeedM3 = "10000";
        private string rapidSpeed = "10000";
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
        private string activeWcs = "G54";
        private readonly double[] wcsOffsetX = new double[6];
        private readonly double[] wcsOffsetY = new double[6];
        private string rawGcodeText = string.Empty;
        private QD75RingBufferRunner activeRingRunner;
        private readonly ProgramRunCompletionTracker programRunCompletionTracker = new ProgramRunCompletionTracker();
        private DateTime lastMachineMqttPublishUtc = DateTime.MinValue;
        private readonly IntervalGate controlUiPushGate = new IntervalGate(PerformanceTuning.ControlUiPushIntervalMs);
        private readonly IntervalGate axisMonitorUiPushGate = new IntervalGate(PerformanceTuning.ControlUiPushIntervalMs);
        private readonly IntervalGate controlTrackingUiPushGate = new IntervalGate(PerformanceTuning.ControlTrackingUiPushIntervalMs);
        private readonly IntervalGate slowPlcMonitorPollGate = new IntervalGate(PerformanceTuning.SlowPlcMonitorPollIntervalMs);
        private readonly IntervalGate plcHeartbeatGate = new IntervalGate(PerformanceTuning.PlcHeartbeatIntervalMs);
        private int machineMqttPublishInFlight;
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
        private volatile bool webReady;
        private volatile bool isClosing;
        private volatile bool isPolling;
        private volatile bool plcStartupReady;
        private CancellationTokenSource plcPollCts;
        private Task plcPollTask;
        
        private string currentView = "control";
        private int navigationRefreshVersion;
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
            mqttService.MessageReceived += MqttService_MessageReceived;
            StartBackgroundVideoService();

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
                webReady = true;
                await InitMqttAsync();
                // Wait a bit for MQTT to connect
                await Task.Delay(500);
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
            ui.OpenDxfCommand = new RelayCommand(HandleOpenDxfAsync);
            ui.ImportDxfCommand = new RelayCommand(HandleImportDxfAsync);
            ui.ToggleCadPathCommand = new RelayCommand(p => HandleToggleCadPathAsync(ToInt(p, -1)));
            ui.NewGcodeCommand = new RelayCommand(HandleNewGcodeAsync);
            ui.SaveGcodeCommand = new RelayCommand(() => HandleSaveGcodeAsync(ui.RawGcodeText));
            ui.PreviewGcodeCommand = new RelayCommand(() => HandlePreviewGcodeAsync(ui.RawGcodeText));
            ui.ClearBufferCommand = new RelayCommand(HandleClearBufferAsync);
            ui.SendCadXCommand = new RelayCommand(async () => await HandleSendCadXAsync());
            ui.TestEngraveAreaCommand = new RelayCommand(HandleTestEngraveAreaAsync);
            ui.ClearLogsCommand = new RelayCommand(HandleClearLogsAsync);
            ui.ApplyDxfSettingsCommand = new RelayCommand(ApplyDxfSettingsAsync);
            ui.ApplyGcodeSettingsCommand = new RelayCommand(ApplyGcodeSettingsAsync);
            ui.SaveSettingsCommand = new RelayCommand(async () =>
            {
                await SaveSelectedConfigurationAsync(showSuccess: true);
            });
            ui.BrowseConfigurationFileCommand = new RelayCommand(PromptForConfigurationFileAsync);
            ui.SetWorkspaceCommand = new RelayCommand(async () =>
            {
                workspaceWidth = ui.WorkspaceWidthInput;
                workspaceHeight = ui.WorkspaceHeightInput;
                SaveSettingsToFile();
                await PushDxfStateAsync();
                await NotifyAsync("success", "Settings", "Updated workspace size.");
            });
            ui.SelectWcsCommand = new RelayCommand(async p =>
            {
                string selectedWcs = Convert.ToString(p, CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(selectedWcs))
                    return;

                int wcsIdx = GetWcsIndex(selectedWcs);
                activeWcs = "G5" + (4 + wcsIdx).ToString(CultureInfo.InvariantCulture);
                ui.ActiveWcs = activeWcs;
                var row = ui.WcsOffsets.FirstOrDefault(item =>
                    string.Equals(item.Name, activeWcs, StringComparison.OrdinalIgnoreCase));
                if (row != null)
                {
                    ui.WcsOffsetXInput = row.OffsetX;
                    ui.WcsOffsetYInput = row.OffsetY;
                }
                await PushDxfStateAsync();
            });
            ui.SetWcsCommand = new RelayCommand(ApplyWcsSettingsAsync);
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

        private async Task ApplyDxfSettingsAsync()
        {
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

        private async Task ApplyGcodeSettingsAsync()
        {
            rapidSpeed = ui.RapidSpeedInput;
            await HandleProcessValueAsync("gcodeSpeedM3", ui.GcodeSpeedM3Input);
            SaveSettingsToFile();
            await PushDxfStateAsync();
            await NotifyAsync("success", "Settings", "Updated G-code motion settings.");
        }

        private async Task ApplyWcsSettingsAsync()
        {
            foreach (var row in ui.WcsOffsets)
            {
                int rowIndex = GetWcsIndex(row.Name);
                wcsOffsetX[rowIndex] = row.OffsetX;
                wcsOffsetY[rowIndex] = row.OffsetY;
            }

            int wcsIdx = GetWcsIndex(ui.ActiveWcs);
            activeWcs = "G5" + (4 + wcsIdx).ToString(CultureInfo.InvariantCulture);
            ui.ActiveWcs = activeWcs;
            ui.WcsOffsetXInput = wcsOffsetX[wcsIdx];
            ui.WcsOffsetYInput = wcsOffsetY[wcsIdx];
            SaveSettingsToFile();
            await PushDxfStateAsync();
            await NotifyAsync("success", "WCS", $"Saved G54-G59 offsets. Active {activeWcs} X={ui.WcsOffsetXInput} Y={ui.WcsOffsetYInput}");
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
            ui.GcodeSpeedM3Input = gcodeSpeedM3;
            ui.RapidSpeedInput = rapidSpeed;
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
            ui.ActiveWcs = activeWcs;
            ui.LaserPowerInput = laserPower;
            ui.CurrentTheme = currentTheme;
            ui.CameraRecordingFolderInput = cameraRecordingDir;
            SyncWcsOffsetsToUi();
        }

        private void SyncSettingsFromUiForPersistence()
        {
            logicalStation = ui.LogicalStationInput;
            plcIpAddress = ui.PlcIpAddressInput;
            plcPort = ui.PlcPortInput;
            globalSpeed = ui.GlobalSpeedInput;
            globalSpeedM3 = ui.GlobalSpeedM3Input;
            gcodeSpeedM3 = ui.GcodeSpeedM3Input;
            rapidSpeed = ui.RapidSpeedInput;
            testEngraveSpeed = ui.TestEngraveSpeedInput;
            engraveSpeed = ui.EngraveSpeedInput;
            engravePower = ui.EngravePowerInput;
            cutSpeed = ui.CutSpeedInput;
            cutPower = ui.CutPowerInput;
            globalDwellM3 = ui.GlobalDwellM3Input;
            globalDwellM4 = ui.GlobalDwellM4Input;
            offsetX = ui.OffsetXInput;
            offsetY = ui.OffsetYInput;
            workspaceWidth = ui.WorkspaceWidthInput;
            workspaceHeight = ui.WorkspaceHeightInput;
            laserPower = ui.LaserPowerInput;
            currentTheme = WpfThemeManager.Normalize(ui.CurrentTheme);

            int activeIndex = GetWcsIndex(ui.ActiveWcs);
            activeWcs = "G5" + (4 + activeIndex).ToString(CultureInfo.InvariantCulture);
            foreach (var row in ui.WcsOffsets)
            {
                int rowIndex = GetWcsIndex(row.Name);
                wcsOffsetX[rowIndex] = row.OffsetX;
                wcsOffsetY[rowIndex] = row.OffsetY;
            }
        }

        private void SyncWcsOffsetsToUi()
        {
            int activeIndex = GetWcsIndex(activeWcs);
            activeWcs = "G5" + (4 + activeIndex).ToString(CultureInfo.InvariantCulture);

            ui.WcsOffsets.Clear();
            for (int i = 0; i < 6; i++)
            {
                ui.WcsOffsets.Add(new WcsOffsetViewModel
                {
                    Name = "G5" + (4 + i).ToString(CultureInfo.InvariantCulture),
                    OffsetX = wcsOffsetX[i],
                    OffsetY = wcsOffsetY[i]
                });
            }

            ui.ActiveWcs = activeWcs;
            ui.WcsOffsetXInput = wcsOffsetX[activeIndex];
            ui.WcsOffsetYInput = wcsOffsetY[activeIndex];
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
                        case "rapidSpeed": rapidSpeed = val; break;
                        case "globalSpeed": globalSpeed = val; break;
                        case "globalSpeedM3": globalSpeedM3 = val; break;
                        case "gcodeSpeedM3": gcodeSpeedM3 = val; break;
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
                        case "activeWcs": activeWcs = val; break;
                        case "laserPower": laserPower = val; break;
                        case "theme": currentTheme = WpfThemeManager.Normalize(val); break;
                        case "cameraRecordingDir": cameraRecordingDir = val; break;
                        default:
                            for (int i = 0; i < 6; i++)
                            {
                                string gName = "G5" + (4 + i);
                                if (key == "wcs" + gName + "X") { double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out wcsOffsetX[i]); break; }
                                if (key == "wcs" + gName + "Y") { double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out wcsOffsetY[i]); break; }
                        }
                        break;
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
                    $"rapidSpeed={rapidSpeed}",
                    $"globalSpeed={globalSpeed}",
                    $"gcodeSpeedM3={gcodeSpeedM3}",
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
                    $"activeWcs={activeWcs}",
                    $"laserPower={laserPower}",
                    $"theme={currentTheme}",
                    $"cameraRecordingDir={cameraRecordingDir}",
                };
                for (int i = 0; i < 6; i++)
                {
                    string gName = "G5" + (4 + i);
                    lines.Add($"wcs{gName}X={wcsOffsetX[i].ToString("0.###", CultureInfo.InvariantCulture)}");
                    lines.Add($"wcs{gName}Y={wcsOffsetY[i].ToString("0.###", CultureInfo.InvariantCulture)}");
                }
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

        private static int GetWcsIndex(string wcs)
        {
            switch (wcs)
            {
                case "G55": return 1;
                case "G56": return 2;
                case "G57": return 3;
                case "G58": return 4;
                case "G59": return 5;
                default: return 0;
            }
        }

        private async Task InitMqttAsync()
        {
            try
            {
                string broker = "beb7179d08fa43f79d440a9be9b95f24.s1.eu.hivemq.cloud";
                string username = "DACDT2026";
                string password = "trungaN123@";
                Console.WriteLine($"[DEBUG] Starting MQTT connection to {broker}:8883...");
                await mqttService.ConnectAsync(broker, username, password);
                await mqttService.SubscribeAsync(
                    "DACDT/machine/command",
                    "DACDT/machine/comment",
                    "DACDT/machine/coment",
                    "DACDT/machine/request",
                    "DACDT/web/connected",
                    "DACDT/camera/command",
                    "DACDT/cad/upload/start",
                    "DACDT/cad/upload/chunk",
                    "DACDT/cad/upload/finish",
                    "DACDT/cad/upload/cancel");
                Console.WriteLine($"[DEBUG] MQTT connection completed. IsConnected={mqttService.IsConnected}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT init failed: {ex.Message}");
                Console.WriteLine($"[DEBUG] MQTT IsConnected after error: {mqttService.IsConnected}");
            }
        }

        private void MqttService_MessageReceived(string topic, string payload)
        {
            if (isClosing)
                return;

            try
            {
                string payloadForLog = string.Equals(topic, "DACDT/cad/upload/chunk", StringComparison.OrdinalIgnoreCase)
                    ? "[CAD upload chunk omitted; bytes=" + (payload?.Length ?? 0).ToString(CultureInfo.InvariantCulture) + "]"
                    : payload;
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [MQTT] Topic: " + topic + ", Payload: " + payloadForLog + "\r\n");
            }
            catch { }



            _ = HandleMqttCommandAsync(topic, payload);
        }

        private async Task HandleMqttCommandAsync(string topic, string payload)
        {
            if (IsWebCadUploadTopic(topic))
            {
                await HandleWebCadUploadMessageAsync(topic, payload);
                return;
            }

            string command = ExtractMqttCommand(payload);
            if (string.IsNullOrWhiteSpace(command))
                return;

            try
            {
                if (IsMachineCommandTopic(topic))
                {
                    await HandleMachineMqttCommandAsync(command);
                }
                else if (string.Equals(topic, "DACDT/camera/command", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleCameraMqttCommandAsync(command);
                }
            }
            catch (Exception ex)
            {
                await NotifyAsync("error", "MQTT Command", $"{topic}: {command} - {ex.Message}");
            }
        }

        private async Task HandleMachineMqttCommandAsync(string command)
        {
            string normalized = NormalizeCommand(command);
            switch (normalized)
            {
                case "RUN":
                case "START":
                    await PulsePlcCommandAsync(HandleStartWriteAsync);
                    await NotifyAsync("success", "MQTT Machine", "RUN command executed.");
                    break;

                case "CONTINUE":
                case "RESUME":
                    await PulsePlcCommandAsync(HandleContinueWriteAsync);
                    await NotifyAsync("success", "MQTT Machine", "CONTINUE command executed.");
                    break;

                case "PAUSE":
                    await PulsePlcCommandAsync(HandlePauseWriteAsync);
                    await NotifyAsync("success", "MQTT Machine", "PAUSE command executed.");
                    break;

                case "HOME":
                case "GOHOME":
                    await PulsePlcCommandAsync(HandleGoHomeWriteAsync);
                    await NotifyAsync("success", "MQTT Machine", "HOME command executed.");
                    break;

                case "HOMEALL":
                    await PulsePlcCommandAsync(HandleHomeAllWriteAsync);
                    await NotifyAsync("success", "MQTT Machine", "HOME ALL command executed.");
                    break;

                case "RESET":
                    await PulsePlcCommandAsync(HandleResetErrorWriteAsync);
                    await NotifyAsync("success", "MQTT Machine", "RESET command executed.");
                    break;

                case "STOP":
                    await HandleStopRunAsync();
                    await NotifyAsync("error", "MQTT Machine", "STOP command executed; run buffer cleared.");
                    break;

                case "REFRESH":
                case "GETSTATE":
                case "REQUESTSTATE":
                case "PUBLISHSTATE":
                case "WEBCONNECTED":
                    await PublishAllMqttAsync();
                    await NotifyAsync("success", "MQTT Machine", "State request received; published machine/cad state once.");
                    break;

                case "ESTOP":
                case "EMERGENCYSTOP":
                    activeRingRunner?.Stop();
                    await HandleEmergencyStopAsync();
                    await NotifyAsync("error", "MQTT Machine", "STOP command executed via emergency stop.");
                    break;

                default:
                    await NotifyAsync("info", "MQTT Machine", "Ignored unknown command: " + command);
                    break;
            }
        }

        private async Task HandleCameraMqttCommandAsync(string command)
        {
            string normalized = NormalizeCommand(command);
            switch (normalized)
            {
                case "START":
                case "STARTCAM":
                case "ON":
                    await StartCameraAsync();
                    await NotifyAsync("success", "MQTT Camera", "START command executed.");
                    break;

                case "STOP":
                case "STOPCAM":
                case "OFF":
                    await StopCameraAsync();
                    await NotifyAsync("info", "MQTT Camera", "STOP command executed.");
                    break;

                case "STARTRECORD":
                case "BATDAUQUAY":
                    await StartCameraAsync();
                    await StartCameraRecordingAsync();
                    await NotifyAsync("success", "MQTT Camera", "Camera recording started.");
                    break;

                case "STOPRECORD":
                case "DUNGQUAY":
                    await StopCameraRecordingAsync();
                    await StopCameraAsync();
                    await NotifyAsync("info", "MQTT Camera", "Camera recording stopped.");
                    break;

                default:
                    await NotifyAsync("info", "MQTT Camera", "Ignored unknown command: " + command);
                    break;
            }
        }

        private static async Task PulsePlcCommandAsync(Func<bool, Task> writeCommand)
        {
            await writeCommand(true);
            await Task.Delay(150);
            await writeCommand(false);
        }

        private static bool IsMachineCommandTopic(string topic)
        {
            return string.Equals(topic, "DACDT/machine/command", StringComparison.OrdinalIgnoreCase)
                || string.Equals(topic, "DACDT/machine/comment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(topic, "DACDT/machine/coment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(topic, "DACDT/machine/request", StringComparison.OrdinalIgnoreCase)
                || string.Equals(topic, "DACDT/web/connected", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return string.Empty;

            var normalized = "";
            foreach (char c in command.Trim())
            {
                if (char.IsLetterOrDigit(c))
                    normalized += char.ToUpperInvariant(c);
            }

            return normalized;
        }

        private static string ExtractMqttCommand(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return string.Empty;

            string text = payload.Trim();
            try
            {
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                if (text.StartsWith("{", StringComparison.Ordinal) && text.EndsWith("}", StringComparison.Ordinal))
                {
                    var map = serializer.Deserialize<Dictionary<string, object>>(text);
                    foreach (string key in new[] { "command", "cmd", "action", "value", "text" })
                    {
                        object value;
                        if (map != null && map.TryGetValue(key, out value) && value != null)
                            return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
                    }
                }
                else if (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal))
                {
                    return serializer.Deserialize<string>(text)?.Trim() ?? string.Empty;
                }
            }
            catch
            {
            }

            return text.Trim('"').Trim();
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
            webReady = false;
            SaveSelectedConfigurationOnClose();
            StopCameraCore();
            StopPlcPolling();
            try { webRtcBridgeClient.Dispose(); } catch { }
            StopBackgroundVideoService();

            if (mqttService != null)
            {
                try { _ = mqttService.DisconnectAsync(); } catch { }
            }

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

        private void StartBackgroundVideoService()
        {
            try
            {
                // Force kill any orphaned background service processes to ensure we run the latest code
                if (System.Diagnostics.Process.GetProcessesByName(System.Diagnostics.Process.GetCurrentProcess().ProcessName).Length <= 1)
                {
                    LogLifecycle("Stopping existing WebRTC background service processes...");
                    StopBackgroundVideoServiceProcessForce();
                }

                if (IsWebRtcBridgeListening())
                {
                    LogLifecycle("Reusing existing WebRTC background service.");
                    return;
                }

                string serviceExe = FindBackgroundVideoServiceExecutable();

                if (!string.IsNullOrEmpty(serviceExe))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = serviceExe,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        Arguments = BackgroundVideoServiceProcess.BuildParentPidArguments(System.Diagnostics.Process.GetCurrentProcess().Id),
                        WorkingDirectory = System.IO.Path.GetDirectoryName(serviceExe)
                    };
                    backgroundServiceProcess = System.Diagnostics.Process.Start(psi);
                    LogLifecycle("Started background service: " + serviceExe);

                    if (!WaitForWebRtcBridge(TimeSpan.FromSeconds(5)))
                    {
                        LogLifecycle("WebRTC background service did not open 127.0.0.1:5080 within 5 seconds.");
                        ui.CameraStatus = "WebRTC service is not ready. Build/run WebRtcCameraService.";
                    }
                }
                else
                {
                    LogLifecycle("Background service executable not found. Build the solution so WebRtcCameraService.exe is copied beside DACDT_2026.exe.");
                    ui.CameraStatus = "WebRTC service executable not found.";
                }
            }
            catch (Exception ex)
            {
                LogLifecycle("Error starting service: " + ex.Message);
                ui.CameraStatus = "Error starting WebRTC service: " + ex.Message;
            }
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

        private static bool WaitForWebRtcBridge(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (IsWebRtcBridgeListening())
                    return true;

                System.Threading.Thread.Sleep(100);
            }

            return IsWebRtcBridgeListening();
        }

        private static bool IsWebRtcBridgeListening()
        {
            try
            {
                return System.Net.NetworkInformation.IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpListeners()
                    .Any(endpoint => endpoint.Port == 5080 && System.Net.IPAddress.IsLoopback(endpoint.Address));
            }
            catch
            {
                return false;
            }
        }

        private static string FindBackgroundVideoServiceExecutable()
        {
            const string serviceFileName = "WebRtcCameraService.exe";

            // A deployed copy can sit beside the desktop application.
            string localCopy = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, serviceFileName);
            if (System.IO.File.Exists(localCopy))
                return localCopy;

            // During development the desktop app can run from Debug, x86\Debug,
            // or x64\Debug. Walk upward instead of relying on a fixed number of
            // ".." segments so all of those output layouts find the shared service.
            var directory = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                foreach (string configuration in new[] { "Debug", "Release" })
                {
                    string candidate = System.IO.Path.Combine(
                        directory.FullName,
                        "WebRtcCameraService",
                        "bin",
                        configuration,
                        serviceFileName);

                    if (System.IO.File.Exists(candidate))
                        return candidate;
                }

                foreach (string platform in new[] { "x64", "Any CPU", "AnyCPU" })
                {
                    foreach (string configuration in new[] { "Debug", "Release" })
                    {
                        string candidate = System.IO.Path.Combine(
                            directory.FullName,
                            "WebRtcCameraService",
                            "bin",
                            platform,
                            configuration,
                            serviceFileName);

                        if (System.IO.File.Exists(candidate))
                            return candidate;
                    }
                }

                directory = directory.Parent;
            }

            return null;
        }

        private void StopBackgroundVideoService()
        {
            try
            {
                // Another dashboard can still be using the shared WebRTC service.
                if (System.Diagnostics.Process.GetProcessesByName(System.Diagnostics.Process.GetCurrentProcess().ProcessName).Length > 1)
                    return;

                if (backgroundServiceProcess != null && !backgroundServiceProcess.HasExited)
                {
                    backgroundServiceProcess.Kill();
                    backgroundServiceProcess.WaitForExit(3000);
                    backgroundServiceProcess.Dispose();
                    backgroundServiceProcess = null;
                    System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                        "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Lifecycle] Stopped background service.\r\n");
                }

                // Also force stop to clean up any orphaned or reused instances
                StopBackgroundVideoServiceProcessForce();
            }
            catch { }
        }

        private static void StopBackgroundVideoServiceProcessForce()
        {
            try
            {
                foreach (var p in System.Diagnostics.Process.GetProcessesByName("WebRtcCameraService"))
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(3000);
                        p.Dispose();
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
