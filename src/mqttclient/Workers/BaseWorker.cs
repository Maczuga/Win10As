using WinMqtt.Mqtt;
using System;
using System.Collections.Generic;
using System.Timers;

namespace WinMqtt.Workers
{
    public abstract class BaseWorker : IDisposable
    {
        protected abstract bool IsEnabled { get; }
        protected abstract decimal UpdateInterval { get; }

        #region Helpers
        protected virtual Dictionary<string, object> PrepareConfigPayload(params string[] sensorArgs)
        {
            var device = new Dictionary<string, object>
            {
                { "name", FriendlyName() },
                { "identifiers", new List<string> { UniqueId() } },
                { "sw_version", "0.0.1 Win MQTT" }
            };

            var payload = new Dictionary<string, object>
            {
                { "device", device },
            };

            return payload;
        }
        protected static string FriendlyName()
        {
            var friendlyName = Utils.Settings.MqttDiscoveryFriendlyName;
            if ((friendlyName + "").Trim() == "")
                friendlyName = Utils.Settings.MqttTopic;

            return friendlyName;
        }
        public string CommandTopic(params string[] attributes) => string.Join("/", Utils.Settings.MqttTopic, "cmd", WorkerType, string.Join("/", attributes));
        public string StateTopic(params string[] attributes) => string.Join("/", Utils.Settings.MqttTopic, WorkerType, string.Join("/", attributes));
        public string UniqueId() => string.Join("/", "win-mqtt", Utils.Settings.MqttTopic);
        public string SensorUniqueId(params string[] attributes) => string.Join("/", UniqueId(), WorkerType, string.Join("/", attributes));
        public string Name(string attribute = "") => string.Join("_", Utils.Settings.MqttTopic, WorkerType, attribute);
        public string WorkerFriendlyType => GetType().Name.Replace("Worker", "");
        public string WorkerType => WorkerFriendlyType.ToLower();
        #endregion

        #region MQTT messages & setup
        //protected abstract void Setup();
        protected abstract List<MqttMessage> PrepareDiscoveryMessages();
        public void SendDiscoveryMessages()
        {
            if (!IsEnabled) return;

            var msgs = PrepareDiscoveryMessages();
            if (msgs == null) return;
            foreach (var msg in msgs)
                MqttConnection.Publish(msg);
        }

        protected abstract List<MqttMessage> PrepareUpdateStatusMessages();
        public void SendUpdateStatusMessages()
        {
            if (!IsEnabled) return;

            var msgs = PrepareUpdateStatusMessages();
            if (msgs == null) return;
            foreach (var msg in msgs)
                MqttConnection.Publish(msg);
        }

        public abstract void HandleCommand(string attribute, string payload);
        #endregion

        #region Timers
        private Timer _updateTimer = null;
        public void SetUpdateTimers()
        {
            if (!IsEnabled || UpdateInterval <= 0)
            {
                if (_updateTimer != null)
                {
                    _updateTimer.Stop();
                    _updateTimer.Elapsed -= OnTimerElapsed;
                    _updateTimer.Dispose();
                }
                return;
            }

            if (_updateTimer == null)
            {
                _updateTimer = new Timer();
                _updateTimer.Elapsed += OnTimerElapsed;
            }

            _updateTimer.Enabled = IsEnabled;
            _updateTimer.Interval = Math.Max(Convert.ToDouble(UpdateInterval * 1000), 1000);
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e) => SendUpdateStatusMessages();

        public void StopUpdateTask()
        {
            if (_updateTimer == null)
                return;

            _updateTimer.Stop();
        }
        #endregion

        #region Disposing
        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing && _updateTimer != null)
            {
                _updateTimer.Elapsed -= OnTimerElapsed;
                _updateTimer.Dispose();
            }

            _disposed = disposing;
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    public enum SensorType { BinarySensor, Camera, Climate, Switch, Light, Sensor, MediaPlayer };
}
