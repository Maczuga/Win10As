using WinMqtt.Mqtt;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinMqtt.Workers
{
    class PowerWorker : BaseWorker
    {
        public PowerWorker() : base() { }

        protected override bool IsEnabled => Utils.Settings.WorkerPowerEnabled;
        protected override decimal UpdateInterval => 0;

        protected override List<MqttMessage> PrepareDiscoveryMessages() => null;
        protected override List<MqttMessage> PrepareUpdateStatusMessages() => null;

        public override void HandleCommand(string attribute, string payload)
        {
            if (!IsEnabled) return;

            switch (attribute)
            {
                case "suspend":
                    SetSuspendState(false, true, true);
                    //MqttConnection.Publish(PowerMqttMessage(false));
                    break;
                case "hibernate":
                    SetSuspendState(true, true, true);
                    //MqttConnection.Publish(PowerMqttMessage(false));
                    break;
                case "monitor_off":
                    Monitor.TurnOff();
                    //MqttConnection.Publish(MonitorMqttMessage(false));
                    break;
                case "monitor_on":
                    Monitor.TurnOn();
                    //MqttConnection.Publish(MonitorMqttMessage(true));
                    break;
                default:
                    break;
            }
        }


        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        //private MqttMessage PowerMqttMessage(bool isOn) => new MqttMessage(StateTopic("power"), isOn ? 1 : 0);
        //private MqttMessage MonitorMqttMessage(bool isOn) => new MqttMessage(StateTopic("monitor"), isOn ? 1 : 0);
    }

    public static class Monitor
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static void TurnOn()
        {
            using (var f = new Form())
            {
                SendMessage(f.Handle, 0x0112, (IntPtr)0xF170, (IntPtr)(-1));
            }
        }

        public static void TurnOff()
        {
            using (var f = new Form())
            {
                SendMessage(f.Handle, 0x0112, (IntPtr)0xF170, (IntPtr)2);
            }
        }
    }
}
