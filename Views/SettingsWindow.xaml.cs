using System.Windows;
using System.Windows.Controls;
using PulseWidget.Services;

namespace PulseWidget.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(AppSettings settings, bool startWithWindows)
    {
        InitializeComponent();
        Result = settings.Clone();

        IntervalComboBox.SelectedItem = IntervalComboBox.Items
            .OfType<ComboBoxItem>()
            .First(item => int.Parse(item.Tag.ToString()!) == settings.UpdateIntervalMilliseconds);
        OpacitySlider.Value = settings.WindowOpacity * 100;
        HistoryComboBox.SelectedItem = HistoryComboBox.Items
            .OfType<ComboBoxItem>()
            .First(item => int.Parse(item.Tag.ToString()!) == settings.ChartHistoryMinutes);
        AlwaysOnTopCheckBox.IsChecked = settings.AlwaysOnTop;
        ClickThroughCheckBox.IsChecked = settings.ClickThrough;
        CompactModeCheckBox.IsChecked = settings.CompactMode;
        StartupCheckBox.IsChecked = startWithWindows;
        AlertsEnabledCheckBox.IsChecked = settings.AlertsEnabled;
        CpuThresholdTextBox.Text = settings.CpuTemperatureThreshold.ToString("0");
        GpuThresholdTextBox.Text = settings.GpuTemperatureThreshold.ToString("0");
    }

    public AppSettings Result { get; }

    public bool StartWithWindows { get; private set; }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValueText is not null)
        {
            OpacityValueText.Text = $"{e.NewValue:0}%";
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (IntervalComboBox.SelectedItem is ComboBoxItem intervalItem)
        {
            Result.UpdateIntervalMilliseconds = int.Parse(intervalItem.Tag.ToString()!);
        }

        Result.WindowOpacity = OpacitySlider.Value / 100;
        if (HistoryComboBox.SelectedItem is ComboBoxItem historyItem)
        {
            Result.ChartHistoryMinutes = int.Parse(historyItem.Tag.ToString()!);
        }
        Result.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;
        Result.ClickThrough = ClickThroughCheckBox.IsChecked == true;
        Result.CompactMode = CompactModeCheckBox.IsChecked == true;
        Result.AlertsEnabled = AlertsEnabledCheckBox.IsChecked == true;
        if (double.TryParse(CpuThresholdTextBox.Text, out var cpuThreshold))
        {
            Result.CpuTemperatureThreshold = Math.Clamp(cpuThreshold, 60, 105);
        }

        if (double.TryParse(GpuThresholdTextBox.Text, out var gpuThreshold))
        {
            Result.GpuTemperatureThreshold = Math.Clamp(gpuThreshold, 55, 105);
        }

        StartWithWindows = StartupCheckBox.IsChecked == true;
        DialogResult = true;
    }
}
