namespace PulseWidget.Services;

public sealed class AppSettings
{
    public double Left { get; set; } = double.NaN;

    public double Top { get; set; } = double.NaN;

    public bool AlwaysOnTop { get; set; } = true;

    public bool ClickThrough { get; set; }

    public int UpdateIntervalMilliseconds { get; set; } = 1000;
}
