using PulseWidget.Services;

namespace PulseWidget.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"PulseWidgetTests-{Guid.NewGuid():N}");

    [Fact]
    public void Save_and_load_round_trip_valid_settings()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new SettingsStore(path);
        var settings = new AppSettings
        {
            Theme = "Graphite",
            CompactMode = true,
            ChartHistoryMinutes = 15,
            WindowOpacity = 0.75
        };

        store.Save(settings);
        Assert.True(File.Exists(path), "Settings file was not created.");
        var loaded = store.Load();

        Assert.Equal("Graphite", loaded.Theme);
        Assert.True(loaded.CompactMode);
        Assert.Equal(15, loaded.ChartHistoryMinutes);
        Assert.Equal(0.75, loaded.WindowOpacity);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Load_normalizes_invalid_values()
    {
        var path = Path.Combine(_directory, "settings.json");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, """{"UpdateIntervalMilliseconds":42,"WindowOpacity":4,"Theme":"Unknown"}""");

        var settings = new SettingsStore(path).Load();

        Assert.Equal(1000, settings.UpdateIntervalMilliseconds);
        Assert.Equal(1, settings.WindowOpacity);
        Assert.Equal("Dark", settings.Theme);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
