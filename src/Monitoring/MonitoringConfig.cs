using NetEx.Hooks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SilentGuardian
{
    public class Thresholds
    {
        public int ScreenshotTimeIntervalMs { get; }
        public int PressedKeysThreshold { get; }
        public int PressedMouseClicksThreshold { get; }
        public int MouseAxisXThreshold { get; }
        public int MouseAxisYThreshold { get; }

        [JsonConstructor]
        public Thresholds(
            int screenshotTimeIntervalMs,
            int pressedKeysThreshold,
            int pressedMouseClicksThreshold,
            int mouseAxisXThreshold,
            int mouseAxisYThreshold)
        {
            ScreenshotTimeIntervalMs = screenshotTimeIntervalMs;
            PressedKeysThreshold = pressedKeysThreshold;
            PressedMouseClicksThreshold = pressedMouseClicksThreshold;
            MouseAxisXThreshold = mouseAxisXThreshold;
            MouseAxisYThreshold = mouseAxisYThreshold;
        }
    }

    public class LogicOptions
    {
        public bool TakeScreenshots { get; }
        public bool RecordScreen { get; }
        public bool AutoLock { get; }

        [JsonConstructor]
        public LogicOptions(bool takeScreenshots, bool recordScreen, bool autoLock)
        {
            TakeScreenshots = takeScreenshots;
            RecordScreen = recordScreen;
            AutoLock = autoLock;
        }
    }

    public static class MonitoringConfig
    {
        public static Thresholds? Thresholds { get; }
        public static LogicOptions? LogicOptions { get; }
        public static string BasePath { get; }
        public static string PathToConfig { get; }
        public static string PathToffmpeg { get; }
        public static string PathToVideoRecords { get; }
        static MonitoringConfig()
        {
            BasePath = @$"C:\Users\User\Downloads";
            PathToConfig = Path.Combine(BasePath, "config.json");
            PathToVideoRecords = @$"C:\Users\User\Downloads";
            PathToffmpeg = @"C:\Users\User\Desktop\ffmpeg-2025-10-21-git-535d4047d3-essentials_build\bin\ffmpeg.exe";

            Thresholds = GetThresholds();
            LogicOptions = GetLogicOptions();
        }

        private static Thresholds GetThresholds()
        {
            var defaultThresholds = new Thresholds(
                AppHelper.DEFAULT_SCREENSHOT_TIME_INTERVAL_MS,
                AppHelper.DEFAULT_PRESSED_KEYS_THRESHOLD,
                AppHelper.DEFAULT_PRESSED_MOUSE_CLICKS_THRESHOLD,
                AppHelper.DEFAULT_MOUSE_AXIS_X_THRESHOLD,
                AppHelper.DEFAULT_MOUSE_AXIS_Y_THRESHOLD
                );

            if (!File.Exists(PathToConfig))
                return defaultThresholds;

            using (FileStream fs = new FileStream(PathToConfig, FileMode.Open, FileAccess.Read))
            {
                var deserializedThresholds = JsonSerializer.Deserialize<Thresholds>(fs) ?? defaultThresholds;
                return deserializedThresholds;
            }
        }

        private static LogicOptions GetLogicOptions()
        {
            var defaultLogicOptions = new LogicOptions(recordScreen: true, takeScreenshots: false, autoLock: false);

            if (!File.Exists(PathToConfig))
                return defaultLogicOptions;

            using var fs = new FileStream(PathToConfig, FileMode.Open, FileAccess.Read);
            var deserializedLogicOptions = JsonSerializer.Deserialize<LogicOptions>(fs) ?? defaultLogicOptions;

            return deserializedLogicOptions;
        }
    }
}
