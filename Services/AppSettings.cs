namespace PulseWidget.Services;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;

    public double Left { get; set; } = double.NaN;

    public double Top { get; set; } = double.NaN;

    public bool AlwaysOnTop { get; set; } = true;

    public bool ClickThrough { get; set; }

    public int UpdateIntervalMilliseconds { get; set; } = 1000;

    public double WindowOpacity { get; set; } = 0.96;

    public AppSettings Clone()
    {
        return new AppSettings
        {
            SchemaVersion = SchemaVersion,
            Left = Left,
            Top = Top,
            AlwaysOnTop = AlwaysOnTop,
            ClickThrough = ClickThrough,
            UpdateIntervalMilliseconds = UpdateIntervalMilliseconds,
            WindowOpacity = WindowOpacity
        };
    }
}
