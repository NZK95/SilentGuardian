using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SilentGuardian
{
    public class MonitoringService
    {
        public static bool IsStarted { get; private set; } = false;
        public static bool DisableScreenshots { get; set; } = false;
        public static bool IsAlarmMessageSent { get; private set; }
        public static bool IsScreenRecording { get; private set; }
        public static bool AreScreenshotsTaking { get; set; }

        public Task? MonitoringTask { get; private set; }
        public static Process? VideoRecordingProcess { get; private set; }

        public async Task Start()
        {
            await MainWindow.TelegramBot.SendMessageAsync("✅ Guardian started.");

            IsStarted = true;
            IsAlarmMessageSent = IsScreenRecording = AreScreenshotsTaking = DisableScreenshots = false;
            MonitoringTask = Task.Run(() => MonitorMetricsAsync());
        }

        public async Task Stop()
        {
            await MainWindow.TelegramBot.SendMessageAsync("🛑 Guardian stopped.");

            IsStarted = IsScreenRecording = AreScreenshotsTaking = IsAlarmMessageSent = DisableScreenshots = false;
            MonitoringTask = null;
            await StopVideoRecording();
        }

        public static async Task StopVideoRecording()
        {
            if (MonitoringConfig.LogicOptions.RecordScreen && VideoRecordingProcess != null)
            {
                await MainWindow.TelegramBot.SendMessageAsync("🛑 Video recording stopped.");
                await VideoRecordingProcess.StandardInput.WriteLineAsync("q");
                await VideoRecordingProcess.WaitForExitAsync();
                VideoRecordingProcess.Dispose();
                VideoRecordingProcess = null;
            }
        }

        public static async Task LockScreen()
        {
            if(MonitoringService.IsStarted)
                await MainWindow.Stop();

            SystemUtils.LockWorkStation();
        }

        private async Task MonitorMetricsAsync()
        {
            while (!MainWindow.Cts.Token.IsCancellationRequested)
            {
                if (AreScreenshotsTaking || IsScreenRecording || !Utils.IsActivityDetected())
                {
                    await Task.Delay(AppHelper.MONITOR_DELAY_MS, MainWindow.Cts.Token);
                    continue;
                }

                await HandleActivityDetectedAsync();
                await Task.Delay(AppHelper.MONITOR_DELAY_MS, MainWindow.Cts.Token);
            }
        }

        private async Task HandleActivityDetectedAsync()
        {
            if (!IsAlarmMessageSent)
            {
                await MainWindow.TelegramBot.SendMessageAsync(Utils.BuildAlarmMessage());
                IsAlarmMessageSent = true;
            }

            if (MonitoringConfig.LogicOptions.AutoLock)
            {
                await MainWindow.TelegramBot.SendMessageAsync("🔒 Automatic window locking.");
                await LockScreen();
            }

            if (MonitoringConfig.LogicOptions.TakeScreenshots && !AreScreenshotsTaking && !DisableScreenshots)
            {
                await MainWindow.TelegramBot.SendMessageAsync(Utils.BuildScreenshotMessage());
                AreScreenshotsTaking = true;
                _ = StartScreenshotsAsync();
            }

            if (MonitoringConfig.LogicOptions.RecordScreen && !IsScreenRecording)
            {
                await MainWindow.TelegramBot.SendMessageAsync(Utils.BuildVideoRecordingMessage());
                IsScreenRecording = true;
                _ = StartScreenRecording();
            }
        }

        private async Task StartScreenshotsAsync()
        {
            while (AreScreenshotsTaking)
            {
                if (MainWindow.Cts.Token.IsCancellationRequested)
                    return;

                await MakeScreenshotAndSendAsync();
                await Task.Delay(MonitoringConfig.Thresholds.ScreenshotTimeIntervalMs, MainWindow.Cts.Token);
            }
        }

        private async Task StartScreenRecording()
        {
            if (MainWindow.Cts.Token.IsCancellationRequested)
                return;

            var pathToVideoRecord = Utils.GetСurrentRecordFilePath();
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

        public static async Task MakeScreenshotAndSendAsync()
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

                await MainWindow.TelegramBot.SendImageAsync(
                    imagePath: currentScreenshotPath,
                    caption: caption
                    );
            }
        }
    }
}
