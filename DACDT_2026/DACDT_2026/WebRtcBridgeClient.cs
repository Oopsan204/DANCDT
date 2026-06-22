using System;
using System.Net.Sockets;

namespace DACDT_2026
{
    public class WebRtcBridgeClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly object _lock = new object();

        public void Connect()
        {
            lock (_lock)
            {
                if (_client != null && _client.Connected) return;
                try
                {
                    _client = new TcpClient();
                    _client.SendTimeout = 2000;
                    _client.Connect("127.0.0.1", 5080);
                    _stream = _client.GetStream();
                }
                catch
                {
                    Disconnect();
                }
            }
        }

        public void SendFrame(byte[] jpegBytes)
        {
            lock (_lock)
            {
                if (_client == null || !_client.Connected || _stream == null)
                {
                    Connect();
                }

                if (_client != null && _client.Connected && _stream != null)
                {
                    try
                      {
                          byte[] header = new byte[8];
                          header[0] = 0x46; // 'F'
                          header[1] = 0x52; // 'R'
                          header[2] = 0x4D; // 'M'
                          header[3] = 0x45; // 'E'
                          
                          int len = jpegBytes.Length;
                          header[4] = (byte)(len & 0xFF);
                          header[5] = (byte)((len >> 8) & 0xFF);
                          header[6] = (byte)((len >> 16) & 0xFF);
                          header[7] = (byte)((len >> 24) & 0xFF);
                          
                          _stream.Write(header, 0, 8);
                          _stream.Write(jpegBytes, 0, len);
                      }
                      catch
                      {
                          Disconnect();
                      }
                  }
              }
          }

          public void Disconnect()
          {
              lock (_lock)
              {
                  try { _stream?.Close(); } catch { }
                  try { _client?.Close(); } catch { }
                  _stream = null;
                  _client = null;
              }
          }

          public void Dispose()
          {
              Disconnect();
          }
      }
  }
