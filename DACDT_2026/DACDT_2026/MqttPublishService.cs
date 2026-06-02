using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace DACDT_2026
{
    public class MqttPublishService
    {
        private IMqttClient _mqttClient;
        private MqttClientOptions _options;
        private bool _isConnected;
        private static readonly MqttFactory MqttFactory = new MqttFactory();

        public bool IsConnected => _isConnected;

        public event Action Connected;
        public event Action Disconnected;

        public MqttPublishService()
        {
            _mqttClient = MqttFactory.CreateMqttClient();
            _mqttClient.ConnectedAsync += OnConnected;
            _mqttClient.DisconnectedAsync += OnDisconnected;
        }

        public async Task ConnectAsync(string broker, string username, string password)
        {
            Console.WriteLine($"[DEBUG] Connecting to MQTT: broker={broker}, user={username}");
            
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, 8883)
                .WithTls()
                .WithClientId("DACDT_2026_" + Guid.NewGuid().ToString().Substring(0, 8))
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .WithTimeout(TimeSpan.FromSeconds(10))
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311);

            if (!string.IsNullOrEmpty(username))
            {
                builder.WithCredentials(username, password);
            }

            _options = builder.Build();

            await TryConnectAsync();
        }

        private async Task TryConnectAsync()
        {
            if (_isConnected) return;
            try
            {
                await _mqttClient.ConnectAsync(_options, CancellationToken.None);
            }
            catch (MqttCommunicationException ex)
            {
                Console.WriteLine($"MQTT Connection Error: {ex.Message}");
                _isConnected = false;
                Disconnected?.Invoke();
                // Schedule a reconnect attempt
                _ = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => TryConnectAsync());
            }
        }

        private Task OnConnected(MqttClientConnectedEventArgs arg)
        {
            _isConnected = true;
            Console.WriteLine("Successfully connected to MQTT broker.");
            Connected?.Invoke();
            return Task.CompletedTask;
        }

        private Task OnDisconnected(MqttClientDisconnectedEventArgs arg)
        {
            _isConnected = false;
            Console.WriteLine("Disconnected from MQTT broker.");
            Disconnected?.Invoke();

            // Only attempt to reconnect if the disconnection was unexpected
            if (arg.ClientWasConnected)
            {
                _ = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => TryConnectAsync());
            }

            return Task.CompletedTask;
        }

        public async Task PublishAsync(string topic, string payload, bool retain = false)
        {
            if (!_isConnected)
            {
                Console.WriteLine("Cannot publish message, MQTT client is not connected.");
                return;
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(retain)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            
            try
            {
                await _mqttClient.PublishAsync(message, CancellationToken.None);
            }
            catch (MqttCommunicationException ex)
            {
                 Console.WriteLine($"Error publishing MQTT message: {ex.Message}");
            }
        }
    }
}