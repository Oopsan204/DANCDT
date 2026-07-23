using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Video;
using AForge.Video.DirectShow;

namespace DACDT_2026
{
    /// <summary>
    /// Form1 — Camera handlers: device enumeration, start/stop, frame capture, recording.
    /// Uses AForge Video library with DirectShow for webcam access.
    /// </summary>
    public partial class Form1
    {
        private IVideoSource cameraSource;
        private FilterInfoCollection cameraDevices;
        private string activeCameraMoniker = string.Empty;
        private string cameraRecordingDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "camera_recordings");
        private int recordedFrameCount;
        private volatile bool cameraRecordingActive;
        private string activeCameraRecordingPath = string.Empty;
        private DateTime cameraRecordingStartedUtc = DateTime.MinValue;
        private DispatcherTimer cameraRecordingDurationTimer;
        private CameraVideoRecorder cameraVideoRecorder;
        private readonly IntervalGate cameraRecordingGate = new IntervalGate(PerformanceTuning.CameraRecordingFrameIntervalMs);
        private readonly SingleFlightGate cameraRecordingSaveGate = new SingleFlightGate();
        private object cameraLock = new object();
        private readonly IntervalGate cameraPreviewGate = new IntervalGate(PerformanceTuning.CameraPreviewIntervalMs);
        private readonly JavaScriptSerializer cameraStatusSerializer = new JavaScriptSerializer();

        private void InitializeCameraRecordingDurationTimer()
        {
            cameraRecordingDurationTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            cameraRecordingDurationTimer.Tick += CameraRecordingDurationTimer_Tick;
        }

        private void CameraRecordingDurationTimer_Tick(object sender, EventArgs e)
        {
            DateTime startedUtc;
            lock (cameraLock)
            {
                if (!cameraRecordingActive || cameraRecordingStartedUtc == DateTime.MinValue)
                {
                    cameraRecordingDurationTimer.Stop();
                    return;
                }

                startedUtc = cameraRecordingStartedUtc;
            }

            ui.CameraRecordingElapsed = CameraRecordingSummary.FormatElapsed(DateTime.UtcNow - startedUtc);
        }

        private void StartCameraRecordingDurationTimer()
        {
            if (cameraRecordingDurationTimer == null || !cameraRecordingActive)
                return;

            CameraRecordingDurationTimer_Tick(this, EventArgs.Empty);
            cameraRecordingDurationTimer.Start();
        }

