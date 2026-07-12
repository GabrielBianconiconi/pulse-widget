using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using PulseWidget.Models;

namespace PulseWidget.Services;

public sealed class RtssIntegration : IDisposable
{
    private const string MappingName = "RTSSSharedMemoryV2";
    private const uint Signature = 0x53535452;
    private CancellationTokenSource? _cancellation;
    private Task? _pollTask;
    private volatile bool _enabled;
    private string _status = "RTSS desativado";

    public event EventHandler<FrameMetrics>? MetricsAvailable;

    public string Status => _status;

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (enabled && _pollTask is null)
        {
            _cancellation = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollAsync(_cancellation.Token));
        }
        else if (!enabled)
        {
            _status = "RTSS desativado";
            MetricsAvailable?.Invoke(this, FrameMetrics.Unavailable(_status));
        }
    }

    public async Task StopAsync()
    {
        if (_cancellation is null || _pollTask is null)
        {
            return;
        }

        await _cancellation.CancelAsync();
        try
        {
            await _pollTask;
        }
        catch (OperationCanceledException)
        {
        }

        _cancellation.Dispose();
        _cancellation = null;
        _pollTask = null;
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        var unavailablePublished = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_enabled)
            {
                await Task.Delay(500, cancellationToken);
                continue;
            }

            var metrics = TryReadMetrics();
            if (metrics.FramesPerSecond.HasValue)
            {
                unavailablePublished = false;
                _status = $"RTSS conectado: {metrics.ProcessName}";
                MetricsAvailable?.Invoke(this, metrics);
            }
            else if (!unavailablePublished)
            {
                unavailablePublished = true;
                _status = metrics.Status;
                MetricsAvailable?.Invoke(this, metrics);
            }

            await Task.Delay(250, cancellationToken);
        }
    }

    private static FrameMetrics TryReadMetrics()
    {
        try
        {
            using var mapping = MemoryMappedFile.OpenExisting(MappingName, MemoryMappedFileRights.Read);
            using var stream = mapping.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            using var reader = new BinaryReader(stream, Encoding.ASCII, true);
            if (reader.ReadUInt32() != Signature)
            {
                return FrameMetrics.Unavailable("RTSS com assinatura desconhecida");
            }

            _ = reader.ReadUInt32();
            var entrySize = reader.ReadUInt32();
            var entriesOffset = reader.ReadUInt32();
            var entryCount = reader.ReadUInt32();
            if (entrySize < 284 || entrySize > 64 * 1024 || entryCount > 256 || entriesOffset >= stream.Length)
            {
                return FrameMetrics.Unavailable("RTSS com estrutura incompativel");
            }

            GetWindowThreadProcessId(GetForegroundWindow(), out var foregroundProcessId);
            RtssEntry? selected = null;
            for (var index = 0; index < entryCount; index++)
            {
                var offset = entriesOffset + index * entrySize;
                if (offset + 284 > stream.Length)
                {
                    break;
                }

                stream.Position = offset;
                var processId = reader.ReadUInt32();
                var nameBytes = reader.ReadBytes(260);
                _ = reader.ReadUInt32();
                var time0 = reader.ReadUInt32();
                var time1 = reader.ReadUInt32();
                var frames = reader.ReadUInt32();
                var frameTime = reader.ReadUInt32();
                if (processId == 0 || time1 <= time0 || frames == 0)
                {
                    continue;
                }

                var zeroIndex = Array.IndexOf(nameBytes, (byte)0);
                var processName = Encoding.ASCII.GetString(nameBytes, 0, zeroIndex < 0 ? nameBytes.Length : zeroIndex);
                var entry = new RtssEntry(processId, processName, time0, time1, frames, frameTime);
                if (processId == foregroundProcessId)
                {
                    selected = entry;
                    break;
                }

                if (selected is null || entry.Time1 > selected.Time1)
                {
                    selected = entry;
                }
            }

            if (selected is null)
            {
                return FrameMetrics.Unavailable("RTSS ativo, sem aplicativo monitorado");
            }

            var fps = 1000d * selected.Frames / (selected.Time1 - selected.Time0);
            var frameTimeMilliseconds = selected.FrameTime > 0 ? selected.FrameTime / 1000d : 1000d / fps;
            return new FrameMetrics(DateTime.Now, fps, frameTimeMilliseconds, selected.Name, "RTSS conectado");
        }
        catch (FileNotFoundException)
        {
            return FrameMetrics.Unavailable("RTSS nao detectado");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return FrameMetrics.Unavailable("RTSS indisponivel");
        }
    }

    public void Dispose()
    {
        _cancellation?.Cancel();
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);

    private sealed record RtssEntry(uint ProcessId, string Name, uint Time0, uint Time1, uint Frames, uint FrameTime);
}
