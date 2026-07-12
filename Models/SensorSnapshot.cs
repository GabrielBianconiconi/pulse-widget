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
    string StorageName,
    double? StorageTemperature,
    double? StorageActivity,
    string FanName,
    double? FanRpm,
    string NetworkName,
    double? NetworkDownloadBytes,
    double? NetworkUploadBytes,
    IReadOnlyList<GpuDescriptor> AvailableGpus,
    string SelectedGpuIdentifier,
    string Status)
{
    public static SensorSnapshot Unavailable(string status)
    {
        return new SensorSnapshot(
            DateTime.Now,
            "CPU",
            "GPU",
            null, null, null, null,
            null, null, null, null, null, null,
            null, null, null,
            "Armazenamento", null, null,
            "Ventoinha", null,
            "Rede", null, null,
            [], "auto",
            status);
    }
}
