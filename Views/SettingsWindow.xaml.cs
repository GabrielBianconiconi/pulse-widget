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
        AlwaysOnTopCheckBox.IsChecked = settings.AlwaysOnTop;
        ClickThroughCheckBox.IsChecked = settings.ClickThrough;
        CompactModeCheckBox.IsChecked = settings.CompactMode;
        StartupCheckBox.IsChecked = startWithWindows;
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
        Result.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;
        Result.ClickThrough = ClickThroughCheckBox.IsChecked == true;
        Result.CompactMode = CompactModeCheckBox.IsChecked == true;
        StartWithWindows = StartupCheckBox.IsChecked == true;
        DialogResult = true;
    }
}
