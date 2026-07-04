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
        private List<Bitmap> recordedFrames;
        private int recordedFrameCount;
        private object cameraLock = new object();
        private DateTime lastCameraMqttPublishUtc = DateTime.MinValue;
        private bool cameraMqttPublishInFlight;
        private int webRtcFrameInFlight;
        private DateTime lastWebRtcFrameUtc = DateTime.MinValue;
        private static readonly bool EnableMqttCameraFrameFallback = false;
        private const int CameraMqttPublishIntervalMs = 200;
        private const long CameraMqttJpegQuality = 25L;
        private const int WebRtcFrameIntervalMs = 66;
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
                        lastWebRtcFrameUtc = DateTime.MinValue;
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

                        Interlocked.Exchange(ref webRtcFrameInFlight, 0);
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
                    if (recordedFrames != null)
                    {
                        foreach (var frame in recordedFrames)
                        {
                            try { frame?.Dispose(); } catch { }
                        }
                        recordedFrames.Clear();
                        recordedFrames = null;
                    }

                    StopCameraSourceLocked();
                    try { webRtcBridgeClient.Disconnect(); } catch { }
                    _ = PublishCameraStatusAsync(false, "stopped", "camera stopped");

                    ui.IsCameraRunning = false;
                    recordedFrameCount = 0;
                    ui.CameraRecordedFrames = 0;
                    lastCameraMqttPublishUtc = DateTime.MinValue;
                    lastWebRtcFrameUtc = DateTime.MinValue;
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
        /// Start recording camera frames to memory (in-memory frame buffer).
        /// Frames are stored as Bitmap objects and can be exported to image files.
        /// </summary>
        private async Task StartCameraRecordingAsync()
        {
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

                        Directory.CreateDirectory(cameraRecordingDir);
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
                        string recordingPath = Path.Combine(cameraRecordingDir, $"camera_{timestamp}");

                        recordedFrames = new List<Bitmap>();
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
                    ui.CameraStatus = $"Error starting recording: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Stop recording camera frames and save them as individual image files.
        /// </summary>
        private async Task StopCameraRecordingAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (cameraLock)
                    {
                        if (recordedFrames != null && recordedFrames.Count > 0)
                        {
                            Directory.CreateDirectory(ui.CameraRecordingPath);
                            for (int i = 0; i < recordedFrames.Count; i++)
                            {
                                try
                                {
                                    string framePath = Path.Combine(ui.CameraRecordingPath, $"frame_{i:D6}.bmp");
                                    recordedFrames[i].Save(framePath);
                                    recordedFrames[i].Dispose();
                                }
                                catch { }
                            }
                        }

                        recordedFrames?.Clear();
                        recordedFrames = null;
                        ui.IsCameraRecording = false;
                        int framesRecorded = recordedFrameCount;
                        recordedFrameCount = 0;
                        ui.CameraRecordedFrames = 0;
                        ui.CameraStatus = $"Recording stopped. Frames saved: {framesRecorded}";
                    }
                }
                catch (Exception ex)
                {
                    ui.CameraStatus = $"Error stopping recording: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Handle new frames from the camera source.
        /// Convert to BitmapImage and update UI; optionally store frame if recording.
        /// </summary>
        private void CameraSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                if (isClosing || !webReady)
                    return;

                lock (cameraLock)
                {
                    using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                    {
                        // Store frame to memory if recording
                        if (ui.IsCameraRecording && recordedFrames != null)
                        {
                            try
                            {
                                recordedFrames.Add((Bitmap)bitmap.Clone());
                                recordedFrameCount++;
                                ui.CameraRecordedFrames = recordedFrameCount;
                            }
                            catch { }
                        }

                        Bitmap mqttBitmap = null;
                        if (ShouldPublishCameraFrameToMqtt(DateTime.UtcNow))
                        {
                            mqttBitmap = (Bitmap)bitmap.Clone();
                        }

                        if (mqttBitmap != null)
                        {
                            _ = Task.Run(() => PublishCameraBitmapToMqttAsync(mqttBitmap));
                        }

                        // Feed frame to WebRTC server for browser streaming
                        if (ShouldSendFrameToWebRtc(DateTime.UtcNow))
                        {
                            var webRtcBitmap = (Bitmap)bitmap.Clone();
                            _ = Task.Run(() =>
                            {
                                try
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        SaveJpeg(webRtcBitmap, ms, 50L); // 50% quality to reduce bandwidth
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

                        // Convert bitmap to BitmapImage for UI display
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = new MemoryStream();
                        bitmap.Save(bitmapImage.StreamSource, System.Drawing.Imaging.ImageFormat.Bmp);
                        bitmapImage.StreamSource.Position = 0;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();

                        // Update UI on the main thread
                        Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            ui.CameraFrame = bitmapImage;
                        }));
                    }
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

            if ((nowUtc - lastWebRtcFrameUtc).TotalMilliseconds < WebRtcFrameIntervalMs)
                return false;

            if (Interlocked.CompareExchange(ref webRtcFrameInFlight, 1, 0) != 0)
                return false;

            lastWebRtcFrameUtc = nowUtc;
            return true;
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
