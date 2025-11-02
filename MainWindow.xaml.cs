using NetEx.Hooks;
using System.Reflection;
using System.Windows;
using System.IO;

namespace SilentGuardian
{
    public partial class MainWindow : Window
    {
        private static MonitoringService _monitoringService = new MonitoringService();

        public MainWindow()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name + ".dll";
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs", name);

                if (File.Exists(path))
                    return Assembly.LoadFrom(path);

                return null;
            };

            InitializeComponent();
            _ = UpdateStatusText();
        }

        private async Task UpdateStatusText()
        {
            while (true)
            {
                if (_monitoringService.IsStarted)
                    StatusTextBlock.Text = "Status: Started";
                else
                    StatusTextBlock.Text = "Status: Stopped";

                await Task.Delay(AppHelper.MONITOR_DELAY_MS);
            }
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_monitoringService.IsStarted) return;

            KeyboardHook.KeyDown += KeyboardHook_KeyDown;
            MouseHook.MouseClick += MouseHook_MouseClick;
            MouseHook.MouseMove += MouseHook_MouseMove;

            await _monitoringService.Start();

            KeyboardHook.Install();
            MouseHook.Install();
        }

        public async void Stop_Click(object sender, RoutedEventArgs e)
        {
            await Stop();
        }

        public static async Task Stop()
        {
            if (!_monitoringService.IsStarted) return;

            await _monitoringService.Stop();
            MonitoringActivityStats.ResetActivity();

            KeyboardHook.Uninstall();
            MouseHook.Uninstall();
        }

        private void KeyboardHook_KeyDown(NetEx.Hooks.KeyEventArgs e)
        {
            if (AppHelper.AllKeys.Contains(e.KeyCode))
                MonitoringActivityStats.PressedKeysCount++;
        }

        private void MouseHook_MouseClick(NetEx.Hooks.MouseEventArgs e)
        {
            MonitoringActivityStats.PressedMouseClicksCount++;
        }

        private void MouseHook_MouseMove(NetEx.Hooks.MouseEventArgs e)
        {
            if (MonitoringActivityStats.StartCordinates is null)
                MonitoringActivityStats.StartCordinates = new global::System.Drawing.Point(e.X, e.Y);

            MonitoringActivityStats.DifferenceBetweenX = Math.Abs(e.X - MonitoringActivityStats.StartCordinates.Value.X);
            MonitoringActivityStats.DifferenceBetweenY = Math.Abs(e.Y - MonitoringActivityStats.StartCordinates.Value.Y);
        }
    }
}