using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;

namespace DACDT_2026
{
    public class WebRtcCameraServer
    {
        private readonly MqttPublishService _mqttService;
        private readonly ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        private readonly ConcurrentDictionary<string, List<RTCIceCandidateInit>> _bufferedCandidates = new ConcurrentDictionary<string, List<RTCIceCandidateInit>>();
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private VpxVideoEncoder _vpxVideoEncoder;
        private readonly object _encoderLock = new object();
        private bool _isRunning;
        private DateTime _startTime;
        private DateTime _lastStatusPublishUtc = DateTime.MinValue;
        private long _encodedFrameCount;
        private long _sentFrameCount;
        private string _lastEncoderError;
        private const int MaxWebRtcStreamWidth = 640;
        private const int TargetWebRtcKbps = 4000;
        private const int StatusPublishIntervalMs = 1000;

        public bool IsRunning => _isRunning;

        public WebRtcCameraServer(MqttPublishService mqttService)
        {
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _startTime = DateTime.UtcNow;
            _encodedFrameCount = 0;
            _sentFrameCount = 0;
            _lastEncoderError = null;
            _lastStatusPublishUtc = DateTime.MinValue;
            lock (_encoderLock)
            {
                _vpxVideoEncoder = new VpxVideoEncoder();
                _vpxVideoEncoder.TargetKbps = TargetWebRtcKbps;
            }
            Log("Camera stream server started.");
            _ = PublishWebRtcStatusAsync("waiting_peer", "WebRTC camera server started", true, true);
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;

            foreach (var kvp in _peerConnections)
            {
                try
                {
                    kvp.Value.close();
                }
                catch (Exception ex)
                {
                    Log($"Error closing peer connection for {kvp.Key}: {ex.Message}");
                }
            }
            _peerConnections.Clear();
            _bufferedCandidates.Clear();

            lock (_encoderLock)
            {
                if (_vpxVideoEncoder != null)
                {
                    try { _vpxVideoEncoder.Dispose(); } catch { }
                    _vpxVideoEncoder = null;
                }
            }
            Log("Camera stream server stopped.");
            _ = PublishWebRtcStatusAsync("stopped", "WebRTC camera server stopped", false, false);
        }

        private Task PublishWebRtcStatusAsync(string state, string message, bool running, bool encoderReady)
        {
            if (_mqttService == null || !_mqttService.IsConnected)
                return Task.CompletedTask;

            var payload = _serializer.Serialize(new
            {
                running = running,
                state = state,
                message = message,
                encoderReady = encoderReady,
                lastEncoderError = _lastEncoderError,
                peerConnections = _peerConnections.Count,
                encodedFrames = _encodedFrameCount,
                sentFrames = _sentFrameCount,
                timestampUtc = DateTime.UtcNow.ToString("o")
            });

            return _mqttService.PublishAsync("DACDT/camera/webrtc/status", payload, true);
        }

        private void PublishWebRtcStatusThrottled(string state, string message, bool encoderReady)
        {
            var nowUtc = DateTime.UtcNow;
            if ((nowUtc - _lastStatusPublishUtc).TotalMilliseconds < StatusPublishIntervalMs)
                return;

            _lastStatusPublishUtc = nowUtc;
            _ = PublishWebRtcStatusAsync(state, message, _isRunning, encoderReady);
        }

