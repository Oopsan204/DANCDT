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
        private readonly IntervalGate cameraRecordingGate = new IntervalGate(PerformanceTuning.CameraRecordingFrameIntervalMs);
        private readonly SingleFlightGate cameraRecordingSaveGate = new SingleFlightGate();
        private object cameraLock = new object();
        private DateTime lastCameraMqttPublishUtc = DateTime.MinValue;
        private bool cameraMqttPublishInFlight;
        private int webRtcFrameInFlight;
        private readonly IntervalGate cameraPreviewGate = new IntervalGate(PerformanceTuning.CameraPreviewIntervalMs);
        private readonly IntervalGate webRtcFrameGate = new IntervalGate(PerformanceTuning.WebRtcFrameIntervalMs);
        private static readonly bool EnableMqttCameraFrameFallback = false;
        private const int CameraMqttPublishIntervalMs = 200;
        private const long CameraMqttJpegQuality = 25L;
        private readonly JavaScriptSerializer cameraStatusSerializer = new JavaScriptSerializer();

        private Task PublishCameraStatusAsync(bool running, string state, string message = null)
        {
            var payload = cameraStatusSerializer.Serialize(new
            {
                running = running,
                state = state,
                message = message,
                cameraReady = cameraSource != null && cameraSource.IsRunning,
                webrtcReady = true, // Managed by x64 service
                selectedCamera = ui.SelectedCamera?.DisplayName,
                selectedCameraMoniker = ui.SelectedCameraMoniker,
                timestampUtc = DateTime.UtcNow.ToString("o")
            });

            return mqttService.PublishAsync("DACDT/camera/status", payload, true);
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
                                webRtcBridgeClient.Connect();
                                _ = PublishCameraStatusAsync(true, "running", "camera already running");
                                SetCameraRunningUiState(true, "Camera already running: " + selectedName, clearFrame: false);
                                return;
                            }

                            StopCameraSourceLocked();
                            try { webRtcBridgeClient.Disconnect(); } catch { }
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
                        webRtcBridgeClient.Connect();
                        _ = PublishCameraStatusAsync(true, "running", "camera started: " + selectedName);

                        recordedFrameCount = 0;
                        lastCameraMqttPublishUtc = DateTime.MinValue;
                        cameraPreviewGate.Reset();
                        webRtcFrameGate.Reset();
                        cameraMqttPublishInFlight = false;
                        Interlocked.Exchange(ref webRtcFrameInFlight, 0);
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
            await Task.Run(() =>
            {
                try
                {
                    lock (cameraLock)
                    {
                        StopCameraSourceLocked();
                        webRtcBridgeClient.Disconnect();
                        _ = PublishCameraStatusAsync(false, "stopped", "camera stopped");

                        cameraRecordingActive = false;
                        activeCameraRecordingPath = string.Empty;
                        Interlocked.Exchange(ref webRtcFrameInFlight, 0);
                        cameraPreviewGate.Reset();
                        webRtcFrameGate.Reset();
                        SetCameraRunningUiState(false, "Camera stopped.", clearFrame: true);
                    }
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
            try
            {
                lock (cameraLock)
                {
                    StopCameraSourceLocked();
                    try { webRtcBridgeClient.Disconnect(); } catch { }
                    _ = PublishCameraStatusAsync(false, "stopped", "camera stopped");

                    cameraRecordingActive = false;
                    activeCameraRecordingPath = string.Empty;
                    ui.IsCameraRunning = false;
                    recordedFrameCount = 0;
                    ui.CameraRecordedFrames = 0;
                    lastCameraMqttPublishUtc = DateTime.MinValue;
                    cameraPreviewGate.Reset();
                    webRtcFrameGate.Reset();
                    cameraMqttPublishInFlight = false;
                    Interlocked.Exchange(ref webRtcFrameInFlight, 0);
                }
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
                        string recordingPath = Path.Combine(cameraRecordingDir, $"camera_{timestamp}");

                        activeCameraRecordingPath = recordingPath;
                        cameraRecordingActive = true;
                        cameraRecordingGate.Reset();
                        ui.CameraRecordingPath = recordingPath;
                        ui.IsCameraRecording = true;
                        recordedFrameCount = 0;
                        ui.CameraRecordedFrames = 0;
                        ui.CameraStatus = $"Recording frames to: {Path.GetFileName(recordingPath)}";
                    }
                }
                catch (Exception ex)
                {
                    ui.IsCameraRecording = false;
                    cameraRecordingActive = false;
                    ui.CameraStatus = $"Error starting recording: {ex.Message}";
                }
            });
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
                    int framesRecorded;
                    lock (cameraLock)
                    {
                        cameraRecordingActive = false;
                        ui.IsCameraRecording = false;
                        framesRecorded = recordedFrameCount;
                        activeCameraRecordingPath = string.Empty;
                        recordedFrameCount = 0;
                    }

                    Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        ui.CameraRecordedFrames = 0;
                        ui.CameraStatus = $"Recording stopped. Frames saved: {framesRecorded}";
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

                Bitmap mqttBitmap = null;
                if (ShouldPublishCameraFrameToMqtt(nowUtc))
                {
                    mqttBitmap = (Bitmap)bitmap.Clone();
                }

                if (mqttBitmap != null)
                {
                    _ = Task.Run(() => PublishCameraBitmapToMqttAsync(mqttBitmap));
                }

                // Feed frame to WebRTC server for browser streaming.
                if (webReady)
                {
                    if (ShouldSendFrameToWebRtc(nowUtc))
                    {
                        var webRtcBitmap = (Bitmap)bitmap.Clone();
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                using (var ms = new MemoryStream())
                                {
                                    SaveJpeg(webRtcBitmap, ms, 50L);
                                    byte[] jpegBytes = ms.ToArray();
                                    webRtcBridgeClient.SendFrame(jpegBytes);
                                }
                            }
                            finally
                            {
                                webRtcBitmap.Dispose();
                                Interlocked.Exchange(ref webRtcFrameInFlight, 0);
                            }
                        });
                    }
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

        private bool ShouldPublishCameraFrameToMqtt(DateTime nowUtc)
        {
            if (!EnableMqttCameraFrameFallback)
                return false;

            if (!mqttService.IsConnected || cameraMqttPublishInFlight)
                return false;

            if ((nowUtc - lastCameraMqttPublishUtc).TotalMilliseconds < CameraMqttPublishIntervalMs)
                return false;

            lastCameraMqttPublishUtc = nowUtc;
            cameraMqttPublishInFlight = true;
            return true;
        }

        private bool ShouldSendFrameToWebRtc(DateTime nowUtc)
        {
            if (!ui.IsCameraRunning)
                return false;

            if (Interlocked.CompareExchange(ref webRtcFrameInFlight, 1, 0) != 0)
                return false;

            if (!webRtcFrameGate.TryEnter(nowUtc))
            {
                Interlocked.Exchange(ref webRtcFrameInFlight, 0);
                return false;
            }

            return true;
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
            int frameNo = 0;
            string folder = activeCameraRecordingPath;

            try
            {
                if (string.IsNullOrWhiteSpace(folder))
                    return;

                Directory.CreateDirectory(folder);
                frameNo = Interlocked.Increment(ref recordedFrameCount);
                string framePath = Path.Combine(folder, $"frame_{frameNo:D6}.jpg");
                using (var stream = File.Create(framePath))
                {
                    SaveJpeg(bitmap, stream, 80L);
                }

                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    ui.CameraRecordedFrames = frameNo;
                }));
            }
            catch (Exception ex)
            {
                SetCameraStatusOnUiThread("Recording frame save error: " + ex.Message);
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

        private static Bitmap ResizeBitmap(Bitmap source, int targetWidth, int targetHeight)
        {
            var result = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(result))
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                g.DrawImage(source, 0, 0, targetWidth, targetHeight);
            }
            return result;
        }

        private async Task PublishCameraBitmapToMqttAsync(Bitmap bitmap)
        {
            try
            {
                byte[] jpegBytes = null;
                using (bitmap)
                {
                    // Downsample to max width 640px for lightweight web streaming
                    const int MaxWebWidth = 640;
                    Bitmap processedBitmap = bitmap;
                    bool wasResized = false;

                    if (bitmap.Width > MaxWebWidth)
                    {
                        int targetHeight = (int)Math.Round((double)bitmap.Height * MaxWebWidth / bitmap.Width);
                        processedBitmap = ResizeBitmap(bitmap, MaxWebWidth, targetHeight);
                        wasResized = true;
                    }

                    try
                    {
                        using (var ms = new MemoryStream())
                        {
                            SaveJpeg(processedBitmap, ms, CameraMqttJpegQuality);
                            jpegBytes = ms.ToArray();
                        }
                    }
                    finally
                    {
                        if (wasResized)
                        {
                            processedBitmap.Dispose();
                        }
                    }
                }

                if (jpegBytes != null && jpegBytes.Length > 0 && mqttService.IsConnected)
                {
                    // Publish raw binary bytes (more efficient than Base64)
                    await mqttService.PublishAsync("DACDT/camera/frame", jpegBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT Camera] Publish error: {ex.Message}");
            }
            finally
            {
                lock (cameraLock)
                {
                    cameraMqttPublishInFlight = false;
                }
            }
        }

        private static void SaveJpeg(Bitmap bitmap, Stream stream, long quality)
        {
            ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(codec => string.Equals(codec.MimeType, "image/jpeg", StringComparison.OrdinalIgnoreCase));

            if (jpegCodec == null)
            {
                bitmap.Save(stream, ImageFormat.Jpeg);
                return;
            }

            using (var parameters = new EncoderParameters(1))
            {
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                bitmap.Save(stream, jpegCodec, parameters);
            }
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
