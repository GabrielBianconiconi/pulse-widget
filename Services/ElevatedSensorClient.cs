using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PulseWidget.Models;

namespace PulseWidget.Services;

public sealed class ElevatedSensorClient : ISensorMonitor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object _writerLock = new();
    private CancellationTokenSource? _cancellation;
    private Task? _connectionTask;
    private StreamWriter? _writer;
    private int _intervalMilliseconds = 1000;
    private string _gpuIdentifier = "auto";
    private string _diagnostics = "Aguardando conexao com o coletor elevado.";

    public event EventHandler<SensorSnapshot>? SnapshotAvailable;

    public void Start(int intervalMilliseconds)
    {
        if (_connectionTask is not null)
        {
            return;
        }

        _intervalMilliseconds = intervalMilliseconds;
        _cancellation = new CancellationTokenSource();
        _connectionTask = Task.Run(() => RunAsync(_cancellation.Token));
    }

    public void SetInterval(int intervalMilliseconds)
    {
        _intervalMilliseconds = Math.Clamp(intervalMilliseconds, 500, 10_000);
        Send(new SensorPipeMessage { Type = "interval", IntervalMilliseconds = _intervalMilliseconds });
    }

    public void SetSelectedGpu(string? identifier)
    {
        _gpuIdentifier = string.IsNullOrWhiteSpace(identifier) ? "auto" : identifier;
        Send(new SensorPipeMessage { Type = "gpu", GpuIdentifier = _gpuIdentifier });
    }

    public string GetDiagnosticsReport() => _diagnostics;

    public async Task StopAsync()
    {
        if (_connectionTask is null || _cancellation is null)
        {
            return;
        }

        Send(new SensorPipeMessage { Type = "shutdown" });
        await _cancellation.CancelAsync();
        try
        {
            await _connectionTask;
        }
        catch (OperationCanceledException)
        {
        }

        _writer?.Dispose();
        _writer = null;
        _connectionTask = null;
        _cancellation.Dispose();
        _cancellation = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var pipeName = $"PulseWidgetSensors_{Environment.ProcessId}_{Guid.NewGuid():N}";
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var server = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        try
        {
            LaunchElevatedHost(pipeName, token);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await server.WaitForConnectionAsync(timeout.Token);
            using var reader = new StreamReader(server, Encoding.UTF8, false, 4096, true);
            _writer = new StreamWriter(server, new UTF8Encoding(false), 4096, true) { AutoFlush = true };

            var hello = await ReadMessageAsync(reader, timeout.Token);
            if (hello?.Type != "hello" || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(hello.Token ?? string.Empty),
                    Encoding.UTF8.GetBytes(token)))
            {
                throw new InvalidDataException("Falha ao autenticar o coletor de sensores.");
            }

            Send(new SensorPipeMessage { Type = "interval", IntervalMilliseconds = _intervalMilliseconds });
            Send(new SensorPipeMessage { Type = "gpu", GpuIdentifier = _gpuIdentifier });

            while (!cancellationToken.IsCancellationRequested && server.IsConnected)
            {
                var message = await ReadMessageAsync(reader, cancellationToken);
                if (message is null)
                {
                    break;
                }

                if (message.Type == "snapshot" && message.Snapshot is not null)
                {
                    SnapshotAvailable?.Invoke(this, message.Snapshot);
                }
                else if (message.Type == "diagnostics" && message.Diagnostics is not null)
                {
                    _diagnostics = message.Diagnostics;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            SnapshotAvailable?.Invoke(this, SensorSnapshot.Unavailable("UAC recusado: metricas elevadas indisponiveis"));
        }
        catch (Exception exception)
        {
            SnapshotAvailable?.Invoke(this, SensorSnapshot.Unavailable($"Coletor indisponivel: {exception.Message}"));
        }
        finally
        {
            lock (_writerLock)
            {
                _writer = null;
            }
        }
    }

    private static void LaunchElevatedHost(string pipeName, string token)
    {
        var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("Executavel atual nao encontrado.");
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = AppContext.BaseDirectory,
            Arguments = $"--sensor-host --pipe {pipeName} --token {token} --parent {Environment.ProcessId}"
        };
        Process.Start(startInfo);
    }

    private void Send(SensorPipeMessage message)
    {
        lock (_writerLock)
        {
            if (_writer is null)
            {
                return;
            }

            try
            {
                _writer.WriteLine(JsonSerializer.Serialize(message, JsonOptions));
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private static async Task<SensorPipeMessage?> ReadMessageAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var line = await reader.ReadLineAsync(cancellationToken);
        if (line is null || line.Length > 256 * 1024)
        {
            return null;
        }

        return JsonSerializer.Deserialize<SensorPipeMessage>(line, JsonOptions);
    }

    public void Dispose()
    {
        _cancellation?.Cancel();
        _writer?.Dispose();
    }
}
