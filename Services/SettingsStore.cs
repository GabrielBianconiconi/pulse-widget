using System.IO;
using System.Text.Json;

namespace PulseWidget.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PulseWidget",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath));
            if (settings is null)
            {
                return new AppSettings();
            }

            settings.UpdateIntervalMilliseconds = settings.UpdateIntervalMilliseconds is 1000 or 2000 or 5000
                ? settings.UpdateIntervalMilliseconds
                : 1000;
            settings.WindowOpacity = double.IsFinite(settings.WindowOpacity)
                ? Math.Clamp(settings.WindowOpacity, 0.55, 1)
                : 0.96;
            settings.SchemaVersion = 1;
            settings.NormalWidth = double.IsFinite(settings.NormalWidth)
                ? Math.Clamp(settings.NormalWidth, 360, 1600)
                : 390;
            settings.NormalHeight = double.IsFinite(settings.NormalHeight)
                ? Math.Clamp(settings.NormalHeight, 590, 1400)
                : 650;
            settings.ChartHistoryMinutes = settings.ChartHistoryMinutes is 2 or 5 or 15 or 30
                ? settings.ChartHistoryMinutes
                : 2;
            settings.CpuTemperatureThreshold = Math.Clamp(settings.CpuTemperatureThreshold, 60, 105);
            settings.GpuTemperatureThreshold = Math.Clamp(settings.GpuTemperatureThreshold, 55, 105);
            settings.AlertDurationSeconds = Math.Clamp(settings.AlertDurationSeconds, 3, 120);
            settings.AlertCooldownMinutes = Math.Clamp(settings.AlertCooldownMinutes, 1, 120);
            settings.AlertHysteresisDegrees = Math.Clamp(settings.AlertHysteresisDegrees, 2, 15);
            settings.SelectedGpuIdentifier = string.IsNullOrWhiteSpace(settings.SelectedGpuIdentifier)
                ? "auto"
                : settings.SelectedGpuIdentifier;
            settings.Theme = settings.Theme is "Dark" or "Graphite" or "Light" ? settings.Theme : "Dark";
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            var temporaryPath = _settingsPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, _settingsPath, true);
        }
        catch
        {
            // A settings failure must not prevent the monitor from closing.
        }
    }
}
