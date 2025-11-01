using NetEx.Hooks;

namespace SilentGuardian
{
    public static class AppHelper
    {
        public const int MONITOR_DELAY_MS = 100;

        public const int DEFAULT_SCREENSHOT_TIME_INTERVAL_MS = 2000;
        public const int DEFAULT_PRESSED_KEYS_THRESHOLD = 2;
        public const int DEFAULT_PRESSED_MOUSE_CLICKS_THRESHOLD = 2;
        public const int DEFAULT_MOUSE_AXIS_X_THRESHOLD = 200;
        public const int DEFAULT_MOUSE_AXIS_Y_THRESHOLD = 200;

        public const string FFMPEG_ARGUMENTS = $@"-f gdigrab -framerate 30 -i desktop -c:v libx264 -preset ultrafast -crf 25 -pix_fmt yuv420p -r 30 ";

        public static IReadOnlyList<Keys> AllKeys { get; } = Enum.GetValues(typeof(Keys)).Cast<Keys>().ToList();
    }
}
