using System;
using System.Collections.Generic;
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
        private readonly object _subscriptionLock = new object();
        private readonly List<string> _subscriptions = new List<string>();
        private static readonly MqttFactory MqttFactory = new MqttFactory();

        public bool IsConnected => _isConnected;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<string, string> MessageReceived;

        public MqttPublishService()
        {
            _mqttClient = MqttFactory.CreateMqttClient();
            _mqttClient.ConnectedAsync += OnConnected;
            _mqttClient.DisconnectedAsync += OnDisconnected;
            _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceived;
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

        private async Task OnConnected(MqttClientConnectedEventArgs arg)
        {
            _isConnected = true;
            Console.WriteLine("Successfully connected to MQTT broker.");
            Connected?.Invoke();
            await ResubscribeAsync();
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
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
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

        public async Task PublishAsync(string topic, byte[] payload, bool retain = false)
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
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
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

        public async Task SubscribeAsync(params string[] topics)
        {
            var topicsToSubscribe = new List<string>();
            lock (_subscriptionLock)
            {
                foreach (string topic in topics ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(topic))
                        continue;

                    if (!_subscriptions.Contains(topic))
                        _subscriptions.Add(topic);

                    topicsToSubscribe.Add(topic);
                }
            }

            if (!_isConnected || topicsToSubscribe.Count == 0)
                return;

            await SubscribeTopicsAsync(topicsToSubscribe);
        }

        private async Task ResubscribeAsync()
        {
            List<string> topics;
            lock (_subscriptionLock)
                topics = new List<string>(_subscriptions);

            if (topics.Count == 0)
                return;

            await SubscribeTopicsAsync(topics);
        }

        private async Task SubscribeTopicsAsync(IEnumerable<string> topics)
        {
            var builder = new MqttClientSubscribeOptionsBuilder();
            bool hasTopic = false;

            foreach (string topic in topics)
            {
                if (string.IsNullOrWhiteSpace(topic))
                    continue;

                builder.WithTopicFilter(
                    topic,
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    false,
                    false,
                    MqttRetainHandling.SendAtSubscribe);
                hasTopic = true;
            }

            if (!hasTopic)
                return;

            try
            {
                await _mqttClient.SubscribeAsync(builder.Build(), CancellationToken.None);
            }
            catch (MqttCommunicationException ex)
            {
                Console.WriteLine($"Error subscribing MQTT topics: {ex.Message}");
            }
        }

        private Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs arg)
        {
            try
            {
                string topic = arg.ApplicationMessage?.Topic ?? string.Empty;
                ArraySegment<byte> payload = arg.ApplicationMessage?.PayloadSegment ?? default(ArraySegment<byte>);
                string text = payload.Array == null
                    ? string.Empty
                    : Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count);

                MessageReceived?.Invoke(topic, text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling MQTT message: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
