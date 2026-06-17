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
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private VpxVideoEncoder _vpxVideoEncoder;
        private readonly object _encoderLock = new object();
        private bool _isRunning;
        private DateTime _startTime;

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
            lock (_encoderLock)
            {
                _vpxVideoEncoder = new VpxVideoEncoder();
                _vpxVideoEncoder.TargetKbps = 1000;
            }
            Console.WriteLine("[WebRTC] Camera stream server started.");
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
                    Console.WriteLine($"[WebRTC] Error closing peer connection for {kvp.Key}: {ex.Message}");
                }
            }
            _peerConnections.Clear();

            lock (_encoderLock)
            {
                if (_vpxVideoEncoder != null)
                {
                    try { _vpxVideoEncoder.Dispose(); } catch { }
                    _vpxVideoEncoder = null;
                }
            }
            Console.WriteLine("[WebRTC] Camera stream server stopped.");
        }

        public async Task ProcessSignalingMessageAsync(string clientId, string type, string payload)
        {
            if (!_isRunning) return;

            try
            {
                if (string.Equals(type, "offer", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[WebRTC] Received offer from client: {clientId}");
                    
                    // Parse offer SDP
                    var dict = _serializer.Deserialize<Dictionary<string, object>>(payload);
                    if (dict == null || !dict.TryGetValue("sdp", out var sdpObj) || sdpObj == null)
                    {
                        Console.WriteLine("[WebRTC] Offer SDP missing from payload");
                        return;
                    }
                    string offerSdp = sdpObj.ToString();

                    // Clean up any existing connection for this client
                    if (_peerConnections.TryRemove(clientId, out var oldPc))
                    {
                        try { oldPc.close(); } catch { }
                    }

                    // Create new Peer Connection
                    var config = new RTCConfiguration
                    {
                        iceServers = new List<RTCIceServer>
                        {
                            new RTCIceServer { urls = "stun:stun.l.google.com:19302" }
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

                    // Set up connection state change handling
                    pc.onconnectionstatechange += (state) =>
                    {
                        Console.WriteLine($"[WebRTC] Client {clientId} connection state: {state}");
                        if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed || state == RTCPeerConnectionState.disconnected)
                        {
                            if (_peerConnections.TryRemove(clientId, out var deadPc))
                            {
                                try { deadPc.close(); } catch { }
                            }
                        }
                    };

                    // Set remote offer description
                    var offerInit = new RTCSessionDescriptionInit
                    {
                        type = RTCSdpType.offer,
                        sdp = offerSdp
                    };

                    var setDescResult = pc.setRemoteDescription(offerInit);
                    if (setDescResult != SetDescriptionResultEnum.OK)
                    {
                        Console.WriteLine($"[WebRTC] SetRemoteDescription failed for client {clientId}: {setDescResult}");
                        pc.close();
                        return;
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

                    // Save the connection
                    _peerConnections[clientId] = pc;
                    Console.WriteLine($"[WebRTC] Set answer and saved peer connection for: {clientId}");
                }
                else if (string.Equals(type, "candidate", StringComparison.OrdinalIgnoreCase))
                {
                    if (_peerConnections.TryGetValue(clientId, out var pc))
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
                                pc.addIceCandidate(candidateInit);
                            }
                        }
                    }
                }
                else if (string.Equals(type, "bye", StringComparison.OrdinalIgnoreCase))
                {
                    if (_peerConnections.TryRemove(clientId, out var pc))
                    {
                        try { pc.close(); } catch { }
                        Console.WriteLine($"[WebRTC] Client {clientId} disconnected via bye command.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebRTC] Error processing signaling message for client {clientId}: {ex.Message}");
            }
        }

        public void SendFrame(Bitmap bitmap)
        {
            if (!_isRunning || _peerConnections.IsEmpty || bitmap == null)
                return;

            int width = bitmap.Width;
            int height = bitmap.Height;

            // Downscale to max width 640px for lightweight stream
            if (width > 640)
            {
                height = (int)Math.Round((double)height * 640 / width);
                width = 640;
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
                Console.WriteLine($"[WebRTC] Error extracting bitmap bytes: {ex.Message}");
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
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WebRTC] Encoder error: {ex.Message}");
                    }
                }
            }

            if (encodedBytes != null && encodedBytes.Length > 0)
            {
                var elapsedMs = (uint)(DateTime.UtcNow - _startTime).TotalMilliseconds;
                var rtpTimestamp = elapsedMs * 90; // 90 kHz clock

                foreach (var kvp in _peerConnections)
                {
                    try
                    {
                        if (kvp.Value.connectionState == RTCPeerConnectionState.connected)
                        {
                            kvp.Value.SendVideo(rtpTimestamp, encodedBytes);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WebRTC] SendVideo error for client {kvp.Key}: {ex.Message}");
                    }
                }
            }
        }
    }
}
