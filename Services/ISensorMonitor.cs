using PulseWidget.Models;

namespace PulseWidget.Services;

public interface ISensorMonitor : IDisposable
{
    event EventHandler<SensorSnapshot>? SnapshotAvailable;

    void Start(int intervalMilliseconds);

    void SetInterval(int intervalMilliseconds);

    void SetSelectedGpu(string? identifier);

    string GetDiagnosticsReport();

    Task StopAsync();
}