        public async Task ProcessSignalingMessageAsync(string clientId, string type, string payload)
        {
            if (!_isRunning) return;

            try
            {
                if (string.Equals(type, "offer", StringComparison.OrdinalIgnoreCase))
                {
                    Log($"Received offer from client: {clientId}");
                    
                    // Parse offer SDP
                    var dict = _serializer.Deserialize<Dictionary<string, object>>(payload);
                    if (dict == null || !dict.TryGetValue("sdp", out var sdpObj) || sdpObj == null)
                    {
                        Log("Offer SDP missing from payload");
                        return;
                    }
                    string offerSdp = sdpObj.ToString();

                    // Clean up any existing connection for this client
                    if (_peerConnections.TryRemove(clientId, out var oldPc))
                    {
                        try { oldPc.close(); } catch { }
                    }
                    _bufferedCandidates.TryRemove(clientId, out _);

                    // Create new Peer Connection
                    var config = new RTCConfiguration
                    {
                        iceServers = new List<RTCIceServer>
                        {
                            new RTCIceServer { urls = "stun:stun.l.google.com:19302" },
                            new RTCIceServer { urls = "stun:stun.cloudflare.com:3478" },
                            new RTCIceServer
                            {
                                urls = "turn:free.expressturn.com:3478",
                                username = "000000002097516783",
                                credential = "RqnWXzfkmJ+Zu0ymmKTGk2SEBFY="
                            },
                            new RTCIceServer
                            {
                                urls = "turn:free.expressturn.com:3478?transport=tcp",
                                username = "000000002097516783",
                                credential = "RqnWXzfkmJ+Zu0ymmKTGk2SEBFY="
                            }
                        }
                    };

                    var pc = new RTCPeerConnection(config);

                    // Add Video Track
                    var videoFormat = new VideoFormat(VideoCodecsEnum.VP8, 96);
                    var videoTrack = new MediaStreamTrack(videoFormat, MediaStreamStatusEnum.SendOnly);
                    pc.addTrack(videoTrack);

                    // Set up ICE candidate generation
                    pc.onicecandidate += (candidate) =>
                    {
                        if (candidate != null)
                        {
                            Log($"ICE candidate generated for {clientId}: type={candidate.type}, candidate={candidate.candidate}, sdpMid={candidate.sdpMid}, sdpMLineIndex={candidate.sdpMLineIndex}");
                            var candidateMsg = new
                            {
                                type = "candidate",
                                candidate = candidate.candidate,
                                sdpMid = candidate.sdpMid,
                                sdpMLineIndex = candidate.sdpMLineIndex
                            };
                            string candPayload = _serializer.Serialize(candidateMsg);
                            _ = _mqttService.PublishAsync($"DACDT/camera/webrtc/signaling/{clientId}/server", candPayload);
                        }
                    };

                    // Log ICE gathering state changes
                    pc.onicegatheringstatechange += (gatherState) =>
                    {
                        Log($"Client {clientId} ICE gathering state: {gatherState}");
                    };

                    // Set up connection state change handling
                    pc.onconnectionstatechange += (state) =>
                    {
                        Log($"Client {clientId} connection state: {state}");
                        _ = PublishWebRtcStatusAsync(
                            state.ToString().ToLowerInvariant(),
                            $"Client {clientId} connection state: {state}",
                            _isRunning,
                            string.IsNullOrEmpty(_lastEncoderError));
                        if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed || state == RTCPeerConnectionState.disconnected)
                        {
                            if (_peerConnections.TryRemove(clientId, out var deadPc))
                            {
                                try { deadPc.close(); } catch { }
                            }
                            _bufferedCandidates.TryRemove(clientId, out _);
                        }
                    };

                    // Set remote offer description
                    var offerInit = new RTCSessionDescriptionInit
                    {
                        type = RTCSdpType.offer,
                        sdp = offerSdp
                    };

                    // Save connection early so candidate messages can find it
                    _peerConnections[clientId] = pc;

                    var setDescResult = pc.setRemoteDescription(offerInit);
                    if (setDescResult != SetDescriptionResultEnum.OK)
                    {
                        Log($"SetRemoteDescription failed for client {clientId}: {setDescResult}");
                        _peerConnections.TryRemove(clientId, out _);
                        pc.close();
                        return;
                    }

                    // Apply any buffered candidates now that remoteDescription is set
                    if (_bufferedCandidates.TryRemove(clientId, out var candidates))
                    {
                        Log($"Applying {candidates.Count} buffered candidates for {clientId}");
                        foreach (var cand in candidates)
                        {
                            try
                            {
                                pc.addIceCandidate(cand);
                            }
                            catch (Exception ex)
                            {
                                Log($"Error adding buffered candidate: {ex.Message}");
                            }
                        }
                    }

                    // Create and set local answer
                    var answerInit = pc.createAnswer(null);
                    await pc.setLocalDescription(answerInit);

                    // Send local answer SDP to client
                    var answerMsg = new
                    {
                        type = "answer",
                        sdp = answerInit.sdp
                    };
                    string answerPayload = _serializer.Serialize(answerMsg);
                    await _mqttService.PublishAsync($"DACDT/camera/webrtc/signaling/{clientId}/server", answerPayload);

                    Log($"Set answer and saved peer connection for: {clientId}");
                }
                else if (string.Equals(type, "candidate", StringComparison.OrdinalIgnoreCase))
                {
                    var dict = _serializer.Deserialize<Dictionary<string, object>>(payload);
                    if (dict != null)
                    {
                        string candidateText = dict.TryGetValue("candidate", out var cVal) && cVal != null ? cVal.ToString() : null;
                        string sdpMid = dict.TryGetValue("sdpMid", out var mVal) && mVal != null ? mVal.ToString() : null;
                        ushort sdpMLineIndex = 0;
                        if (dict.TryGetValue("sdpMLineIndex", out var idxVal) && idxVal != null)
                        {
                            ushort.TryParse(idxVal.ToString(), out sdpMLineIndex);
                        }

                        if (!string.IsNullOrEmpty(candidateText))
                        {
                            var candidateInit = new RTCIceCandidateInit
                            {
                                candidate = candidateText,
                                sdpMid = sdpMid,
                                sdpMLineIndex = sdpMLineIndex
                            };

                            if (_peerConnections.TryGetValue(clientId, out var pc) && pc.remoteDescription != null)
                            {
                                pc.addIceCandidate(candidateInit);
                            }
                            else
                            {
                                _bufferedCandidates.AddOrUpdate(clientId,
                                    new List<RTCIceCandidateInit> { candidateInit },
                                    (k, list) => { lock (list) { list.Add(candidateInit); } return list; });
                            }
                        }
                    }
                }
                else if (string.Equals(type, "bye", StringComparison.OrdinalIgnoreCase))
                {
                    if (_peerConnections.TryRemove(clientId, out var pc))
                    {
                        try { pc.close(); } catch { }
                        Log($"Client {clientId} disconnected via bye command.");
                    }
                    _bufferedCandidates.TryRemove(clientId, out _);
                }
            }
            catch (Exception ex)
            {
                Log($"Error processing signaling message for client {clientId}: {ex.Message}");
            }
        }

