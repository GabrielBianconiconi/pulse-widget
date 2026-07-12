using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PulseWidget.Models;
using PulseWidget.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace PulseWidget;

public partial class MainWindow : Window
{
    private const int ExtendedStyleIndex = -20;
    private const int TransparentExtendedStyle = 0x20;
    private bool _clickThrough;
    private bool _allowClose;
    private bool _compactMode;

    public MainWindow(AppSettings settings)
    {
        InitializeComponent();

        Topmost = settings.AlwaysOnTop;
        Opacity = settings.WindowOpacity;
        PinButton.Foreground = Topmost ? FindResource("CpuAccent") as Brush : FindResource("MutedText") as Brush;
        SetIntervalLabel(settings.UpdateIntervalMilliseconds);
        ApplyChartSettings(settings);
        ApplyCompactMode(settings);

        if (double.IsNaN(settings.Left) || double.IsNaN(settings.Top))
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        Loaded += (_, _) =>
        {
            RestorePosition(settings);
            SetClickThrough(settings.ClickThrough);
        };
        Closing += Window_Closing;
    }

    public event EventHandler? HideRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler<bool>? CompactModeChanged;

    public void UpdateSnapshot(SensorSnapshot snapshot)
    {
        CpuUsageText.Text = FormatNumber(snapshot.CpuUsage, "0");
        CpuTemperatureText.Text = FormatValue(snapshot.CpuTemperature, "0", " C");
        CpuDetailText.Text = $"{FormatClock(snapshot.CpuClock)}  /  {FormatValue(snapshot.CpuPower, "0", " W")}";

        GpuUsageText.Text = FormatNumber(snapshot.GpuUsage, "0");
        GpuTemperatureText.Text = FormatValue(snapshot.GpuTemperature, "0", " C");
        GpuDetailText.Text = $"{FormatClock(snapshot.GpuClock)}  /  {FormatValue(snapshot.GpuPower, "0", " W")}";
        CpuCard.ToolTip = snapshot.CpuName;
        GpuCard.ToolTip = snapshot.GpuName;

        MemoryUsageText.Text = FormatValue(snapshot.MemoryUsage, "0", "%");
        MemoryProgress.Value = snapshot.MemoryUsage ?? 0;
        var totalMemory = snapshot.MemoryUsedGb + snapshot.MemoryAvailableGb;
        MemoryDetailText.Text = snapshot.MemoryUsedGb.HasValue && totalMemory.HasValue
            ? $"{snapshot.MemoryUsedGb:0.0} / {totalMemory:0.0} GB"
            : "-- / -- GB";
        VramMetricText.Text = FormatVram(snapshot);
        StorageMetricText.Text = snapshot.StorageTemperature.HasValue
            ? $"SSD {snapshot.StorageTemperature:0} C / {FormatValue(snapshot.StorageActivity, "0", "%")}" : "SSD --";
        StorageMetricText.ToolTip = snapshot.StorageName;
        FanMetricText.Text = snapshot.FanRpm.HasValue ? $"FAN {snapshot.FanRpm:0} RPM" : "FAN --";
        FanMetricText.ToolTip = snapshot.FanName;
        NetworkMetricText.Text = $"NET D {FormatThroughput(snapshot.NetworkDownloadBytes)} U {FormatThroughput(snapshot.NetworkUploadBytes)}";
        NetworkMetricText.ToolTip = snapshot.NetworkName;

        UsageChart.AddPoints(snapshot.Timestamp, snapshot.CpuUsage, snapshot.GpuUsage);
        TemperatureChart.AddPoints(snapshot.Timestamp, snapshot.CpuTemperature, snapshot.GpuTemperature);
        CpuUsageLegendValue.Text = FormatValue(snapshot.CpuUsage, "0", "%");
        CpuTemperatureLegendValue.Text = FormatValue(snapshot.CpuTemperature, "0", " C");
        GpuUsageLegendValue.Text = FormatValue(snapshot.GpuUsage, "0", "%");
        GpuTemperatureLegendValue.Text = FormatValue(snapshot.GpuTemperature, "0", " C");
        UsageStatsText.Text = FormatStatistics(UsageChart.GetStatistics(true), UsageChart.GetStatistics(false));
        TemperatureStatsText.Text = FormatStatistics(TemperatureChart.GetStatistics(true), TemperatureChart.GetStatistics(false));

        var extra = BuildExtraStatus(snapshot);
        StatusText.Text = string.IsNullOrEmpty(extra) ? snapshot.Status : $"{snapshot.Status}  |  {extra}";
        StatusIndicator.Fill = snapshot.CpuUsage.HasValue
            ? FindResource("CpuAccent") as Brush
            : Brushes.OrangeRed;
    }

