using NetEx.Hooks;
using System.Reflection;
using System.Windows;
using System.IO;

namespace SilentGuardian
{
    public partial class MainWindow : Window
    {
        public static MonitoringService MonitoringService { get; private set; }
        public static CancellationTokenSource? Cts { get; private set; }
        public static TelegramBot? TelegramBot { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeBotAndServices();
            _ = UpdateStatusText();
        }

        private async Task InitializeBotAndServices()
        {
            MonitoringService = new MonitoringService();
            TelegramBot = new TelegramBot();

            await TelegramBot.InitAsync();
            await TelegramBot.SendMessageAsync("▶️ The program is running.");
        }

        private async Task UpdateStatusText()
        {
            const string STARTED = "Status: Started";
            const string STOPPED = "Status: Stopped";

            while (true)
            {
                if (MonitoringService.IsStarted && StatusTextBlock.Text != STARTED)
                    StatusTextBlock.Text = STARTED;

                if (!MonitoringService.IsStarted && StatusTextBlock.Text != STOPPED)
                    StatusTextBlock.Text = STOPPED;

                await Task.Delay(AppHelper.MONITOR_DELAY_MS);
            }
        }

        private static DateTime _lastMessageTime = DateTime.MinValue;
        private static readonly TimeSpan _messageCooldown = TimeSpan.FromSeconds(3);

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (MonitoringService.IsStarted)
            {
                if (DateTime.Now - _lastMessageTime > _messageCooldown)
                {
                    await TelegramBot.SendMessageAsync("⚠️ Already started.");
                    _lastMessageTime = DateTime.Now;
                }
                return;
            }

            KeyboardHook.KeyDown += KeyboardHook_KeyDown;
            MouseHook.MouseClick += MouseHook_MouseClick;
            MouseHook.MouseMove += MouseHook_MouseMove;

            Cts = new CancellationTokenSource();
            await MonitoringService.Start();

            KeyboardHook.Install();
            MouseHook.Install();
        }

        public static async Task Stop()
        {
            if (!MonitoringService.IsStarted)
            {
                if (DateTime.Now - _lastMessageTime > _messageCooldown)
                {
                    await TelegramBot.SendMessageAsync("⚠️ Already stopped.");
                    _lastMessageTime = DateTime.Now;
                }
                return;
            }

            await ResetCancellationToken();
            await MonitoringService.Stop();
            MonitoringActivityStats.ResetActivity();

            KeyboardHook.Uninstall();
            MouseHook.Uninstall();
        }


        private static async Task ResetCancellationToken()
        {
            try { Cts.Cancel(); }
            catch (Exception) { }
            finally { Cts.Dispose(); }
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

        private async void Stop_Click(object sender, RoutedEventArgs e) => await Stop();
    }
}