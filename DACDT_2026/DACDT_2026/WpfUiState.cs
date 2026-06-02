using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private string currentView = "control";
        private string currentTheme = "dark";
        private bool isConnected;
        private string connectionBanner = "PLC disconnected";
        private string connectionButtonText = "CONNECT PLC Q";
        private string connectionMeta = "MX Component logical station: 0";
        private int logicalStationInput;
        private string plcIpAddressInput = "192.168.3.39";
        private int plcPortInput = 3000;
        private float jogSpeedD406 = 1000f;
        private double jogSpeedInput = 1000.0;
        private string progressText = "0%";
        private bool progressVisible;
        private int progressPercent;
        private string fileKind = "";
        private string filePath = "";
        private string fileName = "";
        private string rawGcodeText = "";
        private string globalSpeedInput = "1000";
        private string globalSpeedM3Input = "10000";
        private string rapidSpeedInput = "10000";
        private string globalDwellM3Input = "100";
        private string globalDwellM4Input = "100";
        private double offsetXInput;
        private double offsetYInput;
        private double workspaceWidthInput = 170.0;
        private double workspaceHeightInput = 170.0;
        private string activeWcs = "G54";
        private double wcsOffsetXInput;
        private double wcsOffsetYInput;
        private string profileNameInput = "";
        private string selectedProfile = "";
        private string telemetryAddressInput = "D100";
        private int telemetryLengthInput = 1;
        private string writeAddressInput = "D100";
        private int writeValueInput = 12345;
        private string selectedPointKey = "";
        private string activeNotice = "";
        private int activeProgramIndex;

        public WpfUiState()
        {
            for (int i = 1; i <= 3; i++)
                Axes.Add(new AxisStatusViewModel { Index = i });
        }

        public BulkObservableCollection<AxisStatusViewModel> Axes { get; } = new BulkObservableCollection<AxisStatusViewModel>();
        public BulkObservableCollection<LogRowViewModel> Logs { get; } = new BulkObservableCollection<LogRowViewModel>();
        public BulkObservableCollection<UiEventViewModel> Events { get; } = new BulkObservableCollection<UiEventViewModel>();
        public BulkObservableCollection<TelemetryRegisterViewModel> TelemetryRegisters { get; } = new BulkObservableCollection<TelemetryRegisterViewModel>();
        public BulkObservableCollection<TelemetryBufferViewModel> TelemetryBuffers { get; } = new BulkObservableCollection<TelemetryBufferViewModel>();
        public BulkObservableCollection<CadPointViewModel> CadPoints { get; } = new BulkObservableCollection<CadPointViewModel>();
        public BulkObservableCollection<GeometryRowViewModel> GeometryRows { get; } = new BulkObservableCollection<GeometryRowViewModel>();
        public BulkObservableCollection<ProcessRowViewModel> ProcessRows { get; } = new BulkObservableCollection<ProcessRowViewModel>();
        public BulkObservableCollection<CadPrimitiveViewModel> CadPrimitives { get; } = new BulkObservableCollection<CadPrimitiveViewModel>();
        public BulkObservableCollection<CadLimitAreaViewModel> CadLimitAreas { get; } = new BulkObservableCollection<CadLimitAreaViewModel>();
        public BulkObservableCollection<CadAxisLineViewModel> CadAxisLines { get; } = new BulkObservableCollection<CadAxisLineViewModel>();
        public BulkObservableCollection<CadAxisLabelViewModel> CadAxisLabels { get; } = new BulkObservableCollection<CadAxisLabelViewModel>();
        public BulkObservableCollection<CadTrackingPointViewModel> CadTrackingPoints { get; } = new BulkObservableCollection<CadTrackingPointViewModel>();
        public BulkObservableCollection<WcsOffsetViewModel> WcsOffsets { get; } = new BulkObservableCollection<WcsOffsetViewModel>();
        public BulkObservableCollection<string> Profiles { get; } = new BulkObservableCollection<string>();

        public ICommand SwitchViewCommand { get; set; }
        public ICommand ToggleThemeCommand { get; set; }
        public ICommand ConnectToggleCommand { get; set; }
        public ICommand EmergencyStopCommand { get; set; }
        public ICommand ExitCommand { get; set; }
        public ICommand JogStartCommand { get; set; }
        public ICommand JogStopCommand { get; set; }
        public ICommand GoHomeStartCommand { get; set; }
        public ICommand GoHomeStopCommand { get; set; }
        public ICommand ResetErrorStartCommand { get; set; }
        public ICommand ResetErrorStopCommand { get; set; }
        public ICommand StartActionStartCommand { get; set; }
        public ICommand StartActionStopCommand { get; set; }
        public ICommand ContinueStartCommand { get; set; }
        public ICommand ContinueStopCommand { get; set; }
        public ICommand PauseStartCommand { get; set; }
        public ICommand PauseStopCommand { get; set; }
        public ICommand SetJogSpeedCommand { get; set; }
        public ICommand OpenDxfCommand { get; set; }
        public ICommand NewGcodeCommand { get; set; }
        public ICommand SaveGcodeCommand { get; set; }
        public ICommand PreviewGcodeCommand { get; set; }
        public ICommand ClearBufferCommand { get; set; }
        public ICommand SendCadXCommand { get; set; }
        public ICommand ClearLogsCommand { get; set; }
        public ICommand AddTelemetryRegisterCommand { get; set; }
        public ICommand AddTelemetryBufferCommand { get; set; }
        public ICommand WriteBufferCommand { get; set; }
        public ICommand ApplyDxfSettingsCommand { get; set; }
        public ICommand SetG0SpeedCommand { get; set; }
        public ICommand SetWorkspaceCommand { get; set; }
        public ICommand SelectWcsCommand { get; set; }
        public ICommand SetWcsCommand { get; set; }
        public ICommand ApplyPlcConnectionCommand { get; set; }
        public ICommand SaveProfileCommand { get; set; }
        public ICommand LoadProfileCommand { get; set; }
        public ICommand DeleteProfileCommand { get; set; }

        public string CurrentView
        {
            get => currentView;
            set
            {
                if (SetProperty(ref currentView, value))
                {
                    OnPropertyChanged(nameof(IsControlView));
                    OnPropertyChanged(nameof(IsDxfView));
                    OnPropertyChanged(nameof(IsTelemetryView));
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
                    OnPropertyChanged(nameof(IsDarkTheme));
            }
        }

        public bool IsControlView => CurrentView == "control";
        public bool IsDxfView => CurrentView == "dxf";
        public bool IsTelemetryView => CurrentView == "telemetry";
        public bool IsLogsView => CurrentView == "logs";
        public bool IsSettingsView => CurrentView == "settings";
        public bool IsHelpView => CurrentView == "help";
        public bool IsDarkTheme => CurrentTheme == "dark";

        public bool IsConnected
        {
            get => isConnected;
            set => SetProperty(ref isConnected, value);
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

        public double JogSpeedInput
        {
            get => jogSpeedInput;
            set => SetProperty(ref jogSpeedInput, value);
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

        public string RapidSpeedInput
        {
            get => rapidSpeedInput;
            set => SetProperty(ref rapidSpeedInput, value);
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

        public string ProfileNameInput
        {
            get => profileNameInput;
            set => SetProperty(ref profileNameInput, value);
        }

        public string SelectedProfile
        {
            get => selectedProfile;
            set => SetProperty(ref selectedProfile, value);
        }

        public string TelemetryAddressInput
        {
            get => telemetryAddressInput;
            set => SetProperty(ref telemetryAddressInput, value);
        }

        public int TelemetryLengthInput
        {
            get => telemetryLengthInput;
            set => SetProperty(ref telemetryLengthInput, value);
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

        public int ActiveProgramIndex
        {
            get => activeProgramIndex;
            set
            {
                if (SetProperty(ref activeProgramIndex, value))
                    OnPropertyChanged(nameof(ActiveProgramText));
            }
        }

        public string ActiveProgramText => ActiveProgramIndex > 0
            ? "Active data no: " + ActiveProgramIndex
            : "Waiting for PLC data no.";

        public string ProgramMonitorTitle
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FileKind))
                    return "Program Monitor";

                return string.Equals(FileKind, "GCODE", StringComparison.OrdinalIgnoreCase)
                    ? "G-code Monitor"
                    : "DXF Point Monitor";
            }
        }

        public string ProgramMonitorSubtitle => string.IsNullOrWhiteSpace(FileName)
            ? "Open a G-code or DXF file to populate this list"
            : FileName + " - highlight follows Axis 1 current data no.";
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
        private string warningCode = "--";
        private string warningCodeAddr = "";
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
        public string WarningCode { get => warningCode; set => SetProperty(ref warningCode, value); }
        public string WarningCodeAddr { get => warningCodeAddr; set => SetProperty(ref warningCodeAddr, value); }
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

    public sealed class TelemetryRegisterViewModel
    {
        public string Register { get; set; }
        public string Value { get; set; }
        public string Status { get; set; }
    }

    public sealed class TelemetryBufferViewModel
    {
        public string Path { get; set; }
        public string Values { get; set; }
        public string Status { get; set; }
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
        public string EndCoordinate { get; set; }
        public string CenterCoordinate { get; set; }
        public string EndZ { get; set; }
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

    public sealed class CadPrimitiveViewModel
    {
        public PointCollection Points { get; set; }
        public Brush Stroke { get; set; }
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

    public sealed class CadTrackingPointViewModel
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Size { get; set; }
        public Brush Fill { get; set; }
        public Brush Stroke { get; set; }
        public string Label { get; set; }
        public string ToolTip { get; set; }
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