    public void SetIntervalLabel(int milliseconds)
    {
        IntervalText.Text = milliseconds >= 1000
            ? $"{milliseconds / 1000.0:0.#} s"
            : $"{milliseconds} ms";
    }

    public void SetClickThrough(bool enabled)
    {
        _clickThrough = enabled;
        if (!IsLoaded)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        var extendedStyle = GetWindowLong(handle, ExtendedStyleIndex);
        var updatedStyle = enabled
            ? extendedStyle | TransparentExtendedStyle
            : extendedStyle & ~TransparentExtendedStyle;
        SetWindowLong(handle, ExtendedStyleIndex, updatedStyle);
    }

    public void ApplySettings(AppSettings settings)
    {
        Topmost = settings.AlwaysOnTop;
        Opacity = settings.WindowOpacity;
        PinButton.Foreground = Topmost ? FindResource("CpuAccent") as Brush : FindResource("MutedText") as Brush;
        SetIntervalLabel(settings.UpdateIntervalMilliseconds);
        SetClickThrough(settings.ClickThrough);
        ApplyCompactMode(settings);
        ApplyChartSettings(settings);
    }

    public void SetTemperatureWarning(bool cpuWarning, bool gpuWarning)
    {
        ApplyWarningBorder(CpuCard, cpuWarning);
        ApplyWarningBorder(GpuCard, gpuWarning);
    }

    private static void ApplyWarningBorder(System.Windows.Controls.Border card, bool warning)
    {
        card.BorderBrush = warning ? Brushes.OrangeRed : Brushes.Transparent;
        card.BorderThickness = warning ? new Thickness(1.5) : new Thickness(0);
    }

    private void ApplyChartSettings(AppSettings settings)
    {
        UsageChart.WindowMinutes = settings.ChartHistoryMinutes;
        TemperatureChart.WindowMinutes = settings.ChartHistoryMinutes;
        TemperatureChart.PrimaryThreshold = settings.CpuTemperatureThreshold;
        TemperatureChart.SecondaryThreshold = settings.GpuTemperatureThreshold;
        UsageChartTitle.Text = $"USO / ULTIMOS {settings.ChartHistoryMinutes} MIN";
        TemperatureChartTitle.Text = $"TEMPERATURA / ULTIMOS {settings.ChartHistoryMinutes} MIN";
    }

    public void ApplyCompactMode(AppSettings settings)
    {
        if (_compactMode == settings.CompactMode && IsLoaded)
        {
            return;
        }

        if (settings.CompactMode)
        {
            if (!_compactMode && WindowState == WindowState.Normal)
            {
                settings.NormalWidth = Width;
                settings.NormalHeight = Height;
            }

            UsageChartRow.Height = new GridLength(0);
            TemperatureChartRow.Height = new GridLength(0);
            BeforeUsageSpacingRow.Height = new GridLength(0);
            BetweenChartsSpacingRow.Height = new GridLength(0);
            MinHeight = 300;
            Height = 330;
            CompactButton.Foreground = FindResource("CpuAccent") as Brush;
        }
        else
        {
            UsageChartRow.Height = new GridLength(1, GridUnitType.Star);
            TemperatureChartRow.Height = new GridLength(1, GridUnitType.Star);
            BeforeUsageSpacingRow.Height = new GridLength(10);
            BetweenChartsSpacingRow.Height = new GridLength(10);
            MinHeight = 590;
            Width = settings.NormalWidth;
            Height = settings.NormalHeight;
            CompactButton.Foreground = FindResource("MutedText") as Brush;
        }

        _compactMode = settings.CompactMode;
    }

