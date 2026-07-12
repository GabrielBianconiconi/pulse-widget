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
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch
        {
            // A settings failure must not prevent the monitor from closing.
        }
    }
}
