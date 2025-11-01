using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SilentGuardian
{
    public class MonitoringService
    {
        public bool IsStarted { get; private set; } = false;
        public bool IsAlarmMessageSent { get; private set; }
        public bool AreScreenshotsTaking { get; private set; }
        public bool IsScreenRecording { get; private set; }

        public CancellationTokenSource? Cts { get; private set; }
        public TelegramBot? TelegramBot { get; private set; }
        public Task? MonitoringTask { get; private set; }
        public static Process? VideoRecordingProcess { get; private set; }

        public async Task Start()
        {
            await InitializeTelegramBotAndToken();
            await TelegramBot.SendMessageAsync("✅ Started.");

            IsStarted = true;
            IsAlarmMessageSent = IsScreenRecording = AreScreenshotsTaking = false;

            MonitoringTask = Task.Run(() => MonitorMetricsAsync());
        }

        public async Task Stop()
        {
            await TelegramBot.SendMessageAsync("🛑 Stopped.");

            IsScreenRecording = AreScreenshotsTaking = IsStarted = false;

            await ResetCancellationToken();
            await StopVideoRecording();

            MonitoringTask = null;
            TelegramBot = null;
            Cts = null;
        }

        public static async Task StopVideoRecording()
        {
            if (MonitoringConfig.LogicOptions.RecordScreen && VideoRecordingProcess != null)
            {
                await VideoRecordingProcess.StandardInput.WriteLineAsync("q");
                await VideoRecordingProcess.WaitForExitAsync();
                VideoRecordingProcess.Dispose();
                VideoRecordingProcess = null;
            }
        }

        private async Task InitializeTelegramBotAndToken()
        {
            Cts = new CancellationTokenSource();
            TelegramBot = new TelegramBot(Cts);

            await TelegramBot.InitAsync();
        }

        private async Task ResetCancellationToken()
        {
            try { Cts.Cancel(); }
            catch (Exception) { }
            finally { Cts.Dispose(); }
        }

        private async Task MonitorMetricsAsync()
        {
            while (!Cts.Token.IsCancellationRequested)
            {
                if (AreScreenshotsTaking || IsScreenRecording || !Utils.IsActivityDetected())
                {
                    await Task.Delay(AppHelper.MONITOR_DELAY_MS, Cts.Token);
                    continue;
                }

                await HandleActivityDetectedAsync();

                await Task.Delay(AppHelper.MONITOR_DELAY_MS, Cts.Token);
            }
        }

        private async Task HandleActivityDetectedAsync()
        {
            if (!IsAlarmMessageSent)
            {
                await SendMessageAsync(Utils.BuildAlarmMessage());
                IsAlarmMessageSent = true;
            }

            if (MonitoringConfig.LogicOptions.AutoLock)
            {
                await SendMessageAsync("🔒 Automatic window locking.");
                await MainWindow.Stop();
                SystemUtils.LockWorkStation();
            }

            if (MonitoringConfig.LogicOptions.TakeScreenshots && !AreScreenshotsTaking)
            {
                AreScreenshotsTaking = true;
                await SendMessageAsync(Utils.BuildScreenshotMessage());
                _ = StartScreenshotsAsync();
            }

            if (MonitoringConfig.LogicOptions.RecordScreen && !IsScreenRecording)
            {
                IsScreenRecording = true;
                await SendMessageAsync(Utils.BuildVideoRecordingMessage());
                _ = StartScreenRecording();
            }
        }

        private async Task SendMessageAsync(string message)
        {
            await TelegramBot.SendMessageAsync(message);
        }

        private async Task StartScreenshotsAsync()
        {
            while (AreScreenshotsTaking)
            {
                if (Cts.Token.IsCancellationRequested)
                    return;

                await MakeScreenshotAsync();
                await Task.Delay(MonitoringConfig.Thresholds.ScreenshotTimeIntervalMs, Cts.Token);
            }
        }

        private async Task StartScreenRecording()
        {
            if (Cts.Token.IsCancellationRequested)
                return;

            var pathToVideoRecord = Path.Combine(MonitoringConfig.BasePath, Utils.GetRecordFilePath());
            var psi = GetProcessStartInfo(pathToVideoRecord);

            VideoRecordingProcess = Process.Start(psi);
        }

        private static ProcessStartInfo GetProcessStartInfo(string pathToVideoRecord)
        {
            return new ProcessStartInfo
            {
                FileName = MonitoringConfig.PathToffmpeg,
                Arguments = AppHelper.FFMPEG_ARGUMENTS + $"\"{pathToVideoRecord}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
        }

        private async Task MakeScreenshotAsync()
        {
            var width = SystemUtils.GetSystemMetrics(SystemUtils.SM_CXSCREEN);
            var height = SystemUtils.GetSystemMetrics(SystemUtils.SM_CYSCREEN);

            using (Bitmap bmp = new Bitmap(width, height))
            {
                using Graphics g = Graphics.FromImage(bmp);
                g.CopyFromScreen(0, 0, 0, 0, bmp.Size);

                var currentScreenshotPath = Utils.GetCurrentScreenshotPath();
                var caption = Utils.GetCurrentScreenshotName();

                bmp.Save(currentScreenshotPath, ImageFormat.Png);

                await TelegramBot.SendImageAsync(
                    imagePath: currentScreenshotPath,
                    caption: caption
                    );
            }
        }
    }
}
