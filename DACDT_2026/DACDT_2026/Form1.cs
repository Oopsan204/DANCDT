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
        private const string StopRunRegister = "M212";
        private const string ContinueRegister = "M211";
        private const string PauseRegister = "M210";
        private const int PlcPollIntervalMs = 100;

        private readonly WpfUiState ui = new WpfUiState();
        private readonly SemaphoreSlim cadLoadGate = new SemaphoreSlim(1, 1);
        private readonly object plcPollSync = new object();

        private readonly CadDocumentService cadService = new CadDocumentService();
        private readonly GcodeCoordinateService gcodeCoordinateService = new GcodeCoordinateService();
        private readonly MqttPublishService mqttService = new MqttPublishService();

        private readonly List<MonitorRow> monitorRows = new List<MonitorRow>();
        private readonly List<ProcessRow> processRows = new List<ProcessRow>();
        private readonly Dictionary<string, string> assignedPointKeys =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> telemetryRegisters = new List<string> { "U0\\G800", "U0\\G900", "U0\\G1000", "U0\\G1100" };
        private readonly List<TelemetryBuffer> telemetryBuffers = new List<TelemetryBuffer> { new TelemetryBuffer { Path = "U0\\G2006", Length = 2 } };

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
        private PLCCommunication plcComm;

        private CadDocumentService.CadLoadResult activeCadDocument;
        private string selectedCadPointKey;
        private string activeDocumentKind = "DXF";
        private string globalZDown = "";
        private string globalZSafe = "";
        private string globalZStart = "";
        private string globalSpeed = "1000";
        private string globalSpeedM3 = "10000";
        private string gcodeSpeedM3 = "10000";
        private string rapidSpeed = "10000";
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

        private volatile bool webReady;
        private volatile bool isClosing;
        private volatile bool isPolling;
        private CancellationTokenSource plcPollCts;
        private Task plcPollTask;
        
        private string currentView = "control";
        private string currentTheme = "dark";
        private string plcIpAddress = "192.168.3.39";
        private int plcPort = 3000;
        private string connectionBanner = "PLC disconnected";
        private string integrityState = "IDLE";
        private string integrityDetail = "STOP";
        private string integrityTone = "idle";
        private float currentJogSpeedD406 = 1000f;
        private bool allowClose;

        public Form1()
        {
            InitializeComponent();
            DataContext = ui;

            InitializeProcessRows();
            UpdateConnectionState(false, "PLC disconnected");
            UpdateIntegrityState(false);

            LoadSettingsFromFile();
            ConfigureCommands();
            SyncSettingsToUi();
            mqttService.MessageReceived += MqttService_MessageReceived;

            Loaded += async (sender, e) =>
            {
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

        private static string SettingsFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.txt");

        private static string ProfilesDirPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings_profiles");

        private void ConfigureCommands()
        {
            ui.SwitchViewCommand = new RelayCommand(async p =>
            {
                await HandleSwitchViewAsync(p);
            });
            ui.ToggleThemeCommand = new RelayCommand(async () =>
            {
                currentTheme = currentTheme == "dark" ? "light" : "dark";
                await PushNavigationStateAsync();
            });
            ui.ConnectToggleCommand = new RelayCommand(() => HandleConnectToggleAsync(Payload("station", ui.LogicalStationInput)));
            ui.EmergencyStopCommand = new RelayCommand(HandleEmergencyStopAsync);
            ui.StopRunCommand = new RelayCommand(HandleStopRunAsync);
            ui.ExitCommand = new RelayCommand(() =>
            {
                allowClose = true;
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
            ui.SetLaserPowerCommand = new RelayCommand(async () =>
            {
                if (double.TryParse(ui.LaserPowerInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                {
                    await HandleSetLaserPowerAsync(val);
                }
                else
                {
                    await NotifyAsync("error", "Laser Power", "Giá trị công suất laze không hợp lệ.");
                }
            });
            ui.OpenDxfCommand = new RelayCommand(HandleOpenDxfAsync);
            ui.NewGcodeCommand = new RelayCommand(HandleNewGcodeAsync);
            ui.SaveGcodeCommand = new RelayCommand(() => HandleSaveGcodeAsync(ui.RawGcodeText));
            ui.PreviewGcodeCommand = new RelayCommand(() => HandlePreviewGcodeAsync(ui.RawGcodeText));
            ui.ClearBufferCommand = new RelayCommand(HandleClearBufferAsync);
            ui.SendCadXCommand = new RelayCommand(HandleSendCadXAsync);
            ui.TestEngraveAreaCommand = new RelayCommand(HandleTestEngraveAreaAsync);
            ui.ClearLogsCommand = new RelayCommand(HandleClearLogsAsync);
            ui.AddTelemetryRegisterCommand = new RelayCommand(() => HandleAddTelemetryRegisterAsync(ui.TelemetryAddressInput));
            ui.AddTelemetryBufferCommand = new RelayCommand(() => HandleAddTelemetryBufferAsync(ui.TelemetryAddressInput, ui.TelemetryLengthInput));
            ui.WriteBufferCommand = new RelayCommand(() => HandleWriteBufferRequestAsync(ui.WriteAddressInput, ui.WriteValueInput));
            ui.ApplyDxfSettingsCommand = new RelayCommand(ApplyDxfSettingsAsync);
            ui.ApplyGcodeSettingsCommand = new RelayCommand(ApplyGcodeSettingsAsync);
            ui.SetG0SpeedCommand = new RelayCommand(async () =>
            {
                rapidSpeed = ui.RapidSpeedInput;
                SaveSettingsToFile();
                await PushDxfStateAsync();
                await NotifyAsync("success", "Settings", "Updated G00 rapid speed.");
            });
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
            ui.SaveProfileCommand = new RelayCommand(SaveProfileAsync);
            ui.LoadProfileCommand = new RelayCommand(LoadProfileAsync);
            ui.DeleteProfileCommand = new RelayCommand(DeleteProfileAsync);
            ui.RefreshCamerasCommand = new RelayCommand(RefreshCamerasAsync);
            ui.StartCameraCommand = new RelayCommand(StartCameraAsync);
            ui.StopCameraCommand = new RelayCommand(StopCameraAsync);
            ui.StartCameraRecordingCommand = new RelayCommand(StartCameraRecordingAsync);
            ui.StopCameraRecordingCommand = new RelayCommand(StopCameraRecordingAsync);
            ui.ExportQD75Command = new RelayCommand(() => _ = HandleExportQD75Async());
        }

        private async Task HandleSwitchViewAsync(object viewPayload)
        {
            currentView = Convert.ToString(viewPayload, CultureInfo.InvariantCulture) ?? "control";
            await PushNavigationStateAsync();
            _ = RefreshViewDataAfterNavigationAsync(currentView);
        }

        private async Task RefreshViewDataAfterNavigationAsync(string viewName)
        {
            try
            {
                if (string.Equals(viewName, "telemetry", StringComparison.OrdinalIgnoreCase))
                {
                    await PushTelemetryStateAsync();
                }
                else if (string.Equals(viewName, "logs", StringComparison.OrdinalIgnoreCase))
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
            }
            catch
            {
            }
        }

        private async Task ApplyDxfSettingsAsync()
        {
            offsetX = ui.OffsetXInput;
            offsetY = ui.OffsetYInput;
            await HandleProcessValueAsync("speed", ui.GlobalSpeedInput);
            await HandleProcessValueAsync("globalSpeedM3", ui.GlobalSpeedM3Input);
            await HandleProcessValueAsync("dwellM3", ui.GlobalDwellM3Input);
            await HandleProcessValueAsync("dwellM4", ui.GlobalDwellM4Input);
            SaveSettingsToFile();
            await HandleScanLimitsAsync();
            await PushDxfStateAsync();
        }

        private async Task ApplyGcodeSettingsAsync()
        {
            await HandleProcessValueAsync("gcodeSpeedM3", ui.GcodeSpeedM3Input);
            SaveSettingsToFile();
            await PushDxfStateAsync();
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

        private async Task SaveProfileAsync()
        {
            string cleanName = CleanProfileName(ui.ProfileNameInput);
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                await NotifyAsync("error", "Profiles", "Ten cau hinh khong hop le.");
                return;
            }

            Directory.CreateDirectory(ProfilesDirPath);
            SaveSettingsToFile(Path.Combine(ProfilesDirPath, cleanName + ".txt"));
            await NotifyAsync("success", "Profiles", $"Da luu cau hinh '{cleanName}'.");
            await PushDxfStateAsync();
        }

        private async Task LoadProfileAsync()
        {
            string cleanName = CleanProfileName(ui.SelectedProfile);
            string profilePath = Path.Combine(ProfilesDirPath, cleanName + ".txt");
            if (string.IsNullOrWhiteSpace(cleanName) || !File.Exists(profilePath))
            {
                await NotifyAsync("error", "Profiles", "Khong tim thay cau hinh.");
                return;
            }

            LoadSettingsFromFile(profilePath);
            SaveSettingsToFile();
            SyncSettingsToUi();

            if (activeCadDocument != null)
                await HandleImportCadToProcessAsync();

            await HandleScanLimitsAsync();
            await PushDxfStateAsync();
            await NotifyAsync("success", "Profiles", $"Da tai cau hinh '{cleanName}'.");
        }

        private async Task DeleteProfileAsync()
        {
            string cleanName = CleanProfileName(ui.SelectedProfile);
            string profilePath = Path.Combine(ProfilesDirPath, cleanName + ".txt");
            if (string.IsNullOrWhiteSpace(cleanName) || !File.Exists(profilePath))
            {
                await NotifyAsync("error", "Profiles", "Khong tim thay cau hinh de xoa.");
                return;
            }

            File.Delete(profilePath);
            await NotifyAsync("success", "Profiles", $"Da xoa cau hinh '{cleanName}'.");
            await PushDxfStateAsync();
        }

        private void SyncSettingsToUi()
        {
            ui.LogicalStationInput = logicalStation;
            ui.PlcIpAddressInput = plcIpAddress;
            ui.PlcPortInput = plcPort;
            ui.JogSpeedInput = currentJogSpeedD406;
            ui.GlobalSpeedInput = globalSpeed;
            ui.GlobalSpeedM3Input = globalSpeedM3;
            ui.GcodeSpeedM3Input = gcodeSpeedM3;
            ui.RapidSpeedInput = rapidSpeed;
            ui.GlobalDwellM3Input = globalDwellM3;
            ui.GlobalDwellM4Input = globalDwellM4;
            ui.OffsetXInput = offsetX;
            ui.OffsetYInput = offsetY;
            ui.WorkspaceWidthInput = workspaceWidth;
            ui.WorkspaceHeightInput = workspaceHeight;
            ui.ActiveWcs = activeWcs;
            ui.LaserPowerInput = laserPower;
            SyncWcsOffsetsToUi();
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

        private void LoadSettingsFromFile(string path = null)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    path = SettingsFilePath;
                if (!File.Exists(path)) return;

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
            }
            catch { }
        }

        private void SaveSettingsToFile(string path = null)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    path = SettingsFilePath;

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
                    $"memberPassword={memberPassword}",
                    $"activeWcs={activeWcs}",
                    $"laserPower={laserPower}",
                };
                for (int i = 0; i < 6; i++)
                {
                    string gName = "G5" + (4 + i);
                    lines.Add($"wcs{gName}X={wcsOffsetX[i].ToString("0.###", CultureInfo.InvariantCulture)}");
                    lines.Add($"wcs{gName}Y={wcsOffsetY[i].ToString("0.###", CultureInfo.InvariantCulture)}");
                }
                File.WriteAllLines(path, lines);
            }
            catch { }
        }

        private List<string> GetProfilesList()
        {
            var profiles = new List<string>();
            try
            {
                if (Directory.Exists(ProfilesDirPath))
                {
                    foreach (var file in Directory.GetFiles(ProfilesDirPath, "*.txt"))
                        profiles.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch { }
            return profiles;
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

        private static string CleanProfileName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return "";
            var cleanName = "";
            foreach (char c in rawName)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                    cleanName += c;
            }
            return cleanName;
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
                    "DACDT/camera/command");
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

            _ = HandleMqttCommandAsync(topic, payload);
        }

        private async Task HandleMqttCommandAsync(string topic, string payload)
        {
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
                    await NotifyAsync("success", "MQTT Camera", "Bắt đầu quay camera.");
                    break;

                case "STOPRECORD":
                case "DUNGQUAY":
                    await StopCameraRecordingAsync();
                    await StopCameraAsync();
                    await NotifyAsync("info", "MQTT Camera", "Dừng quay camera.");
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
                return;
            }

            isClosing = true;
            webReady = false;
            StopCameraCore();
            StopPlcPolling();

            if (plcComm != null)
            {
                try { plcComm.Dispose(); } catch { }
                plcComm = null;
            }

            base.OnClosing(e);
        }
    }
}