        private Task PublishCameraStatusAsync(bool running, string state, string message = null)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Refresh available camera devices and populate the UI list.
        /// </summary>
        private async Task RefreshCamerasAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (cameraLock)
                    {
                        cameraDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                        
                        var cameras = new List<CameraDeviceViewModel>();
                        foreach (FilterInfo device in cameraDevices)
                        {
                            cameras.Add(new CameraDeviceViewModel
                            {
                                Name = device.Name,
                                MonikerString = device.MonikerString
                            });
                        }

                        Dispatcher.Invoke(() =>
                        {
                            ui.Cameras.ReplaceWith(cameras);

                            if (cameras.Count > 0)
                            {
                                var selectionModels = cameras.Select(c => new CameraDeviceSelection.CameraDevice(c.Name, c.MonikerString)).ToList();
                                var selectedModel = CameraDeviceSelection.FindByMonikerOrPreferred(selectionModels, ui.SelectedCameraMoniker);
                                var selected = cameras.FirstOrDefault(c => string.Equals(c.MonikerString, selectedModel?.MonikerString, StringComparison.OrdinalIgnoreCase)) ?? cameras[0];
                                ui.SelectedCamera = selected;
                                ui.CameraStatus = $"Found {cameras.Count} camera(s). Ready to start.";
                                try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Camera] Found " + cameras.Count + " camera(s). Selected: " + selected.DisplayName + " [" + selected.MonikerString + "]\r\n"); } catch { }
                            }
                            else
                            {
                                ui.CameraStatus = "No cameras found.";
                                try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Camera] No cameras found.\r\n"); } catch { }
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    ui.CameraStatus = $"Error refreshing cameras: {ex.Message}";


                    try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                        "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Camera] Error refreshing cameras: " + ex.Message + "\r\n" + ex.StackTrace + "\r\n"); } catch { }
                }
            });
        }

        /// <summary>
        /// Start the selected camera and begin capturing frames to the UI.
        /// </summary>
        private async Task StartCameraAsync()
        {
            await StartCameraAsync(forceRestart: false);
        }

        private async Task StartCameraAsync(bool forceRestart)
        {
            string selectedMoniker = GetSelectedCameraMonikerOnUiThread();
            string selectedName = GetSelectedCameraNameOnUiThread();

            await Task.Run(() =>
            {
                try
                {
                    lock (cameraLock)
                    {
                        try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                            "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Camera] Starting camera: '" + selectedName + "' [" + selectedMoniker + "] forceRestart=" + forceRestart + "\r\n"); } catch { }

                        if (string.IsNullOrWhiteSpace(selectedMoniker))
                        {
                            SetCameraStatusOnUiThread("No camera selected.");
                            return;
                        }

                        if (cameraSource != null && cameraSource.IsRunning)
                        {
                            bool switchCamera = CameraDeviceSelection.ShouldSwitch(activeCameraMoniker, selectedMoniker);
                            if (!forceRestart && !switchCamera)
                            {
                                _ = PublishCameraStatusAsync(true, "running", "camera already running");
                                SetCameraRunningUiState(true, "Camera already running: " + selectedName, clearFrame: false);
                                return;
                            }

                            StopCameraSourceLocked();
                        }
                        else if (cameraSource != null)
                        {
                            StopCameraSourceLocked();
                        }

                        var videoDevice = new VideoCaptureDevice(selectedMoniker);
                        videoDevice.NewFrame += CameraSource_NewFrame;
                        videoDevice.VideoSourceError += CameraSource_VideoSourceError;
                        cameraSource = videoDevice;
                        activeCameraMoniker = selectedMoniker;
                        cameraSource.Start();
                        _ = PublishCameraStatusAsync(true, "running", "camera started: " + selectedName);

                        recordedFrameCount = 0;
                        cameraPreviewGate.Reset();
                        SetCameraRunningUiState(true, "Camera started: " + selectedName, clearFrame: true);
                    }
                }
                catch (Exception ex)
                {
                    SetCameraRunningUiState(false, $"Error starting camera: {ex.Message}", clearFrame: false);
                    _ = PublishCameraStatusAsync(false, "error", ex.Message);
                    try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                        "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Camera] Error starting camera: " + ex.Message + "\r\n" + ex.StackTrace + "\r\n"); } catch { }
                }
            });
        }

        /// <summary>
        /// Stop the currently running camera.
        /// </summary>
        private async Task StopCameraAsync()
        {
            if (cameraRecordingActive)
                await StopCameraRecordingAsync();

            await Task.Run(() =>
            {
                try
                {
                    CameraVideoRecorder recorderToClose;
                    lock (cameraLock)
                    {
                        StopCameraSourceLocked();
                        _ = PublishCameraStatusAsync(false, "stopped", "camera stopped");

                        cameraRecordingActive = false;
                        cameraRecordingStartedUtc = DateTime.MinValue;
                        ui.IsCameraRecording = false;
                        recorderToClose = cameraVideoRecorder;
                        cameraVideoRecorder = null;
                        activeCameraRecordingPath = string.Empty;
                        ui.CameraRecordedFrames = 0;
                        cameraPreviewGate.Reset();
                        SetCameraRunningUiState(false, "Camera stopped.", clearFrame: true);
                    }

                    recorderToClose?.Complete();
                    Dispatcher?.BeginInvoke(new Action(() => cameraRecordingDurationTimer?.Stop()));
                }
                catch (Exception ex)
                {
                    ui.CameraStatus = $"Error stopping camera: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Core method to stop camera without async wrapper (called from OnClosing).
        /// </summary>
        private void StopCameraCore()
        {
            CameraVideoRecorder recorderToClose = null;
            try
            {
                lock (cameraLock)
                {
                    StopCameraSourceLocked();
                    _ = PublishCameraStatusAsync(false, "stopped", "camera stopped");

                    cameraRecordingActive = false;
                    cameraRecordingStartedUtc = DateTime.MinValue;
                    ui.IsCameraRecording = false;
                    recorderToClose = cameraVideoRecorder;
                    cameraVideoRecorder = null;
                    activeCameraRecordingPath = string.Empty;
                    ui.IsCameraRunning = false;
                    recordedFrameCount = 0;
                    ui.CameraRecordedFrames = 0;
                    cameraPreviewGate.Reset();
                }

                recorderToClose?.Complete();
                cameraRecordingDurationTimer?.Stop();
            }
            catch { }
        }

        private void StopCameraSourceLocked()
        {
            if (cameraSource == null)
            {
                activeCameraMoniker = string.Empty;
                return;
            }

            try { cameraSource.VideoSourceError -= CameraSource_VideoSourceError; } catch { }
            try { cameraSource.NewFrame -= CameraSource_NewFrame; } catch { }

            try
            {
                if (cameraSource.IsRunning)
                {
                    cameraSource.SignalToStop();
                    cameraSource.WaitForStop();
                }
            }
            catch { }

            cameraSource = null;
            activeCameraMoniker = string.Empty;
        }

        private string GetSelectedCameraMonikerOnUiThread()
        {
            try
            {
                if (Dispatcher == null || Dispatcher.CheckAccess())
                    return ui.SelectedCamera?.MonikerString ?? ui.SelectedCameraMoniker;

                return Dispatcher.Invoke(() => ui.SelectedCamera?.MonikerString ?? ui.SelectedCameraMoniker);
            }
            catch
            {
                return ui.SelectedCameraMoniker;
            }
        }

        private string GetSelectedCameraNameOnUiThread()
        {
            try
            {
                if (Dispatcher == null || Dispatcher.CheckAccess())
                    return ui.SelectedCamera?.DisplayName ?? "selected camera";

                return Dispatcher.Invoke(() => ui.SelectedCamera?.DisplayName ?? "selected camera");
            }
            catch
            {
                return "selected camera";
            }
        }

        private void SetCameraStatusOnUiThread(string status)
        {
            try
            {
                Dispatcher?.BeginInvoke(new Action(() => ui.CameraStatus = status));
            }
            catch
            {
                ui.CameraStatus = status;
            }
        }

        private async Task BrowseCameraRecordingFolderAsync()
        {
            await Task.Yield();

            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select the camera recording folder";
                dialog.SelectedPath = Directory.Exists(cameraRecordingDir)
                    ? cameraRecordingDir
                    : AppDomain.CurrentDomain.BaseDirectory;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ui.CameraRecordingFolderInput = dialog.SelectedPath;
                    await SetCameraRecordingFolderAsync(dialog.SelectedPath);
                }
            }
        }

        private async Task SetCameraRecordingFolderAsync(string path)
        {
            await Task.Yield();

            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    ui.CameraStatus = "Recording folder is required.";
                    return;
                }

                string normalizedPath = Path.GetFullPath(path.Trim());
                Directory.CreateDirectory(normalizedPath);
                cameraRecordingDir = normalizedPath;
                ui.CameraRecordingFolderInput = normalizedPath;
                SaveSettingsToFile();
                ui.CameraStatus = "Recording folder set.";
            }
            catch (Exception ex)
            {
                ui.CameraStatus = "Recording folder error: " + ex.Message;
            }
        }

        private void SetCameraRunningUiState(bool running, string status, bool clearFrame)
        {
            try
            {
                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    ui.IsCameraRunning = running;
                    ui.CameraStatus = status;
                    ui.CameraRecordedFrames = running ? 0 : ui.CameraRecordedFrames;
                    if (clearFrame)
                        ui.CameraFrame = null;
                }));
            }
            catch
            {
                ui.IsCameraRunning = running;
                ui.CameraStatus = status;
                if (clearFrame)
                    ui.CameraFrame = null;
            }
        }

        /// <summary>
        /// Start recording camera frames to disk. Frames are throttled and saved by a
        /// background worker to avoid keeping large Bitmap lists in memory.
        /// </summary>
        private async Task StartCameraRecordingAsync()
        {
            string requestedDirectory = ui.CameraRecordingFolderInput;
            await Task.Run(() =>
            {
                try
                {
                    lock (cameraLock)
                    {
                        if (cameraSource == null || !cameraSource.IsRunning)
                        {
                            ui.CameraStatus = "Camera is not running.";
                            return;
                        }

                        if (ui.IsCameraRecording)
                        {
                            ui.CameraStatus = "Recording already in progress.";
                            return;
                        }

                        string normalizedDirectory = string.IsNullOrWhiteSpace(requestedDirectory)
                            ? cameraRecordingDir
                            : Path.GetFullPath(requestedDirectory.Trim());
                        Directory.CreateDirectory(normalizedDirectory);
                        cameraRecordingDir = normalizedDirectory;
                        ui.CameraRecordingFolderInput = normalizedDirectory;
                        SaveSettingsToFile();
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
                        string recordingPath = Path.Combine(cameraRecordingDir, $"camera_{timestamp}.mp4");

                        cameraVideoRecorder = new CameraVideoRecorder(recordingPath, 10, 2000000);
                        activeCameraRecordingPath = recordingPath;
                        cameraRecordingStartedUtc = DateTime.UtcNow;
                        cameraRecordingActive = true;
                        cameraRecordingGate.Reset();
                        ui.CameraRecordingPath = recordingPath;
                        ui.IsCameraRecording = true;
                        recordedFrameCount = 0;
                        ui.CameraRecordedFrames = 0;
                        ui.CameraRecordingElapsed = "00:00:00";
                        ui.CameraRecordingCompletedText = "MP4 recording stopped";
                        ui.CameraStatus = $"Recording MP4: {Path.GetFileName(recordingPath)}";
                    }
                }
                catch (Exception ex)
                {
                    ui.IsCameraRecording = false;
                    cameraRecordingActive = false;
                    ui.CameraStatus = $"Error starting recording: {ex.Message}";
                }
            });

            StartCameraRecordingDurationTimer();
        }

        /// <summary>
        /// Stop recording camera frames. Frames are already being saved incrementally.
        /// </summary>
        private async Task StopCameraRecordingAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    CameraVideoRecorder recorderToClose;
                    string recordingPath;
                    DateTime recordingStartedUtc;
                    lock (cameraLock)
                    {
                        cameraRecordingActive = false;
                        ui.IsCameraRecording = false;
                        recorderToClose = cameraVideoRecorder;
                        cameraVideoRecorder = null;
                        recordingPath = activeCameraRecordingPath;
                        recordingStartedUtc = cameraRecordingStartedUtc;
                        cameraRecordingStartedUtc = DateTime.MinValue;
                        activeCameraRecordingPath = string.Empty;
                        recordedFrameCount = 0;
                    }

                    recorderToClose?.Complete();
                    TimeSpan recordingElapsed = recordingStartedUtc == DateTime.MinValue
                        ? TimeSpan.Zero
                        : DateTime.UtcNow - recordingStartedUtc;
                    long fileSize = File.Exists(recordingPath) ? new FileInfo(recordingPath).Length : 0;
                    string savedText = CameraRecordingSummary.FormatSavedText(recordingElapsed, fileSize);

                    Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        cameraRecordingDurationTimer?.Stop();
                        ui.CameraRecordedFrames = 0;
                        ui.CameraRecordingElapsed = CameraRecordingSummary.FormatElapsed(recordingElapsed);
                        ui.CameraRecordingCompletedText = savedText;
                        ui.CameraStatus = savedText + ": " + Path.GetFileName(recordingPath);
                    }));
                }
                catch (Exception ex)
                {
                    SetCameraStatusOnUiThread($"Error stopping recording: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Handle new frames from the camera source.
        /// Convert to BitmapImage and update UI; optionally store frame if recording.
        /// </summary>
        private void CameraSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = null;
            try
            {
                if (isClosing)
                    return;

                bitmap = (Bitmap)eventArgs.Frame.Clone();
                var nowUtc = DateTime.UtcNow;

                if (ShouldSaveRecordingFrame(nowUtc))
                {
                    var recordingBitmap = (Bitmap)bitmap.Clone();
                    _ = Task.Run(() => SaveCameraRecordingFrame(recordingBitmap));
                }

                if (ShouldUpdateCameraPreview(nowUtc))
                {
                    var bitmapImage = CreateBitmapImage(bitmap);
                    Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        ui.CameraFrame = bitmapImage;
                    }));
                }
            }
            catch (Exception ex)
            {
                // Silently handle frame processing errors
                if (!isClosing)
                {
                    try
                    {
                        Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            ui.CameraStatus = $"Frame processing error: {ex.Message}";
                        }));
                    }
                    catch { }
                }
            }
            finally
            {
                bitmap?.Dispose();
            }
        }

        private bool ShouldUpdateCameraPreview(DateTime nowUtc)
        {
            return ui.IsCameraRunning && cameraPreviewGate.TryEnter(nowUtc);
        }

        private bool ShouldSaveRecordingFrame(DateTime nowUtc)
        {
            return cameraRecordingActive
                   && !string.IsNullOrWhiteSpace(activeCameraRecordingPath)
                   && cameraRecordingGate.TryEnter(nowUtc)
                   && cameraRecordingSaveGate.TryEnter();
        }

        private void SaveCameraRecordingFrame(Bitmap bitmap)
        {
            CameraVideoRecorder recorder;

            try
            {
                lock (cameraLock)
                {
                    recorder = cameraVideoRecorder;
                }

                if (recorder == null || !recorder.WriteFrame(bitmap))
                    return;

                Interlocked.Increment(ref recordedFrameCount);
            }
            catch (Exception ex)
            {
                SetCameraStatusOnUiThread("Recording MP4 error: " + ex.Message);
            }
            finally
            {
                try { bitmap?.Dispose(); } catch { }
                cameraRecordingSaveGate.Exit();
            }
        }

        private static BitmapImage CreateBitmapImage(Bitmap bitmap)
        {
            var bitmapImage = new BitmapImage();
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Bmp);
                stream.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }

        private void CameraSource_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            try
            {
                string errMsg = eventArgs.Description;
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Camera] Source error: " + errMsg + "\r\n"); } catch { }

                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    ui.CameraStatus = $"Camera error: {errMsg}";
                }));

                _ = PublishCameraStatusAsync(false, "error", errMsg);

                if (ui.IsCameraRunning && !isClosing)
                {
                    try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                        "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Camera] Camera connection lost. Refreshing and reconnecting in 1 second...\r\n"); } catch { }

                    _ = Task.Delay(CameraDeviceSelection.ReconnectDelayMs).ContinueWith(async _ =>
                    {
                        if (ui.IsCameraRunning && !isClosing)
                        {
                            await RefreshCamerasAsync();
                            await StartCameraAsync(forceRestart: true);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"), 
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [Camera] Error handling source error callback: " + ex.Message + "\r\n"); } catch { }
            }
        }
    }
}