    public AppSettings CaptureSettings(AppSettings settings)
    {
        settings.Left = Left;
        settings.Top = Top;
        settings.AlwaysOnTop = Topmost;
        settings.ClickThrough = _clickThrough;
        settings.WindowOpacity = Opacity;
        settings.CompactMode = _compactMode;
        if (!_compactMode && WindowState == WindowState.Normal)
        {
            settings.NormalWidth = Width;
            settings.NormalHeight = Height;
        }
        return settings;
    }

    public void AllowClose()
    {
        _allowClose = true;
    }

    private void RestorePosition(AppSettings settings)
    {
        if (double.IsNaN(settings.Left) || double.IsNaN(settings.Top))
        {
            return;
        }

        var horizontalVisible = settings.Left + Width > SystemParameters.VirtualScreenLeft
                                && settings.Left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        var verticalVisible = settings.Top + Height > SystemParameters.VirtualScreenTop
                              && settings.Top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;

        if (horizontalVisible && verticalVisible)
        {
            Left = settings.Left;
            Top = settings.Top;
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        PinButton.Foreground = Topmost ? FindResource("CpuAccent") as Brush : FindResource("MutedText") as Brush;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CompactButton_Click(object sender, RoutedEventArgs e)
    {
        CompactModeChanged?.Invoke(this, !_compactMode);
    }

    private void DragArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else
        {
            DragMove();
        }

        e.Handled = true;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        HideRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        HideRequested?.Invoke(this, EventArgs.Empty);
    }

    private static string BuildExtraStatus(SensorSnapshot snapshot)
    {
        var parts = new List<string>();
        parts.Add(snapshot.GpuName);

        return string.Join("  |  ", parts);
    }

    private static string FormatClock(double? megahertz)
    {
        return megahertz.HasValue ? $"{megahertz.Value / 1000:0.00} GHz" : "-- GHz";
    }

    private static string FormatValue(double? value, string format, string suffix)
    {
        return value.HasValue ? $"{value.Value.ToString(format)}{suffix}" : $"--{suffix}";
    }

    private static string FormatNumber(double? value, string format)
    {
        return value.HasValue ? value.Value.ToString(format) : "--";
    }

    private static string FormatStatistics(SeriesStatistics primary, SeriesStatistics secondary)
    {
        return $"CPU min/med/max {FormatStat(primary.Minimum)}/{FormatStat(primary.Average)}/{FormatStat(primary.Maximum)}   " +
               $"GPU {FormatStat(secondary.Minimum)}/{FormatStat(secondary.Average)}/{FormatStat(secondary.Maximum)}";
    }

    private static string FormatStat(double? value) => value.HasValue ? value.Value.ToString("0") : "--";

    private static string FormatVram(SensorSnapshot snapshot)
    {
        return snapshot.GpuMemoryUsedMb.HasValue && snapshot.GpuMemoryTotalMb.HasValue
            ? $"VRAM {snapshot.GpuMemoryUsedMb / 1024:0.0}/{snapshot.GpuMemoryTotalMb / 1024:0.0} GB"
            : "VRAM --";
    }

    private static string FormatThroughput(double? bytesPerSecond)
    {
        if (!bytesPerSecond.HasValue)
        {
            return "--";
        }

        return bytesPerSecond.Value >= 1024 * 1024
            ? $"{bytesPerSecond.Value / (1024 * 1024):0.0} MB/s"
            : $"{bytesPerSecond.Value / 1024:0} KB/s";
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(nint windowHandle, int index, int newLong);
}
