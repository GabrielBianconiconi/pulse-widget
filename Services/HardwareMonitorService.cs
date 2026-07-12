using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using PulseWidget.Models;

namespace PulseWidget.Services;

public sealed class HardwareMonitorService : IDisposable
{
    private static readonly HardwareType[] GpuTypes =
    [
        HardwareType.GpuNvidia,
        HardwareType.GpuAmd,
        HardwareType.GpuIntel
    ];

    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = false,
        IsMotherboardEnabled = true,
        IsControllerEnabled = true,
        IsStorageEnabled = true,
        IsNetworkEnabled = true
    };

    private CancellationTokenSource? _cancellation;
    private Task? _monitorTask;
    private int _intervalMilliseconds = 1000;
    private int _sampleCount;
    private bool _computerOpened;

    public event EventHandler<SensorSnapshot>? SnapshotAvailable;

    public void Start(int intervalMilliseconds)
    {
        if (_monitorTask is not null)
        {
            return;
        }

        SetInterval(intervalMilliseconds);
        _cancellation = new CancellationTokenSource();
        _monitorTask = Task.Run(() => MonitorLoopAsync(_cancellation.Token));
    }

    public void SetInterval(int intervalMilliseconds)
    {
        Interlocked.Exchange(ref _intervalMilliseconds, Math.Clamp(intervalMilliseconds, 500, 10_000));
    }

    public async Task StopAsync()
    {
        if (_cancellation is null || _monitorTask is null)
        {
            return;
        }

        await _cancellation.CancelAsync();
        try
        {
            await _monitorTask;
        }
        catch (OperationCanceledException)
        {
        }

        _monitorTask = null;
        _cancellation.Dispose();
        _cancellation = null;
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _computer.Open();
            _computerOpened = true;
        }
        catch (Exception exception)
        {
            PublishUnavailable($"Sensores indisponiveis: {exception.Message}");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var startedAt = Environment.TickCount64;
            SensorSnapshot snapshot;
            try
            {
                snapshot = ReadSnapshot();
            }
            catch (Exception exception)
            {
                snapshot = CreateUnavailableSnapshot($"Falha ao ler sensores: {exception.Message}");
            }

            SnapshotAvailable?.Invoke(this, snapshot);

            var elapsed = (int)Math.Min(int.MaxValue, Environment.TickCount64 - startedAt);
            var delay = Math.Max(50, Volatile.Read(ref _intervalMilliseconds) - elapsed);
            await Task.Delay(delay, cancellationToken);
        }
    }

    private SensorSnapshot ReadSnapshot()
    {
        var hardware = FlattenHardware(_computer.Hardware).ToArray();
        var updateSlowSensors = _sampleCount++ % 5 == 0;
        foreach (var item in hardware)
        {
            if (updateSlowSensors || IsFastHardware(item.HardwareType))
            {
                item.Update();
            }
        }

        var cpuHardware = hardware.Where(item => item.HardwareType == HardwareType.Cpu).ToArray();
        var gpuHardware = hardware.Where(item => GpuTypes.Contains(item.HardwareType)).ToArray();
        var storageHardware = hardware.Where(item => item.HardwareType == HardwareType.Storage).ToArray();
        var networkHardware = hardware.Where(item => item.HardwareType == HardwareType.Network).ToArray();

        var selectedGpu = gpuHardware
            .OrderByDescending(item => PreferredValue([item], SensorType.Load, "GPU Core", "D3D 3D") ?? -1)
            .FirstOrDefault();
        var selectedGpuArray = selectedGpu is null ? [] : new[] { selectedGpu };

        var cpuUsage = PreferredValue(cpuHardware, SensorType.Load, "CPU Total");
        var cpuTemperature = PreferredValue(cpuHardware, SensorType.Temperature,
            "CPU Package", "Core (Tctl/Tdie)", "CPU (Tctl/Tdie)", "Core Average")
            ?? MaximumValue(cpuHardware, SensorType.Temperature);
        var cpuClock = AverageValue(cpuHardware, SensorType.Clock,
            sensor => sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase));

        var gpuMemoryUsed = PreferredSensor(selectedGpuArray, SensorType.SmallData, "GPU Memory Used")?.Value;
        var gpuMemoryTotal = PreferredSensor(selectedGpuArray, SensorType.SmallData, "GPU Memory Total")?.Value;

        var memory = ReadPhysicalMemory();
        var selectedStorage = storageHardware
            .OrderByDescending(item => MaximumValue([item], SensorType.Temperature) ?? -1)
            .FirstOrDefault();
        var selectedStorageArray = selectedStorage is null ? [] : new[] { selectedStorage };
        var selectedFan = MaximumSensor(hardware, SensorType.Fan);
        var selectedNetwork = networkHardware
            .OrderByDescending(item =>
                (NamedValue([item], SensorType.Throughput, "Download Speed", "Download") ?? 0) +
                (NamedValue([item], SensorType.Throughput, "Upload Speed", "Upload") ?? 0))
            .FirstOrDefault();
        var selectedNetworkArray = selectedNetwork is null ? [] : new[] { selectedNetwork };

        var hasCoreMetrics = cpuUsage.HasValue || selectedGpu is not null;
        var status = !cpuTemperature.HasValue
            ? "Temperatura CPU indisponivel: autorize o UAC e verifique o driver"
            : hasCoreMetrics
                ? "Monitorando"
                : "Execute como administrador para acessar mais sensores";
        return new SensorSnapshot(
            DateTime.Now,
            cpuHardware.FirstOrDefault()?.Name ?? "CPU",
            selectedGpu?.Name ?? "GPU nao detectada",
            cpuUsage,
            cpuTemperature,
            PreferredValue(cpuHardware, SensorType.Power, "CPU Package", "Package"),
            cpuClock,
            PreferredValue(selectedGpuArray, SensorType.Load, "GPU Core", "D3D 3D"),
            PreferredValue(selectedGpuArray, SensorType.Temperature, "GPU Core", "GPU Hot Spot"),
            PreferredValue(selectedGpuArray, SensorType.Power, "GPU Package", "GPU Power"),
            PreferredValue(selectedGpuArray, SensorType.Clock, "GPU Core"),
            gpuMemoryUsed,
            gpuMemoryTotal,
            memory?.UsagePercentage,
            memory?.UsedGigabytes,
            memory?.AvailableGigabytes,
            selectedStorage?.Name ?? "Armazenamento",
            MaximumValue(selectedStorageArray, SensorType.Temperature),
            NamedValue(selectedStorageArray, SensorType.Load, "Total Activity", "Activity"),
            selectedFan?.Hardware.Name ?? "Ventoinha",
            selectedFan?.Sensor.Value,
            selectedNetwork?.Name ?? "Rede",
            NamedValue(selectedNetworkArray, SensorType.Throughput, "Download Speed", "Download"),
            NamedValue(selectedNetworkArray, SensorType.Throughput, "Upload Speed", "Upload"),
            status);
    }

    private void PublishUnavailable(string status)
    {
        SnapshotAvailable?.Invoke(this, CreateUnavailableSnapshot(status));
    }

    private static SensorSnapshot CreateUnavailableSnapshot(string status)
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
            status);
    }

    private static IEnumerable<IHardware> FlattenHardware(IEnumerable<IHardware> hardware)
    {
        foreach (var item in hardware)
        {
            yield return item;
            foreach (var child in FlattenHardware(item.SubHardware))
            {
                yield return child;
            }
        }
    }

    private static bool IsFastHardware(HardwareType hardwareType)
    {
        return hardwareType == HardwareType.Cpu
               || hardwareType == HardwareType.Memory
               || hardwareType == HardwareType.Network
               || GpuTypes.Contains(hardwareType);
    }

    private static PhysicalMemorySnapshot? ReadPhysicalMemory()
    {
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref status) || status.TotalPhysical == 0)
        {
            return null;
        }

        const double bytesPerGigabyte = 1024d * 1024d * 1024d;
        var usedPhysical = status.TotalPhysical - status.AvailablePhysical;
        return new PhysicalMemorySnapshot(
            usedPhysical * 100d / status.TotalPhysical,
            usedPhysical / bytesPerGigabyte,
            status.AvailablePhysical / bytesPerGigabyte);
    }

    private readonly record struct PhysicalMemorySnapshot(
        double UsagePercentage,
        double UsedGigabytes,
        double AvailableGigabytes);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    private static ISensor? PreferredSensor(
        IEnumerable<IHardware> hardware,
        SensorType sensorType,
        params string[] preferredNames)
    {
        var sensors = hardware.SelectMany(item => item.Sensors)
            .Where(sensor => sensor.SensorType == sensorType && sensor.Value.HasValue)
            .ToArray();

        foreach (var preferredName in preferredNames)
        {
            var exact = sensors.FirstOrDefault(sensor =>
                sensor.Name.Equals(preferredName, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }
        }

        foreach (var preferredName in preferredNames)
        {
            var partial = sensors.FirstOrDefault(sensor =>
                sensor.Name.Contains(preferredName, StringComparison.OrdinalIgnoreCase));
            if (partial is not null)
            {
                return partial;
            }
        }

        return sensors.FirstOrDefault();
    }

    private static double? PreferredValue(
        IEnumerable<IHardware> hardware,
        SensorType sensorType,
        params string[] preferredNames)
    {
        return PreferredSensor(hardware, sensorType, preferredNames)?.Value;
    }

    private static double? NamedValue(
        IEnumerable<IHardware> hardware,
        SensorType sensorType,
        params string[] preferredNames)
    {
        var sensors = hardware.SelectMany(item => item.Sensors)
            .Where(sensor => sensor.SensorType == sensorType && sensor.Value.HasValue)
            .ToArray();
        foreach (var name in preferredNames)
        {
            var sensor = sensors.FirstOrDefault(item =>
                item.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                item.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (sensor is not null)
            {
                return sensor.Value;
            }
        }

        return null;
    }

    private static (IHardware Hardware, ISensor Sensor)? MaximumSensor(
        IEnumerable<IHardware> hardware,
        SensorType sensorType)
    {
        var maximum = hardware
            .SelectMany(item => item.Sensors
                .Where(sensor => sensor.SensorType == sensorType && sensor.Value.HasValue)
                .Select(sensor => (Hardware: item, Sensor: sensor)))
            .OrderByDescending(item => item.Sensor.Value)
            .FirstOrDefault();
        return maximum.Sensor is null ? null : maximum;
    }

    private static double? MaximumValue(IEnumerable<IHardware> hardware, SensorType sensorType)
    {
        var values = hardware.SelectMany(item => item.Sensors)
            .Where(sensor => sensor.SensorType == sensorType && sensor.Value.HasValue)
            .Select(sensor => (double)sensor.Value!.Value)
            .ToArray();
        return values.Length == 0 ? null : values.Max();
    }

    private static double? AverageValue(
        IEnumerable<IHardware> hardware,
        SensorType sensorType,
        Func<ISensor, bool> predicate)
    {
        var values = hardware.SelectMany(item => item.Sensors)
            .Where(sensor => sensor.SensorType == sensorType && sensor.Value.HasValue && predicate(sensor))
            .Select(sensor => (double)sensor.Value!.Value)
            .ToArray();
        return values.Length == 0 ? null : values.Average();
    }

    public void Dispose()
    {
        if (_computerOpened)
        {
            _computer.Close();
            _computerOpened = false;
        }
    }
}