        public void SendFrame(Bitmap bitmap)
        {
            if (!_isRunning || _peerConnections.IsEmpty || bitmap == null)
                return;

            int width = bitmap.Width;
            int height = bitmap.Height;

            // Downscale for lightweight browser streaming.
            if (width > MaxWebRtcStreamWidth)
            {
                height = (int)Math.Round((double)height * MaxWebRtcStreamWidth / width);
                width = MaxWebRtcStreamWidth;
            }

            // WebRTC encoders require even dimensions
            if (width % 2 != 0) width--;
            if (height % 2 != 0) height--;

            if (width <= 0 || height <= 0) return;

            byte[] bgraBytes = null;
            Bitmap resized = null;
            BitmapData bmpData = null;

            try
            {
                if (bitmap.Width != width || bitmap.Height != height)
                {
                    resized = new Bitmap(width, height, PixelFormat.Format32bppRgb);
                    using (var g = Graphics.FromImage(resized))
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                        g.DrawImage(bitmap, 0, 0, width, height);
                    }
                    bmpData = resized.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                }
                else
                {
                    if (bitmap.PixelFormat != PixelFormat.Format32bppArgb && bitmap.PixelFormat != PixelFormat.Format32bppRgb)
                    {
                        resized = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                        using (var g = Graphics.FromImage(resized))
                        {
                            g.DrawImage(bitmap, 0, 0);
                        }
                        bmpData = resized.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    }
                    else
                    {
                        bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    }
                }

                int bytesCount = bmpData.Stride * height;
                bgraBytes = new byte[bytesCount];
                System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, bgraBytes, 0, bytesCount);
            }
            catch (Exception ex)
            {
                Log($"Error extracting bitmap bytes: {ex.Message}");
                return;
            }
            finally
            {
                if (bmpData != null)
                {
                    if (resized != null)
                    {
                        try { resized.UnlockBits(bmpData); } catch { }
                        resized.Dispose();
                    }
                    else
                    {
                        try { bitmap.UnlockBits(bmpData); } catch { }
                    }
                }
            }

            byte[] encodedBytes = null;
            lock (_encoderLock)
            {
                if (_vpxVideoEncoder != null)
                {
                    try
                    {
                        encodedBytes = _vpxVideoEncoder.EncodeVideo(width, height, bgraBytes, VideoPixelFormatsEnum.Bgra, VideoCodecsEnum.VP8);
                        _lastEncoderError = null;
                    }
                    catch (Exception ex)
                    {
                        _lastEncoderError = ex.Message;
                        Log($"Encoder error: {ex.Message}");
                        PublishWebRtcStatusThrottled("encoder_error", ex.Message, false);
                    }
                }
            }

            if (encodedBytes != null && encodedBytes.Length > 0)
            {
                _encodedFrameCount++;
                var elapsedMs = (uint)(DateTime.UtcNow - _startTime).TotalMilliseconds;
                var rtpTimestamp = elapsedMs * 90; // 90 kHz clock

                foreach (var kvp in _peerConnections)
                {
                    try
                    {
                        if (kvp.Value.connectionState == RTCPeerConnectionState.connected)
                        {
                            kvp.Value.SendVideo(rtpTimestamp, encodedBytes);
                            _sentFrameCount++;
                            PublishWebRtcStatusThrottled("streaming", "WebRTC video frames are being sent", true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"SendVideo error for client {kvp.Key}: {ex.Message}");
                    }
                }
            }
        }

        private void Log(string message)
        {
            try
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt"),
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [WebRTC] " + message + "\r\n"
                );
            }
            catch { }
        }
    }
}
