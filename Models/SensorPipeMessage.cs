namespace PulseWidget.Models;

public sealed record SensorPipeMessage
{
    public string Type { get; init; } = string.Empty;

    public string? Token { get; init; }

    public SensorSnapshot? Snapshot { get; init; }

    public int? IntervalMilliseconds { get; init; }

    public string? GpuIdentifier { get; init; }

    public string? Diagnostics { get; init; }
}
