using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DACDT_2026
{
    public partial class Form1
    {
        private DateTime lastWebCadUploadStatusUtc = DateTime.MinValue;

        private static bool IsWebCadUploadTopic(string topic)
        {
            return string.Equals(topic, "DACDT/cad/upload/start", StringComparison.OrdinalIgnoreCase)
                || string.Equals(topic, "DACDT/cad/upload/chunk", StringComparison.OrdinalIgnoreCase)
                || string.Equals(topic, "DACDT/cad/upload/finish", StringComparison.OrdinalIgnoreCase)
                || string.Equals(topic, "DACDT/cad/upload/cancel", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseBinaryUploadTopic(string topic, out string jobId, out int index)
        {
            jobId = string.Empty;
            index = -1;
            const string prefix = "DACDT/cad/upload/binary/";
            if (string.IsNullOrWhiteSpace(topic) || !topic.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string[] parts = topic.Substring(prefix.Length).Split('/');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
                return false;

            jobId = parts[0];
            return int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out index);
        }

        private async Task HandleWebCadBinaryUploadMessageAsync(string topic, byte[] payload)
        {
            await webCadUploadMessageGate.WaitAsync();
            try
            {
                string jobId;
                int index;
                if (!TryParseBinaryUploadTopic(topic, out jobId, out index))
                    throw new InvalidOperationException("Binary CAD upload topic is invalid.");

                bool complete = webCadUploadSession.AddBinaryChunk(jobId, index, payload);
                if (complete || ShouldPublishWebCadUploadProgress())
                {
                    await PublishCadUploadStatusAsync(
                        complete ? "received" : "receiving",
                        complete ? "Upload chunks received." : "Receiving chunks...",
                        webCadUploadSession.ReceivedChunks,
                        webCadUploadSession.ExpectedChunks);
                }
            }
            catch (Exception ex)
            {
                await PublishCadUploadStatusAsync("error", ex.Message, webCadUploadSession.ReceivedChunks, webCadUploadSession.ExpectedChunks);
                await NotifyAsync("error", "Web CAD Upload", ex.Message);
            }
            finally
            {
                webCadUploadMessageGate.Release();
            }
        }

        private async Task HandleWebCadUploadMessageAsync(string topic, string payload)
        {
            await webCadUploadMessageGate.WaitAsync();
            try
            {
                await HandleWebCadUploadMessageCoreAsync(topic, payload);
            }
            finally
            {
                webCadUploadMessageGate.Release();
            }
        }

        private async Task HandleWebCadUploadMessageCoreAsync(string topic, string payload)
        {
            try
            {
                var map = DeserializeUploadPayload(payload);
                if (string.Equals(topic, "DACDT/cad/upload/start", StringComparison.OrdinalIgnoreCase))
                {
                    webCadUploadSession.Begin(
                        GetUploadString(map, "jobId"),
                        GetUploadString(map, "fileName"),
                        GetUploadInt(map, "totalChunks"),
                        GetUploadInt(map, "totalBytes"));

                    lastWebCadUploadStatusUtc = DateTime.UtcNow;
                    await PublishCadUploadStatusAsync("receiving", $"Receiving {webCadUploadSession.FileName}", webCadUploadSession.ReceivedChunks, webCadUploadSession.ExpectedChunks);
                    return;
                }

                if (string.Equals(topic, "DACDT/cad/upload/chunk", StringComparison.OrdinalIgnoreCase))
                {
                    bool complete = webCadUploadSession.AddChunk(
                        GetUploadString(map, "jobId"),
                        GetUploadInt(map, "index"),
                        GetUploadString(map, "data"));

                    if (complete || ShouldPublishWebCadUploadProgress())
                    {
                        await PublishCadUploadStatusAsync(
                            complete ? "received" : "receiving",
                            complete ? "Upload chunks received." : "Receiving chunks...",
                            webCadUploadSession.ReceivedChunks,
                            webCadUploadSession.ExpectedChunks);
                    }
                    return;
                }

                if (string.Equals(topic, "DACDT/cad/upload/cancel", StringComparison.OrdinalIgnoreCase))
                {
                    webCadUploadSession.Reset();
                    await PublishCadUploadStatusAsync("cancelled", "Upload cancelled.", 0, 0);
                    return;
                }

                if (string.Equals(topic, "DACDT/cad/upload/finish", StringComparison.OrdinalIgnoreCase))
                {
                    await FinishWebCadUploadAsync(GetUploadString(map, "jobId"));
                }
            }
            catch (Exception ex)
            {
                await PublishCadUploadStatusAsync("error", ex.Message, webCadUploadSession.ReceivedChunks, webCadUploadSession.ExpectedChunks);
                await NotifyAsync("error", "Web CAD Upload", ex.Message);
            }
        }

        private bool ShouldPublishWebCadUploadProgress()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - lastWebCadUploadStatusUtc).TotalMilliseconds < 250)
                return false;

            lastWebCadUploadStatusUtc = now;
            return true;
        }

        private async Task FinishWebCadUploadAsync(string jobId)
        {
            if (!string.Equals(webCadUploadSession.JobId, jobId, StringComparison.Ordinal))
                throw new InvalidOperationException("Upload finish job id does not match.");

            if (!await cadLoadGate.WaitAsync(0))
            {
                await PublishCadUploadStatusAsync("busy", "App is loading another CAD file. Try again.", webCadUploadSession.ReceivedChunks, webCadUploadSession.ExpectedChunks);
                return;
            }

            string path = null;
            try
            {
                byte[] bytes = webCadUploadSession.Assemble();
                path = SaveWebUploadedFile(webCadUploadSession.FileName, bytes);
                await PublishCadUploadStatusAsync("loading", "Upload received; loading preview in app.", webCadUploadSession.ReceivedChunks, webCadUploadSession.ExpectedChunks);
                await LoadUploadedCadFileAsync(path);
                await PublishCadUploadStatusAsync("loaded", "File loaded in app preview. Use the app Send/Run flow to write PLC safely.", webCadUploadSession.ReceivedChunks, webCadUploadSession.ExpectedChunks);
            }
            finally
            {
                _ = SendProgressAsync(false, 0);
                cadLoadGate.Release();
            }
        }

        private string SaveWebUploadedFile(string fileName, byte[] bytes)
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DACDT_2026",
                "WebUploads");
            Directory.CreateDirectory(dir);

            string safeName = MakeSafeFileName(Path.GetFileName(fileName));
            string path = Path.Combine(dir, DateTime.Now.ToString("yyyyMMdd_HHmmss_", CultureInfo.InvariantCulture) + safeName);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static string MakeSafeFileName(string fileName)
        {
            var sb = new StringBuilder();
            foreach (char c in fileName)
            {
                sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
            }
            return sb.Length == 0 ? "upload.nc" : sb.ToString();
        }

        private async Task LoadUploadedCadFileAsync(string selectedPath)
        {
            bool isGcode = IsGcodeFile(selectedPath);
            string sourceName = isGcode ? "GCODE" : "DXF";
            AddLogEntry(sourceName, selectedPath, "Read", "Selected", "WebUpload");

            ClearLoadedFileState();
            await SendProgressAsync(true, 5);
            await Task.Yield();

            CadDocumentService.CadLoadResult loadedDoc = null;
            string loadedGcodeText = string.Empty;
            NcGcodeCleaner.CleanResult cleanResult = null;

            await Task.Run(() =>
            {
                if (isGcode)
                {
                    string originalGcodeText = File.ReadAllText(selectedPath);
                    cleanResult = NcGcodeCleaner.Clean(originalGcodeText);
                    loadedGcodeText = cleanResult.Text;
                    loadedDoc = gcodeCoordinateService.LoadAsCadFromText(loadedGcodeText, selectedPath);
                }
                else
                {
                    loadedDoc = cadService.Load(selectedPath);
                }

                NormalizeCadDocumentPaths(loadedDoc, isGcode);
                if (!isGcode)
                    TagCadDocumentProcessKind(loadedDoc, EngraveCutProcessComposer.EngraveKind);
            });

            await SendProgressAsync(true, 35);

            activeCadDocument = loadedDoc;
            rawGcodeText = loadedGcodeText;
            activeDocumentKind = isGcode ? "GCODE" : "DXF";
            isMixedEngraveCutProgram = !isGcode && activeCadDocument != null;
            selectedCadPointKey = activeCadDocument?.Points?.FirstOrDefault()?.Key;
            assignedPointKeys.Clear();

            if (isGcode)
            {
                var firstSpeed = activeCadDocument?.Primitives?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Speed))?.Speed;
                if (!string.IsNullOrEmpty(firstSpeed))
                    gcodeSpeedM3 = firstSpeed;
            }

            currentView = "dxf";

            AddLogEntry(sourceName, activeCadDocument?.FilePath ?? selectedPath, "Read", "OK",
                $"Loaded web upload: {activeCadDocument?.FileName ?? Path.GetFileName(selectedPath)}");
            await NotifyAsync("success", "Web CAD Upload",
                $"Loaded: {activeCadDocument?.FileName ?? Path.GetFileName(selectedPath)}");
            if (isGcode)
                await ReportNcCleanerResultAsync(cleanResult);

            if (isGcode)
                await HandleImportCadToProcessAsync();
            else
                await RebuildMixedEngraveCutProgramAsync();
            await SendProgressAsync(true, 65);
            await HandleScanLimitsAsync();
            await SendProgressAsync(true, 85);
            await PushDxfStateAsync();
            await PublishAllMqttAsync();
        }

        private async Task PublishCadUploadStatusAsync(string status, string message, int receivedChunks, int totalChunks)
        {
            if (mqttService == null || !mqttService.IsConnected)
                return;

            var serializer = new JavaScriptSerializer();
            string payload = serializer.Serialize(new Dictionary<string, object>
            {
                { "status", status },
                { "message", message },
                { "fileName", webCadUploadSession.FileName ?? "" },
                { "receivedChunks", receivedChunks },
                { "totalChunks", totalChunks },
                { "ts", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) }
            });
            await mqttService.PublishAsync("DACDT/cad/upload/status", payload);
        }

        private static Dictionary<string, object> DeserializeUploadPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                throw new ArgumentException("Upload payload is empty.");

            var serializer = new JavaScriptSerializer();
            var map = serializer.Deserialize<Dictionary<string, object>>(payload);
            if (map == null)
                throw new ArgumentException("Upload payload is invalid.");
            return map;
        }

        private static string GetUploadString(Dictionary<string, object> map, string key)
        {
            object value;
            if (!map.TryGetValue(key, out value) || value == null)
                return string.Empty;
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static int GetUploadInt(Dictionary<string, object> map, string key)
        {
            object value;
            if (!map.TryGetValue(key, out value) || value == null)
                return 0;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
    }
}
