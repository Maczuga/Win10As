using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WinMqtt.Mqtt;

namespace WinMqtt.Workers
{
    class CameraWorker : BaseWorker
    {
        protected override bool IsEnabled => Utils.Settings.WorkerCameraEnabled;
        protected override decimal UpdateInterval => Utils.Settings.WorkerCameraInterval;

        protected void Setup()
        {
            Camera.SetDevice(IsEnabled ? Utils.Settings.WorkerCameraDevice : "");
        }

        protected override List<MqttMessage> PrepareDiscoveryMessages()
        {
            var result = new List<MqttMessage>();

            var payload = PrepareConfigPayload();
            payload.Add("unique_id", SensorUniqueId("live"));
            payload.Add("name", $"{FriendlyName()} - {WorkerFriendlyType} - live");
            payload.Add("topic", StateTopic("live"));

            // Another HomeAssistant weirdness - camera is documented to be able to receive "device" dictionary, but there is no code support, thus throwing errors.
            // Reported here: https://github.com/home-assistant/home-assistant/issues/26024
            payload.Remove("device");

            var mqttMsg = new MqttConfigMessage(SensorType.Camera, $"{WorkerType}_live", payload);
            result.Add(mqttMsg);

            return result;
        }

        protected override List<MqttMessage> PrepareUpdateStatusMessages()
        {
            var result = new List<MqttMessage>();

            Camera.Save();
            result.Add(new MqttImageMessage(StateTopic("live"), Camera.GetRecentCapture()));

            return result;
        }

        public override void HandleCommand(string attribute, string payload)
        {
            switch (attribute)
            {
                case "snapshot":
                    Camera.Save(payload);
                    break;
            }
        }
    }

    static class Camera
    {
        private static VideoCaptureDevice _videoDevice;
        static Camera() => SetDevice(Utils.Settings.WorkerCameraDevice);

        private static string SaveLocation => Utils.Settings.WorkerCameraSaveLocation + "" != ""
                ? Utils.Settings.WorkerCameraSaveLocation
                : Path.Combine(Path.GetTempPath(), "win-mqtt");

        public static List<string> GetDevices()
        {
            var result = new List<string>();
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in videoDevices)
                result.Add(device.Name);

            return result;
        }

        public static void SetDevice(string name)
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (_videoDevice != null)
            {
                _videoDevice.NewFrame -= HandleNewFrame;
                _videoDevice.SignalToStop();
                _videoDevice.WaitForStop();
            }

            if (name + "" == "")
                return;

            foreach (FilterInfo device in videoDevices)
            {
                if (device.Name != name) continue;
                _videoDevice = new VideoCaptureDevice(device.MonikerString);
                _videoDevice.NewFrame += HandleNewFrame;
                _videoDevice.Start();
                break;
            }
        }

        public static void Save(string fileName = "", bool preview = false)
        {
            Cursor.Current = Cursors.WaitCursor;

            Capture = true;

            // Wait for task to finish
            while (Frame == null) { }

            if (!Directory.Exists(SaveLocation))
                Directory.CreateDirectory(SaveLocation);

            if (fileName + "" == "")
                fileName = $"auto_{Guid.NewGuid().ToString()}";

            var path = Path.Combine(SaveLocation, $"{fileName}.jpg");
            Frame.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
            Frame.Dispose();

            if (preview)
                Process.Start(path);

            var files = Directory.GetFiles(SaveLocation, "auto_*.jpg");
            for (var i = 100; i < files.Length; i++)
                File.Delete(files[i]);

            Cursor.Current = Cursors.Default;
        }

        private static bool Capture = false;
        private static Bitmap Frame = null;

        private static void HandleNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!Capture) return;

            Frame = (Bitmap)eventArgs.Frame.Clone();
            Capture = false;
        }

        public static string GetRecentCapture()
        {
            var directory = new DirectoryInfo(SaveLocation);
            var file = directory.GetFiles()
                         .OrderByDescending(f => f.LastWriteTime)
                         .First();

            return file.FullName;
        }
    }
}
