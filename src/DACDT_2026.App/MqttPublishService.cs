using System;
using System.Collections.Concurrent;
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
    public class MqttPublishService : IDisposable
    {
        private readonly IMqttClient _mqttClient;
        private MqttClientOptions _options;
        private bool _shouldBeConnected;
        private static readonly MqttFactory MqttFactory = new MqttFactory();

        // Connection Synchronization
        private readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);
        private int _reconnectScheduled = 0;

        // Background Message Queue
        private const int MaxQueueSize = 200;
        private readonly ConcurrentQueue<MqttApplicationMessage> _publishQueue = new ConcurrentQueue<MqttApplicationMessage>();
        private readonly SemaphoreSlim _queueSemaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _brokerPublishSemaphore = new SemaphoreSlim(1, 1);
        private readonly object _queueLock = new object();
        private Task _queueProcessorTask;
        private CancellationTokenSource _queueCts;

        // Subscription tracking (locked by _subscriptionLock)
        private readonly object _subscriptionLock = new object();
        private readonly List<string> _subscriptions = new List<string>();

        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<string, string> MessageReceived;
        public event Action<string, byte[]> BinaryMessageReceived;

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
                .WithTlsOptions(o => o.UseTls())
                .WithClientId("DACDT_2026_" + Guid.NewGuid().ToString().Substring(0, 8))
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30)) // Optimized keep-alive interval
                .WithTimeout(TimeSpan.FromSeconds(10))
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311);

            if (!string.IsNullOrEmpty(username))
            {
                builder.WithCredentials(username, password);
            }

            _options = builder.Build();
            _shouldBeConnected = true;

            // Start queue processor
            StartQueueProcessor();

            await TryConnectAsync();
        }

        private async Task TryConnectAsync()
        {
            if (IsConnected) return;

            // Prevent concurrent connection attempts
            if (!await _connectionSemaphore.WaitAsync(0))
            {
                return;
            }

            try
            {
                if (IsConnected) return;
                Console.WriteLine("[MQTT] Attempting to connect to broker...");
                
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    await _mqttClient.ConnectAsync(_options, timeoutCts.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT Connection Error: {ex.Message}");
                ScheduleReconnect();
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        private void ScheduleReconnect()
        {
            if (!_shouldBeConnected) return;

            if (Interlocked.CompareExchange(ref _reconnectScheduled, 1, 0) == 0)
            {
                Console.WriteLine("[MQTT] Scheduling reconnect attempt in 5 seconds...");
                _ = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(async t =>
                {
                    Interlocked.Exchange(ref _reconnectScheduled, 0);
                    await TryConnectAsync();
                });
            }
        }

        private async Task OnConnected(MqttClientConnectedEventArgs arg)
        {
            Console.WriteLine("Successfully connected to MQTT broker.");
            Connected?.Invoke();
            await ResubscribeAsync();
        }

        private Task OnDisconnected(MqttClientDisconnectedEventArgs arg)
        {
            Console.WriteLine("Disconnected from MQTT broker.");
            Disconnected?.Invoke();

            if (_shouldBeConnected)
            {
                ScheduleReconnect();
            }

            return Task.CompletedTask;
        }

        public Task PublishAsync(string topic, string payload, bool retain = false)
        {
            EnqueuePublish(topic, payload != null ? Encoding.UTF8.GetBytes(payload) : Array.Empty<byte>(), retain);
            return Task.CompletedTask;
        }

        public Task PublishAsync(string topic, byte[] payload, bool retain = false)
        {
            EnqueuePublish(topic, payload, retain);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Publishes one message immediately, preserving order with the background queue.
        /// Used for multi-part CAD transfers so a large transfer cannot be silently dropped
        /// by the bounded general-purpose queue.
        /// </summary>
        public async Task PublishDirectAsync(string topic, string payload, bool retain = false)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(topic))
                return;

            var message = BuildMessage(topic, payload != null ? Encoding.UTF8.GetBytes(payload) : Array.Empty<byte>(), retain);
            await _brokerPublishSemaphore.WaitAsync();
            try
            {
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    await _mqttClient.PublishAsync(message, timeoutCts.Token);
                }
            }
            finally
            {
                _brokerPublishSemaphore.Release();
            }
        }

        private void EnqueuePublish(string topic, byte[] payload, bool retain)
        {
            var message = BuildMessage(topic, payload, retain);

            lock (_queueLock)
            {
                _publishQueue.Enqueue(message);
                if (_publishQueue.Count > MaxQueueSize)
                {
                    // Drop the oldest message to control queue size
                    if (_publishQueue.TryDequeue(out _))
                    {
                        return; // Do not increment semaphore as we just replaced an item
                    }
                }
                _queueSemaphore.Release();
            }
        }

        private static MqttApplicationMessage BuildMessage(string topic, byte[] payload, bool retain)
        {
            return new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload ?? Array.Empty<byte>())
                .WithRetainFlag(retain)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();
        }

        private void StartQueueProcessor()
        {
            lock (_queueLock)
            {
                if (_queueProcessorTask != null) return;
                _queueCts = new CancellationTokenSource();
                _queueProcessorTask = Task.Run(() => ProcessQueueAsync(_queueCts.Token));
                Console.WriteLine("[MQTT] Background queue processor started.");
            }
        }

        private void StopQueueProcessor()
        {
            CancellationTokenSource cts = null;
            Task task = null;

            lock (_queueLock)
            {
                cts = _queueCts;
                task = _queueProcessorTask;
                _queueCts = null;
                _queueProcessorTask = null;
            }

            if (cts != null)
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch { }
            }

            // Clear the queue
            lock (_queueLock)
            {
                while (_publishQueue.TryDequeue(out _)) { }
                try
                {
                    while (_queueSemaphore.CurrentCount > 0)
                    {
                        _queueSemaphore.Wait(0);
                    }
                }
                catch { }
            }
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _queueSemaphore.WaitAsync(cancellationToken);

                    MqttApplicationMessage message = null;
                    lock (_queueLock)
                    {
                        _publishQueue.TryDequeue(out message);
                    }

                    if (message != null)
                    {
                        // Wait until connected
                        while (!cancellationToken.IsCancellationRequested && !IsConnected)
                        {
                            await Task.Delay(100, cancellationToken);
                        }

                        if (cancellationToken.IsCancellationRequested) break;

                        // Publish with a timeout (5 seconds) to prevent hanging
                        using (var publishTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                        {
                            publishTimeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
                            try
                            {
                            await _brokerPublishSemaphore.WaitAsync(publishTimeoutCts.Token);
                            try
                            {
                                await _mqttClient.PublishAsync(message, publishTimeoutCts.Token);
                            }
                            finally
                            {
                                _brokerPublishSemaphore.Release();
                            }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[MQTT] Background publish error: {ex.Message}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MQTT] Background processor error: {ex.Message}");
                    try
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (OperationCanceledException) { break; }
                }
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

            if (!IsConnected || topicsToSubscribe.Count == 0)
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
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    await _mqttClient.SubscribeAsync(builder.Build(), timeoutCts.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error subscribing MQTT topics: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            _shouldBeConnected = false;
            
            StopQueueProcessor();

            if (_mqttClient != null)
            {
                await _connectionSemaphore.WaitAsync();
                try
                {
                    await _mqttClient.DisconnectAsync();
                }
                catch { }
                finally
                {
                    _connectionSemaphore.Release();
                }
            }
        }

        private Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs arg)
        {
            try
            {
                string topic = arg.ApplicationMessage?.Topic ?? string.Empty;
                ArraySegment<byte> payload = arg.ApplicationMessage?.PayloadSegment ?? default(ArraySegment<byte>);
                if (topic.StartsWith("DACDT/cad/upload/binary/", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = new byte[payload.Count];
                    if (payload.Array != null && payload.Count > 0)
                        Buffer.BlockCopy(payload.Array, payload.Offset, bytes, 0, payload.Count);

                    BinaryMessageReceived?.Invoke(topic, bytes);
                    return Task.CompletedTask;
                }

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

        public void Dispose()
        {
            _shouldBeConnected = false;
            StopQueueProcessor();
            
            try
            {
                _mqttClient?.Dispose();
            }
            catch { }

            try
            {
                _connectionSemaphore?.Dispose();
            }
            catch { }

            try
            {
                _queueSemaphore?.Dispose();
            }
            catch { }
        }
    }
}
