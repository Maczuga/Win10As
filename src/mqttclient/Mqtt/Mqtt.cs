using System;
using System.Collections.Generic;
using System.Text;
using WinMqtt.Workers;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Threading.Tasks;

namespace WinMqtt.Mqtt
{
    public static class MqttConnection
    {
        private static MqttClient _client = null;
        public static bool IsConnected => _client == null ? false : _client.IsConnected;
        private static bool _isConnecting = false;

        private static Dictionary<string, BaseWorker> _workers = new Dictionary<string, BaseWorker>();
        public static Dictionary<string, BaseWorker> Workers
        {
            get
            {
                if (_workers.Count != 0)
                    return _workers;

                var workers = ReflectiveEnumerator.GetEnumerableOfType<BaseWorker>();
                foreach (var worker in workers)
                    _workers.Add(worker.WorkerType, worker);

                return _workers;
            }
        }

        private static string CommandPrefix => $"{Utils.Settings.MqttTopic}/cmd";

        public static bool Connect()
        {
            if (IsConnected)
                return true;

            if (_isConnecting)
                return false;

            var hostname = Utils.Settings.MqttServer;
            var port = Convert.ToInt32(Utils.Settings.MqttPort);
            var username = Utils.Settings.MqttUsername;
            var password = Utils.Settings.MqttPassword;

            if (hostname + "" == "" || port <= 0)
                return false;

            var task = Task.Run(() =>
            {
                _isConnecting = true;
                _client = new MqttClient(hostname, port, false, null, null, MqttSslProtocols.None, null);
                _client.Connect(Guid.NewGuid().ToString(), username, password);
            });

            var taskWait = task.Wait(TimeSpan.FromSeconds(10));
            _isConnecting = false;

            if (!taskWait)
                return false;

            if (!IsConnected)
                return false;

            _client.MqttMsgPublishReceived += OnMqttMsgReceived;
            _client.MqttMsgSubscribed += OnMqttSubscribed;
            _client.MqttMsgUnsubscribed += OnMqttUnsubscribed;
            _client.ConnectionClosed += ClientMqttConnectionClosed;

            _client.Subscribe(new[] { $"{CommandPrefix}/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

            return IsConnected;
        }

        private static void OnMqttUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            Disconnect();
            Connect();
        }

        private static void OnMqttMsgReceived(object sender, MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message);
            Log.Add("Message recived " + e.Topic + " value " + message);

            var subTopic = e.Topic.Replace(CommandPrefix + "/", "");
            var parts = subTopic.Split('/');

            var workerName = parts[0];
            var attribute = parts[1];

            if (!Workers.ContainsKey(workerName))
                return;

            var worker = Workers[workerName];
            worker.HandleCommand(attribute, message);
        }

        public static void InitializeWorkers()
        {
            foreach (var kvp in Workers)
            {
                var worker = kvp.Value;
                worker.SendDiscoveryMessages();
                worker.SetUpdateTimers();
            }
        }

        private static void OnMqttSubscribed(object sender, MqttMsgSubscribedEventArgs e) => InitializeWorkers();

        private static void ClientMqttConnectionClosed(object sender, EventArgs e)
        {
            _client.MqttMsgPublishReceived -= OnMqttMsgReceived;
            _client.MqttMsgSubscribed -= OnMqttSubscribed;
            _client.MqttMsgUnsubscribed -= OnMqttUnsubscribed;
            _client.ConnectionClosed -= ClientMqttConnectionClosed;

            Log.Add("Mqtt connection closed");
        }

        public static void Publish(MqttMessage message)
        {
            Connect();

            if (!IsConnected)
            {
                Log.Add($"Failed to publish: '{message.Topic}' - not connected to the server.");
                return;
            }

            string strPayload = null;
            byte[] payload;
            if (message.Payload is byte[])
                payload = (byte[])message.Payload;
            else
            {
                if (message.Payload is string)
                    strPayload = message.Payload + "";
                else
                    strPayload = JsonConvert.SerializeObject(message.Payload);

                payload = Encoding.UTF8.GetBytes(strPayload);
            }

            _client.Publish(message.Topic, payload, 0, message.Retain);
            Log.Add($"Message published: '{message.Topic}', payload: '{strPayload ?? message.Payload}'");
        }

        public static void Disconnect()
        {
            if (_client == null)
                return;

            foreach (var kvp in Workers)
            {
                var worker = kvp.Value;
                worker.StopUpdateTask();
            }

            _client.Disconnect();
            _client = null;
        }
    }
}