namespace PulseWidget.Models;

public sealed record FrameMetrics(
    DateTime Timestamp,
    double? FramesPerSecond,
    double? FrameTimeMilliseconds,
    string ProcessName,
    string Status)
{
    public static FrameMetrics Unavailable(string status) => new(DateTime.Now, null, null, string.Empty, status);
}
