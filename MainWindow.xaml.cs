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

    public MainWindow(AppSettings settings)
    {
        InitializeComponent();

        Topmost = settings.AlwaysOnTop;
        Opacity = settings.WindowOpacity;
        PinButton.Foreground = Topmost ? FindResource("CpuAccent") as Brush : FindResource("MutedText") as Brush;
        SetIntervalLabel(settings.UpdateIntervalMilliseconds);

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

    public void UpdateSnapshot(SensorSnapshot snapshot)
    {
        CpuUsageText.Text = FormatNumber(snapshot.CpuUsage, "0");
        CpuTemperatureText.Text = FormatValue(snapshot.CpuTemperature, "0", " C");
        CpuDetailText.Text = $"{FormatClock(snapshot.CpuClock)}  /  {FormatValue(snapshot.CpuPower, "0", " W")}";

        GpuUsageText.Text = FormatNumber(snapshot.GpuUsage, "0");
        GpuTemperatureText.Text = FormatValue(snapshot.GpuTemperature, "0", " C");
        GpuDetailText.Text = $"{FormatClock(snapshot.GpuClock)}  /  {FormatValue(snapshot.GpuPower, "0", " W")}";

        MemoryUsageText.Text = FormatValue(snapshot.MemoryUsage, "0", "%");
        MemoryProgress.Value = snapshot.MemoryUsage ?? 0;
        var totalMemory = snapshot.MemoryUsedGb + snapshot.MemoryAvailableGb;
        MemoryDetailText.Text = snapshot.MemoryUsedGb.HasValue && totalMemory.HasValue
            ? $"{snapshot.MemoryUsedGb:0.0} / {totalMemory:0.0} GB"
            : "-- / -- GB";

        UsageChart.AddPoints(snapshot.CpuUsage, snapshot.GpuUsage);
        TemperatureChart.AddPoints(snapshot.CpuTemperature, snapshot.GpuTemperature);
        CpuUsageLegendValue.Text = FormatValue(snapshot.CpuUsage, "0", "%");
        CpuTemperatureLegendValue.Text = FormatValue(snapshot.CpuTemperature, "0", " C");
        GpuUsageLegendValue.Text = FormatValue(snapshot.GpuUsage, "0", "%");
        GpuTemperatureLegendValue.Text = FormatValue(snapshot.GpuTemperature, "0", " C");

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
    }

    public AppSettings CaptureSettings(AppSettings settings)
    {
        settings.Left = Left;
        settings.Top = Top;
        settings.AlwaysOnTop = Topmost;
        settings.ClickThrough = _clickThrough;
        settings.WindowOpacity = Opacity;
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
        if (snapshot.StorageTemperature.HasValue)
        {
            parts.Add($"SSD {snapshot.StorageTemperature:0} C");
        }

        if (snapshot.FanRpm.HasValue)
        {
            parts.Add($"FAN {snapshot.FanRpm:0} RPM");
        }

        if (snapshot.GpuMemoryUsedMb.HasValue && snapshot.GpuMemoryTotalMb.HasValue)
        {
            parts.Add($"VRAM {snapshot.GpuMemoryUsedMb / 1024:0.0}/{snapshot.GpuMemoryTotalMb / 1024:0.0} GB");
        }

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

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(nint windowHandle, int index, int newLong);
}
