using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DACDT_2026;

namespace WebRtcCameraService
{
    class Program
    {
        private static MqttPublishService mqttService;
        private static WebRtcCameraServer webRtcCameraServer;
        private static TcpListener listener;
        private static CancellationTokenSource cts = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting WebRTC Camera Background Service (x64)...");

            // Connect to HiveMQ MQTT broker for signaling
            mqttService = new MqttPublishService();
            webRtcCameraServer = new WebRtcCameraServer(mqttService);

            try
            {
                string broker = "beb7179d08fa43f79d440a9be9b95f24.s1.eu.hivemq.cloud";
                string username = "DACDT2026";
                string password = "trungaN123@";
                await mqttService.ConnectAsync(broker, username, password);
                await mqttService.SubscribeAsync(
                    "DACDT/camera/command",
                    "DACDT/camera/webrtc/signaling/+/client"
                );
                mqttService.MessageReceived += MqttService_MessageReceived;
                Console.WriteLine("Connected to MQTT Broker.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT Init Failed: {ex.Message}");
            }

            webRtcCameraServer.Start();

            // Start Local TCP Frame Server
            listener = new TcpListener(IPAddress.Loopback, 5080);
            listener.Start();
            Console.WriteLine("TCP Frame Bridge listening on 127.0.0.1:5080");

            _ = Task.Run(() => AcceptClientsAsync(cts.Token));

            Console.WriteLine("Service is running. Press Ctrl+C to exit.");
            
            // Keep alive until cancelled
            var keepAliveEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                keepAliveEvent.Set();
            };
            keepAliveEvent.WaitOne();

            StopAll();
        }

        private static void MqttService_MessageReceived(string topic, string payload)
        {
            if (topic.StartsWith("DACDT/camera/webrtc/signaling/", StringComparison.OrdinalIgnoreCase))
            {
                var parts = topic.Split('/');
                if (parts.Length >= 6)
                {
                    string clientId = parts[4];
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var dict = serializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(payload);
                    if (dict != null && dict.TryGetValue("type", out var typeVal) && typeVal != null)
                      {
                          string type = typeVal.ToString();
                          _ = webRtcCameraServer.ProcessSignalingMessageAsync(clientId, type, payload);
                      }
                  }
              }
              else if (string.Equals(topic, "DACDT/camera/command", StringComparison.OrdinalIgnoreCase))
              {
                  string cmd = payload.Trim().ToUpperInvariant();
                  if (cmd == "START" || cmd == "ON")
                  {
                      webRtcCameraServer.Start();
                  }
                  else if (cmd == "STOP" || cmd == "OFF")
                  {
                      webRtcCameraServer.Stop();
                  }
              }
          }

          private static async Task AcceptClientsAsync(CancellationToken token)
          {
              while (!token.IsCancellationRequested)
              {
                  try
                  {
                      var client = await listener.AcceptTcpClientAsync();
                      _ = Task.Run(() => HandleClientAsync(client, token));
                  }
                  catch
                  {
                      if (token.IsCancellationRequested) break;
                      await Task.Delay(1000);
                  }
              }
          }

          private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
          {
              using (client)
              using (var stream = client.GetStream())
              {
                  byte[] header = new byte[8];
                  while (!token.IsCancellationRequested)
                  {
                      // Read Magic Header & Length
                      int bytesRead = await ReadExactAsync(stream, header, 8, token);
                      if (bytesRead < 8) break;

                      // Check Magic "FRME" (0x46524D45)
                      if (header[0] != 0x46 || header[1] != 0x52 || header[2] != 0x4D || header[3] != 0x45)
                      {
                          Console.WriteLine("Invalid packet magic.");
                          break;
                      }

                      int len = BitConverter.ToInt32(header, 4);
                      if (len <= 0 || len > 10 * 1024 * 1024)
                      {
                          Console.WriteLine($"Invalid frame length: {len}");
                          break;
                      }

                      byte[] jpegBytes = new byte[len];
                      int payloadRead = await ReadExactAsync(stream, jpegBytes, len, token);
                      if (payloadRead < len) break;

                      // Feed to WebRTC Server
                      try
                      {
                          using (var ms = new MemoryStream(jpegBytes))
                          using (var bitmap = new Bitmap(ms))
                          {
                              webRtcCameraServer.SendFrame(bitmap);
                          }
                      }
                      catch (Exception ex)
                      {
                          Console.WriteLine($"Error decoding frame: {ex.Message}");
                      }
                  }
              }
          }

          private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
          {
              int offset = 0;
              while (offset < count && !token.IsCancellationRequested)
              {
                  int read = await stream.ReadAsync(buffer, offset, count - offset, token);
                  if (read == 0) return offset; // Connection closed
                  offset += read;
              }
              return offset;
          }

          private static void StopAll()
          {
              try { cts.Cancel(); } catch { }
              try { listener.Stop(); } catch { }
              try { webRtcCameraServer.Stop(); } catch { }
          }
      }
  }
