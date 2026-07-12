namespace PulseWidget.Models;

public readonly record struct ChartSample(DateTime Timestamp, double? Primary, double? Secondary);
