namespace PulseWidget.Models;

public sealed record SensorSnapshot(
    DateTime Timestamp,
    string CpuName,
    string GpuName,
    double? CpuUsage,
    double? CpuTemperature,
    double? CpuPower,
    double? CpuClock,
    double? GpuUsage,
    double? GpuTemperature,
    double? GpuPower,
    double? GpuClock,
    double? GpuMemoryUsedMb,
    double? GpuMemoryTotalMb,
    double? MemoryUsage,
    double? MemoryUsedGb,
    double? MemoryAvailableGb,
    double? StorageTemperature,
    double? FanRpm,
    string Status);
