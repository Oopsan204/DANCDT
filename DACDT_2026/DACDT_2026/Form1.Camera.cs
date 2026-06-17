using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private string cameraRecordingDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "camera_recordings");
        private List<Bitmap> recordedFrames;
        private int recordedFrameCount;
        private object cameraLock = new object();
        private DateTime lastCameraMqttPublishUtc = DateTime.MinValue;
        private bool cameraMqttPublishInFlight;
        private const int CameraMqttPublishIntervalMs = 200;
        private const long CameraMqttJpegQuality = 25L;

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

                        ui.Cameras.ReplaceWith(cameras);

                        if (cameras.Count > 0)
                        {
                            ui.SelectedCameraMoniker = cameras[0].MonikerString;
                            ui.CameraStatus = $"Found {cameras.Count} camera(s). Ready to start.";
                        }
                        else
                        {
                            ui.CameraStatus = "No cameras found.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ui.CameraStatus = $"Error refreshing cameras: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Start the selected camera and begin capturing frames to the UI.
        /// </summary>
        private async Task StartCameraAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (cameraLock)
                    {
                        if (string.IsNullOrWhiteSpace(ui.SelectedCameraMoniker))
                        {
                            ui.CameraStatus = "No camera selected.";
                            return;
                        }

                        if (cameraSource != null && cameraSource.IsRunning)
                        {
                            ui.CameraStatus = "Camera already running.";
                            return;
                        }

                        cameraSource = new VideoCaptureDevice(ui.SelectedCameraMoniker);
                        cameraSource.NewFrame += CameraSource_NewFrame;
                        cameraSource.Start();
                        webRtcCameraServer.Start();
                        _ = mqttService.PublishAsync("DACDT/camera/status", "{\"running\":true}", true);

                        ui.IsCameraRunning = true;
                        ui.CameraStatus = "Camera started.";
                        recordedFrameCount = 0;
                        ui.CameraRecordedFrames = 0;
                        lastCameraMqttPublishUtc = DateTime.MinValue;
                        cameraMqttPublishInFlight = false;
                    }
                }
                catch (Exception ex)
                {
                    ui.IsCameraRunning = false;
                    ui.CameraStatus = $"Error starting camera: {ex.Message}";
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
                        if (cameraSource != null && cameraSource.IsRunning)
                        {
                            cameraSource.SignalToStop();
                            cameraSource.WaitForStop();
                            cameraSource.NewFrame -= CameraSource_NewFrame;
                            cameraSource = null;
                        }
                        webRtcCameraServer.Stop();
                        _ = mqttService.PublishAsync("DACDT/camera/status", "{\"running\":false}", true);

                        ui.IsCameraRunning = false;
                        ui.CameraStatus = "Camera stopped.";
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

                    if (cameraSource != null && cameraSource.IsRunning)
                    {
                        try
                        {
                            cameraSource.SignalToStop();
                            cameraSource.WaitForStop();
                        }
                        catch { }
                        
                        try { cameraSource.NewFrame -= CameraSource_NewFrame; } catch { }
                        cameraSource = null;
                    }
                    try { webRtcCameraServer.Stop(); } catch { }
                    _ = mqttService.PublishAsync("DACDT/camera/status", "{\"running\":false}", true);

                    ui.IsCameraRunning = false;
                    recordedFrameCount = 0;
                    ui.CameraRecordedFrames = 0;
                    lastCameraMqttPublishUtc = DateTime.MinValue;
                    cameraMqttPublishInFlight = false;
                }
            }
            catch { }
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

                        // Stream via WebRTC
                        if (webRtcCameraServer != null && webRtcCameraServer.IsRunning)
                        {
                            var webrtcBitmap = (Bitmap)bitmap.Clone();
                            _ = Task.Run(() =>
                            {
                                try
                                {
                                    webRtcCameraServer.SendFrame(webrtcBitmap);
                                }
                                catch { }
                                finally
                                {
                                    try { webrtcBitmap.Dispose(); } catch { }
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
            if (!mqttService.IsConnected || cameraMqttPublishInFlight)
                return false;

            if ((nowUtc - lastCameraMqttPublishUtc).TotalMilliseconds < CameraMqttPublishIntervalMs)
                return false;

            lastCameraMqttPublishUtc = nowUtc;
            cameraMqttPublishInFlight = true;
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
    }
}
