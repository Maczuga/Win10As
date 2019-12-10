using WinMqtt.Forms;
using System;
using System.Threading;
using System.Windows.Forms;
using WinMqtt.Workers;

namespace WinMqtt
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Properties.Settings.Default.SettingChanging += OnSettingsChanging;

            var form = new MainForm();
            //Application.ThreadException += new ThreadExceptionEventHandler(form.UnhandledThreadExceptionHandler);
            Application.Run(form);
        }

        private static void OnSettingsChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            switch (e.SettingName)
            {
                case "WorkerVolumeControlDevice":
                    Audio.SetDevice(e.NewValue + "");
                    break;
                case "WorkerCameraDevice":
                    Camera.SetDevice(e.NewValue + "");
                    break;
            }
        }
    }
}