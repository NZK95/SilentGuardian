using System.IO;

namespace SilentGuardian
{
    public static class Utils
    {
        public static string GetCurrentScreenshotName()
        {
            return $"Screenshot - {DateTime.Now.ToString("dd MMM yyyy - HH-mm-ss")}";
        }

        public static string GetCurrentScreenshotPath()
        {
            var screenshotPath = @$"{GetCurrentScreenshotName()}.png";
            return Path.Combine(MonitoringConfig.BasePath, screenshotPath);
        }

        public static string GetRecordFileName()
        {
            return $"Video record - {DateTime.Now.ToString("dd MMM yyyy - HH-mm-ss")}";
        }

        public static string GetRecordFilePath()
        {
            var recordFilePath = @$"{GetRecordFileName()}.mp4";
            return Path.Combine(MonitoringConfig.BasePath, recordFilePath);
        }

        public static string BuildAlarmMessage()
        {
            var t = MonitoringConfig.Thresholds;

            return $"🚨 Alarm - for " +
                     $"{DateTime.Now.ToString("dd/M - H:m")}\n\n" +
                     $"📊Activity Stats:\n" +
                     $"- Pressed keys (Threshold {t.PressedKeysThreshold}) - {MonitoringActivityStats.PressedKeysCount}\n" +
                     $"- Pressed mouse clicks (Threshold {t.PressedMouseClicksThreshold}) - {MonitoringActivityStats.PressedMouseClicksCount}\n" +
                     $"- Axis X movement (Threshold {t.MouseAxisXThreshold}) - {MonitoringActivityStats.DifferenceBetweenX}\n" +
                     $"- Axis Y movement (Threshold {t.MouseAxisYThreshold}) - {MonitoringActivityStats.DifferenceBetweenY}\n";
        }

        public static string BuildScreenshotMessage()
        {
            return "📷 Start taking screenshots";
        }

        public static string BuildVideoRecordingMessage()
        {
            return "📹 Start video recording";
        }

        public static string BuildAboutMessage()
        {
            return "🌐 Developer: https://www.github.com/NZK95" +
                        "\n🌐 SilentGuardian: https://www.github.com/NZK95/SilentGuarding";
        }

        public static bool IsActivityDetected()
        {
            return MonitoringActivityStats.PressedKeysCount >= MonitoringConfig.Thresholds?.PressedKeysThreshold ||
                    MonitoringActivityStats.PressedMouseClicksCount >= MonitoringConfig.Thresholds?.PressedMouseClicksThreshold ||
                    MonitoringActivityStats.DifferenceBetweenY >= MonitoringConfig.Thresholds?.MouseAxisYThreshold ||
                    MonitoringActivityStats.DifferenceBetweenX >= MonitoringConfig.Thresholds?.MouseAxisXThreshold;
        }

        public static FileInfo GetLastVideo(string[] files)
        {
            return files.Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault()!;
        }
        
        public static (string name,string path) GetLastVideoProperties(FileInfo lastVideo)
        {
            var path = lastVideo.FullName;
            var name = path.Replace(MonitoringConfig.PathToVideoRecords, string.Empty);
            return (name, path);
        }

    }
}
