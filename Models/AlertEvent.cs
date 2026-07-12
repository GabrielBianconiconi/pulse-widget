namespace PulseWidget.Models;

public sealed record AlertEvent(string Metric, double Value, double Threshold, DateTime Timestamp);
