using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;

namespace DACDT_2026
{
    public partial class Form1
    {
        private readonly object cameraSync = new object();
        private VideoCaptureDevice activeCamera;
        private bool cameraRecording;
        private string cameraRecordingDirectory;
        private int cameraRecordedFrameCount;
        private DateTime lastCameraUiFrameUtc = DateTime.MinValue;
        private DateTime lastCameraRecordedFrameUtc = DateTime.MinValue;

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private async Task RefreshCamerasAsync()
        {
            try
            {
                var rows = await Task.Run(() =>
                {
                    var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                    var result = new List<CameraDeviceViewModel>();
                    foreach (FilterInfo device in devices)
                    {
                        result.Add(new CameraDeviceViewModel
                        {
                            Name = device.Name,
                            MonikerString = device.MonikerString
                        });
                    }

                    return result;
                });

                await RunOnUiAsync(() =>
                {
                    ReplaceCollection(ui.Cameras, rows);
                    if (string.IsNullOrWhiteSpace(ui.SelectedCameraMoniker) && rows.Count > 0)
                        ui.SelectedCameraMoniker = rows[0].MonikerString;

                    ui.CameraStatus = rows.Count == 0
                        ? "No camera detected."
                        : "Camera list refreshed.";
                });
            }
            catch (Exception ex)
            {
                await RunOnUiAsync(() => ui.CameraStatus = "Camera scan failed: " + ex.Message);
            }
        }

        private async Task StartCameraAsync()
        {
            string moniker = ui.SelectedCameraMoniker;
            if (string.IsNullOrWhiteSpace(moniker))
            {
                await NotifyAsync("error", "Camera", "No camera selected.");
                return;
            }

            try
            {
                await StopCameraAsync();

                var camera = new VideoCaptureDevice(moniker);
                if (camera.VideoCapabilities != null && camera.VideoCapabilities.Length > 0)
                    camera.VideoResolution = camera.VideoCapabilities[0];

                camera.NewFrame += Camera_NewFrame;

                lock (cameraSync)
                {
                    activeCamera = camera;
                    lastCameraUiFrameUtc = DateTime.MinValue;
                    lastCameraRecordedFrameUtc = DateTime.MinValue;
                }

                camera.Start();
                await RunOnUiAsync(() =>
                {
                    ui.IsCameraRunning = true;
                    ui.CameraStatus = "Camera running.";
                });
            }
            catch (Exception ex)
            {
                StopCameraCore();
                await NotifyAsync("error", "Camera", ex.Message);
                await RunOnUiAsync(() => ui.CameraStatus = "Camera start failed: " + ex.Message);
            }
        }

        private async Task StopCameraAsync()
        {
            await Task.Run(StopCameraCore);
            await RunOnUiAsync(() =>
            {
                ui.IsCameraRunning = false;
                ui.CameraStatus = "Camera stopped.";
            });
        }

        private async Task StartCameraRecordingAsync()
        {
            if (!ui.IsCameraRunning)
                await StartCameraAsync();

            if (!ui.IsCameraRunning)
                return;

            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "DACDT_2026_Camera");
            string dir = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(dir);

            lock (cameraSync)
            {
                cameraRecordingDirectory = dir;
                cameraRecordedFrameCount = 0;
                cameraRecording = true;
                lastCameraRecordedFrameUtc = DateTime.MinValue;
            }

            await RunOnUiAsync(() =>
            {
                ui.IsCameraRecording = true;
                ui.CameraRecordedFrames = 0;
                ui.CameraRecordingPath = dir;
                ui.CameraStatus = "Recording camera frames.";
            });
        }

        private async Task StopCameraRecordingAsync()
        {
            string dir;
            int frameCount;

            lock (cameraSync)
            {
                cameraRecording = false;
                dir = cameraRecordingDirectory;
                frameCount = cameraRecordedFrameCount;
            }

            if (!string.IsNullOrWhiteSpace(dir))
            {
                try
                {
                    File.WriteAllText(Path.Combine(dir, "recording.txt"),
                        "DACDT 2026 camera recording" + Environment.NewLine +
                        "Frames: " + frameCount + Environment.NewLine +
                        "Saved: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                catch
                {
                }
            }

            await RunOnUiAsync(() =>
            {
                ui.IsCameraRecording = false;
                ui.CameraRecordedFrames = frameCount;
                ui.CameraStatus = "Recording stopped.";
            });
        }

        private void StopCameraCore()
        {
            VideoCaptureDevice camera;
            lock (cameraSync)
            {
                cameraRecording = false;
                camera = activeCamera;
                activeCamera = null;
            }

            if (camera == null)
                return;

            try { camera.NewFrame -= Camera_NewFrame; } catch { }
            try
            {
                if (camera.IsRunning)
                {
                    camera.SignalToStop();
                    camera.WaitForStop();
                }
            }
            catch
            {
                try { camera.Stop(); } catch { }
            }
        }

        private void Camera_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (eventArgs?.Frame == null || isClosing)
                return;

            DateTime now = DateTime.UtcNow;
            bool updateUi = (now - lastCameraUiFrameUtc).TotalMilliseconds >= 100.0;
            bool recordFrame;
            string recordDir;

            lock (cameraSync)
            {
                recordFrame = cameraRecording && (now - lastCameraRecordedFrameUtc).TotalMilliseconds >= 100.0;
                if (recordFrame)
                    lastCameraRecordedFrameUtc = now;

                recordDir = cameraRecordingDirectory;
            }

            if (updateUi)
            {
                lastCameraUiFrameUtc = now;
                try
                {
                    using (var uiBitmap = (Bitmap)eventArgs.Frame.Clone())
                    {
                        BitmapSource source = ConvertBitmapToBitmapSource(uiBitmap);
                        Dispatcher.BeginInvoke(new Action(() => ui.CameraFrame = source));
                    }
                }
                catch
                {
                }
            }

            if (recordFrame && !string.IsNullOrWhiteSpace(recordDir))
            {
                Bitmap frame = null;
                try
                {
                    frame = (Bitmap)eventArgs.Frame.Clone();
                    string fileName;
                    int count;
                    lock (cameraSync)
                    {
                        cameraRecordedFrameCount++;
                        count = cameraRecordedFrameCount;
                        fileName = Path.Combine(recordDir, "frame_" + count.ToString("D6") + ".jpg");
                    }

                    Task.Run(() =>
                    {
                        using (frame)
                        {
                            frame.Save(fileName, ImageFormat.Jpeg);
                        }

                        Dispatcher.BeginInvoke(new Action(() => ui.CameraRecordedFrames = count));
                    });
                }
                catch
                {
                    if (frame != null)
                        frame.Dispose();
                }
            }
        }

        private static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
    }
}
