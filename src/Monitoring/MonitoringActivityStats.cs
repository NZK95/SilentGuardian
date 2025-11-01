using System.Drawing;

namespace SilentGuardian
{
    internal static class MonitoringActivityStats
    {
        public static Point? StartCordinates { get; set; }
        public static int PressedKeysCount { get; set; }
        public static int PressedMouseClicksCount { get; set; }
        public static int DifferenceBetweenX { get; set; }
        public static int DifferenceBetweenY { get; set; }

        public static void ResetActivity()
        {
            StartCordinates = null;
            PressedKeysCount = PressedMouseClicksCount = DifferenceBetweenX = DifferenceBetweenY = 0;
        }
    }
}
