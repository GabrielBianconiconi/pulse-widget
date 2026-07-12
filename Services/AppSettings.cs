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

    public bool CompactMode { get; set; }

    public double NormalWidth { get; set; } = 390;

    public double NormalHeight { get; set; } = 650;

    public int ChartHistoryMinutes { get; set; } = 2;

    public double CpuTemperatureThreshold { get; set; } = 85;

    public double GpuTemperatureThreshold { get; set; } = 83;

    public bool AlertsEnabled { get; set; } = true;

    public int AlertDurationSeconds { get; set; } = 10;

    public int AlertCooldownMinutes { get; set; } = 10;

    public double AlertHysteresisDegrees { get; set; } = 5;

    public string SelectedGpuIdentifier { get; set; } = "auto";

    public string Theme { get; set; } = "Dark";

    public bool GpuCardFirst { get; set; }

    public bool ShowVram { get; set; } = true;

    public bool ShowStorage { get; set; } = true;

    public bool ShowFans { get; set; } = true;

    public bool ShowNetwork { get; set; } = true;

    public bool RtssEnabled { get; set; }

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
            WindowOpacity = WindowOpacity,
            CompactMode = CompactMode,
            NormalWidth = NormalWidth,
            NormalHeight = NormalHeight,
            ChartHistoryMinutes = ChartHistoryMinutes,
            CpuTemperatureThreshold = CpuTemperatureThreshold,
            GpuTemperatureThreshold = GpuTemperatureThreshold,
            AlertsEnabled = AlertsEnabled,
            AlertDurationSeconds = AlertDurationSeconds,
            AlertCooldownMinutes = AlertCooldownMinutes,
            AlertHysteresisDegrees = AlertHysteresisDegrees,
            SelectedGpuIdentifier = SelectedGpuIdentifier,
            Theme = Theme,
            GpuCardFirst = GpuCardFirst,
            ShowVram = ShowVram,
            ShowStorage = ShowStorage,
            ShowFans = ShowFans,
            ShowNetwork = ShowNetwork,
            RtssEnabled = RtssEnabled
        };
    }
}
