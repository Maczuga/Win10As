﻿using WinMqtt.Mqtt;
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
            var friendlyName = Utils.Settings.MqttDiscoveryFriendlyName;
            if ((friendlyName + "").Trim() == "")
                friendlyName = Utils.Settings.MqttTopic;

            var device = new Dictionary<string, object>
            {
                { "name", friendlyName },
                { "identifiers", new List<string> { UniqueId() } },
                { "sw_version", "0.0.1 Win MQTT" }
            };

            var payload = new Dictionary<string, object>
            {
                { "device", device },
            };

            return payload;
        }
        public string StateTopic(params string[] attributes) => string.Join("/", Utils.Settings.MqttTopic, WorkerType, string.Join("/", attributes));
        public string UniqueId() => string.Join("/", "win-mqtt", Utils.Settings.MqttTopic);
        public string SensorUniqueId(params string[] attributes) => string.Join("/", UniqueId(), WorkerType, string.Join("/", attributes));
        public string Name(string attribute = "") => string.Join("_", Utils.Settings.MqttTopic, WorkerType, attribute);
        protected string WorkerType => GetType().Name.ToLower();
        #endregion

        #region MQTT messages
        public abstract List<MqttMessage> PrepareDiscoveryMessages();
        public void SendDiscoveryMessages()
        {
            var msgs = PrepareDiscoveryMessages();
            if (msgs == null) return;
            foreach (var msg in msgs)
                MqttConnection.Publish(msg);
        }

        public abstract List<MqttMessage> PrepareUpdateStatusMessages();
        public void SendUpdateStatusMessages()
        {
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
            if (!IsEnabled)
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

    public enum SensorType { BinarySensor, Switch, Light, Sensor, MediaPlayer, Climate };
}
