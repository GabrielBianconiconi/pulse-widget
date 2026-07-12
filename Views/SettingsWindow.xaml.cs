using System.Windows;
using System.Windows.Controls;
using PulseWidget.Models;
using PulseWidget.Services;

namespace PulseWidget.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(AppSettings settings, bool startWithWindows, IReadOnlyList<GpuDescriptor> availableGpus)
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
        var gpuOptions = new List<GpuOption> { new("auto", "Automatico") };
        gpuOptions.AddRange(availableGpus.Select(gpu => new GpuOption(gpu.Identifier, gpu.Name)));
        GpuComboBox.ItemsSource = gpuOptions;
        GpuComboBox.SelectedItem = gpuOptions.FirstOrDefault(gpu => gpu.Identifier == settings.SelectedGpuIdentifier)
                                   ?? gpuOptions[0];
        ThemeComboBox.SelectedItem = ThemeComboBox.Items
            .OfType<ComboBoxItem>()
            .First(item => item.Tag.ToString() == settings.Theme);
        AlwaysOnTopCheckBox.IsChecked = settings.AlwaysOnTop;
        ClickThroughCheckBox.IsChecked = settings.ClickThrough;
        CompactModeCheckBox.IsChecked = settings.CompactMode;
        StartupCheckBox.IsChecked = startWithWindows;
        AlertsEnabledCheckBox.IsChecked = settings.AlertsEnabled;
        CpuThresholdTextBox.Text = settings.CpuTemperatureThreshold.ToString("0");
        GpuThresholdTextBox.Text = settings.GpuTemperatureThreshold.ToString("0");
        GpuFirstCheckBox.IsChecked = settings.GpuCardFirst;
        ShowVramCheckBox.IsChecked = settings.ShowVram;
        ShowStorageCheckBox.IsChecked = settings.ShowStorage;
        ShowFansCheckBox.IsChecked = settings.ShowFans;
        ShowNetworkCheckBox.IsChecked = settings.ShowNetwork;
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
        if (GpuComboBox.SelectedItem is GpuOption gpuOption)
        {
            Result.SelectedGpuIdentifier = gpuOption.Identifier;
        }

        if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem)
        {
            Result.Theme = themeItem.Tag.ToString()!;
        }
        if (HistoryComboBox.SelectedItem is ComboBoxItem historyItem)
        {
            Result.ChartHistoryMinutes = int.Parse(historyItem.Tag.ToString()!);
        }
        Result.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;
        Result.ClickThrough = ClickThroughCheckBox.IsChecked == true;
        Result.CompactMode = CompactModeCheckBox.IsChecked == true;
        Result.AlertsEnabled = AlertsEnabledCheckBox.IsChecked == true;
        Result.GpuCardFirst = GpuFirstCheckBox.IsChecked == true;
        Result.ShowVram = ShowVramCheckBox.IsChecked == true;
        Result.ShowStorage = ShowStorageCheckBox.IsChecked == true;
        Result.ShowFans = ShowFansCheckBox.IsChecked == true;
        Result.ShowNetwork = ShowNetworkCheckBox.IsChecked == true;
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

    private sealed record GpuOption(string Identifier, string Name);
}
