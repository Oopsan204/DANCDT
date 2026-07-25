using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace DACDT_2026
{
    public sealed class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool suppressNotifications;

        public void ReplaceWith(IEnumerable<T> items)
        {
            suppressNotifications = true;
            try
            {
                ClearItems();
                foreach (T item in items)
                    Items.Add(item);
            }
            finally
            {
                suppressNotifications = false;
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddRange(IEnumerable<T> items)
        {
            bool anyAdded = false;

            suppressNotifications = true;
            try
            {
                foreach (T item in items)
                {
                    Items.Add(item);
                    anyAdded = true;
                }
            }
            finally
            {
                suppressNotifications = false;
            }

            if (!anyAdded)
                return;

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!suppressNotifications)
                base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!suppressNotifications)
                base.OnPropertyChanged(e);
        }
    }

    public sealed class WpfUiState : ObservableState
    {
        private const int MaxCadSelectionOverlayPoints = 10000;
        private const int LazyTableBatchSize = 100;

        private string currentView = "control";
        private string currentTheme = "dark";
        private bool isConnected;
        private bool isStartActionEnabled;
        private string connectionBanner = "PLC disconnected";
        private string connectionButtonText = "CONNECT PLC Q";
        private string connectionMeta = "MX Component logical station: 0";
        private int logicalStationInput;
        private string plcIpAddressInput = "192.168.3.39";
        private int plcPortInput = 3000;
        private float jogSpeedD406 = 1000f;
        private string jogSpeedInput = "1000";
        private string zHeightInput = "0";
        private bool suppressJogSpeedInputDirty;
        private bool jogSpeedInputDirty;
        private string laserPowerInput = "100";
        private string progressText = "0%";
        private bool progressVisible;
        private int progressPercent;
        private bool runProgressVisible;
        private string fileKind = "";
        private string filePath = "";
        private string fileName = "";
        private string rawGcodeText = "";
        private string globalSpeedInput = "1000";
        private string globalSpeedM3Input = "10000";
        private string gcodeSpeedM3Input = "10000";
        private string rapidSpeedInput = "10000";
        private string testEngraveSpeedInput = "10000";
        private string engraveSpeedInput = "1200";
        private string engravePowerInput = "35";
        private string cutSpeedInput = "500";
        private string cutPowerInput = "80";
        private string globalDwellM3Input = "100";
        private string globalDwellM4Input = "100";
        private double offsetXInput;
        private double offsetYInput;
        private double workspaceWidthInput = 170.0;
        private double workspaceHeightInput = 170.0;
        private string activeWcs = "G54";
        private double wcsOffsetXInput;
        private double wcsOffsetYInput;
        private string configurationFilePathInput = "";
        private string writeAddressInput = "D100";
        private int writeValueInput = 12345;
        private string selectedPointKey = "";
        private string activeNotice = "";
        private int activeProgramIndex;
        private int lastHighlightedProgramIndex;
        private ImageSource cadPreviewImage;
        private System.Windows.Media.Geometry cadPreviewGeometry;
        private System.Windows.Media.Geometry cadEngravePreviewGeometry;
        private System.Windows.Media.Geometry cadCutPreviewGeometry;
        private System.Windows.Media.Geometry cadSelectionOverlayGeometry;
        private Brush cadSelectionOverlayStroke = Brushes.Transparent;
        private CadPathHitIndex cadPathHitIndex;
        private double cadPreviewStrokeThickness = 0.65;
        private ImageSource cameraFrame;
        private CameraDeviceViewModel selectedCamera;
        private string selectedCameraMoniker = "";
        private string cameraStatus = "Camera idle.";
        private string cameraRecordingPath = "";
        private string cameraRecordingFolderInput = "";
        private bool isCameraRunning;
        private bool isCameraRecording;
        private int cameraRecordedFrames;
        private string cameraRecordingElapsed = "00:00:00";
        private string cameraRecordingCompletedText = "MP4 recording stopped";
        private readonly List<CadPointViewModel> allCadPoints = new List<CadPointViewModel>();
        private readonly List<GeometryRowViewModel> allGeometryRows = new List<GeometryRowViewModel>();
        private Func<int, int, IReadOnlyList<ProcessRowViewModel>> processRowWindowLoader;
        private int processRowCount;
        private bool hasEngraveCutProgram;

        public WpfUiState()
        {
            for (int i = 1; i <= 3; i++)
                Axes.Add(new AxisStatusViewModel { Index = i });
        }

        public BulkObservableCollection<AxisStatusViewModel> Axes { get; } = new BulkObservableCollection<AxisStatusViewModel>();
        public BulkObservableCollection<LogRowViewModel> Logs { get; } = new BulkObservableCollection<LogRowViewModel>();
        public BulkObservableCollection<UiEventViewModel> Events { get; } = new BulkObservableCollection<UiEventViewModel>();
        public BulkObservableCollection<CameraDeviceViewModel> Cameras { get; } = new BulkObservableCollection<CameraDeviceViewModel>();
        public BulkObservableCollection<CadPointViewModel> CadPoints { get; } = new BulkObservableCollection<CadPointViewModel>();
        public BulkObservableCollection<GeometryRowViewModel> GeometryRows { get; } = new BulkObservableCollection<GeometryRowViewModel>();
        public BulkObservableCollection<ProcessRowViewModel> ProcessRows { get; } = new BulkObservableCollection<ProcessRowViewModel>();
        public BulkObservableCollection<ProcessRowViewModel> ProgramRows { get; } = new BulkObservableCollection<ProcessRowViewModel>();
        // Kept empty for the existing document-reset path; the CAD view never binds or populates it.
        public BulkObservableCollection<CadPrimitiveViewModel> CadPrimitives { get; } = new BulkObservableCollection<CadPrimitiveViewModel>();
        public BulkObservableCollection<CadLimitAreaViewModel> CadLimitAreas { get; } = new BulkObservableCollection<CadLimitAreaViewModel>();
        public BulkObservableCollection<CadAxisLineViewModel> CadAxisLines { get; } = new BulkObservableCollection<CadAxisLineViewModel>();
        public BulkObservableCollection<CadAxisLabelViewModel> CadAxisLabels { get; } = new BulkObservableCollection<CadAxisLabelViewModel>();
        public BulkObservableCollection<CadTrackingPointViewModel> CadTrackingPoints { get; } = new BulkObservableCollection<CadTrackingPointViewModel>();
        public BulkObservableCollection<WcsOffsetViewModel> WcsOffsets { get; } = new BulkObservableCollection<WcsOffsetViewModel>();

        public ICommand SwitchViewCommand { get; set; }
        public ICommand ToggleThemeCommand { get; set; }
        public ICommand ConnectToggleCommand { get; set; }
        public ICommand EmergencyStopCommand { get; set; }
        public ICommand StopRunCommand { get; set; }
        public ICommand ExitCommand { get; set; }
        public ICommand JogStartCommand { get; set; }
        public ICommand JogStopCommand { get; set; }
        public ICommand GoHomeStartCommand { get; set; }
        public ICommand GoHomeStopCommand { get; set; }
        public ICommand HomeAllStartCommand { get; set; }
        public ICommand HomeAllStopCommand { get; set; }
        public ICommand ResetErrorStartCommand { get; set; }
        public ICommand ResetErrorStopCommand { get; set; }
        public ICommand StartActionStartCommand { get; set; }
        public ICommand StartActionStopCommand { get; set; }
        public ICommand ContinueStartCommand { get; set; }
        public ICommand ContinueStopCommand { get; set; }
        public ICommand PauseStartCommand { get; set; }
        public ICommand PauseStopCommand { get; set; }
        public ICommand SetJogSpeedCommand { get; set; }
        public ICommand SetZHeightCommand { get; set; }
        public ICommand SetLaserPowerCommand { get; set; }
        public ICommand ImportDxfCommand { get; set; }
        public ICommand ToggleCadPathCommand { get; set; }
        public ICommand ClearBufferCommand { get; set; }
        public ICommand SendCadXCommand { get; set; }
        public ICommand TestEngraveAreaCommand { get; set; }
        public ICommand ClearLogsCommand { get; set; }
        public ICommand ApplyDxfSettingsCommand { get; set; }
        public ICommand SaveSettingsCommand { get; set; }
        public ICommand BrowseConfigurationFileCommand { get; set; }
        public ICommand SetWorkspaceCommand { get; set; }
        public ICommand SelectWcsCommand { get; set; }
        public ICommand SetWcsCommand { get; set; }
        public ICommand ApplyPlcConnectionCommand { get; set; }
        public ICommand RefreshCamerasCommand { get; set; }
        public ICommand StartCameraCommand { get; set; }
        public ICommand StopCameraCommand { get; set; }
        public ICommand StartCameraRecordingCommand { get; set; }
        public ICommand StopCameraRecordingCommand { get; set; }
        public ICommand BrowseCameraRecordingFolderCommand { get; set; }
        public ICommand SetCameraRecordingFolderCommand { get; set; }
        public ICommand ExportQD75Command { get; set; }

        public string CurrentView
        {
            get => currentView;
            set
            {
                if (SetProperty(ref currentView, value))
                {
                    OnPropertyChanged(nameof(IsControlView));
                    OnPropertyChanged(nameof(IsDxfView));
                    OnPropertyChanged(nameof(IsMonitorView));
                    OnPropertyChanged(nameof(IsLogsView));
                    OnPropertyChanged(nameof(IsSettingsView));
                    OnPropertyChanged(nameof(IsHelpView));
                }
            }
        }

        public string CurrentTheme
        {
            get => currentTheme;
            set
            {
                if (SetProperty(ref currentTheme, value))
                {
                    OnPropertyChanged(nameof(IsDarkTheme));
                    OnPropertyChanged(nameof(ThemeToggleText));
                }
            }
        }

        public bool IsControlView => CurrentView == "control";
        public bool IsDxfView => CurrentView == "dxf";
        public bool IsMonitorView => CurrentView == "monitor";
        public bool IsLogsView => CurrentView == "logs";
        public bool IsSettingsView => CurrentView == "settings";
        public bool IsHelpView => CurrentView == "help";
        public bool IsDarkTheme => CurrentTheme == "dark";
        public string ThemeToggleText => IsDarkTheme ? "☀ Light" : "🌙 Dark";

        public bool IsConnected
        {
            get => isConnected;
            set => SetProperty(ref isConnected, value);
        }

        public bool IsStartActionEnabled
        {
            get => isStartActionEnabled;
            set => SetProperty(ref isStartActionEnabled, value);
        }

        public string ConnectionBanner
        {
            get => connectionBanner;
            set => SetProperty(ref connectionBanner, value);
        }

        public string ConnectionButtonText
        {
            get => connectionButtonText;
            set => SetProperty(ref connectionButtonText, value);
        }

        public string ConnectionMeta
        {
            get => connectionMeta;
            set => SetProperty(ref connectionMeta, value);
        }

        public int LogicalStationInput
        {
            get => logicalStationInput;
            set => SetProperty(ref logicalStationInput, value);
        }

        public string PlcIpAddressInput
        {
            get => plcIpAddressInput;
            set => SetProperty(ref plcIpAddressInput, value);
        }

        public int PlcPortInput
        {
            get => plcPortInput;
            set => SetProperty(ref plcPortInput, value);
        }

        public float JogSpeedD406
        {
            get => jogSpeedD406;
            set => SetProperty(ref jogSpeedD406, value);
        }

        public string JogSpeedInput
        {
            get => jogSpeedInput;
            set
            {
                if (SetProperty(ref jogSpeedInput, value ?? string.Empty) && !suppressJogSpeedInputDirty)
                    jogSpeedInputDirty = true;
            }
        }

        public string ZHeightInput
        {
            get => zHeightInput;
            set => SetProperty(ref zHeightInput, value ?? string.Empty);
        }

        public void SetJogSpeedInputFromPlc(float value)
        {
            if (jogSpeedInputDirty)
                return;

            suppressJogSpeedInputDirty = true;
            try
            {
                JogSpeedInput = DecimalInputParser.FormatFloat(value);
            }
            finally
            {
                suppressJogSpeedInputDirty = false;
            }
        }

        public void AcceptJogSpeedInputAsSynced()
        {
            jogSpeedInputDirty = false;
            SetJogSpeedInputFromPlc(jogSpeedD406);
        }

        public string LaserPowerInput
        {
            get => laserPowerInput;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    SetProperty(ref laserPowerInput, "");
                    return;
                }
                if (int.TryParse(value, out int val))
                {
                    if (val > 2000) value = "2000";
                    else if (val < 0) value = "0";
                }
                else
                {
                    return;
                }
                SetProperty(ref laserPowerInput, value);
            }
        }

        public bool ProgressVisible
        {
            get => progressVisible;
            set => SetProperty(ref progressVisible, value);
        }

        public int ProgressPercent
        {
            get => progressPercent;
            set
            {
                if (SetProperty(ref progressPercent, value))
                    ProgressText = value + "%";
            }
        }

        public string ProgressText
        {
            get => progressText;
            set => SetProperty(ref progressText, value);
        }

        public string FileKind
        {
            get => fileKind;
            set
            {
                if (SetProperty(ref fileKind, value))
                {
                    OnPropertyChanged(nameof(ProgramMonitorTitle));
                    OnPropertyChanged(nameof(ProgramMonitorSubtitle));
                }
            }
        }

        public string FilePath
        {
            get => filePath;
            set => SetProperty(ref filePath, value);
        }

        public string FileName
        {
            get => fileName;
            set
            {
                if (SetProperty(ref fileName, value))
                    OnPropertyChanged(nameof(ProgramMonitorSubtitle));
            }
        }

        public string RawGcodeText
        {
            get => rawGcodeText;
            set => SetProperty(ref rawGcodeText, value);
        }

        public string GlobalSpeedInput
        {
            get => globalSpeedInput;
            set => SetProperty(ref globalSpeedInput, value);
        }

        public string GlobalSpeedM3Input
        {
            get => globalSpeedM3Input;
            set => SetProperty(ref globalSpeedM3Input, value);
        }

        public string GcodeSpeedM3Input
        {
            get => gcodeSpeedM3Input;
            set => SetProperty(ref gcodeSpeedM3Input, value);
        }

        public string RapidSpeedInput
        {
            get => rapidSpeedInput;
            set => SetProperty(ref rapidSpeedInput, value);
        }

        public string TestEngraveSpeedInput
        {
            get => testEngraveSpeedInput;
            set => SetProperty(ref testEngraveSpeedInput, value);
        }

        public string EngraveSpeedInput
        {
            get => engraveSpeedInput;
            set => SetProperty(ref engraveSpeedInput, value);
        }

        public string EngravePowerInput
        {
            get => engravePowerInput;
            set => SetProperty(ref engravePowerInput, value);
        }

        public string CutSpeedInput
        {
            get => cutSpeedInput;
            set => SetProperty(ref cutSpeedInput, value);
        }

        public string CutPowerInput
        {
            get => cutPowerInput;
            set => SetProperty(ref cutPowerInput, value);
        }

        public string GlobalDwellM3Input
        {
            get => globalDwellM3Input;
            set => SetProperty(ref globalDwellM3Input, value);
        }

        public string GlobalDwellM4Input
        {
            get => globalDwellM4Input;
            set => SetProperty(ref globalDwellM4Input, value);
        }

        public double OffsetXInput
        {
            get => offsetXInput;
            set => SetProperty(ref offsetXInput, value);
        }

        public double OffsetYInput
        {
            get => offsetYInput;
            set => SetProperty(ref offsetYInput, value);
        }

        public double WorkspaceWidthInput
        {
            get => workspaceWidthInput;
            set => SetProperty(ref workspaceWidthInput, value);
        }

        public double WorkspaceHeightInput
        {
            get => workspaceHeightInput;
            set => SetProperty(ref workspaceHeightInput, value);
        }

        public string ActiveWcs
        {
            get => activeWcs;
            set => SetProperty(ref activeWcs, value);
        }

        public double WcsOffsetXInput
        {
            get => wcsOffsetXInput;
            set => SetProperty(ref wcsOffsetXInput, value);
        }

        public double WcsOffsetYInput
        {
            get => wcsOffsetYInput;
            set => SetProperty(ref wcsOffsetYInput, value);
        }

        public string ConfigurationFilePathInput
        {
            get => configurationFilePathInput;
            set => SetProperty(ref configurationFilePathInput, value);
        }

        public string WriteAddressInput
        {
            get => writeAddressInput;
            set => SetProperty(ref writeAddressInput, value);
        }

        public int WriteValueInput
        {
            get => writeValueInput;
            set => SetProperty(ref writeValueInput, value);
        }

        public string SelectedPointKey
        {
            get => selectedPointKey;
            set => SetProperty(ref selectedPointKey, value);
        }

        public string ActiveNotice
        {
            get => activeNotice;
            set => SetProperty(ref activeNotice, value);
        }

        public ImageSource CadPreviewImage
        {
            get => cadPreviewImage;
            set => SetProperty(ref cadPreviewImage, value);
        }

        public System.Windows.Media.Geometry CadPreviewGeometry
        {
            get => cadPreviewGeometry;
            set => SetProperty(ref cadPreviewGeometry, value);
        }

        public System.Windows.Media.Geometry CadEngravePreviewGeometry
        {
            get => cadEngravePreviewGeometry;
            set => SetProperty(ref cadEngravePreviewGeometry, value);
        }

        public System.Windows.Media.Geometry CadCutPreviewGeometry
        {
            get => cadCutPreviewGeometry;
            set => SetProperty(ref cadCutPreviewGeometry, value);
        }

        public System.Windows.Media.Geometry CadSelectionOverlayGeometry
        {
            get => cadSelectionOverlayGeometry;
            private set => SetProperty(ref cadSelectionOverlayGeometry, value);
        }

        public Brush CadSelectionOverlayStroke
        {
            get => cadSelectionOverlayStroke;
            private set => SetProperty(ref cadSelectionOverlayStroke, value);
        }

        public CadPathHitIndex CadPathHitIndex
        {
            get => cadPathHitIndex;
            set => SetProperty(ref cadPathHitIndex, value);
        }

        public double CadPreviewStrokeThickness
        {
            get => cadPreviewStrokeThickness;
            set => SetProperty(ref cadPreviewStrokeThickness, value);
        }

        public ImageSource CameraFrame
        {
            get => cameraFrame;
            set => SetProperty(ref cameraFrame, value);
        }

        public string SelectedCameraMoniker
        {
            get => selectedCameraMoniker;
            set
            {
                if (SetProperty(ref selectedCameraMoniker, value))
                {
                    var match = Cameras.FirstOrDefault(c => string.Equals(c.MonikerString, value, StringComparison.OrdinalIgnoreCase));
                    if (!Equals(selectedCamera, match))
                    {
                        selectedCamera = match;
                        OnPropertyChanged(nameof(SelectedCamera));
                    }
                }
            }
        }

        public CameraDeviceViewModel SelectedCamera
        {
            get => selectedCamera;
            set
            {
                if (SetProperty(ref selectedCamera, value))
                {
                    SelectedCameraMoniker = value?.MonikerString ?? string.Empty;
                }
            }
        }

        public string CameraStatus
        {
            get => cameraStatus;
            set => SetProperty(ref cameraStatus, value);
        }

        public string CameraRecordingPath
        {
            get => cameraRecordingPath;
            set => SetProperty(ref cameraRecordingPath, value);
        }

        public string CameraRecordingFolderInput
        {
            get => cameraRecordingFolderInput;
            set => SetProperty(ref cameraRecordingFolderInput, value ?? string.Empty);
        }

        public bool IsCameraRunning
        {
            get => isCameraRunning;
            set => SetProperty(ref isCameraRunning, value);
        }

        public bool IsCameraRecording
        {
            get => isCameraRecording;
            set
            {
                if (SetProperty(ref isCameraRecording, value))
                    OnPropertyChanged(nameof(CameraRecordingText));
            }
        }

        public int CameraRecordedFrames
        {
            get => cameraRecordedFrames;
            set => SetProperty(ref cameraRecordedFrames, value);
        }

        public string CameraRecordingElapsed
        {
            get => cameraRecordingElapsed;
            set
            {
                if (SetProperty(ref cameraRecordingElapsed, value ?? "00:00:00"))
                    OnPropertyChanged(nameof(CameraRecordingText));
            }
        }

        public string CameraRecordingCompletedText
        {
            get => cameraRecordingCompletedText;
            set
            {
                if (SetProperty(ref cameraRecordingCompletedText, value ?? "MP4 recording stopped"))
                    OnPropertyChanged(nameof(CameraRecordingText));
            }
        }

        public string CameraRecordingText => IsCameraRecording
            ? "Recording MP4: " + CameraRecordingElapsed
            : CameraRecordingCompletedText;

        public int ActiveProgramIndex
        {
            get => activeProgramIndex;
            set
            {
                if (SetProperty(ref activeProgramIndex, value))
                {
                    OnPropertyChanged(nameof(ActiveProgramText));
                    OnPropertyChanged(nameof(RunProgressText));
                    OnPropertyChanged(nameof(RunProgressPercent));
                    OnPropertyChanged(nameof(IsPauseContinueEnabled));
                }
            }
        }

        public bool IsPauseContinueEnabled => processRowCount > 0 && ActiveProgramIndex > 0 && ActiveProgramIndex <= processRowCount;

        public string ActiveProgramText => ActiveProgramIndex > 0
            ? "Active data no: " + ActiveProgramIndex
            : "Waiting for PLC data no.";

        private bool HasEngraveCutProgram => hasEngraveCutProgram;

        public bool RunProgressVisible
        {
            get => runProgressVisible && HasEngraveCutProgram;
            set => SetProperty(ref runProgressVisible, value);
        }

        public int RunProgressPercent
        {
            get
            {
                int total = processRowCount;
                if (!HasEngraveCutProgram || total <= 0 || ActiveProgramIndex <= 0)
                    return 0;

                int current = ActiveProgramIndex > total ? total : ActiveProgramIndex;
                return (int)System.Math.Round(current * 100.0 / total);
            }
        }

        public string RunProgressText
        {
            get
            {
                int total = processRowCount;
                if (total <= 0)
                    return "No program loaded";

                int current = ActiveProgramIndex;
                if (current < 0) current = 0;
                if (current > total) current = total;

                return "Running line " + current + " / " + total + " (" + RunProgressPercent + "%)";
            }
        }

        public string ProgramMonitorTitle => "DXF Point Monitor";

        public string ProgramMonitorSubtitle => string.IsNullOrWhiteSpace(FileName)
            ? "Open a DXF file to populate this list"
            : FileName + " - highlight follows Axis 1 current data no.";

        public void SetCadPointRows(IEnumerable<CadPointViewModel> rows, int activeIndex)
        {
            ReplaceList(allCadPoints, rows);
            foreach (var row in allCadPoints)
                row.IsActive = activeIndex > 0 && row.Index == activeIndex;

            ReplaceVisibleRows(CadPoints, allCadPoints, GetInitialVisibleCount(allCadPoints.Count, 0));
        }

        public void SetGeometryRows(IEnumerable<GeometryRowViewModel> rows)
        {
            ReplaceList(allGeometryRows, rows);
            ReplaceVisibleRows(GeometryRows, allGeometryRows, GetInitialVisibleCount(allGeometryRows.Count, 0));
        }

        public void SetProcessRows(
            int totalCount,
            bool hasEngraveCut,
            Func<int, int, IReadOnlyList<ProcessRowViewModel>> windowLoader,
            int activeIndex)
        {
            processRowCount = Math.Max(0, totalCount);
            hasEngraveCutProgram = hasEngraveCut;
            processRowWindowLoader = windowLoader;
            lastHighlightedProgramIndex = 0;

            ProcessRows.ReplaceWith(LoadProcessRowWindow(0, LazyTableBatchSize));
            ReplaceProgramRowsWindow(activeIndex);
            lastHighlightedProgramIndex = activeIndex;
            OnPropertyChanged(nameof(RunProgressVisible));
            OnPropertyChanged(nameof(RunProgressText));
            OnPropertyChanged(nameof(RunProgressPercent));
            OnPropertyChanged(nameof(IsPauseContinueEnabled));
        }

        public void UpdateCadPathStroke(int pathId, bool isCut)
        {
            Brush stroke = isCut ? Brushes.OrangeRed : Brushes.DodgerBlue;
            IReadOnlyList<System.Windows.Point> points;
            if (CadPathHitIndex == null
                || !CadPathHitIndex.TryGetPathPoints(pathId, out points)
                || points == null
                || points.Count < 2)
            {
                ClearCadSelectionOverlay();
                return;
            }

            IReadOnlyList<System.Windows.Point> overlayPoints =
                CadPathPointSampler.Sample(points, MaxCadSelectionOverlayPoints);
            var geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(overlayPoints[0], isFilled: false, isClosed: false);
                for (int i = 1; i < overlayPoints.Count; i++)
                    context.LineTo(overlayPoints[i], isStroked: true, isSmoothJoin: true);
            }
            geometry.Freeze();

            CadSelectionOverlayStroke = stroke;
            CadSelectionOverlayGeometry = geometry;
        }

        public void ClearCadSelectionOverlay()
        {
            CadSelectionOverlayGeometry = null;
            CadSelectionOverlayStroke = Brushes.Transparent;
        }

        public bool LoadMoreCadPoints()
            => AppendNextRows(CadPoints, allCadPoints);

        public bool LoadMoreGeometryRows()
            => AppendNextRows(GeometryRows, allGeometryRows);

        public bool LoadMoreProcessRows()
        {
            int start = ProcessRows.Count == 0 ? 0 : ProcessRows[ProcessRows.Count - 1].Index;
            return AppendProcessRows(ProcessRows, start);
        }

        public bool LoadMoreProgramRows()
        {
            int start = 0;
            if (ProgramRows.Count > 0)
            {
                int lastIndex = ProgramRows[ProgramRows.Count - 1].Index;
                start = Math.Max(0, lastIndex);
            }

            int limit = Math.Min(start + LazyTableBatchSize, processRowCount);
            if (start >= limit)
                return false;

            List<ProcessRowViewModel> rows = LoadProcessRowWindow(start, limit - start);
            ProgramRows.AddRange(rows);
            return rows.Count > 0;
        }

        public void UpdateCadTrackingPoint(CadTrackingPointViewModel point)
        {
            if (point == null)
            {
                CadTrackingPoints.Clear();
                return;
            }

            if (CadTrackingPoints.Count == 0)
            {
                CadTrackingPoints.Add(point);
                return;
            }

            CadTrackingPoints[0].UpdateFrom(point);
            while (CadTrackingPoints.Count > 1)
                CadTrackingPoints.RemoveAt(CadTrackingPoints.Count - 1);
        }

        public void ApplyActiveProgramIndex(int activeIndex, bool ensureProcessVisible)
        {
            ActiveProgramIndex = activeIndex;

            if (lastHighlightedProgramIndex != activeIndex)
            {
                SetProcessRowActive(lastHighlightedProgramIndex, false);
                SetCadPointActive(lastHighlightedProgramIndex, false);
                SetProcessRowActive(activeIndex, true);
                SetCadPointActive(activeIndex, true);
                lastHighlightedProgramIndex = activeIndex;
            }

            if (ensureProcessVisible)
                EnsureProcessRowVisible(activeIndex);
        }

        public bool EnsureProcessRowVisible(int rowIndex)
        {
            if (rowIndex <= 0)
                return false;

            return EnsureProgramRowVisible(rowIndex);
        }

        private bool EnsureProgramRowVisible(int rowIndex)
        {
            if (rowIndex <= 0 || processRowCount == 0)
                return false;

            foreach (var row in ProgramRows)
            {
                if (row.Index == rowIndex)
                    return false;
            }

            ReplaceProgramRowsWindow(rowIndex);
            return true;
        }

        private void ReplaceProgramRowsWindow(int focusIndex)
        {
            int start = 0;
            if (focusIndex > 0)
                start = ((focusIndex - 1) / LazyTableBatchSize) * LazyTableBatchSize;

            if (start >= processRowCount)
                start = Math.Max(0, processRowCount - LazyTableBatchSize);

            List<ProcessRowViewModel> visible =
                LoadProcessRowWindow(start, LazyTableBatchSize);
            ProgramRows.ReplaceWith(visible);
        }

        private bool AppendProcessRows(
            BulkObservableCollection<ProcessRowViewModel> target,
            int start)
        {
            List<ProcessRowViewModel> rows =
                LoadProcessRowWindow(start, LazyTableBatchSize);
            if (rows.Count == 0)
                return false;

            target.AddRange(rows);
            return true;
        }

        private List<ProcessRowViewModel> LoadProcessRowWindow(int start, int count)
        {
            start = Math.Max(0, start);
            int available = Math.Max(0, processRowCount - start);
            int requested = Math.Min(Math.Max(0, count), available);
            var result = new List<ProcessRowViewModel>(requested);
            if (requested == 0 || processRowWindowLoader == null)
                return result;

            IReadOnlyList<ProcessRowViewModel> loaded =
                processRowWindowLoader(start, requested);
            if (loaded == null)
                return result;

            for (int i = 0; i < loaded.Count && result.Count < requested; i++)
            {
                ProcessRowViewModel row = loaded[i];
                if (row == null)
                    continue;
                row.IsActive = ActiveProgramIndex > 0 && row.Index == ActiveProgramIndex;
                result.Add(row);
            }

            return result;
        }

        private static void ReplaceList<T>(List<T> target, IEnumerable<T> source)
        {
            target.Clear();
            if (source == null)
                return;

            foreach (T item in source)
            {
                if (item != null)
                    target.Add(item);
            }
        }

        private static void ReplaceVisibleRows<T>(BulkObservableCollection<T> target, List<T> source, int count)
        {
            var visible = new List<T>();
            int limit = Math.Min(count, source.Count);
            for (int i = 0; i < limit; i++)
                visible.Add(source[i]);

            target.ReplaceWith(visible);
        }

        private static bool AppendNextRows<T>(BulkObservableCollection<T> target, List<T> source)
        {
            return AppendRowsToCount(target, source, target.Count + LazyTableBatchSize);
        }

        private static bool AppendRowsToCount<T>(BulkObservableCollection<T> target, List<T> source, int targetCount)
        {
            int start = target.Count;
            int limit = Math.Min(targetCount, source.Count);
            if (start >= limit)
                return false;

            var rows = new List<T>();
            for (int i = start; i < limit; i++)
                rows.Add(source[i]);

            target.AddRange(rows);
            return true;
        }

        private static int GetInitialVisibleCount(int totalCount, int focusIndex)
        {
            int count = Math.Min(totalCount, LazyTableBatchSize);
            if (focusIndex > count)
            {
                int focusedBatch = ((focusIndex + LazyTableBatchSize - 1) / LazyTableBatchSize) * LazyTableBatchSize;
                count = Math.Min(totalCount, focusedBatch);
            }

            return count;
        }

        private void SetProcessRowActive(int index, bool isActive)
        {
            if (index <= 0)
                return;

            SetProcessRowActive(ProcessRows, index, isActive);
            SetProcessRowActive(ProgramRows, index, isActive);
        }

        private static void SetProcessRowActive(
            IEnumerable<ProcessRowViewModel> rows,
            int index,
            bool isActive)
        {
            foreach (ProcessRowViewModel row in rows)
            {
                if (row.Index == index)
                {
                    row.IsActive = isActive;
                    return;
                }
            }
        }

        private void SetCadPointActive(int index, bool isActive)
        {
            if (index <= 0)
                return;

            CadPointViewModel point = GetIndexedRow(allCadPoints, index);
            if (point != null)
                point.IsActive = isActive;
        }

        private static CadPointViewModel GetIndexedRow(List<CadPointViewModel> rows, int index)
        {
            int offset = index - 1;
            if (offset >= 0 && offset < rows.Count && rows[offset].Index == index)
                return rows[offset];

            foreach (var row in rows)
            {
                if (row.Index == index)
                    return row;
            }

            return null;
        }
    }

    public class ObservableState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class AxisStatusViewModel : ObservableState
    {
        private int index;
        private string currentPos = "--";
        private string currentPosAddr = "";
        private string currentSpeed = "--";
        private string currentSpeedAddr = "";
        private string mCode = "--";
        private string mCodeAddr = "";
        private string errorCode = "--";
        private string errorCodeAddr = "";
        private string errorDescription = "";
        private string warningCode = "--";
        private string warningCodeAddr = "";
        private string warningDescription = "";
        private string axisStatus = "--";
        private string axisStatusAddr = "";
        private string currentDataNo = "--";
        private string currentDataNoAddr = "";
        private string lastDataNo = "--";
        private string lastDataNoAddr = "";
        private bool limitMinus;
        private bool limitPlus;
        private bool homeDog;
        private bool isComplete;

        public int Index { get => index; set => SetProperty(ref index, value); }
        public string Name => "Axis " + Index;
        public string CurrentPos { get => currentPos; set => SetProperty(ref currentPos, value); }
        public string CurrentPosAddr { get => currentPosAddr; set => SetProperty(ref currentPosAddr, value); }
        public string CurrentSpeed { get => currentSpeed; set => SetProperty(ref currentSpeed, value); }
        public string CurrentSpeedAddr { get => currentSpeedAddr; set => SetProperty(ref currentSpeedAddr, value); }
        public string MCode { get => mCode; set => SetProperty(ref mCode, value); }
        public string MCodeAddr { get => mCodeAddr; set => SetProperty(ref mCodeAddr, value); }
        public string ErrorCode { get => errorCode; set => SetProperty(ref errorCode, value); }
        public string ErrorCodeAddr { get => errorCodeAddr; set => SetProperty(ref errorCodeAddr, value); }
        public string ErrorDescription { get => errorDescription; set => SetProperty(ref errorDescription, value); }
        public string WarningCode { get => warningCode; set => SetProperty(ref warningCode, value); }
        public string WarningCodeAddr { get => warningCodeAddr; set => SetProperty(ref warningCodeAddr, value); }
        public string WarningDescription { get => warningDescription; set => SetProperty(ref warningDescription, value); }
        public string AxisStatus { get => axisStatus; set => SetProperty(ref axisStatus, value); }
        public string AxisStatusAddr { get => axisStatusAddr; set => SetProperty(ref axisStatusAddr, value); }
        public string CurrentDataNo { get => currentDataNo; set => SetProperty(ref currentDataNo, value); }
        public string CurrentDataNoAddr { get => currentDataNoAddr; set => SetProperty(ref currentDataNoAddr, value); }
        public string LastDataNo { get => lastDataNo; set => SetProperty(ref lastDataNo, value); }
        public string LastDataNoAddr { get => lastDataNoAddr; set => SetProperty(ref lastDataNoAddr, value); }
        public bool LimitMinus { get => limitMinus; set => SetProperty(ref limitMinus, value); }
        public bool LimitPlus { get => limitPlus; set => SetProperty(ref limitPlus, value); }
        public bool HomeDog { get => homeDog; set => SetProperty(ref homeDog, value); }
        public bool IsComplete { get => isComplete; set => SetProperty(ref isComplete, value); }
    }

    public sealed class LogRowViewModel
    {
        public string Timestamp { get; set; }
        public string Direction { get; set; }
        public string Address { get; set; }
        public string Value { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public sealed class UiEventViewModel
    {
        public string Time { get; set; }
        public string Kind { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }

    public sealed class CameraDeviceViewModel
    {
        public string Name { get; set; }
        public string MonikerString { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Camera" : Name;

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed class CadPointViewModel : ObservableState
    {
        private bool isActive;

        public int Index { get; set; }
        public string LineType { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
        public string Z { get; set; }
        public string Key { get; set; }
        public bool IsActive
        {
            get => isActive;
            set
            {
                if (SetProperty(ref isActive, value))
                    OnPropertyChanged(nameof(ActiveMarker));
            }
        }
        public string ActiveMarker => IsActive ? "RUN" : string.Empty;
    }

    public sealed class GeometryRowViewModel
    {
        public int Index { get; set; }
        public string LineType { get; set; }
        public string StartX { get; set; }
        public string StartY { get; set; }
        public string StartZ { get; set; }
        public string EndX { get; set; }
        public string EndY { get; set; }
        public string EndZ { get; set; }
        public string CenterX { get; set; }
        public string CenterY { get; set; }
        public string CenterZ { get; set; }
        public string Key { get; set; }
    }

    public sealed class ProcessRowViewModel : ObservableState
    {
        private bool isActive;

        public int Index { get; set; }
        public string Key { get; set; }
        public string MotionType { get; set; }
        public string MCodeValue { get; set; }
        public string Dwell { get; set; }
        public string Speed { get; set; }
        public string ProcessKind { get; set; }
        public string LaserPower { get; set; }
        public string EndCoordinate { get; set; }
        public string CenterCoordinate { get; set; }
        public string EndZ { get; set; }
        public string ProcessKindLabel
        {
            get
            {
                if (string.Equals(ProcessKind, EngraveCutProcessComposer.EngraveKind, StringComparison.OrdinalIgnoreCase))
                    return "Engrave";
                if (string.Equals(ProcessKind, EngraveCutProcessComposer.CutKind, StringComparison.OrdinalIgnoreCase))
                    return "Cut";
                return string.Empty;
            }
        }

        public bool IsActive
        {
            get => isActive;
            set
            {
                if (SetProperty(ref isActive, value))
                    OnPropertyChanged(nameof(ActiveMarker));
            }
        }
        public string ActiveMarker => IsActive ? "RUN" : string.Empty;
    }

    public sealed class WcsOffsetViewModel : ObservableState
    {
        private string name;
        private double offsetX;
        private double offsetY;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public double OffsetX
        {
            get => offsetX;
            set => SetProperty(ref offsetX, value);
        }

        public double OffsetY
        {
            get => offsetY;
            set => SetProperty(ref offsetY, value);
        }
    }

    public sealed class CadPrimitiveViewModel : ObservableState
    {
        private Brush stroke;

        public int PathId { get; set; }
        public PointCollection Points { get; set; }
        public Brush Stroke
        {
            get => stroke;
            set => SetProperty(ref stroke, value);
        }
        public double StrokeThickness { get; set; }
    }

    public sealed class CadAxisLineViewModel
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public Brush Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public double Opacity { get; set; } = 1.0;
    }

    public sealed class CadAxisLabelViewModel
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Text { get; set; }
        public Brush Foreground { get; set; }
    }

    public sealed class CadTrackingPointViewModel : ObservableState
    {
        private double x;
        private double y;
        private double size;
        private Brush fill;
        private Brush stroke;
        private string label;
        private string toolTip;

        public double X { get => x; set => SetProperty(ref x, value); }
        public double Y { get => y; set => SetProperty(ref y, value); }
        public double Size { get => size; set => SetProperty(ref size, value); }
        public Brush Fill { get => fill; set => SetProperty(ref fill, value); }
        public Brush Stroke { get => stroke; set => SetProperty(ref stroke, value); }
        public string Label { get => label; set => SetProperty(ref label, value); }
        public string ToolTip { get => toolTip; set => SetProperty(ref toolTip, value); }

        public void UpdateFrom(CadTrackingPointViewModel source)
        {
            if (source == null)
                return;

            X = source.X;
            Y = source.Y;
            Size = source.Size;
            Fill = source.Fill;
            Stroke = source.Stroke;
            Label = source.Label;
            ToolTip = source.ToolTip;
        }
    }

    public sealed class CadLimitAreaViewModel
    {
        public PointCollection Points { get; set; }
        public Brush Stroke { get; set; }
        public Brush Fill { get; set; }
        public double StrokeThickness { get; set; }
        public DoubleCollection StrokeDashArray { get; set; }
    }
}
